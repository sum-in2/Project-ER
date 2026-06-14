using ProjectER.Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Scene
{
    /// <summary>
    /// 로그인 씬 UI 제어.
    /// 로그인 / 회원가입 전환 및 서버 인증 요청 담당.
    /// </summary>
    public class LoginSceneController : MonoBehaviour
    {
        // ── UI 참조 ───────────────────────────────────────────────
        [SerializeField] private TMP_InputField _usernameInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Button         _loginButton;
        [SerializeField] private Button         _registerButton;
        [SerializeField] private TMP_Text       _statusText;

        // ── 상수 ─────────────────────────────────────────────────
        private const string SceneLobby   = "02_LobbyScene";
        private const string SceneConnect = "00_ConnectScene";

        // ── 생명주기 ─────────────────────────────────────────────
        private void Start()
        {
            // Start에서 구독: Instance 보장
            if (NetworkClient.Instance != null)
            {
                NetworkClient.Instance.OnLoginSuccess    += HandleLoginSuccess;
                NetworkClient.Instance.OnLoginFailed     += HandleAuthFailed;
                NetworkClient.Instance.OnRegisterSuccess += HandleRegisterSuccess;
                NetworkClient.Instance.OnRegisterFailed  += HandleAuthFailed;
                NetworkClient.Instance.OnDisconnected    += HandleDisconnected;
            }
            else
            {
                SetStatus("오류: 서버에 연결되어 있지 않습니다.");
                SetInteractable(false);
            }
        }

        private void OnEnable()
        {
            _loginButton.onClick.AddListener(OnLoginButtonClicked);
            _registerButton.onClick.AddListener(OnRegisterButtonClicked);
        }

        private void OnDisable()
        {
            _loginButton.onClick.RemoveListener(OnLoginButtonClicked);
            _registerButton.onClick.RemoveListener(OnRegisterButtonClicked);

            if (NetworkClient.Instance == null)
                return;

            NetworkClient.Instance.OnLoginSuccess    -= HandleLoginSuccess;
            NetworkClient.Instance.OnLoginFailed     -= HandleAuthFailed;
            NetworkClient.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
            NetworkClient.Instance.OnRegisterFailed  -= HandleAuthFailed;
            NetworkClient.Instance.OnDisconnected    -= HandleDisconnected;
        }

        // ── 버튼 콜백 ─────────────────────────────────────────────
        private void OnLoginButtonClicked()
        {
            if (!ValidateInput())
                return;

            SetStatus("로그인 중...");
            SetInteractable(false);
            NetworkClient.Instance.Login(_usernameInput.text.Trim(), _passwordInput.text);
        }

        private void OnRegisterButtonClicked()
        {
            if (!ValidateInput())
                return;

            SetStatus("회원가입 중...");
            SetInteractable(false);
            NetworkClient.Instance.Register(_usernameInput.text.Trim(), _passwordInput.text);
        }

        // ── 네트워크 이벤트 핸들러 ───────────────────────────────
        private void HandleLoginSuccess()
        {
            SetStatus("로그인 성공");
            SceneManager.LoadScene(SceneLobby);
        }

        private void HandleRegisterSuccess()
        {
            SetStatus("회원가입 성공! 로그인 해주세요.");
            SetInteractable(true);
        }

        private void HandleAuthFailed(string reason)
        {
            SetStatus(reason);
            SetInteractable(true);
        }

        private void HandleDisconnected()
        {
            Debug.Log("[LoginScene] 서버 연결 끊김");
            SceneManager.LoadScene(SceneConnect);
        }

        // ── 내부 유틸 ─────────────────────────────────────────────
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_usernameInput.text))
            {
                SetStatus("아이디를 입력하세요.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(_passwordInput.text))
            {
                SetStatus("비밀번호를 입력하세요.");
                return false;
            }
            return true;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        private void SetInteractable(bool interactable)
        {
            _loginButton.interactable    = interactable;
            _registerButton.interactable = interactable;
            _usernameInput.interactable  = interactable;
            _passwordInput.interactable  = interactable;
        }
    }
}
