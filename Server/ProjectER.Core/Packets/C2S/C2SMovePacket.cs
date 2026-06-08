using MessagePack;

namespace ProjectER.Core.Packets.C2S
{
    /// <summary>
    /// 클라이언트 → 서버: 이동 목적지 전송
    /// </summary>
    [MessagePackObject]
    public class C2SMovePacket
    {
        /// <summary>목적지 X 좌표</summary>
        [Key(0)] public float DestX;

        /// <summary>목적지 Z 좌표 (Unity 3D 기준 Y 아님)</summary>
        [Key(1)] public float DestZ;

        /// <summary>패킷 생성 시각 (서버 지연 측정용)</summary>
        [Key(2)] public long Timestamp;
    }
}
