## 구현 현황 (2026-06-09 기준)

### 완료

경로 기준: `Client/Assets/Scripts/` (클라이언트), `Server/` (서버)

| 시스템 | 주요 파일 | 비고 |
|---|---|---|
| 데이터 레이어 | `Scripts/Data/*.cs` | ItemData(등급 포함), ItemDatabase, RecipeData, RecipeDatabase, 열거형 전체 |
| 아이템 등급 | `Scripts/Data/ItemGrade.cs`, `ItemGradeColorConfig.cs` | Common~Mythic, 색상 SO |
| 인벤토리 시스템 | `Scripts/Inventory/InventorySystem.cs` | 가방 10슬롯 + 장비 5슬롯, 스택, 장착/해제, 이벤트 |
| 크래프팅 시스템 | `Scripts/Crafting/CraftingSystem.cs` | 재료 검증, 차감, 환불, 제작 가능 레시피 조회 |
| 인벤토리 UI | `Scripts/UI/InventoryUI.cs`, `InventorySlotUI.cs` | 슬롯 렌더링, 이벤트 바인딩 |
| 크래프팅 UI | `Scripts/UI/CraftingUI.cs`, `CraftingSlotUI.cs` | 레시피 목록, 재료 표시 |
| 도감 UI | `Scripts/UI/InventoryTestPanel.cs` | 전체 아이템 탐색, 타입 필터, 등급색, 선택/획득, 루트 기반 정렬 |
| 스탯창 | `Scripts/UI/InventoryTestPanel.cs` | 장착 스탯 합산 표시 (4열 2그룹, 12종) |
| 목표 루트 UI | `Scripts/UI/TargetItemPanelUI.cs`, `TargetItemSlotUI.cs` | 부위별 5슬롯, 루트 추가, 우클릭 제거 |
| 삼각형 표시 | `Scripts/UI/TriangleIndicator.cs` | 루트 필요 재료 슬롯 좌상단 표시 |
| 테스트 UI 빌더 | `Scripts/Editor/InventoryTestUIBuilder.cs` | 1920×1080 3패널 자동 생성, 전체 연결 |
| BSER 에디터 도구 | `Scripts/Editor/BserItemImporter.cs` | JSON 4종 파싱 → ItemData SO + RecipeData SO 자동 생성 |
| 스프라이트 연결 | `Scripts/Editor/BserSpriteLinker.cs` | 아이콘 자동 매핑 |
| 아이템 데이터 | `ScriptableObjects/Items/BSER/` | BSER API 기반 786개 (무기/방어구/소비/재료) |
| 레시피 데이터 | `ScriptableObjects/Recipes/BSER/` | 666개 |
| 서버 네트워크 레이어 | `Server/ProjectER.Server/Network/` | TcpGameServer, ClientSession, PacketDispatcher, PacketBuilder |
| 서버 로비 시스템 | `Server/ProjectER.Server/Lobby/` | LobbyRoom(18명), LobbyManager(동적 생성/삭제) |
| 서버 패킷 정의 | `Server/ProjectER.Core/Packets/` | C2S_Connect/Move/MatchRequest/MatchCancel, S2C_Connected/MoveSync/MatchQueued/MatchCancelled/MatchFound |
| 서버 핸들러 | `Server/ProjectER.Server/Handlers/` | ConnectHandler(버전 검증, 로비 배정), MoveHandler(stub), MatchRequestHandler |
| 서버 매치메이킹 | `Server/ProjectER.Server/Matchmaking/` | MatchmakingConfig(MinPlayers/MaxPlayers), MatchmakingQueue, MatchmakingManager |
| 클라이언트 네트워크 | `Scripts/Network/` | NetworkClient(매치메이킹 API 포함), MiniMsgPack, PacketSerializer, PacketBuilder, Dispatcher |
| 접속/로비 씬 | `Scripts/Scene/`, `Assets/Scenes/` | ConnectSceneController, LobbySceneController(매치 찾기 버튼), 에디터 빌더 |

### 미구현 (다음 작업 대상)

| 시스템 | 우선순위 | 비고 |
|---|---|---|
| 이동 시스템 | 핵심 | NavMesh 클릭투무브, CharacterData SO |
| 캐릭터 시스템 | 핵심 | CharacterBase, PlayerController, CharacterState 머신 |
| 전투 시스템 | 핵심 | IDamageable, IAttackable, 기본 공격/피격/사망 |
| 아이템 줍기 | 핵심 | 월드 아이템 프리팹, 줍기 인터랙션 |
| 이동 동기화 | 네트워크 | MoveHandler GameRoom 연동, S2C_MoveSync 브로드캐스트 |
| 전투 동기화 | 네트워크 | AttackHandler, S2C_TakeDamage/Die 패킷 |
| GameRoom | 서버 | 틱 루프, 플레이어 위치 관리 |
| 스탯 아이콘 | UI | 에셋 준비 후 StatDefs에 아이콘 슬롯 연결 |
| 무기 세부 필터 | UI | 캐릭터 선택 시 해당 캐릭터 무기군으로 표시 |
| 몬스터 AI | 선택 | 순찰 → 어그로 → 추격 |
| 미니맵 | 선택 | - |
| 금지구역 | 선택 | - |
