using ProjectER.Character;
using ProjectER.Data;
using ProjectER.Inventory;
using ProjectER.UI;
using ProjectER.UI.InGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectER.Editor
{
    /// <summary>
    /// 04_InGameScene에 테스트용 HUD(체력/상태 표시, 전투·아이템·스킬 테스트 버튼, 하단 인벤토리 바)를
    /// 코드로 생성하는 에디터 유틸. 씬의 "Player"에 참조를 연결한다.
    /// 메뉴: ProjectER / Build InGame HUD
    /// </summary>
    public static class InGameHudBuilder
    {
        private const string ScenePath        = "Assets/Scenes/04_InGameScene.unity";
        private const string GradeConfigPath  = "Assets/ScriptableObjects/ItemGradeColorConfig.asset";
        private const string ItemDatabasePath = "Assets/ScriptableObjects/ItemDatabase.asset";

        private const string CanvasName = "InGameHudCanvas";

        private const int   BagSize    = 10;
        private const float SlotWidth  = 72f;
        private const float SlotAspect = 108f / 64f; // 슬롯 가로:세로 비율 (system-design 규칙)
        private static float SlotHeight => SlotWidth / SlotAspect;

        private static Font _font;

        [MenuItem("ProjectER/Build InGame HUD")]
        public static void Build()
        {
            UnityEngine.SceneManagement.Scene scene = OpenScene();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Canvas canvas = CreateCanvas();
            EnsureEventSystem();

            GameObject playerObj = GameObject.Find("Player");
            CharacterBase player = playerObj != null ? playerObj.GetComponent<CharacterBase>() : null;
            InventorySystem inventory = playerObj != null ? playerObj.GetComponent<InventorySystem>() : null;
            if (player == null || inventory == null)
                Debug.LogWarning("[InGameHudBuilder] 씬에서 Player(CharacterBase/InventorySystem)를 찾지 못했습니다. 먼저 Build InGame Scene 실행 필요.");

            ItemGradeColorConfig gradeConfig = AssetDatabase.LoadAssetAtPath<ItemGradeColorConfig>(GradeConfigPath);
            ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

            BuildStatusHud(canvas.transform, player);
            BuildCombatTestPanel(canvas.transform, player, inventory, itemDatabase);
            BuildInventoryBar(canvas.transform, inventory, gradeConfig);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("완료", "04_InGameScene 테스트 HUD 생성 완료.", "확인");
        }

        private static UnityEngine.SceneManagement.Scene OpenScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);
            return scene;
        }

        // ── Canvas / EventSystem ─────────────────────────────────────

        private static Canvas CreateCanvas()
        {
            GameObject existing = GameObject.Find(CanvasName);
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject go = new(CanvasName, typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            // New Input System 사용 → InputSystemUIInputModule 필수 (CLAUDE.md 에디터 씬 빌더 규칙)
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ── 상태 HUD (우상단) ────────────────────────────────────────

        private static void BuildStatusHud(Transform parent, CharacterBase player)
        {
            RectTransform panel = NewRect("StatusHud", parent);
            SetAnchor(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -20f), new Vector2(260f, 64f));

            // HP 바 배경
            RectTransform barBg = NewRect("HpBarBg", panel);
            SetAnchor(barBg, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 24f));
            barBg.offsetMin = new Vector2(0f, barBg.offsetMin.y);
            barBg.offsetMax = new Vector2(0f, barBg.offsetMax.y);
            AddImage(barBg, new Color(0.1f, 0.1f, 0.1f, 0.9f));

            // HP 채움 (Filled 이미지)
            RectTransform fillRt = NewRect("HpFill", barBg);
            SetStretch(fillRt);
            Image hpFill = AddImage(fillRt, new Color(0.85f, 0.2f, 0.2f, 1f));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = 1f;
            hpFill.raycastTarget = false;

            // HP 텍스트 (바 위 중앙)
            RectTransform hpTextRt = NewRect("HpText", barBg);
            SetStretch(hpTextRt);
            Text hpText = AddText(hpTextRt, "HP", 13, TextAnchor.MiddleCenter, Color.white);
            hpText.raycastTarget = false;

            // 상태 텍스트 (바 아래)
            RectTransform stateRt = NewRect("StateText", panel);
            SetAnchor(stateRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(0f, 22f));
            stateRt.offsetMin = new Vector2(0f, stateRt.offsetMin.y);
            stateRt.offsetMax = new Vector2(0f, stateRt.offsetMax.y);
            Text stateText = AddText(stateRt, "State: -", 14, TextAnchor.MiddleRight, Color.white);
            stateText.raycastTarget = false;

            PlayerStatusHud hud = panel.gameObject.AddComponent<PlayerStatusHud>();
            SerializedObject so = new(hud);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_hpFill").objectReferenceValue = hpFill;
            so.FindProperty("_hpText").objectReferenceValue = hpText;
            so.FindProperty("_stateText").objectReferenceValue = stateText;
            so.ApplyModifiedProperties();
        }

        // ── 전투/아이템/스킬 테스트 버튼 (좌상단) ─────────────────────

        private static void BuildCombatTestPanel(Transform parent, CharacterBase player,
            InventorySystem inventory, ItemDatabase itemDatabase)
        {
            RectTransform panel = NewRect("CombatTestPanel", parent);
            SetAnchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -20f), new Vector2(160f, 10f));
            AddImage(panel, new Color(0f, 0f, 0f, 0.35f));

            VerticalLayoutGroup vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = panel.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            CombatTestPanel comp = panel.gameObject.AddComponent<CombatTestPanel>();

            Button dmgSmall = CreateButton(panel, "데미지 -25");
            Button dmgLarge = CreateButton(panel, "데미지 -50");
            Button heal     = CreateButton(panel, "회복 +30");
            Button down     = CreateButton(panel, "빈사로");
            Button revive   = CreateButton(panel, "부활");
            Button kill     = CreateButton(panel, "사망 확정");
            Button addItem  = CreateButton(panel, "아이템 추가");
            Button clear    = CreateButton(panel, "인벤토리 비우기");
            Button skillQ   = CreateButton(panel, "스킬 Q");
            Button skillW   = CreateButton(panel, "스킬 W");
            Button skillE   = CreateButton(panel, "스킬 E");
            Button skillR   = CreateButton(panel, "스킬 R");

            SerializedObject so = new(comp);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_inventory").objectReferenceValue = inventory;
            so.FindProperty("_itemDatabase").objectReferenceValue = itemDatabase;
            so.FindProperty("_damageSmallButton").objectReferenceValue = dmgSmall;
            so.FindProperty("_damageLargeButton").objectReferenceValue = dmgLarge;
            so.FindProperty("_healButton").objectReferenceValue = heal;
            so.FindProperty("_downButton").objectReferenceValue = down;
            so.FindProperty("_reviveButton").objectReferenceValue = revive;
            so.FindProperty("_killButton").objectReferenceValue = kill;
            so.FindProperty("_addItemButton").objectReferenceValue = addItem;
            so.FindProperty("_clearItemButton").objectReferenceValue = clear;
            so.FindProperty("_skillQButton").objectReferenceValue = skillQ;
            so.FindProperty("_skillWButton").objectReferenceValue = skillW;
            so.FindProperty("_skillEButton").objectReferenceValue = skillE;
            so.FindProperty("_skillRButton").objectReferenceValue = skillR;
            so.ApplyModifiedProperties();
        }

        // ── 인벤토리 바 (우하단, 10칸) ────────────────────────────────

        private static void BuildInventoryBar(Transform parent, InventorySystem inventory,
            ItemGradeColorConfig gradeConfig)
        {
            RectTransform bar = NewRect("InventoryBar", parent);
            SetAnchor(bar, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 20f), new Vector2(10f, 10f));

            HorizontalLayoutGroup hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            ContentSizeFitter csf = bar.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            InGameInventoryBar comp = bar.gameObject.AddComponent<InGameInventoryBar>();

            InventorySlotUI[] slots = new InventorySlotUI[BagSize];
            for (int i = 0; i < BagSize; i++)
                slots[i] = CreateInventorySlot(bar, i, gradeConfig);

            SerializedObject so = new(comp);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventory;
            SerializedProperty arr = so.FindProperty("_bagSlotUIs");
            arr.arraySize = BagSize;
            for (int i = 0; i < BagSize; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedProperties();
        }

        private static InventorySlotUI CreateInventorySlot(Transform parent, int index,
            ItemGradeColorConfig gradeConfig)
        {
            RectTransform rt = NewRect($"Slot_{index}", parent);
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = SlotWidth;
            le.preferredHeight = SlotHeight;

            Image bg = AddImage(rt, new Color(0.12f, 0.12f, 0.14f, 0.9f));

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            InventorySlotUI slotUI = rt.gameObject.AddComponent<InventorySlotUI>();

            // 등급색 테두리 (슬롯 전체를 덮는 이미지, 아이템 있을 때만 색 적용)
            RectTransform borderRt = NewRect("GradeBorder", rt);
            SetStretch(borderRt);
            Image border = AddImage(borderRt, Color.clear);
            border.raycastTarget = false;

            // 아이콘 (슬롯 내부 10% inset)
            RectTransform iconRt = NewRect("Icon", rt);
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 수량 텍스트 (우하단)
            RectTransform amountRt = NewRect("Amount", rt);
            SetStretch(amountRt);
            Text amount = AddText(amountRt, string.Empty, 12, TextAnchor.LowerRight, Color.white);
            amount.raycastTarget = false;

            SerializedObject so = new(slotUI);
            so.FindProperty("_iconImage").objectReferenceValue = icon;
            so.FindProperty("_amountText").objectReferenceValue = amount;
            so.FindProperty("_gradeBorderImage").objectReferenceValue = border;
            so.FindProperty("_gradeConfig").objectReferenceValue = gradeConfig;
            so.FindProperty("_isDraggable").boolValue = false;
            so.ApplyModifiedProperties();

            return slotUI;
        }

        // ── UI 생성 헬퍼 ─────────────────────────────────────────────

        private static Button CreateButton(Transform parent, string label)
        {
            RectTransform rt = NewRect($"{label}", parent);
            Image bg = AddImage(rt, new Color(0.22f, 0.22f, 0.28f, 0.95f));

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;

            RectTransform labelRt = NewRect("Text", rt);
            SetStretch(labelRt);
            Text text = AddText(labelRt, label, 13, TextAnchor.MiddleCenter, Color.white);
            text.raycastTarget = false;

            return button;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rt, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text AddText(RectTransform rt, string content, int fontSize, TextAnchor anchor, Color color)
        {
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
