using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.UI
{
    /// <summary>
    /// 인벤토리 전체 UI — 가방 슬롯 10개 + 장비 슬롯 5개
    /// 슬롯 클릭: 가방 → 장착 시도 / 장비 → 해제 시도
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private InventorySlotUI[] _bagSlotUIs;    // 10개
        [SerializeField] private InventorySlotUI[] _equipSlotUIs;  // 5개

        // 장비 슬롯 배열 인덱스와 EquipmentSlotType 매핑 (Inspector 순서와 일치해야 함)
        private static readonly EquipmentSlotType[] EquipOrder =
        {
            EquipmentSlotType.Weapon,
            EquipmentSlotType.Chest,
            EquipmentSlotType.Helmet,
            EquipmentSlotType.Arms,
            EquipmentSlotType.Shoes,
        };

        private void Awake()
        {
            for (int i = 0; i < _bagSlotUIs.Length; i++)
                _bagSlotUIs[i].Initialize(i, OnBagSlotClicked, OnBagSlotRightClicked);

            for (int i = 0; i < _equipSlotUIs.Length; i++)
                _equipSlotUIs[i].Initialize(i, OnEquipSlotClicked);
        }

        private void OnEnable()
        {
            _inventorySystem.OnBagSlotChanged += HandleBagSlotChanged;
            _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            _inventorySystem.OnBagSlotChanged -= HandleBagSlotChanged;
            _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleBagSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < _bagSlotUIs.Length)
                _bagSlotUIs[index].Refresh(slot);
        }

        private void HandleEquipmentChanged(EquipmentSlotType slotType, ItemData item)
        {
            int index = Array.IndexOf(EquipOrder, slotType);
            if (index >= 0 && index < _equipSlotUIs.Length)
                _equipSlotUIs[index].RefreshEquipment(item);
        }

        private void RefreshAll()
        {
            IReadOnlyList<InventorySlot> bagSlots = _inventorySystem.BagSlots;
            for (int i = 0; i < _bagSlotUIs.Length; i++)
                _bagSlotUIs[i].Refresh(i < bagSlots.Count ? bagSlots[i] : null);

            for (int i = 0; i < EquipOrder.Length; i++)
                _equipSlotUIs[i].RefreshEquipment(_inventorySystem.GetEquippedItem(EquipOrder[i]));
        }

        // 가방 슬롯 좌클릭 — 장비 아이템이면 장착 시도
        private void OnBagSlotClicked(int index)
        {
            InventorySlot slot = _inventorySystem.BagSlots[index];
            if (!slot.IsEmpty)
                _inventorySystem.TryEquip(slot.Item);
        }

        // 가방 슬롯 우클릭 — 슬롯 전체 버리기
        private void OnBagSlotRightClicked(int index)
        {
            InventorySlot slot = _inventorySystem.BagSlots[index];
            if (!slot.IsEmpty)
                _inventorySystem.TryRemoveItem(slot.Item.Id, slot.Amount);
        }

        // 장비 슬롯 클릭 — 해제 시도
        private void OnEquipSlotClicked(int index)
        {
            if (index >= 0 && index < EquipOrder.Length)
                _inventorySystem.TryUnequip(EquipOrder[index]);
        }
    }
}
