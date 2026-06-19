using ProjectER.Character;
using ProjectER.Data;
using ProjectER.Inventory;
using ProjectER.UI;
using ProjectER.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectER.Editor
{
    /// <summary>
    /// 04_InGameScene 그레이박스(바닥 + NavMesh + 플레이어 + 루트박스)를 자동으로 생성하는 에디터 유틸.
    /// 메뉴: ProjectER / Build InGame Scene
    /// </summary>
    public static class InGameSceneBuilder
    {
        private const string ScenePath             = "Assets/Scenes/04_InGameScene.unity";
        private const string PlayerPrefabPath      = "Assets/Prefabs/Characters/PlayerCharacter.prefab";
        private const string TestCharacterDataPath = "Assets/ScriptableObjects/Characters/BSER/Character_1.asset"; // Jackie (이동 테스트용)
        private const float  FloorScale             = 5f; // Plane 기본 10x10 → 50x50

        private const string LootBoxPrefabPath   = "Assets/Prefabs/Items/LootBox.prefab";
        private const string LootBoxUIPrefabPath = "Assets/Prefabs/UI/LootBoxUI.prefab";
        private const string ZoneSpawnPath       = "Assets/ScriptableObjects/ZoneSpawns/BSER";
        private const int    TargetAreaCode      = 10;

        // 서브존은 동일한 풀의 반복 정의이므로 대표로 1001을 사용
        private const int RepresentativeSubZone = 1001;

        // 4개 구역 중심 위치 (50×50 바닥 기준 4분할)
        private static readonly Vector2[] ZoneOffsets =
        {
            new(-12f, -12f), new(-12f, 12f),
            new( 12f, -12f), new( 12f, 12f),
        };

        // 구역별 식별 색상
        private static readonly Color[] ZoneColors =
        {
            new(1.00f, 0.35f, 0.35f), // 빨강 (좌하)
            new(0.35f, 0.90f, 0.35f), // 초록 (좌상)
            new(0.35f, 0.55f, 1.00f), // 파랑 (우하)
            new(1.00f, 0.90f, 0.20f), // 노랑 (우상)
        };

        [MenuItem("ProjectER/Build InGame Scene")]
        public static void Build()
        {
            UnityEngine.SceneManagement.Scene scene = OpenScene();

            BuildFloor();
            BuildPlayer(scene);
            BuildLootZones();
            SetupCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("완료", "04_InGameScene 그레이박스(바닥 + NavMesh + 플레이어 + 루트박스) 생성 완료.", "확인");
        }

        private static UnityEngine.SceneManagement.Scene OpenScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);
            return scene;
        }

        // ── 바닥 + NavMesh ───────────────────────────────────────────

        private static void BuildFloor()
        {
            GameObject existing = GameObject.Find("Floor");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position   = Vector3.zero;
            floor.transform.localScale = new Vector3(FloorScale, 1f, FloorScale);

            NavMeshSurface surface = floor.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }

        // ── 플레이어 ─────────────────────────────────────────────────

        private static void BuildPlayer(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject existing = GameObject.Find("Player");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject prefab = GetOrCreatePlayerPrefab();

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Player";
            instance.transform.position = new Vector3(0f, 1f, 0f);
        }

        private static GameObject GetOrCreatePlayerPrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            bool isExisting = existingPrefab != null;

            // 기존 프리팹은 내용 수정을 위해 임시로 로드, 없으면 새로 생성
            GameObject root = isExisting
                ? PrefabUtility.LoadPrefabContents(PlayerPrefabPath)
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);

            if (!isExisting)
            {
                root.name = "PlayerCharacter";
                root.AddComponent<NavMeshAgent>();
                root.AddComponent<PlayerController>();
            }

            // 이전에 생성된 프리팹이라도 누락된 컴포넌트가 있으면 보강
            if (!root.TryGetComponent<InventorySystem>(out _))
                root.AddComponent<InventorySystem>();

            PlayerController controller = root.GetComponent<PlayerController>();
            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(TestCharacterDataPath);
            if (characterData == null)
                Debug.LogWarning($"[InGameSceneBuilder] 테스트용 CharacterData를 찾을 수 없음: {TestCharacterDataPath}");

            SerializedObject so = new(controller);
            so.FindProperty("_characterData").objectReferenceValue = characterData;
            so.ApplyModifiedProperties();

            EnsureDirectory("Assets/Prefabs/Characters");
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);

            if (isExisting)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);

            return savedPrefab;
        }

        // ── 루트 박스 ────────────────────────────────────────────────

        private static void BuildLootZones()
        {
            GameObject existing = GameObject.Find("LootZones");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject root      = new("LootZones");
            LootBox    boxPrefab = GetOrCreateLootBoxPrefab();

            string        spawnPath = $"{ZoneSpawnPath}/Zone_{TargetAreaCode}_{RepresentativeSubZone}.asset";
            ZoneSpawnData spawnData = AssetDatabase.LoadAssetAtPath<ZoneSpawnData>(spawnPath);
            if (spawnData == null)
                Debug.LogWarning($"[InGameSceneBuilder] {spawnPath} 없음 — 먼저 ProjectER > Import BSER Item Spawns 실행 필요");

            // 4개 구역 × 박스 5개 (= 총 박스 20개)
            for (int i = 0; i < ZoneOffsets.Length; i++)
            {
                Vector2    offset  = ZoneOffsets[i];
                GameObject zoneObj = new($"LootZone_{TargetAreaCode}_{i + 1}");
                zoneObj.transform.SetParent(root.transform);
                zoneObj.transform.position = new Vector3(offset.x, 0f, offset.y);

                LootZone         zone = zoneObj.AddComponent<LootZone>();
                SerializedObject so   = new(zone);
                so.FindProperty("_spawnData").objectReferenceValue = spawnData;
                so.FindProperty("_boxPrefab").objectReferenceValue = boxPrefab;
                so.FindProperty("_zoneColor").colorValue           = ZoneColors[i];
                so.ApplyModifiedProperties();
            }
        }

        private static LootBox GetOrCreateLootBoxPrefab()
        {
            // 상호작용 시 열 LootBoxUI 프리팹 (먼저 Build LootBox UI Prefab 실행 필요)
            LootBoxUI lootBoxUI = AssetDatabase.LoadAssetAtPath<LootBoxUI>(LootBoxUIPrefabPath);
            if (lootBoxUI == null)
                Debug.LogWarning($"[InGameSceneBuilder] {LootBoxUIPrefabPath} 없음 — " +
                                 "먼저 ProjectER > Build LootBox UI Prefab 실행 필요. (UI 없이 콘솔 로그만 출력됨)");

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LootBoxPrefabPath);
            bool isExisting = existingPrefab != null;

            // 기존 프리팹은 참조 보강을 위해 임시 로드, 없으면 새로 생성
            GameObject root = isExisting
                ? PrefabUtility.LoadPrefabContents(LootBoxPrefabPath)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (!isExisting)
            {
                root.name = "LootBox";
                root.AddComponent<LootBox>();
            }

            if (!root.TryGetComponent(out LootBox lootBox))
                lootBox = root.AddComponent<LootBox>();

            // _lootBoxUIPrefab 참조 보강 + 자동 닫힘 거리 설정
            // (닫힘 거리가 상호작용 거리보다 작으면 UI가 열리자마자 닫히므로 넉넉하게)
            SerializedObject so = new(lootBox);
            so.FindProperty("_lootBoxUIPrefab").objectReferenceValue = lootBoxUI;
            so.FindProperty("_closeRange").floatValue = 3f;
            so.ApplyModifiedProperties();

            EnsureDirectory("Assets/Prefabs/Items");
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, LootBoxPrefabPath);

            if (isExisting)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);

            return savedPrefab.GetComponent<LootBox>();
        }

        // ── 카메라 ───────────────────────────────────────────────────

        private static void SetupCamera()
        {
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj == null) return;

            cameraObj.transform.position = new Vector3(0f, 12f, -10f);
            cameraObj.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        }

        // ── 유틸 ─────────────────────────────────────────────────────

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
