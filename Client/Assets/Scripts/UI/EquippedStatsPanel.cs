using System;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 장착 스탯 합산 표시 패널 — 빌드와 갱신을 담당한다.
    /// </summary>
    public class EquippedStatsPanel
    {
        private static readonly (string Label, Func<ItemData, float> Getter, bool IsPercent)[] StatDefs =
        {
            ("공격력",   item => item.AttackPower,          false),
            ("최대체력", item => item.MaxHpBonus,           false),
            ("방어력",   item => item.Defense,              false),
            ("이동속도", item => item.MoveSpeedBonus,       false),
            ("공격속도", item => item.AttackSpeedBonus,     true),
            ("스킬증폭", item => item.SkillAmp,             false),
            ("쿨감",     item => item.CooldownReduction,    true),
            ("적응형",   item => item.AdaptiveForce,        false),
            ("치명타",   item => item.CriticalStrikeChance, true),
            ("생명흡수", item => item.LifeSteal,            true),
            ("체력재생", item => item.HpRegenRatio,         true),
            ("방어관통", item => item.PenetrationDefense,   false),
        };

        private readonly Transform _container;
        private Text[]             _valueTexts;

        public EquippedStatsPanel(Transform container)
        {
            _container = container;
        }

        public void Build()
        {
            if (_container == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GridLayoutGroup grid = _container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(88f, 22f);
            grid.spacing         = new Vector2(4f,  2f);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            _valueTexts = new Text[StatDefs.Length];
            for (int i = 0; i < StatDefs.Length; i++)
            {
                CreateLabel(_container, StatDefs[i].Label, builtinFont);
                _valueTexts[i] = CreateValue(_container, builtinFont);
            }
        }

        public void Refresh(InventorySystem inventory)
        {
            if (_valueTexts == null || inventory == null) return;

            float[] totals = new float[StatDefs.Length];
            // ⚠️ GC 주의: Enum.GetValues는 Array를 할당함 — 장비 변경 시에만 호출되므로 허용
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                ItemData item = inventory.GetEquippedItem(slotType);
                if (item == null) continue;
                for (int i = 0; i < StatDefs.Length; i++)
                    totals[i] += StatDefs[i].Getter(item);
            }

            for (int i = 0; i < StatDefs.Length; i++)
            {
                float val = totals[i];
                // ⚠️ GC 주의: string 생성 — 장비 변경 시에만 호출되므로 허용
                _valueTexts[i].text = StatDefs[i].IsPercent
                    ? $"{val * 100f:0.#}%"
                    : $"{val:0.##}";
            }
        }

        private static void CreateLabel(Transform parent, string label, Font font)
        {
            GameObject go  = new($"Label_{label}");
            go.transform.SetParent(parent, false);
            Text text      = go.AddComponent<Text>();
            text.text      = label;
            text.font      = font;
            text.fontSize  = 13;
            text.color     = new Color(0.75f, 0.75f, 0.75f);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private static Text CreateValue(Transform parent, Font font)
        {
            GameObject go  = new("Value");
            go.transform.SetParent(parent, false);
            Text text      = go.AddComponent<Text>();
            text.text      = "0";
            text.font      = font;
            text.fontSize  = 13;
            text.color     = Color.white;
            text.alignment = TextAnchor.MiddleRight;
            return text;
        }
    }
}
