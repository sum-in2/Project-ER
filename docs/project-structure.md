## 프로젝트 구조 (실제 현황)

```
Project-ER/                           ← 모노레포 루트
├── Client/                           ← Unity 프로젝트 루트 (Unity Hub에서 이 폴더를 열 것)
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── ConnectScene.unity    ← 서버 접속 씬
│   │   │   └── LobbyScene.unity      ← 대기 씬
│   │   ├── Scripts/
│   │   │   ├── Crafting/
│   │   │   │   └── CraftingSystem.cs
│   │   │   ├── Data/
│   │   │   │   ├── ItemData.cs
│   │   │   │   ├── ItemDatabase.cs
│   │   │   │   ├── ItemGrade.cs
│   │   │   │   ├── ItemGradeColorConfig.cs
│   │   │   │   ├── ItemIngredient.cs
│   │   │   │   ├── ItemType.cs
│   │   │   │   ├── RecipeData.cs
│   │   │   │   ├── RecipeDatabase.cs
│   │   │   │   └── WeaponType.cs
│   │   │   ├── Editor/
│   │   │   │   ├── BserItemImporter.cs
│   │   │   │   ├── BserSpriteLinker.cs
│   │   │   │   ├── InventoryTestUIBuilder.cs
│   │   │   │   └── NetworkSceneBuilder.cs    ← ConnectScene/LobbyScene 자동 생성
│   │   │   ├── Inventory/
│   │   │   │   ├── EquipmentSlotType.cs
│   │   │   │   ├── InventorySlot.cs
│   │   │   │   └── InventorySystem.cs
│   │   │   ├── Network/
│   │   │   │   ├── Protocol/
│   │   │   │   │   ├── PacketType.cs         ← 패킷 식별자 (서버 Core와 값 일치 유지)
│   │   │   │   │   ├── PacketHeader.cs       ← 헤더 상수 (Size = 4)
│   │   │   │   │   ├── C2SConnectPacket.cs
│   │   │   │   │   └── S2CConnectedPacket.cs
│   │   │   │   ├── MiniMsgPack.cs            ← MessagePack 호환 최소 구현
│   │   │   │   ├── PacketSerializer.cs       ← 패킷별 직렬화/역직렬화
│   │   │   │   ├── PacketBuilder.cs          ← [헤더+바디] 조립
│   │   │   │   ├── ClientPacketDispatcher.cs ← 패킷 타입별 핸들러 라우팅
│   │   │   │   └── NetworkClient.cs          ← TcpClient 래퍼 (DontDestroyOnLoad)
│   │   │   ├── Scene/
│   │   │   │   ├── ConnectSceneController.cs ← 접속 씬 UI 제어
│   │   │   │   └── LobbySceneController.cs   ← 로비 씬 UI 제어
│   │   │   └── UI/
│   │   │       ├── CraftingSlotUI.cs
│   │   │       ├── CraftingUI.cs
│   │   │       ├── InventorySlotUI.cs
│   │   │       ├── InventoryTestPanel.cs
│   │   │       ├── InventoryUI.cs
│   │   │       ├── TargetItemPanelUI.cs
│   │   │       ├── TargetItemSlotUI.cs
│   │   │       └── TriangleIndicator.cs
│   │   ├── ScriptableObjects/
│   │   │   ├── ItemDatabase.asset
│   │   │   ├── ItemGradeColorConfig.asset
│   │   │   ├── RecipeDatabase.asset
│   │   │   ├── Items/BSER/                   ← ItemData SO 786개
│   │   │   └── Recipes/BSER/                 ← RecipeData SO 666개
│   │   └── Resources/
│   │       └── Image/Item/                   ← 아이템 아이콘 스프라이트
│   ├── Packages/
│   └── ProjectSettings/
└── Server/                           ← C# .NET 9.0 게임 서버
    ├── ProjectER.sln
    ├── ProjectER.Core/               ← netstandard2.1 (패킷 정의, 공유 가능)
    │   └── Packets/
    │       ├── PacketType.cs
    │       ├── PacketHeader.cs
    │       ├── C2S/
    │       │   ├── C2SConnectPacket.cs
    │       │   └── C2SMovePacket.cs
    │       └── S2C/
    │           ├── S2CConnectedPacket.cs
    │           └── S2CMoveSyncPacket.cs
    ├── ProjectER.Server/             ← net9.0 서버 실행 프로젝트
    │   ├── Program.cs
    │   ├── Handlers/
    │   │   ├── ConnectHandler.cs     ← C2S_Connect 처리, 버전 검증, 로비 배정
    │   │   ├── MoveHandler.cs        ← C2S_Move 처리 (TODO: GameRoom 연동)
    │   │   └── MatchRequestHandler.cs ← C2S_MatchRequest/Cancel 처리
    │   ├── Lobby/
    │   │   ├── LobbyRoom.cs          ← 세션 그룹 (최대 18명)
    │   │   └── LobbyManager.cs       ← 동적 생성/삭제, 세션 배정
    │   ├── Matchmaking/
    │   │   ├── MatchmakingConfig.cs  ← MinPlayers/MaxPlayers 설정
    │   │   ├── MatchmakingQueue.cs   ← 스레드 안전 대기 큐
    │   │   └── MatchmakingManager.cs ← 큐 관리, 매치 성사, 연결 해제 정리
    │   └── Network/
    │       ├── TcpGameServer.cs      ← 연결 수락, 세션 생명주기
    │       ├── ClientSession.cs      ← 세션 1개 (수신/송신 루프)
    │       ├── PacketDispatcher.cs   ← 패킷 라우팅
    │       └── PacketBuilder.cs      ← [헤더+바디] 조립 유틸
    └── ProjectER.Tests/              ← net9.0, xUnit
```
