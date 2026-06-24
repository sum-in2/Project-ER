namespace ProjectER.Combat
{
    /// <summary>
    /// 데미지 계산 시 방어자 측 스탯을 노출하는 인터페이스 (ISP: 방어 관련만 분리).
    /// 공격자는 대상의 IDamageable을 ICombatStats로 캐스팅해 방어력을 읽는다.
    /// 미구현 방어 스탯(피해감소 등)은 추후 확장 — 현재는 방어력만.
    /// </summary>
    public interface ICombatStats
    {
        /// <summary>방어력 (장비 보너스 합산 포함 예정).</summary>
        float Defense { get; }
    }
}
