using System;
using System.Collections.Generic;
using ProjectER.Server.Network;

namespace ProjectER.Server.Matchmaking
{
    /// <summary>
    /// 매치메이킹 대기 큐. 스레드 안전.
    /// </summary>
    public class MatchmakingQueue
    {
        private readonly List<ClientSession> _queue = new();
        private readonly object _lock = new();

        public int Count
        {
            get { lock (_lock) { return _queue.Count; } }
        }

        /// <summary>
        /// 세션을 큐에 추가.
        /// 이미 있으면 false + 현재 순서 반환.
        /// </summary>
        public bool TryEnqueue(ClientSession session, out int position)
        {
            lock (_lock)
            {
                int existing = _queue.IndexOf(session);
                if (existing >= 0)
                {
                    position = existing + 1;
                    return false;
                }

                _queue.Add(session);
                position = _queue.Count;
                return true;
            }
        }

        /// <summary>세션을 큐에서 제거. 큐에 없으면 false 반환.</summary>
        public bool TryDequeue(ClientSession session)
        {
            lock (_lock)
            {
                return _queue.Remove(session);
            }
        }

        /// <summary>큐 앞에서 최대 count개 세션을 꺼내 반환. 큐에서 제거됨.</summary>
        public List<ClientSession> TakeUpTo(int count)
        {
            lock (_lock)
            {
                int take = Math.Min(count, _queue.Count);
                List<ClientSession> taken = _queue.GetRange(0, take); // ⚠️ GC 주의
                _queue.RemoveRange(0, take);
                return taken;
            }
        }
    }
}
