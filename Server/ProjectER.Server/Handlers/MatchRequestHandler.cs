using MessagePack;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.C2S;
using ProjectER.Server.Matchmaking;
using ProjectER.Server.Network;

namespace ProjectER.Server.Handlers
{
    /// <summary>
    /// C2S_MatchRequest / C2S_MatchCancel 패킷 처리.
    /// </summary>
    public class MatchRequestHandler
    {
        private readonly MatchmakingManager _matchmaking;

        public MatchRequestHandler(MatchmakingManager matchmaking)
        {
            _matchmaking = matchmaking;
        }

        public void Register(PacketDispatcher dispatcher)
        {
            dispatcher.Register(PacketType.C2S_MatchRequest, HandleMatchRequest);
            dispatcher.Register(PacketType.C2S_MatchCancel, HandleMatchCancel);
        }

        private void HandleMatchRequest(ClientSession session, byte[] body)
        {
            // 인증 전 요청 차단
            if (session.AccountId == null)
            {
                Console.WriteLine($"[MatchRequestHandler] 세션 {session.SessionId} 미인증 상태로 매치 요청 → 거부");
                return;
            }

            _ = MessagePackSerializer.Deserialize<C2SMatchRequestPacket>(body);
            _matchmaking.EnqueueSession(session);
        }

        private void HandleMatchCancel(ClientSession session, byte[] body)
        {
            // 인증 전 요청 차단
            if (session.AccountId == null)
                return;

            _ = MessagePackSerializer.Deserialize<C2SMatchCancelPacket>(body);
            _matchmaking.CancelSession(session);
        }
    }
}
