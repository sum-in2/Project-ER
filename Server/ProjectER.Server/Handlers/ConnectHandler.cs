using System;
using MessagePack;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.C2S;
using ProjectER.Core.Packets.S2C;
using ProjectER.Server.Lobby;
using ProjectER.Server.Network;

namespace ProjectER.Server.Handlers
{
    /// <summary>
    /// C2S_Connect 패킷 처리.
    /// 버전 검증 → LobbyManager 배정 → S2C_Connected 응답.
    /// </summary>
    public class ConnectHandler
    {
        private const string ServerVersion = "0.1.0";

        private readonly LobbyManager _lobbyManager;

        public ConnectHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }

        public void Register(PacketDispatcher dispatcher)
        {
            dispatcher.Register(PacketType.C2S_Connect, Handle);
        }

        private void Handle(ClientSession session, byte[] body)
        {
            C2SConnectPacket request = MessagePackSerializer.Deserialize<C2SConnectPacket>(body);

            // 버전 검증
            if (request.ClientVersion != ServerVersion)
            {
                Console.WriteLine($"[ConnectHandler] 세션 {session.SessionId} 버전 불일치: {request.ClientVersion}");
                SendResponse(session, accepted: false, $"버전 불일치 (서버: {ServerVersion})");
                session.Disconnect();
                return;
            }

            // 로비 배정
            _lobbyManager.Assign(session);

            // 접속 수락 응답
            SendResponse(session, accepted: true, string.Empty);
            Console.WriteLine($"[ConnectHandler] 세션 {session.SessionId} 접속 수락");
        }

        private static void SendResponse(ClientSession session, bool accepted, string rejectReason)
        {
            S2CConnectedPacket response = new()
            {
                SessionId    = session.SessionId,
                Accepted     = accepted,
                RejectReason = rejectReason,
            };

            byte[] packet = PacketBuilder.Build(PacketType.S2C_Connected, response);
            _ = session.SendAsync(packet);
        }
    }
}
