using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;

namespace ProjectER.UI
{
    /// <summary>
    /// 레시피 트리 재귀 탐색 유틸 — 루트 정렬 및 특수 재료 필터에 사용된다.
    /// </summary>
    public static class RouteSorter
    {
        /// <summary>
        /// 목표 루트 전체에서 필요한 모든 재료 ID를 재귀 수집한다.
        /// targetPanel 또는 db가 null이면 빈 셋 반환.
        /// </summary>
        public static HashSet<string> CollectAllNeededItemIds(TargetItemPanelUI targetPanel, RecipeDatabase db)
        {
            // ⚠️ GC 주의: HashSet 할당 — 루트 변경 시에만 호출
            HashSet<string> result = new HashSet<string>();
            if (targetPanel == null || db == null) return result;

            foreach (KeyValuePair<EquipmentSlotType, ItemData> pair in targetPanel.GetTargetItems())
                CollectIngredients(pair.Value.Id, result, db);

            return result;
        }

        /// <summary>
        /// itemId의 레시피 트리에 targetMaterialId가 포함되어 있으면 true.
        /// visited로 순환 참조를 방지한다.
        /// </summary>
        public static bool ItemNeedsMaterial(string itemId, string targetMaterialId, RecipeDatabase db, HashSet<string> visited)
        {
            if (itemId == targetMaterialId) return true;
            if (!visited.Add(itemId)) return false;
            if (db == null) return false;

            RecipeData recipe = db.GetByResultId(itemId);
            if (recipe == null) return false;

            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (ItemNeedsMaterial(ingredient.Item.Id, targetMaterialId, db, visited))
                    return true;
            }
            return false;
        }

        private static void CollectIngredients(string itemId, HashSet<string> result, RecipeDatabase db)
        {
            RecipeData recipe = db.GetByResultId(itemId);
            if (recipe == null) return;

            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (result.Add(ingredient.Item.Id))
                    CollectIngredients(ingredient.Item.Id, result, db);
            }
        }
    }
}
