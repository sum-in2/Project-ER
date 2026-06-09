using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProjectER.Core.Packets;

namespace ProjectER.Server.Network
{
    /// <summary>
    /// 연결된 클라이언트 1개를 나타내는 세션.
    /// 수신 루프, 송신 큐, 연결 해제 처리를 담당.
    /// </summary>
    public class ClientSession : IDisposable
    {
        // ── 상태 ────────────────────────────────────────────────
        public int  SessionId   { get; }
        public int? AccountId   { get; set; }   // 로그인 성공 후 설정
        public bool IsConnected => _tcpClient?.Connected ?? false;

        // ── 의존성 ──────────────────────────────────────────────
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly PacketDispatcher _dispatcher;

        // ── 수신 버퍼 ───────────────────────────────────────────
        private readonly byte[] _recvBuffer = new byte[4096];

        // ── 패킷 크기 상한 ───────────────────────────────────────
        private const int MaxBodySize = 4096;

        // ── 이벤트 ──────────────────────────────────────────────
        public event Action<ClientSession>? OnDisconnected;

        public ClientSession(int sessionId, TcpClient tcpClient, PacketDispatcher dispatcher)
        {
            SessionId    = sessionId;
            _tcpClient   = tcpClient;
            _stream      = tcpClient.GetStream();
            _dispatcher  = dispatcher;
        }

        // ── 수신 루프 ────────────────────────────────────────────
        public async Task StartReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // 헤더 읽기
                    int headerBytesRead = await ReadExactAsync(_recvBuffer, 0, PacketHeader.Size, ct);
                    if (headerBytesRead == 0)
                        break; // 연결 종료

                    ushort totalLength = BitConverter.ToUInt16(_recvBuffer, 0);
                    ushort typeRaw     = BitConverter.ToUInt16(_recvBuffer, 2);
                    PacketType type    = (PacketType)typeRaw;

                    int bodyLength = totalLength - PacketHeader.Size;
                    if (bodyLength < 0 || bodyLength > MaxBodySize)
                    {
                        Console.WriteLine($"[Session {SessionId}] 비정상 패킷 크기: {bodyLength} → 연결 종료");
                        break;
                    }

                    // 바디 읽기
                    byte[] body = new byte[bodyLength]; // ⚠️ GC 주의: 패킷마다 할당. 추후 ArrayPool 적용 권장
                    if (bodyLength > 0)
                    {
                        int bodyBytesRead = await ReadExactAsync(body, 0, bodyLength, ct);
                        if (bodyBytesRead == 0)
                            break;
                    }

                    _dispatcher.Dispatch(this, type, body);
                }
            }
            catch (OperationCanceledException) { /* 정상 종료 */ }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId}] 수신 오류: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        // ── 송신 ─────────────────────────────────────────────────
        public async Task SendAsync(byte[] data, CancellationToken ct = default)
        {
            try
            {
                await _stream.WriteAsync(data, 0, data.Length, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId}] 송신 오류: {ex.Message}");
                Disconnect();
            }
        }

        // ── 연결 해제 ────────────────────────────────────────────
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _tcpClient.Close();
            OnDisconnected?.Invoke(this);
        }

        // ── 내부 유틸 ────────────────────────────────────────────
        /// <summary>정확히 count 바이트를 읽을 때까지 반복 수신. 연결 끊기면 0 반환.</summary>
        private async Task<int> ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
                if (read == 0)
                    return 0;
                totalRead += read;
            }
            return totalRead;
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}
