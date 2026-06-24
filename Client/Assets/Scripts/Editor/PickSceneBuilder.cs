using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Scene;
using ProjectER.UI.Pick;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectER.Editor
{
    /// <summary>
    /// 03_PickScene UI를 자동으로 생성하는 에디터 유틸.
    /// 메뉴: ProjectER / Build Pick Scene
    /// </summary>
    public static class PickSceneBuilder
    {
        private const string FontPath              = "Fonts & Materials/Pretendard-Medium SDF";
        private const string SlotPrefabPath         = "Assets/Prefabs/UI/CharacterSelectSlot.prefab";
        private const string UIDefaultMaterialPath  = "Assets/Art/Materials/UI_Default.mat";
        private const string CharacterDatabasePath  = "Assets/ScriptableObjects/CharacterDatabase.asset";
        private const string ItemDatabasePath       = "Assets/ScriptableObjects/ItemDatabase.asset";
        private const string GradeConfigPath        = "Assets/ScriptableObjects/ItemGradeColorConfig.asset";
        private const string MatchSelectionPath     = "Assets/ScriptableObjects/MatchSelectionData.asset";
        private const string ScenePath              = "Assets/00_Scenes/03_PickScene.unity";
        private const int    PlayerSlotCount        = 3;

        // CharacterFilterPanelUI.RoleOrder 와 동일한 순서
        private static readonly string[] RoleLabels =
        {
            "전체", "전사", "수호자", "암살자", "사격수", "마법사", "지원가",
        };

        [MenuItem("ProjectER/Build Pick Scene")]
        public static void Build()
        {
            CharacterSelectSlotUI slotPrefab = BuildSlotPrefab();
            BuildScene(slotPrefab);

            EditorUtility.DisplayDialog("완료", "03_PickScene 생성 완료.\nBuild Settings 씬 목록에 자동으로 추가되었습니다.", "확인");
        }

        // ── 슬롯 프리팹 참조만 재연결 (전체 빌드 없이) ──────────────
        // Build Pick Scene 후 _slotPrefab이 {fileID:0}으로 풀렸을 때 1클릭으로 복구.
        [MenuItem("ProjectER/Rewire Pick Scene Slot Prefab")]
        public static void RewireSlotPrefab()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            CharacterGridUI grid = Object.FindFirstObjectByType<CharacterGridUI>();
            if (grid == null)
            {
                Debug.LogError("[PickSceneBuilder] 씬에서 CharacterGridUI를 찾지 못했습니다. 먼저 Build Pick Scene 실행 필요.");
                return;
            }

            CharacterSelectSlotUI slotPrefab = LoadSlotPrefab();
            if (slotPrefab == null)
            {
                Debug.LogError($"[PickSceneBuilder] 슬롯 프리팹 로드 실패: {SlotPrefabPath}");
                return;
            }

            SerializedObject so = new(grid);
            so.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PickSceneBuilder] _slotPrefab 재연결 완료.");
        }

        // 슬롯 프리팹을 경로에서 로드해 컴포넌트를 반환 (없으면 null)
        private static CharacterSelectSlotUI LoadSlotPrefab()
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            return go != null ? go.GetComponent<CharacterSelectSlotUI>() : null;
        }

        // ── 캐릭터 그리드 채우기 (기존 씬의 사용자 조정 크기 반영) ──
        [MenuItem("ProjectER/Populate Pick Scene Character Grid")]
        public static void PopulateCharacterGrid()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject leftPanel = GameObject.Find("LeftPanel");
            Transform gridScroll = leftPanel != null ? leftPanel.transform.Find("CharacterGridScroll") : null;
            Transform content    = gridScroll != null ? gridScroll.Find("Viewport/Content") : null;

            if (content == null)
            {
                Debug.LogError("[PickSceneBuilder] LeftPanel/CharacterGridScroll/Viewport/Content를 찾을 수 없음. 먼저 Build Pick Scene을 실행하세요.");
                return;
            }

            // GridLayoutGroup은 FixedColumnCount = 5로 이미 고정되어 있으므로 cellSize는 기본값 유지
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            float cellWidth  = grid.cellSize.x;
            float cellHeight = grid.cellSize.y;

            // 기존 미리보기 슬롯 제거
            // Inspector가 제거될 슬롯(또는 그 자식)을 선택 중이면
            // SerializedObjectNotCreatableException / MissingReferenceException 발생
            if (Selection.activeGameObject != null && Selection.activeGameObject.transform.IsChildOf(content))
                Selection.activeObject = null;

            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);

            GameObject slotPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            CharacterDatabase characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
            if (slotPrefabAsset == null || characterDatabase == null)
            {
                Debug.LogError("[PickSceneBuilder] 슬롯 프리팹 또는 CharacterDatabase를 찾을 수 없음.");
                return;
            }

            // 한글 오름차순 정렬
            List<CharacterData> characters = new(characterDatabase.Characters);
            characters.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

            foreach (CharacterData character in characters)
            {
                if (character == null) continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefabAsset, content);
                instance.GetComponent<CharacterSelectSlotUI>().Setup(character, null);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PickSceneBuilder] 캐릭터 그리드 채움 완료: {characters.Count}개, 셀 크기 {cellWidth:F1}x{cellHeight:F1}");
        }

        // ── 캐릭터 슬롯 프리팹 ─────────────────────────────────────
        private static CharacterSelectSlotUI BuildSlotPrefab()
        {
            GameObject go = new("CharacterSelectSlot");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(176, 150);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.16f, 0.2f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            GameObject portraitGo = CreatePanel(go.transform, "Portrait", Color.white);
            RectTransform portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0f, 0.22f);
            portraitRt.anchorMax = new Vector2(1f, 1f);
            portraitRt.offsetMin = Vector2.zero;
            portraitRt.offsetMax = Vector2.zero;
            Image portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;

            GameObject nameGo = CreateText(go.transform, "NameText", string.Empty, 14, TextAlignmentOptions.Center);
            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0.22f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            GameObject highlightGo = CreatePanel(go.transform, "SelectedHighlight", new Color(1f, 0.85f, 0.2f, 0.35f));
            Stretch(highlightGo);
            highlightGo.SetActive(false);

            CharacterSelectSlotUI slot = go.AddComponent<CharacterSelectSlotUI>();
            SerializedObject so = new(slot);
            so.FindProperty("_portraitImage").objectReferenceValue     = portraitImage;
            so.FindProperty("_nameText").objectReferenceValue          = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("_selectedHighlight").objectReferenceValue = highlightGo;

            // 기존 프리팹에 인스펙터로 연결해둔 머터리얼이 있다면 재빌드 후에도 유지
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            if (existingPrefab != null && existingPrefab.TryGetComponent(out CharacterSelectSlotUI existingSlot))
            {
                SerializedObject existingSo = new(existingSlot);
                so.FindProperty("_portraitMaterial").objectReferenceValue = existingSo.FindProperty("_portraitMaterial").objectReferenceValue;
                so.FindProperty("_textMaterial").objectReferenceValue     = existingSo.FindProperty("_textMaterial").objectReferenceValue;
            }

            so.ApplyModifiedProperties();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);

            // Inspector가 go(또는 자식)를 선택 중인 상태로 DestroyImmediate하면
            // SerializedObjectNotCreatableException / MissingReferenceException 발생
            if (Selection.activeGameObject == go)
                Selection.activeObject = null;

            Object.DestroyImmediate(go);

            // 방금 생성한 프리팹 에셋을 완전히 등록한 뒤 다시 로드해야
            // 씬에 참조를 할당할 때 {fileID: 0}으로 직렬화되는 문제를 방지함
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath).GetComponent<CharacterSelectSlotUI>();
        }

        // ── 씬 본체 ───────────────────────────────────────────────
        private static void BuildScene(CharacterSelectSlotUI slotPrefab)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AddCamera();
            AddEventSystem();

            GameObject canvasGo = CreateCanvas();

            GameObject leftPanel  = CreateStretchPanel(canvasGo.transform, "LeftPanel", new Color(0.08f, 0.08f, 0.1f, 1f), Vector2.zero, new Vector2(0.62f, 1f));
            GameObject rightPanel = CreateStretchPanel(canvasGo.transform, "RightPanel", new Color(0.05f, 0.05f, 0.07f, 1f), new Vector2(0.62f, 0f), Vector2.one);

            VerticalLayoutGroup leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(16, 16, 16, 16);
            leftLayout.spacing = 12;
            leftLayout.childForceExpandWidth  = true;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childControlWidth      = true;
            leftLayout.childControlHeight     = true;

            VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(16, 16, 16, 16);
            rightLayout.spacing = 12;
            rightLayout.childForceExpandWidth  = true;
            rightLayout.childForceExpandHeight = false;
            rightLayout.childControlWidth      = true;
            rightLayout.childControlHeight     = true;

            // ── 좌측: 필터 바 ──────────────────────────────────
            GameObject filterBar = CreatePanel(leftPanel.transform, "FilterBar", new Color(0.12f, 0.12f, 0.15f, 1f));
            AddLayoutElement(filterBar, preferredHeight: 56, flexibleHeight: 0);
            HorizontalLayoutGroup filterLayout = filterBar.AddComponent<HorizontalLayoutGroup>();
            filterLayout.padding = new RectOffset(8, 8, 8, 8);
            filterLayout.spacing = 6;
            filterLayout.childForceExpandWidth  = false;
            filterLayout.childForceExpandHeight = true;
            filterLayout.childAlignment = TextAnchor.MiddleLeft;

            Button[] roleButtons = new Button[RoleLabels.Length];
            for (int i = 0; i < RoleLabels.Length; i++)
            {
                GameObject btn = CreateButton(filterBar.transform, $"RoleButton_{RoleLabels[i]}", RoleLabels[i]);
                AddLayoutElement(btn, preferredWidth: 64);
                roleButtons[i] = btn.GetComponent<Button>();
            }

            GameObject sortBtn = CreateButton(filterBar.transform, "SortButton", "이름순");
            AddLayoutElement(sortBtn, preferredWidth: 90);

            GameObject searchField = CreateInputField(filterBar.transform, "SearchInput", "실험체 검색");
            AddLayoutElement(searchField, flexibleWidth: 1);

            // ── 좌측: 캐릭터 그리드 ────────────────────────────
            (GameObject gridScrollGo, RectTransform gridContent) = CreateScrollGrid(leftPanel.transform, "CharacterGridScroll", new Vector2(176, 150), new Vector2(8, 8), 5);
            AddLayoutElement(gridScrollGo, flexibleHeight: 1);

            // ── 좌측: 채팅 (구현 보류) ─────────────────────────
            GameObject chatPanel = CreatePanel(leftPanel.transform, "ChatPanel", new Color(0.1f, 0.1f, 0.12f, 1f));
            AddLayoutElement(chatPanel, preferredHeight: 200, flexibleHeight: 0);
            GameObject chatLabel = CreateText(chatPanel.transform, "ChatPlaceholder", "채팅 (구현 보류)", 16, TextAlignmentOptions.Center);
            Stretch(chatLabel);
            chatLabel.GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.4f);

            // ── 좌측 오버레이: 루트 선택 패널 (확인 시 표시, 초기 숨김) ──
            ItemGradeColorConfig gradeConfig = AssetDatabase.LoadAssetAtPath<ItemGradeColorConfig>(GradeConfigPath);
            if (gradeConfig == null)
                Debug.LogWarning($"[PickSceneBuilder] ItemGradeColorConfig 없음: {GradeConfigPath}");
            RouteSelectPanelUI routePanel = BuildRoutePanel(leftPanel.transform, gradeConfig);

            // ── 우측: 타이머 ───────────────────────────────────
            GameObject timerPanel = CreatePanel(rightPanel.transform, "TimerPanel", Color.clear);
            AddLayoutElement(timerPanel, preferredHeight: 64, flexibleHeight: 0);
            GameObject timerText = CreateText(timerPanel.transform, "TimerText", "30", 36, TextAlignmentOptions.Center);
            Stretch(timerText);

            // ── 우측: 선택 실험체 패널 ─────────────────────────
            GameObject selectedPanel = CreatePanel(rightPanel.transform, "SelectedCharacterPanel", new Color(0.1f, 0.1f, 0.13f, 1f));
            AddLayoutElement(selectedPanel, flexibleHeight: 1);

            GameObject portraitGo = CreatePanel(selectedPanel.transform, "Portrait", Color.white);
            RectTransform portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0.1f, 0.12f);
            portraitRt.anchorMax = new Vector2(0.9f, 1f);
            portraitRt.offsetMin = Vector2.zero;
            portraitRt.offsetMax = Vector2.zero;
            Image selectedPortraitImage = portraitGo.GetComponent<Image>();
            selectedPortraitImage.preserveAspect = true;

            GameObject selectedNameGo = CreateText(selectedPanel.transform, "NameText", string.Empty, 28, TextAlignmentOptions.Center);
            RectTransform selectedNameRt = selectedNameGo.GetComponent<RectTransform>();
            selectedNameRt.anchorMin = new Vector2(0f, 0f);
            selectedNameRt.anchorMax = new Vector2(1f, 0.12f);
            selectedNameRt.offsetMin = Vector2.zero;
            selectedNameRt.offsetMax = Vector2.zero;

            // ── 우측: 스킨 목록 (구현 예정) ────────────────────
            GameObject skinPanel = CreatePanel(rightPanel.transform, "SkinListPlaceholder", new Color(0.1f, 0.1f, 0.12f, 1f));
            AddLayoutElement(skinPanel, preferredHeight: 160, flexibleHeight: 0);
            GameObject skinLabel = CreateText(skinPanel.transform, "SkinPlaceholderText", "스킨 (구현 예정)", 16, TextAlignmentOptions.Center);
            Stretch(skinLabel);
            skinLabel.GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.4f);

            // ── 우측: 플레이어 슬롯 3칸 ────────────────────────
            GameObject playerSlotsRow = CreatePanel(rightPanel.transform, "PlayerSlots", Color.clear);
            AddLayoutElement(playerSlotsRow, preferredHeight: 140, flexibleHeight: 0);
            HorizontalLayoutGroup slotsLayout = playerSlotsRow.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.spacing = 10;
            slotsLayout.childForceExpandWidth  = true;
            slotsLayout.childForceExpandHeight = true;

            PlayerSlotUI[] playerSlots = new PlayerSlotUI[PlayerSlotCount];
            for (int i = 0; i < PlayerSlotCount; i++)
                playerSlots[i] = BuildPlayerSlot(playerSlotsRow.transform, $"PlayerSlot_{i}");

            // ── 우측: 액션 버튼 행 [확인][테스트:인게임 진입] ─────
            GameObject actionRow = CreatePanel(rightPanel.transform, "ActionButtons", Color.clear);
            AddLayoutElement(actionRow, preferredHeight: 40, flexibleHeight: 0);
            HorizontalLayoutGroup actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 8;
            actionLayout.childForceExpandWidth  = true;
            actionLayout.childForceExpandHeight = true;

            GameObject confirmButton    = CreateButton(actionRow.transform, "ConfirmButton", "확인");
            GameObject testInGameButton = CreateButton(actionRow.transform, "TestInGameButton", "테스트: 인게임 진입");

            // ── 컴포넌트 연결 ──────────────────────────────────
            CharacterFilterPanelUI filterPanel = leftPanel.AddComponent<CharacterFilterPanelUI>();
            SerializedObject filterSo = new(filterPanel);
            SerializedProperty roleButtonsProp = filterSo.FindProperty("_roleButtons");
            roleButtonsProp.arraySize = roleButtons.Length;
            for (int i = 0; i < roleButtons.Length; i++)
                roleButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = roleButtons[i];
            filterSo.FindProperty("_sortButton").objectReferenceValue  = sortBtn.GetComponent<Button>();
            filterSo.FindProperty("_searchInput").objectReferenceValue = searchField.GetComponent<TMP_InputField>();
            filterSo.ApplyModifiedProperties();

            CharacterDatabase characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
            if (characterDatabase == null)
                Debug.LogWarning($"[PickSceneBuilder] CharacterDatabase 없음: {CharacterDatabasePath}");

            CharacterGridUI characterGrid = leftPanel.AddComponent<CharacterGridUI>();
            SerializedObject gridSo = new(characterGrid);
            gridSo.FindProperty("_characterDatabase").objectReferenceValue = characterDatabase;
            // 슬롯 프리팹은 전달받은 참조 대신 경로에서 새로 로드해 할당
            // ({fileID:0} 직렬화 방지 — 막 생성한 에셋 참조가 씬 저장 시 null로 떨어지던 반복 버그)
            CharacterSelectSlotUI freshSlotPrefab = LoadSlotPrefab() ?? slotPrefab;
            if (freshSlotPrefab == null)
                Debug.LogError($"[PickSceneBuilder] 슬롯 프리팹 로드 실패 — _slotPrefab 미연결: {SlotPrefabPath}");
            gridSo.FindProperty("_slotPrefab").objectReferenceValue  = freshSlotPrefab;
            gridSo.FindProperty("_gridContent").objectReferenceValue = gridContent;
            gridSo.ApplyModifiedProperties();

            SelectedCharacterPanelUI selectedPanelUI = selectedPanel.AddComponent<SelectedCharacterPanelUI>();
            SerializedObject selectedSo = new(selectedPanelUI);
            selectedSo.FindProperty("_portraitImage").objectReferenceValue = selectedPortraitImage;
            selectedSo.FindProperty("_nameText").objectReferenceValue      = selectedNameGo.GetComponent<TMP_Text>();
            selectedSo.ApplyModifiedProperties();

            PickTimerUI timer = timerPanel.AddComponent<PickTimerUI>();
            SerializedObject timerSo = new(timer);
            timerSo.FindProperty("_timeText").objectReferenceValue = timerText.GetComponent<TMP_Text>();
            timerSo.ApplyModifiedProperties();

            GameObject controllerObj = new("PickSceneController");
            PickSceneController controller = controllerObj.AddComponent<PickSceneController>();
            SerializedObject controllerSo = new(controller);
            controllerSo.FindProperty("_characterGrid").objectReferenceValue = characterGrid;
            controllerSo.FindProperty("_filterPanel").objectReferenceValue   = filterPanel;
            controllerSo.FindProperty("_selectedPanel").objectReferenceValue = selectedPanelUI;
            controllerSo.FindProperty("_timer").objectReferenceValue        = timer;
            controllerSo.FindProperty("_testInGameButton").objectReferenceValue = testInGameButton.GetComponent<Button>();
            SerializedProperty playerSlotsProp = controllerSo.FindProperty("_playerSlots");
            playerSlotsProp.arraySize = playerSlots.Length;
            for (int i = 0; i < playerSlots.Length; i++)
                playerSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = playerSlots[i];

            // 루트 선택 단계 배선
            ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            if (itemDatabase == null)
                Debug.LogWarning($"[PickSceneBuilder] ItemDatabase 없음: {ItemDatabasePath}");
            MatchSelectionData matchSelection = LoadOrCreateMatchSelection();

            controllerSo.FindProperty("_confirmButton").objectReferenceValue       = confirmButton.GetComponent<Button>();
            controllerSo.FindProperty("_characterGridScroll").objectReferenceValue = gridScrollGo;
            controllerSo.FindProperty("_routePanel").objectReferenceValue          = routePanel;
            controllerSo.FindProperty("_itemDatabase").objectReferenceValue        = itemDatabase;
            controllerSo.FindProperty("_matchSelection").objectReferenceValue      = matchSelection;
            controllerSo.ApplyModifiedProperties();

            // 저장 전 레이아웃을 강제로 재계산 → FilterBar/CharacterGridScroll 등의
            // RectTransform이 VerticalLayoutGroup 계산 결과로 저장되도록 보장
            LayoutRebuilder.ForceRebuildLayoutImmediate(leftPanel.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(rightPanel.GetComponent<RectTransform>());

            SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        private static PlayerSlotUI BuildPlayerSlot(Transform parent, string name)
        {
            GameObject go = CreatePanel(parent, name, new Color(0.12f, 0.12f, 0.15f, 1f));

            GameObject nameGo = CreateText(go.transform, "NameText", string.Empty, 14, TextAlignmentOptions.Center);
            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.78f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            GameObject iconGo = CreatePanel(go.transform, "CharacterIcon", Color.clear);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.05f);
            iconRt.anchorMax = new Vector2(0.85f, 0.75f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;

            GameObject emptyGo = CreateText(go.transform, "EmptyText", "EMPTY", 14, TextAlignmentOptions.Center);
            Stretch(emptyGo);
            emptyGo.GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.3f);

            PlayerSlotUI slot = go.AddComponent<PlayerSlotUI>();
            SerializedObject so = new(slot);
            so.FindProperty("_playerNameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("_characterIcon").objectReferenceValue  = icon;
            so.FindProperty("_emptyIndicator").objectReferenceValue = emptyGo;
            so.FindProperty("_iconMaterial").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<Material>(UIDefaultMaterialPath);
            so.ApplyModifiedProperties();

            return slot;
        }

        // ── 루트 선택 패널 (좌측 그리드 위 오버레이) ───────────────
        private static RouteSelectPanelUI BuildRoutePanel(Transform parent, ItemGradeColorConfig gradeConfig)
        {
            GameObject panelGo = CreatePanel(parent, "RouteSelectPanel", new Color(0.07f, 0.07f, 0.09f, 1f));
            // 좌측 VerticalLayoutGroup에서 제외하고 좌측 패널 전체를 덮도록 스트레치
            LayoutElement ignore = panelGo.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            Stretch(panelGo);

            GameObject title = CreateText(panelGo.transform, "Title", "루트 선택", 22, TextAlignmentOptions.Center);
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            GameObject scrollGo = CreatePanel(panelGo.transform, "RouteScroll", new Color(0.05f, 0.05f, 0.05f, 0.6f));
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 0.92f);
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -8f);
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical   = true;

            GameObject viewport = CreatePanel(scrollGo.transform, "Viewport", Color.clear);
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUIObject("Content", viewport.transform);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content  = contentRt;

            RouteSelectPanelUI panel = panelGo.AddComponent<RouteSelectPanelUI>();
            panel.Editor_SetReferences(contentRt, gradeConfig);

            panelGo.SetActive(false); // 확인 버튼 클릭 시 컨트롤러가 표시
            return panel;
        }

        private static MatchSelectionData LoadOrCreateMatchSelection()
        {
            MatchSelectionData asset = AssetDatabase.LoadAssetAtPath<MatchSelectionData>(MatchSelectionPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<MatchSelectionData>();
            System.IO.Directory.CreateDirectory("Assets/ScriptableObjects");
            AssetDatabase.CreateAsset(asset, MatchSelectionPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PickSceneBuilder] MatchSelectionData 생성: {MatchSelectionPath}");
            return asset;
        }

        // ── 공통 씬 오브젝트 ─────────────────────────────────────
        private static void AddCamera()
        {
            GameObject camObj = new("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            cam.orthographic    = true;
            camObj.AddComponent<AudioListener>();
        }

        private static void AddEventSystem()
        {
            GameObject esObj = new("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateCanvas()
        {
            GameObject obj = new("Canvas");
            Canvas canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            obj.AddComponent<GraphicRaycaster>();
            return obj;
        }

        // ── UI 생성 유틸 ──────────────────────────────────────────
        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontPath);
            if (font == null)
                Debug.LogWarning($"[PickSceneBuilder] 폰트를 찾을 수 없음: {FontPath}");
            return font;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = NewUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateStretchPanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = CreatePanel(parent, name, color);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = NewUIObject(name, parent);
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.text      = content;
            text.fontSize  = fontSize;
            text.alignment = alignment;
            text.color     = Color.white;

            TMP_FontAsset font = LoadFont();
            if (font != null)
                text.font = font;

            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label)
        {
            GameObject go = CreatePanel(parent, name, new Color(0.25f, 0.25f, 0.3f, 1f));
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            GameObject labelGo = CreateText(go.transform, "Label", label, 18, TextAlignmentOptions.Center);
            Stretch(labelGo);

            return go;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholderText)
        {
            GameObject go = CreatePanel(parent, name, new Color(0.15f, 0.15f, 0.15f, 1f));
            TMP_InputField field = go.AddComponent<TMP_InputField>();

            GameObject textArea = NewUIObject("Text Area", go.transform);
            Stretch(textArea);
            textArea.AddComponent<RectMask2D>();

            GameObject placeholderGo = CreateText(textArea.transform, "Placeholder", placeholderText, 16, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholderGo);
            TMP_Text placeholderTextComponent = placeholderGo.GetComponent<TMP_Text>();
            placeholderTextComponent.color     = new Color(1f, 1f, 1f, 0.5f);
            placeholderTextComponent.fontStyle = FontStyles.Italic;

            GameObject textGo = CreateText(textArea.transform, "Text", string.Empty, 16, TextAlignmentOptions.MidlineLeft);
            Stretch(textGo);

            field.textViewport  = textArea.GetComponent<RectTransform>();
            field.textComponent = textGo.GetComponent<TMP_Text>();
            field.placeholder   = placeholderTextComponent;

            return go;
        }

        private static (GameObject ScrollGo, RectTransform Content) CreateScrollGrid(Transform parent, string name, Vector2 cellSize, Vector2 spacing, int columns)
        {
            GameObject scrollGo = CreatePanel(parent, name, new Color(0.05f, 0.05f, 0.05f, 0.6f));
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            GameObject viewport = CreatePanel(scrollGo.transform, "Viewport", Color.clear);
            Stretch(viewport);
            // 스텐실 기반 Mask는 URP에서 자식의 Stencil Comp:Equal 비교가 실패해 콘텐츠가 보이지 않는 문제가 있음
            // → 클립 사각형 기반 RectMask2D 사용 (스텐실 미사용)
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUIObject("Content", viewport.transform);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize        = cellSize;
            grid.spacing         = spacing;
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.padding         = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;

            return (scrollGo, contentRt);
        }

        private static void AddLayoutElement(GameObject go, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            LayoutElement le = go.AddComponent<LayoutElement>();
            if (preferredWidth  >= 0) le.preferredWidth  = preferredWidth;
            if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
            if (flexibleWidth   >= 0) le.flexibleWidth   = flexibleWidth;
            if (flexibleHeight  >= 0) le.flexibleHeight  = flexibleHeight;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            System.IO.Directory.CreateDirectory("Assets/00_Scenes");
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[PickSceneBuilder] 저장 완료: {path}");
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene existing in scenes)
                if (existing.path == path) return;

            int insertIndex = scenes.Count;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path.Contains("04_InGameScene"))
                {
                    insertIndex = i;
                    break;
                }
            }

            scenes.Insert(insertIndex, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
