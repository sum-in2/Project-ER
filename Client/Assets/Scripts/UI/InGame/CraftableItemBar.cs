using System.Collections.Generic;
using ProjectER.Character;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 인벤토리 구역 위에 표시되는 "조합 가능 아이템" 5칸.
    /// 현재 인벤토리로 제작 가능한 레시피의 결과물을 표시한다 (순서는 레시피 DB 순, 추후 정렬 예정).
    /// 인벤토리가 바뀔 때마다 갱신한다. 조합 실행 연동은 추후.
    /// </summary>
    public class CraftableItemBar : MonoBehaviour
    {
        public const int SlotCount = 5;

        [SerializeField] private InventorySlotUI[] _slots; // 5개
        [SerializeField] private CraftingSystem    _craftingSystem;
        [SerializeField] private InventorySystem   _inventorySystem;

        // ⚠️ GC 주의: 갱신마다 재사용하는 버퍼 (할당 회피)
        private readonly List<RecipeData> _recipeBuffer = new();
        private readonly ItemData[]       _itemBuffer   = new ItemData[SlotCount];

        // 슬롯 인덱스 → 표시 중인 레시피 (클릭 시 조합 대상 매핑)
        private readonly RecipeData[]     _slotRecipes  = new RecipeData[SlotCount];

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Initialize(i, OnSlotClicked);
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_inventorySystem != null)
                _inventorySystem.OnBagSlotChanged += HandleInventoryChanged;

            // 열린 루트박스가 바뀌거나 내용물이 바뀌면 조합 후보 갱신
            LootBoxUI.OnActiveBoxChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnBagSlotChanged -= HandleInventoryChanged;

            LootBoxUI.OnActiveBoxChanged -= Refresh;
        }

        // 빌더가 참조를 연결하지 못한 경우 대비한 런타임 폴백 (테스트 HUD 한정).
        // 인벤토리 바와 동일하게 조작 중인 플레이어의 컴포넌트를 사용한다.
        private void ResolveReferences()
        {
            if (_inventorySystem != null && _craftingSystem != null) return;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (_inventorySystem == null) player.TryGetComponent(out _inventorySystem);
            if (_craftingSystem == null) player.TryGetComponent(out _craftingSystem);
        }

        private void HandleInventoryChanged(int index, InventorySlot slot) => Refresh();

        private void Refresh()
        {
            if (_craftingSystem == null)
            {
                for (int i = 0; i < SlotCount; i++) _slotRecipes[i] = null;
                SetCraftables(null);
                return;
            }

            // 열린 박스가 있으면 그 내용물을 추가 재료로 포함해 조합 후보 계산 (없으면 인벤토리만)
            _craftingSystem.GetCraftableRecipes(_recipeBuffer, SlotCount, LootBoxUI.ActiveBox);
            for (int i = 0; i < SlotCount; i++)
            {
                RecipeData recipe = i < _recipeBuffer.Count ? _recipeBuffer[i] : null;
                _slotRecipes[i] = recipe;
                _itemBuffer[i]  = recipe != null ? recipe.ResultItem : null;
            }

            SetCraftables(_itemBuffer);
        }

        /// <summary>
        /// 조합 가능 아이템을 슬롯에 표시한다 (최대 5개).
        /// </summary>
        public void SetCraftables(IReadOnlyList<ItemData> items)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                ItemData item = items != null && i < items.Count ? items[i] : null;
                _slots[i].RefreshEquipment(item); // 수량 표시 없는 단일 아이콘 표시
            }
        }

        private void OnSlotClicked(int index)
        {
            if (index < 0 || index >= SlotCount) return;

            // 클릭 시점의 레시피를 먼저 확보 — TryCraft가 인벤토리를 바꾸면
            // OnBagSlotChanged → Refresh로 _slotRecipes가 갱신되기 때문
            RecipeData recipe = _slotRecipes[index];
            if (recipe == null || _craftingSystem == null) return;

            // 제작 — 인벤토리 우선, 부족분은 열린 박스에서 보충. 성공 시 이벤트로 표시 자동 갱신.
            _craftingSystem.TryCraft(recipe, LootBoxUI.ActiveBox);
        }
    }
}
