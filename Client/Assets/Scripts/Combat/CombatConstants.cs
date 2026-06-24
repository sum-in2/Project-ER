namespace ProjectER.Combat
{
    /// <summary>
    /// 전투 데미지 계산에 쓰이는 고정 상수 (매직넘버 금지).
    /// 수치 출처: docs/combat-damage-formula.md (Eternal Return 공식 위키 기준).
    /// </summary>
    public static class CombatConstants
    {
        public const float DefenseConstant = 100f;  // 방어 상수 (감소율 = 방어력 / (방어력 + 100))
        public const float MinDamage       = 1f;    // 최소 피해 (고정)
        public const float MinAmp          = 0.1f;  // 피해 증폭 최소값 (고정)
        public const float CritBaseBonus   = 0.75f; // 기본공격 치명타 계수 (1 + 0.75 = 1.75배)
    }
}
