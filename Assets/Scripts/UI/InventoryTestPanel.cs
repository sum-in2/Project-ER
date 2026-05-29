using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 테스트 패널 — 아이템 획득 버튼 + 장착 스탯 합계 표시
    /// </summary>
    public class InventoryTestPanel : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private List<ItemData>  _acquisitionItems;
        [SerializeField] private Transform       _buttonContainer;
        [SerializeField] private Transform       _statContainer;

        private Text[] _statValueTexts;

        // 스탯 정의: (라벨, 값 추출 함수, 퍼센트 여부)
        // IsPercent=true → 원본값 * 100 후 % 표기 (0.05 → 5%)
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

        private void OnEnable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void OnDisable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void Start()
        {
            Debug.Log($"[InventoryTestPanel] Start — " +
                      $"InventorySystem={_inventorySystem != null}, " +
                      $"ButtonContainer={_buttonContainer != null}, " +
                      $"StatContainer={_statContainer != null}, " +
                      $"아이템 수={_acquisitionItems?.Count ?? 0}");

            if (_inventorySystem == null)
            {
                Debug.LogError("[InventoryTestPanel] InventorySystem이 연결되지 않았습니다.");
                return;
            }

            if (_acquisitionItems != null && _acquisitionItems.Count > 0)
                BuildButtons();
            else
                Debug.LogWarning("[InventoryTestPanel] _acquisitionItems가 비어있습니다.");

            BuildStatPanel();
            RefreshStats();
        }

        // ── 획득 버튼 ────────────────────────────────────────────────

        private void BuildButtons()
        {
            Transform parent = _buttonContainer != null ? _buttonContainer : transform;
            int created = 0;
            foreach (ItemData item in _acquisitionItems)
            {
                if (item == null) continue;
                CreateButton(item, parent);
                created++;
            }
            Debug.Log($"[InventoryTestPanel] 버튼 {created}개 생성 완료 — 부모: {parent.name}");
        }

        private static readonly Color[] GradeColors =
        {
            new Color(0.30f, 0.30f, 0.30f), // Common   — 일반 (회색)
            new Color(0.18f, 0.38f, 0.18f), // Uncommon — 고급 (녹색)
            new Color(0.15f, 0.28f, 0.50f), // Rare     — 희귀 (청색)
            new Color(0.38f, 0.18f, 0.50f), // Epic     — 영웅 (보라)
            new Color(0.55f, 0.38f, 0.08f), // Legend   — 전설 (금색)
            new Color(0.55f, 0.15f, 0.15f), // Mythic   — 초월 (적색)
        };

        private void CreateButton(ItemData item, Transform parent)
        {
            GameObject go = new($"Btn_{item.Id}");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = GradeColors[(int)item.ItemGrade];

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
            btn.colors          = cb;

            // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
            ItemData captured = item;
            btn.onClick.AddListener(() => OnAddItem(captured));

            GameObject iconGo   = new("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon          = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            if (item.Icon != null)
                icon.sprite = item.Icon;
            else
                icon.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.preferredWidth  = 44f;
        }

        private void OnAddItem(ItemData item)
        {
            Debug.Log($"[InventoryTestPanel] 버튼 클릭 — {item.DisplayName} 획득 시도");
            if (_inventorySystem.TryAddItem(item))
                Debug.Log($"[InventoryTestPanel] {item.DisplayName} 획득 성공");
            else
                Debug.LogWarning($"[InventoryTestPanel] 가방이 꽉 참 — {item.DisplayName}");
        }

        // ── 스탯 패널 ────────────────────────────────────────────────

        private void BuildStatPanel()
        {
            if (_statContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 4열 그리드: 라벨 | 수치 | 라벨 | 수치
            GridLayoutGroup grid        = _statContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize               = new Vector2(88f, 22f);
            grid.spacing                = new Vector2(4f, 2f);
            grid.constraint             = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount        = 4;

            _statValueTexts = new Text[StatDefs.Length];

            for (int i = 0; i < StatDefs.Length; i++)
            {
                CreateStatLabel(_statContainer, StatDefs[i].Label, builtinFont);
                _statValueTexts[i] = CreateStatValue(_statContainer, builtinFont);
            }
        }

        private void CreateStatLabel(Transform parent, string label, Font font)
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

        private Text CreateStatValue(Transform parent, Font font)
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

        private void RefreshStats()
        {
            if (_statValueTexts == null || _inventorySystem == null) return;

            float[] totals = new float[StatDefs.Length];

            // ⚠️ GC 주의: Enum.GetValues는 Array를 할당함 — 장비 변경 시에만 호출되므로 허용
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                ItemData item = _inventorySystem.GetEquippedItem(slotType);
                if (item == null) continue;

                for (int i = 0; i < StatDefs.Length; i++)
                    totals[i] += StatDefs[i].Getter(item);
            }

            for (int i = 0; i < StatDefs.Length; i++)
            {
                float val = totals[i];
                // ⚠️ GC 주의: string 생성 — 장비 변경 시에만 호출되므로 허용
                _statValueTexts[i].text = StatDefs[i].IsPercent
                    ? $"{val * 100f:0.#}%"
                    : $"{val:0.##}";
            }
        }

        private void HandleEquipmentChanged(EquipmentSlotType slotType, ItemData item)
        {
            RefreshStats();
        }
    }
}
