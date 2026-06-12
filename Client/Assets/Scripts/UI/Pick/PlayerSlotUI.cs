using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 우측 하단 플레이어 슬롯 1개 — 플레이어 이름 + 선택한 실험체 Mini 아이콘
    /// 빈 슬롯은 EMPTY 표시
    /// </summary>
    public class PlayerSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _playerNameText;
        [SerializeField] private Image      _characterIcon;
        [SerializeField] private GameObject _emptyIndicator;
        [SerializeField] private Material   _iconMaterial; // 디폴트 스프라이트 머터리얼 (Sprites-Default) — 비어있으면 머터리얼 변경 안 함

        private void Awake()
        {
            if (_iconMaterial != null && _characterIcon != null)
                _characterIcon.material = _iconMaterial;

            SetEmpty();
        }

        /// <summary>빈 슬롯으로 표시</summary>
        public void SetEmpty()
        {
            if (_playerNameText == null || _characterIcon == null) return; // 에디터 씬 빌더에서 필드 연결 전 AddComponent 시점 방지

            _playerNameText.text   = string.Empty;
            _characterIcon.enabled = false;
            _characterIcon.sprite  = null;

            if (_emptyIndicator != null)
                _emptyIndicator.SetActive(true);
        }

        /// <summary>플레이어 이름 및 선택한 실험체 아이콘으로 갱신</summary>
        public void SetPlayer(string playerName, CharacterData character)
        {
            if (_playerNameText == null || _characterIcon == null) return; // 에디터 씬 빌더에서 필드 연결 전 AddComponent 시점 방지

            _playerNameText.text = playerName;

            bool hasIcon = character != null && character.PortraitMini != null;
            _characterIcon.enabled = hasIcon;
            _characterIcon.sprite  = hasIcon ? character.PortraitMini : null;
            _characterIcon.color   = Color.white; // 빌더에서 Color.clear로 생성되어 스프라이트가 보이지 않는 문제 방지

            if (_emptyIndicator != null)
                _emptyIndicator.SetActive(false);
        }
    }
}
