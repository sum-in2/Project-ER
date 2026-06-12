using ProjectER.Data;
using ProjectER.Network;
using ProjectER.UI.Pick;
using UnityEngine;

namespace ProjectER.Scene
{
    /// <summary>
    /// 03_PickScene 진행 제어 — 실험체 선택, 플레이어 슬롯, 선택 제한시간(30초) 처리
    /// </summary>
    public class PickSceneController : MonoBehaviour
    {
        [SerializeField] private CharacterGridUI           _characterGrid;
        [SerializeField] private CharacterFilterPanelUI    _filterPanel;
        [SerializeField] private SelectedCharacterPanelUI  _selectedPanel;
        [SerializeField] private PlayerSlotUI[]            _playerSlots; // 3슬롯 고정
        [SerializeField] private PickTimerUI               _timer;

        private const int LocalPlayerSlotIndex = 0;

        private string _localPlayerName;

        private void Start()
        {
            _localPlayerName = NetworkClient.Instance != null
                ? $"Player #{NetworkClient.Instance.SessionId}"
                : "Player";

            foreach (PlayerSlotUI slot in _playerSlots)
                slot.SetEmpty();

            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, null);

            _timer.StartTimer();
        }

        private void OnEnable()
        {
            _characterGrid.OnCharacterSelected += HandleCharacterSelected;
            _filterPanel.OnRoleFilterChanged   += _characterGrid.SetRoleFilter;
            _filterPanel.OnSortChanged         += _characterGrid.SetSortDescending;
            _filterPanel.OnSearchTextChanged   += _characterGrid.SetSearchText;
            _timer.OnTimeExpired               += HandleTimeExpired;
        }

        private void OnDisable()
        {
            _characterGrid.OnCharacterSelected -= HandleCharacterSelected;
            _filterPanel.OnRoleFilterChanged   -= _characterGrid.SetRoleFilter;
            _filterPanel.OnSortChanged         -= _characterGrid.SetSortDescending;
            _filterPanel.OnSearchTextChanged   -= _characterGrid.SetSearchText;
            _timer.OnTimeExpired               -= HandleTimeExpired;
        }

        private void HandleCharacterSelected(CharacterData character)
        {
            _selectedPanel.SetCharacter(character);
            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, character);
        }

        private void HandleTimeExpired()
        {
            // TODO: 루트 선택(30초) 단계로 진행 또는 자동 확정 처리
            Debug.Log("[PickSceneController] 캐릭터 선택 제한시간 종료");
        }
    }
}
