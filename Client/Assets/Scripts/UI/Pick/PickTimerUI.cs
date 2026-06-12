using System;
using UnityEngine;
using TMPro;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 캐릭터 선택 제한시간 카운트다운 표시 (기본 30초)
    /// </summary>
    public class PickTimerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private float    _duration = 30f;

        private float _remaining;
        private bool  _running;
        private int   _lastDisplayedSeconds = -1;

        public event Action OnTimeExpired;

        /// <summary>카운트다운 시작 (재시작 가능)</summary>
        public void StartTimer()
        {
            _remaining            = _duration;
            _running              = true;
            _lastDisplayedSeconds = -1;
            UpdateDisplay();
        }

        public void StopTimer() => _running = false;

        private void Update()
        {
            if (!_running) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running   = false;
                UpdateDisplay();
                OnTimeExpired?.Invoke();
                return;
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int seconds = Mathf.CeilToInt(_remaining);
            if (seconds == _lastDisplayedSeconds) return; // ⚠️ GC 주의: 표시 초가 바뀔 때만 문자열 갱신

            _lastDisplayedSeconds = seconds;
            _timeText.text = seconds.ToString();
        }
    }
}
