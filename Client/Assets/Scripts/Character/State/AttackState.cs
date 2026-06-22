using ProjectER.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectER.Character.State
{
    /// <summary>
    /// 기본 공격 상태. 사거리 안의 대상을 공격 간격마다 타격한다.
    /// - 진입 시 에이전트를 정지하고 즉시 1회 공격 가능하도록 쿨다운을 비운다.
    /// - 대상이 사라지거나(파괴) 사거리를 벗어나면 Idle로 복귀한다 (자동 추격은 TODO).
    /// 데미지는 현재 공격력 고정값(단순 루프). 방어력/치명타/증폭 공식은 IDamageCalculator로 분리 예정(TODO).
    /// </summary>
    public sealed class AttackState : ICharacterState
    {
        private readonly NavMeshAgent _agent;
        private readonly CharacterStateMachine _machine;
        private readonly Transform _owner;

        private float _damage;          // 1회 공격 피해 (현재: 공격력 고정)
        private float _attackRange = 2f; // 공격 사거리
        private float _attackInterval = 1f; // 공격 간격(초) = 1 / 공격속도

        private IDamageable _target;
        private Transform _targetTransform;
        private float _cooldownTimer;

        public CharacterState Id => CharacterState.Attack;

        public AttackState(NavMeshAgent agent, CharacterStateMachine machine, Transform owner)
        {
            _agent = agent;
            _machine = machine;
            _owner = owner;
        }

        // 공격 스탯 주입 (PlayerController가 CharacterData 기반으로 Start에서 설정)
        public void Configure(float damage, float attackRange, float attackInterval)
        {
            _damage = damage;
            _attackRange = attackRange;
            _attackInterval = attackInterval > 0f ? attackInterval : 1f;
        }

        // 공격 대상 지정 (PlayerController가 사거리 진입 후 호출)
        public void SetTarget(IDamageable target, Transform targetTransform)
        {
            _target = target;
            _targetTransform = targetTransform;
        }

        public void Enter()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.isStopped = true;
            }

            // 진입 즉시 첫 타격이 나가도록 쿨다운을 비운다
            _cooldownTimer = 0f;
        }

        public void Tick()
        {
            // Transform이 null이면 대상 GameObject가 파괴된 것 (Unity 오버로드 null 비교)
            if (_targetTransform == null)
            {
                _machine.ChangeState(CharacterState.Idle);
                return;
            }

            float distance = Vector3.Distance(_owner.position, _targetTransform.position);
            if (distance > _attackRange)
            {
                // TODO: 사거리 이탈 시 자동 추격 (현재는 정지 후 Idle 복귀, 재클릭 필요)
                _machine.ChangeState(CharacterState.Idle);
                return;
            }

            FaceTarget();

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            _cooldownTimer = _attackInterval;
            // TODO: 공격 모션/판정 타이밍, 데미지 계산기(방어력/치명타/증폭) 연동
            _target.TakeDamage(_damage);
        }

        public void Exit()
        {
            _target = null;
            _targetTransform = null;
        }

        private void FaceTarget()
        {
            Vector3 direction = _targetTransform.position - _owner.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            _owner.rotation = Quaternion.LookRotation(direction);
        }
    }
}
