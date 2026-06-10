using ProjectER.Character;

namespace ProjectER.Skill
{
    /// <summary>
    /// 모든 스킬(액티브/무기/전술)이 공유하는 공통 인터페이스.
    /// 패시브 스킬은 발동 개념이 없으므로 <see cref="IPassiveSkill"/>로 별도 분리한다.
    /// </summary>
    public interface ISkill
    {
        /// <summary>
        /// 스킬 쿨다운 시간 (초).
        /// </summary>
        float Cooldown { get; }

        /// <summary>
        /// 현재 사용 가능한 상태인지 여부 (쿨다운 종료 여부).
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 스킬을 발동한다.
        /// </summary>
        /// <param name="caster">스킬을 시전하는 캐릭터</param>
        void Activate(CharacterBase caster);
    }
}
