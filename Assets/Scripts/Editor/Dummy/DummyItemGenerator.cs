using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// 테스트용 더미 아이템 에셋 생성 도구
    /// 메뉴: ProjectER > Generate Dummy Items
    /// </summary>
    public static class DummyItemGenerator
    {
        private const string ItemSavePath = "Assets/ScriptableObjects/Dummy/Items";
        private const string DBSavePath   = "Assets/ScriptableObjects/Dummy";

        [MenuItem("ProjectER/Generate Dummy Items")]
        public static void Generate()
        {
            // ── 재료 5개 ─────────────────────────────────────────────
            CreateItem("mat_wood",        "나무",       ItemType.Material, stackable: true,  maxStack: 20);
            CreateItem("mat_stone",       "돌",         ItemType.Material, stackable: true,  maxStack: 20);
            CreateItem("mat_leather",     "가죽",       ItemType.Material, stackable: true,  maxStack: 20);
            CreateItem("mat_iron_ore",    "철광석",     ItemType.Material, stackable: true,  maxStack: 20);
            CreateItem("mat_fiber",       "섬유",       ItemType.Material, stackable: true,  maxStack: 20);

            // ── 상위 재료 3개 ─────────────────────────────────────────
            CreateItem("mat_iron_ingot",  "철 주괴",    ItemType.Material, stackable: true,  maxStack: 10);
            CreateItem("mat_hard_leather","강화 가죽",  ItemType.Material, stackable: true,  maxStack: 10);
            CreateItem("mat_plank",       "목재 판자",  ItemType.Material, stackable: true,  maxStack: 10);

            // ── 장비 3개 ─────────────────────────────────────────────
            CreateItem("wpn_iron_sword",  "철 검",      ItemType.Weapon,   attackPower: 20f);
            CreateItem("arm_leather_vest","가죽 조끼",  ItemType.Chest,    defense: 10f, maxHpBonus: 30f);
            CreateItem("arm_iron_helmet", "철 투구",    ItemType.Helmet,   defense: 8f,  maxHpBonus: 20f);

            // ── ItemDatabase 생성 / 갱신 ──────────────────────────────
            string dbPath = $"{DBSavePath}/ItemDatabase.asset";
            ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            // DB Items 리스트를 직접 채움 (SerializedObject 사용)
            SerializedObject serializedDB = new SerializedObject(db);
            SerializedProperty itemsProp = serializedDB.FindProperty("_items");
            itemsProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemSavePath });
            itemsProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = item;
            }

            serializedDB.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[DummyItemGenerator] 아이템 {guids.Length}개 생성, ItemDatabase 갱신 완료.");
            EditorUtility.FocusProjectWindow();
        }

        private static void CreateItem(
            string id,
            string displayName,
            ItemType itemType,
            bool stackable      = false,
            int maxStack        = 1,
            float attackPower   = 0f,
            float defense       = 0f,
            float maxHpBonus    = 0f,
            float hpRestore     = 0f)
        {
            string assetPath = $"{ItemSavePath}/{id}.asset";

            // 이미 있으면 덮어쓰지 않음
            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
            {
                Debug.Log($"[DummyItemGenerator] 이미 존재, 스킵: {assetPath}");
                return;
            }

            ItemData data = ScriptableObject.CreateInstance<ItemData>();

            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_id").stringValue           = id;
            so.FindProperty("_displayName").stringValue  = displayName;
            so.FindProperty("_itemType").enumValueIndex  = (int)itemType;
            so.FindProperty("_isStackable").boolValue    = stackable;
            so.FindProperty("_maxStack").intValue        = maxStack;
            so.FindProperty("_attackPower").floatValue   = attackPower;
            so.FindProperty("_defense").floatValue       = defense;
            so.FindProperty("_maxHpBonus").floatValue    = maxHpBonus;
            so.FindProperty("_hpRestore").floatValue     = hpRestore;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(data, assetPath);
        }
    }
}
