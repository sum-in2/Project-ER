using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 인게임 우상단 목표 아이템 HUD — 선택 루트(MatchSelectionData)의 부위별 목표 장비 5종을 표시한다.
    /// 읽기 전용 표시. 5칸을 런타임 생성하고(가로 배열) 루트값으로 아이콘/등급색을 채운다.
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

        private Image[] _backgrounds;
        private Image[] _icons;
        private bool    _built;

        private void Awake()  => BuildSlots();
        private void OnEnable() => Refresh();

        /// <summary>선택 루트를 다시 읽어 슬롯을 갱신한다.</summary>
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

            _backgrounds = new Image[SlotCount];
            _icons       = new Image[SlotCount];

            for (int i = 0; i < SlotCount; i++)
                CreateSlot(i);
        }

        private void CreateSlot(int index)
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

            _backgrounds[index] = bg;
            _icons[index]       = icon;
        }

#if UNITY_EDITOR
        public void Editor_SetReferences(MatchSelectionData matchSelection, ItemGradeColorConfig gradeConfig)
        {
            _matchSelection = matchSelection;
            _gradeConfig = gradeConfig;
        }
#endif
    }
}
