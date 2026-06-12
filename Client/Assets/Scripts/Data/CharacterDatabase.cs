using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 전체 실험체 목록을 보유하는 ScriptableObject DB
    /// — 런타임에 Dictionary 캐시를 구축해 O(1) 조회 제공
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "ProjectER/Data/CharacterDatabase")]
    public class CharacterDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterData> _characters = new();

        // GC 주의: 런타임 캐시 — Initialize() 한 번만 호출
        private Dictionary<int, CharacterData> _codeCache;

        public IReadOnlyList<CharacterData> Characters => _characters;

        /// <summary>
        /// 사용 전 반드시 한 번 호출 (GameBootstrap 또는 첫 접근 시 지연 초기화)
        /// </summary>
        public void Initialize()
        {
            _codeCache = new Dictionary<int, CharacterData>(_characters.Count);

            foreach (CharacterData character in _characters)
            {
                if (character == null) continue;

                if (!_codeCache.TryAdd(character.BserCode, character))
                    Debug.LogWarning($"[CharacterDatabase] 중복 BSER 코드 발견: {character.BserCode}", this);
            }
        }

        /// <summary>
        /// BSER 실험체 코드(int)로 조회. 없으면 null 반환.
        /// </summary>
        public CharacterData GetByCode(int bserCode)
        {
            EnsureInitialized();
            _codeCache.TryGetValue(bserCode, out CharacterData result);
            return result;
        }

        /// <summary>
        /// 무기군을 사용 가능한 실험체 목록 반환 (GC 주의 — 빈번한 호출 자제, 결과 캐싱 권장)
        /// </summary>
        public List<CharacterData> GetByWeaponType(WeaponType weaponType)
        {
            // GC 주의: 호출마다 List 할당
            List<CharacterData> result = new();
            foreach (CharacterData character in _characters)
            {
                if (character == null) continue;

                IReadOnlyList<WeaponType> masteries = character.WeaponMasteries;
                for (int i = 0; i < masteries.Count; i++)
                {
                    if (masteries[i] == weaponType)
                    {
                        result.Add(character);
                        break;
                    }
                }
            }
            return result;
        }

        private void EnsureInitialized()
        {
            if (_codeCache != null) return;
            // GC 주의: 초기화 없이 조회 시 자동 초기화 (경고 포함)
            Debug.LogWarning("[CharacterDatabase] Initialize()가 호출되지 않았습니다. 자동 초기화합니다.");
            Initialize();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 변경 시 캐시 무효화
            _codeCache = null;
        }
#endif
    }
}
