using System;
using System.Collections.Generic;
using System.Threading;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.S2C;
using ProjectER.Server.Network;
using ProjectER.Server.Pick;

namespace ProjectER.Server.Matchmaking
{
    /// <summary>
    /// 매치메이킹 큐와 매치 생성을 관리.
    /// 큐 인원이 MinPlayers 이상이 되면 즉시 매치를 성사시킴.
    /// </summary>
    public class MatchmakingManager
    {
        private readonly MatchmakingConfig _config;
        private readonly PickManager _pickManager;
        private readonly MatchmakingQueue _queue = new();
        private int _nextMatchId;

        public MatchmakingManager(MatchmakingConfig config, PickManager pickManager)
        {
            _config = config;
            _pickManager = pickManager;
        }

        // ── 큐 진입 ──────────────────────────────────────────────
        /// <summary>
        /// 세션을 큐에 추가하고 S2C_MatchQueued 응답.
        /// MinPlayers 충족 시 즉시 매치 성사.
        /// </summary>
        public void EnqueueSession(ClientSession session)
        {
            if (!_queue.TryEnqueue(session, out int position))
            {
                Console.WriteLine($"[Matchmaking] 세션 {session.SessionId} 이미 큐에 있음 (순서: {position})");
                return;
            }

            Console.WriteLine($"[Matchmaking] 세션 {session.SessionId} 큐 진입 (순서: {position}, 현재 대기: {_queue.Count}/{_config.MinPlayers})");
            SendMatchQueued(session, position);

            if (_queue.Count >= _config.MinPlayers)
                TryCreateMatch();
        }

        // ── 큐 취소 ──────────────────────────────────────────────
        /// <summary>세션을 큐에서 제거하고 S2C_MatchCancelled 응답.</summary>
        public void CancelSession(ClientSession session)
        {
            if (_queue.TryDequeue(session))
            {
                Console.WriteLine($"[Matchmaking] 세션 {session.SessionId} 큐 취소");
                SendMatchCancelled(session);
            }
        }

        // ── 연결 해제 처리 ────────────────────────────────────────
        /// <summary>연결이 끊긴 세션을 큐에서 조용히 제거. 응답 패킷 없음.</summary>
        public void ReleaseSession(ClientSession session)
        {
            if (_queue.TryDequeue(session))
                Console.WriteLine($"[Matchmaking] 세션 {session.SessionId} 연결 해제 → 큐에서 제거");
        }

        // ── 내부: 매치 생성 ──────────────────────────────────────
        private void TryCreateMatch()
        {
            List<ClientSession> matched = _queue.TakeUpTo(_config.MaxPlayers);

            // TakeUpTo와 Count 체크 사이에 세션이 이탈했을 수 있음
            if (matched.Count < _config.MinPlayers)
            {
                // 꺼낸 세션이 최소 인원에 못 미치면 다시 큐에 되돌릴 수 없으므로 각자에게 취소 통보
                foreach (ClientSession session in matched)
                {
                    _queue.TryEnqueue(session, out _);
                    SendMatchQueued(session, _queue.Count);
                }
                return;
            }

            int matchId = Interlocked.Increment(ref _nextMatchId);
            Console.WriteLine($"[Matchmaking] 매치 #{matchId} 성사 ({matched.Count}명)");

            foreach (ClientSession session in matched)
                SendMatchFound(session, matchId, matched.Count);

            _pickManager.CreateSession(matchId, matched);
        }

        // ── 패킷 송신 ─────────────────────────────────────────────
        private static void SendMatchQueued(ClientSession session, int position)
        {
            S2CMatchQueuedPacket packet = new() { QueuePosition = position };
            byte[] data = PacketBuilder.Build(PacketType.S2C_MatchQueued, packet);
            _ = session.SendAsync(data);
        }

        private static void SendMatchCancelled(ClientSession session)
        {
            S2CMatchCancelledPacket packet = new();
            byte[] data = PacketBuilder.Build(PacketType.S2C_MatchCancelled, packet);
            _ = session.SendAsync(data);
        }

        private static void SendMatchFound(ClientSession session, int matchId, int playerCount)
        {
            S2CMatchFoundPacket packet = new() { MatchId = matchId, PlayerCount = playerCount };
            byte[] data = PacketBuilder.Build(PacketType.S2C_MatchFound, packet);
            _ = session.SendAsync(data);
        }
    }
}
