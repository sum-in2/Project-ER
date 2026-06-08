namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 서버 → 클라이언트: 접속 수락/거절 응답
    /// </summary>
    public class S2CConnectedPacket
    {
        public int    SessionId;
        public bool   Accepted;
        public string RejectReason;
    }
}
