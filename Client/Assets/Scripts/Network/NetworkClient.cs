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

        // ── 상태 ─────────────────────────────────────────────────
        public bool IsConnected { get; private set; }
        public int  SessionId   { get; private set; }

        // ── 이벤트 (메인 스레드에서 발생) ────────────────────────
        public event Action           OnConnectSuccess;
        public event Action<string>   OnConnectFailed;
        public event Action           OnDisconnected;

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
            // 중복 인스턴스 방지
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
            // 수신 큐 드레인 (메인 스레드에서 안전하게 처리)
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
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(host, port);
                _stream    = _tcpClient.GetStream();
                IsConnected = true;

                // 수신 스레드 시작
                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();

                // 접속 패킷 전송
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

            IsConnected = false;
            _tcpClient?.Close();
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
                    // 헤더 읽기
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

            IsConnected = false;
            _tcpClient?.Close();

            // 메인 스레드에서 이벤트 발생하도록 큐에 특수 마커 삽입
            _receiveQueue.Enqueue((PacketType.C2S_Disconnect, Array.Empty<byte>()));
        }

        // ── 내부: 핸들러 등록 ────────────────────────────────────
        private void RegisterHandlers()
        {
            _dispatcher.Register(PacketType.S2C_Connected, HandleConnected);
            _dispatcher.Register(PacketType.C2S_Disconnect, _ => OnDisconnected?.Invoke());
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
    }
}
