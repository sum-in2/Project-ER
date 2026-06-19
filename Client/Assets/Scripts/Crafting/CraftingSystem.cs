using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.Crafting
{
    /// <summary>
    /// 크래프팅 로직 — 재료 검증, 차감, 결과물 추가
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private RecipeDatabase  _recipeDatabase;

        public event Action<RecipeData> OnCraftComplete;

        public bool CanCraft(RecipeData recipe) => CanCraft(recipe, null);

        /// <summary>
        /// 제작 가능 여부 판정. extra가 주어지면 인벤토리 보유량 + extra 보유량으로 계산한다
        /// (예: 열린 루트박스 내용물).
        /// </summary>
        public bool CanCraft(RecipeData recipe, IMaterialSource extra)
        {
            if (recipe == null || recipe.ResultItem == null) return false;
            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) return false;

                int available = _inventorySystem.GetItemCount(ingredient.Item.Id);
                if (extra != null)
                    available += extra.GetCount(ingredient.Item.Id);

                if (available < ingredient.Amount) return false;
            }
            return true;
        }

        public bool TryCraft(RecipeData recipe) => TryCraft(recipe, null);

        /// <summary>
        /// 제작. extra(예: 열린 루트박스)가 있으면 인벤토리 부족분을 extra에서 보충한다.
        /// 재료 소모 우선순위: 인벤토리 &gt; extra.
        /// </summary>
        public bool TryCraft(RecipeData recipe, IMaterialSource extra)
        {
            if (!CanCraft(recipe, extra)) return false;

            // 재료 차감 — 인벤토리 먼저, 부족분만 extra에서
            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                string id   = ingredient.Item.Id;
                int    need = ingredient.Amount;

                int fromInv = Mathf.Min(_inventorySystem.GetItemCount(id), need);
                if (fromInv > 0)
                {
                    _inventorySystem.TryRemoveItem(id, fromInv);
                    need -= fromInv;
                }

                if (need > 0 && extra != null)
                    extra.Remove(id, need);
            }

            bool success = _inventorySystem.TryAddItem(recipe.ResultItem, recipe.ResultAmount);
            if (success)
            {
                OnCraftComplete?.Invoke(recipe);
            }
            else
            {
                // 가방 공간 부족 — 차감한 재료를 인벤토리로 환불 (extra로 되돌리지는 않음)
                foreach (ItemIngredient ingredient in recipe.Ingredients)
                    _inventorySystem.TryAddItem(ingredient.Item, ingredient.Amount);
                Debug.LogWarning($"[CraftingSystem] 가방 공간 부족 — {recipe.ResultItem.DisplayName} 제작 불가");
            }
            return success;
        }

        /// <summary>
        /// 현재 인벤토리로 제작 가능한 레시피를 results에 채움.
        /// ⚠️ GC 주의: results 리스트는 호출자가 제공 — 내부에서 Clear 후 채움
        /// </summary>
        public void GetCraftableRecipes(List<RecipeData> results, int maxCount = int.MaxValue)
            => GetCraftableRecipes(results, maxCount, null);

        /// <summary>
        /// 현재 인벤토리(+extra 추가 재료)로 제작 가능한 레시피를 results에 채움.
        /// extra는 인벤토리 외 가용 재료(예: 열린 루트박스).
        /// ⚠️ GC 주의: results 리스트는 호출자가 제공 — 내부에서 Clear 후 채움
        /// </summary>
        public void GetCraftableRecipes(List<RecipeData> results, int maxCount,
            IMaterialSource extra)
        {
            results.Clear();

            if (_recipeDatabase == null)
            {
                Debug.LogError("[CraftingSystem] RecipeDatabase가 연결되지 않았습니다.");
                return;
            }
            if (_inventorySystem == null)
            {
                Debug.LogError("[CraftingSystem] InventorySystem이 연결되지 않았습니다.");
                return;
            }

            foreach (RecipeData recipe in _recipeDatabase.Recipes)
            {
                if (results.Count >= maxCount) break;
                if (CanCraft(recipe, extra))
                    results.Add(recipe);
            }
        }
    }
}
