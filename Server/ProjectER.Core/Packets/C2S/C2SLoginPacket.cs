using MessagePack;

namespace ProjectER.Core.Packets.C2S
{
    /// <summary>클라이언트 → 서버: 로그인 요청</summary>
    [MessagePackObject]
    public class C2SLoginPacket
    {
        [Key(0)] public string Username = string.Empty;
        [Key(1)] public string Password = string.Empty;
    }
}
