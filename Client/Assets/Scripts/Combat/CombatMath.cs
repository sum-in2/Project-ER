using UnityEngine;

namespace ProjectER.Combat
{
    /// <summary>
    /// 평타·스킬이 공통으로 쓰는 방어력 → 피해 감소 계산 (DRY).
    /// 순수 함수만 모은 정적 클래스 — 상태/GC 없음.
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// 방어관통을 적용한 "적용 방어력"을 구한다.
        /// 적용방어력 = 방어력 × (1 - 방어관통%) - 방어관통(고정)
        /// </summary>
        public static float EffectiveDefense(float rawDefense, float penetrationPercent, float penetrationFlat)
        {
            return rawDefense * (1f - penetrationPercent) - penetrationFlat;
        }

        /// <summary>
        /// 적용 방어력 기준 피해 감소 후 배율 = 100 / (적용방어력 + 100).
        /// 분모는 1 미만으로 떨어지지 않게 보호 (음수 방어력 폭주 방지).
        /// </summary>
        public static float DefenseMultiplier(float effectiveDefense)
        {
            float denominator = Mathf.Max(1f, effectiveDefense + CombatConstants.DefenseConstant);
            return CombatConstants.DefenseConstant / denominator;
        }
    }
}
