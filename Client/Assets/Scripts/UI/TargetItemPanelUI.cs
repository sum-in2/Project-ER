using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.UI
{
    /// <summary>
    /// 목표 아이템 패널 — 부위별 5슬롯을 관리하고 현재 목표 아이템 목록을 제공한다.
    /// </summary>
    public class TargetItemPanelUI : MonoBehaviour
    {
        [SerializeField] private TargetItemSlotUI[] _slots; // 5개 (Weapon / Helmet / Chest / Arms / Shoes)

        // 현재 목표로 설정된 아이템 (슬롯 타입 → 아이템)
        private readonly Dictionary<EquipmentSlotType, ItemData> _targetItems = new();

        // 목표 아이템이 변경될 때 외부 시스템이 구독할 수 있음
        public event System.Action OnTargetChanged;

        private void OnEnable()
        {
            foreach (TargetItemSlotUI slot in _slots)
            {
                if (slot != null)
                    slot.OnItemChanged += HandleSlotChanged;
            }
        }

        private void OnDisable()
        {
            foreach (TargetItemSlotUI slot in _slots)
            {
                if (slot != null)
                    slot.OnItemChanged -= HandleSlotChanged;
            }
        }

        /// <summary>
        /// 아이템 타입에 맞는 슬롯에 아이템 설정. 장비 타입이 아니면 false 반환.
        /// </summary>
        public bool TrySetItem(ItemData item)
        {
            if (item == null) return false;

            EquipmentSlotType slotType;
            switch (item.ItemType)
            {
                case ItemType.Weapon: slotType = EquipmentSlotType.Weapon; break;
                case ItemType.Helmet: slotType = EquipmentSlotType.Helmet; break;
                case ItemType.Chest:  slotType = EquipmentSlotType.Chest;  break;
                case ItemType.Arms:   slotType = EquipmentSlotType.Arms;   break;
                case ItemType.Shoes:  slotType = EquipmentSlotType.Shoes;  break;
                default: return false;
            }

            foreach (TargetItemSlotUI slot in _slots)
            {
                if (slot == null || slot.SlotType != slotType) continue;
                slot.SetItem(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 현재 목표로 설정된 아이템 전체 반환 (null 슬롯 제외)
        /// </summary>
        public IReadOnlyDictionary<EquipmentSlotType, ItemData> GetTargetItems() => _targetItems;

        private void HandleSlotChanged(EquipmentSlotType slotType, ItemData item)
        {
            if (item != null)
                _targetItems[slotType] = item;
            else
                _targetItems.Remove(slotType);

            OnTargetChanged?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 빌더에서 슬롯 배열 주입용
        /// </summary>
        public void Editor_SetSlots(TargetItemSlotUI[] slots) => _slots = slots;
#endif
    }
}
