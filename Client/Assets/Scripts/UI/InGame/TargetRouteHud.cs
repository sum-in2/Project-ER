using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 인게임 우상단 목표 아이템 HUD — 선택 루트(MatchSelectionData)의 부위별 목표 장비 5종을 표시한다.
    /// 5칸을 런타임 생성하고(가로 배열) 루트값으로 아이콘/등급색을 채운다.
    /// 각 슬롯 하단에 기초 재료(노말 등급) 진행도 "보유/필요"를 표시한다 (인벤토리 변경 시 갱신).
    /// 루트 미선택이면 빈 슬롯(회색)으로 표시.
    /// </summary>
    public sealed class TargetRouteHud : MonoBehaviour
    {
        // 슬롯 비율 108:64 (아이템 슬롯 표기 규칙)
        private const float SlotHeight = 40f;
        private const float SlotWidth  = SlotHeight * (108f / 64f);
        private const int   SlotCount  = 5; // EquipmentSlotType 개수

        [SerializeField] private MatchSelectionData   _matchSelection;
        [SerializeField] private ItemGradeColorConfig _gradeConfig;
        [SerializeField] private InventorySystem      _inventory;      // 재료 보유량 조회
        [SerializeField] private RecipeDatabase       _recipeDatabase; // 기초 재료 전개용

        private Image[] _backgrounds;
        private Image[] _icons;
        private Text[]  _countTexts;
        private Image[] _countBackgrounds;
        private bool    _built;

        // ⚠️ GC 주의: 슬롯별 기초 재료 집계에 재사용하는 버퍼 (인벤토리 변경 시에만 사용)
        private readonly Dictionary<ItemData, int> _materialBuffer = new();

        private void Awake() => BuildSlots();

        private void OnEnable()
        {
            if (_inventory != null)
                _inventory.OnBagSlotChanged += HandleInventoryChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null)
                _inventory.OnBagSlotChanged -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged(int index, InventorySlot slot) => Refresh();

        /// <summary>선택 루트를 다시 읽어 슬롯(아이콘 + 재료 진행도)을 갱신한다.</summary>
        public void Refresh()
        {
            if (!_built) BuildSlots();

            SavedRoute route = _matchSelection != null ? _matchSelection.SelectedRoute : null;
            for (int i = 0; i < SlotCount; i++)
            {
                ItemData item = route != null ? route.GetItem((EquipmentSlotType)i) : null;
                ApplySlot(i, item);
            }
        }

        private void ApplySlot(int index, ItemData item)
        {
            bool hasItem = item != null;

            _backgrounds[index].color = hasItem && _gradeConfig != null
                ? _gradeConfig.GetBackgroundColor(item.ItemGrade)
                : new Color(0.16f, 0.16f, 0.2f, 1f);

            _icons[index].enabled = hasItem && item.Icon != null;
            _icons[index].sprite  = hasItem ? item.Icon : null;

            ApplyCount(index, item);
        }

        // 목표 장비의 기초 재료 중 노말(Common) 등급에 대한 진행도 "보유/필요"를 표시한다.
        private void ApplyCount(int index, ItemData item)
        {
            Text text = _countTexts[index];
            if (item == null || _recipeDatabase == null)
            {
                text.enabled = false;
                _countBackgrounds[index].enabled = false;
                return;
            }

            _materialBuffer.Clear();
            RouteSorter.CollectBaseMaterials(item, _recipeDatabase, _materialBuffer);

            int needed = 0;
            int owned  = 0;
            foreach (KeyValuePair<ItemData, int> pair in _materialBuffer)
            {
                if (pair.Key.ItemGrade != ItemGrade.Common) continue;
                needed += pair.Value;
                int have = _inventory != null ? _inventory.GetItemCount(pair.Key.Id) : 0;
                owned += Mathf.Min(have, pair.Value); // 필요 수량까지만 진행도에 반영
            }

            if (needed <= 0)
            {
                text.enabled = false;
                _countBackgrounds[index].enabled = false;
                return;
            }

            text.enabled = true;
            _countBackgrounds[index].enabled = true;
            // ⚠️ GC 주의: string 생성 — 인벤토리 변경 시에만 호출되므로 허용
            text.text  = $"{owned}/{needed}";
            text.color = owned >= needed ? new Color(0.4f, 1f, 0.4f) : Color.white;
        }

        private void BuildSlots()
        {
            if (_built) return;
            _built = true;

            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleRight;

            _backgrounds      = new Image[SlotCount];
            _icons            = new Image[SlotCount];
            _countTexts       = new Text[SlotCount];
            _countBackgrounds = new Image[SlotCount];

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < SlotCount; i++)
                CreateSlot(i, builtinFont);
        }

        private void CreateSlot(int index, Font font)
        {
            GameObject slotGo = new($"TargetSlot_{index}");
            slotGo.transform.SetParent(transform, false);

            Image bg = slotGo.AddComponent<Image>();
            bg.raycastTarget = false;

            LayoutElement le = slotGo.AddComponent<LayoutElement>();
            le.preferredWidth  = SlotWidth;
            le.preferredHeight = SlotHeight;

            GameObject iconGo = new("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            Image icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget  = false;

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            // 재료 진행도 텍스트 (슬롯 하단, 가독성용 반투명 띠 위)
            GameObject countBgGo = new("CountBg");
            countBgGo.transform.SetParent(slotGo.transform, false);
            Image countBg = countBgGo.AddComponent<Image>();
            countBg.color = new Color(0f, 0f, 0f, 0.6f);
            countBg.raycastTarget = false;
            countBg.enabled = false;
            RectTransform countBgRt = countBgGo.GetComponent<RectTransform>();
            countBgRt.anchorMin = new Vector2(0f, 0f);
            countBgRt.anchorMax = new Vector2(1f, 0f);
            countBgRt.pivot     = new Vector2(0.5f, 0f);
            countBgRt.sizeDelta = new Vector2(0f, 14f);
            countBgRt.anchoredPosition = Vector2.zero;

            GameObject countGo = new("Count");
            countGo.transform.SetParent(countBgGo.transform, false);
            Text count = countGo.AddComponent<Text>();
            count.font          = font;
            count.fontSize      = 11;
            count.alignment     = TextAnchor.MiddleCenter;
            count.color         = Color.white;
            count.raycastTarget = false;
            count.enabled       = false;
            RectTransform countRt = countGo.GetComponent<RectTransform>();
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = Vector2.zero;
            countRt.offsetMax = Vector2.zero;

            _backgrounds[index]      = bg;
            _icons[index]            = icon;
            _countTexts[index]       = count;
            _countBackgrounds[index] = countBg;
        }

#if UNITY_EDITOR
        public void Editor_SetReferences(MatchSelectionData matchSelection, ItemGradeColorConfig gradeConfig,
            InventorySystem inventory, RecipeDatabase recipeDatabase)
        {
            _matchSelection = matchSelection;
            _gradeConfig    = gradeConfig;
            _inventory      = inventory;
            _recipeDatabase = recipeDatabase;
        }
#endif
    }
}
