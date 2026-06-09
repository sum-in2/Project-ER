namespace ProjectER.Network.Protocol
{
    /// <summary>서버 → 클라이언트: 매치메이킹 큐 진입 확인</summary>
    public class S2CMatchQueuedPacket
    {
        /// <summary>현재 큐 순서 (1-based)</summary>
        public int QueuePosition;
    }
}
