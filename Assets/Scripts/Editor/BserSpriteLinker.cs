using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// BSER 아이템 스프라이트 자동 연결 도구
    /// 메뉴: ProjectER > Link BSER Sprites
    ///
    /// 스프라이트 파일 배치 규칙:
    ///   SpritePath/{code}.png  (예: Assets/Resources/Data/BSER/images/item/101201.png)
    ///
    /// BserItemImporter 실행 후 사용
    /// </summary>
    public static class BserSpriteLinker
    {
        // BSER 이미지 리소스를 배치할 경로 — BSER_DATA_CONTEXT.md 기준
        private const string SpritePath = "Assets/Resources/Data/BSER/images/item";

        // 임포트된 BSER ItemData SO 경로
        private const string ItemPath = "Assets/ScriptableObjects/Items/BSER";

        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".tga" };

        [MenuItem("ProjectER/Link BSER Sprites")]
        public static void LinkSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpritePath))
            {
                Debug.LogError(
                    $"[BserSpriteLinker] 스프라이트 폴더 없음: {SpritePath}\n" +
                    $"BSER 이미지 리소스를 해당 경로에 배치한 뒤 다시 실행하세요.");
                return;
            }

            string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemPath });
            if (itemGuids.Length == 0)
            {
                Debug.LogWarning("[BserSpriteLinker] BSER ItemData가 없습니다. Import BSER Items를 먼저 실행하세요.");
                return;
            }

            int linked  = 0;
            int skipped = 0; // 이미 아이콘 있는 경우
            int missing = 0;

            foreach (string guid in itemGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (itemData == null) continue;

                // 이미 아이콘이 연결돼 있으면 스킵
                if (itemData.Icon != null)
                {
                    skipped++;
                    continue;
                }

                // BserCode 우선, 없으면 Id(코드 문자열)로 fallback
                string codeStr = itemData.BserCode != 0
                    ? itemData.BserCode.ToString()
                    : itemData.Id;

                Sprite sprite = FindAndConvertSprite(codeStr);
                if (sprite != null)
                {
                    SerializedObject so = new SerializedObject(itemData);
                    so.FindProperty("_icon").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    linked++;
                }
                else
                {
                    Debug.LogWarning($"[BserSpriteLinker] 스프라이트 없음: {SpritePath}/{codeStr}.*");
                    missing++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[BserSpriteLinker] 완료 — " +
                $"연결 {linked}개 / 스킵(기존 아이콘) {skipped}개 / 누락 {missing}개");
        }

        /// <summary>
        /// 이미 아이콘이 연결된 아이템도 포함해 전체 재연결
        /// 메뉴: ProjectER > Link BSER Sprites (Force Relink)
        /// </summary>
        [MenuItem("ProjectER/Link BSER Sprites (Force Relink)")]
        public static void LinkSpritesForce()
        {
            if (!AssetDatabase.IsValidFolder(SpritePath))
            {
                Debug.LogError(
                    $"[BserSpriteLinker] 스프라이트 폴더 없음: {SpritePath}");
                return;
            }

            string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemPath });
            int linked  = 0;
            int missing = 0;

            foreach (string guid in itemGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (itemData == null) continue;

                string codeStr = itemData.BserCode != 0
                    ? itemData.BserCode.ToString()
                    : itemData.Id;

                Sprite sprite = FindAndConvertSprite(codeStr);
                if (sprite != null)
                {
                    SerializedObject so = new SerializedObject(itemData);
                    so.FindProperty("_icon").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    linked++;
                }
                else
                {
                    missing++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BserSpriteLinker] 강제 재연결 완료 — 연결 {linked}개 / 누락 {missing}개");
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────

        private static Sprite FindAndConvertSprite(string codeStr)
        {
            foreach (string ext in Extensions)
            {
                string path = $"{SpritePath}/{codeStr}{ext}";

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                // Sprite 타입이 아니면 자동 변환 후 재임포트
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType      = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
            }
            return null;
        }
    }
}
