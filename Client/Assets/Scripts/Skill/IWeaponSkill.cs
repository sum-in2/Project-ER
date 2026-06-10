using ProjectER.Data;

namespace ProjectER.Skill
{
    /// <summary>
    /// 무기 스킬 (D 키) 인터페이스. 장착 무기 종류에 따라 교체된다.
    /// </summary>
    public interface IWeaponSkill : ISkill
    {
        /// <summary>
        /// 이 스킬이 속한 무기 종류.
        /// </summary>
        WeaponType WeaponType { get; }
    }
}
