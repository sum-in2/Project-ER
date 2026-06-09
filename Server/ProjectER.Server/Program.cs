using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectER.Server.Database;
using ProjectER.Server.Handlers;
using ProjectER.Server.Lobby;
using ProjectER.Server.Matchmaking;
using ProjectER.Server.Network;

namespace ProjectER.Server
{
    internal static class Program
    {
        private const int    Port         = 7777;
        private const int    RoomCapacity = 18;
        private const string DbPath       = "data/accounts.db";

        private static async Task Main()
        {
            Console.WriteLine("=== ProjectER 게임 서버 ===");

            // DB 초기화
            AccountDb         accountDb    = new(DbPath);
            AccountRepository accountRepo  = new(accountDb);

            // 의존성 구성
            LobbyManager     lobbyManager   = new(RoomCapacity);
            PacketDispatcher dispatcher     = new();

            MatchmakingConfig matchConfig   = new()
            {
                MinPlayers = 1,  // 테스트: 1명만 있어도 매치 성사
                MaxPlayers = 18,
            };
            MatchmakingManager matchmaking  = new(matchConfig);

            // 핸들러 등록
            new ConnectHandler(lobbyManager).Register(dispatcher);
            MoveHandler.Register(dispatcher);
            new MatchRequestHandler(matchmaking).Register(dispatcher);
            new LoginHandler(accountRepo).Register(dispatcher);
            // TODO: AttackHandler.Register(dispatcher);

            // 서버 시작
            TcpGameServer server = new(Port, dispatcher);

            // 세션 해제 시 로비 + 매치메이킹 큐에서도 제거
            server.OnSessionDisconnected += session =>
            {
                lobbyManager.Release(session);
                matchmaking.ReleaseSession(session);
            };

            using CancellationTokenSource cts = new();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n[Server] 종료 요청 수신");
            };

            await server.StartAsync(cts.Token);
        }
    }
}
