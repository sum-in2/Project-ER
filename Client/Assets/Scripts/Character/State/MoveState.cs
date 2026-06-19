using UnityEngine;
using UnityEngine.AI;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 이동 상태. NavMeshAgent를 직접 구동한다 (이동 책임을 상태가 가짐 — 단일책임).
    /// 목적지 도착 시 Idle로 전환한다.
    /// </summary>
    public sealed class MoveState : ICharacterState
    {
        private readonly NavMeshAgent _agent;
        private readonly CharacterStateMachine _machine;

        private Vector3 _destination;

        public CharacterState Id => CharacterState.Move;

        public MoveState(NavMeshAgent agent, CharacterStateMachine machine)
        {
            _agent = agent;
            _machine = machine;
        }

        // 목적지를 설정하고 즉시 경로를 적용한다 (PlayerController가 우클릭 시 호출).
        // 이미 Move 상태일 때 재이동해도 ChangeState가 무시되므로, 여기서 직접 경로를 갱신한다.
        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            ApplyPath();
        }

        public void Enter()
        {
            ApplyPath();
        }

        private void ApplyPath()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            // 이동 중 새 목적지가 들어오면 기존 경로를 즉시 버리고 새 경로로 전환
            _agent.isStopped = false;
            _agent.ResetPath();
            _agent.SetDestination(_destination);
        }

        public void Tick()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            // 경로 계산이 끝났고 남은 거리가 정지 거리 이내면 도착으로 판정
            if (_agent.pathPending) return;
            if (!_agent.hasPath) return; // 아직 경로가 없으면(계산 직후) 도착으로 오판하지 않음
            if (_agent.remainingDistance > _agent.stoppingDistance) return;

            _machine.ChangeState(CharacterState.Idle);
        }

        public void Exit()
        {
        }
    }
}
