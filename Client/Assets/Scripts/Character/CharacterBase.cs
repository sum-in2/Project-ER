using System;
using ProjectER.Character.State;
using ProjectER.Combat;
using UnityEngine;

namespace ProjectER.Character
{
    /// <summary>
    /// 모든 캐릭터(플레이어/몬스터)의 공통 베이스.
    /// 상태머신(CharacterStateMachine)을 소유하며, 구체 상태 등록·초기 상태 지정은 자식이 담당한다.
    /// TODO: CharacterData(SO) 스탯 연동.
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHp = 100f;

        // 부활 시 회복할 HP 비율 (0~1). 빈사 → 부활 기믹에서 사용.
        [SerializeField, Range(0f, 1f)] private float _reviveHpRatio = 0.3f;

        private float _currentHp;

        public float MaxHp => _maxHp;
        public float CurrentHp => _currentHp;

        // HP 변경 이벤트 (현재 HP, 최대 HP) — HUD 등 구독용
        public event Action<float, float> OnHpChanged;

        // 상태 변경 이벤트 (새 상태) — StateMachine 변경을 외부로 중계
        public event Action<CharacterState> OnStateChanged;

        // 상태머신은 자식이 RegisterState로 상태를 채운 뒤 사용한다.
        protected CharacterStateMachine StateMachine { get; private set; }

        public CharacterState CurrentState =>
            StateMachine?.Current != null ? StateMachine.Current.Id : CharacterState.Idle;

        protected virtual void Awake()
        {
            _currentHp = _maxHp;
            StateMachine = new CharacterStateMachine();
            StateMachine.OnStateChanged += RaiseStateChanged;
        }

        private void RaiseStateChanged(CharacterState state) => OnStateChanged?.Invoke(state);

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void Update()
        {
            StateMachine?.Tick();
        }

        public virtual void TakeDamage(float amount)
        {
            // 빈사·사망 상태에서는 추가 피해를 받지 않는다 (처형 등 별도 처리는 추후)
            if (CurrentState == CharacterState.Downed || CurrentState == CharacterState.Dead) return;

            // TODO: 방어력 계산식, 피격 이벤트(OnDamaged) 발생은 전투 시스템 구현 시 추가
            _currentHp = Mathf.Max(0f, _currentHp - amount);
            OnHpChanged?.Invoke(_currentHp, _maxHp);

            // 전투 디버그 로그 (그레이박스 검증용 — 전투 시스템 안정화 후 제거 예정)
            Debug.Log($"[전투] {name} 피해 {amount:0.#} → HP {_currentHp:0.#}/{_maxHp:0.#}", this);

            if (_currentHp <= 0f)
            {
                // HP 0 도달 시 즉시 사망이 아니라 빈사로 진입 (부활 가능)
                Debug.Log($"[전투] {name} 빈사(Downed) 진입", this);
                StateMachine?.ChangeState(CharacterState.Downed);
            }
        }

        /// <summary>
        /// 체력 회복. 빈사·사망 상태에서는 무효 (부활은 Revive 사용).
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (amount <= 0f) return;
            if (CurrentState == CharacterState.Downed || CurrentState == CharacterState.Dead) return;

            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            OnHpChanged?.Invoke(_currentHp, _maxHp);
        }

        /// <summary>
        /// 빈사 상태에서 부활. HP를 일부 회복하고 Idle로 복귀한다.
        /// </summary>
        public virtual void Revive()
        {
            if (CurrentState != CharacterState.Downed) return;

            _currentHp = _maxHp * _reviveHpRatio;
            OnHpChanged?.Invoke(_currentHp, _maxHp);
            StateMachine?.ChangeState(CharacterState.Idle);
        }

        /// <summary>
        /// 최종 사망 확정 (빈사에서 부활 실패/처형). IDamageable 계약 구현.
        /// </summary>
        public virtual void Die()
        {
            StateMachine?.ChangeState(CharacterState.Dead);
        }
    }
}
