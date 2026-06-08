using MessagePack;

namespace ProjectER.Core.Packets.C2S
{
    /// <summary>
    /// 클라이언트 → 서버: 최초 접속 요청
    /// </summary>
    [MessagePackObject]
    public class C2SConnectPacket
    {
        /// <summary>클라이언트 버전 (서버와 불일치 시 거절 가능)</summary>
        [Key(0)] public string ClientVersion = string.Empty;
    }
}
