namespace ProjectER.Data
{
    /// <summary>
    /// 아이템 분류 타입
    /// </summary>
    public enum ItemType
    {
        None,

        // 재료
        Material,       // 기본 재료 (나무, 돌, 가죽 등)

        // 음식
        Food,           // 음식 (취식 시 효과 발동)

        // 장비
        Weapon,         // 무기 (종류 다양 — 추후 세분화 가능)
        Chest,          // 방어구 (옷 / 몸통)
        Helmet,         // 방어구 (머리)
        Arms,           // 방어구 (팔)
        Shoes,          // 방어구 (신발)

        // 소비 / 사용 가능 아이템
        Consumable,     // 즉시 사용 아이템 (포션 등)
    }
}
