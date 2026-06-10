using ProjectER.Character;

namespace ProjectER.Interaction
{
    /// <summary>
    /// 우클릭 상호작용이 가능한 월드 오브젝트가 구현하는 인터페이스.
    /// 월드 아이템 줍기도 이 인터페이스의 구현체에서 처리한다 (별도 IPickupable 없음).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 상호작용을 수행한다 (예: 아이템 줍기, 문 열기, NPC 대화 등).
        /// </summary>
        /// <param name="interactor">상호작용을 시도한 플레이어</param>
        void Interact(PlayerController interactor);
    }
}
