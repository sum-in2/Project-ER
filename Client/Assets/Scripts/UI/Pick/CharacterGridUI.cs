using System;
using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 실험체 선택 그리드 — 역할군 필터/검색/이름 정렬 결과를 슬롯에 반영
    /// </summary>
    public class CharacterGridUI : MonoBehaviour
    {
        [SerializeField] private CharacterDatabase      _characterDatabase;
        [SerializeField] private CharacterSelectSlotUI  _slotPrefab;
        [SerializeField] private Transform              _gridContent;

        private readonly List<CharacterSelectSlotUI> _slots = new();
        private readonly List<CharacterSelectSlotUI> _visibleSlots = new(); // GC 주의: Refresh()에서만 재사용

        private CharacterArcheType _roleFilter = CharacterArcheType.None; // None = 전체
        private string _searchText = string.Empty;
        private bool   _sortDescending;
        private CharacterData _selectedCharacter;

        public event Action<CharacterData> OnCharacterSelected;

        private void Awake()
        {
            // 에디터 씬 빌더에서 필드 연결 전 AddComponent 시점 방지
            if (_characterDatabase == null || _slotPrefab == null || _gridContent == null) return;

            BuildSlots();
        }

        /// <summary>역할군 필터 변경 (None = 전체)</summary>
        public void SetRoleFilter(CharacterArcheType type)
        {
            _roleFilter = type;
            Refresh();
        }

        /// <summary>이름 검색어 변경</summary>
        public void SetSearchText(string text)
        {
            _searchText = text ?? string.Empty;
            Refresh();
        }

        /// <summary>이름 정렬 방향 변경</summary>
        public void SetSortDescending(bool descending)
        {
            _sortDescending = descending;
            Refresh();
        }

        /// <summary>필터/정렬 조건에 맞는 슬롯만 표시하고 이름순으로 재배치</summary>
        public void Refresh()
        {
            _visibleSlots.Clear();

            foreach (CharacterSelectSlotUI slot in _slots)
            {
                bool visible = MatchesFilter(slot.Character);
                if (visible)
                    _visibleSlots.Add(slot);
                else
                    slot.gameObject.SetActive(false);
            }

            _visibleSlots.Sort(CompareByName);

            for (int i = 0; i < _visibleSlots.Count; i++)
            {
                _visibleSlots[i].gameObject.SetActive(true);
                _visibleSlots[i].transform.SetSiblingIndex(i);
            }
        }

        private void BuildSlots()
        {
#if UNITY_EDITOR
            // Play 모드 진입 시 미리보기 슬롯이 Hierarchy에서 선택된 상태면
            // 파괴 후 Inspector가 끊긴 참조를 그리려다 예외(SerializedObjectNotCreatableException 등) 발생
            if (UnityEditor.Selection.activeGameObject != null &&
                UnityEditor.Selection.activeGameObject.transform.IsChildOf(_gridContent))
                UnityEditor.Selection.activeObject = null;
#endif

            // 에디터 빌더가 미리 배치한 미리보기 슬롯 제거 후 런타임에 새로 생성
            for (int i = _gridContent.childCount - 1; i >= 0; i--)
                Destroy(_gridContent.GetChild(i).gameObject);

            foreach (CharacterData character in _characterDatabase.Characters)
            {
                if (character == null) continue;

                CharacterSelectSlotUI slot = Instantiate(_slotPrefab, _gridContent);
                slot.Setup(character, HandleSlotClicked);
                _slots.Add(slot);
            }

            Refresh();
        }

        private bool MatchesFilter(CharacterData character)
        {
            if (_roleFilter != CharacterArcheType.None &&
                character.ArcheType1 != _roleFilter && character.ArcheType2 != _roleFilter)
                return false;

            if (!string.IsNullOrEmpty(_searchText) &&
                character.DisplayName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0 &&
                character.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        private int CompareByName(CharacterSelectSlotUI a, CharacterSelectSlotUI b)
        {
            int result = string.CompareOrdinal(a.Character.DisplayName, b.Character.DisplayName);
            return _sortDescending ? -result : result;
        }

        private void HandleSlotClicked(CharacterData character)
        {
            if (_selectedCharacter == character) return;

            foreach (CharacterSelectSlotUI slot in _slots)
                slot.SetSelected(slot.Character == character);

            _selectedCharacter = character;
            OnCharacterSelected?.Invoke(character);
        }
    }
}
