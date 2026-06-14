using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// BSER ItemSpawn.json → ZoneSpawnData ScriptableObject 일괄 임포터
    /// 메뉴: ProjectER > Import BSER Item Spawns
    ///
    /// 스코프: 전체 맵 재현은 스코프 아웃이므로 TargetAreaCode 1개 지역만 임포트한다.
    /// </summary>
    public static class BserItemSpawnImporter
    {
        private const string JsonRoot   = "Assets/Resources/Data/BSER";
        private const string SavePath   = "Assets/ScriptableObjects/ZoneSpawns/BSER";
        private const string ItemDbPath = "Assets/ScriptableObjects/ItemDatabase.asset";

        // 그레이박스 인게임 씬 대상 지역 (BSER ItemSpawn.json areaCode 기준)
        private const int TargetAreaCode = 10;

        // ── JSON 파싱용 내부 클래스 ───────────────────────────────────

        [Serializable]
        private class BserItemSpawn
        {
            public int code;
            public int areaCode;
            public int areaSpawnGroup;
            public int itemCode;
            public int dropCount;
        }

        [Serializable]
        private class BserItemSpawnList { public BserItemSpawn[] items; }

        // ── 진입점 ───────────────────────────────────────────────────

        [MenuItem("ProjectER/Import BSER Item Spawns")]
        public static void Import()
        {
            EnsureDirectory(SavePath);

            ItemDatabase itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDbPath);
            if (itemDb == null)
            {
                Debug.LogError("[BserItemSpawnImporter] ItemDatabase.asset 없음 — 먼저 ProjectER > Import BSER Items 실행 필요");
                return;
            }
            itemDb.Initialize();

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/ItemSpawn.json");
            if (json == null)
            {
                Debug.LogError("[BserItemSpawnImporter] ItemSpawn.json 없음");
                return;
            }

            BserItemSpawnList list = JsonUtility.FromJson<BserItemSpawnList>("{\"items\":" + json.text + "}");

            var groups = list.items
                .Where(src => src.areaCode == TargetAreaCode)
                .GroupBy(src => src.areaSpawnGroup)
                .OrderBy(g => g.Key);

            int zoneCount = 0;
            foreach (var group in groups)
            {
                ZoneSpawnData so = GetOrCreateZoneSpawn(TargetAreaCode, group.Key);
                SerializedObject s = new SerializedObject(so);

                s.FindProperty("_areaCode").intValue       = TargetAreaCode;
                s.FindProperty("_areaSpawnGroup").intValue = group.Key;

                SerializedProperty entriesProp = s.FindProperty("_entries");
                entriesProp.ClearArray();

                int index = 0;
                foreach (BserItemSpawn src in group)
                {
                    ItemData item = itemDb.GetByCode(src.itemCode);
                    if (item == null)
                    {
                        Debug.LogWarning($"[BserItemSpawnImporter] itemCode {src.itemCode} 에 해당하는 ItemData 없음 (areaCode {src.areaCode}, group {src.areaSpawnGroup})");
                        continue;
                    }

                    entriesProp.InsertArrayElementAtIndex(index);
                    SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("_item").objectReferenceValue = item;
                    entry.FindPropertyRelative("_dropCount").intValue        = src.dropCount;
                    index++;
                }

                s.ApplyModifiedProperties();
                zoneCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BserItemSpawnImporter] 임포트 완료 — 지역 {TargetAreaCode}, 구역 {zoneCount}개");
            EditorUtility.FocusProjectWindow();
        }

        // ── 유틸 ─────────────────────────────────────────────────────

        private static ZoneSpawnData GetOrCreateZoneSpawn(int areaCode, int areaSpawnGroup)
        {
            string groupLabel = areaSpawnGroup == -1 ? "Common" : areaSpawnGroup.ToString();
            string path = $"{SavePath}/Zone_{areaCode}_{groupLabel}.asset";

            ZoneSpawnData so = AssetDatabase.LoadAssetAtPath<ZoneSpawnData>(path);
            if (so != null) return so;

            so = ScriptableObject.CreateInstance<ZoneSpawnData>();
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
    }
}
