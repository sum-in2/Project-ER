using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>서버 → 클라이언트: 회원가입 결과</summary>
    [MessagePackObject]
    public class S2CRegisterResultPacket
    {
        [Key(0)] public bool   Success      = false;
        [Key(1)] public string RejectReason = string.Empty;
    }
}
