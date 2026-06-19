using UnityEngine.AI;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 빈사 상태. HP 0 도달 시 진입하며 부활 기믹으로 복귀 가능하다.
    /// - 공격 불가 (Attack 상태 진입 차단 — 입력단에서 처리)
    /// - 스탯 변경 적용 예정 (이동속도 감소 등) — TODO
    /// - 빈사 전용 스킬셋(Q/W/E/R 키 유지, 스킬 교체) 사용 예정 — TODO
    /// 빈사 중 이동 허용 여부는 미정 (현재는 정지). 부활 성공 시 Idle, 실패/처형 시 Dead.
    /// </summary>
    public sealed class DownedState : ICharacterState
    {
        private readonly NavMeshAgent _agent;

        public CharacterState Id => CharacterState.Downed;

        public DownedState(NavMeshAgent agent)
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
            // TODO: 빈사 스탯 적용, 빈사 전용 스킬셋 교체
        }

        public void Tick()
        {
        }

        public void Exit()
        {
            // TODO: 부활 시 빈사 스탯/스킬셋 원복
        }
    }
}
