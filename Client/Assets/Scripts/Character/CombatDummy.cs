using ProjectER.Character.State;
using UnityEngine;

namespace ProjectER.Character
{
    /// <summary>
    /// 전투 테스트용 더미 타깃. CharacterBase를 상속해 IDamageable로 동작한다.
    /// NavMeshAgent 없이 제자리에서 피격만 받으며, 빈사/사망 시 색으로 표시한다.
    /// 그레이박스 테스트 전용 — 실제 전투 대상(몬스터)은 별도 구현 예정.
    /// </summary>
    public class CombatDummy : CharacterBase
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _normalColor = Color.red;
        [SerializeField] private Color _downedColor = new(0.35f, 0f, 0f);

        // ⚠️ GC 주의: MaterialPropertyBlock은 1회 생성 후 재사용 (프레임마다 new 금지)
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        protected override void Awake()
        {
            base.Awake();

            // 상태 등록 — 더미는 NavMeshAgent가 없으므로 null 전달 (각 상태가 agent null을 허용)
            StateMachine.RegisterState(new IdleState(null));
            StateMachine.RegisterState(new DownedState(null));
            StateMachine.RegisterState(new DeadState(null));
            StateMachine.ChangeState(CharacterState.Idle);

            _mpb = new MaterialPropertyBlock();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnStateChanged += HandleStateChanged;
            ApplyColor(_normalColor);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnStateChanged -= HandleStateChanged;
        }

        // 빈사/사망 시 어둡게, 그 외엔 기본색으로 표시
        private void HandleStateChanged(CharacterState state)
        {
            bool down = state == CharacterState.Downed || state == CharacterState.Dead;
            ApplyColor(down ? _downedColor : _normalColor);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
