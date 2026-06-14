namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 클라이언트 → 서버: 픽 화면 실험체 선택
    /// </summary>
    public class C2SSelectCharacterPacket
    {
        /// <summary>선택한 실험체 ID (CharacterData.BserCode)</summary>
        public int CharacterId;
    }
}
