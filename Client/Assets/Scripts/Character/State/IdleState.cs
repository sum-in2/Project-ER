using UnityEngine.AI;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 정지 상태. 입력 대기. 진입 시 잔여 경로를 비우고 에이전트를 재가동한다.
    /// </summary>
    public sealed class IdleState : ICharacterState
    {
        private readonly NavMeshAgent _agent;

        public CharacterState Id => CharacterState.Idle;

        public IdleState(NavMeshAgent agent)
        {
            _agent = agent;
        }

        public void Enter()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.ResetPath();
            }
        }

        public void Tick()
        {
        }

        public void Exit()
        {
        }
    }
}
