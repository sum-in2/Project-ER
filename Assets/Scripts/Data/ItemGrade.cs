namespace ProjectER.Data
{
    /// <summary>
    /// 아이템 등급 — BSER itemGrade 값과 1:1 대응
    /// 순서가 곧 정렬 우선순위 (낮을수록 낮은 등급)
    /// </summary>
    public enum ItemGrade
    {
        Common   = 0, // 일반
        Uncommon = 1, // 고급
        Rare     = 2, // 희귀
        Epic     = 3, // 영웅
        Legend   = 4, // 전설
        Mythic   = 5, // 초월
    }
}
