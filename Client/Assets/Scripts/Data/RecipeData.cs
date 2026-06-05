using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 제작 레시피 데이터 — ScriptableObject 1개 = 레시피 1개
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "ProjectER/Data/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [Header("재료")]
        [SerializeField] private List<ItemIngredient> _ingredients = new();

        [Header("결과물")]
        [SerializeField] private ItemData _resultItem;
        [SerializeField] private int _resultAmount = 1;

        public IReadOnlyList<ItemIngredient> Ingredients => _ingredients;
        public ItemData ResultItem                       => _resultItem;
        public int      ResultAmount                     => _resultAmount;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_resultAmount < 1)
                _resultAmount = 1;
        }
#endif
    }
}
