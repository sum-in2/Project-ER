namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 서버 → 클라이언트: 픽 단계 시작 (제한시간 동기화)
    /// </summary>
    public class S2CPickStartedPacket
    {
        public float DurationSeconds;
    }
}
