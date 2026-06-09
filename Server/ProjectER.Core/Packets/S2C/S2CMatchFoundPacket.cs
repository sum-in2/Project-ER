using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>서버 → 클라이언트: 매치 성사 알림</summary>
    [MessagePackObject]
    public class S2CMatchFoundPacket
    {
        /// <summary>매치 고유 ID</summary>
        [Key(0)] public int MatchId;

        /// <summary>매치에 참여하는 플레이어 수</summary>
        [Key(1)] public int PlayerCount;
    }
}
