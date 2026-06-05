using System.Collections.Generic;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 조합 가능 레시피 전체 목록 UI — 인벤토리 변경 시 자동 갱신, 스크롤 지원
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private CraftingSystem      _craftingSystem;
        [SerializeField] private InventorySystem   _inventorySystem;
        [SerializeField] private Transform         _slotContainer;
        [SerializeField] private ItemGradeColorConfig _gradeConfig;

        // ⚠️ GC 주의: 버퍼·풀 재사용으로 매 갱신 시 List 할당 방지
        private readonly List<RecipeData>     _craftableBuffer = new List<RecipeData>();
        private readonly List<CraftingSlotUI> _slotPool        = new List<CraftingSlotUI>();

        private static readonly Color SlotBg     = new Color(0.15f, 0.22f, 0.15f, 1f);
        private static readonly Color BtnNormal  = new Color(0.18f, 0.48f, 0.18f, 1f);
        private static readonly Color BtnHover   = new Color(0.28f, 0.68f, 0.28f, 1f);
        private static readonly Color BtnPressed = new Color(0.10f, 0.28f, 0.10f, 1f);
        private static readonly Color IngrColor  = new Color(0.72f, 0.85f, 0.72f, 1f);

        private void OnEnable()
        {
            if (_craftingSystem == null || _inventorySystem == null || _slotContainer == null)
            {
                Debug.LogError($"[CraftingUI] 연결 누락 — " +
                               $"CraftingSystem={_craftingSystem != null}, " +
                               $"InventorySystem={_inventorySystem != null}, " +
                               $"SlotContainer={_slotContainer != null}");
                return;
            }
            _inventorySystem.OnBagSlotChanged   += HandleInventoryChanged;
            _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
            // 레이아웃 높이가 계산된 후 RefreshSlots 실행 (RectMask2D 클리핑 방지)
            Canvas.ForceUpdateCanvases();
            RefreshSlots();
        }

        private void OnDisable()
        {
            if (_inventorySystem == null) return;
            _inventorySystem.OnBagSlotChanged   -= HandleInventoryChanged;
            _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleInventoryChanged(int _, InventorySlot __) => RefreshSlots();
        private void HandleEquipmentChanged(EquipmentSlotType _, ItemData __) => RefreshSlots();

        private void RefreshSlots()
        {
            _craftingSystem.GetCraftableRecipes(_craftableBuffer);

            // 부족한 슬롯 보충
            while (_slotPool.Count < _craftableBuffer.Count)
                _slotPool.Add(CreateSlot());

            // 조합 가능 레시피 표시
            for (int i = 0; i < _craftableBuffer.Count; i++)
            {
                _slotPool[i].gameObject.SetActive(true);
                _slotPool[i].Show(_craftableBuffer[i], OnCraft);
            }

            // 남는 슬롯 숨김 — SetActive(false)로 ContentSizeFitter 레이아웃에서 제외
            for (int i = _craftableBuffer.Count; i < _slotPool.Count; i++)
                _slotPool[i].gameObject.SetActive(false);
        }

        private void OnCraft(RecipeData recipe)
        {
            // TryCraft → 인벤토리 변경 → OnBagSlotChanged → RefreshSlots 자동 호출
            _craftingSystem.TryCraft(recipe);
        }

        // ── 슬롯 동적 생성 ──────────────────────────────────────────

        private CraftingSlotUI CreateSlot()
        {
            GameObject go = new GameObject($"CraftingSlot_{_slotPool.Count}");
            go.transform.SetParent(_slotContainer, false);
            go.AddComponent<Image>().color = SlotBg;

            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;

            VerticalLayoutGroup vl   = go.AddComponent<VerticalLayoutGroup>();
            vl.padding               = new RectOffset(6, 6, 4, 4);
            vl.spacing               = 2f;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            Text resultName  = MakeText(go.transform, "ResultName",  13, FontStyle.Bold,   Color.white, 17f);
            Text ingredients = MakeText(go.transform, "Ingredients", 11, FontStyle.Normal, IngrColor,   15f);

            // 제작 버튼
            GameObject btnGo = new GameObject("CraftButton");
            btnGo.transform.SetParent(go.transform, false);
            btnGo.AddComponent<Image>().color = BtnNormal;
            Button craftBtn   = btnGo.AddComponent<Button>();
            ColorBlock cb     = craftBtn.colors;
            cb.highlightedColor = BtnHover;
            cb.pressedColor     = BtnPressed;
            craftBtn.colors   = cb;
            btnGo.AddComponent<LayoutElement>().preferredHeight = 20f;

            Text btnLabel      = MakeText(btnGo.transform, "Label", 12, FontStyle.Normal, Color.white, 20f);
            btnLabel.text      = "제작";
            btnLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform blr  = btnLabel.GetComponent<RectTransform>();
            blr.anchorMin = Vector2.zero;
            blr.anchorMax = Vector2.one;
            blr.offsetMin = Vector2.zero;
            blr.offsetMax = Vector2.zero;

            // CanvasGroup — CraftingSlotUI.Show/Hide에서 사용
            go.AddComponent<CanvasGroup>();

            CraftingSlotUI slot = go.AddComponent<CraftingSlotUI>();
            slot.Init(resultName, ingredients, craftBtn, _gradeConfig);
            return slot;
        }

        private static Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, float height)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t         = go.AddComponent<Text>();
            t.font         = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize     = size;
            t.fontStyle    = style;
            t.color        = color;
            t.alignment    = TextAnchor.MiddleLeft;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return t;
        }
    }
}
