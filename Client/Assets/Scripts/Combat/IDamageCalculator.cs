namespace ProjectER.Combat
{
    /// <summary>
    /// 데미지 계산기. 기본 피해(IDamageProfile)와 공통 보정(DamageModifiers)을 받아
    /// 최종 피해를 산출하는 순수 함수.
    /// 서버 검증과 동일 공식을 사용한다 (데미지는 서버가 직접 재계산 — system-design.md 참조).
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// 최종 피해 = (기본 피해 × 증폭 + 고정 추가피해) × 최종증폭 × 모드증폭, 최소 1.
        /// 제네릭 + struct 프로파일로 박싱/GC 없이 호출한다.
        /// </summary>
        float Calculate<TProfile>(in TProfile profile, in DamageModifiers modifiers)
            where TProfile : IDamageProfile;
    }
}
