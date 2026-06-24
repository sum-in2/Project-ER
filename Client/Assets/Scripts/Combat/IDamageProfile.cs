namespace ProjectER.Combat
{
    /// <summary>
    /// "기본 피해"(방어력·치명타 적용까지)를 산출하는 다형 프로파일.
    /// 평타(BasicAttackProfile)와 스킬(SkillDamageProfile)은 구조가 동일하고
    /// 기본 피해의 출처만 다르다 → if/else 타입 분기 대신 이 인터페이스로 분리한다.
    /// 구현체는 readonly struct로, IDamageCalculator의 제네릭 호출에서 박싱 없이 사용한다.
    /// </summary>
    public interface IDamageProfile
    {
        /// <summary>
        /// 방어력·방어관통·(평타는 치명타)까지 반영한 기본 피해를 반환한다.
        /// 이후의 고정 추가피해/증폭/최종증폭/모드증폭은 IDamageCalculator가 처리한다.
        /// </summary>
        float ComputeBaseDamage();
    }
}
