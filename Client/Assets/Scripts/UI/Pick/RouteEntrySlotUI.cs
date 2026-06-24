using System;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 루트 선택 목록의 한 줄 — 루트 이름 + 부위별 목표 장비 5아이콘을 표시한다.
    /// 런타임 생성 UI(ItemBrowserView와 동일 방식, 레거시 Text + 빌트인 폰트).
    /// </summary>
    public sealed class RouteEntrySlotUI : MonoBehaviour
    {
        // 슬롯 비율 108:64 (아이템 슬롯 표기 규칙)
        private const float IconHeight = 40f;
        private const float IconWidth  = IconHeight * (108f / 64f);

        private static readonly Color NormalColor   = new(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color SelectedColor  = new(0.85f, 0.85f, 0.50f, 1f);

        private Image _background;

        public SavedRoute Route { get; private set; }

        // 클릭 시 자신을 알린다 (선택 처리는 패널이 담당)
        public event Action<RouteEntrySlotUI> OnClicked;

        public void SetSelected(bool selected)
        {
            if (_background != null)
                _background.color = selected ? SelectedColor : NormalColor;
        }

        /// <summary>루트 1개에 대한 행 UI를 런타임 생성한다.</summary>
        public static RouteEntrySlotUI Create(Transform parent, SavedRoute route, Font font, ItemGradeColorConfig gradeConfig)
        {
            GameObject go = new($"RouteEntry_{route.DisplayName}");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = NormalColor;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 6;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            LayoutElement rowLe = go.AddComponent<LayoutElement>();
            rowLe.preferredHeight = IconHeight + 12f;

            // 루트 이름
            CreateNameLabel(go.transform, route.DisplayName, font);

            // 부위별 아이콘 5칸 (빈 부위는 회색)
            foreach (EquipmentSlotType slotType in (EquipmentSlotType[])Enum.GetValues(typeof(EquipmentSlotType)))
                CreateIcon(go.transform, route.GetItem(slotType), gradeConfig);

            RouteEntrySlotUI slot = go.AddComponent<RouteEntrySlotUI>();
            slot.Route = route;
            slot._background = bg;
            button.onClick.AddListener(() => slot.OnClicked?.Invoke(slot));

            return slot;
        }

        private static void CreateNameLabel(Transform parent, string name, Font font)
        {
            GameObject go = new("Name");
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.text      = name;
            text.font      = font;
            text.fontSize  = 16;
            text.color     = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 90f;
        }

        private static void CreateIcon(Transform parent, ItemData item, ItemGradeColorConfig gradeConfig)
        {
            GameObject go = new("Icon");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = item != null && gradeConfig != null
                ? gradeConfig.GetBackgroundColor(item.ItemGrade)
                : new Color(0.18f, 0.18f, 0.2f, 1f);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = IconWidth;
            le.preferredHeight = IconHeight;

            GameObject iconGo = new("Sprite");
            iconGo.transform.SetParent(go.transform, false);
            Image icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget  = false;
            icon.sprite         = item != null ? item.Icon : null;
            if (item == null || item.Icon == null)
                icon.color = new Color(0.5f, 0.5f, 0.5f, item == null ? 0.3f : 1f);

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
        }
    }
}
