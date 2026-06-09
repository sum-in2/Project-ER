namespace ProjectER.Network.Protocol
{
    /// <summary>클라이언트 → 서버: 회원가입 요청</summary>
    public class C2SRegisterPacket
    {
        public string Username = string.Empty;
        public string Password = string.Empty;
    }
}
