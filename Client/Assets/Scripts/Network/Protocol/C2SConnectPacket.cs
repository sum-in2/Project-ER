namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 클라이언트 → 서버: 최초 접속 요청
    /// </summary>
    public class C2SConnectPacket
    {
        public string ClientVersion;
    }
}
