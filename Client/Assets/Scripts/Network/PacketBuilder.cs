using System;
using ProjectER.Network.Protocol;

namespace ProjectER.Network
{
    /// <summary>
    /// [헤더 4바이트 | 직렬화된 바디] 형식으로 전송용 바이트 배열 조립.
    /// </summary>
    public static class PacketBuilder
    {
        public static byte[] Build(PacketType type, byte[] body)
        {
            int totalLength = PacketHeader.Size + body.Length;
            byte[] packet = new byte[totalLength]; // ⚠️ GC 주의

            // 헤더: 리틀엔디안
            packet[0] = (byte)(totalLength & 0xFF);
            packet[1] = (byte)(totalLength >> 8);
            packet[2] = (byte)((ushort)type & 0xFF);
            packet[3] = (byte)((ushort)type >> 8);

            Array.Copy(body, 0, packet, PacketHeader.Size, body.Length);
            return packet;
        }
    }
}
