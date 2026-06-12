using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 무기군별 기본 스탯 전체 목록을 보유하는 ScriptableObject DB
    /// — 런타임에 Dictionary 캐시를 구축해 O(1) 조회 제공
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponTypeDatabase", menuName = "ProjectER/Data/WeaponTypeDatabase")]
    public class WeaponTypeDatabase : ScriptableObject
    {
        [SerializeField] private List<WeaponTypeInfoData> _weaponTypes = new();

        // GC 주의: 런타임 캐시 — Initialize() 한 번만 호출
        private Dictionary<WeaponType, WeaponTypeInfoData> _cache;

        public IReadOnlyList<WeaponTypeInfoData> WeaponTypes => _weaponTypes;

        /// <summary>
        /// 사용 전 반드시 한 번 호출 (GameBootstrap 또는 첫 접근 시 지연 초기화)
        /// </summary>
        public void Initialize()
        {
            _cache = new Dictionary<WeaponType, WeaponTypeInfoData>(_weaponTypes.Count);

            foreach (WeaponTypeInfoData info in _weaponTypes)
            {
                if (info == null) continue;

                if (!_cache.TryAdd(info.WeaponType, info))
                    Debug.LogWarning($"[WeaponTypeDatabase] 중복 무기군 발견: {info.WeaponType}", this);
            }
        }

        /// <summary>
        /// 무기군(WeaponType)으로 조회. 없으면 null 반환.
        /// </summary>
        public WeaponTypeInfoData GetByType(WeaponType weaponType)
        {
            EnsureInitialized();
            _cache.TryGetValue(weaponType, out WeaponTypeInfoData result);
            return result;
        }

        private void EnsureInitialized()
        {
            if (_cache != null) return;
            // GC 주의: 초기화 없이 조회 시 자동 초기화 (경고 포함)
            Debug.LogWarning("[WeaponTypeDatabase] Initialize()가 호출되지 않았습니다. 자동 초기화합니다.");
            Initialize();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 변경 시 캐시 무효화
            _cache = null;
        }
#endif
    }
}
