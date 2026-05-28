using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// 더미 스프라이트 자동 연결
    /// 메뉴: ProjectER > Link Dummy Sprites
    /// 규칙: Dummy/Sprites/Items/{itemId}.png → ItemData._icon
    /// Texture2D로 임포트된 이미지도 Sprite 타입으로 자동 변환
    /// </summary>
    public static class DummySpriteLinker
    {
        private const string SpritePath = "Assets/Dummy/Sprites/Items";
        private const string ItemPath   = "Assets/ScriptableObjects/Dummy/Items";

        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

        [MenuItem("ProjectER/Link Dummy Sprites")]
        public static void LinkSprites()
        {
            string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemPath });
            int linked  = 0;
            int missing = 0;

            foreach (string guid in itemGuids)
            {
                string assetPath  = AssetDatabase.GUIDToAssetPath(guid);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (itemData == null) continue;

                Sprite sprite = FindAndConvertSprite(itemData.Id);
                if (sprite != null)
                {
                    SerializedObject so = new(itemData);
                    so.FindProperty("_icon").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    linked++;
                }
                else
                {
                    Debug.LogWarning($"[DummySpriteLinker] 스프라이트 없음: {SpritePath}/{itemData.Id}.*");
                    missing++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DummySpriteLinker] 완료 — 연결 {linked}개 / 누락 {missing}개");
        }

        private static Sprite FindAndConvertSprite(string itemId)
        {
            foreach (string ext in Extensions)
            {
                string path = $"{SpritePath}/{itemId}{ext}";

                // Texture2D로 먼저 시도 — 임포트 타입 무관하게 파일 존재 확인 가능
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                // Texture2D 타입이면 Sprite로 변환 후 재임포트
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
