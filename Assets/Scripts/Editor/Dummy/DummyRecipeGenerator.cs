using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// 테스트용 더미 레시피 에셋 생성 도구
    /// 메뉴: ProjectER > Generate Dummy Recipes
    /// 실행 전 Generate Dummy Items 먼저 실행 필요
    /// </summary>
    public static class DummyRecipeGenerator
    {
        private const string ItemPath = "Assets/ScriptableObjects/Dummy/Items";
        private const string RecipePath = "Assets/ScriptableObjects/Dummy/Recipes";
        private const string DBPath = "Assets/ScriptableObjects/Dummy/RecipeDatabase.asset";

        [MenuItem("ProjectER/Generate Dummy Recipes")]
        public static void Generate()
        {
            // ── 재료 조합 레시피 ─────────────────────────────────────
            // 나무 x1 + 돌 x1 → 목재 판자 x1
            CreateRecipe("recipe_plank",
                result: "mat_plank",
                ("mat_wood", 1),
                ("mat_stone", 1));

            // 철광석 x1 + 나무 x1 → 철 주괴 x1
            CreateRecipe("recipe_iron_ingot",
                result: "mat_iron_ingot",
                ("mat_iron_ore", 1),
                ("mat_wood", 1));

            // 가죽 x1 + 섬유 x1 → 강화 가죽 x1
            CreateRecipe("recipe_hard_leather",
                result: "mat_hard_leather",
                ("mat_leather", 1),
                ("mat_fiber", 1));

            // ── 장비 제작 레시피 ─────────────────────────────────────
            // 철 주괴 x1 + 목재 판자 x1 → 철 검 x1
            CreateRecipe("recipe_iron_sword",
                result: "wpn_iron_sword",
                ("mat_iron_ingot", 1),
                ("mat_plank", 1));

            // 강화 가죽 x1 + 섬유 x1 → 가죽 조끼 x1
            CreateRecipe("recipe_leather_vest",
                result: "arm_leather_vest",
                ("mat_hard_leather", 1),
                ("mat_fiber", 1));

            // 강화 가죽 x1 + 철 주괴 x1 → 철 투구 x1
            CreateRecipe("recipe_iron_helmet",
                result: "arm_iron_helmet",
                ("mat_hard_leather", 1),
                ("mat_iron_ingot", 1));

            // ── RecipeDatabase 생성 / 갱신 ───────────────────────────
            RecipeDatabase db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(DBPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<RecipeDatabase>();
                AssetDatabase.CreateAsset(db, DBPath);
            }

            SerializedObject serializedDB = new SerializedObject(db);
            SerializedProperty recipesProp = serializedDB.FindProperty("_recipes");
            recipesProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:RecipeData", new[] { RecipePath });
            recipesProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = recipe;
            }

            serializedDB.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[DummyRecipeGenerator] 레시피 {guids.Length}개 생성, RecipeDatabase 갱신 완료.");
            EditorUtility.FocusProjectWindow();
        }

        private static void CreateRecipe(string assetName, string result, params (string id, int amount)[] ingredients)
        {
            string assetPath = $"{RecipePath}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath) != null)
            {
                Debug.Log($"[DummyRecipeGenerator] 이미 존재, 스킵: {assetPath}");
                return;
            }

            ItemData resultItem = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemPath}/{result}.asset");
            if (resultItem == null)
            {
                Debug.LogError($"[DummyRecipeGenerator] 결과 아이템 없음: {result} — Generate Dummy Items 먼저 실행하세요.");
                return;
            }

            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            SerializedObject so = new SerializedObject(recipe);

            so.FindProperty("_resultItem").objectReferenceValue = resultItem;
            so.FindProperty("_resultAmount").intValue = 1;

            SerializedProperty ingredientsProp = so.FindProperty("_ingredients");
            ingredientsProp.arraySize = ingredients.Length;
            for (int i = 0; i < ingredients.Length; i++)
            {
                ItemData ingredientItem = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemPath}/{ingredients[i].id}.asset");
                if (ingredientItem == null)
                {
                    Debug.LogError($"[DummyRecipeGenerator] 재료 아이템 없음: {ingredients[i].id}");
                    continue;
                }

                SerializedProperty element = ingredientsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_item").objectReferenceValue = ingredientItem;
                element.FindPropertyRelative("_amount").intValue = ingredients[i].amount;
            }

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(recipe, assetPath);
        }
    }
}
