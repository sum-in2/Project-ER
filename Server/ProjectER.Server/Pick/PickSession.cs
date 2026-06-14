using System;
using System.Collections.Generic;
using System.Threading;
using ProjectER.Server.Network;

namespace ProjectER.Server.Pick
{
    /// <summary>
    /// 매치 1개의 픽 단계(실험체 선택)를 나타냄.
    /// 제한시간 타이머와 참가자별 선택 상태를 관리.
    /// </summary>
    public class PickSession : IDisposable
    {
        // ── 식별 ────────────────────────────────────────────────
        public int MatchId { get; }
        public IReadOnlyList<ClientSession> Participants { get; }

        // ── 선택 상태 ────────────────────────────────────────────
        private readonly Dictionary<int, int> _selections = new(); // sessionId → characterId
        private readonly object _lock = new();

        // ── 제한시간 타이머 ──────────────────────────────────────
        private readonly Timer _timeoutTimer;

        public PickSession(int matchId, IReadOnlyList<ClientSession> participants, float durationSeconds, Action<PickSession> onTimeout)
        {
            MatchId      = matchId;
            Participants = participants;

            _timeoutTimer = new Timer(
                _ => onTimeout(this),
                null,
                TimeSpan.FromSeconds(durationSeconds),
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>실험체 선택(재선택 시 갱신)</summary>
        public void SetSelection(int sessionId, int characterId)
        {
            lock (_lock)
            {
                _selections[sessionId] = characterId;
            }
        }

        /// <summary>참가자 전원이 선택을 완료했는지 여부</summary>
        public bool AllSelected
        {
            get
            {
                lock (_lock)
                {
                    return _selections.Count >= Participants.Count;
                }
            }
        }

        public void Dispose()
        {
            _timeoutTimer.Dispose();
        }
    }
}
