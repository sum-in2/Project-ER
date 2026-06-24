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

        // ── 루트 선택(픽 2단계) ─────────────────────────────────────
        [SerializeField] private Button             _confirmButton;        // 실험체 확정 → 루트 선택 단계 진입
        [SerializeField] private GameObject         _characterGridScroll;  // 루트 단계에서 비활성화할 그리드 영역
        [SerializeField] private RouteSelectPanelUI _routePanel;           // 저장된 루트 선택 패널
        [SerializeField] private ItemDatabase       _itemDatabase;         // 더미 루트 합성용 (스텁)
        [SerializeField] private MatchSelectionData _matchSelection;       // 인게임으로 전달할 출전 정보 캐리어

        private const int LocalPlayerSlotIndex = 0;
        private const string SceneLobby  = "02_LobbyScene";
        private const string SceneInGame = "04_InGameScene";

        private string _localPlayerName;
        private CharacterData _selectedCharacter;
        private ISavedRouteSource _routeSource;

        private void Start()
        {
            _localPlayerName = NetworkClient.Instance != null
                ? $"Player #{NetworkClient.Instance.SessionId}"
                : "Player";

            foreach (PlayerSlotUI slot in _playerSlots)
                slot.SetEmpty();

            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, null);

            // 출전 정보 캐리어 초기화 (이전 매치 잔여값 제거)
            _matchSelection?.Clear();

            // 루트 단계는 확인 버튼을 눌러야 진입 — 시작 시 패널 숨김
            _routePanel?.Hide();

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

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
            if (_routePanel != null)
                _routePanel.OnRouteChosen += HandleRouteChosen;

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

            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            if (_routePanel != null)
                _routePanel.OnRouteChosen -= HandleRouteChosen;

            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnPickDodged -= HandlePickDodged;
        }

        private void HandleCharacterSelected(CharacterData character)
        {
            _selectedCharacter = character;
            _selectedPanel.SetCharacter(character);
            _playerSlots[LocalPlayerSlotIndex].SetPlayer(_localPlayerName, character);

            NetworkClient.Instance?.SelectCharacter(character.BserCode);
        }

        /// <summary>실험체 확정 → 루트 선택 단계 진입 (그리드 비활성, 저장 루트 목록 표시)</summary>
        private void HandleConfirmClicked()
        {
            if (_selectedCharacter == null)
            {
                Debug.LogWarning("[PickSceneController] 실험체를 먼저 선택하세요.");
                return;
            }

            _matchSelection?.SetCharacter(_selectedCharacter);

            if (_characterGridScroll != null) _characterGridScroll.SetActive(false);
            if (_confirmButton != null) _confirmButton.interactable = false;

            // 저장된 루트 로드 (현재는 스텁 — 추후 계정 DB 조회로 교체)
            _routeSource ??= new StubSavedRouteSource(_itemDatabase);
            if (_routePanel != null)
            {
                _routePanel.Show();
                _routePanel.Build(_routeSource.GetRoutes());
            }
        }

        /// <summary>루트 선택 → 출전 정보 캐리어에 기록 (인게임 진입 시 전달)</summary>
        private void HandleRouteChosen(SavedRoute route)
        {
            _matchSelection?.SetRoute(route);
            Debug.Log($"[PickSceneController] 루트 선택: {route?.DisplayName}");
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
            // 확인을 거치지 않고 바로 진입하는 경우에도 현재 선택 실험체는 캐리에 반영
            if (_matchSelection != null && _selectedCharacter != null)
                _matchSelection.SetCharacter(_selectedCharacter);

            _timer.StopTimer();
            Debug.Log("[PickSceneController] 테스트 버튼 → 인게임 씬 진입");
            SceneManager.LoadScene(SceneInGame);
        }
    }
}
