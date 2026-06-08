namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 모든 패킷 앞에 붙는 고정 헤더 (4바이트).
    /// [총 길이(2) | 타입(2)]
    /// 서버 ProjectER.Core.Packets.PacketHeader 와 동일 구조.
    /// </summary>
    public static class PacketHeader
    {
        /// <summary>헤더 고정 크기 (바이트)</summary>
        public const int Size = 4;
    }
}
