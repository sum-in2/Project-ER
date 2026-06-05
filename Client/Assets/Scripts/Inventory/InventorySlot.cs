using ProjectER.Data;

namespace ProjectER.Inventory
{
    /// <summary>
    /// 가방 슬롯 하나 — 아이템 참조 + 수량
    /// </summary>
    public class InventorySlot
    {
        public ItemData Item   { get; private set; }
        public int      Amount { get; private set; }

        public bool IsEmpty => Item == null;

        public void Set(ItemData item, int amount)
        {
            Item   = item;
            Amount = amount;
        }

        public void AddAmount(int delta)
        {
            Amount += delta;
        }

        public void Clear()
        {
            Item   = null;
            Amount = 0;
        }
    }
}
