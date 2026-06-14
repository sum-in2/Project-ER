using System;
using MessagePack;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.C2S;
using ProjectER.Server.Network;
using ProjectER.Server.Pick;

namespace ProjectER.Server.Handlers
{
    /// <summary>
    /// C2S_SelectCharacter 패킷 처리.
    /// </summary>
    public class PickHandler
    {
        private readonly PickManager _pickManager;

        public PickHandler(PickManager pickManager)
        {
            _pickManager = pickManager;
        }

        public void Register(PacketDispatcher dispatcher)
        {
            dispatcher.Register(PacketType.C2S_SelectCharacter, HandleSelectCharacter);
        }

        private void HandleSelectCharacter(ClientSession session, byte[] body)
        {
            // 인증 전 요청 차단
            if (session.AccountId == null)
            {
                Console.WriteLine($"[PickHandler] 세션 {session.SessionId} 미인증 상태로 실험체 선택 요청 → 거부");
                return;
            }

            C2SSelectCharacterPacket packet = MessagePackSerializer.Deserialize<C2SSelectCharacterPacket>(body);
            _pickManager.HandleSelectCharacter(session, packet.CharacterId);
        }
    }
}
