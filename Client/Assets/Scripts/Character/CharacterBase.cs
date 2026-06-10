using ProjectER.Combat;
using UnityEngine;

namespace ProjectER.Character
{
    /// <summary>
    /// 모든 캐릭터(플레이어/몬스터)의 공통 베이스.
    /// TODO: CharacterData(SO) 스탯 연동, CharacterState 상태머신은 추후 구현 예정.
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHp = 100f;

        private float _currentHp;

        public float MaxHp => _maxHp;
        public float CurrentHp => _currentHp;

        protected virtual void Awake()
        {
            _currentHp = _maxHp;
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        public virtual void TakeDamage(float amount)
        {
            // TODO: 방어력 계산식, 피격 이벤트(OnDamaged) 발생은 전투 시스템 구현 시 추가
            _currentHp = Mathf.Max(0f, _currentHp - amount);

            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        public virtual void Die()
        {
            // TODO: 사망 처리(애니메이션, 비활성화, 아이템 드롭 등)는 추후 구현 예정
        }
    }
}
