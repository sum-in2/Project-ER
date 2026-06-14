using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>서버 → 클라이언트: 픽 단계 시작 (제한시간 동기화)</summary>
    [MessagePackObject]
    public class S2CPickStartedPacket
    {
        /// <summary>픽 1단계(실험체 선택) 제한시간(초)</summary>
        [Key(0)] public float DurationSeconds;
    }
}
