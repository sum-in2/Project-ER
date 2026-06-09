namespace ProjectER.Server.Matchmaking
{
    /// <summary>
    /// 매치메이킹 동작 설정.
    /// Program.cs에서 생성 시 주입.
    /// </summary>
    public class MatchmakingConfig
    {
        /// <summary>매치 성사에 필요한 최소 인원 (기본 1 = 테스트용, 실제 서비스 시 변경)</summary>
        public int MinPlayers { get; set; } = 1;

        /// <summary>한 매치에 들어갈 최대 인원</summary>
        public int MaxPlayers { get; set; } = 18;
    }
}
