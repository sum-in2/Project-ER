using System;
using System.Collections.Generic;
using ProjectER.Server.Network;

namespace ProjectER.Server.Lobby
{
    /// <summary>
    /// LobbyRoom 목록을 동적으로 관리.
    /// - 빈 자리 있는 Room에 세션 배정
    /// - 모든 Room이 꽉 차면 새 Room 생성
    /// - 빈 Room은 자동 제거 (메모리 회수)
    /// </summary>
    public class LobbyManager
    {
        // ── 설정 ────────────────────────────────────────────────
        private readonly int _roomCapacity;

        // ── 상태 ────────────────────────────────────────────────
        private readonly List<LobbyRoom> _rooms = new();
        private readonly Dictionary<int, LobbyRoom> _sessionRoomMap = new(); // sessionId → LobbyRoom
        private readonly object _lock = new();
        private int _nextRoomId = 1;

        public LobbyManager(int roomCapacity = 18)
        {
            _roomCapacity = roomCapacity;
        }

        // ── 배정 ─────────────────────────────────────────────────
        /// <summary>
        /// 세션을 빈 자리가 있는 Room에 배정.
        /// 모든 Room이 꽉 찼으면 새 Room 생성 후 배정.
        /// </summary>
        public LobbyRoom Assign(ClientSession session)
        {
            lock (_lock)
            {
                // 빈 자리 있는 Room 탐색
                foreach (LobbyRoom room in _rooms)
                {
                    if (room.TryAdd(session))
                    {
                        _sessionRoomMap[session.SessionId] = room;
                        Console.WriteLine($"[LobbyManager] 세션 {session.SessionId} → Room #{room.RoomId} 배정 ({room.Count}/{room.MaxCapacity})");
                        return room;
                    }
                }

                // 모든 Room 만석 → 새 Room 생성
                LobbyRoom newRoom = CreateRoom();
                newRoom.TryAdd(session);
                _sessionRoomMap[session.SessionId] = newRoom;
                Console.WriteLine($"[LobbyManager] 세션 {session.SessionId} → 신규 Room #{newRoom.RoomId} 생성 및 배정");
                return newRoom;
            }
        }

        // ── 제거 ─────────────────────────────────────────────────
        /// <summary>
        /// 세션을 소속 Room에서 제거.
        /// Room이 비면 자동 삭제.
        /// </summary>
        public void Release(ClientSession session)
        {
            lock (_lock)
            {
                if (!_sessionRoomMap.TryGetValue(session.SessionId, out LobbyRoom? room))
                    return;

                room.Remove(session);
                _sessionRoomMap.Remove(session.SessionId);

                // 빈 Room 제거
                if (room.IsEmpty)
                {
                    _rooms.Remove(room);
                    Console.WriteLine($"[LobbyManager] Room #{room.RoomId} 비어있음 → 제거 (남은 Room: {_rooms.Count}개)");
                }
                else
                {
                    Console.WriteLine($"[LobbyManager] 세션 {session.SessionId} Room #{room.RoomId} 에서 제거 ({room.Count}/{room.MaxCapacity})");
                }
            }
        }

        // ── 조회 ─────────────────────────────────────────────────
        public int RoomCount
        {
            get { lock (_lock) { return _rooms.Count; } }
        }

        // ── 내부 ─────────────────────────────────────────────────
        private LobbyRoom CreateRoom()
        {
            LobbyRoom room = new(_nextRoomId++, _roomCapacity);
            _rooms.Add(room);
            Console.WriteLine($"[LobbyManager] Room #{room.RoomId} 생성 (총 {_rooms.Count}개)");
            return room;
        }
    }
}
