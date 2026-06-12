namespace ProjectER.Data
{
    /// <summary>
    /// 실험체 교전 거리 — BSER Character.json weaponRangeType 값과 1:1 대응
    /// </summary>
    public enum WeaponRangeType
    {
        Melee, // 근거리
        Range, // 원거리
        Both,  // 근거리+원거리
    }
}
