using System.Collections.Generic;
using ProjectER.Character;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 인게임 화면 하단의 가방 전용 인벤토리 바 (10칸).
    /// InventorySlotUI를 재사용하며, 장비 슬롯은 다루지 않는다.
    /// 좌클릭: 장착 시도 / 우클릭: 슬롯 버리기 (InventoryUI 가방 동작과 동일).
    /// </summary>
    public class InGameInventoryBar : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private InventorySlotUI[] _bagSlotUIs; // 10개

        private void Awake()
        {
            for (int i = 0; i < _bagSlotUIs.Length; i++)
                _bagSlotUIs[i].Initialize(i, OnSlotClicked, OnSlotRightClicked);
        }

        private void OnEnable()
        {
            // 빌더가 참조를 연결하지 못한 경우 대비한 런타임 폴백 (테스트 HUD 한정).
            // 씬에는 LoadOut 패널 등 다른 InventorySystem도 존재하므로, 반드시 조작 중인
            // 플레이어(PlayerController)의 InventorySystem을 가져와 LootBox가 넣는 대상과 일치시킨다.
            if (_inventorySystem == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    player.TryGetComponent(out _inventorySystem);
            }

            if (_inventorySystem == null) return;

            _inventorySystem.OnBagSlotChanged += HandleBagSlotChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_inventorySystem == null) return;

            _inventorySystem.OnBagSlotChanged -= HandleBagSlotChanged;
        }

        private void HandleBagSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < _bagSlotUIs.Length)
                _bagSlotUIs[index].Refresh(slot);
        }

        private void RefreshAll()
        {
            IReadOnlyList<InventorySlot> bagSlots = _inventorySystem.BagSlots;
            for (int i = 0; i < _bagSlotUIs.Length; i++)
                _bagSlotUIs[i].Refresh(i < bagSlots.Count ? bagSlots[i] : null);
        }

        private void OnSlotClicked(int index)
        {
            InventorySlot slot = _inventorySystem.BagSlots[index];
            if (!slot.IsEmpty)
                _inventorySystem.TryEquip(slot.Item);
        }

        private void OnSlotRightClicked(int index)
        {
            InventorySlot slot = _inventorySystem.BagSlots[index];
            if (!slot.IsEmpty)
                _inventorySystem.TryRemoveItem(slot.Item.Id, slot.Amount);
        }
    }
}
