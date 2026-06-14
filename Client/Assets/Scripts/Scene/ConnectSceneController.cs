using ProjectER.Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Scene
{
    /// <summary>
    /// 접속 씬 UI 제어.
    /// NetworkClient 이벤트를 구독해 접속 결과에 따라 씬 전환.
    /// </summary>
    public class ConnectSceneController : MonoBehaviour
    {
        // ── UI 참조 ───────────────────────────────────────────────
        [SerializeField] private GameObject     _connectPanel;
        [SerializeField] private TMP_InputField _hostInput;
        [SerializeField] private TMP_InputField _portInput;
        [SerializeField] private Button         _connectButton;
        [SerializeField] private TMP_Text       _statusText;

        // ── 상수 ─────────────────────────────────────────────────
        private const string DefaultHost   = "127.0.0.1";
        private const string DefaultPort   = "7777";
        private const string SceneLogin    = "01_LoginScene";

        // ── 생명주기 ─────────────────────────────────────────────
        private void Start()
        {
            _hostInput.text = DefaultHost;
            _portInput.text = DefaultPort;
            SetStatus(string.Empty);

            // Start에서 구독: 모든 Awake() 완료 후 호출되므로 Instance 보장
            if (NetworkClient.Instance != null)
            {
                NetworkClient.Instance.OnConnectSuccess += HandleConnectSuccess;
                NetworkClient.Instance.OnConnectFailed  += HandleConnectFailed;
            }
            else
            {
                Debug.LogError("[ConnectSceneController] NetworkClient.Instance가 null입니다. 씬에 NetworkClient 오브젝트가 있는지 확인하세요.");
                SetStatus("초기화 오류: NetworkClient 없음");
            }
        }

        private void OnEnable()
        {
            _connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        private void OnDisable()
        {
            _connectButton.onClick.RemoveListener(OnConnectButtonClicked);

            if (NetworkClient.Instance != null)
            {
                NetworkClient.Instance.OnConnectSuccess -= HandleConnectSuccess;
                NetworkClient.Instance.OnConnectFailed  -= HandleConnectFailed;
            }
        }

        // ── 버튼 콜백 ─────────────────────────────────────────────
        private void OnConnectButtonClicked()
        {
            string host = _hostInput.text.Trim();
            string portStr = _portInput.text.Trim();

            if (!int.TryParse(portStr, out int port))
            {
                SetStatus("포트 번호가 올바르지 않습니다.");
                return;
            }

            SetStatus("접속 중...");
            SetPanelVisible(false);

            NetworkClient.Instance.Connect(host, port);
        }

        // ── 네트워크 이벤트 핸들러 ───────────────────────────────
        private void HandleConnectSuccess()
        {
            SetStatus("접속 성공");
            SceneManager.LoadScene(SceneLogin);
        }

        private void HandleConnectFailed(string reason)
        {
            SetStatus($"접속 실패: {reason}");
            SetPanelVisible(true);
        }

        // ── 내부 유틸 ─────────────────────────────────────────────
        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        private void SetPanelVisible(bool visible)
        {
            if (_connectPanel != null)
                _connectPanel.SetActive(visible);
        }
    }
}
