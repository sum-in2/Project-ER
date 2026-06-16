using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템 슬롯 UI 공통 베이스 클래스.
    /// IItemSlot 구현, 등급 테두리색 적용 유틸을 제공한다.
    /// </summary>
    public abstract class BaseItemSlotUI : MonoBehaviour, IItemSlot
    {
        [SerializeField] protected ItemGradeColorConfig _gradeConfig;
        [SerializeField] protected bool                 _isDraggable;

        protected ItemData _currentItem;

        // ── IItemSlot ─────────────────────────────────────────────────
        public ItemData CurrentItem => _currentItem;
        public bool     IsDraggable => _isDraggable;

        // ── 유틸 ──────────────────────────────────────────────────────

        /// <summary>
        /// 테두리/하이라이트용 밝은 등급색을 target Image에 적용한다.
        /// item이 null이거나 _gradeConfig가 없으면 Color.clear.
        /// </summary>
        protected void ApplyGradeColor(Image target, ItemData item)
        {
            if (target == null) return;
            target.color = (item != null && _gradeConfig != null)
                ? _gradeConfig.GetColor(item.ItemGrade)
                : Color.clear;
        }
    }
}
