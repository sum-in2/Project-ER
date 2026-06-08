using MessagePack;

namespace ProjectER.Core.Packets
{
    /// <summary>
    /// 모든 패킷 앞에 붙는 고정 헤더 (4바이트).
    /// [총 길이(2) | 타입(2)]
    /// </summary>
    [MessagePackObject]
    public struct PacketHeader
    {
        /// <summary>헤더 포함 전체 패킷 바이트 길이</summary>
        [Key(0)] public ushort TotalLength;

        /// <summary>패킷 종류 식별자</summary>
        [Key(1)] public PacketType Type;

        public const int Size = 4; // ushort(2) + ushort(2)
    }
}
