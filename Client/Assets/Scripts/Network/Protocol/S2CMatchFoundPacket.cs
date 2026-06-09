namespace ProjectER.Network.Protocol
{
    /// <summary>서버 → 클라이언트: 매치 성사 알림</summary>
    public class S2CMatchFoundPacket
    {
        /// <summary>매치 고유 ID</summary>
        public int MatchId;

        /// <summary>매치에 참여하는 플레이어 수</summary>
        public int PlayerCount;
    }
}
