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

        public bool CanCraft(RecipeData recipe)
        {
            if (recipe == null || recipe.ResultItem == null) return false;
            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) return false;
                if (!_inventorySystem.HasItem(ingredient.Item.Id, ingredient.Amount))
                    return false;
            }
            return true;
        }

        public bool TryCraft(RecipeData recipe)
        {
            if (!CanCraft(recipe)) return false;

            foreach (ItemIngredient ingredient in recipe.Ingredients)
                _inventorySystem.TryRemoveItem(ingredient.Item.Id, ingredient.Amount);

            bool success = _inventorySystem.TryAddItem(recipe.ResultItem, recipe.ResultAmount);
            if (success)
            {
                OnCraftComplete?.Invoke(recipe);
            }
            else
            {
                // 가방 공간 부족 — 차감한 재료 환불
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
                if (CanCraft(recipe))
                    results.Add(recipe);
            }

            Debug.Log($"[CraftingSystem] 조합 가능 레시피 {results.Count}개 / 전체 {_recipeDatabase.Recipes.Count}개");
        }
    }
}
