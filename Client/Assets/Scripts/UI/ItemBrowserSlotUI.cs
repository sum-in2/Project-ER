using ProjectER.Data;
using UnityEngine;

namespace ProjectER.UI
{
    /// <summary>
    /// 도감 그리드 슬롯 — 아이템이 생성 시 고정되며 변경되지 않는다.
    /// 기본적으로 드래그 가능(목표 루트 슬롯으로 드래그하여 배치).
    /// </summary>
    public class ItemBrowserSlotUI : MonoBehaviour, IItemSlot
    {
        [SerializeField] private bool _isDraggable = true;

        private ItemData _item;

        // ── IItemSlot ─────────────────────────────────────────────────
        public ItemData CurrentItem => _item;
        public bool     IsDraggable => _isDraggable;

        /// <summary>생성 시 1회 호출 — 이후 변경되지 않음</summary>
        public void SetItem(ItemData item) => _item = item;
    }
}
