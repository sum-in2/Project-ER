namespace ProjectER.Crafting
{
    /// <summary>
    /// 인벤토리 외 추가 재료 공급원 (예: 열린 루트박스).
    /// 조합 가능 여부 계산과 재료 소모(인벤토리 다음 순위)에 사용한다.
    /// </summary>
    public interface IMaterialSource
    {
        // 해당 아이템 보유 수량
        int GetCount(string itemId);

        // 해당 아이템을 amount만큼 차감 (보유량 이내)
        void Remove(string itemId, int amount);
    }
}
