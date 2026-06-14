using ProjectER.Data;
using ProjectER.Network;
using ProjectER.UI.Pick;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Scene
{
    /// <summary>
    /// 03_PickScene 진행 제어 — 실험체 선택, 플레이어 슬롯, 선택 제한시간(서버 동기화) 처리
    /// </summary>
    public class PickSceneController : MonoBehaviour
    {
        [SerializeField] private CharacterGridUI           _characterGrid;
        [SerializeField] private CharacterFilterPanelUI    _filterPanel;
        [SerializeField] private SelectedCharacterPanelUI  _selectedPanel;
        [SerializeField] private PlayerSlotUI[]            _playerSlots; // 3슬롯 고정
        [SerializeField] private PickTimerUI               _timer;
        [SerializeField] private Button                    _testInGameButton; // 테스트용: 인게임 씬 즉시 진입

        private const int LocalPlayerSlotIndex = 0;
        private const string SceneLobby  = "02_LobbyScene";
        private const string SceneInGame = "04_InGameScene";

        private string _localPlayerName;

        private void Start()
        {
            _localPlayerName = NetworkClient.Instance != null
                ? $"Player #{NetworkClient.Instance.SessionId}"
                : "Player";

            foreach (PlayerSlotUI slot in _playerSlots)
                slot.SetEmpty();

            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, null);

            // 픽 제한시간은 서버가 관리 (S2C_PickStarted로 받은 시작 시각/제한시간 기준 남은 시간)
            float remaining = NetworkClient.Instance != null
                ? NetworkClient.Instance.PickPhaseRemainingSeconds
                : 30f;
            _timer.StartTimer(remaining);
        }

        private void OnEnable()
        {
            _characterGrid.OnCharacterSelected += HandleCharacterSelected;
            _filterPanel.OnRoleFilterChanged   += _characterGrid.SetRoleFilter;
            _filterPanel.OnSortChanged         += _characterGrid.SetSortDescending;
            _filterPanel.OnSearchTextChanged   += _characterGrid.SetSearchText;
            _timer.OnTimeExpired               += HandleTimeExpired;
            _testInGameButton.onClick.AddListener(HandleTestInGameButtonClicked);

            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnPickDodged += HandlePickDodged;
        }

        private void OnDisable()
        {
            _characterGrid.OnCharacterSelected -= HandleCharacterSelected;
            _filterPanel.OnRoleFilterChanged   -= _characterGrid.SetRoleFilter;
            _filterPanel.OnSortChanged         -= _characterGrid.SetSortDescending;
            _filterPanel.OnSearchTextChanged   -= _characterGrid.SetSearchText;
            _timer.OnTimeExpired               -= HandleTimeExpired;
            _testInGameButton.onClick.RemoveListener(HandleTestInGameButtonClicked);

            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnPickDodged -= HandlePickDodged;
        }

        private void HandleCharacterSelected(CharacterData character)
        {
            _selectedPanel.SetCharacter(character);
            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, character);

            NetworkClient.Instance?.SelectCharacter(character.BserCode);
        }

        private void HandleTimeExpired()
        {
            // TODO: 루트 선택(30초) 단계로 진행 또는 자동 확정 처리
            Debug.Log("[PickSceneController] 캐릭터 선택 제한시간 종료");
        }

        private void HandlePickDodged()
        {
            _timer.StopTimer();
            Debug.Log("[PickSceneController] 닷지로 매치 취소 → 로비로 복귀");
            SceneManager.LoadScene(SceneLobby);
        }

        /// <summary>테스트용: 인게임 씬으로 즉시 진입 (정식 흐름의 2단계/서버 트리거를 대체)</summary>
        private void HandleTestInGameButtonClicked()
        {
            _timer.StopTimer();
            Debug.Log("[PickSceneController] 테스트 버튼 → 인게임 씬 진입");
            SceneManager.LoadScene(SceneInGame);
        }
    }
}
