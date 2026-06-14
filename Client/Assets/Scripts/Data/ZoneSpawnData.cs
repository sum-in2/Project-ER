using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 구역(서브존) 1개의 아이템 스폰 데이터 — ScriptableObject 1개 = areaCode + areaSpawnGroup 조합 1개
    /// BSER ItemSpawn.json 기준 (areaSpawnGroup: -1 = 지역 공용, 그 외 = 서브구역 코드)
    /// </summary>
    [CreateAssetMenu(fileName = "ZoneSpawn_New", menuName = "ProjectER/Data/ZoneSpawn")]
    public class ZoneSpawnData : ScriptableObject
    {
        [Header("BSER 연동")]
        [SerializeField] private int _areaCode;
        [SerializeField] private int _areaSpawnGroup; // -1 = 지역 공용

        [Header("스폰 목록")]
        [SerializeField] private List<SpawnEntry> _entries = new();

        public int AreaCode       => _areaCode;
        public int AreaSpawnGroup => _areaSpawnGroup;

        public IReadOnlyList<SpawnEntry> Entries => _entries;
    }
}
