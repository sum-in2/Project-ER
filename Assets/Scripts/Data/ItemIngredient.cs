using System;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 레시피 재료 1개 — ItemData 참조 + 필요 수량
    /// </summary>
    [Serializable]
    public struct ItemIngredient
    {
        [SerializeField] private ItemData _item;
        [SerializeField] private int _amount;

        public ItemData Item   => _item;
        public int      Amount => _amount;
    }
}
