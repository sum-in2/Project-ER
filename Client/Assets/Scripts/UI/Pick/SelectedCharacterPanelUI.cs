using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 우측 상단 — 현재 선택한 실험체의 Full 초상화와 이름 표시
    /// </summary>
    public class SelectedCharacterPanelUI : MonoBehaviour
    {
        [SerializeField] private Image    _portraitImage;
        [SerializeField] private TMP_Text _nameText;

        private void Awake()
        {
            SetCharacter(null);
        }

        /// <summary>선택한 실험체로 갱신. null이면 빈 상태로 표시</summary>
        public void SetCharacter(CharacterData character)
        {
            if (_portraitImage == null || _nameText == null) return; // 에디터 씬 빌더에서 필드 연결 전 AddComponent 시점 방지

            bool hasCharacter = character != null;

            _portraitImage.enabled = hasCharacter && character.PortraitFull != null;
            _portraitImage.sprite  = hasCharacter ? character.PortraitFull : null;
            _portraitImage.color   = Color.white; // 빌더에서 Color.clear로 생성되어 스프라이트가 보이지 않는 문제 방지
            _nameText.text         = hasCharacter ? character.DisplayName : string.Empty;
        }
    }
}
