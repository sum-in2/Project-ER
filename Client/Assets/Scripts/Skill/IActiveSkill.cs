namespace ProjectER.Skill
{
    /// <summary>
    /// 액티브 스킬 (Q/W/E/R, 스킬 슬롯 1~4) 인터페이스.
    /// </summary>
    public interface IActiveSkill : ISkill
    {
        /// <summary>
        /// 스킬 슬롯 인덱스 (0~3 → Q/W/E/R).
        /// </summary>
        int SlotIndex { get; }
    }
}
