using System;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 캐릭터 선택 그리드의 슬롯 1개 — 실험체 Mini 아이콘 + 이름 표시
    /// </summary>
    public class CharacterSelectSlotUI : MonoBehaviour
    {
        [SerializeField] private Image    _portraitImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private GameObject _selectedHighlight;
        [SerializeField] private Material _portraitMaterial; // 디폴트 스프라이트 머터리얼 (Sprites-Default) — 비어있으면 머터리얼 변경 안 함
        [SerializeField] private Material _textMaterial;     // TMP 폰트 머터리얼 — 비어있으면 머터리얼 변경 안 함

        private Button       _button;
        private CharacterData _character;
        private Action<CharacterData> _onClicked;

        public CharacterData Character => _character;

        private void Awake()
        {
            TryGetComponent(out _button);
            _button?.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(HandleClick);
        }

        /// <summary>실험체 데이터로 슬롯 갱신, 클릭 콜백 등록</summary>
        public void Setup(CharacterData character, Action<CharacterData> onClicked)
        {
            _character = character;
            _onClicked  = onClicked;

            _portraitImage.sprite  = character.PortraitMini;
            _portraitImage.enabled = character.PortraitMini != null;
            _nameText.text         = character.DisplayName;

            if (_portraitMaterial != null)
                _portraitImage.material = _portraitMaterial;

            if (_textMaterial != null)
                _nameText.fontSharedMaterial = _textMaterial;

            if (_selectedHighlight != null && _portraitMaterial != null
                && _selectedHighlight.TryGetComponent(out Image highlightImage))
                highlightImage.material = _portraitMaterial;

            SetSelected(false);
        }

        /// <summary>선택 강조 표시 토글</summary>
        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(selected);
        }

        private void HandleClick() => _onClicked?.Invoke(_character);
    }
}
