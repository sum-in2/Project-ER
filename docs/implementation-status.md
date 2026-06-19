## 구현 현황 (2026-06-15 기준)

설계 의도/규칙은 system-design.md를, 파일 위치는 project-structure.md를 참조.
이 표는 "무엇이 됐고 어디에 있나 + 설계와 다른 비자명한 점"만 기록한다.

### 완료

경로 기준: `Client/Assets/Scripts/` (클라이언트), `Server/` (서버)

| 시스템 | 주요 파일 | 비고 |
|---|---|---|
| 데이터 레이어 | `Scripts/Data/*.cs` | ItemData(등급)/ItemDatabase/RecipeData/RecipeDatabase + 열거형 전체 |
| 실험체 데이터 | `Scripts/Data/CharacterData.cs` 외 | BSER 89개 (역할군/교전거리/무기군/전투스탯) |
| 무기군 데이터 | `Scripts/Data/WeaponTypeInfoData.cs`, `WeaponTypeDatabase.cs` | BSER 23개 |
| 아이템 등급 | `Scripts/Data/ItemGrade.cs`, `ItemGradeColorConfig.cs` | Common~Mythic + 색상 SO |
| 인벤토리 시스템 | `Scripts/Inventory/InventorySystem.cs` | 가방 10 + 장비 5슬롯, 스택, 장착/해제, 이벤트 |
| 크래프팅 시스템 | `Scripts/Crafting/CraftingSystem.cs` | 재료 검증/차감/환불, 제작 가능 레시피 조회 |
| 인벤토리/크래프팅 UI | `Scripts/UI/Inventory*.cs`, `Crafting*.cs` | 슬롯 렌더링, 레시피/재료 표시 |
| 도감 UI | `Scripts/UI/ItemBrowserView.cs`, `ItemFilterController.cs`, `ItemBrowserSlotUI.cs`, `RouteSorter.cs` | 전체 아이템 탐색, 타입 필터, 등급색, 선택/획득, 루트 기반 정렬 |
| 스탯창 | `Scripts/UI/EquippedStatsPanel.cs` | 장착 스탯 합산 (4열 2그룹, 12종) |
| 목표 루트 UI | `Scripts/UI/TargetItemPanelUI.cs`, `TargetItemSlotUI.cs`, `TriangleIndicator.cs` | 부위별 5슬롯, 루트 추가, 우클릭 제거, 필요 재료 삼각형 표시 |
| 아이템 슬롯 추상화 | `Scripts/UI/IItemSlot.cs`, `IItemDropTarget.cs`, `ItemDragHandler.cs`, `BaseItemSlotUI.cs` | 드래그 앤 드롭. 설계는 system-design "드래그 앤 드롭" 섹션. 현재 도감→목표 루트 슬롯만 구현 |
| LoadOut 패널 | `Scripts/UI/LoadOutPanel.cs`, `Scripts/Editor/LoadOutPanelBuilder.cs` | 인벤토리/크래프팅/도감/루트 3패널 프리팹 자동 생성 |
| BSER 임포터/링커 | `Scripts/Editor/Bser*.cs` | JSON → SO(아이템/레시피/실험체/무기군/스폰) 자동 생성 + 아이콘·초상화 매핑 |
| 게임 데이터(SO) | `ScriptableObjects/.../BSER/` | 아이템 786, 레시피 666, 실험체 89, 무기군 23 |
| 실험체 초상화 | `CharacterData.cs`(Full/Half/Mini), `BserCharacterSpriteLinker.cs` | Fankit 이미지 자동 연결 (88/89, CravER 제외) |
| 서버 네트워크/로비 | `Server/ProjectER.Server/Network/`, `Lobby/` | TcpGameServer/ClientSession/Dispatcher, LobbyRoom(18명)/LobbyManager |
| 서버 매치메이킹/픽 | `Server/ProjectER.Server/Matchmaking/`, `Pick/` | Config/Queue/Manager, PickSession(30초 타이머)/PickManager (픽 1단계) |
| 서버 핸들러 | `Server/ProjectER.Server/Handlers/` | Connect(버전 검증)/Login/Move(stub)/MatchRequest/Pick |
| 계정 시스템 | `Server/ProjectER.Server/Database/`, `Handlers/LoginHandler.cs`, `Scripts/Scene/LoginSceneController.cs` | 로그인/회원가입 (AccountDb/Repository, C2S_Login/Register, 01_LoginScene) |
| 패킷 정의 | `Server/ProjectER.Core/Packets/`, `Scripts/Network/Protocol/` | C2S/S2C 양쪽 동기화. 클라 `Scripts/Network/`(NetworkClient/MiniMsgPack/Serializer/Dispatcher) |
| 접속/로비 씬 | `Scripts/Scene/`, `Scripts/UI/Lobby*.cs` | Connect/Lobby Controller, LobbyUIManager(패널 전환) |
| 캐릭터 선택 씬 (1차) | `Scripts/UI/Pick/*.cs`, `PickSceneController.cs`, `PickSceneBuilder.cs` | 필터/정렬/검색 + 5열 그리드, 선택 초상화/플레이어 슬롯 3칸/서버 동기화 타이머. 채팅/스킨/멀티 연동 보류 |
| 이동 (그레이박스) | `Scripts/Character/PlayerController.cs` | NavMesh 클릭투무브, 우클릭 시 ResetPath 후 재설정 + accel/angularSpeed 튜닝 |
| 캐릭터 베이스 | `Scripts/Character/CharacterBase.cs` | IDamageable + HP, 상태머신 소유. HP 0 → Downed(빈사), Revive() 부활, Die() 최종 사망. 스킬/공격은 TODO |
| 상태머신 | `Scripts/Character/State/*.cs` | State 패턴 + enum 하이브리드. Idle/Move/Downed/Dead 구현. MoveState가 NavMeshAgent 직접 구동, 빈사 중 입력 차단. Attack/Skill 상태는 enum에만 (전투·스킬 작업 때 추가). 빈사 스탯/전용 스킬셋은 TODO |
| 전투/스킬 인터페이스 | `Scripts/Combat/`, `Scripts/Interaction/`, `Scripts/Skill/` | 인터페이스 정의만 완료, 로직 구현 TODO |
| 루트박스 (그레이박스) | `Scripts/World/LootBox.cs`, `LootZone.cs`, `Scripts/Data/SpawnEntry.cs`, `ZoneSpawnData.cs` | IInteractable. 스폰 그룹 1개를 5상자에 자체 알고리즘으로 분배, 상호작용 시 인벤토리 이전 후 파괴 |
| 아이템 스폰 임포터 | `Scripts/Editor/BserItemSpawnImporter.cs` | ItemSpawn.json → areaCode 10 ZoneSpawnData 5종. Common(필드 산개) 미사용 |
| 인게임 씬 빌더 | `Scripts/Editor/InGameSceneBuilder.cs`, `04_InGameScene.unity` | 바닥+NavMesh, 플레이어 프리팹, 루트박스 4구역, 카메라 자동 세팅 |
| 인게임 테스트 HUD | `Scripts/UI/InGame/*.cs`, `Scripts/Editor/InGameHudBuilder.cs` | PlayerStatusHud(HP바+상태), CombatTestPanel(데미지/회복/빈사/부활/사망/아이템/스킬 스텁 버튼), InGameInventoryBar(하단 우측 10칸). CharacterBase에 OnHpChanged/OnStateChanged/Heal 추가. 빌더가 Player에 참조 연결 (메뉴 Build InGame HUD) |

### 미구현 (다음 작업 대상)

| 시스템 | 우선순위 | 비고 |
|---|---|---|
| Attack/Skill 상태 | 핵심 | 상태머신에 Attack/Skill 구체 상태 추가 (현재 enum에만 존재) |
| 스킬 슬롯 | 핵심 | Q/W/E/R(액티브)·D(무기)·F(전술)·T(패시브) 인터페이스 연동. 빈사 시 전용 스킬셋 교체 포함 |
| 전투 시스템 | 핵심 | 기본 공격/피격/사망 로직 (공격 판정, 데미지 계산). 빈사 스탯 변경 적용 포함 |
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
