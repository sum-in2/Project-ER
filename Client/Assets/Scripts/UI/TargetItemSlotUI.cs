using System;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 목표 아이템 슬롯 — 드래그로 아이템 설정, 우클릭으로 제거.
    /// 슬롯 타입에 맞는 부위 아이템만 수락한다.
    /// </summary>
    public class TargetItemSlotUI : MonoBehaviour, IItemDropTarget, IPointerClickHandler, IItemSlot
    {
        [SerializeField] private EquipmentSlotType _slotType;
        [SerializeField] private Image             _bgImage;
        [SerializeField] private Image             _iconImage;
        [SerializeField] private bool              _isDraggable;

        private static readonly Color EmptyColor = new Color(0.20f, 0.20f, 0.20f);

        private static readonly Color[] GradeColors =
        {
            new Color(0.30f, 0.30f, 0.30f), // Common   — 일반
            new Color(0.18f, 0.38f, 0.18f), // Uncommon — 고급
            new Color(0.15f, 0.28f, 0.50f), // Rare     — 희귀
            new Color(0.38f, 0.18f, 0.50f), // Epic     — 영웅
            new Color(0.55f, 0.38f, 0.08f), // Legend   — 전설
            new Color(0.55f, 0.15f, 0.15f), // Mythic   — 초월
        };

        private ItemData _currentItem;

        // 슬롯 타입 (TargetItemPanelUI에서 읽음)
        public EquipmentSlotType SlotType    => _slotType;

        // ── IItemSlot ─────────────────────────────────────────────────
        public ItemData CurrentItem => _currentItem;
        public bool     IsDraggable => _isDraggable;

        // 아이템 변경 이벤트 (슬롯 타입, 새 아이템 — 제거 시 null)
        public event Action<EquipmentSlotType, ItemData> OnItemChanged;

        private void Start()
        {
            ApplyEmpty();
        }

        // ── IItemDropTarget ────────────────────────────────────────────

        public bool TryDropItem(IItemSlot source)
        {
            ItemData dragged = source?.CurrentItem;
            if (dragged == null) return false;
            if (!IsCompatible(dragged))
            {
                Debug.Log($"[TargetItemSlotUI] {dragged.DisplayName}은 {_slotType} 슬롯에 맞지 않습니다.");
                return false;
            }

            ApplyItem(dragged);
            return true;
        }

        // ── IPointerClickHandler ──────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                ApplyEmpty();
        }

        // ── 외부 API ──────────────────────────────────────────────────

        /// <summary>
        /// 외부(TargetItemPanelUI 등)에서 직접 아이템 설정
        /// </summary>
        public void SetItem(ItemData item) => ApplyItem(item);

        /// <summary>
        /// 외부에서 슬롯 초기화
        /// </summary>
        public void ClearItem() => ApplyEmpty();

        // ── 내부 ─────────────────────────────────────────────────────

        private bool IsCompatible(ItemData item)
        {
            return _slotType switch
            {
                EquipmentSlotType.Weapon => item.ItemType == ItemType.Weapon,
                EquipmentSlotType.Helmet => item.ItemType == ItemType.Helmet,
                EquipmentSlotType.Chest  => item.ItemType == ItemType.Chest,
                EquipmentSlotType.Arms   => item.ItemType == ItemType.Arms,
                EquipmentSlotType.Shoes  => item.ItemType == ItemType.Shoes,
                _                        => false,
            };
        }

        private void ApplyItem(ItemData item)
        {
            _currentItem          = item;
            _bgImage.color        = GradeColors[(int)item.ItemGrade];
            _iconImage.sprite     = item.Icon;
            _iconImage.enabled    = item.Icon != null;
            OnItemChanged?.Invoke(_slotType, item);
        }

        private void ApplyEmpty()
        {
            _currentItem       = null;
            _bgImage.color     = EmptyColor;
            _iconImage.enabled = false;
            OnItemChanged?.Invoke(_slotType, null);
        }
    }
}
