using ProjectER.Character;

namespace ProjectER.Skill
{
    /// <summary>
    /// 패시브 스킬 (T) 인터페이스.
    /// 명시적 발동(Activate) 대신 장착/해제 시점에 효과를 적용/해제한다.
    /// </summary>
    public interface IPassiveSkill
    {
        /// <summary>
        /// 패시브 스킬 효과를 적용한다 (캐릭터 장착 시 1회 호출).
        /// </summary>
        /// <param name="owner">패시브를 보유한 캐릭터</param>
        void OnEquip(CharacterBase owner);

        /// <summary>
        /// 패시브 스킬 효과를 해제한다 (캐릭터 교체/제거 시 호출).
        /// </summary>
        /// <param name="owner">패시브를 보유했던 캐릭터</param>
        void OnUnequip(CharacterBase owner);
    }
}
