using UnityEngine.AI;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 최종 사망 상태. 빈사에서 부활 실패/처형 시 진입한다. 모든 입력·이동 차단.
    /// </summary>
    public sealed class DeadState : ICharacterState
    {
        private readonly NavMeshAgent _agent;

        public CharacterState Id => CharacterState.Dead;

        public DeadState(NavMeshAgent agent)
        {
            _agent = agent;
        }

        public void Enter()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.isStopped = true;
            }
            // TODO: 사망 연출(애니메이션), 오브젝트 비활성화, 아이템 드롭
        }

        public void Tick()
        {
        }

        public void Exit()
        {
        }
    }
}
