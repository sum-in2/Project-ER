namespace ProjectER.Network.Protocol
{
    /// <summary>
    /// 클라이언트 ↔ 서버 패킷 식별자.
    /// 서버 ProjectER.Core.Packets.PacketType 과 값 일치 유지 필요.
    /// </summary>
    public enum PacketType : ushort
    {
        // ── 연결 ──────────────────────────────
        C2S_Connect     = 100,
        S2C_Connected   = 101,
        C2S_Disconnect  = 102,

        // ── 이동 ──────────────────────────────
        C2S_Move        = 200,
        S2C_MoveSync    = 201,

        // ── 전투 ──────────────────────────────
        C2S_Attack      = 300,
        S2C_TakeDamage  = 301,
        S2C_Die         = 302,
    }
}
