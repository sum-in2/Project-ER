using System;
using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;

namespace ProjectER.Inventory
{
    /// <summary>
    /// 플레이어 인벤토리 — 가방 슬롯 10개 + 장비 슬롯 5개
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        private const int BagSize = 10;

        // 가방 슬롯
        private InventorySlot[] _bagSlots;

        // 장비 슬롯 — EquipmentSlotType 키로 O(1) 접근
        private Dictionary<EquipmentSlotType, ItemData> _equipmentSlots;

        // 가방 슬롯 변경 이벤트 (슬롯 인덱스, 변경된 슬롯)
        public event Action<int, InventorySlot> OnBagSlotChanged;

        // 장비 슬롯 변경 이벤트 (슬롯 타입, 장착된 아이템 — 해제 시 null)
        public event Action<EquipmentSlotType, ItemData> OnEquipmentChanged;

        public IReadOnlyList<InventorySlot> BagSlots
        {
            get
            {
                if (_bagSlots == null) InitializeCollections();
                return _bagSlots;
            }
        }

        private void Awake() => InitializeCollections();

        private void InitializeCollections()
        {
            if (_bagSlots != null) return;

            _bagSlots = new InventorySlot[BagSize];
            for (int i = 0; i < BagSize; i++)
                _bagSlots[i] = new InventorySlot();

            _equipmentSlots = new Dictionary<EquipmentSlotType, ItemData>();
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
                _equipmentSlots[slotType] = null;
        }

        // ── 가방 ─────────────────────────────────────────────────────

        /// <summary>
        /// 아이템을 가방에 추가. 스택 가능하면 기존 슬롯에 합산, 아니면 빈 슬롯 사용.
        /// </summary>
        public bool TryAddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            if (item.IsStackable)
            {
                // 이미 같은 아이템이 있는 슬롯에 추가
                for (int i = 0; i < BagSize; i++)
                {
                    InventorySlot slot = _bagSlots[i];
                    if (slot.IsEmpty || slot.Item != item) continue;
                    if (slot.Amount >= item.MaxStack) continue;

                    int addable = item.MaxStack - slot.Amount;
                    int toAdd = Math.Min(addable, amount);
                    slot.AddAmount(toAdd);
                    amount -= toAdd;
                    OnBagSlotChanged?.Invoke(i, slot);

                    if (amount <= 0) return true;
                }
            }

            // 빈 슬롯 탐색
            for (int i = 0; i < BagSize; i++)
            {
                if (!_bagSlots[i].IsEmpty) continue;

                int toAdd = item.IsStackable ? Math.Min(item.MaxStack, amount) : 1;
                _bagSlots[i].Set(item, toAdd);
                amount -= toAdd;
                OnBagSlotChanged?.Invoke(i, _bagSlots[i]);

                if (amount <= 0) return true;
            }

            // 가방이 꽉 찬 경우
            if (amount > 0)
                Debug.LogWarning($"[InventorySystem] 가방 공간 부족 — {item.DisplayName} {amount}개 추가 실패");

            return amount <= 0;
        }

        /// <summary>
        /// 아이템 ID로 지정 수량만큼 제거.
        /// </summary>
        public bool TryRemoveItem(string itemId, int amount = 1)
        {
            if (amount <= 0) return false;
            if (!HasItem(itemId, amount)) return false;

            for (int i = 0; i < BagSize && amount > 0; i++)
            {
                InventorySlot slot = _bagSlots[i];
                if (slot.IsEmpty || slot.Item.Id != itemId) continue;

                int toRemove = Math.Min(slot.Amount, amount);
                slot.AddAmount(-toRemove);
                amount -= toRemove;

                if (slot.Amount <= 0)
                    slot.Clear();

                OnBagSlotChanged?.Invoke(i, slot);
            }

            return true;
        }

        /// <summary>
        /// 아이템 ID로 보유 수량 반환.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            int total = 0;
            for (int i = 0; i < BagSize; i++)
            {
                InventorySlot slot = _bagSlots[i];
                if (!slot.IsEmpty && slot.Item.Id == itemId)
                    total += slot.Amount;
            }
            return total;
        }

        /// <summary>
        /// 지정 수량 이상 보유 중인지 확인.
        /// </summary>
        public bool HasItem(string itemId, int amount = 1)
        {
            return GetItemCount(itemId) >= amount;
        }

        // ── 장비 ─────────────────────────────────────────────────────

        /// <summary>
        /// 아이템을 해당 장비 슬롯에 장착. 기존 장착 아이템은 가방으로 이동.
        /// </summary>
        public bool TryEquip(ItemData item)
        {
            if (item == null) return false;
            if (!TryGetEquipmentSlotType(item.ItemType, out EquipmentSlotType slotType)) return false;

            ItemData current = _equipmentSlots[slotType];
            if (current != null)
            {
                // 기존 장비를 가방으로 — 공간 없으면 장착 취소
                if (!TryAddItem(current))
                {
                    Debug.LogWarning($"[InventorySystem] 가방 공간 부족 — {item.DisplayName} 장착 불가");
                    return false;
                }
            }

            TryRemoveItem(item.Id);
            _equipmentSlots[slotType] = item;
            OnEquipmentChanged?.Invoke(slotType, item);
            return true;
        }

        /// <summary>
        /// 장비 슬롯 해제. 해제된 아이템을 가방으로 이동.
        /// </summary>
        public bool TryUnequip(EquipmentSlotType slotType)
        {
            ItemData item = _equipmentSlots[slotType];
            if (item == null) return false;

            if (!TryAddItem(item))
            {
                Debug.LogWarning($"[InventorySystem] 가방 공간 부족 — {item.DisplayName} 해제 불가");
                return false;
            }

            _equipmentSlots[slotType] = null;
            OnEquipmentChanged?.Invoke(slotType, null);
            return true;
        }

        /// <summary>
        /// 장비 슬롯 아이템 조회. 비어있으면 null 반환.
        /// </summary>
        public ItemData GetEquippedItem(EquipmentSlotType slotType)
        {
            if (_equipmentSlots == null) InitializeCollections();
            return _equipmentSlots[slotType];
        }

        // ── 내부 유틸 ────────────────────────────────────────────────

        private bool TryGetEquipmentSlotType(ItemType itemType, out EquipmentSlotType slotType)
        {
            switch (itemType)
            {
                case ItemType.Weapon: slotType = EquipmentSlotType.Weapon; return true;
                case ItemType.Chest: slotType = EquipmentSlotType.Chest; return true;
                case ItemType.Helmet: slotType = EquipmentSlotType.Helmet; return true;
                case ItemType.Arms: slotType = EquipmentSlotType.Arms; return true;
                case ItemType.Shoes: slotType = EquipmentSlotType.Shoes; return true;
                default: slotType = default; return false;
            }
        }
    }
}
