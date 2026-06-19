namespace ProjectER.Character.State
{
    /// <summary>
    /// 다형 상태 한 개. if/else 타입 분기 대신 상태별 구현체로 행동을 분리한다.
    /// </summary>
    public interface ICharacterState
    {
        CharacterState Id { get; }

        // 상태 진입 시 1회 호출
        void Enter();

        // 매 프레임 호출 (전환 조건 판정 포함)
        void Tick();

        // 상태 이탈 시 1회 호출
        void Exit();
    }
}
