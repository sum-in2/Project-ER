using System;
using System.Collections.Generic;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 캐릭터 상태 전환을 관리하는 순수 C# 클래스 (MonoBehaviour 아님).
    /// CharacterBase가 소유하고 Update에서 Tick을 위임한다.
    /// </summary>
    public sealed class CharacterStateMachine
    {
        private readonly Dictionary<CharacterState, ICharacterState> _states =
            new Dictionary<CharacterState, ICharacterState>();

        public ICharacterState Current { get; private set; }

        // 상태 변경 시 새 상태 식별자를 전달 (애니메이션/UI/네트워크 동기화 구독용)
        public event Action<CharacterState> OnStateChanged;

        public void RegisterState(ICharacterState state)
        {
            if (state == null) return;
            _states[state.Id] = state;
        }

        public void ChangeState(CharacterState next)
        {
            // 같은 상태로의 재진입은 무시
            if (Current != null && Current.Id == next) return;
            if (!_states.TryGetValue(next, out ICharacterState state)) return;

            Current?.Exit();
            Current = state;
            Current.Enter();
            OnStateChanged?.Invoke(next);
        }

        public void Tick()
        {
            Current?.Tick();
        }
    }
}
