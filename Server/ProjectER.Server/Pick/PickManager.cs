using System;
using System.Collections.Generic;
using ProjectER.Core.Data;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.S2C;
using ProjectER.Server.Network;

namespace ProjectER.Server.Pick
{
    /// <summary>
    /// 매치별 픽 단계(PickSession)를 생성/관리.
    /// 제한시간 종료 시 전원 선택 여부를 검사해 닷지(매치 취소)를 처리.
    /// </summary>
    public class PickManager
    {
        // ── 설정 ────────────────────────────────────────────────
        private const float PickDurationSeconds = 30f;

        // ── 상태 ────────────────────────────────────────────────
        private readonly Dictionary<int, PickSession> _sessionMap = new(); // sessionId → PickSession
        private readonly object _lock = new();

        // ── 생성 ─────────────────────────────────────────────────
        /// <summary>매치 성사 직후 픽 세션을 생성하고 제한시간 타이머를 시작.</summary>
        public void CreateSession(int matchId, IReadOnlyList<ClientSession> participants)
        {
            PickSession session = new(matchId, participants, PickDurationSeconds, HandleTimeout);

            lock (_lock)
            {
                foreach (ClientSession participant in participants)
                    _sessionMap[participant.SessionId] = session;
            }

            BroadcastPickStarted(participants);
            Console.WriteLine($"[PickManager] 매치 #{matchId} 픽 세션 시작 ({participants.Count}명)");
        }

        // ── 실험체 선택 ──────────────────────────────────────────
        /// <summary>C2S_SelectCharacter 처리. 픽 세션이 없거나 존재하지 않는 실험체 ID면 무시.</summary>
        public void HandleSelectCharacter(ClientSession session, int characterId)
        {
            if (!CharacterCatalog.IsValid(characterId))
            {
                Console.WriteLine($"[PickManager] 세션 {session.SessionId} 잘못된 실험체 ID({characterId}) → 선택 무시");
                return;
            }

            PickSession? pickSession;
            lock (_lock)
            {
                if (!_sessionMap.TryGetValue(session.SessionId, out pickSession))
                {
                    Console.WriteLine($"[PickManager] 세션 {session.SessionId} 픽 세션 없음 → 선택 무시");
                    return;
                }
            }

            pickSession.SetSelection(session.SessionId, characterId);
            Console.WriteLine($"[PickManager] 세션 {session.SessionId} 실험체 선택: {characterId}");
        }

        // ── 연결 해제 ────────────────────────────────────────────
        /// <summary>연결이 끊긴 세션이 픽 단계 중이면 매치 전체를 닷지 처리.</summary>
        public void ReleaseSession(ClientSession session)
        {
            PickSession? pickSession;
            lock (_lock)
            {
                if (!_sessionMap.TryGetValue(session.SessionId, out pickSession))
                    return;
            }

            if (!TryFinalize(pickSession))
                return; // 이미 타임아웃 등으로 처리됨

            Console.WriteLine($"[PickManager] 세션 {session.SessionId} 연결 해제 → 매치 #{pickSession.MatchId} 닷지 처리");
            BroadcastDodged(pickSession);
            pickSession.Dispose();
        }

        // ── 내부: 타임아웃 처리 ──────────────────────────────────
        private void HandleTimeout(PickSession pickSession)
        {
            if (!TryFinalize(pickSession))
                return; // 이미 연결 해제 등으로 처리됨

            if (pickSession.AllSelected)
            {
                Console.WriteLine($"[PickManager] 매치 #{pickSession.MatchId} 전원 선택 완료 → 2단계 진행 (TODO)");
            }
            else
            {
                Console.WriteLine($"[PickManager] 매치 #{pickSession.MatchId} 제한시간 종료, 미선택자 존재 → 닷지 처리");
                BroadcastDodged(pickSession);
            }

            pickSession.Dispose();
        }

        // ── 내부: 세션 정리 ───────────────────────────────────────
        /// <summary>
        /// pickSession에 속한 참가자 전원을 맵에서 제거.
        /// 이미 다른 경로(타임아웃/연결 해제)로 처리된 경우 false 반환.
        /// </summary>
        private bool TryFinalize(PickSession pickSession)
        {
            lock (_lock)
            {
                int leaderSessionId = pickSession.Participants[0].SessionId;
                if (!_sessionMap.TryGetValue(leaderSessionId, out PickSession? current) || current != pickSession)
                    return false;

                foreach (ClientSession participant in pickSession.Participants)
                    _sessionMap.Remove(participant.SessionId);

                return true;
            }
        }

        // ── 내부: 패킷 송신 ────────────────────────────────────────
        private static void BroadcastDodged(PickSession pickSession)
        {
            S2CPickDodgedPacket packet = new();
            byte[] data = PacketBuilder.Build(PacketType.S2C_PickDodged, packet);

            foreach (ClientSession participant in pickSession.Participants)
                _ = participant.SendAsync(data);
        }

        /// <summary>픽 단계 시작과 제한시간을 참가자 전원에게 통지 (클라이언트 타이머 동기화 기준)</summary>
        private static void BroadcastPickStarted(IReadOnlyList<ClientSession> participants)
        {
            S2CPickStartedPacket packet = new() { DurationSeconds = PickDurationSeconds };
            byte[] data = PacketBuilder.Build(PacketType.S2C_PickStarted, packet);

            foreach (ClientSession participant in participants)
                _ = participant.SendAsync(data);
        }
    }
}
