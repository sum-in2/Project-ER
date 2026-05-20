using System;
using System.Text;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 조합 가능 슬롯 하나 — 결과물명 + 재료 목록 + 제작 버튼
    /// SetActive 대신 CanvasGroup alpha로 표시/숨김 — VLG 레이아웃 안정성 유지
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CraftingSlotUI : MonoBehaviour
    {
        [SerializeField] private Text   _resultNameText;
        [SerializeField] private Text   _ingredientsText;
        [SerializeField] private Button _craftButton;

        private RecipeData         _currentRecipe;
        private Action<RecipeData> _onCraft;
        private CanvasGroup        _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_craftButton != null)
                _craftButton.onClick.AddListener(HandleCraftClick);
        }

        private void OnDestroy()
        {
            if (_craftButton != null)
                _craftButton.onClick.RemoveListener(HandleCraftClick);
        }

        public void Show(RecipeData recipe, Action<RecipeData> onCraft)
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _currentRecipe = recipe;
            _onCraft       = onCraft;

            if (_resultNameText == null || _ingredientsText == null)
            {
                Debug.LogError($"[CraftingSlotUI] {gameObject.name} — Text 참조가 null입니다. 빌더를 다시 실행하세요.");
                return;
            }

            _resultNameText.text = recipe.ResultItem.DisplayName;

            // ⚠️ GC 주의: 이벤트 발생 시에만 호출 — StringBuilder 허용
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(recipe.Ingredients[i].Item.DisplayName);
            }
            _ingredientsText.text = sb.ToString();

            _canvasGroup.alpha          = 1f;
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _currentRecipe              = null;
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void HandleCraftClick() => _onCraft?.Invoke(_currentRecipe);
    }
}
