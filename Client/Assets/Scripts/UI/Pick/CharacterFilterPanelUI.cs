using System;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 캐릭터 그리드 상단 필터 영역 — 역할군 필터, 이름순 정렬 토글, 이름 검색
    /// </summary>
    public class CharacterFilterPanelUI : MonoBehaviour
    {
        // 역할군 필터 버튼 7개: 전체(None) + Warrior/Tanker/Assasin/Marksman/Mage/Supporter
        [SerializeField] private Button[] _roleButtons;
        [SerializeField] private Button   _sortButton;
        [SerializeField] private TMP_InputField _searchInput;

        private static readonly CharacterArcheType[] RoleOrder =
        {
            CharacterArcheType.None, // 전체
            CharacterArcheType.Warrior,
            CharacterArcheType.Tanker,
            CharacterArcheType.Assasin,
            CharacterArcheType.Marksman,
            CharacterArcheType.Mage,
            CharacterArcheType.Supporter,
        };

        // ⚠️ GC 주의: 버튼별 클로저 핸들러 — Awake에서 1회만 생성
        private UnityAction[] _roleHandlers;
        private bool _sortDescending;

        public event Action<CharacterArcheType> OnRoleFilterChanged;
        public event Action<bool>               OnSortChanged;
        public event Action<string>             OnSearchTextChanged;

        private void Awake()
        {
            if (_roleButtons == null) return; // 에디터 씬 빌더에서 필드 연결 전 AddComponent 시점 방지

            _roleHandlers = new UnityAction[_roleButtons.Length];
            for (int i = 0; i < _roleButtons.Length; i++)
            {
                int index = i;
                _roleHandlers[i] = () => OnRoleFilterChanged?.Invoke(RoleOrder[index]);
                _roleButtons[i].onClick.AddListener(_roleHandlers[i]);
            }

            _sortButton.onClick.AddListener(HandleSortClicked);
            _searchInput.onValueChanged.AddListener(HandleSearchChanged);
        }

        private void OnDestroy()
        {
            if (_roleHandlers == null) return;

            for (int i = 0; i < _roleButtons.Length; i++)
                _roleButtons[i].onClick.RemoveListener(_roleHandlers[i]);

            _sortButton.onClick.RemoveListener(HandleSortClicked);
            _searchInput.onValueChanged.RemoveListener(HandleSearchChanged);
        }

        private void HandleSortClicked()
        {
            _sortDescending = !_sortDescending;
            OnSortChanged?.Invoke(_sortDescending);
        }

        private void HandleSearchChanged(string text) => OnSearchTextChanged?.Invoke(text);
    }
}
