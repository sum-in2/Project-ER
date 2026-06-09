using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>서버 → 클라이언트: 로그인 결과</summary>
    [MessagePackObject]
    public class S2CLoginResultPacket
    {
        [Key(0)] public bool   Success      = false;
        [Key(1)] public int    AccountId    = 0;
        [Key(2)] public string RejectReason = string.Empty;
    }
}
