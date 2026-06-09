using System.Collections.Generic;
using ProjectER.Network.Protocol;

namespace ProjectER.Network
{
    /// <summary>
    /// 패킷별 MessagePack 직렬화/역직렬화.
    /// 새 패킷 추가 시 Serialize/Deserialize 쌍으로 등록.
    /// </summary>
    public static class PacketSerializer
    {
        // ── 직렬화 (클라이언트 → 서버) ──────────────────────────
        public static byte[] Serialize(C2SConnectPacket packet)
        {
            List<byte> buf = new List<byte>(); // ⚠️ GC 주의
            MiniMsgPack.WriteArrayHeader(buf, 1);
            MiniMsgPack.WriteString(buf, packet.ClientVersion);
            return buf.ToArray();
        }

        public static byte[] Serialize(C2SMatchRequestPacket _)
        {
            List<byte> buf = new List<byte>(); // ⚠️ GC 주의
            MiniMsgPack.WriteArrayHeader(buf, 0);
            return buf.ToArray();
        }

        public static byte[] Serialize(C2SMatchCancelPacket _)
        {
            List<byte> buf = new List<byte>(); // ⚠️ GC 주의
            MiniMsgPack.WriteArrayHeader(buf, 0);
            return buf.ToArray();
        }

        public static byte[] Serialize(C2SRegisterPacket packet)
        {
            List<byte> buf = new List<byte>(); // ⚠️ GC 주의
            MiniMsgPack.WriteArrayHeader(buf, 2);
            MiniMsgPack.WriteString(buf, packet.Username);
            MiniMsgPack.WriteString(buf, packet.Password);
            return buf.ToArray();
        }

        public static byte[] Serialize(C2SLoginPacket packet)
        {
            List<byte> buf = new List<byte>(); // ⚠️ GC 주의
            MiniMsgPack.WriteArrayHeader(buf, 2);
            MiniMsgPack.WriteString(buf, packet.Username);
            MiniMsgPack.WriteString(buf, packet.Password);
            return buf.ToArray();
        }

        // ── 역직렬화 (서버 → 클라이언트) ────────────────────────
        public static S2CConnectedPacket DeserializeConnected(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CConnectedPacket
            {
                SessionId    = MiniMsgPack.ReadInt32(data, ref offset),
                Accepted     = MiniMsgPack.ReadBool(data, ref offset),
                RejectReason = MiniMsgPack.ReadString(data, ref offset),
            };
        }

        public static S2CMatchQueuedPacket DeserializeMatchQueued(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CMatchQueuedPacket
            {
                QueuePosition = MiniMsgPack.ReadInt32(data, ref offset),
            };
        }

        public static S2CMatchCancelledPacket DeserializeMatchCancelled(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CMatchCancelledPacket();
        }

        public static S2CMatchFoundPacket DeserializeMatchFound(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CMatchFoundPacket
            {
                MatchId     = MiniMsgPack.ReadInt32(data, ref offset),
                PlayerCount = MiniMsgPack.ReadInt32(data, ref offset),
            };
        }

        public static S2CRegisterResultPacket DeserializeRegisterResult(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CRegisterResultPacket
            {
                Success      = MiniMsgPack.ReadBool(data, ref offset),
                RejectReason = MiniMsgPack.ReadString(data, ref offset),
            };
        }

        public static S2CLoginResultPacket DeserializeLoginResult(byte[] data)
        {
            int offset = 0;
            MiniMsgPack.ReadArrayHeader(data, ref offset);
            return new S2CLoginResultPacket
            {
                Success      = MiniMsgPack.ReadBool(data, ref offset),
                AccountId    = MiniMsgPack.ReadInt32(data, ref offset),
                RejectReason = MiniMsgPack.ReadString(data, ref offset),
            };
        }
    }
}
