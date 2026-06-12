using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectER.Data;

namespace ProjectER.Editor
{
    /// <summary>
    /// 팬킷 실험체 초상화(Full/Half/Mini) 자동 연결 도구
    /// 메뉴: ProjectER > Link BSER Character Sprites
    ///
    /// 이미지 폴더 규칙:
    ///   Assets/Resources/Image/Character/{팬킷번호}. {영문이름}/02. Default/Full_*.png
    ///                                                                     /Half_*.png
    ///                                                                     /Mini_*.png
    ///   (팬킷 번호 체계는 BSER API의 _bserCode와 일치하지 않는 경우가 있어
    ///    폴더명의 영문 이름과 CharacterData._name으로 매칭함)
    ///
    /// 사전 조건:
    ///   Import BSER Characters 실행 완료
    /// </summary>
    public static class BserCharacterSpriteLinker
    {
        private const string ImageRoot         = "Assets/Resources/Image/Character";
        private const string CharacterSavePath = "Assets/ScriptableObjects/Characters/BSER";
        private const string DefaultSubFolder  = "02. Default";

        // "001. Jackie" → "Jackie"
        private static readonly Regex FolderNamePattern = new Regex(@"^\d+\.\s*(.+)$");

        [MenuItem("ProjectER/Link BSER Character Sprites")]
        public static void LinkSprites() => RunLink(force: false);

        [MenuItem("ProjectER/Link BSER Character Sprites (Force Relink)")]
        public static void LinkSpritesForce() => RunLink(force: true);

        private static void RunLink(bool force)
        {
            if (!AssetDatabase.IsValidFolder(ImageRoot))
            {
                Debug.LogError($"[BserCharacterSpriteLinker] 이미지 폴더 없음: {ImageRoot}");
                return;
            }

            Dictionary<string, CharacterData> charactersByName = LoadCharactersByName();

            int linked = 0, skipped = 0, noFolder = 0, noCharacter = 0;

            foreach (string folderGuid in AssetDatabase.FindAssets("", new[] { ImageRoot }))
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                if (!AssetDatabase.IsValidFolder(folderPath)) continue;

                string folderName = System.IO.Path.GetFileName(folderPath);
                Match match = FolderNamePattern.Match(folderName);
                if (!match.Success) continue;

                string englishName = match.Groups[1].Value;

                if (!charactersByName.TryGetValue(englishName, out CharacterData character))
                {
                    noCharacter++;
                    Debug.LogWarning($"[BserCharacterSpriteLinker] CharacterData 없음: name={englishName} ({folderName})");
                    continue;
                }

                string defaultPath = $"{folderPath}/{DefaultSubFolder}";
                if (!AssetDatabase.IsValidFolder(defaultPath))
                {
                    noFolder++;
                    Debug.LogWarning($"[BserCharacterSpriteLinker] {DefaultSubFolder} 폴더 없음: {folderName}");
                    continue;
                }

                if (!force &&
                    character.PortraitFull != null && character.PortraitHalf != null && character.PortraitMini != null)
                {
                    skipped++;
                    continue;
                }

                Sprite full = FindSprite(defaultPath, "Full_");
                Sprite half = FindSprite(defaultPath, "Half_");
                Sprite mini = FindSprite(defaultPath, "Mini_");

                SerializedObject so = new SerializedObject(character);
                if (full != null) so.FindProperty("_portraitFull").objectReferenceValue = full;
                if (half != null) so.FindProperty("_portraitHalf").objectReferenceValue = half;
                if (mini != null) so.FindProperty("_portraitMini").objectReferenceValue = mini;
                so.ApplyModifiedProperties();

                linked++;
            }

            AssetDatabase.SaveAssets();
            string label = force ? "강제 재연결" : "연결";
            Debug.Log(
                $"[BserCharacterSpriteLinker] {label} 완료 — " +
                $"연결 {linked}  스킵 {skipped}  Default폴더없음 {noFolder}  CharacterData없음 {noCharacter}");
        }

        /// <summary>CharacterData._name(영문 코드명) → CharacterData 매핑</summary>
        private static Dictionary<string, CharacterData> LoadCharactersByName()
        {
            Dictionary<string, CharacterData> result = new(System.StringComparer.OrdinalIgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterSavePath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (character != null && !string.IsNullOrEmpty(character.Name))
                    result[character.Name] = character;
            }

            return result;
        }

        /// <summary>
        /// 지정 폴더에서 파일명이 prefix로 시작하는 첫 텍스처를 Sprite로 임포트 후 반환
        /// </summary>
        private static Sprite FindSprite(string folderPath, string prefix)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileName(path);
                if (!fileName.StartsWith(prefix)) continue;

                EnsureSpriteImport(path);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

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
