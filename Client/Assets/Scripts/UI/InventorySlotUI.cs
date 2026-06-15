using System;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 슬롯 하나의 시각 표현 — 가방/장비 슬롯 공용
    /// 좌클릭: 장착, 우클릭: 버리기 (가방 슬롯 전용)
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IItemSlot
    {
        [SerializeField] private Image                _iconImage;
        [SerializeField] private Text                 _amountText;
        [SerializeField] private Image                _gradeBorderImage;
        [SerializeField] private ItemGradeColorConfig _gradeConfig;
        [SerializeField] private bool                 _isDraggable;

        private Button       _button;
        private int          _index;
        private Action<int>  _onClicked;
        private Action<int>  _onRightClicked;
        private ItemData     _currentItem;

        // ── IItemSlot ─────────────────────────────────────────────────
        public ItemData CurrentItem => _currentItem;
        public bool     IsDraggable => _isDraggable;

        private void Awake()
        {
            TryGetComponent(out _button);
            _button?.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(HandleClick);
        }

        public void Initialize(int index, Action<int> onClicked, Action<int> onRightClicked = null)
        {
            _index          = index;
            _onClicked      = onClicked;
            _onRightClicked = onRightClicked;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                _onRightClicked?.Invoke(_index);
        }

        /// <summary>가방 슬롯 갱신</summary>
        public void Refresh(InventorySlot slot)
        {
            bool hasItem = slot != null && !slot.IsEmpty;
            _currentItem = hasItem ? slot.Item : null;
            _iconImage.enabled  = hasItem;
            _amountText.enabled = hasItem;

            ApplyGradeBorder(hasItem ? slot.Item : null);

            if (!hasItem) return;

            _iconImage.sprite = slot.Item.Icon;
            bool showAmount   = slot.Item.IsStackable && slot.Amount > 1;
            _amountText.text  = showAmount ? slot.Amount.ToString() : string.Empty;
        }

        /// <summary>장비 슬롯 갱신 (수량 표시 없음)</summary>
        public void RefreshEquipment(ItemData item)
        {
            bool hasItem        = item != null;
            _currentItem        = item;
            _iconImage.enabled  = hasItem;
            _amountText.enabled = false;

            ApplyGradeBorder(item);

            if (hasItem)
                _iconImage.sprite = item.Icon;
        }

        private void ApplyGradeBorder(ItemData item)
        {
            if (_gradeBorderImage == null) return;

            if (item == null || _gradeConfig == null)
            {
                _gradeBorderImage.color = Color.clear;
                return;
            }

            _gradeBorderImage.color = _gradeConfig.GetColor(item.ItemGrade);
        }

        private void HandleClick() => _onClicked?.Invoke(_index);
    }
}
