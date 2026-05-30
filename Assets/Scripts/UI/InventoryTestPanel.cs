using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 테스트 패널 — 아이템 브라우저(선택/드래그) + 획득/루트추가 버튼 + 장착 스탯 합계
    /// </summary>
    public class InventoryTestPanel : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private List<ItemData>  _acquisitionItems;
        [SerializeField] private Transform       _buttonContainer;
        [SerializeField] private Transform       _statContainer;

        // 우측 패널 — 빌더에서 연결
        [SerializeField] private Button            _acquireButton;
        [SerializeField] private Button            _addRouteButton;
        [SerializeField] private TargetItemPanelUI _targetItemPanel;

        private Text[]   _statValueTexts;
        private ItemData _selectedItem;
        private Image    _selectedButtonBg; // 현재 선택된 브라우저 버튼의 배경 Image

        // 선택 강조색 — 등급색보다 밝게
        private static readonly Color SelectedHighlight = new Color(0.85f, 0.85f, 0.50f, 1f);

        private static readonly Color[] GradeColors =
        {
            new Color(0.30f, 0.30f, 0.30f), // Common   — 일반
            new Color(0.18f, 0.38f, 0.18f), // Uncommon — 고급
            new Color(0.15f, 0.28f, 0.50f), // Rare     — 희귀
            new Color(0.38f, 0.18f, 0.50f), // Epic     — 영웅
            new Color(0.55f, 0.38f, 0.08f), // Legend   — 전설
            new Color(0.55f, 0.15f, 0.15f), // Mythic   — 초월
        };

        // 스탯 정의: (라벨, 값 추출 함수, 퍼센트 여부)
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
            if (_inventorySystem == null)
            {
                Debug.LogError("[InventoryTestPanel] InventorySystem이 연결되지 않았습니다.");
                return;
            }

            // 우측 패널 버튼 바인딩
            if (_acquireButton != null)
                _acquireButton.onClick.AddListener(AcquireSelected);
            if (_addRouteButton != null)
                _addRouteButton.onClick.AddListener(AddToRoute);

            if (_acquisitionItems != null && _acquisitionItems.Count > 0)
                BuildButtons();
            else
                Debug.LogWarning("[InventoryTestPanel] _acquisitionItems가 비어있습니다.");

            BuildStatPanel();
            RefreshStats();
        }

        // ── 브라우저 버튼 ────────────────────────────────────────────

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
            Debug.Log($"[InventoryTestPanel] 버튼 {created}개 생성 완료");
        }

        private void CreateButton(ItemData item, Transform parent)
        {
            GameObject go = new($"Btn_{item.Id}");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = GradeColors[(int)item.ItemGrade];

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.pressedColor     = new Color(0.20f, 0.20f, 0.20f);
            cb.normalColor      = Color.white; // tint 없이 bg.color 그대로 사용
            btn.colors          = cb;

            // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
            ItemData captured   = item;
            Image    bgCaptured = bg;
            btn.onClick.AddListener(() => SelectItem(captured, bgCaptured));

            GameObject iconGo   = new("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon          = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget  = false;
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
            le.preferredHeight = 48f;
            le.preferredWidth  = 48f;
        }

        // ── 선택 ─────────────────────────────────────────────────────

        private void SelectItem(ItemData item, Image buttonBg)
        {
            // 이전 선택 버튼 색 복원
            if (_selectedButtonBg != null && _selectedItem != null)
                _selectedButtonBg.color = GradeColors[(int)_selectedItem.ItemGrade];

            _selectedItem     = item;
            _selectedButtonBg = buttonBg;
            buttonBg.color    = SelectedHighlight;

            Debug.Log($"[InventoryTestPanel] 선택: {item.DisplayName} ({item.ItemGrade})");
        }

        // ── 외부 버튼 액션 ───────────────────────────────────────────

        /// <summary>
        /// 획득 버튼 — 선택된 아이템을 인벤토리에 추가
        /// </summary>
        public void AcquireSelected()
        {
            if (_selectedItem == null)
            {
                Debug.LogWarning("[InventoryTestPanel] 획득할 아이템을 선택하세요.");
                return;
            }

            if (_inventorySystem.TryAddItem(_selectedItem))
                Debug.Log($"[InventoryTestPanel] 획득: {_selectedItem.DisplayName}");
            else
                Debug.LogWarning($"[InventoryTestPanel] 가방이 꽉 참 — {_selectedItem.DisplayName}");
        }

        /// <summary>
        /// 루트 추가 버튼 — 선택된 장비 아이템을 목표 슬롯에 설정
        /// </summary>
        public void AddToRoute()
        {
            if (_selectedItem == null)
            {
                Debug.LogWarning("[InventoryTestPanel] 루트에 추가할 아이템을 선택하세요.");
                return;
            }

            if (_targetItemPanel == null)
            {
                Debug.LogWarning("[InventoryTestPanel] TargetItemPanelUI가 연결되지 않았습니다.");
                return;
            }

            if (_targetItemPanel.TrySetItem(_selectedItem))
                Debug.Log($"[InventoryTestPanel] 루트 추가: {_selectedItem.DisplayName}");
            else
                Debug.LogWarning($"[InventoryTestPanel] {_selectedItem.DisplayName}은 장비 아이템이 아닙니다.");
        }

        // ── 스탯 패널 ────────────────────────────────────────────────

        private void BuildStatPanel()
        {
            if (_statContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GridLayoutGroup grid = _statContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(88f, 22f);
            grid.spacing         = new Vector2(4f,  2f);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

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
