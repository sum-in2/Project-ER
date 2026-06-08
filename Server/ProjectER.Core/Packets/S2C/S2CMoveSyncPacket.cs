using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>
    /// 서버 → 클라이언트: 플레이어 위치 동기화 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public class S2CMoveSyncPacket
    {
        /// <summary>대상 플레이어 세션 ID</summary>
        [Key(0)] public int PlayerId;

        /// <summary>서버 확정 현재 위치 X</summary>
        [Key(1)] public float PosX;

        /// <summary>서버 확정 현재 위치 Z</summary>
        [Key(2)] public float PosZ;

        /// <summary>서버 확정 목적지 X</summary>
        [Key(3)] public float DestX;

        /// <summary>서버 확정 목적지 Z</summary>
        [Key(4)] public float DestZ;
    }
}
