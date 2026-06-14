using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using ProjectER.Network.Protocol;
using UnityEngine;

namespace ProjectER.Network
{
    /// <summary>
    /// 서버와의 TCP 연결을 관리하는 클라이언트 네트워크 레이어.
    /// DontDestroyOnLoad로 씬 전환 간 유지.
    /// static Instance는 씬 전환 지속을 위한 예외적 사용.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        // ── 싱글톤 ───────────────────────────────────────────────
        public static NetworkClient Instance { get; private set; }

        // ── 서버 버전 ─────────────────────────────────────────────
        private const string ClientVersion = "0.1.0";

        // ── 픽 단계 제한시간 동기화 ──────────────────────────────
        private const float DefaultPickDuration = 30f;
        private float _pickPhaseDuration = -1f;
        private float _pickPhaseStartRealtime;

        // ── 상태 ─────────────────────────────────────────────────
        public bool IsConnected   { get; private set; }
        public bool IsMatchmaking { get; private set; }
        public bool IsLoggedIn    { get; private set; }
        public int  SessionId     { get; private set; }
        public int  AccountId     { get; private set; }

        // ── 이벤트 (메인 스레드에서 발생) ────────────────────────
        public event Action           OnConnectSuccess;
        public event Action<string>   OnConnectFailed;
        public event Action           OnDisconnected;

        public event Action<int>      OnMatchQueued;    // 인수: 큐 순서(1-based)
        public event Action           OnMatchCancelled;
        public event Action<int, int> OnMatchFound;     // 인수: matchId, playerCount

        public event Action           OnPickDodged;     // 픽 제한시간 내 미선택자 존재 → 매치 취소

        /// <summary>픽 1단계(실험체 선택) 남은 시간(초). 서버가 보낸 시작 시각/제한시간 기준으로 계산.</summary>
        public float PickPhaseRemainingSeconds
        {
            get
            {
                if (_pickPhaseDuration < 0f)
                    return DefaultPickDuration;

                float elapsed = Time.realtimeSinceStartup - _pickPhaseStartRealtime;
                return Mathf.Max(0f, _pickPhaseDuration - elapsed);
            }
        }

        public event Action           OnRegisterSuccess;
        public event Action<string>   OnRegisterFailed;
        public event Action           OnLoginSuccess;
        public event Action<string>   OnLoginFailed;

        // ── 내부 컴포넌트 ─────────────────────────────────────────
        private TcpClient              _tcpClient;
        private NetworkStream          _stream;
        private Thread                 _receiveThread;
        private ClientPacketDispatcher _dispatcher;

        // ── 수신 큐 (백그라운드 스레드 → 메인 스레드) ────────────
        private readonly ConcurrentQueue<(PacketType type, byte[] body)> _receiveQueue = new();

        // ── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _dispatcher = new ClientPacketDispatcher();
            RegisterHandlers();
        }

        private void Update()
        {
            while (_receiveQueue.TryDequeue(out (PacketType type, byte[] body) packet))
            {
                _dispatcher.Dispatch(packet.type, packet.body);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // ── 연결 ──────────────────────────────────────────────────
        public async void Connect(string host, int port)
        {
            if (IsConnected)
                return;

            try
            {
                _tcpClient  = new TcpClient();
                await _tcpClient.ConnectAsync(host, port);
                _stream     = _tcpClient.GetStream();
                IsConnected = true;

                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();

                SendConnectPacket();
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Debug.LogWarning($"[NetworkClient] 연결 실패: {ex.Message}");
                OnConnectFailed?.Invoke(ex.Message);
            }
        }

        // ── 송신 ──────────────────────────────────────────────────
        public void Send(byte[] data)
        {
            if (!IsConnected || _stream == null)
                return;

            try
            {
                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkClient] 송신 실패: {ex.Message}");
                HandleDisconnect();
            }
        }

        // ── 연결 해제 ─────────────────────────────────────────────
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            IsConnected   = false;
            IsMatchmaking = false;
            IsLoggedIn    = false;
            _tcpClient?.Close();
        }

        // ── 인증 API ──────────────────────────────────────────────
        /// <summary>회원가입 요청.</summary>
        public void Register(string username, string password)
        {
            if (!IsConnected)
                return;

            C2SRegisterPacket packet = new() { Username = username, Password = password };
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_Register, body);
            Send(data);
        }

        /// <summary>로그인 요청.</summary>
        public void Login(string username, string password)
        {
            if (!IsConnected)
                return;

            C2SLoginPacket packet = new() { Username = username, Password = password };
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_Login, body);
            Send(data);
        }

        // ── 매치메이킹 API ────────────────────────────────────────
        /// <summary>매치메이킹 큐 진입 요청. 이미 대기 중이면 무시.</summary>
        public void RequestMatch()
        {
            if (!IsConnected || IsMatchmaking)
                return;

            C2SMatchRequestPacket packet = new();
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_MatchRequest, body);
            Send(data);
        }

        /// <summary>매치메이킹 큐 취소 요청. 대기 중이 아니면 무시.</summary>
        public void CancelMatch()
        {
            if (!IsConnected || !IsMatchmaking)
                return;

            C2SMatchCancelPacket packet = new();
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_MatchCancel, body);
            Send(data);
        }

        // ── 픽 API ────────────────────────────────────────────────
        /// <summary>픽 화면 실험체 선택 요청.</summary>
        public void SelectCharacter(int characterId)
        {
            if (!IsConnected)
                return;

            C2SSelectCharacterPacket packet = new() { CharacterId = characterId };
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_SelectCharacter, body);
            Send(data);
        }

        // ── 내부: 접속 패킷 전송 ─────────────────────────────────
        private void SendConnectPacket()
        {
            C2SConnectPacket packet = new() { ClientVersion = ClientVersion };
            byte[] body = PacketSerializer.Serialize(packet);
            byte[] data = PacketBuilder.Build(PacketType.C2S_Connect, body);
            Send(data);
        }

        // ── 내부: 수신 루프 (백그라운드 스레드) ──────────────────
        private void ReceiveLoop()
        {
            byte[] headerBuf = new byte[PacketHeader.Size];

            while (IsConnected)
            {
                try
                {
                    ReadExact(headerBuf, 0, PacketHeader.Size);

                    ushort totalLength = (ushort)(headerBuf[0] | (headerBuf[1] << 8));
                    ushort typeRaw     = (ushort)(headerBuf[2] | (headerBuf[3] << 8));
                    PacketType type    = (PacketType)typeRaw;

                    int bodyLength = totalLength - PacketHeader.Size;
                    byte[] body = new byte[bodyLength]; // ⚠️ GC 주의
                    if (bodyLength > 0)
                        ReadExact(body, 0, bodyLength);

                    _receiveQueue.Enqueue((type, body));
                }
                catch
                {
                    break;
                }
            }

            HandleDisconnect();
        }

        private void ReadExact(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new Exception("연결 끊김");
                totalRead += read;
            }
        }

        private void HandleDisconnect()
        {
            if (!IsConnected)
                return;

            IsConnected   = false;
            IsMatchmaking = false;
            IsLoggedIn    = false;
            _tcpClient?.Close();

            _receiveQueue.Enqueue((PacketType.C2S_Disconnect, Array.Empty<byte>()));
        }

        // ── 내부: 핸들러 등록 ────────────────────────────────────
        private void RegisterHandlers()
        {
            _dispatcher.Register(PacketType.S2C_Connected,   HandleConnected);
            _dispatcher.Register(PacketType.C2S_Disconnect,  _ => OnDisconnected?.Invoke());

            _dispatcher.Register(PacketType.S2C_MatchQueued,    HandleMatchQueued);
            _dispatcher.Register(PacketType.S2C_MatchCancelled, HandleMatchCancelled);
            _dispatcher.Register(PacketType.S2C_MatchFound,     HandleMatchFound);

            _dispatcher.Register(PacketType.S2C_RegisterResult, HandleRegisterResult);
            _dispatcher.Register(PacketType.S2C_LoginResult,    HandleLoginResult);

            _dispatcher.Register(PacketType.S2C_PickDodged, HandlePickDodged);
            _dispatcher.Register(PacketType.S2C_PickStarted, HandlePickStarted);
        }

        private void HandleConnected(byte[] body)
        {
            S2CConnectedPacket response = PacketSerializer.DeserializeConnected(body);

            if (response.Accepted)
            {
                SessionId = response.SessionId;
                Debug.Log($"[NetworkClient] 접속 수락 (SessionId: {SessionId})");
                OnConnectSuccess?.Invoke();
            }
            else
            {
                IsConnected = false;
                _tcpClient?.Close();
                Debug.LogWarning($"[NetworkClient] 접속 거절: {response.RejectReason}");
                OnConnectFailed?.Invoke(response.RejectReason);
            }
        }

        private void HandleMatchQueued(byte[] body)
        {
            S2CMatchQueuedPacket response = PacketSerializer.DeserializeMatchQueued(body);
            IsMatchmaking = true;
            Debug.Log($"[NetworkClient] 매치메이킹 큐 진입 (순서: {response.QueuePosition})");
            OnMatchQueued?.Invoke(response.QueuePosition);
        }

        private void HandleMatchCancelled(byte[] body)
        {
            _ = PacketSerializer.DeserializeMatchCancelled(body);
            IsMatchmaking = false;
            Debug.Log("[NetworkClient] 매치메이킹 취소됨");
            OnMatchCancelled?.Invoke();
        }

        private void HandleMatchFound(byte[] body)
        {
            S2CMatchFoundPacket response = PacketSerializer.DeserializeMatchFound(body);
            IsMatchmaking = false;
            Debug.Log($"[NetworkClient] 매치 성사! (MatchId: {response.MatchId}, 플레이어: {response.PlayerCount}명)");
            OnMatchFound?.Invoke(response.MatchId, response.PlayerCount);
        }

        private void HandleRegisterResult(byte[] body)
        {
            S2CRegisterResultPacket response = PacketSerializer.DeserializeRegisterResult(body);
            if (response.Success)
            {
                Debug.Log("[NetworkClient] 회원가입 성공");
                OnRegisterSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[NetworkClient] 회원가입 실패: {response.RejectReason}");
                OnRegisterFailed?.Invoke(response.RejectReason);
            }
        }

        private void HandleLoginResult(byte[] body)
        {
            S2CLoginResultPacket response = PacketSerializer.DeserializeLoginResult(body);
            if (response.Success)
            {
                AccountId  = response.AccountId;
                IsLoggedIn = true;
                Debug.Log($"[NetworkClient] 로그인 성공 (AccountId: {AccountId})");
                OnLoginSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[NetworkClient] 로그인 실패: {response.RejectReason}");
                OnLoginFailed?.Invoke(response.RejectReason);
            }
        }

        private void HandlePickDodged(byte[] body)
        {
            _ = PacketSerializer.DeserializePickDodged(body);
            Debug.Log("[NetworkClient] 픽 제한시간 내 미선택자 존재 → 매치 취소");
            OnPickDodged?.Invoke();
        }

        private void HandlePickStarted(byte[] body)
        {
            S2CPickStartedPacket response = PacketSerializer.DeserializePickStarted(body);
            _pickPhaseDuration      = response.DurationSeconds;
            _pickPhaseStartRealtime = Time.realtimeSinceStartup;
            Debug.Log($"[NetworkClient] 픽 단계 시작 (제한시간: {response.DurationSeconds}초)");
        }
    }
}
