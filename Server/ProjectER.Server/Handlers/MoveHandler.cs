using System;
using MessagePack;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.C2S;
using ProjectER.Server.Network;

namespace ProjectER.Server.Handlers
{
    /// <summary>
    /// C2S_Move 패킷 처리.
    /// 수신한 이동 요청을 검증하고 GameRoom으로 전달.
    /// </summary>
    public static class MoveHandler
    {
        public static void Register(PacketDispatcher dispatcher)
        {
            dispatcher.Register(PacketType.C2S_Move, Handle);
        }

        private static void Handle(ClientSession session, byte[] body)
        {
            // 인증 전 요청 차단
            if (session.AccountId == null)
            {
                Console.WriteLine($"[MoveHandler] 세션 {session.SessionId} 미인증 상태로 이동 요청 → 거부");
                return;
            }

            C2SMovePacket packet = MessagePackSerializer.Deserialize<C2SMovePacket>(body);

            // TODO: GameRoom에서 플레이어 위치 갱신 및 브로드캐스트
            Console.WriteLine($"[MoveHandler] 세션 {session.SessionId} → 목적지 ({packet.DestX:F1}, {packet.DestZ:F1})");
        }
    }
}
