using MessagePack;

namespace ProjectER.Core.Packets.C2S
{
    /// <summary>클라이언트 → 서버: 픽 화면 실험체 선택</summary>
    [MessagePackObject]
    public class C2SSelectCharacterPacket
    {
        /// <summary>선택한 실험체 ID (CharacterData.BserCode)</summary>
        [Key(0)] public int CharacterId;
    }
}
