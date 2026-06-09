namespace ProjectER.Network.Protocol
{
    /// <summary>클라이언트 → 서버: 로그인 요청</summary>
    public class C2SLoginPacket
    {
        public string Username = string.Empty;
        public string Password = string.Empty;
    }
}
