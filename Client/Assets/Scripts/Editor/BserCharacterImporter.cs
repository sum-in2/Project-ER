using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// BSER Open API JSON → CharacterData / WeaponTypeInfoData ScriptableObject 일괄 임포터
    /// 메뉴: ProjectER > Import BSER Characters
    ///
    /// 사전 조건:
    ///   Assets/Resources/Data/BSER/Character.json
    ///   Assets/Resources/Data/BSER/CharacterAttributes.json
    ///   Assets/Resources/Data/BSER/WeaponTypeInfo.json
    ///   Assets/Resources/Data/BSER/l10n_Korean_character.json
    ///     (bser_api.py --target character 로 수집 후 배치)
    /// </summary>
    public static class BserCharacterImporter
    {
        private const string JsonRoot          = "Assets/Resources/Data/BSER";
        private const string CharacterSavePath = "Assets/ScriptableObjects/Characters/BSER";
        private const string WeaponTypeSavePath = "Assets/ScriptableObjects/WeaponTypes/BSER";
        private const string DbSavePath        = "Assets/ScriptableObjects";

        // JSON {"code": "name"} 파싱 (l10n 딕셔너리용)
        private static readonly Regex JsonPairPattern = new Regex(@"""(\d+)""\s*:\s*""((?:[^""\\]|\\.)*)""");

        // ── JSON 파싱용 내부 클래스 ───────────────────────────────────

        [Serializable]
        private class BserCharacter
        {
            public int    code;
            public string name;
            public float  maxHp;
            public float  attackPower;
            public float  defense;
            public float  skillAmp;
            public float  adaptiveForce;
            public float  criticalStrikeChance;
            public float  hpRegen;
            public float  attackSpeed;
            public float  attackSpeedLimit;
            public float  moveSpeed;
            public float  sightRange;
            public string charArcheType1;
            public string charArcheType2;
            public string weaponRangeType;
        }

        [Serializable]
        private class BserCharacterList { public BserCharacter[] items; }

        [Serializable]
        private class BserCharacterAttribute
        {
            public int    characterCode;
            public string mastery;
        }

        [Serializable]
        private class BserCharacterAttributeList { public BserCharacterAttribute[] items; }

        [Serializable]
        private class BserWeaponTypeInfo
        {
            public string type;
            public float  attackSpeed;
            public float  attackRange;
            public int    shopFilter;
        }

        [Serializable]
        private class BserWeaponTypeInfoList { public BserWeaponTypeInfo[] items; }

        // ── 진입점 ───────────────────────────────────────────────────

        [MenuItem("ProjectER/Import BSER Characters")]
        public static void Import()
        {
            EnsureDirectory(CharacterSavePath);
            EnsureDirectory(WeaponTypeSavePath);

            ImportWeaponTypes();
            ImportCharacters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RebuildCharacterDatabase();
            RebuildWeaponTypeDatabase();

            AssetDatabase.SaveAssets();
            Debug.Log("[BserCharacterImporter] 임포트 완료");
            EditorUtility.FocusProjectWindow();
        }

        // ── 실험체 임포트 ────────────────────────────────────────────

        private static void ImportCharacters()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/Character.json");
            if (json == null) { Debug.LogError("[BserCharacterImporter] Character.json 없음"); return; }

            Dictionary<int, string> displayNames = LoadKoreanNames();
            Dictionary<int, List<WeaponType>> masteries = LoadWeaponMasteries();

            BserCharacterList list = JsonUtility.FromJson<BserCharacterList>("{\"items\":" + json.text + "}");
            foreach (BserCharacter src in list.items)
            {
                CharacterData so = GetOrCreateCharacter(src.code);
                SerializedObject s = new SerializedObject(so);

                // BSER 연동
                s.FindProperty("_bserCode").intValue = src.code;

                // 기본 정보
                s.FindProperty("_name").stringValue = src.name;
                s.FindProperty("_displayName").stringValue =
                    displayNames.TryGetValue(src.code, out string displayName) ? displayName : src.name;

                // 분류
                s.FindProperty("_archeType1").enumValueIndex = (int)ParseArcheType(src.charArcheType1);
                s.FindProperty("_archeType2").enumValueIndex = (int)ParseArcheType(src.charArcheType2);
                s.FindProperty("_weaponRangeType").enumValueIndex = (int)ParseWeaponRangeType(src.weaponRangeType);

                SerializedProperty masteryProp = s.FindProperty("_weaponMasteries");
                masteryProp.ClearArray();
                if (masteries.TryGetValue(src.code, out List<WeaponType> weaponTypes))
                {
                    masteryProp.arraySize = weaponTypes.Count;
                    for (int i = 0; i < weaponTypes.Count; i++)
                        masteryProp.GetArrayElementAtIndex(i).enumValueIndex = (int)weaponTypes[i];
                }

                // 전투 스탯
                s.FindProperty("_maxHp").floatValue                = src.maxHp;
                s.FindProperty("_attackPower").floatValue          = src.attackPower;
                s.FindProperty("_defense").floatValue              = src.defense;
                s.FindProperty("_hpRegen").floatValue              = src.hpRegen;
                s.FindProperty("_attackSpeed").floatValue          = src.attackSpeed;
                s.FindProperty("_attackSpeedLimit").floatValue     = src.attackSpeedLimit;
                s.FindProperty("_moveSpeed").floatValue            = src.moveSpeed;
                s.FindProperty("_sightRange").floatValue           = src.sightRange;
                s.FindProperty("_skillAmp").floatValue             = src.skillAmp;
                s.FindProperty("_adaptiveForce").floatValue        = src.adaptiveForce;
                s.FindProperty("_criticalStrikeChance").floatValue = src.criticalStrikeChance;

                s.ApplyModifiedProperties();
            }
            Debug.Log($"[BserCharacterImporter] 실험체 {list.items.Length}개 처리");
        }

        /// <summary>
        /// CharacterAttributes.json → characterCode 별 사용 가능 무기군 목록
        /// (한 실험체가 여러 무기군을 가질 수 있음)
        /// </summary>
        private static Dictionary<int, List<WeaponType>> LoadWeaponMasteries()
        {
            Dictionary<int, List<WeaponType>> result = new();

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/CharacterAttributes.json");
            if (json == null)
            {
                Debug.LogWarning("[BserCharacterImporter] CharacterAttributes.json 없음 — 무기군 정보 생략");
                return result;
            }

            BserCharacterAttributeList list =
                JsonUtility.FromJson<BserCharacterAttributeList>("{\"items\":" + json.text + "}");

            foreach (BserCharacterAttribute src in list.items)
            {
                WeaponType weaponType = ParseWeaponType(src.mastery);
                if (weaponType == WeaponType.None) continue;

                if (!result.TryGetValue(src.characterCode, out List<WeaponType> weaponTypes))
                {
                    weaponTypes = new List<WeaponType>();
                    result[src.characterCode] = weaponTypes;
                }

                if (!weaponTypes.Contains(weaponType))
                    weaponTypes.Add(weaponType);
            }

            return result;
        }

        /// <summary>
        /// l10n_Korean_character.json → {code: 한국어 이름}
        /// </summary>
        private static Dictionary<int, string> LoadKoreanNames()
        {
            Dictionary<int, string> result = new();

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/l10n_Korean_character.json");
            if (json == null)
            {
                Debug.LogWarning("[BserCharacterImporter] l10n_Korean_character.json 없음 — 영문 이름 사용");
                return result;
            }

            foreach (Match m in JsonPairPattern.Matches(json.text))
            {
                int code = int.Parse(m.Groups[1].Value);
                result[code] = m.Groups[2].Value;
            }

            return result;
        }

        // ── 무기군 정보 임포트 ────────────────────────────────────────

        private static void ImportWeaponTypes()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{JsonRoot}/WeaponTypeInfo.json");
            if (json == null) { Debug.LogError("[BserCharacterImporter] WeaponTypeInfo.json 없음"); return; }

            BserWeaponTypeInfoList list = JsonUtility.FromJson<BserWeaponTypeInfoList>("{\"items\":" + json.text + "}");
            foreach (BserWeaponTypeInfo src in list.items)
            {
                WeaponType weaponType = ParseWeaponType(src.type);
                if (weaponType == WeaponType.None)
                {
                    Debug.LogWarning($"[BserCharacterImporter] 알 수 없는 무기군: {src.type}");
                    continue;
                }

                WeaponTypeInfoData so = GetOrCreateWeaponType(weaponType);
                SerializedObject s = new SerializedObject(so);

                s.FindProperty("_weaponType").enumValueIndex = (int)weaponType;
                s.FindProperty("_attackSpeed").floatValue = src.attackSpeed;
                s.FindProperty("_attackRange").floatValue = src.attackRange;
                s.FindProperty("_shopFilter").intValue    = src.shopFilter;

                s.ApplyModifiedProperties();
            }
            Debug.Log($"[BserCharacterImporter] 무기군 {list.items.Length}개 처리");
        }

        // ── DB 재구축 ─────────────────────────────────────────────────

        private static void RebuildCharacterDatabase()
        {
            string dbPath = $"{DbSavePath}/CharacterDatabase.asset";
            CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CharacterDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            SerializedObject s = new SerializedObject(db);
            SerializedProperty itemsProp = s.FindProperty("_characters");
            itemsProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterSavePath });
            itemsProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            }

            s.ApplyModifiedProperties();
            Debug.Log($"[BserCharacterImporter] CharacterDatabase 갱신 — {guids.Length}개");
        }

        private static void RebuildWeaponTypeDatabase()
        {
            string dbPath = $"{DbSavePath}/WeaponTypeDatabase.asset";
            WeaponTypeDatabase db = AssetDatabase.LoadAssetAtPath<WeaponTypeDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<WeaponTypeDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            SerializedObject s = new SerializedObject(db);
            SerializedProperty itemsProp = s.FindProperty("_weaponTypes");
            itemsProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:WeaponTypeInfoData", new[] { WeaponTypeSavePath });
            itemsProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<WeaponTypeInfoData>(path);
            }

            s.ApplyModifiedProperties();
            Debug.Log($"[BserCharacterImporter] WeaponTypeDatabase 갱신 — {guids.Length}개");
        }

        // ── 타입 변환 헬퍼 ───────────────────────────────────────────

        private static CharacterArcheType ParseArcheType(string raw)
        {
            return raw switch
            {
                "Warrior"   => CharacterArcheType.Warrior,
                "Tanker"    => CharacterArcheType.Tanker,
                "Assasin"   => CharacterArcheType.Assasin,
                "Marksman"  => CharacterArcheType.Marksman,
                "Mage"      => CharacterArcheType.Mage,
                "Supporter" => CharacterArcheType.Supporter,
                _           => CharacterArcheType.None,
            };
        }

        private static WeaponRangeType ParseWeaponRangeType(string raw)
        {
            return raw switch
            {
                "Melee" => WeaponRangeType.Melee,
                "Range" => WeaponRangeType.Range,
                "Both"  => WeaponRangeType.Both,
                _       => WeaponRangeType.Melee,
            };
        }

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

        // ── 유틸 ─────────────────────────────────────────────────────

        private static CharacterData GetOrCreateCharacter(int code)
        {
            string path = $"{CharacterSavePath}/Character_{code}.asset";
            CharacterData so = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (so != null) return so;

            so = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static WeaponTypeInfoData GetOrCreateWeaponType(WeaponType weaponType)
        {
            string path = $"{WeaponTypeSavePath}/WeaponType_{weaponType}.asset";
            WeaponTypeInfoData so = AssetDatabase.LoadAssetAtPath<WeaponTypeInfoData>(path);
            if (so != null) return so;

            so = ScriptableObject.CreateInstance<WeaponTypeInfoData>();
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
