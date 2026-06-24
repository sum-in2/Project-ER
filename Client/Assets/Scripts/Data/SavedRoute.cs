using ProjectER.Inventory;

namespace ProjectER.Data
{
    /// <summary>
    /// 저장된 루트 1개 — 부위별 목표 장비 5종(무기→옷→머리→팔→신발).
    /// 계정 단위로 저장/관리될 예정(추후 계정 해시 외래키 기반 별도 DB).
    /// 현재는 런타임 모델로만 사용하며 ItemData 참조를 직접 보유한다.
    /// (네트워크 전송 시에는 ItemData.BserCode 5개로 직렬화 — C2S_SelectRoute, 추후)
    /// </summary>
    public sealed class SavedRoute
    {
        // EquipmentSlotType 순서(Weapon=0…Shoes=4)로 인덱싱. 빈 부위는 null.
        private readonly ItemData[] _items;

        public string DisplayName { get; }

        public SavedRoute(string displayName, ItemData[] items)
        {
            DisplayName = displayName;
            _items = items ?? new ItemData[EquipmentSlotCount];
        }

        public static int EquipmentSlotCount => 5; // EquipmentSlotType 개수

        /// <summary>부위별 목표 아이템 조회. 비어있으면 null.</summary>
        public ItemData GetItem(EquipmentSlotType slotType)
        {
            int index = (int)slotType;
            if (index < 0 || index >= _items.Length) return null;
            return _items[index];
        }

        /// <summary>부위 순서대로의 아이템 배열(빈 부위 null 포함). 읽기 전용 용도.</summary>
        public ItemData[] Items => _items;
    }
}
