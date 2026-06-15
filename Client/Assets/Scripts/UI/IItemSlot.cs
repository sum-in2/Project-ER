using ProjectER.Data;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템을 표시하는 슬롯의 공통 인터페이스.
    /// ItemDragHandler가 이 인터페이스를 통해 현재 아이템과 드래그 가능 여부를 조회한다.
    /// </summary>
    public interface IItemSlot
    {
        /// <summary>현재 슬롯에 표시 중인 아이템 (비어있으면 null)</summary>
        ItemData CurrentItem { get; }

        /// <summary>드래그로 이 슬롯의 아이템을 꺼낼 수 있는지 여부</summary>
        bool IsDraggable { get; }
    }
}
