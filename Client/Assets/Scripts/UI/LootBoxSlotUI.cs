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
    public class LootBoxSlotUI : MonoBehaviour, IItemSlot
    {
        [SerializeField] private Image                _iconImage;
        [SerializeField] private Text                 _countText;
        [SerializeField] private Image                _gradeBorderImage;
        [SerializeField] private ItemGradeColorConfig _gradeConfig;

        private int         _slotIndex;
        private Action<int> _onClicked;
        private ItemData    _item;

        // ── IItemSlot ──────────────────────────────────────────────────
        public ItemData CurrentItem => _item;
        public bool     IsDraggable => false; // 루트박스 슬롯은 드래그 금지

        private void Awake()
        {
            if (TryGetComponent(out Button btn))
                btn.onClick.AddListener(HandleClick);
        }

        private void OnEnable()  { }
        private void OnDisable() { }

        private void OnDestroy()
        {
            if (TryGetComponent(out Button btn))
                btn.onClick.RemoveListener(HandleClick);
        }

        public void Initialize(int index, Action<int> onClicked)
        {
            _slotIndex = index;
            _onClicked = onClicked;
        }

        public void Refresh(ItemData item, int count)
        {
            _item = item;

            bool hasItem = item != null && count > 0;
            _iconImage.enabled = hasItem;
            _countText.enabled = hasItem;
            ApplyGradeBorder(hasItem ? item : null);

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
            _item = null;
            _iconImage.enabled = false;
            _countText.enabled = false;
            _iconImage.color   = Color.white;
            _countText.color   = Color.white;
            ApplyGradeBorder(null);
        }

        private void ApplyGradeBorder(ItemData item)
        {
            if (_gradeBorderImage == null) return;
            _gradeBorderImage.color = (item != null && _gradeConfig != null)
                ? _gradeConfig.GetColor(item.ItemGrade)
                : Color.clear;
        }

        private void HandleClick() => _onClicked?.Invoke(_slotIndex);
    }
}
