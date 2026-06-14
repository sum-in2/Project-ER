## 프로젝트 구조 (실제 현황)

```
Project-ER/                           ← 모노레포 루트
├── Client/                           ← Unity 프로젝트 루트 (Unity Hub에서 이 폴더를 열 것)
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── 00_ConnectScene.unity ← 서버 접속 씬
│   │   │   ├── 01_LoginScene.unity   ← 로그인/회원가입 씬
│   │   │   ├── 02_LobbyScene.unity   ← 대기 씬
│   │   │   ├── 03_PickScene.unity    ← 매칭 후 캐릭터 선택 씬
│   │   │   └── 04_InGameScene.unity  ← 인게임 씬
│   │   ├── Scripts/
│   │   │   ├── Crafting/
│   │   │   │   └── CraftingSystem.cs
│   │   │   ├── Data/
│   │   │   │   ├── CharacterArcheType.cs
│   │   │   │   ├── CharacterData.cs
│   │   │   │   ├── CharacterDatabase.cs
│   │   │   │   ├── ItemData.cs
│   │   │   │   ├── ItemDatabase.cs
│   │   │   │   ├── ItemGrade.cs
│   │   │   │   ├── ItemGradeColorConfig.cs
│   │   │   │   ├── ItemIngredient.cs
│   │   │   │   ├── ItemType.cs
│   │   │   │   ├── RecipeData.cs
│   │   │   │   ├── RecipeDatabase.cs
│   │   │   │   ├── WeaponRangeType.cs
│   │   │   │   ├── WeaponType.cs
│   │   │   │   ├── WeaponTypeDatabase.cs
│   │   │   │   └── WeaponTypeInfoData.cs
│   │   │   ├── Editor/
│   │   │   │   ├── BserCharacterImporter.cs  ← Character/CharacterAttributes/WeaponTypeInfo JSON → SO 자동 생성
│   │   │   │   ├── BserItemImporter.cs
│   │   │   │   ├── BserSpriteLinker.cs
│   │   │   │   ├── BserCharacterSpriteLinker.cs ← 실험체 초상화(Full/Half/Mini) 자동 연결
│   │   │   │   ├── InventoryTestUIBuilder.cs
│   │   │   │   ├── NetworkSceneBuilder.cs    ← ConnectScene/LobbyScene 자동 생성
│   │   │   │   └── PickSceneBuilder.cs       ← PickScene 자동 생성
│   │   │   ├── Inventory/
│   │   │   │   ├── EquipmentSlotType.cs
│   │   │   │   ├── InventorySlot.cs
│   │   │   │   └── InventorySystem.cs
│   │   │   ├── Network/
│   │   │   │   ├── Protocol/
│   │   │   │   │   ├── PacketType.cs         ← 패킷 식별자 (서버 Core와 값 일치 유지)
│   │   │   │   │   ├── PacketHeader.cs       ← 헤더 상수 (Size = 4)
│   │   │   │   │   ├── C2SConnectPacket.cs
│   │   │   │   │   ├── S2CConnectedPacket.cs
│   │   │   │   │   ├── C2SSelectCharacterPacket.cs ← 픽 화면 실험체 선택
│   │   │   │   │   ├── S2CPickDodgedPacket.cs      ← 픽 닷지로 매치 취소
│   │   │   │   │   └── S2CPickStartedPacket.cs     ← 픽 단계 시작 (제한시간 동기화)
│   │   │   │   ├── MiniMsgPack.cs            ← MessagePack 호환 최소 구현 (float 포함)
│   │   │   │   ├── PacketSerializer.cs       ← 패킷별 직렬화/역직렬화
│   │   │   │   ├── PacketBuilder.cs          ← [헤더+바디] 조립
│   │   │   │   ├── ClientPacketDispatcher.cs ← 패킷 타입별 핸들러 라우팅
│   │   │   │   └── NetworkClient.cs          ← TcpClient 래퍼 (DontDestroyOnLoad)
│   │   │   ├── Scene/
│   │   │   │   ├── ConnectSceneController.cs ← 접속 씬 UI 제어
│   │   │   │   ├── LobbySceneController.cs   ← 로비 씬 UI 제어
│   │   │   │   └── PickSceneController.cs    ← 캐릭터 선택 씬 진행 제어 (선택/플레이어 슬롯/제한시간)
│   │   │   ├── UI/
│   │   │   │   ├── CraftingSlotUI.cs
│   │   │   │   ├── CraftingUI.cs
│   │   │   │   ├── InventorySlotUI.cs
│   │   │   │   ├── InventoryTestPanel.cs
│   │   │   │   ├── InventoryUI.cs
│   │   │   │   ├── TargetItemPanelUI.cs
│   │   │   │   ├── TargetItemSlotUI.cs
│   │   │   │   └── TriangleIndicator.cs
│   │   │   └── UI/Pick/
│   │   │       ├── CharacterFilterPanelUI.cs   ← 역할군 필터/이름순 정렬/검색
│   │   │       ├── CharacterGridUI.cs          ← 실험체 그리드 (필터·정렬·검색 결과 표시)
│   │   │       ├── CharacterSelectSlotUI.cs    ← 그리드 슬롯 1개 (Mini 아이콘 + 이름)
│   │   │       ├── PickTimerUI.cs              ← 선택 제한시간(30초) 카운트다운
│   │   │       ├── PlayerSlotUI.cs             ← 플레이어 슬롯 (이름 + 선택 아이콘, 3칸)
│   │   │       └── SelectedCharacterPanelUI.cs ← 선택한 실험체 Full 초상화 표시
│   │   ├── Prefabs/
│   │   │   └── UI/
│   │   │       └── CharacterSelectSlot.prefab ← PickSceneBuilder가 생성하는 그리드 슬롯 프리팹
│   │   ├── ScriptableObjects/
│   │   │   ├── CharacterDatabase.asset
│   │   │   ├── ItemDatabase.asset
│   │   │   ├── ItemGradeColorConfig.asset
│   │   │   ├── RecipeDatabase.asset
│   │   │   ├── WeaponTypeDatabase.asset
│   │   │   ├── Characters/BSER/              ← CharacterData SO 89개
│   │   │   ├── Items/BSER/                   ← ItemData SO 786개
│   │   │   ├── Recipes/BSER/                 ← RecipeData SO 666개
│   │   │   └── WeaponTypes/BSER/             ← WeaponTypeInfoData SO 23개
│   │   └── Resources/
│   │       ├── Data/BSER/                    ← Character/CharacterAttributes/WeaponTypeInfo/TacticalSkillSet/l10n JSON
│   │       ├── Image/Character/              ← 실험체 초상화 (Full/Half/Mini, 코드별 폴더)
│   │       └── Image/Item/                   ← 아이템 아이콘 스프라이트
│   ├── Packages/
│   └── ProjectSettings/
└── Server/                           ← C# .NET 9.0 게임 서버
    ├── ProjectER.sln
    ├── ProjectER.Core/               ← netstandard2.1 (패킷 정의, 공유 가능)
    │   ├── Data/
    │   │   └── CharacterCatalog.cs   ← 유효한 실험체 ID 목록 (서버 검증용, Character.json code 1~89)
    │   └── Packets/
    │       ├── PacketType.cs
    │       ├── PacketHeader.cs
    │       ├── C2S/
    │       │   ├── C2SConnectPacket.cs
    │       │   ├── C2SMovePacket.cs
    │       │   └── C2SSelectCharacterPacket.cs ← 픽 화면 실험체 선택
    │       └── S2C/
    │           ├── S2CConnectedPacket.cs
    │           ├── S2CMoveSyncPacket.cs
    │           ├── S2CPickDodgedPacket.cs      ← 픽 닷지로 매치 취소
    │           └── S2CPickStartedPacket.cs     ← 픽 단계 시작 (제한시간 동기화)
    ├── ProjectER.Server/             ← net9.0 서버 실행 프로젝트
    │   ├── Program.cs
    │   ├── Handlers/
    │   │   ├── ConnectHandler.cs     ← C2S_Connect 처리, 버전 검증, 로비 배정
    │   │   ├── MoveHandler.cs        ← C2S_Move 처리 (TODO: GameRoom 연동)
    │   │   ├── MatchRequestHandler.cs ← C2S_MatchRequest/Cancel 처리
    │   │   └── PickHandler.cs        ← C2S_SelectCharacter 처리
    │   ├── Lobby/
    │   │   ├── LobbyRoom.cs          ← 세션 그룹 (최대 18명)
    │   │   └── LobbyManager.cs       ← 동적 생성/삭제, 세션 배정
    │   ├── Matchmaking/
    │   │   ├── MatchmakingConfig.cs  ← MinPlayers/MaxPlayers 설정
    │   │   ├── MatchmakingQueue.cs   ← 스레드 안전 대기 큐
    │   │   └── MatchmakingManager.cs ← 큐 관리, 매치 성사, 연결 해제 정리, PickManager.CreateSession 호출
    │   ├── Pick/
    │   │   ├── PickSession.cs        ← 매치 1개의 픽 단계 (참가자/선택 상태/30초 타이머)
    │   │   └── PickManager.cs        ← 매치별 PickSession 관리, 닷지 판정
    │   └── Network/
    │       ├── TcpGameServer.cs      ← 연결 수락, 세션 생명주기
    │       ├── ClientSession.cs      ← 세션 1개 (수신/송신 루프)
    │       ├── PacketDispatcher.cs   ← 패킷 라우팅
    │       └── PacketBuilder.cs      ← [헤더+바디] 조립 유틸
    └── ProjectER.Tests/              ← net9.0, xUnit
```
