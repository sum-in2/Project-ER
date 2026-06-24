namespace ProjectER.Combat
{
    /// <summary>
    /// 기본 공격(평타) 기본 피해 프로파일.
    /// 기본 피해 = 공격력 × (100 / (적용방어력 + 100)) × {1 + 치명타여부 × (0.75 + 치명타피해증가%)}
    /// 치명타 굴림(Random)은 계산기 밖(AttackState 등)에서 수행하고 isCrit만 주입 → 순수 함수 유지.
    /// </summary>
    public readonly struct BasicAttackProfile : IDamageProfile
    {
        private readonly float _attackPower;
        private readonly float _rawDefense;
        private readonly float _penetrationPercent;
        private readonly float _penetrationFlat;
        private readonly bool _isCrit;
        private readonly float _critDamageBonus; // 치명타 피해 증가% (없으면 0)

        public BasicAttackProfile(
            float attackPower,
            float rawDefense,
            float penetrationPercent,
            float penetrationFlat,
            bool isCrit,
            float critDamageBonus)
        {
            _attackPower = attackPower;
            _rawDefense = rawDefense;
            _penetrationPercent = penetrationPercent;
            _penetrationFlat = penetrationFlat;
            _isCrit = isCrit;
            _critDamageBonus = critDamageBonus;
        }

        public float ComputeBaseDamage()
        {
            float effectiveDefense = CombatMath.EffectiveDefense(_rawDefense, _penetrationPercent, _penetrationFlat);
            float defenseMultiplier = CombatMath.DefenseMultiplier(effectiveDefense);
            float critMultiplier = _isCrit ? 1f + CombatConstants.CritBaseBonus + _critDamageBonus : 1f;
            return _attackPower * defenseMultiplier * critMultiplier;
        }
    }
}
