namespace ProjectER.Network.Protocol
{
    /// <summary>서버 → 클라이언트: 회원가입 결과</summary>
    public class S2CRegisterResultPacket
    {
        public bool   Success;
        public string RejectReason = string.Empty;
    }
}
