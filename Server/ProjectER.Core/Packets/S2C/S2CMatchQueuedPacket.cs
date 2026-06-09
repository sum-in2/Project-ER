using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>서버 → 클라이언트: 매치메이킹 큐 진입 확인</summary>
    [MessagePackObject]
    public class S2CMatchQueuedPacket
    {
        /// <summary>현재 큐 순서 (1-based)</summary>
        [Key(0)] public int QueuePosition;
    }
}
