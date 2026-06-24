using System;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.Combat
{
    /// <summary>
    /// 기본 스탯(CharacterData) + 장착 장비(InventorySystem) 보너스를 합산해
    /// 최종 CombatStats를 만드는 순수 함수. 상태 없음 → 테스트 용이.
    /// 합산 규칙:
    /// - 고정 스탯(공격력/방어력/최대체력/이동속도/치명타/스킬증폭 등): 기본 + 장비 합
    /// - 공격속도: 기본 × (1 + 장비 공속비율 합), 공속 상한(AttackSpeedLimit)으로 클램프
    /// </summary>
    public static class CombatStatsBuilder
    {
        /// <summary>
        /// baseData 기준 스탯에 inventory의 장착 장비 보너스를 더한 최종 스탯을 반환한다.
        /// baseData가 null이면 모든 기본값 0, inventory가 null이면 기본 스탯만 사용한다.
        /// </summary>
        public static CombatStats Build(CharacterData baseData, InventorySystem inventory)
        {
            float maxHp        = baseData != null ? baseData.MaxHp : 0f;
            float attackPower  = baseData != null ? baseData.AttackPower : 0f;
            float defense      = baseData != null ? baseData.Defense : 0f;
            float baseAtkSpeed = baseData != null ? baseData.AttackSpeed : 0f;
            float atkSpeedLimit = baseData != null ? baseData.AttackSpeedLimit : 0f;
            float moveSpeed    = baseData != null ? baseData.MoveSpeed : 0f;
            float critChance   = baseData != null ? baseData.CriticalStrikeChance : 0f;
            float skillAmp     = baseData != null ? baseData.SkillAmp : 0f;
            float adaptiveForce = baseData != null ? baseData.AdaptiveForce : 0f;
            float hpRegen      = baseData != null ? baseData.HpRegen : 0f;

            // CharacterData에 기본값이 없는 스탯은 0에서 시작 (장비로만 획득)
            float cooldownReduction = 0f;
            float lifeSteal = 0f;
            float penetrationDefense = 0f;

            // 공속은 비율 합산이 필요하므로 별도 누적
            float attackSpeedRatioSum = 0f;

            if (inventory != null)
            {
                // ⚠️ GC 주의: Enum.GetValues는 배열 할당 — 장비 변경 시에만 호출되므로 허용
                foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    ItemData item = inventory.GetEquippedItem(slotType);
                    if (item == null) continue;

                    maxHp               += item.MaxHpBonus;
                    attackPower         += item.AttackPower;
                    defense             += item.Defense;
                    moveSpeed           += item.MoveSpeedBonus;
                    critChance          += item.CriticalStrikeChance;
                    skillAmp            += item.SkillAmp;
                    adaptiveForce       += item.AdaptiveForce;
                    cooldownReduction   += item.CooldownReduction;
                    lifeSteal           += item.LifeSteal;
                    penetrationDefense  += item.PenetrationDefense;
                    hpRegen             += item.HpRegenRatio;
                    attackSpeedRatioSum += item.AttackSpeedBonus; // attackSpeedRatio (비율)
                }
            }

            float attackSpeed = baseAtkSpeed * (1f + attackSpeedRatioSum);
            if (atkSpeedLimit > 0f)
                attackSpeed = Mathf.Min(attackSpeed, atkSpeedLimit);

            return new CombatStats(
                maxHp,
                attackPower,
                defense,
                attackSpeed,
                moveSpeed,
                critChance,
                skillAmp,
                cooldownReduction,
                adaptiveForce,
                lifeSteal,
                penetrationDefense,
                hpRegen);
        }
    }
}
