using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// Unity 레거시 InputField의 한글 IME 실시간 이벤트 어댑터.
    /// InputField.onValueChanged는 IME 조합 완료 시에만 발생하므로,
    /// Update()에서 Input.compositionString 변화를 감시해 보완한다.
    /// InputField와 같은 GameObject에 부착해 사용한다.
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class KoreanInputFieldAdapter : MonoBehaviour
    {
        /// <summary>
        /// 한글 조합 중에도 실시간으로 발생하는 텍스트 변경 이벤트.
        /// 전달값은 소문자 변환 전 원본 텍스트이다.
        /// </summary>
        public event Action<string> OnTextChanged;

        private InputField _inputField;
        private string     _lastRawValue          = string.Empty;
        private string     _lastCompositionString = string.Empty;

        private void Awake()
        {
            TryGetComponent(out _inputField);
        }

        private void OnEnable()
        {
            if (_inputField == null) return;
            _inputField.onValueChanged.AddListener(HandleValueChanged);
            _inputField.onEndEdit.AddListener(HandleEndEdit);
        }

        private void OnDisable()
        {
            if (_inputField == null) return;
            _inputField.onValueChanged.RemoveListener(HandleValueChanged);
            _inputField.onEndEdit.RemoveListener(HandleEndEdit);
        }

        private void Update()
        {
            // InputField.isFocused는 내부 bool 필드 접근이므로 비용 무시 가능
            if (_inputField == null || !_inputField.isFocused) return;

            string composition = Input.compositionString;
            if (composition == _lastCompositionString) return;

            _lastCompositionString = composition;
            // ⚠️ GC 주의: 문자열 연결 — compositionString 변화 시에만 실행
            Notify(_inputField.text + composition);
        }

        private void HandleValueChanged(string value)
        {
            Notify(value);
        }

        private void HandleEndEdit(string value)
        {
            _lastCompositionString = string.Empty; // 포커스 해제 시 조합 상태 초기화
        }

        private void Notify(string rawValue)
        {
            if (rawValue == _lastRawValue) return; // 중복 호출 방지
            _lastRawValue = rawValue;
            OnTextChanged?.Invoke(rawValue);
        }
    }
}
