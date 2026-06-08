using MessagePack;

namespace ProjectER.Core.Packets.S2C
{
    /// <summary>
    /// 서버 → 클라이언트: 접속 수락 응답
    /// </summary>
    [MessagePackObject]
    public class S2CConnectedPacket
    {
        /// <summary>서버가 발급한 세션 ID</summary>
        [Key(0)] public int SessionId;

        /// <summary>접속 수락 여부 (false면 클라이언트 연결 끊기)</summary>
        [Key(1)] public bool Accepted;

        /// <summary>거절 사유 (Accepted == false 일 때만 유효)</summary>
        [Key(2)] public string RejectReason = string.Empty;
    }
}
