using System;
using System.Buffers.Binary;
using MessagePack;
using ProjectER.Core.Packets;

namespace ProjectER.Server.Network
{
    /// <summary>
    /// 패킷 직렬화 유틸.
    /// [헤더 4바이트 | MessagePack 바디] 형식으로 조립.
    /// </summary>
    public static class PacketBuilder
    {
        /// <summary>패킷 타입 + MessagePack 직렬화 가능한 객체로 전송용 바이트 배열 생성.</summary>
        public static byte[] Build<T>(PacketType type, T payload)
        {
            byte[] body = MessagePackSerializer.Serialize(payload); // ⚠️ GC 주의: 추후 ArrayPool 적용 권장
            return BuildRaw(type, body);
        }

        /// <summary>이미 직렬화된 바디 바이트로 패킷 조립.</summary>
        public static byte[] BuildRaw(PacketType type, byte[] body)
        {
            int totalLength = PacketHeader.Size + body.Length;
            byte[] packet = new byte[totalLength]; // ⚠️ GC 주의

            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)totalLength);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), (ushort)type);
            body.CopyTo(packet.AsSpan(PacketHeader.Size));

            return packet;
        }
    }
}
