using ProjectER.Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Scene
{
    /// <summary>
    /// 로비 씬 UI 제어.
    /// 서버 연결이 끊기면 접속 씬으로 복귀.
    /// </summary>
    public class LobbySceneController : MonoBehaviour
    {
        // ── UI 참조 ───────────────────────────────────────────────
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button   _disconnectButton;

        // ── 상수 ─────────────────────────────────────────────────
        private const string SceneConnect = "ConnectScene";

        // ── 생명주기 ─────────────────────────────────────────────
        private void Start()
        {
            if (NetworkClient.Instance != null)
                _statusText.text = $"서버 접속됨 (세션 #{NetworkClient.Instance.SessionId})";
        }

        private void OnEnable()
        {
            _disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);

            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnDisconnected += HandleDisconnected;
        }

        private void OnDisable()
        {
            _disconnectButton.onClick.RemoveListener(OnDisconnectButtonClicked);

            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnDisconnected -= HandleDisconnected;
        }

        // ── 버튼 콜백 ─────────────────────────────────────────────
        private void OnDisconnectButtonClicked()
        {
            NetworkClient.Instance?.Disconnect();
            SceneManager.LoadScene(SceneConnect);
        }

        // ── 네트워크 이벤트 핸들러 ───────────────────────────────
        private void HandleDisconnected()
        {
            Debug.Log("[LobbyScene] 서버 연결 끊김 → 접속 씬으로 복귀");
            SceneManager.LoadScene(SceneConnect);
        }
    }
}
