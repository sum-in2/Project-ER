using System;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 루트박스 4x4 그리드의 슬롯 1개.
    /// 드래그는 비활성, 클릭으로만 아이템을 취득한다.
    /// </summary>
    public class LootBoxSlotUI : BaseItemSlotUI
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text  _countText;
        [SerializeField] private Image _gradeBorderImage;

        private int         _slotIndex;
        private Action<int> _onClicked;
        private Button      _button;

        // 루트 필요 재료 표시용 삼각형 (도감과 동일 패턴) — 런타임 생성, 기본 숨김
        private TriangleIndicator _routeIndicator;

        private void Awake()
        {
            TryGetComponent(out _button);
            _button?.onClick.AddListener(HandleClick);
            CreateRouteIndicator();
        }

        private void OnEnable()  { }
        private void OnDisable() { }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(HandleClick);
        }

        public void Initialize(int index, Action<int> onClicked)
        {
            _slotIndex = index;
            _onClicked = onClicked;
        }

        public void Refresh(ItemData item, int count)
        {
            _currentItem = item;

            bool hasItem = item != null && count > 0;
            _iconImage.enabled = hasItem;
            _countText.enabled = hasItem;
            ApplyGradeColor(_gradeBorderImage, hasItem ? item : null);

            if (!hasItem) return;

            _iconImage.sprite = item.Icon;
            // ⚠️ GC 주의: 수량 표시마다 string 생성 — Update 루프 외부이므로 허용
            _countText.text = count > 1 ? count.ToString() : string.Empty;
        }

        /// <summary>
        /// 서버 응답 대기 중 상태 — 슬롯을 흐리게 표시해 클릭 의도를 즉시 반영한다.
        /// 인벤토리엔 아직 추가하지 않으므로 롤백 시 이 상태만 되돌리면 된다.
        /// </summary>
        public void SetPending()
        {
            _iconImage.color = new Color(1f, 1f, 1f, 0.3f);
            _countText.color = new Color(1f, 1f, 1f, 0.3f);
        }

        /// <summary>서버가 거부했을 때 Pending 상태를 되돌린다.</summary>
        public void ClearPending()
        {
            _iconImage.color = Color.white;
            _countText.color = Color.white;
        }

        public void Clear()
        {
            _currentItem = null;
            _iconImage.enabled = false;
            _countText.enabled = false;
            _iconImage.color   = Color.white;
            _countText.color   = Color.white;
            ApplyGradeColor(_gradeBorderImage, null);
            SetRouteMarked(false);
        }

        /// <summary>이 슬롯 아이템이 선택 루트의 필요 재료인지 표시(좌상단 노란 삼각형).</summary>
        public void SetRouteMarked(bool marked)
        {
            if (_routeIndicator != null)
                _routeIndicator.enabled = marked;
        }

        private void CreateRouteIndicator()
        {
            GameObject go = new("RouteIndicator");
            go.transform.SetParent(transform, false);

            _routeIndicator = go.AddComponent<TriangleIndicator>();
            _routeIndicator.color         = Color.yellow;
            _routeIndicator.raycastTarget = false;
            _routeIndicator.enabled       = false;

            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.sizeDelta        = new Vector2(16f, 16f);
            rt.anchoredPosition = Vector2.zero;
        }

        private void HandleClick() => _onClicked?.Invoke(_slotIndex);
    }
}
