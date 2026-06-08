using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectER.Server.Network
{
    /// <summary>
    /// TCP 연결 수락 및 세션 생명주기 관리.
    /// 게임 로직과 분리: 연결/해제 이벤트만 노출.
    /// </summary>
    public class TcpGameServer
    {
        // ── 설정 ────────────────────────────────────────────────
        private readonly int _port;

        // ── 런타임 상태 ──────────────────────────────────────────
        private TcpListener? _listener;
        private int _nextSessionId = 1;

        private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
        private readonly PacketDispatcher _dispatcher;

        // ── 이벤트 ──────────────────────────────────────────────
        public event Action<ClientSession>? OnSessionConnected;
        public event Action<ClientSession>? OnSessionDisconnected;

        public TcpGameServer(int port, PacketDispatcher dispatcher)
        {
            _port       = port;
            _dispatcher = dispatcher;
        }

        // ── 서버 시작/중지 ───────────────────────────────────────
        public async Task StartAsync(CancellationToken ct)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"[Server] 포트 {_port} 대기 시작");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(ct);
                    int sessionId = Interlocked.Increment(ref _nextSessionId);
                    ClientSession session = new(sessionId, tcpClient, _dispatcher);

                    session.OnDisconnected += HandleDisconnected;
                    _sessions[sessionId] = session;

                    Console.WriteLine($"[Server] 세션 {sessionId} 연결됨");
                    OnSessionConnected?.Invoke(session);

                    // 세션 수신 루프를 독립 Task로 실행 (await 하지 않음)
                    _ = session.StartReceiveLoopAsync(ct);
                }
            }
            catch (OperationCanceledException) { /* 정상 종료 */ }
            finally
            {
                _listener.Stop();
                Console.WriteLine("[Server] 서버 종료");
            }
        }

        // ── 브로드캐스트 ─────────────────────────────────────────
        public async Task BroadcastAsync(byte[] data, int excludeSessionId = -1)
        {
            foreach ((int id, ClientSession session) in _sessions)
            {
                if (id == excludeSessionId)
                    continue;
                await session.SendAsync(data);
            }
        }

        // ── 내부 처리 ────────────────────────────────────────────
        private void HandleDisconnected(ClientSession session)
        {
            _sessions.TryRemove(session.SessionId, out _);
            Console.WriteLine($"[Server] 세션 {session.SessionId} 해제됨");
            OnSessionDisconnected?.Invoke(session);
        }
    }
}
