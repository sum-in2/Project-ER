using System.Collections.Generic;
using ProjectER.Server.Network;

namespace ProjectER.Server.Lobby
{
    /// <summary>
    /// 세션 N개를 묶는 로비 단위.
    /// 브로드캐스트 범위를 제한하고 메모리를 Room 단위로 관리하기 위해 사용.
    /// 게임 로직 없음 - 순수 세션 그룹.
    /// </summary>
    public class LobbyRoom
    {
        // ── 식별 ────────────────────────────────────────────────
        public int RoomId { get; }

        // ── 용량 ────────────────────────────────────────────────
        public int MaxCapacity { get; }
        public int Count => _sessions.Count;
        public bool IsFull => _sessions.Count >= MaxCapacity;
        public bool IsEmpty => _sessions.Count == 0;

        // ── 세션 목록 ────────────────────────────────────────────
        private readonly HashSet<ClientSession> _sessions = new();
        private readonly object _lock = new();

        public LobbyRoom(int roomId, int maxCapacity)
        {
            RoomId      = roomId;
            MaxCapacity = maxCapacity;
        }

        // ── 입장 ─────────────────────────────────────────────────
        /// <summary>세션 추가. 성공 여부 반환.</summary>
        public bool TryAdd(ClientSession session)
        {
            lock (_lock)
            {
                if (IsFull)
                    return false;

                _sessions.Add(session);
                return true;
            }
        }

        // ── 퇴장 ─────────────────────────────────────────────────
        /// <summary>세션 제거.</summary>
        public void Remove(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
            }
        }

        // ── 조회 ─────────────────────────────────────────────────
        /// <summary>현재 세션 목록 스냅샷 반환. 순회 중 컬렉션 변경 방지용.</summary>
        public IReadOnlyList<ClientSession> GetSessions()
        {
            lock (_lock)
            {
                return new List<ClientSession>(_sessions); // ⚠️ GC 주의: 필요 시 ArrayPool로 교체
            }
        }
    }
}
