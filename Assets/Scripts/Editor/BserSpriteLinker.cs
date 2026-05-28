using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// BSER 아이템 스프라이트 자동 연결 도구
    /// 메뉴: ProjectER > Link BSER Sprites
    ///
    /// 이미지 폴더 규칙:
    ///   Assets/Resources/Image/Item/**/{넘버링}. {영어이름}.png
    ///
    /// 사전 조건:
    ///   1. Import BSER Items 실행 완료
    ///   2. Assets/Resources/Data/BSER/l10n_English.json 배치 완료
    ///      (bser_api.py --mode l10n 으로 생성 후 복사)
    ///   3. Assets/Resources/Data/BSER/sprite_corrections.json (선택, 팬킷 오타 보정)
    /// </summary>
    public static class BserSpriteLinker
    {
        private const string L10nPath        = "Assets/Resources/Data/BSER/l10n_English.json";
        private const string CorrectionsPath = "Assets/Resources/Data/BSER/sprite_corrections.json";
        private const string ImageRoot       = "Assets/Resources/Image/Item";
        private const string ItemPath        = "Assets/ScriptableObjects/Items/BSER";

        // "001. Kitchen Knife" → "Kitchen Knife"
        private static readonly Regex PrefixPattern = new Regex(@"^\d+\.\s*");
        // Hunter_s Pot → Hunter's Pot (소유격 패턴만)
        private static readonly Regex ApostrophePattern = new Regex(@"(?<=\w)_s(?=\s|$)");
        // <color=yellow>text</color> → text
        private static readonly Regex HtmlTagPattern = new Regex(@"<[^>]+>");
        // JSON {"code": "name"} 파싱
        private static readonly Regex JsonPairPattern = new Regex(@"""(\d+)""\s*:\s*""((?:[^""\\]|\\.)*)""");
        // JSON {"key": "value"} 파싱 (보정 맵용)
        private static readonly Regex JsonStrPairPattern = new Regex(@"""([^""\\]+)""\s*:\s*""([^""\\]+)""");

        // 비아이템 폴더 제외 (스킬 아이콘, 무기 타입 그룹 아이콘)
        private static readonly string[] ExcludedFolders =
        {
            "★Weapon Skill",
            "00. Weapon Group",
        };

        // ── 진입점 ───────────────────────────────────────────────────

        [MenuItem("ProjectER/Link BSER Sprites")]
        public static void LinkSprites() => RunLink(force: false);

        [MenuItem("ProjectER/Link BSER Sprites (Force Relink)")]
        public static void LinkSpritesForce() => RunLink(force: true);

        // ── 메인 로직 ─────────────────────────────────────────────────

        private static void RunLink(bool force)
        {
            // 대소문자 무시 매칭 (Bread In Tears ↔ Bread in Tears)
            Dictionary<string, int> nameToCode = LoadL10nReverseMap();
            if (nameToCode == null) return;

            Dictionary<string, string> corrections = LoadCorrections();

            Dictionary<int, ItemData> codeToItem = LoadItemDataMap();
            if (codeToItem.Count == 0)
            {
                Debug.LogWarning("[BserSpriteLinker] BSER ItemData 없음. Import BSER Items를 먼저 실행하세요.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ImageRoot))
            {
                Debug.LogError($"[BserSpriteLinker] 이미지 폴더 없음: {ImageRoot}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ImageRoot });
            int linked = 0, skipped = 0, missing = 0, noItem = 0;

            foreach (string guid in guids)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(guid);

                bool excluded = false;
                foreach (string folder in ExcludedFolders)
                    if (spritePath.Contains(folder)) { excluded = true; break; }
                if (excluded) continue;

                string rawName   = System.IO.Path.GetFileNameWithoutExtension(spritePath);
                string cleanName = ApostrophePattern.Replace(PrefixPattern.Replace(rawName, ""), "'s");

                if (!TryResolveCode(cleanName, nameToCode, corrections, out int code))
                {
                    missing++;
                    Debug.LogWarning($"[BserSpriteLinker] l10n 매칭 실패: '{cleanName}'  ({spritePath})");
                    continue;
                }

                if (!codeToItem.TryGetValue(code, out ItemData itemData))
                {
                    noItem++;
                    Debug.LogWarning($"[BserSpriteLinker] ItemData 없음: code={code}  name='{cleanName}'");
                    continue;
                }

                if (!force && itemData.Icon != null)
                {
                    skipped++;
                    continue;
                }

                EnsureSpriteImport(spritePath);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) { noItem++; continue; }

                SerializedObject so = new SerializedObject(itemData);
                so.FindProperty("_icon").objectReferenceValue = sprite;
                so.ApplyModifiedProperties();
                linked++;
            }

            AssetDatabase.SaveAssets();
            string label = force ? "강제 재연결" : "연결";
            Debug.Log(
                $"[BserSpriteLinker] {label} 완료 — " +
                $"연결 {linked}  스킵 {skipped}  l10n미매칭 {missing}  ItemData없음 {noItem}");
        }

        // ── 이름 → 코드 해석 (3단계 폴백) ───────────────────────────

        private static bool TryResolveCode(
            string cleanName,
            Dictionary<string, int> nameToCode,
            Dictionary<string, string> corrections,
            out int code)
        {
            // 1단계: 직접 매칭 (대소문자 무시)
            if (nameToCode.TryGetValue(cleanName, out code)) return true;

            // 2단계: _ → 공백 변환 (Long_Sword → Long Sword, 후행 _ 제거)
            string spaceVariant = cleanName.Replace('_', ' ').Trim();
            if (nameToCode.TryGetValue(spaceVariant, out code)) return true;

            // 3단계: 수동 오타 보정 맵 적용
            if (corrections.TryGetValue(cleanName, out string corrected) &&
                nameToCode.TryGetValue(corrected, out code)) return true;

            code = 0;
            return false;
        }

        // ── l10n 역방향 맵 (대소문자 무시) ───────────────────────────

        private static Dictionary<string, int> LoadL10nReverseMap()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(L10nPath);
            if (json == null)
            {
                Debug.LogError(
                    $"[BserSpriteLinker] l10n 파일 없음: {L10nPath}\n" +
                    "bser_api.py --mode l10n 실행 후 해당 경로에 배치하세요.");
                return null;
            }

            // 대소문자 무시 딕셔너리로 빌드
            Dictionary<string, int> result = new(System.StringComparer.OrdinalIgnoreCase);
            foreach (Match m in JsonPairPattern.Matches(json.text))
            {
                int    code = int.Parse(m.Groups[1].Value);
                string name = HtmlTagPattern.Replace(m.Groups[2].Value, "").Trim();
                if (!string.IsNullOrEmpty(name))
                    result[name] = code;
            }

            Debug.Log($"[BserSpriteLinker] l10n 로드 완료 — {result.Count}개");
            return result;
        }

        // ── 오타 보정 맵 로드 ─────────────────────────────────────────

        private static Dictionary<string, string> LoadCorrections()
        {
            Dictionary<string, string> result = new(System.StringComparer.OrdinalIgnoreCase);
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(CorrectionsPath);
            if (json == null) return result;

            foreach (Match m in JsonStrPairPattern.Matches(json.text))
                result[m.Groups[1].Value] = m.Groups[2].Value;

            Debug.Log($"[BserSpriteLinker] 오타 보정 로드 — {result.Count}개");
            return result;
        }

        // ── ItemData 코드 맵 구축 ──────────────────────────────────────

        private static Dictionary<int, ItemData> LoadItemDataMap()
        {
            Dictionary<int, ItemData> map = new();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemPath });
            foreach (string guid in guids)
            {
                string   path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null && item.BserCode != 0)
                    map[item.BserCode] = item;
            }
            return map;
        }

        // ── 텍스처 → Sprite 임포트 보장 ──────────────────────────────

        private static void EnsureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.Sprite) return;

            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }
}
