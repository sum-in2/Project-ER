using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 전체 아이템 목록을 보유하는 ScriptableObject DB
    /// — 런타임에 Dictionary 캐시를 구축해 O(1) 조회 제공
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "ProjectER/Data/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> _items = new();

        // GC 주의: 런타임 캐시 — Initialize() 한 번만 호출
        private Dictionary<string, ItemData> _cache;
        private Dictionary<int, ItemData>    _codeCache; // BSER 코드 기반 조회용

        public IReadOnlyList<ItemData> Items => _items;

        /// <summary>
        /// 사용 전 반드시 한 번 호출 (GameBootstrap 또는 첫 접근 시 지연 초기화)
        /// </summary>
        public void Initialize()
        {
            _cache     = new Dictionary<string, ItemData>(_items.Count);
            _codeCache = new Dictionary<int, ItemData>(_items.Count);

            foreach (ItemData item in _items)
            {
                if (item == null) continue;

                if (!_cache.TryAdd(item.Id, item))
                    Debug.LogWarning($"[ItemDatabase] 중복 ID 발견: {item.Id}", this);

                if (item.BserCode != 0 && !_codeCache.TryAdd(item.BserCode, item))
                    Debug.LogWarning($"[ItemDatabase] 중복 BSER 코드 발견: {item.BserCode}", this);
            }
        }

        /// <summary>
        /// string ID로 아이템 조회. 없으면 null 반환.
        /// </summary>
        public ItemData GetById(string id)
        {
            EnsureInitialized();
            _cache.TryGetValue(id, out ItemData result);
            return result;
        }

        /// <summary>
        /// BSER 아이템 코드(int)로 아이템 조회. 없으면 null 반환.
        /// </summary>
        public ItemData GetByCode(int bserCode)
        {
            EnsureInitialized();
            _codeCache.TryGetValue(bserCode, out ItemData result);
            return result;
        }

        private void EnsureInitialized()
        {
            if (_cache != null) return;
            // GC 주의: 초기화 없이 조회 시 자동 초기화 (경고 포함)
            Debug.LogWarning("[ItemDatabase] Initialize()가 호출되지 않았습니다. 자동 초기화합니다.");
            Initialize();
        }

        /// <summary>
        /// 타입별 아이템 목록 반환 (GC 주의 — 빈번한 호출 자제, 결과 캐싱 권장)
        /// </summary>
        public List<ItemData> GetByType(ItemType type)
        {
            // GC 주의: 호출마다 List 할당
            List<ItemData> result = new();
            foreach (ItemData item in _items)
            {
                if (item != null && item.ItemType == type)
                    result.Add(item);
            }
            return result;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 변경 시 캐시 무효화
            _cache     = null;
            _codeCache = null;
        }
#endif
    }
}
