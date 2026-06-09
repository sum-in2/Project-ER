namespace ProjectER.Network.Protocol
{
    /// <summary>서버 → 클라이언트: 로그인 결과</summary>
    public class S2CLoginResultPacket
    {
        public bool   Success;
        public int    AccountId;
        public string RejectReason = string.Empty;
    }
}
