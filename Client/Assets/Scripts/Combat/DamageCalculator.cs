using UnityEngine;

namespace ProjectER.Combat
{
    /// <summary>
    /// IDamageCalculator 기본 구현. 상태 없음 → 인스턴스 재사용 가능, GC 없음.
    /// 연산 순서(docs/combat-damage-formula.md "구현 시 연산 순서"):
    ///   방어관통·치명타 반영된 기본피해(IDamageProfile)
    ///   → 증폭(%) 곱 + 고정 추가피해 더하기
    ///   → 최종피해 증폭 → 모드 보정 → max(1, 결과)
    /// 핵심: 고정 추가피해는 방어력/증폭 계산 후에 더해진다(사실상 방어 무시).
    /// </summary>
    public sealed class DamageCalculator : IDamageCalculator
    {
        public float Calculate<TProfile>(in TProfile profile, in DamageModifiers modifiers)
            where TProfile : IDamageProfile
        {
            float baseDamage = profile.ComputeBaseDamage();

            float amp = Mathf.Max(CombatConstants.MinAmp, modifiers.Amp);
            float amplified = baseDamage * amp + modifiers.FixedBonus;

            float finalAmp = Mathf.Max(CombatConstants.MinAmp, modifiers.FinalAmp);
            float result = amplified * finalAmp * modifiers.ModeAmp;

            return Mathf.Max(CombatConstants.MinDamage, result);
        }
    }
}
