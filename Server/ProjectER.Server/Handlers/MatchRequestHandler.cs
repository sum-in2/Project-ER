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
            _ = MessagePackSerializer.Deserialize<C2SMatchRequestPacket>(body);
            _matchmaking.EnqueueSession(session);
        }

        private void HandleMatchCancel(ClientSession session, byte[] body)
        {
            _ = MessagePackSerializer.Deserialize<C2SMatchCancelPacket>(body);
            _matchmaking.CancelSession(session);
        }
    }
}
