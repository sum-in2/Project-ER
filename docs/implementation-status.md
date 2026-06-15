## 구현 현황 (2026-06-15 기준)

### 완료

경로 기준: `Client/Assets/Scripts/` (클라이언트), `Server/` (서버)

| 시스템 | 주요 파일 | 비고 |
|---|---|---|
| 데이터 레이어 | `Scripts/Data/*.cs` | ItemData(등급 포함), ItemDatabase, RecipeData, RecipeDatabase, 열거형 전체 |
| 실험체 데이터 | `Scripts/Data/CharacterData.cs`, `CharacterDatabase.cs`, `CharacterArcheType.cs`, `WeaponRangeType.cs` | BSER API 기반 89개, 역할군/교전거리/무기군/전투스탯 |
| 무기군 데이터 | `Scripts/Data/WeaponTypeInfoData.cs`, `WeaponTypeDatabase.cs` | BSER API 기반 23개, 무기군별 공속/사거리/상점필터 |
| BSER 실험체 임포터 | `Scripts/Editor/BserCharacterImporter.cs` | Character/CharacterAttributes/WeaponTypeInfo/l10n_Korean_character JSON → SO 자동 생성 |
| 아이템 등급 | `Scripts/Data/ItemGrade.cs`, `ItemGradeColorConfig.cs` | Common~Mythic, 색상 SO |
| 인벤토리 시스템 | `Scripts/Inventory/InventorySystem.cs` | 가방 10슬롯 + 장비 5슬롯, 스택, 장착/해제, 이벤트 |
| 크래프팅 시스템 | `Scripts/Crafting/CraftingSystem.cs` | 재료 검증, 차감, 환불, 제작 가능 레시피 조회 |
| 인벤토리 UI | `Scripts/UI/InventoryUI.cs`, `InventorySlotUI.cs` | 슬롯 렌더링, 이벤트 바인딩 |
| 크래프팅 UI | `Scripts/UI/CraftingUI.cs`, `CraftingSlotUI.cs` | 레시피 목록, 재료 표시 |
| 도감 UI | `Scripts/UI/InventoryTestPanel.cs` | 전체 아이템 탐색, 타입 필터, 등급색, 선택/획득, 루트 기반 정렬 |
| 스탯창 | `Scripts/UI/InventoryTestPanel.cs` | 장착 스탯 합산 표시 (4열 2그룹, 12종) |
| 목표 루트 UI | `Scripts/UI/TargetItemPanelUI.cs`, `TargetItemSlotUI.cs` | 부위별 5슬롯, 루트 추가, 우클릭 제거 |
| 아이템 슬롯 추상화 | `Scripts/UI/IItemSlot.cs`, `IItemDropTarget.cs`, `ItemDragHandler.cs`, `ItemBrowserSlotUI.cs`, `InventorySlotUI.cs`, `TargetItemSlotUI.cs` | IItemSlot(CurrentItem/IsDraggable) 공통 인터페이스, 슬롯별 `_isDraggable` 플래그로 드래그 가능 여부 결정. IItemDropTarget(TryDropItem)으로 드롭 대상 추상화 — ItemDragHandler.OnEndDrag가 포인터 아래 IItemDropTarget 탐색 후 호출(없으면 월드 드랍). 도감 그리드(ItemBrowserSlotUI)→목표 루트 슬롯(TargetItemSlotUI) 드래그 앤 드롭 |
| 삼각형 표시 | `Scripts/UI/TriangleIndicator.cs` | 루트 필요 재료 슬롯 좌상단 표시 |
| LoadOut 패널 빌더 | `Scripts/Editor/LoadOutPanelBuilder.cs` | 인벤토리/크래프팅/도감/목표 루트 3패널 LoadOut 패널 프리팹 자동 생성 (`Build LoadOut Panel Prefab`) |
| BSER 에디터 도구 | `Scripts/Editor/BserItemImporter.cs` | JSON 4종 파싱 → ItemData SO + RecipeData SO 자동 생성 |
| 스프라이트 연결 | `Scripts/Editor/BserSpriteLinker.cs` | 아이콘 자동 매핑 |
| 아이템 데이터 | `ScriptableObjects/Items/BSER/` | BSER API 기반 786개 (무기/방어구/소비/재료) |
| 레시피 데이터 | `ScriptableObjects/Recipes/BSER/` | 666개 |
| 실험체/무기군 SO | `ScriptableObjects/Characters/BSER/`, `WeaponTypes/BSER/`, `CharacterDatabase.asset`, `WeaponTypeDatabase.asset` | 실험체 89개, 무기군 23개 |
| 서버 네트워크 레이어 | `Server/ProjectER.Server/Network/` | TcpGameServer, ClientSession, PacketDispatcher, PacketBuilder |
| 서버 로비 시스템 | `Server/ProjectER.Server/Lobby/` | LobbyRoom(18명), LobbyManager(동적 생성/삭제) |
| 서버 패킷 정의 | `Server/ProjectER.Core/Packets/` | C2S_Connect/Move/MatchRequest/MatchCancel/SelectCharacter, S2C_Connected/MoveSync/MatchQueued/MatchCancelled/MatchFound/PickDodged/PickStarted |
| 서버 핸들러 | `Server/ProjectER.Server/Handlers/` | ConnectHandler(버전 검증, 로비 배정), MoveHandler(stub), MatchRequestHandler, PickHandler |
| 서버 매치메이킹 | `Server/ProjectER.Server/Matchmaking/` | MatchmakingConfig(MinPlayers/MaxPlayers), MatchmakingQueue, MatchmakingManager |
| 서버 픽 단계 (1단계) | `Server/ProjectER.Server/Pick/` | PickManager(매치별 PickSession 관리), PickSession(30초 타이머, 선택 상태). 세션 생성 시 S2C_PickStarted(제한시간) 브로드캐스트, 미선택자 존재 시 S2C_PickDodged 전체 브로드캐스트 |
| 클라이언트 네트워크 | `Scripts/Network/` | NetworkClient(매치메이킹/픽 API 포함, PickPhaseRemainingSeconds로 서버 동기화 타이머 계산), MiniMsgPack(float 지원), PacketSerializer, PacketBuilder, Dispatcher |
| 접속/로비 씬 | `Scripts/Scene/`, `Assets/Scenes/` | ConnectSceneController, LobbySceneController(매치 찾기 버튼), 에디터 빌더 |
| 실험체 초상화 | `Scripts/Data/CharacterData.cs`(PortraitFull/Half/Mini), `Scripts/Editor/BserCharacterSpriteLinker.cs` | Eternal Return Fankit 이미지 자동 연결 (88/89, CravER 제외) |
| 캐릭터 선택 씬 (1차) | `Scripts/UI/Pick/*.cs`, `Scripts/Scene/PickSceneController.cs`, `Scripts/Editor/PickSceneBuilder.cs`, `Assets/Scenes/03_PickScene.unity` | 좌측 역할군 필터/이름순 정렬/검색 + 5열 그리드, 우측 선택 초상화 + 플레이어 슬롯 3칸 + 서버 동기화 타이머. 선택 시 서버 전송(C2S_SelectCharacter), 미선택 닷지 시 로비 복귀(S2C_PickDodged), 타이머는 S2C_PickStarted 기준. 테스트용 인게임 진입 버튼 포함. 채팅/스킨 목록/멀티플레이어 연동은 보류 |
| 이동 시스템 (그레이박스) | `Scripts/Character/PlayerController.cs`, `Scripts/Data/CharacterData.cs`(StoppingDistance) | NavMesh 클릭투무브, 우클릭 시 `ResetPath` 후 재설정 + acceleration/angularSpeed 튜닝으로 방향 즉시 전환 |
| 캐릭터 베이스 | `Scripts/Character/CharacterBase.cs`, `PlayerController.cs` | CharacterBase(IDamageable, MaxHp/CurrentHp 스텁), PlayerController 우클릭 우선순위 분기(공격 대상 > 상호작용 대상 > 이동). 상호작용 대상이 사거리 밖이면 이동 후 도착 시 자동 상호작용. CharacterState 머신/스킬 슬롯·공격 로직은 TODO |
| 전투 인터페이스 | `Scripts/Combat/IDamageable.cs`, `IAttackable.cs`, `Scripts/Interaction/IInteractable.cs` | 인터페이스 정의 완료, 공격/피격 로직 구현은 TODO |
| 루트박스 시스템 (그레이박스) | `Scripts/World/LootBox.cs`, `LootZone.cs`, `Scripts/Data/SpawnEntry.cs`, `ZoneSpawnData.cs` | IInteractable 구현. ZoneSpawnData(스폰 그룹) 1개를 5상자에 분배(자체 설계 알고리즘)해 영역 내 배치, 상호작용 시 InventorySystem으로 아이템 이전 후 파괴 |
| BSER 아이템 스폰 임포터 | `Scripts/Editor/BserItemSpawnImporter.cs` | `ItemSpawn.json` → areaCode 10 기준 ZoneSpawnData SO 5종 생성 (Common + 1001~1004). 현재 Common(필드 산개) 데이터는 미사용 |
| 인게임 씬 그레이박스 빌더 | `Scripts/Editor/InGameSceneBuilder.cs`, `Assets/Scenes/04_InGameScene.unity` | 바닥+NavMesh, 플레이어 프리팹(NavMeshAgent+InventorySystem+PlayerController) 자동 생성, 루트박스 4구역 자동 배치, 카메라 세팅 |

### 미구현 (다음 작업 대상)

| 시스템 | 우선순위 | 비고 |
|---|---|---|
| CharacterState 상태머신 | 핵심 | enum 기반 상태머신, 이동/공격/사망 상태 전환 |
| 스킬 슬롯 | 핵심 | Q/W/E/R(액티브)·D(무기)·F(전술)·T(패시브) 인터페이스 연동 |
| 전투 시스템 | 핵심 | 기본 공격/피격/사망 로직 (IDamageable.TakeDamage/Die 구현, 공격 판정) |
| 필드 아이템 줍기 (Common 구역) | 선택 | ItemSpawn.json areaSpawnGroup -1 데이터 기반 필드 산개 아이템, 별도 줍기 인터랙션 |
| 아이템 월드 드랍 | 핵심 | ItemDragHandler.OnEndDrag에서 슬롯(IItemDropTarget) 밖에 드롭 시 캐릭터 발 밑에 아이템 스폰 — 인게임 인벤토리 UI 구현 시 연동 |
| 가방 슬롯 간 드래그 이동 | 선택 | InventorySlotUI에 IItemDropTarget 적용 — 가방 재배치, 가방↔장비 드래그 장착/해제 |
| 이동 동기화 | 네트워크 | MoveHandler GameRoom 연동, S2C_MoveSync 브로드캐스트 |
| 전투 동기화 | 네트워크 | AttackHandler, S2C_TakeDamage/Die 패킷 |
| GameRoom | 서버 | 틱 루프, 플레이어 위치 관리 |
| 스탯 아이콘 | UI | 에셋 준비 후 StatDefs에 아이콘 슬롯 연결 |
| 무기 세부 필터 | UI | 캐릭터 선택 시 해당 캐릭터 무기군으로 표시 |
| 픽 화면 - 채팅/스킨/루트 선택 | 핵심 | 채팅창, 스킨 목록(에셋 준비 후), 루트 선택 등 마무리(30초) 단계 → 시작 시 인게임 씬(04) 로딩 |
| 픽 화면 - 멀티플레이어 연동 | 핵심 | 매칭된 다른 플레이어 정보를 플레이어 슬롯 2/3에 표시 (현재 로컬 1인만 슬롯 0에 표시) |
| 몬스터 AI | 선택 | 순찰 → 어그로 → 추격 |
| 미니맵 | 선택 | - |
| 금지구역 | 선택 | - |
