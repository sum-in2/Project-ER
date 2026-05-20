using System;
using System.Text;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 조합 가능 슬롯 하나 — 결과물명 + 재료 목록 + 제작 버튼
    /// </summary>
    public class CraftingSlotUI : MonoBehaviour
    {
        [SerializeField] private Text   _resultNameText;
        [SerializeField] private Text   _ingredientsText;
        [SerializeField] private Button _craftButton;

        private RecipeData         _currentRecipe;
        private Action<RecipeData> _onCraft;

        private void Awake()
        {
            _craftButton?.onClick.AddListener(HandleCraftClick);
        }

        private void OnDestroy()
        {
            _craftButton?.onClick.RemoveListener(HandleCraftClick);
        }

        public void Show(RecipeData recipe, Action<RecipeData> onCraft)
        {
            _currentRecipe = recipe;
            _onCraft       = onCraft;

            _resultNameText.text = recipe.ResultItem.DisplayName;

            // ⚠️ GC 주의: 이벤트 발생 시에만 호출 — StringBuilder 허용
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(recipe.Ingredients[i].Item.DisplayName);
            }
            _ingredientsText.text = sb.ToString();

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _currentRecipe = null;
            gameObject.SetActive(false);
        }

        private void HandleCraftClick() => _onCraft?.Invoke(_currentRecipe);
    }
}
