using ProjectER.Data;
using ProjectER.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProjectER.Editor
{
    /// <summary>
    /// LootBoxUI 프리팹 자동 생성.
    /// 메뉴: ProjectER / Build LootBox UI Prefab
    /// </summary>
    public static class LootBoxUIBuilder
    {
        private const string PrefabPath        = "Assets/Prefabs/UI/LootBoxUI.prefab";
        private const string GradeConfigPath   = "Assets/ScriptableObjects/ItemGradeColorConfig.asset";
        private const string RecipeDatabasePath = "Assets/ScriptableObjects/RecipeDatabase.asset";
        private const string MatchSelectionPath = "Assets/ScriptableObjects/MatchSelectionData.asset";

        // 슬롯 비율 — 108:64 고정 (LoadOutPanelBuilder와 동일)
        // 크기 지정 시 x(가로)만 정의하고 y = x * (64/108) 으로 자동 계산
        private const float SlotAspectRatio = 108f / 64f;
        private const float SlotX     = 50f;                       // x:50
        private const float SlotWidth  = SlotX;
        private const float SlotHeight = SlotX / SlotAspectRatio;  // ≈ 29.6
        private const float SlotGap    = 4f;
        private const int   Columns    = 4;
        private const int   Rows       = 4;
        private const int   SlotCount  = Columns * Rows;

        // 패널 내부 여백
        private const float PaddingH    = 16f;
        private const float PaddingTop  = 20f;
        private const float PaddingBot  = 16f;
        private const float TitleHeight = 28f;
        private const float TitleGap    = 10f;

        private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelBgColor = new(0.11f, 0.11f, 0.11f, 1f);
        private static readonly Color InnerBgColor = new(0.28f, 0.28f, 0.28f, 1f);

        [MenuItem("ProjectER/Build LootBox UI Prefab")]
        public static void Build()
        {
            ItemGradeColorConfig gradeConfig =
                AssetDatabase.LoadAssetAtPath<ItemGradeColorConfig>(GradeConfigPath);
            if (gradeConfig == null)
                Debug.LogWarning("[LootBoxUIBuilder] ItemGradeColorConfig 없음 — " +
                                 GradeConfigPath);

            // ── 루트: Screen Space Overlay Canvas ────────────────────
            GameObject root  = new("LootBoxUI");
            Canvas     canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // 인게임 UI보다 위

            CanvasScaler scaler       = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // 풀스크린 오버레이 이미지
            Image overlay        = root.AddComponent<Image>();
            overlay.color        = OverlayColor;
            // 레이캐스트 차단 해제 — 박스가 열려도 뒤쪽 HUD(조합칸 등) 클릭이 통과되도록
            overlay.raycastTarget = false;
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin     = Vector2.zero;
            rootRt.anchorMax     = Vector2.one;
            rootRt.offsetMin     = Vector2.zero;
            rootRt.offsetMax     = Vector2.zero;

            // ── 중앙 패널 ─────────────────────────────────────────────
            float gridW = Columns * SlotWidth  + (Columns - 1) * SlotGap;
            float gridH = Rows    * SlotHeight + (Rows    - 1) * SlotGap;
            float panelW = gridW + PaddingH * 2f;
            float panelH = PaddingTop + TitleHeight + TitleGap + gridH + PaddingBot;

            GameObject panel = new("Panel");
            panel.transform.SetParent(root.transform, false);
            Image panelImg   = panel.AddComponent<Image>();
            panelImg.color   = PanelBgColor;
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin  = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax  = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta  = new Vector2(panelW, panelH);
            panelRt.anchoredPosition = Vector2.zero;

            // ── 타이틀 ────────────────────────────────────────────────
            Text title        = CreateChild<Text>(panel.transform, "Title");
            title.text        = "루트박스";
            title.fontSize    = 16;
            title.fontStyle   = FontStyle.Bold;
            title.color       = Color.white;
            title.alignment   = TextAnchor.MiddleLeft;
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin  = new Vector2(0f, 1f);
            titleRt.anchorMax  = new Vector2(1f, 1f);
            titleRt.pivot      = new Vector2(0.5f, 1f);
            titleRt.offsetMin  = new Vector2(PaddingH, -(PaddingTop + TitleHeight));
            titleRt.offsetMax  = new Vector2(-PaddingH, -PaddingTop);

            // ── 그리드 ────────────────────────────────────────────────
            GameObject grid    = new("Grid");
            grid.transform.SetParent(panel.transform, false);
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(SlotWidth, SlotHeight);
            glg.spacing         = new Vector2(SlotGap, SlotGap);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = Columns;
            glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment  = TextAnchor.UpperLeft;

            RectTransform gridRt = grid.GetComponent<RectTransform>();
            gridRt.anchorMin     = new Vector2(0f, 1f);
            gridRt.anchorMax     = new Vector2(1f, 1f);
            gridRt.pivot         = new Vector2(0.5f, 1f);
            float gridTop        = PaddingTop + TitleHeight + TitleGap;
            gridRt.offsetMin     = new Vector2(PaddingH,  -(gridTop + gridH));
            gridRt.offsetMax     = new Vector2(-PaddingH, -gridTop);

            // ── 슬롯 16개 ────────────────────────────────────────────
            LootBoxSlotUI[] slots = new LootBoxSlotUI[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                slots[i] = BuildSlot(grid.transform, i, gradeConfig);

            // ── LootBoxUI 컴포넌트 연결 ───────────────────────────────
            LootBoxUI lootBoxUI = root.AddComponent<LootBoxUI>();
            SerializedObject so  = new(lootBoxUI);
            SerializedProperty slotsProp = so.FindProperty("_slots");
            slotsProp.arraySize = SlotCount;
            for (int i = 0; i < SlotCount; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            // 루트 우선표기 — 레시피 트리 + 선택 루트 캐리어 연결 (없으면 표기만 비활성, 무해)
            so.FindProperty("_recipeDatabase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RecipeDatabase>(RecipeDatabasePath);
            so.FindProperty("_matchSelection").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MatchSelectionData>(MatchSelectionPath);
            // _takeAllButton / _closeButton / _titleText 은 인게임 UI 구성 시 연결
            so.ApplyModifiedProperties();

            root.SetActive(false); // 기본 비활성 — LootBox.Interact() 시 활성화

            // ── 프리팹 저장 ───────────────────────────────────────────
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[LootBoxUIBuilder] 프리팹 저장 완료: {PrefabPath}");
            EditorUtility.DisplayDialog("완료", $"LootBoxUI 프리팹 생성 완료.\n{PrefabPath}", "확인");
        }

        private static LootBoxSlotUI BuildSlot(Transform parent, int index, ItemGradeColorConfig gradeConfig)
        {
            // 슬롯 루트 — 외곽 Image가 등급 테두리
            GameObject go   = new($"LootBoxSlot_{index}");
            go.transform.SetParent(parent, false);
            Image borderImg = go.AddComponent<Image>();
            borderImg.color = Color.clear;

            Button     btn = go.AddComponent<Button>();
            ColorBlock cb  = btn.colors;
            cb.highlightedColor = new Color(0.45f, 0.55f, 0.65f);
            cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
            btn.colors          = cb;

            // 등급 테두리 3px 안쪽 배경
            GameObject innerBgGo = new("InnerBg");
            innerBgGo.transform.SetParent(go.transform, false);
            innerBgGo.AddComponent<Image>().color = InnerBgColor;
            RectTransform innerRt = innerBgGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3f, 3f);
            innerRt.offsetMax = new Vector2(-3f, -3f);

            // 아이콘 — 슬롯 중앙 고정, 스트레치 없음
            // 슬롯 높이 기준 정사각형으로 크기를 고정하고 preserveAspect로 원본 비율 유지
            Image         icon   = CreateChild<Image>(go.transform, "Icon");
            icon.enabled         = false;
            icon.preserveAspect  = true;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
            iconRt.pivot            = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta        = new Vector2(SlotHeight, SlotHeight);
            iconRt.anchoredPosition = Vector2.zero;

            // 수량 텍스트 — 우하단
            Text          count = CreateChild<Text>(go.transform, "Count");
            count.alignment     = TextAnchor.LowerRight;
            count.fontSize      = 11;
            count.fontStyle     = FontStyle.Bold;
            count.color         = Color.white;
            count.enabled       = false;
            RectTransform cntRt = count.GetComponent<RectTransform>();
            cntRt.anchorMin     = Vector2.zero;
            cntRt.anchorMax     = Vector2.one;
            cntRt.offsetMin     = Vector2.zero;
            cntRt.offsetMax     = new Vector2(-2f, -2f);

            LootBoxSlotUI slotUI = go.AddComponent<LootBoxSlotUI>();
            SerializedObject so  = new(slotUI);
            so.FindProperty("_iconImage").objectReferenceValue        = icon;
            so.FindProperty("_countText").objectReferenceValue        = count;
            so.FindProperty("_gradeBorderImage").objectReferenceValue = borderImg;
            so.FindProperty("_gradeConfig").objectReferenceValue      = gradeConfig;
            so.ApplyModifiedProperties();

            return slotUI;
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
