using MessagePack;

namespace ProjectER.Core.Packets.C2S
{
    /// <summary>클라이언트 → 서버: 회원가입 요청</summary>
    [MessagePackObject]
    public class C2SRegisterPacket
    {
        [Key(0)] public string Username = string.Empty;
        [Key(1)] public string Password = string.Empty;
    }
}
