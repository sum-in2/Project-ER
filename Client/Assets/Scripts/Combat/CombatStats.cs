namespace ProjectER.Combat
{
    /// <summary>
    /// 캐릭터의 최종 전투 스탯 (기본 스탯 + 장착 장비 보너스 합산 결과).
    /// 불변 값 객체 — 장비 변경 시 CombatStatsBuilder가 새로 생성한다.
    /// 비율 스탯(치명타/공속비율/쿨감 등)은 0~1 비율 단위(BSER 원본과 동일).
    /// </summary>
    public readonly struct CombatStats
    {
        public readonly float MaxHp;
        public readonly float AttackPower;
        public readonly float Defense;
        public readonly float AttackSpeed;          // 최종 초당 공격 횟수 (공속비율·상한 적용 후)
        public readonly float MoveSpeed;
        public readonly float CriticalStrikeChance; // 0~1 비율
        public readonly float SkillAmp;
        public readonly float CooldownReduction;
        public readonly float AdaptiveForce;
        public readonly float LifeSteal;
        public readonly float PenetrationDefense;
        public readonly float HpRegen;

        public CombatStats(
            float maxHp,
            float attackPower,
            float defense,
            float attackSpeed,
            float moveSpeed,
            float criticalStrikeChance,
            float skillAmp,
            float cooldownReduction,
            float adaptiveForce,
            float lifeSteal,
            float penetrationDefense,
            float hpRegen)
        {
            MaxHp = maxHp;
            AttackPower = attackPower;
            Defense = defense;
            AttackSpeed = attackSpeed;
            MoveSpeed = moveSpeed;
            CriticalStrikeChance = criticalStrikeChance;
            SkillAmp = skillAmp;
            CooldownReduction = cooldownReduction;
            AdaptiveForce = adaptiveForce;
            LifeSteal = lifeSteal;
            PenetrationDefense = penetrationDefense;
            HpRegen = hpRegen;
        }

        /// <summary>공격 간격(초) = 1 / 공격속도. 공속 0 이하면 1초로 폴백.</summary>
        public float AttackInterval => AttackSpeed > 0f ? 1f / AttackSpeed : 1f;
    }
}
