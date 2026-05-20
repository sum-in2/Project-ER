using System.Collections.Generic;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.UI
{
    /// <summary>
    /// 조합 가능 레시피 목록 UI — 인벤토리 변경 시 자동 갱신, 최대 5개 표시
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private CraftingSystem   _craftingSystem;
        [SerializeField] private InventorySystem  _inventorySystem;
        [SerializeField] private CraftingSlotUI[] _slots;

        // ⚠️ GC 주의: 버퍼 재사용으로 매 갱신 시 List 할당 방지
        private readonly List<RecipeData> _craftableBuffer = new List<RecipeData>(5);

        private void OnEnable()
        {
            _inventorySystem.OnBagSlotChanged   += HandleInventoryChanged;
            _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
            RefreshSlots();
        }

        private void OnDisable()
        {
            _inventorySystem.OnBagSlotChanged   -= HandleInventoryChanged;
            _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleInventoryChanged(int _, InventorySlot __) => RefreshSlots();
        private void HandleEquipmentChanged(EquipmentSlotType _, ItemData __) => RefreshSlots();

        private void RefreshSlots()
        {
            _craftingSystem.GetCraftableRecipes(_craftableBuffer, _slots.Length);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < _craftableBuffer.Count)
                    _slots[i].Show(_craftableBuffer[i], OnCraft);
                else
                    _slots[i].Hide();
            }
        }

        private void OnCraft(RecipeData recipe)
        {
            // TryCraft → 인벤토리 변경 → OnBagSlotChanged → RefreshSlots 자동 호출
            _craftingSystem.TryCraft(recipe);
        }
    }
}
