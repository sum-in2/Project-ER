namespace ProjectER.Combat
{
    /// <summary>
    /// 스킬 기본 피해 프로파일.
    /// 기본 피해 = 스킬계수데미지 × (100 / (적용방어력 + 100)) (치명타 보통 없음)
    /// </summary>
    public readonly struct SkillDamageProfile : IDamageProfile
    {
        private readonly float _skillCoefDamage;
        private readonly float _rawDefense;
        private readonly float _penetrationPercent;
        private readonly float _penetrationFlat;

        public SkillDamageProfile(
            float skillCoefDamage,
            float rawDefense,
            float penetrationPercent,
            float penetrationFlat)
        {
            _skillCoefDamage = skillCoefDamage;
            _rawDefense = rawDefense;
            _penetrationPercent = penetrationPercent;
            _penetrationFlat = penetrationFlat;
        }

        public float ComputeBaseDamage()
        {
            float effectiveDefense = CombatMath.EffectiveDefense(_rawDefense, _penetrationPercent, _penetrationFlat);
            float defenseMultiplier = CombatMath.DefenseMultiplier(effectiveDefense);
            return _skillCoefDamage * defenseMultiplier;
        }
    }
}
