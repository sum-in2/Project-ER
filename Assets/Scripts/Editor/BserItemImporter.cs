using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// BSER Open API JSON → ItemData ScriptableObject 일괄 임포터
    /// 메뉴: ProjectER > Import BSER Items
    /// </summary>
    public static class BserItemImporter
    {
        private const string JsonRoot      = "Assets/Resources/Data/BSER";
        private const string ItemSavePath  = "Assets/ScriptableObjects/Items/BSER";
        private const string DbSavePath    = "Assets/ScriptableObjects";

        // ── JSON 파싱용 내부 클래스 ───────────────────────────────────

        [Serializable]
        private class BserItemWeapon
        {
            public int    code;
            public string name;
            public string weaponType;
            public string itemGrade;
            public bool   isCompletedItem;
            public int    manufacturableType;
            public int    makeMaterial1;
            public int    makeMaterial2;
            public int    stackable;
            public float  attackPower;
            public float  defense;
            public float  maxHp;
            public float  moveSpeed;
            public float  attackSpeedRatio;
            public float  skillAmp;
            public float  criticalStrikeChance;
            public float  lifeSteal;
            public float  cooldownReduction;
            public float  adaptiveForce;
            public float  hpRegenRatio;
            public float  penetrationDefense;
        }

        [Serializable]
        private class BserItemWeaponList { public BserItemWeapon[] items; }

        [Serializable]
        private class BserItemArmor
        {
            public int    code;
            public string name;
            public string armorType;
            public string itemGrade;
            public bool   isCompletedItem;
            public int    manufacturableType;
            public int    makeMaterial1;
            public int    makeMaterial2;
            public int    stackable;
            public float  attackPower;
            public float  defense;
            public float  maxHp;
            public float  moveSpeed;
            public float  attackSpeedRatio;
            public float  skillAmp;
            public float  criticalStrikeChance;
            public float  lifeSteal;
            public float  cooldownReduction;
            public float  adaptiveForce;
            public float  hpRegenRatio;
            public float  penetrationDefense;
        }

        [Serializable]
        private class BserItemArmorList { public BserItemArmor[] items; }

        [Serializable]
        private class BserItemConsumable
        {
            public int    code;
            public string name;
            public string consumableType;
            public string itemGrade;
            public bool   isCompletedItem;
            public int    manufacturableType;
            public int    makeMaterial1;
            public int    makeMaterial2;
            public int    stackable;
            public float  hpRecover;
        }

        [Serializable]
        private class BserItemConsumableList { public BserItemConsumable[] items; }

        [Serializable]
        private class BserItemMisc
        {
            public int    code;
            public string name;
            public string itemGrade;
            public bool   isCompletedItem;
            public int    manufacturableType;
            public int    makeMaterial1;
            public int    makeMaterial2;
            public int    stackable;
        }

        [Serializable]
        private class BserItemMiscList { public BserItemMisc[] items; }

        // ── 진입점 ───────────────────────────────────────────────────

        [MenuItem("ProjectER/Import BSER Items")]
        public static void Import()
        {
            EnsureDirectory(ItemSavePath);

            // 파싱 → SO 생성
            Dictionary<int, ItemData> createdItems = new();
            ImportWeapons(createdItems);
            ImportArmors(createdItems);
            ImportConsumables(createdItems);
            ImportMisc(createdItems);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ItemDatabase 갱신
            RebuildItemDatabase();

            // RecipeDatabase 갱신
            RebuildRecipeDatabase(createdItems);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BserItemImporter] 임포트 완료 — 아이템 {createdItems.Count}개");
            EditorUtility.FocusProjectWindow();
        }

        // ── 무기 임포트 ──────────────────────────────────────────────

        private static void ImportWeapons(Dictionary<int, ItemData> createdItems)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/ItemWeapon.json");
            if (json == null) { Debug.LogError("[BserItemImporter] ItemWeapon.json 없음"); return; }

            BserItemWeaponList list = JsonUtility.FromJson<BserItemWeaponList>("{\"items\":" + json.text + "}");
            foreach (BserItemWeapon src in list.items)
            {
                ItemData so = GetOrCreateItem(src.code, "wpn");
                SerializedObject s = new SerializedObject(so);

                // BSER 연동
                s.FindProperty("_bserCode").intValue           = src.code;
                s.FindProperty("_isCompletedItem").boolValue   = src.isCompletedItem;
                s.FindProperty("_manufacturableType").intValue = src.manufacturableType;

                // 기본 정보
                s.FindProperty("_id").stringValue          = src.code.ToString();
                s.FindProperty("_displayName").stringValue = src.name;
                s.FindProperty("_itemType").enumValueIndex = (int)ItemType.Weapon;
                s.FindProperty("_weaponType").enumValueIndex = (int)ParseWeaponType(src.weaponType);

                // 인벤토리
                s.FindProperty("_isStackable").boolValue = src.stackable > 1;
                s.FindProperty("_maxStack").intValue     = Mathf.Max(1, src.stackable);

                // 스탯
                s.FindProperty("_attackPower").floatValue          = src.attackPower;
                s.FindProperty("_defense").floatValue              = src.defense;
                s.FindProperty("_maxHpBonus").floatValue           = src.maxHp;
                s.FindProperty("_moveSpeedBonus").floatValue       = src.moveSpeed;
                s.FindProperty("_attackSpeedBonus").floatValue     = src.attackSpeedRatio;
                s.FindProperty("_skillAmp").floatValue             = src.skillAmp;
                s.FindProperty("_cooldownReduction").floatValue    = src.cooldownReduction;
                s.FindProperty("_adaptiveForce").floatValue        = src.adaptiveForce;
                s.FindProperty("_criticalStrikeChance").floatValue = src.criticalStrikeChance;
                s.FindProperty("_lifeSteal").floatValue            = src.lifeSteal;

                s.ApplyModifiedProperties();
                createdItems[src.code] = so;
            }
            Debug.Log($"[BserItemImporter] 무기 {list.items.Length}개 처리");
        }

        // ── 방어구 임포트 ─────────────────────────────────────────────

        private static void ImportArmors(Dictionary<int, ItemData> createdItems)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/ItemArmor.json");
            if (json == null) { Debug.LogError("[BserItemImporter] ItemArmor.json 없음"); return; }

            BserItemArmorList list = JsonUtility.FromJson<BserItemArmorList>("{\"items\":" + json.text + "}");
            foreach (BserItemArmor src in list.items)
            {
                ItemData so = GetOrCreateItem(src.code, "arm");
                SerializedObject s = new SerializedObject(so);

                // BSER 연동
                s.FindProperty("_bserCode").intValue           = src.code;
                s.FindProperty("_isCompletedItem").boolValue   = src.isCompletedItem;
                s.FindProperty("_manufacturableType").intValue = src.manufacturableType;

                // 기본 정보
                s.FindProperty("_id").stringValue          = src.code.ToString();
                s.FindProperty("_displayName").stringValue = src.name;
                s.FindProperty("_itemType").enumValueIndex = (int)ParseArmorType(src.armorType);

                // 인벤토리
                s.FindProperty("_isStackable").boolValue = src.stackable > 1;
                s.FindProperty("_maxStack").intValue     = Mathf.Max(1, src.stackable);

                // 스탯
                s.FindProperty("_attackPower").floatValue          = src.attackPower;
                s.FindProperty("_defense").floatValue              = src.defense;
                s.FindProperty("_maxHpBonus").floatValue           = src.maxHp;
                s.FindProperty("_moveSpeedBonus").floatValue       = src.moveSpeed;
                s.FindProperty("_attackSpeedBonus").floatValue     = src.attackSpeedRatio;
                s.FindProperty("_skillAmp").floatValue             = src.skillAmp;
                s.FindProperty("_cooldownReduction").floatValue    = src.cooldownReduction;
                s.FindProperty("_adaptiveForce").floatValue        = src.adaptiveForce;
                s.FindProperty("_criticalStrikeChance").floatValue = src.criticalStrikeChance;
                s.FindProperty("_lifeSteal").floatValue            = src.lifeSteal;
                s.FindProperty("_hpRegenRatio").floatValue         = src.hpRegenRatio;
                s.FindProperty("_penetrationDefense").floatValue   = src.penetrationDefense;

                s.ApplyModifiedProperties();
                createdItems[src.code] = so;
            }
            Debug.Log($"[BserItemImporter] 방어구 {list.items.Length}개 처리");
        }

        // ── 소비 아이템 임포트 ────────────────────────────────────────

        private static void ImportConsumables(Dictionary<int, ItemData> createdItems)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/ItemConsumable.json");
            if (json == null) { Debug.LogError("[BserItemImporter] ItemConsumable.json 없음"); return; }

            BserItemConsumableList list = JsonUtility.FromJson<BserItemConsumableList>("{\"items\":" + json.text + "}");
            foreach (BserItemConsumable src in list.items)
            {
                ItemData so = GetOrCreateItem(src.code, "con");
                SerializedObject s = new SerializedObject(so);

                // BSER 연동
                s.FindProperty("_bserCode").intValue           = src.code;
                s.FindProperty("_isCompletedItem").boolValue   = src.isCompletedItem;
                s.FindProperty("_manufacturableType").intValue = src.manufacturableType;

                // 기본 정보
                s.FindProperty("_id").stringValue          = src.code.ToString();
                s.FindProperty("_displayName").stringValue = src.name;
                s.FindProperty("_itemType").enumValueIndex = (int)ParseConsumableType(src.consumableType);

                // 인벤토리 (소비 아이템은 stackable)
                s.FindProperty("_isStackable").boolValue = src.stackable > 1;
                s.FindProperty("_maxStack").intValue     = Mathf.Max(1, src.stackable);

                // 소비 효과
                s.FindProperty("_hpRestore").floatValue = src.hpRecover;

                s.ApplyModifiedProperties();
                createdItems[src.code] = so;
            }
            Debug.Log($"[BserItemImporter] 소비 아이템 {list.items.Length}개 처리");
        }

        // ── 기타 아이템 임포트 ────────────────────────────────────────

        private static void ImportMisc(Dictionary<int, ItemData> createdItems)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/ItemMisc.json");
            if (json == null) { Debug.LogError("[BserItemImporter] ItemMisc.json 없음"); return; }

            BserItemMiscList list = JsonUtility.FromJson<BserItemMiscList>("{\"items\":" + json.text + "}");
            foreach (BserItemMisc src in list.items)
            {
                ItemData so = GetOrCreateItem(src.code, "mat");
                SerializedObject s = new SerializedObject(so);

                // BSER 연동
                s.FindProperty("_bserCode").intValue           = src.code;
                s.FindProperty("_isCompletedItem").boolValue   = src.isCompletedItem;
                s.FindProperty("_manufacturableType").intValue = src.manufacturableType;

                // 기본 정보
                s.FindProperty("_id").stringValue          = src.code.ToString();
                s.FindProperty("_displayName").stringValue = src.name;
                s.FindProperty("_itemType").enumValueIndex = (int)ItemType.Material;

                // 인벤토리
                s.FindProperty("_isStackable").boolValue = src.stackable > 1;
                s.FindProperty("_maxStack").intValue     = Mathf.Max(1, src.stackable);

                s.ApplyModifiedProperties();
                createdItems[src.code] = so;
            }
            Debug.Log($"[BserItemImporter] 기타(재료) {list.items.Length}개 처리");
        }

        // ── DB 재구축 ─────────────────────────────────────────────────

        private static void RebuildItemDatabase()
        {
            string dbPath = $"{DbSavePath}/ItemDatabase.asset";
            ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            SerializedObject s = new SerializedObject(db);
            SerializedProperty itemsProp = s.FindProperty("_items");
            itemsProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemSavePath });
            itemsProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<ItemData>(path);
            }

            s.ApplyModifiedProperties();
            Debug.Log($"[BserItemImporter] ItemDatabase 갱신 — {guids.Length}개");
        }

        private static void RebuildRecipeDatabase(Dictionary<int, ItemData> allItems)
        {
            string recipeSavePath = "Assets/ScriptableObjects/Recipes/BSER";
            EnsureDirectory(recipeSavePath);

            // 재료 코드가 ItemData에 저장되지 않으므로 JSON을 다시 읽어 레시피 구성
            // (에디터 전용 처리 — 런타임 성능 무관)
            BuildRecipesFromJson<BserItemWeaponList, BserItemWeapon>(
                $"{JsonRoot}/ItemWeapon.json", allItems, recipeSavePath,
                src => src.code, src => src.makeMaterial1, src => src.makeMaterial2);

            BuildRecipesFromJson<BserItemArmorList, BserItemArmor>(
                $"{JsonRoot}/ItemArmor.json", allItems, recipeSavePath,
                src => src.code, src => src.makeMaterial1, src => src.makeMaterial2);

            BuildRecipesFromJson<BserItemConsumableList, BserItemConsumable>(
                $"{JsonRoot}/ItemConsumable.json", allItems, recipeSavePath,
                src => src.code, src => src.makeMaterial1, src => src.makeMaterial2);

            BuildRecipesFromJson<BserItemMiscList, BserItemMisc>(
                $"{JsonRoot}/ItemMisc.json", allItems, recipeSavePath,
                src => src.code, src => src.makeMaterial1, src => src.makeMaterial2);

            AssetDatabase.SaveAssets();

            // RecipeDatabase 갱신
            string dbPath = $"{DbSavePath}/RecipeDatabase.asset";
            RecipeDatabase db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<RecipeDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            SerializedObject s = new SerializedObject(db);
            SerializedProperty recipesProp = s.FindProperty("_recipes");
            recipesProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:RecipeData", new[] { recipeSavePath });
            recipesProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                recipesProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            }

            s.ApplyModifiedProperties();
            Debug.Log($"[BserItemImporter] RecipeDatabase 갱신 — {guids.Length}개");
        }

        /// <summary>
        /// 하나의 JSON 파일에서 레시피 SO를 생성한다.
        /// </summary>
        private static void BuildRecipesFromJson<TList, TItem>(
            string jsonPath,
            Dictionary<int, ItemData> allItems,
            string savePath,
            Func<TItem, int> getCode,
            Func<TItem, int> getMat1,
            Func<TItem, int> getMat2)
        {
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (jsonAsset == null) return;

            // TList는 { items: [...] } 래퍼가 필요
            string wrapped = "{\"items\":" + jsonAsset.text + "}";
            TList list = JsonUtility.FromJson<TList>(wrapped);

            // 리플렉션 없이 IEnumerable 처리 — 타입별 캐스팅
            System.Collections.IEnumerable items = GetItems(list);
            if (items == null) return;

            foreach (object obj in items)
            {
                TItem src = (TItem)obj;
                int code = getCode(src);
                int mat1 = getMat1(src);
                int mat2 = getMat2(src);

                // 재료가 없으면 레시피 불필요
                if (mat1 == 0) continue;

                if (!allItems.TryGetValue(code, out ItemData resultItem)) continue;

                string assetPath = $"{savePath}/Recipe_{code}.asset";
                RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
                if (recipe == null)
                {
                    recipe = ScriptableObject.CreateInstance<RecipeData>();
                    AssetDatabase.CreateAsset(recipe, assetPath);
                }

                SerializedObject s = new SerializedObject(recipe);
                s.FindProperty("_resultItem").objectReferenceValue = resultItem;
                s.FindProperty("_resultAmount").intValue = 1;

                SerializedProperty ingProp = s.FindProperty("_ingredients");
                ingProp.ClearArray();

                int ingredientCount = mat2 != 0 ? 2 : 1;
                ingProp.arraySize = ingredientCount;

                if (allItems.TryGetValue(mat1, out ItemData mat1Data))
                {
                    SerializedProperty ing0 = ingProp.GetArrayElementAtIndex(0);
                    ing0.FindPropertyRelative("_item").objectReferenceValue = mat1Data;
                    ing0.FindPropertyRelative("_amount").intValue = 1;
                }
                else
                {
                    Debug.LogWarning($"[BserItemImporter] 재료 코드 {mat1} 에 해당하는 ItemData 없음 (레시피 {code})");
                }

                if (mat2 != 0)
                {
                    if (allItems.TryGetValue(mat2, out ItemData mat2Data))
                    {
                        SerializedProperty ing1 = ingProp.GetArrayElementAtIndex(1);
                        ing1.FindPropertyRelative("_item").objectReferenceValue = mat2Data;
                        ing1.FindPropertyRelative("_amount").intValue = 1;
                    }
                    else
                    {
                        Debug.LogWarning($"[BserItemImporter] 재료 코드 {mat2} 에 해당하는 ItemData 없음 (레시피 {code})");
                    }
                }

                s.ApplyModifiedProperties();
            }
        }

        // ── 타입 변환 헬퍼 ───────────────────────────────────────────

        private static WeaponType ParseWeaponType(string raw)
        {
            return raw switch
            {
                "OneHandSword"  => WeaponType.OneHandSword,
                "TwoHandSword"  => WeaponType.TwoHandSword,
                "DualSword"     => WeaponType.DualSword,
                "Hammer"        => WeaponType.Hammer,
                "Axe"           => WeaponType.Axe,
                "Spear"         => WeaponType.Spear,
                "Bat"           => WeaponType.Bat,
                "Whip"          => WeaponType.Whip,
                "Glove"         => WeaponType.Glove,
                "Tonfa"         => WeaponType.Tonfa,
                "HighAngleFire" => WeaponType.HighAngleFire,
                "DirectFire"    => WeaponType.DirectFire,
                "Bow"           => WeaponType.Bow,
                "CrossBow"      => WeaponType.CrossBow,
                "Pistol"        => WeaponType.Pistol,
                "AssaultRifle"  => WeaponType.AssaultRifle,
                "SniperRifle"   => WeaponType.SniperRifle,
                "Nunchaku"      => WeaponType.Nunchaku,
                "Rapier"        => WeaponType.Rapier,
                "Guitar"        => WeaponType.Guitar,
                "Camera"        => WeaponType.Camera,
                "Arcana"        => WeaponType.Arcana,
                "VFArm"         => WeaponType.VFArm,
                _ => WeaponType.None,
            };
        }

        /// <summary>
        /// BSER armorType → ItemType 매핑
        /// Head → Helmet / Chest → Chest / Arm → Arms / Leg → Shoes
        /// </summary>
        private static ItemType ParseArmorType(string raw)
        {
            return raw switch
            {
                "Head"  => ItemType.Helmet,
                "Chest" => ItemType.Chest,
                "Arm"   => ItemType.Arms,
                "Leg"   => ItemType.Shoes,
                _       => ItemType.None,
            };
        }

        /// <summary>
        /// BSER consumableType → ItemType 매핑
        /// Food / SpecialFood → Food
        /// Bounty / GadgetEnergy → Consumable
        /// </summary>
        private static ItemType ParseConsumableType(string raw)
        {
            return raw switch
            {
                "Food"        => ItemType.Food,
                "SpecialFood" => ItemType.Food,
                "Bounty"      => ItemType.Consumable,
                "GadgetEnergy"=> ItemType.Consumable,
                _             => ItemType.Consumable,
            };
        }

        // ── 유틸 ─────────────────────────────────────────────────────

        private static ItemData GetOrCreateItem(int code, string prefix)
        {
            string path = $"{ItemSavePath}/{prefix}_{code}.asset";
            ItemData so = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (so != null) return so;

            so = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// 제네릭 리스트 래퍼에서 items 배열을 꺼냄
        /// </summary>
        private static System.Collections.IEnumerable GetItems<T>(T list)
        {
            System.Reflection.FieldInfo field = typeof(T).GetField("items");
            return field?.GetValue(list) as System.Collections.IEnumerable;
        }
    }
}
