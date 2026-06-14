using System;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 구역 내 아이템 스폰 항목 1개 — ItemData 참조 + 스폰 개수
    /// </summary>
    [Serializable]
    public struct SpawnEntry
    {
        [SerializeField] private ItemData _item;
        [SerializeField] private int _dropCount;

        public SpawnEntry(ItemData item, int dropCount)
        {
            _item = item;
            _dropCount = dropCount;
        }

        public ItemData Item      => _item;
        public int      DropCount => _dropCount;
    }
}
