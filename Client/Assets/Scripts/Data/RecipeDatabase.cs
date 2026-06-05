using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 전체 레시피 목록을 보유하는 ScriptableObject DB
    /// — 결과 아이템 ID를 키로 Dictionary 캐시 구축
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "ProjectER/Data/RecipeDatabase")]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<RecipeData> _recipes = new();

        // GC 주의: 런타임 캐시 — Initialize() 한 번만 호출
        private Dictionary<string, RecipeData> _cache;

        public IReadOnlyList<RecipeData> Recipes => _recipes;

        public void Initialize()
        {
            _cache = new Dictionary<string, RecipeData>(_recipes.Count);
            foreach (RecipeData recipe in _recipes)
            {
                if (recipe == null || recipe.ResultItem == null) continue;

                if (!_cache.TryAdd(recipe.ResultItem.Id, recipe))
                    Debug.LogWarning($"[RecipeDatabase] 중복 결과 아이템 ID: {recipe.ResultItem.Id}", this);
            }
        }

        /// <summary>
        /// 결과 아이템 ID로 레시피 조회. 없으면 null 반환.
        /// </summary>
        public RecipeData GetByResultId(string resultItemId)
        {
            if (_cache == null)
            {
                Debug.LogWarning("[RecipeDatabase] Initialize()가 호출되지 않았습니다. 자동 초기화합니다.");
                Initialize();
            }

            _cache.TryGetValue(resultItemId, out RecipeData result);
            return result;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _cache = null;
        }
#endif
    }
}
