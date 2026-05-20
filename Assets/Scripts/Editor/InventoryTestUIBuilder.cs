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
    /// 실행 전 Generate Dummy Items / Generate Dummy Recipes 먼저 실행 필요
    /// </summary>
    public static class InventoryTestUIBuilder
    {
        private static readonly Color PanelColor     = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        private static readonly Color SlotColor      = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color EquipSlotColor = new Color(0.22f, 0.28f, 0.35f, 1f);
        private static readonly Color CraftSlotColor = new Color(0.15f, 0.22f, 0.15f, 1f);

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
            GameObject matSection        = BuildSection(root.transform, "MaterialSection", 155f, "재료 획득");
            // 버튼 전용 컨테이너 — Image 먼저 추가해 RectTransform 확보 후 투명 처리
            GameObject matContainer      = new GameObject("ButtonContainer");
            matContainer.transform.SetParent(matSection.transform, false);
            Image matContainerBg         = matContainer.AddComponent<Image>();
            matContainerBg.color         = Color.clear;
            matContainerBg.raycastTarget = false;
            RectTransform matContainerRt = matContainer.GetComponent<RectTransform>();
            matContainerRt.anchorMin     = new Vector2(0f, 0f);
            matContainerRt.anchorMax     = new Vector2(1f, 1f);
            matContainerRt.offsetMin     = new Vector2(6f, 6f);
            matContainerRt.offsetMax     = new Vector2(-6f, -30f);
            VerticalLayoutGroup matVL    = matContainer.AddComponent<VerticalLayoutGroup>();
            matVL.spacing                = 4f;
            matVL.childForceExpandWidth  = true;
            matVL.childForceExpandHeight = false;
            InventoryTestPanel testPanel = matSection.AddComponent<InventoryTestPanel>();

            // ── 조합 섹션 ─────────────────────────────────────────────
            GameObject craftSection         = BuildSection(root.transform, "CraftingSection", 205f, "조합 가능");
            VerticalLayoutGroup craftVL     = craftSection.AddComponent<VerticalLayoutGroup>();
            craftVL.spacing                 = 4f;
            craftVL.padding                 = new RectOffset(6, 6, 30, 6);
            craftVL.childForceExpandWidth   = true;
            craftVL.childForceExpandHeight  = false;
            CraftingSlotUI[] craftSlotUIs   = new CraftingSlotUI[5];
            for (int i = 0; i < 5; i++)
                craftSlotUIs[i] = BuildCraftingSlot(craftSection.transform, i);
            CraftingUI craftingUI = craftSection.AddComponent<CraftingUI>();

            // ── InventoryUI ───────────────────────────────────────────
            InventoryUI inventoryUI = root.AddComponent<InventoryUI>();

            // ── 레퍼런스 연결 ─────────────────────────────────────────
            WireInventoryUI(inventoryUI, inventory, bagSlotUIs, equipSlotUIs);
            WireTestPanel(testPanel, inventory, matContainer.transform);
            WireCraftingSystem(crafting, inventory);
            WireCraftingUI(craftingUI, crafting, inventory, craftSlotUIs);

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
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
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

        private static CraftingSlotUI BuildCraftingSlot(Transform parent, int index)
        {
            GameObject go   = new GameObject($"CraftingSlot_{index}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = CraftSlotColor;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 64f);

            VerticalLayoutGroup vl   = go.AddComponent<VerticalLayoutGroup>();
            vl.padding               = new RectOffset(6, 6, 4, 4);
            vl.spacing               = 2f;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            Text resultName   = BuildLabel(go.transform, "ResultName",   13, FontStyle.Bold,   Color.white);
            Text ingredients  = BuildLabel(go.transform, "Ingredients",  11, FontStyle.Normal, new Color(0.72f, 0.85f, 0.72f));

            // 제작 버튼
            GameObject btnGo  = new GameObject("CraftButton");
            btnGo.transform.SetParent(go.transform, false);
            btnGo.AddComponent<Image>().color = new Color(0.18f, 0.48f, 0.18f);
            Button craftBtn   = btnGo.AddComponent<Button>();
            ColorBlock cb     = craftBtn.colors;
            cb.highlightedColor = new Color(0.28f, 0.68f, 0.28f);
            cb.pressedColor     = new Color(0.1f, 0.28f, 0.1f);
            craftBtn.colors   = cb;
            btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

            Text btnLabel     = CreateChild<Text>(btnGo.transform, "Label");
            btnLabel.text     = "제작";
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.fontSize = 12;
            btnLabel.color    = Color.white;
            RectTransform blr = btnLabel.GetComponent<RectTransform>();
            blr.anchorMin     = Vector2.zero;
            blr.anchorMax     = Vector2.one;
            blr.offsetMin     = Vector2.zero;
            blr.offsetMax     = Vector2.zero;

            CraftingSlotUI slotUI = go.AddComponent<CraftingSlotUI>();
            SerializedObject so   = new SerializedObject(slotUI);
            so.FindProperty("_resultNameText").objectReferenceValue  = resultName;
            so.FindProperty("_ingredientsText").objectReferenceValue = ingredients;
            so.FindProperty("_craftButton").objectReferenceValue     = craftBtn;
            so.ApplyModifiedProperties();

            go.SetActive(false); // CraftingSlotUI.Show() 가 활성화
            return slotUI;
        }

        private static Text BuildLabel(Transform parent, string name, int fontSize, FontStyle style, Color color)
        {
            Text t         = CreateChild<Text>(parent, name);
            t.fontSize     = fontSize;
            t.fontStyle    = style;
            t.color        = color;
            t.alignment    = TextAnchor.MiddleLeft;
            t.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, fontSize + 4f);
            return t;
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
            string[] baseMatIds = { "mat_wood", "mat_stone", "mat_leather", "mat_iron_ore", "mat_fiber" };
            const string itemPath = "Assets/ScriptableObjects/Dummy/Items";

            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_inventorySystem").objectReferenceValue  = inventory;
            so.FindProperty("_buttonContainer").objectReferenceValue  = buttonContainer;

            SerializedProperty itemsProp = so.FindProperty("_acquisitionItems");
            itemsProp.arraySize = baseMatIds.Length;
            for (int i = 0; i < baseMatIds.Length; i++)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"{itemPath}/{baseMatIds[i]}.asset");
                if (item == null)
                    Debug.LogWarning($"[InventoryTestUIBuilder] 기본 재료 없음: {baseMatIds[i]} — Generate Dummy Items 먼저 실행하세요.");
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = item;
            }
            so.ApplyModifiedProperties();
        }

        private static void WireCraftingSystem(CraftingSystem craftingSystem, InventorySystem inventory)
        {
            SerializedObject so = new SerializedObject(craftingSystem);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;

            string[] guids = AssetDatabase.FindAssets("t:RecipeDatabase", new[] { "Assets/ScriptableObjects/Dummy" });
            if (guids.Length > 0)
            {
                RecipeDatabase db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                so.FindProperty("_recipeDatabase").objectReferenceValue = db;
            }
            else
            {
                Debug.LogWarning("[InventoryTestUIBuilder] RecipeDatabase 없음 — Generate Dummy Recipes 먼저 실행하세요.");
            }
            so.ApplyModifiedProperties();
        }

        private static void WireCraftingUI(
            CraftingUI ui, CraftingSystem craftingSystem,
            InventorySystem inventory, CraftingSlotUI[] slots)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("_craftingSystem").objectReferenceValue  = craftingSystem;
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            SetArray(so, "_slots", slots);
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
