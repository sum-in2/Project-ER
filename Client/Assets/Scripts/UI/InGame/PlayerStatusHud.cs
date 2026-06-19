using ProjectER.Character;
using ProjectER.Character.State;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 플레이어 체력/상태를 표시하는 인게임 HUD (테스트용).
    /// CharacterBase의 OnHpChanged/OnStateChanged를 구독해 갱신한다.
    /// </summary>
    public class PlayerStatusHud : MonoBehaviour
    {
        [SerializeField] private CharacterBase _player;
        [SerializeField] private Image _hpFill; // Image.type = Filled (Horizontal)
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _stateText;

        private void OnEnable()
        {
            // 빌더가 참조를 연결하지 못한 경우 대비한 런타임 폴백 (테스트 HUD 한정)
            if (_player == null)
                _player = FindFirstObjectByType<CharacterBase>();

            if (_player == null) return;

            _player.OnHpChanged += HandleHpChanged;
            _player.OnStateChanged += HandleStateChanged;

            // 구독 시점의 현재 값으로 초기 표시
            HandleHpChanged(_player.CurrentHp, _player.MaxHp);
            HandleStateChanged(_player.CurrentState);
        }

        private void OnDisable()
        {
            if (_player == null) return;

            _player.OnHpChanged -= HandleHpChanged;
            _player.OnStateChanged -= HandleStateChanged;
        }

        private void HandleHpChanged(float current, float max)
        {
            if (_hpFill != null)
                _hpFill.fillAmount = max > 0f ? current / max : 0f;

            if (_hpText != null)
                _hpText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void HandleStateChanged(CharacterState state)
        {
            if (_stateText != null)
                _stateText.text = $"State: {state}";
        }
    }
}
