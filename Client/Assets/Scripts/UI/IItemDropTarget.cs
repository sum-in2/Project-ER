namespace ProjectER.UI
{
    /// <summary>
    /// 드래그한 아이템을 받을 수 있는 슬롯의 공통 인터페이스.
    /// ItemDragHandler가 드롭 시점에 마우스 포인터 아래에 있는 IItemDropTarget을 찾아 TryDropItem을 호출한다.
    /// </summary>
    public interface IItemDropTarget
    {
        /// <summary>source의 아이템을 이 슬롯에 배치할 수 있으면 처리 후 true 반환</summary>
        bool TryDropItem(IItemSlot source);
    }
}
