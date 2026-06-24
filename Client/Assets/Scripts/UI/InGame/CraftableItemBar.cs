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
    /// 현재 인벤토리(+열린 박스)로 제작 가능한 레시피의 결과물을 표시한다.
    /// 표시 순서: 선택 루트의 필요 재료/목표 장비 레시피를 앞으로 우선 배치, 나머지는 DB 순.
    /// 인벤토리/열린 박스가 바뀔 때마다 갱신한다.
    /// </summary>
    public class CraftableItemBar : MonoBehaviour
    {
        public const int SlotCount = 5;

        [SerializeField] private InventorySlotUI[] _slots; // 5개
        [SerializeField] private CraftingSystem    _craftingSystem;
        [SerializeField] private InventorySystem   _inventorySystem;

        // 루트 우선표기용 — 선택 루트(MatchSelectionData) + 레시피 트리(RecipeDatabase)
        [SerializeField] private MatchSelectionData _matchSelection;
        [SerializeField] private RecipeDatabase     _recipeDatabase;

        // ⚠️ GC 주의: 갱신마다 재사용하는 버퍼 (할당 회피)
        private readonly List<RecipeData> _recipeBuffer = new();
        private readonly ItemData[]       _itemBuffer   = new ItemData[SlotCount];

        // 슬롯 인덱스 → 표시 중인 레시피 (클릭 시 조합 대상 매핑)
        private readonly RecipeData[]     _slotRecipes  = new RecipeData[SlotCount];

        // 선택 루트의 필요 아이템 ID 집합 (목표 장비 + 하위 재료). 루트 확정 후 1회 계산.
        private HashSet<string> _routeNeeded;
        private bool            _routeNeededBuilt;

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

            // 열린 박스가 있으면 그 내용물을 추가 재료로 포함해 조합 후보 전체 계산 (없으면 인벤토리만)
            // ⚠️ GC 주의: 전체 레시피 순회 — 인벤토리/박스 변경 시에만 호출되므로 허용
            _craftingSystem.GetCraftableRecipes(_recipeBuffer, int.MaxValue, LootBoxUI.ActiveBox);

            HashSet<string> needed = GetRouteNeeded();

            // 2패스 우선순위 채우기: 1) 루트 관련 레시피, 2) 나머지 (각 패스 내 DB 순 유지)
            int filled = 0;
            filled = FillSlots(filled, needed, routeOnly: true);
            filled = FillSlots(filled, needed, routeOnly: false);

            for (int i = filled; i < SlotCount; i++)
            {
                _slotRecipes[i] = null;
                _itemBuffer[i]  = null;
            }

            SetCraftables(_itemBuffer);
        }

        // routeOnly에 맞는 레시피를 빈 슬롯에 채우고 다음 채울 인덱스를 반환한다.
        private int FillSlots(int start, HashSet<string> needed, bool routeOnly)
        {
            int filled = start;
            for (int i = 0; i < _recipeBuffer.Count && filled < SlotCount; i++)
            {
                RecipeData recipe = _recipeBuffer[i];
                bool isRoute = recipe?.ResultItem != null && needed != null && needed.Contains(recipe.ResultItem.Id);
                if (isRoute != routeOnly) continue;

                _slotRecipes[filled] = recipe;
                _itemBuffer[filled]  = recipe != null ? recipe.ResultItem : null;
                filled++;
            }
            return filled;
        }

        // 선택 루트의 필요 아이템 집합 (1회 계산 후 캐시). 루트 미선택이면 null.
        private HashSet<string> GetRouteNeeded()
        {
            if (_routeNeededBuilt) return _routeNeeded;
            _routeNeededBuilt = true;

            SavedRoute route = _matchSelection != null ? _matchSelection.SelectedRoute : null;
            if (route != null && _recipeDatabase != null)
                _routeNeeded = ProjectER.UI.RouteSorter.CollectAllNeededItemIds(route, _recipeDatabase);

            return _routeNeeded;
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
