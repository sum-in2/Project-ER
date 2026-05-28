using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using ProjectER.UI;

namespace ProjectER.Editor
{
    /// <summary>
    /// 인벤토리 + 크래프팅 테스트 UI 자동 생성
    /// 메뉴: ProjectER > Build Inventory Test UI
    /// 실행 전 Import BSER Items 먼저 실행 필요
    /// </summary>
    public static class InventoryTestUIBuilder
    {
        private static readonly Color PanelColor     = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        private static readonly Color SlotColor      = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color EquipSlotColor = new Color(0.22f, 0.28f, 0.35f, 1f);

        [MenuItem("ProjectER/Build Inventory Test UI")]
        public static void Build()
        {
            // ── Player ────────────────────────────────────────────────
            GameObject player           = new GameObject("Player");
            InventorySystem inventory   = player.AddComponent<InventorySystem>();
            CraftingSystem  crafting    = player.AddComponent<CraftingSystem>();

            // ── Canvas ────────────────────────────────────────────────
            Canvas canvas = BuildCanvas();

            // ── 루트 패널 ─────────────────────────────────────────────
            GameObject root          = BuildPanel(canvas.transform, "InventoryTestRoot", PanelColor);
            RectTransform rootRect   = root.GetComponent<RectTransform>();
            rootRect.anchorMin       = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax       = new Vector2(0.5f, 0.5f);
            rootRect.pivot           = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta       = new Vector2(860f, 390f);
            HorizontalLayoutGroup hl = root.AddComponent<HorizontalLayoutGroup>();
            hl.spacing               = 8f;
            hl.padding               = new RectOffset(8, 8, 8, 8);
            hl.childControlHeight     = true;   // 자식 높이를 레이아웃이 직접 제어 (RectMask2D 클리핑 오류 방지)
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth  = false;
            hl.childAlignment         = TextAnchor.UpperLeft;

            // ── 가방 섹션 ─────────────────────────────────────────────
            GameObject bagSection        = BuildSection(root.transform, "BagSection", 295f, "가방  (클릭 → 장착)");
            GameObject bagGrid           = BuildGrid(bagSection.transform);
            InventorySlotUI[] bagSlotUIs = new InventorySlotUI[10];
            for (int i = 0; i < 10; i++)
                bagSlotUIs[i] = BuildSlot(bagGrid.transform, $"BagSlot_{i}", SlotColor);

            // ── 장비 섹션 ─────────────────────────────────────────────
            string[] equipLabels          = { "무기", "옷", "머리", "팔", "신발" };
            GameObject equipSection       = BuildSection(root.transform, "EquipSection", 162f, "장비  (클릭 → 해제)");
            VerticalLayoutGroup equipVL   = equipSection.AddComponent<VerticalLayoutGroup>();
            equipVL.spacing               = 6f;
            equipVL.padding               = new RectOffset(6, 6, 30, 6);
            equipVL.childForceExpandWidth  = true;
            equipVL.childForceExpandHeight = false;
            InventorySlotUI[] equipSlotUIs = new InventorySlotUI[5];
            for (int i = 0; i < 5; i++)
                equipSlotUIs[i] = BuildEquipRow(equipSection.transform, equipLabels[i], EquipSlotColor);

            // ── 재료 획득 섹션 ────────────────────────────────────────
            // 버튼 수가 섹션 높이를 초과할 수 있으므로 스크롤뷰 사용
            GameObject matSection    = BuildSection(root.transform, "MaterialSection", 155f, "재료 획득");
            Transform  matContent    = BuildScrollView(matSection.transform);
            InventoryTestPanel testPanel = matSection.AddComponent<InventoryTestPanel>();

            // ── 조합 섹션 ─────────────────────────────────────────────
            // 스크롤뷰 — CraftingUI가 슬롯을 동적으로 생성하므로 컨테이너만 준비
            GameObject craftSection = BuildSection(root.transform, "CraftingSection", 205f, "조합 가능");
            Transform  craftContent = BuildScrollView(craftSection.transform);
            CraftingUI craftingUI   = craftSection.AddComponent<CraftingUI>();

            // ── InventoryUI ───────────────────────────────────────────
            InventoryUI inventoryUI = root.AddComponent<InventoryUI>();

            // ── 레퍼런스 연결 ─────────────────────────────────────────
            WireInventoryUI(inventoryUI, inventory, bagSlotUIs, equipSlotUIs);
            WireTestPanel(testPanel, inventory, matContent);
            WireCraftingSystem(crafting, inventory);
            WireCraftingUI(craftingUI, crafting, inventory, craftContent);

            Debug.Log("[InventoryTestUIBuilder] 완료. " +
                      "Generate Dummy Items / Recipes가 없으면 Inspector에서 직접 할당하세요.");
            Selection.activeGameObject = root;
        }

        // ── 빌드 헬퍼 ────────────────────────────────────────────────

        private static Canvas BuildCanvas()
        {
            GameObject go          = new GameObject("Canvas");
            Canvas canvas          = go.AddComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler    = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();

            // EventSystem 없으면 생성 — UI 클릭 입력에 필수
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }

        private static GameObject BuildPanel(Transform parent, string name, Color color)
        {
            GameObject go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img      = go.AddComponent<Image>();
            img.sprite     = null;
            img.color      = color;
            return go;
        }

        private static GameObject BuildSection(Transform parent, string name, float width, string title)
        {
            GameObject go       = BuildPanel(parent, name, PanelColor);
            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(width, 0f);

            GameObject titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(go.transform, false);
            Text t              = titleGo.AddComponent<Text>();
            t.text              = title;
            t.fontSize          = 13;
            t.fontStyle         = FontStyle.Bold;
            t.color             = new Color(0.85f, 0.85f, 0.85f);
            t.alignment         = TextAnchor.MiddleLeft;
            RectTransform trt   = titleGo.GetComponent<RectTransform>();
            trt.anchorMin       = new Vector2(0f, 1f);
            trt.anchorMax       = new Vector2(1f, 1f);
            trt.pivot           = new Vector2(0.5f, 1f);
            trt.sizeDelta       = new Vector2(0f, 24f);
            trt.anchoredPosition = new Vector2(6f, 0f);
            return go;
        }

        private static GameObject BuildGrid(Transform parent)
        {
            GameObject go        = new GameObject("BagGrid");
            go.transform.SetParent(parent, false);
            GridLayoutGroup grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(52f, 52f);
            grid.spacing         = new Vector2(5f, 5f);
            grid.padding         = new RectOffset(6, 6, 30, 6);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            RectTransform rt     = go.GetComponent<RectTransform>();
            rt.anchorMin         = Vector2.zero;
            rt.anchorMax         = Vector2.one;
            rt.offsetMin         = Vector2.zero;
            rt.offsetMax         = Vector2.zero;
            return go;
        }

        private static InventorySlotUI BuildSlot(Transform parent, string name, Color bgColor)
        {
            GameObject go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = bgColor;

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.45f, 0.55f, 0.65f);
            cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
            btn.colors          = cb;

            Image icon          = CreateChild<Image>(go.transform, "Icon");
            icon.enabled        = false;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin    = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax    = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin    = Vector2.zero;
            iconRt.offsetMax    = Vector2.zero;

            Text amount         = CreateChild<Text>(go.transform, "Amount");
            amount.alignment    = TextAnchor.LowerRight;
            amount.fontSize     = 11;
            amount.fontStyle    = FontStyle.Bold;
            amount.color        = Color.white;
            amount.enabled      = false;
            RectTransform amt   = amount.GetComponent<RectTransform>();
            amt.anchorMin       = Vector2.zero;
            amt.anchorMax       = Vector2.one;
            amt.offsetMin       = Vector2.zero;
            amt.offsetMax       = new Vector2(-2f, -2f);

            InventorySlotUI slotUI = go.AddComponent<InventorySlotUI>();
            SerializedObject so    = new SerializedObject(slotUI);
            so.FindProperty("_iconImage").objectReferenceValue  = icon;
            so.FindProperty("_amountText").objectReferenceValue = amount;
            so.ApplyModifiedProperties();
            return slotUI;
        }

        private static InventorySlotUI BuildEquipRow(Transform parent, string label, Color bgColor)
        {
            GameObject row          = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing               = 6f;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth  = false;
            h.childAlignment         = TextAnchor.MiddleLeft;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

            Text lbl         = CreateChild<Text>(row.transform, "Label");
            lbl.text         = label;
            lbl.fontSize     = 12;
            lbl.color        = new Color(0.75f, 0.75f, 0.75f);
            lbl.alignment    = TextAnchor.MiddleLeft;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 52f);

            return BuildSlot(row.transform, $"EquipSlot_{label}", bgColor);
        }

        private static Transform BuildScrollView(Transform parent)
        {
            // 스크롤 뷰 루트 — 타이틀 높이(28px) 아래 영역을 채움, RectMask2D로 클리핑
            GameObject scrollGo = new GameObject("CraftScrollView");
            scrollGo.transform.SetParent(parent, false);
            scrollGo.AddComponent<Image>().color = Color.clear;
            scrollGo.AddComponent<RectMask2D>();
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin    = new Vector2(0f, 0f);
            scrollRt.anchorMax    = new Vector2(1f, 1f);
            scrollRt.offsetMin    = new Vector2(4f,  4f);
            scrollRt.offsetMax    = new Vector2(-4f, -28f);

            // 컨텐츠 — 슬롯이 추가될수록 아래로 늘어남
            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            contentGo.AddComponent<Image>().color = Color.clear;
            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing               = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            ScrollRect sr        = scrollGo.AddComponent<ScrollRect>();
            sr.content           = contentRt;
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 20f;
            sr.movementType      = ScrollRect.MovementType.Clamped;

            return contentRt.transform;
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        // ── 레퍼런스 와이어링 ─────────────────────────────────────────

        private static void WireInventoryUI(
            InventoryUI ui, InventorySystem inventory,
            InventorySlotUI[] bagSlots, InventorySlotUI[] equipSlots)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            SetArray(so, "_bagSlotUIs",  bagSlots);
            SetArray(so, "_equipSlotUIs", equipSlots);
            so.ApplyModifiedProperties();
        }

        private static void WireTestPanel(InventoryTestPanel panel, InventorySystem inventory, Transform buttonContainer)
        {
            // BSER 기본 획득 아이템 전체 (manufacturableType == 1)
            // — 기본 재료(ItemMisc) 37종 + 기본 무기 22종 + 기본 방어구 15종
            string[] bserAssetNames =
            {
                // ── 기본 재료 (ItemMisc, 37종) ──────────────────────────
                "mat_101101",  // Scissors
                "mat_101102",  // Fountain Pen
                "mat_105102",  // Pickaxe
                "mat_108101",  // Branch
                "mat_112101",  // Stone
                "mat_112103",  // Iron Ball
                "mat_112104",  // Glass Bottle
                "mat_113102",  // Playing Cards
                "mat_113104",  // Chalk
                "mat_205101",  // Feather
                "mat_205102",  // Flower
                "mat_205103",  // Ribbon
                "mat_205109",  // Cross
                "mat_205110",  // Binoculars
                "mat_205303",  // Magazine
                "mat_302103",  // Ice
                "mat_401101",  // Nail
                "mat_401103",  // Leather
                "mat_401104",  // Turtle Shell
                "mat_401105",  // Rubber
                "mat_401106",  // Scrap Metal
                "mat_401107",  // Lighter
                "mat_401108",  // Laser Pointer
                "mat_401109",  // Stallion Medal
                "mat_401110",  // Battery
                "mat_401112",  // Oil
                "mat_401113",  // Cloth
                "mat_401114",  // Gemstone
                "mat_401117",  // Paper
                "mat_401121",  // Gunpowder
                "mat_401123",  // Chemicals
                "mat_401124",  // Graphite
                "mat_401309",  // Plastic
                "mat_401405",  // Crimson Shard
                "mat_401406",  // Dawnlight Shard
                "mat_502104",  // Piano Wire
                "mat_502401",  // Thread

                // ── 기본 무기 (ItemWeapon, manufacturableType==1, 22종) ─
                // 상위 무기 제작 재료로도 사용됨 — 가방에 있어야 조합 가능
                "wpn_101104",  // Kitchen Knife
                "wpn_102101",  // Rusty Sword
                "wpn_103201",  // Twin Blades
                "wpn_104101",  // Hammer
                "wpn_105103",  // Hatchet
                "wpn_107101",  // Short Spear
                "wpn_108102",  // Short Rod
                "wpn_108103",  // Bamboo
                "wpn_109101",  // Whip
                "wpn_110102",  // Cotton Gloves
                "wpn_112105",  // Baseball
                "wpn_113101",  // Razor
                "wpn_114101",  // Bow
                "wpn_115101",  // Short Crossbow
                "wpn_116101",  // Walther PPK
                "wpn_117101",  // Fedorova
                "wpn_118101",  // Long Rifle
                "wpn_119101",  // Steel Chain
                "wpn_120101",  // Needle
                "wpn_121101",  // Starter Guitar
                "wpn_122101",  // Lens
                "wpn_130101",  // Glass Bead

                // ── 기본 방어구 (ItemArmor, manufacturableType==1, 15종) ─
                // 상위 방어구 제작 재료로도 사용됨 — 가방에 있어야 조합 가능
                "arm_201101",  // Hairband
                "arm_201102",  // Hat
                "arm_201104",  // Bike Helmet
                "arm_201201",  // Mask
                "arm_202101",  // Windbreaker
                "arm_202103",  // Monk's Robe
                "arm_202105",  // Wetsuit
                "arm_202106",  // Shirt
                "arm_203101",  // Watch
                "arm_203102",  // Bandage
                "arm_203104",  // Bracelet
                "arm_204101",  // Slippers
                "arm_204102",  // Running Shoes
                "arm_204103",  // Tights
                "arm_204205",  // Clogs
            };
            const string itemPath = "Assets/ScriptableObjects/Items/BSER";

            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            so.FindProperty("_buttonContainer").objectReferenceValue = buttonContainer;

            SerializedProperty itemsProp = so.FindProperty("_acquisitionItems");
            itemsProp.arraySize = bserAssetNames.Length;
            for (int i = 0; i < bserAssetNames.Length; i++)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"{itemPath}/{bserAssetNames[i]}.asset");
                if (item == null)
                    Debug.LogWarning($"[InventoryTestUIBuilder] 아이템 SO 없음: {bserAssetNames[i]} — ProjectER > Import BSER Items 먼저 실행하세요.");
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = item;
            }
            so.ApplyModifiedProperties();
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
            InventorySystem inventory, Transform slotContainer)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("_craftingSystem").objectReferenceValue  = craftingSystem;
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            so.FindProperty("_slotContainer").objectReferenceValue   = slotContainer;
            so.ApplyModifiedProperties();
        }

        private static void SetArray<T>(SerializedObject so, string propName, T[] items) where T : UnityEngine.Object
        {
            SerializedProperty prop = so.FindProperty(propName);
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
