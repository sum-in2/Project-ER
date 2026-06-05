using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using ProjectER.UI;
using Object = UnityEngine.Object;

namespace ProjectER.Editor
{
    /// <summary>
    /// 인벤토리 + 크래프팅 테스트 UI 자동 생성 (1920×1080 풀스크린 3패널)
    /// 메뉴: ProjectER > Build Inventory Test UI
    /// 실행 전 Import BSER Items 먼저 실행 필요
    /// </summary>
    public static class InventoryTestUIBuilder
    {
        private static readonly Color BgColor        = new Color(0.09f, 0.09f, 0.09f, 1f);
        private static readonly Color PanelBg        = new Color(0.13f, 0.13f, 0.13f, 1f);
        private static readonly Color SectionBg      = new Color(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color SlotColor      = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color EquipSlotColor = new Color(0.22f, 0.28f, 0.35f, 1f);
        private static readonly Color DividerColor   = new Color(0.06f, 0.06f, 0.06f, 1f);

        private const float LeftPanelWidth  = 440f;
        private const float RightPanelWidth = 380f;

        private const string GradeConfigPath = "Assets/ScriptableObjects/ItemGradeColorConfig.asset";

        [MenuItem("ProjectER/Build Inventory Test UI")]
        public static void Build()
        {
            ItemGradeColorConfig gradeConfig = AssetDatabase.LoadAssetAtPath<ItemGradeColorConfig>(GradeConfigPath);
            if (gradeConfig == null)
                Debug.LogWarning("[InventoryTestUIBuilder] ItemGradeColorConfig 없음 — " +
                                 "Create > ProjectER/Config/ItemGradeColor 로 생성 후 다시 실행하세요.");

            // ── 플레이어 ────────────────────────────────────────────
            GameObject      player    = new("Player");
            InventorySystem inventory = player.AddComponent<InventorySystem>();
            CraftingSystem  crafting  = player.AddComponent<CraftingSystem>();

            // ── 캔버스 ──────────────────────────────────────────────
            Canvas canvas = BuildCanvas();

            // ── 루트 (전체 화면) ────────────────────────────────────
            GameObject    root   = new("InventoryTestRoot");
            root.transform.SetParent(canvas.transform, false);
            root.AddComponent<Image>().color = BgColor;
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin     = Vector2.zero;
            rootRt.anchorMax     = Vector2.one;
            rootRt.offsetMin     = Vector2.zero;
            rootRt.offsetMax     = Vector2.zero;

            HorizontalLayoutGroup rootHL  = root.AddComponent<HorizontalLayoutGroup>();
            rootHL.spacing                = 2f;
            rootHL.padding                = new RectOffset(0, 0, 0, 0);
            rootHL.childControlWidth      = true;
            rootHL.childControlHeight     = true;
            rootHL.childForceExpandWidth  = false;
            rootHL.childForceExpandHeight = true;

            // ── 좌측 패널 (인벤토리 + 장비 + 스탯) ─────────────────
            (InventorySlotUI[] bagSlots, InventorySlotUI[] equipSlots, Transform statContent)
                = BuildLeftPanel(root.transform);

            // ── 중앙 패널 (아이템 브라우저) ─────────────────────────
            (Transform itemButtonContainer, Transform filterContainer, KoreanInputFieldAdapter searchAdapter, Transform specialMaterialContainer, Transform statFilterContainer)
                = BuildCenterPanel(root.transform);

            // ── 우측 패널 (조합 + 목표 + 획득버튼) ──────────────────
            (Transform craftContent, CraftingUI craftingUI, Button acquireButton, Button addRouteButton, TargetItemPanelUI targetPanel)
                = BuildRightPanel(root.transform);

            // ── 컴포넌트 부착 ────────────────────────────────────────
            InventoryUI        inventoryUI = root.AddComponent<InventoryUI>();
            InventoryTestPanel testPanel   = root.AddComponent<InventoryTestPanel>();

            // ── 레퍼런스 연결 ────────────────────────────────────────
            WireInventoryUI(inventoryUI, inventory, bagSlots, equipSlots);
            WireTestPanel(testPanel, inventory, itemButtonContainer, statContent, filterContainer,
                acquireButton, addRouteButton, targetPanel,
                searchAdapter, specialMaterialContainer, statFilterContainer);
            WireCraftingSystem(crafting, inventory);
            WireCraftingUI(craftingUI, crafting, inventory, craftContent, gradeConfig);
            WireGradeConfigToSlots(root, gradeConfig);

            Debug.Log("[InventoryTestUIBuilder] 완료.");
            Selection.activeGameObject = root;
        }

        // ── 좌측 패널 ───────────────────────────────────────────────

        private static (InventorySlotUI[] bagSlots, InventorySlotUI[] equipSlots, Transform statContent)
            BuildLeftPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "LeftPanel", PanelBg);
            AddLayoutElement(panel, preferredWidth: LeftPanelWidth, flexibleWidth: 0f);

            VerticalLayoutGroup vl    = panel.AddComponent<VerticalLayoutGroup>();
            vl.spacing                = 2f;
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            // 가방 (5열 2행 — 셀 72px, 상단 타이틀 30px 포함)
            GameObject bagSec          = CreateSection(panel.transform, "BagSection", SectionBg, "가방");
            AddLayoutElement(bagSec, preferredHeight: 210f, flexibleHeight: 0f);
            GameObject bagGrid         = CreateBagGrid(bagSec.transform);
            InventorySlotUI[] bagSlots = new InventorySlotUI[10];
            for (int i = 0; i < 10; i++)
                bagSlots[i] = BuildSlot(bagGrid.transform, $"BagSlot_{i}", SlotColor);

            CreateDivider(panel.transform);

            // 장착 장비 (1열 5행 — 행 높이 62px)
            GameObject equipSec = CreateSection(panel.transform, "EquipSection", SectionBg, "장착 장비");
            AddLayoutElement(equipSec, preferredHeight: 400f, flexibleHeight: 0f);
            VerticalLayoutGroup equipVL    = equipSec.AddComponent<VerticalLayoutGroup>();
            equipVL.spacing                = 8f;
            equipVL.padding                = new RectOffset(8, 8, 30, 8);
            equipVL.childForceExpandWidth  = true;
            equipVL.childForceExpandHeight = false;
            equipVL.childControlWidth      = true;
            equipVL.childControlHeight     = false;
            string[] equipLabels           = { "무기", "옷", "머리", "팔", "신발" };
            InventorySlotUI[] equipSlots   = new InventorySlotUI[5];
            for (int i = 0; i < 5; i++)
                equipSlots[i] = BuildEquipRow(equipSec.transform, equipLabels[i], EquipSlotColor);

            CreateDivider(panel.transform);

            // 장착 스탯 (남은 공간 전부)
            // InventoryTestPanel.BuildStatPanel()이 런타임에 GridLayoutGroup + Text를 추가함
            GameObject statSec = CreateSection(panel.transform, "StatSection", SectionBg, "장착 스탯");
            AddLayoutElement(statSec, flexibleHeight: 1f);
            GameObject statContent     = new("StatContent");
            statContent.transform.SetParent(statSec.transform, false);
            statContent.AddComponent<RectTransform>();
            RectTransform statRt       = statContent.GetComponent<RectTransform>();
            statRt.anchorMin           = new Vector2(0f, 0f);
            statRt.anchorMax           = new Vector2(1f, 1f);
            statRt.offsetMin           = new Vector2(8f,  8f);
            statRt.offsetMax           = new Vector2(-8f, -30f);

            return (bagSlots, equipSlots, statContent.transform);
        }

        // ── 중앙 패널 ───────────────────────────────────────────────

        private static (Transform itemContent, Transform filterContainer, KoreanInputFieldAdapter searchAdapter, Transform specialMaterialContainer, Transform statFilterContainer)
            BuildCenterPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "CenterPanel", PanelBg);
            AddLayoutElement(panel, flexibleWidth: 1f);

            VerticalLayoutGroup vl    = panel.AddComponent<VerticalLayoutGroup>();
            vl.spacing                = 2f;
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            // 상단 필터 바 (2행: 이름검색+전설재료 | 스탯필터)
            GameObject filterBar = CreatePanel(panel.transform, "FilterBar", SectionBg);
            AddLayoutElement(filterBar, preferredHeight: 84f, flexibleHeight: 0f);

            VerticalLayoutGroup filterBarVL   = filterBar.AddComponent<VerticalLayoutGroup>();
            filterBarVL.spacing               = 4f;
            filterBarVL.padding               = new RectOffset(4, 4, 4, 4);
            filterBarVL.childControlWidth     = true;
            filterBarVL.childControlHeight    = false;
            filterBarVL.childForceExpandWidth = true;
            filterBarVL.childForceExpandHeight = false;

            // 1행: 이름 검색 InputField + 전설재료 필터 버튼
            GameObject row1 = new("FilterRow1");
            row1.transform.SetParent(filterBar.transform, false);
            row1.AddComponent<RectTransform>();
            AddLayoutElement(row1, preferredHeight: 36f);
            HorizontalLayoutGroup row1HL   = row1.AddComponent<HorizontalLayoutGroup>();
            row1HL.spacing                 = 4f;
            row1HL.childControlWidth       = true;
            row1HL.childControlHeight      = true;
            row1HL.childForceExpandHeight  = true;
            row1HL.childForceExpandWidth   = false;

            KoreanInputFieldAdapter searchAdapter       = BuildSearchField(row1.transform);
            Transform               specialMaterialContainer = BuildSpecialMaterialContainer(row1.transform);

            // 2행: 스탯 필터 버튼 (런타임에 InventoryTestPanel.BuildStatFilterButtons()가 채움)
            GameObject row2 = new("FilterRow2");
            row2.transform.SetParent(filterBar.transform, false);
            row2.AddComponent<RectTransform>();
            AddLayoutElement(row2, preferredHeight: 36f);
            Transform statFilterContainer = row2.transform;

            CreateDivider(panel.transform);

            // 메인 영역 (타입 필터 1열 + 아이템 그리드)
            GameObject mainArea = CreatePanel(panel.transform, "MainArea", BgColor);
            AddLayoutElement(mainArea, flexibleHeight: 1f);
            HorizontalLayoutGroup mainHL   = mainArea.AddComponent<HorizontalLayoutGroup>();
            mainHL.spacing                 = 2f;
            mainHL.childControlWidth       = true;
            mainHL.childControlHeight      = true;
            mainHL.childForceExpandWidth   = false;
            mainHL.childForceExpandHeight  = true;

            // 장비 구분 필터 열 (런타임에 InventoryTestPanel.BuildFilterButtons()가 채움)
            GameObject typeFilter = CreatePanel(mainArea.transform, "TypeFilter", SectionBg);
            AddLayoutElement(typeFilter, preferredWidth: 60f, flexibleWidth: 0f);

            // 아이템 그리드 (스크롤)
            GameObject gridArea = CreatePanel(mainArea.transform, "ItemGridArea", BgColor);
            AddLayoutElement(gridArea, flexibleWidth: 1f);
            Transform itemContent = BuildItemScrollView(gridArea.transform);

            return (itemContent, typeFilter.transform, searchAdapter, specialMaterialContainer, statFilterContainer);
        }

        private static KoreanInputFieldAdapter BuildSearchField(Transform parent)
        {
            GameObject go = new("SearchField");
            go.transform.SetParent(parent, false);
            AddLayoutElement(go, flexibleWidth: 1f);
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Placeholder 텍스트
            GameObject    phGo  = new("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            Text          phTxt = phGo.AddComponent<Text>();
            phTxt.text          = "아이템 검색...";
            phTxt.font          = builtinFont;
            phTxt.fontSize      = 13;
            phTxt.color         = new Color(0.40f, 0.40f, 0.40f);
            phTxt.alignment     = TextAnchor.MiddleLeft;
            RectTransform phRt  = phGo.GetComponent<RectTransform>();
            phRt.anchorMin      = Vector2.zero;
            phRt.anchorMax      = Vector2.one;
            phRt.offsetMin      = new Vector2(6f, 0f);
            phRt.offsetMax      = new Vector2(-6f, 0f);

            // 입력 텍스트
            GameObject    inGo  = new("Text");
            inGo.transform.SetParent(go.transform, false);
            Text          inTxt = inGo.AddComponent<Text>();
            inTxt.font          = builtinFont;
            inTxt.fontSize      = 13;
            inTxt.color         = Color.white;
            inTxt.alignment     = TextAnchor.MiddleLeft;
            RectTransform inRt  = inGo.GetComponent<RectTransform>();
            inRt.anchorMin      = Vector2.zero;
            inRt.anchorMax      = Vector2.one;
            inRt.offsetMin      = new Vector2(6f, 0f);
            inRt.offsetMax      = new Vector2(-6f, 0f);

            InputField input     = go.AddComponent<InputField>();
            input.textComponent  = inTxt;
            input.placeholder    = phTxt;

            // IME 실시간 이벤트 어댑터 — InputField와 같은 GO에 부착
            KoreanInputFieldAdapter adapter = go.AddComponent<KoreanInputFieldAdapter>();
            return adapter;
        }

        private static Transform BuildSpecialMaterialContainer(Transform parent)
        {
            // 특수 재료 필터 버튼 5개를 담을 컨테이너
            // 런타임에 InventoryTestPanel.BuildSpecialMaterialButtons()가 버튼을 채움
            // 예상 너비: 생(28)+운(28)+미(28)+포(28)+VF(32) + spacing*4 = 156px
            GameObject go = new("SpecialMaterialContainer");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            AddLayoutElement(go, preferredWidth: 160f, flexibleWidth: 0f);
            return go.transform;
        }

        // ── 우측 패널 ───────────────────────────────────────────────

        private static (Transform craftContent, CraftingUI craftingUI, Button acquireButton, Button addRouteButton, TargetItemPanelUI targetPanel) BuildRightPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "RightPanel", PanelBg);
            AddLayoutElement(panel, preferredWidth: RightPanelWidth, flexibleWidth: 0f);

            VerticalLayoutGroup vl    = panel.AddComponent<VerticalLayoutGroup>();
            vl.spacing                = 2f;
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            // 조합 가능 리스트 (스크롤)
            GameObject craftSec   = CreateSection(panel.transform, "CraftingSection", SectionBg, "조합 가능");
            AddLayoutElement(craftSec, preferredHeight: 200f, flexibleHeight: 0f);
            Transform  craftContent = BuildCraftScrollView(craftSec.transform);
            CraftingUI craftingUI   = craftSec.AddComponent<CraftingUI>();

            CreateDivider(panel.transform);

            // 목표 아이템 (부위별 5슬롯 — 1행)
            GameObject targetSec = CreateSection(panel.transform, "TargetSection", SectionBg, "목표 아이템");
            AddLayoutElement(targetSec, preferredHeight: 120f, flexibleHeight: 0f);
            HorizontalLayoutGroup targetHL    = targetSec.AddComponent<HorizontalLayoutGroup>();
            targetHL.spacing                  = 6f;
            targetHL.padding                  = new RectOffset(8, 8, 30, 8);
            targetHL.childForceExpandWidth    = true;
            targetHL.childForceExpandHeight   = true;
            targetHL.childControlWidth        = true;
            targetHL.childControlHeight       = true;

            EquipmentSlotType[] slotTypes  = { EquipmentSlotType.Weapon, EquipmentSlotType.Chest, EquipmentSlotType.Helmet, EquipmentSlotType.Arms, EquipmentSlotType.Shoes };
            string[]            slotLabels = { "무기", "옷", "머리", "팔", "다리" };
            TargetItemSlotUI[]  targetSlots = new TargetItemSlotUI[5];
            for (int i = 0; i < 5; i++)
                targetSlots[i] = BuildTargetSlot(targetSec.transform, slotLabels[i], slotTypes[i]);

            TargetItemPanelUI targetPanel = targetSec.AddComponent<TargetItemPanelUI>();
            SerializedObject  targetSO    = new SerializedObject(targetPanel);
            SetArray(targetSO, "_slots", targetSlots);
            targetSO.ApplyModifiedProperties();

            // 스페이서
            GameObject spacer = new("Spacer");
            spacer.transform.SetParent(panel.transform, false);
            spacer.AddComponent<RectTransform>();
            AddLayoutElement(spacer, flexibleHeight: 1f);

            // 버튼 행 — 획득 / 루트 추가
            GameObject btnRow = new("ButtonRow");
            btnRow.transform.SetParent(panel.transform, false);
            btnRow.AddComponent<RectTransform>();
            AddLayoutElement(btnRow, preferredHeight: 52f, flexibleHeight: 0f);
            HorizontalLayoutGroup btnHL    = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHL.spacing                  = 6f;
            btnHL.padding                  = new RectOffset(6, 6, 6, 6);
            btnHL.childForceExpandWidth    = true;
            btnHL.childForceExpandHeight   = true;
            btnHL.childControlWidth        = true;
            btnHL.childControlHeight       = true;

            Button acquireButton   = BuildActionButton(btnRow.transform, "AcquireButton",   "획득",      new Color(0.18f, 0.42f, 0.18f));
            Button addRouteButton  = BuildActionButton(btnRow.transform, "AddRouteButton",  "루트 추가", new Color(0.18f, 0.28f, 0.45f));

            return (craftContent, craftingUI, acquireButton, addRouteButton, targetPanel);
        }

        // ── 캔버스 ──────────────────────────────────────────────────

        private static Canvas BuildCanvas()
        {
            GameObject go         = new("Canvas");
            Canvas     canvas     = go.AddComponent<Canvas>();
            canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler   = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode    = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esGo = new("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }

        // ── 공용 헬퍼 ───────────────────────────────────────────────

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateSection(Transform parent, string name, Color color, string title)
        {
            GameObject go = CreatePanel(parent, name, color);

            GameObject titleGo  = new("Title");
            titleGo.transform.SetParent(go.transform, false);
            Text t              = titleGo.AddComponent<Text>();
            t.text              = title;
            t.fontSize          = 13;
            t.fontStyle         = FontStyle.Bold;
            t.color             = new Color(0.80f, 0.80f, 0.80f);
            t.alignment         = TextAnchor.MiddleLeft;
            RectTransform trt   = titleGo.GetComponent<RectTransform>();
            trt.anchorMin       = new Vector2(0f, 1f);
            trt.anchorMax       = new Vector2(1f, 1f);
            trt.pivot           = new Vector2(0.5f, 1f);
            trt.sizeDelta       = new Vector2(0f, 26f);
            trt.anchoredPosition = new Vector2(8f, 0f);

            return go;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject go = new("Divider");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = DividerColor;
            AddLayoutElement(go, preferredHeight: 2f, flexibleHeight: 0f);
        }

        private static void AddLayoutElement(GameObject go,
            float preferredWidth  = -1f, float preferredHeight = -1f,
            float flexibleWidth   = -1f, float flexibleHeight  = -1f)
        {
            LayoutElement le = go.AddComponent<LayoutElement>();
            if (preferredWidth  >= 0f) le.preferredWidth  = preferredWidth;
            if (preferredHeight >= 0f) le.preferredHeight = preferredHeight;
            if (flexibleWidth   >= 0f) le.flexibleWidth   = flexibleWidth;
            if (flexibleHeight  >= 0f) le.flexibleHeight  = flexibleHeight;
        }

        private static void StretchFill(RectTransform rt, float left, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(0f,   top);
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        // ── 가방 그리드 ─────────────────────────────────────────────

        private static GameObject CreateBagGrid(Transform parent)
        {
            GameObject   go   = new("BagGrid");
            go.transform.SetParent(parent, false);
            GridLayoutGroup grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(72f, 72f);
            grid.spacing         = new Vector2(6f,  6f);
            grid.padding         = new RectOffset(8, 8, 30, 8);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            RectTransform rt     = go.GetComponent<RectTransform>();
            rt.anchorMin         = Vector2.zero;
            rt.anchorMax         = Vector2.one;
            rt.offsetMin         = Vector2.zero;
            rt.offsetMax         = Vector2.zero;
            return go;
        }

        // ── 슬롯 ────────────────────────────────────────────────────

        private static InventorySlotUI BuildSlot(Transform parent, string name, Color bgColor)
        {
            // 슬롯 GO의 Image가 등급 테두리로 사용됨 — 아이템 없을 때 투명
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            Image borderImage   = go.AddComponent<Image>();
            borderImage.color   = Color.clear;

            Button     btn = go.AddComponent<Button>();
            ColorBlock cb  = btn.colors;
            cb.highlightedColor = new Color(0.45f, 0.55f, 0.65f);
            cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
            btn.colors          = cb;

            // 등급 테두리 3px 안쪽 배경
            GameObject innerBgGo     = new("InnerBg");
            innerBgGo.transform.SetParent(go.transform, false);
            innerBgGo.AddComponent<Image>().color = bgColor;
            RectTransform innerRt    = innerBgGo.GetComponent<RectTransform>();
            innerRt.anchorMin        = Vector2.zero;
            innerRt.anchorMax        = Vector2.one;
            innerRt.offsetMin        = new Vector2(3f, 3f);
            innerRt.offsetMax        = new Vector2(-3f, -3f);

            Image         icon   = CreateChild<Image>(go.transform, "Icon");
            icon.enabled         = false;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            Text          amount = CreateChild<Text>(go.transform, "Amount");
            amount.alignment     = TextAnchor.LowerRight;
            amount.fontSize      = 11;
            amount.fontStyle     = FontStyle.Bold;
            amount.color         = Color.white;
            amount.enabled       = false;
            RectTransform amt    = amount.GetComponent<RectTransform>();
            amt.anchorMin        = Vector2.zero;
            amt.anchorMax        = Vector2.one;
            amt.offsetMin        = Vector2.zero;
            amt.offsetMax        = new Vector2(-2f, -2f);

            InventorySlotUI slotUI = go.AddComponent<InventorySlotUI>();
            SerializedObject so    = new SerializedObject(slotUI);
            so.FindProperty("_iconImage").objectReferenceValue        = icon;
            so.FindProperty("_amountText").objectReferenceValue       = amount;
            so.FindProperty("_gradeBorderImage").objectReferenceValue = borderImage;
            so.ApplyModifiedProperties();
            return slotUI;
        }

        private static InventorySlotUI BuildEquipRow(Transform parent, string label, Color bgColor)
        {
            GameObject            row = new($"Row_{label}");
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup h   = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing                 = 8f;
            h.childForceExpandHeight  = false;
            h.childForceExpandWidth   = false;
            h.childAlignment          = TextAnchor.MiddleLeft;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);

            Text lbl      = CreateChild<Text>(row.transform, "Label");
            lbl.text      = label;
            lbl.fontSize  = 12;
            lbl.color     = new Color(0.75f, 0.75f, 0.75f);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 62f);

            return BuildSlot(row.transform, $"EquipSlot_{label}", bgColor);
        }

        // ── 스크롤 뷰 ───────────────────────────────────────────────

        private static Transform BuildItemScrollView(Transform parent)
        {
            GameObject scrollGo    = new("ItemScrollView");
            scrollGo.transform.SetParent(parent, false);
            scrollGo.AddComponent<Image>().color = Color.clear;
            scrollGo.AddComponent<RectMask2D>();
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin     = Vector2.zero;
            scrollRt.anchorMax     = Vector2.one;
            scrollRt.offsetMin     = Vector2.zero;
            scrollRt.offsetMax     = Vector2.zero;

            // 아이템 버튼 — GridLayoutGroup
            // 중앙 패널 실제 너비(~1096px) 기준 48px셀+4px간격 = 약 18열
            GameObject      contentGo = new("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            contentGo.AddComponent<Image>().color = Color.clear;
            GridLayoutGroup grid       = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize              = new Vector2(48f, 48f);
            grid.spacing               = new Vector2(4f,  4f);
            grid.padding               = new RectOffset(6, 6, 6, 6);
            grid.constraint            = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount       = 18;
            ContentSizeFitter csf      = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit            = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRt    = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin        = new Vector2(0f, 1f);
            contentRt.anchorMax        = new Vector2(1f, 1f);
            contentRt.pivot            = new Vector2(0.5f, 1f);
            contentRt.sizeDelta        = Vector2.zero;

            ScrollRect sr              = scrollGo.AddComponent<ScrollRect>();
            sr.content                 = contentRt;
            sr.horizontal              = false;
            sr.vertical                = true;
            sr.scrollSensitivity       = 20f;
            sr.movementType            = ScrollRect.MovementType.Clamped;

            return contentRt.transform;
        }

        private static Transform BuildCraftScrollView(Transform parent)
        {
            GameObject scrollGo    = new("CraftScrollView");
            scrollGo.transform.SetParent(parent, false);
            scrollGo.AddComponent<Image>().color = Color.clear;
            scrollGo.AddComponent<RectMask2D>();
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin     = new Vector2(0f, 0f);
            scrollRt.anchorMax     = new Vector2(1f, 1f);
            scrollRt.offsetMin     = new Vector2(4f,   4f);
            scrollRt.offsetMax     = new Vector2(-4f, -30f);

            GameObject contentGo   = new("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            contentGo.AddComponent<Image>().color = Color.clear;
            VerticalLayoutGroup vlg    = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf      = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit            = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRt    = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin        = new Vector2(0f, 1f);
            contentRt.anchorMax        = new Vector2(1f, 1f);
            contentRt.pivot            = new Vector2(0.5f, 1f);
            contentRt.sizeDelta        = Vector2.zero;

            ScrollRect sr              = scrollGo.AddComponent<ScrollRect>();
            sr.content                 = contentRt;
            sr.horizontal              = false;
            sr.vertical                = true;
            sr.scrollSensitivity       = 20f;
            sr.movementType            = ScrollRect.MovementType.Clamped;

            return contentRt.transform;
        }

        // ── 레퍼런스 와이어링 ────────────────────────────────────────

        private static void WireInventoryUI(
            InventoryUI ui, InventorySystem inventory,
            InventorySlotUI[] bagSlots, InventorySlotUI[] equipSlots)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            SetArray(so, "_bagSlotUIs",   bagSlots);
            SetArray(so, "_equipSlotUIs", equipSlots);
            so.ApplyModifiedProperties();
        }

        private static void WireTestPanel(InventoryTestPanel panel, InventorySystem inventory,
            Transform buttonContainer, Transform statContainer, Transform filterContainer,
            Button acquireButton, Button addRouteButton, TargetItemPanelUI targetItemPanel,
            KoreanInputFieldAdapter searchAdapter, Transform specialMaterialContainer, Transform statFilterContainer)
        {
            // ItemDatabase에서 전체 아이템 로드 후 타입 → 등급 순서로 정렬
            const string dbPath = "Assets/ScriptableObjects/ItemDatabase.asset";
            ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
            if (db == null)
            {
                Debug.LogWarning($"[InventoryTestUIBuilder] ItemDatabase 없음: {dbPath} — Import BSER Items 먼저 실행하세요.");
                return;
            }

            List<ItemData> sorted = new List<ItemData>(db.Items);
            sorted.Sort((a, b) =>
            {
                int typeA = ItemTypeSortOrder(a.ItemType);
                int typeB = ItemTypeSortOrder(b.ItemType);
                if (typeA != typeB) return typeA.CompareTo(typeB);
                return ((int)a.ItemGrade).CompareTo((int)b.ItemGrade);
            });

            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_inventorySystem").objectReferenceValue  = inventory;
            so.FindProperty("_buttonContainer").objectReferenceValue  = buttonContainer;
            so.FindProperty("_statContainer").objectReferenceValue    = statContainer;
            so.FindProperty("_filterContainer").objectReferenceValue  = filterContainer;
            so.FindProperty("_acquireButton").objectReferenceValue              = acquireButton;
            so.FindProperty("_addRouteButton").objectReferenceValue             = addRouteButton;
            so.FindProperty("_targetItemPanel").objectReferenceValue            = targetItemPanel;
            so.FindProperty("_searchAdapter").objectReferenceValue              = searchAdapter;
            so.FindProperty("_specialMaterialContainer").objectReferenceValue   = specialMaterialContainer;
            so.FindProperty("_statFilterContainer").objectReferenceValue        = statFilterContainer;

            const string recipePath = "Assets/ScriptableObjects/RecipeDatabase.asset";
            RecipeDatabase recipeDb = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(recipePath);
            if (recipeDb != null)
                so.FindProperty("_recipeDatabase").objectReferenceValue = recipeDb;
            else
                Debug.LogWarning($"[InventoryTestUIBuilder] RecipeDatabase 없음: {recipePath}");

            SerializedProperty itemsProp = so.FindProperty("_acquisitionItems");
            itemsProp.arraySize = sorted.Count;
            for (int i = 0; i < sorted.Count; i++)
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = sorted[i];

            so.ApplyModifiedProperties();
        }

        private static Button BuildActionButton(Transform parent, string name, string label, Color bgColor)
        {
            GameObject go = CreatePanel(parent, name, bgColor);

            Button     btn = go.AddComponent<Button>();
            ColorBlock cb  = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
            btn.colors          = cb;

            Text lbl      = CreateChild<Text>(go.transform, "Label");
            lbl.text      = label;
            lbl.fontSize  = 14;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color     = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
            StretchFill(lbl.GetComponent<RectTransform>(), 0f, 0f);

            return btn;
        }

        private static TargetItemSlotUI BuildTargetSlot(Transform parent, string label, EquipmentSlotType slotType)
        {
            GameObject go = new($"TargetSlot_{label}");
            go.transform.SetParent(parent, false);

            // 배경 — 등급색 표시 + 드롭 수신 Raycast 대상
            Image bg    = go.AddComponent<Image>();
            bg.color    = new Color(0.20f, 0.20f, 0.20f);

            // 부위 라벨
            Text lbl        = CreateChild<Text>(go.transform, "Label");
            lbl.text        = label;
            lbl.fontSize    = 11;
            lbl.color       = new Color(0.60f, 0.60f, 0.60f);
            lbl.alignment   = TextAnchor.UpperCenter;
            RectTransform lblRt      = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin          = new Vector2(0f, 1f);
            lblRt.anchorMax          = new Vector2(1f, 1f);
            lblRt.pivot              = new Vector2(0.5f, 1f);
            lblRt.sizeDelta          = new Vector2(0f, 20f);
            lblRt.anchoredPosition   = Vector2.zero;

            // 아이콘
            Image         icon   = CreateChild<Image>(go.transform, "Icon");
            icon.enabled         = false;
            icon.preserveAspect  = true;
            icon.raycastTarget   = false;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            // TargetItemSlotUI 컴포넌트
            TargetItemSlotUI slotUI = go.AddComponent<TargetItemSlotUI>();
            SerializedObject so     = new SerializedObject(slotUI);
            so.FindProperty("_slotType").enumValueIndex          = (int)slotType;
            so.FindProperty("_bgImage").objectReferenceValue     = bg;
            so.FindProperty("_iconImage").objectReferenceValue   = icon;
            so.ApplyModifiedProperties();
            return slotUI;
        }

        // 무기 → 옷 → 머리 → 팔 → 다리 → 음식 → 소비 → 재료 → 기타
        private static int ItemTypeSortOrder(ItemType type)
        {
            return type switch
            {
                ItemType.Weapon    => 0,
                ItemType.Chest     => 1,
                ItemType.Helmet    => 2,
                ItemType.Arms      => 3,
                ItemType.Shoes     => 4,
                ItemType.Food      => 5,
                ItemType.Consumable => 6,
                ItemType.Material  => 7,
                ItemType.Special   => 8,
                _                  => 9,
            };
        }

        private static void WireCraftingSystem(CraftingSystem craftingSystem, InventorySystem inventory)
        {
            SerializedObject so = new SerializedObject(craftingSystem);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            const string dbPath = "Assets/ScriptableObjects/RecipeDatabase.asset";
            RecipeDatabase db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(dbPath);
            if (db != null)
                so.FindProperty("_recipeDatabase").objectReferenceValue = db;
            else
                Debug.LogWarning($"[InventoryTestUIBuilder] RecipeDatabase 없음: {dbPath} — Import BSER Items 먼저 실행하세요.");
            so.ApplyModifiedProperties();
        }

        private static void WireCraftingUI(
            CraftingUI ui, CraftingSystem craftingSystem,
            InventorySystem inventory, Transform slotContainer,
            ItemGradeColorConfig gradeConfig)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("_craftingSystem").objectReferenceValue  = craftingSystem;
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            so.FindProperty("_slotContainer").objectReferenceValue   = slotContainer;
            so.FindProperty("_gradeConfig").objectReferenceValue     = gradeConfig;
            so.ApplyModifiedProperties();
        }

        private static void WireGradeConfigToSlots(GameObject root, ItemGradeColorConfig gradeConfig)
        {
            if (gradeConfig == null) return;
            InventorySlotUI[] slots = root.GetComponentsInChildren<InventorySlotUI>();
            foreach (InventorySlotUI slot in slots)
            {
                SerializedObject so = new SerializedObject(slot);
                so.FindProperty("_gradeConfig").objectReferenceValue = gradeConfig;
                so.ApplyModifiedProperties();
            }
        }

        private static void SetArray<T>(SerializedObject so, string propName, T[] items) where T : Object
        {
            SerializedProperty prop = so.FindProperty(propName);
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
