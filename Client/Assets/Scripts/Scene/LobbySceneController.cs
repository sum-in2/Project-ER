using ProjectER.Network;
using ProjectER.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Scene
{
    /// <summary>
    /// 로비 씬 UI 제어.
    /// 매치메이킹 요청/취소 및 서버 연결 해제를 담당.
    /// </summary>
    public class LobbySceneController : MonoBehaviour
    {
        // ── UI 참조 ───────────────────────────────────────────────
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _matchStatusText;
        [SerializeField] private Button   _matchButton;
        [SerializeField] private Button   _disconnectButton;
        [SerializeField] private Button   _loadOutButton;
        [SerializeField] private LobbyUIManager _lobbyUIManager;

        // ── 상수 ─────────────────────────────────────────────────
        private const string SceneConnect = "ConnectScene";

        // ── 생명주기 ─────────────────────────────────────────────
        private void Start()
        {
            if (NetworkClient.Instance != null)
                _statusText.text = $"서버 접속됨 (세션 #{NetworkClient.Instance.SessionId})";

            RefreshMatchButton();
        }

        private void OnEnable()
        {
            _matchButton.onClick.AddListener(OnMatchButtonClicked);
            _disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
            _loadOutButton.onClick.AddListener(OnLoadOutButtonClicked);

            if (NetworkClient.Instance == null)
                return;

            NetworkClient.Instance.OnDisconnected    += HandleDisconnected;
            NetworkClient.Instance.OnMatchQueued     += HandleMatchQueued;
            NetworkClient.Instance.OnMatchCancelled  += HandleMatchCancelled;
            NetworkClient.Instance.OnMatchFound      += HandleMatchFound;
        }

        private void OnDisable()
        {
            _matchButton.onClick.RemoveListener(OnMatchButtonClicked);
            _disconnectButton.onClick.RemoveListener(OnDisconnectButtonClicked);
            _loadOutButton.onClick.RemoveListener(OnLoadOutButtonClicked);

            if (NetworkClient.Instance == null)
                return;

            NetworkClient.Instance.OnDisconnected    -= HandleDisconnected;
            NetworkClient.Instance.OnMatchQueued     -= HandleMatchQueued;
            NetworkClient.Instance.OnMatchCancelled  -= HandleMatchCancelled;
            NetworkClient.Instance.OnMatchFound      -= HandleMatchFound;
        }

        // ── 버튼 콜백 ─────────────────────────────────────────────
        private void OnMatchButtonClicked()
        {
            if (NetworkClient.Instance == null)
                return;

            if (NetworkClient.Instance.IsMatchmaking)
                NetworkClient.Instance.CancelMatch();
            else
                NetworkClient.Instance.RequestMatch();
        }

        private void OnDisconnectButtonClicked()
        {
            NetworkClient.Instance?.Disconnect();
            SceneManager.LoadScene(SceneConnect);
        }

        private void OnLoadOutButtonClicked()
        {
            _lobbyUIManager.OpenPanel(LobbyPanelType.LoadOut);
        }

        // ── 네트워크 이벤트 핸들러 ───────────────────────────────
        private void HandleDisconnected()
        {
            Debug.Log("[LobbyScene] 서버 연결 끊김 → 접속 씬으로 복귀");
            SceneManager.LoadScene(SceneConnect);
        }

        private void HandleMatchQueued(int position)
        {
            _matchStatusText.text = $"매치 대기 중... ({position}번째)";
            RefreshMatchButton();
        }

        private void HandleMatchCancelled()
        {
            _matchStatusText.text = "매치메이킹 취소됨";
            RefreshMatchButton();
        }

        private void HandleMatchFound(int matchId, int playerCount)
        {
            _matchStatusText.text = $"매치 성사! (매치 #{matchId}, {playerCount}명)";
            RefreshMatchButton();
            // TODO: GameScene 전환
        }

        // ── 내부 유틸 ─────────────────────────────────────────────
        private void RefreshMatchButton()
        {
            if (NetworkClient.Instance == null)
                return;

            TMP_Text label = _matchButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = NetworkClient.Instance.IsMatchmaking ? "매치 취소" : "매치 찾기";
        }
    }
}
