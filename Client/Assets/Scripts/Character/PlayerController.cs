using ProjectER.Character.State;
using ProjectER.Combat;
using ProjectER.Data;
using ProjectER.Interaction;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ProjectER.Character
{
    /// <summary>
    /// 플레이어가 조작하는 캐릭터.
    /// 우클릭 → Raycast 결과에 따라 공격(IDamageable) > 상호작용(IInteractable) > 이동(NavMesh) 순으로 분기.
    /// 이동은 상태머신의 MoveState가 NavMeshAgent를 직접 구동한다.
    /// TODO: 액티브(Q/W/E/R)·무기(D)·전술(F)·패시브(T) 스킬 슬롯 연동은 추후 구현 예정.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : CharacterBase
    {
        [SerializeField] private CharacterData _characterData;

        private const float RaycastMaxDistance = 100f;

        // 클릭투무브 시 방향 전환을 즉각적으로 만들기 위한 값 (가속도/회전속도를 사실상 무제한으로)
        private const float InstantAcceleration = 999f;
        private const float InstantAngularSpeed = 999f;

        // 상호작용 가능 거리 (캐릭터 콜라이더와 거의 맞닿아야 할 정도의 좁은 범위)
        private const float InteractRange = 1.2f;

        private NavMeshAgent _agent;
        private Camera _mainCamera;

        // 이동 명령 시 목적지를 전달하기 위해 MoveState 인스턴스를 직접 보유
        private MoveState _moveState;

        // 상호작용 대상으로 이동 중일 때, 도착 시 실행할 상호작용 정보
        private IInteractable _pendingInteractable;
        private Transform _pendingInteractTarget;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _agent);

            // 상태 등록 (이동을 담당하는 MoveState는 NavMeshAgent를 직접 구동)
            _moveState = new MoveState(_agent, StateMachine);
            StateMachine.RegisterState(new IdleState(_agent));
            StateMachine.RegisterState(_moveState);
            StateMachine.RegisterState(new DownedState(_agent));
            StateMachine.RegisterState(new DeadState(_agent));
            StateMachine.ChangeState(CharacterState.Idle);
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_agent == null) return;
            _agent.acceleration = InstantAcceleration;
            _agent.angularSpeed = InstantAngularSpeed;

            if (_characterData == null) return;
            _agent.speed = _characterData.MoveSpeed;
            _agent.stoppingDistance = _characterData.StoppingDistance;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void Update()
        {
            // 상태머신 Tick (MoveState 도착 판정 등)
            base.Update();

            // 빈사·사망 중에는 우클릭 입력(이동·공격·상호작용)을 받지 않는다
            if (!CanAcceptInput()) return;

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                HandleRightClick();

            TryResolvePendingInteraction();
        }

        private bool CanAcceptInput()
        {
            return CurrentState != CharacterState.Downed && CurrentState != CharacterState.Dead;
        }

        private void HandleRightClick()
        {
            if (_mainCamera == null || _agent == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, RaycastMaxDistance)) return;

            // 우클릭 입력 처리 우선순위: 공격 대상 > 상호작용 대상 > 이동
            if (hit.collider.TryGetComponent<IDamageable>(out _))
            {
                // TODO: 기본 공격은 전투 시스템 구현 시 추가
                _pendingInteractable = null;
                _pendingInteractTarget = null;
                return;
            }

            // GetComponentInParent: Collider가 자식 오브젝트에 있어도 부모의 IInteractable을 찾는다
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // 상호작용 대상이 사거리 밖이면 우선 대상 위치로 이동, 도착 후 자동으로 상호작용 실행
                _pendingInteractable = interactable;
                _pendingInteractTarget = hit.collider.transform;
                MoveTo(_pendingInteractTarget.position);
                return;
            }

            _pendingInteractable = null;
            _pendingInteractTarget = null;
            MoveTo(hit.point);
        }

        private void MoveTo(Vector3 destination)
        {
            // 목적지를 MoveState에 전달하고 이동 상태로 전환 (실제 NavMeshAgent 구동은 MoveState가 담당)
            _moveState.SetDestination(destination);
            StateMachine.ChangeState(CharacterState.Move);
        }

        private void TryResolvePendingInteraction()
        {
            if (_pendingInteractable == null || _pendingInteractTarget == null) return;

            float distance = Vector3.Distance(transform.position, _pendingInteractTarget.position);
            if (distance > InteractRange) return;

            IInteractable interactable = _pendingInteractable;
            _pendingInteractable = null;
            _pendingInteractTarget = null;

            _agent.ResetPath();
            interactable.Interact(this);
        }
    }
}
