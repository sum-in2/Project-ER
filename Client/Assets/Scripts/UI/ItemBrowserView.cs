using System;
using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템 브라우저 버튼 목록을 관리한다 — 생성, 선택 강조, 루트 정렬, 표시/숨김.
    /// </summary>
    public class ItemBrowserView
    {
        public event Action<ItemData> OnItemSelected;

        private static readonly Color SelectedHighlight = new Color(0.85f, 0.85f, 0.50f, 1f);

        private readonly IReadOnlyList<ItemData> _items;
        private readonly Transform               _container;
        private readonly ItemGradeColorConfig    _gradeConfig;

        private readonly List<(ItemData Item, Transform Button, TriangleIndicator Indicator)> _buttons = new();
        private ItemData _selectedItem;
        private Image    _selectedButtonBg;

        public ItemBrowserView(IReadOnlyList<ItemData> items, Transform container, ItemGradeColorConfig gradeConfig)
        {
            _items       = items;
            _container   = container;
            _gradeConfig = gradeConfig;
        }

        public void Build()
        {
            if (_container == null)
            {
                Debug.LogError("[ItemBrowserView] container가 null입니다.");
                return;
            }

            int created = 0;
            foreach (ItemData item in _items)
            {
                if (item == null) continue;
                CreateButton(item, _container);
                created++;
            }
            Debug.Log($"[ItemBrowserView] 버튼 {created}개 생성 완료");
        }

        /// <summary>
        /// neededIds에 포함된 버튼을 앞으로, 나머지를 뒤로 이동하고 삼각형 인디케이터를 갱신한다.
        /// </summary>
        public void SortByRoute(HashSet<string> neededIds)
        {
            if (_buttons.Count == 0) return;

            int index = 0;
            foreach ((ItemData item, Transform btn, TriangleIndicator indicator) in _buttons)
            {
                bool needed = neededIds.Contains(item.Id);
                indicator.enabled = needed;
                if (needed) btn.SetSiblingIndex(index++);
            }
            foreach ((ItemData item, Transform btn, TriangleIndicator _) in _buttons)
                if (!neededIds.Contains(item.Id))
                    btn.SetSiblingIndex(index++);
        }

        /// <summary>predicate를 통과하는 버튼만 활성화한다.</summary>
        public void ApplyVisibility(Func<ItemData, bool> predicate)
        {
            foreach ((ItemData item, Transform btn, TriangleIndicator _) in _buttons)
                btn.gameObject.SetActive(predicate(item));
        }

        private void CreateButton(ItemData item, Transform parent)
        {
            GameObject go = new($"Btn_{item.Id}");
            go.transform.SetParent(parent, false);

            Image bg    = go.AddComponent<Image>();
            bg.color    = _gradeConfig != null ? _gradeConfig.GetBackgroundColor(item.ItemGrade) : Color.gray;

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.pressedColor     = new Color(0.20f, 0.20f, 0.20f);
            btn.colors          = cb;

            // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
            ItemData captured   = item;
            Image    bgCaptured = bg;
            btn.onClick.AddListener(() => HandleSelect(captured, bgCaptured));

            // 삼각형 인디케이터 — 목표 루트 필요 재료일 때만 활성화
            GameObject    indGo = new("RouteIndicator");
            indGo.transform.SetParent(go.transform, false);
            TriangleIndicator tri  = indGo.AddComponent<TriangleIndicator>();
            tri.color              = Color.yellow;
            tri.raycastTarget      = false;
            tri.enabled            = false;
            RectTransform indRt    = indGo.GetComponent<RectTransform>();
            indRt.anchorMin        = new Vector2(0f, 1f);
            indRt.anchorMax        = new Vector2(0f, 1f);
            indRt.pivot            = new Vector2(0f, 1f);
            indRt.sizeDelta        = new Vector2(16f, 16f);
            indRt.anchoredPosition = Vector2.zero;

            GameObject iconGo   = new("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon          = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget  = false;
            icon.sprite         = item.Icon;
            if (item.Icon == null)
                icon.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.preferredWidth  = 67.5f;

            ItemBrowserSlotUI browserSlot = go.AddComponent<ItemBrowserSlotUI>();
            browserSlot.SetItem(item);
            go.AddComponent<ItemDragHandler>();

            _buttons.Add((item, go.transform, tri));
        }

        private void HandleSelect(ItemData item, Image buttonBg)
        {
            if (_selectedButtonBg != null && _selectedItem != null)
                _selectedButtonBg.color = _gradeConfig != null
                    ? _gradeConfig.GetBackgroundColor(_selectedItem.ItemGrade)
                    : Color.gray;

            _selectedItem     = item;
            _selectedButtonBg = buttonBg;
            buttonBg.color    = SelectedHighlight;

            Debug.Log($"[ItemBrowserView] 선택: {item.DisplayName} ({item.ItemGrade})");
            OnItemSelected?.Invoke(item);
        }
    }
}
