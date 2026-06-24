namespace ProjectER.Combat
{
    /// <summary>
    /// 기본 피해 이후에 적용되는 공통 보정값 (평타·스킬 공통).
    /// 최종 피해 = (기본 피해 × 증폭 + 고정 추가피해) × 최종증폭 × 모드증폭
    /// 각 값은 이미 "공격자 - 방어자" 상쇄가 끝난 결과를 담는다(증폭류는 1 기준, 고정은 0 기준).
    /// 현재 ItemData/CharacterData에 없는 스탯은 Neutral(중립값)으로 시작한다.
    /// </summary>
    public readonly struct DamageModifiers
    {
        public readonly float FixedBonus; // 고정 추가피해 (= 추가피해 - 적 피해감소(고정)), 방어력 무시
        public readonly float Amp;        // 피해 증폭 (= 1 + 증폭% - 적 피해감소%)
        public readonly float FinalAmp;   // 최종 피해 증폭 (= 1 + 최종피해증가율 - 적 최종피해감소율)
        public readonly float ModeAmp;    // 모드 증폭 (= 모드 주는피해 - 적 모드 받는피해)

        public DamageModifiers(float fixedBonus, float amp, float finalAmp, float modeAmp)
        {
            FixedBonus = fixedBonus;
            Amp = amp;
            FinalAmp = finalAmp;
            ModeAmp = modeAmp;
        }

        /// <summary>보정 없음(중립). 증폭류 1, 고정 0.</summary>
        public static DamageModifiers Neutral => new DamageModifiers(0f, 1f, 1f, 1f);
    }
}
