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
        /// 저장 루트(SavedRoute)에서 목표 장비 자신 + 필요한 모든 재료 ID를 재귀 수집한다.
        /// 인게임 루트박스/조합칸의 우선표기에 사용. route 또는 db가 null이면 빈 셋 반환.
        /// 목표 장비 자신도 포함하므로(완성 장비가 박스에 드랍될 수 있음), 도감용과 달리 대상 ID도 넣는다.
        /// </summary>
        public static HashSet<string> CollectAllNeededItemIds(SavedRoute route, RecipeDatabase db)
        {
            // ⚠️ GC 주의: HashSet 할당 — 루트 확정 시 1회 호출
            HashSet<string> result = new HashSet<string>();
            if (route == null || db == null) return result;

            foreach (ItemData target in route.Items)
            {
                if (target == null) continue;
                result.Add(target.Id);            // 목표 장비 자신
                CollectIngredients(target.Id, result, db); // 하위 재료 재귀
            }

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

        /// <summary>
        /// item을 만드는 데 필요한 "기초 재료"(레시피가 없는 잎 아이템)를 수량과 함께 재귀 집계한다.
        /// result에 ItemData → 누적 수량을 더한다(multiplier = 상위 레시피에서의 필요 수량 곱).
        /// 잎 판정: RecipeDatabase에 결과 레시피가 없는 아이템.
        /// </summary>
        public static void CollectBaseMaterials(ItemData item, RecipeDatabase db,
            Dictionary<ItemData, int> result, int multiplier = 1)
        {
            if (item == null || db == null || result == null) return;

            RecipeData recipe = db.GetByResultId(item.Id);
            if (recipe == null)
            {
                // 잎(기초 재료) — 수량 누적
                result.TryGetValue(item, out int prev);
                result[item] = prev + multiplier;
                return;
            }

            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                CollectBaseMaterials(ingredient.Item, db, result, multiplier * ingredient.Amount);
            }
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
