## 구현 현황 (2026-06-24 기준)

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
| 루트 선택 (픽 2단계, 선택만) | `Scripts/UI/Pick/RouteSelectPanelUI.cs`, `RouteEntrySlotUI.cs`, `Scripts/Data/SavedRoute.cs`, `ISavedRouteSource.cs`, `StubSavedRouteSource.cs`, `MatchSelectionData.cs` | 확인 버튼(시작 버튼 좌측) → 그리드 비활성 + 좌측 오버레이로 저장 루트 목록 표시 → 단일 선택. 저장 루트는 현재 StubSavedRouteSource(ItemDatabase 완성장비 더미). 선택 실험체+루트는 MatchSelectionData(SO 캐리어)에 기록해 인게임으로 전달. 루트 생성 UI·계정 DB 영속화·서버 전송(C2S_SelectRoute)은 TODO. PickSceneBuilder가 패널/버튼/캐리어 에셋 자동 생성 |
| 루트 인게임 우선표기 | `Scripts/UI/RouteSorter.cs`(SavedRoute 오버로드), `LootBoxUI.cs`/`LootBoxSlotUI.cs`, `Scripts/UI/InGame/CraftableItemBar.cs` | MatchSelectionData의 선택 루트 → RouteSorter.CollectAllNeededItemIds(목표 장비+하위 재료 재귀)로 필요 ID 집합 산출. 루트박스 4x4: 필요 재료 슬롯 좌상단 노란 삼각형(런타임 생성). 조합칸 5칸: 루트 관련 레시피 2패스 우선 채움(기존 "조합칸 정렬" TODO 일부 흡수). LootBoxUIBuilder/InGameHudBuilder가 RecipeDatabase·MatchSelectionData 연결 |
| 목표 아이템 HUD | `Scripts/UI/InGame/TargetRouteHud.cs`, `Scripts/Editor/InGameHudBuilder.cs` | 인게임 우상단(상태 HUD 아래) 선택 루트 목표 장비 5종 표시. 부위 순서 5칸 런타임 생성, 등급색 배경+아이콘. 각 슬롯 하단에 노말(Common) 기초 재료 진행도 "보유/필요"(예 0/5, 완료 시 초록) — RouteSorter.CollectBaseMaterials(레시피 트리 잎까지 수량 전개) + 인벤토리 조회, 인벤토리 변경 시 갱신. MatchSelectionData에서 루트 읽음(미선택 시 빈 슬롯). InGameHudBuilder가 패널 생성+참조(루트/등급색/인벤토리/레시피DB) 연결. 더미 루트(StubSavedRouteSource)는 영웅(Epic) 완성장비 한정 |
| 이동 (그레이박스) | `Scripts/Character/PlayerController.cs` | NavMesh 클릭투무브, 우클릭 시 ResetPath 후 재설정 + accel/angularSpeed 튜닝 |
| 캐릭터 베이스 | `Scripts/Character/CharacterBase.cs` | IDamageable + HP, 상태머신 소유. HP 0 → Downed(빈사), Revive() 부활, Die() 최종 사망. 스킬/공격은 TODO |
| 상태머신 | `Scripts/Character/State/*.cs` | State 패턴 + enum 하이브리드. Idle/Move/Downed/Dead 구현. MoveState가 NavMeshAgent 직접 구동, 빈사 중 입력 차단. Attack/Skill 상태는 enum에만 (전투·스킬 작업 때 추가). 빈사 스탯/전용 스킬셋은 TODO |
| 전투/스킬 인터페이스 | `Scripts/Combat/`, `Scripts/Interaction/`, `Scripts/Skill/` | 인터페이스 정의만 완료, 로직 구현 TODO |
| 데미지 계산기 | `Scripts/Combat/DamageCalculator.cs` 외 | ER 평타 공식(combat-damage-formula.md). CombatConstants/CombatMath/IDamageProfile(Basic·Skill struct)/DamageModifiers. 방어력/치명타 반영, 순수 함수·GC 없음(제네릭). 방어자 방어력=ICombatStats(CharacterBase). 증폭/고정추가/모드/방관/치피는 미보유 스탯 → 중립. AttackState가 평타에 연동(간격마다 즉시 타격) |
| 런타임 전투 스탯 집계 | `Scripts/Combat/CombatStats.cs`, `CombatStatsBuilder.cs` | 기본 스탯(CharacterData) + 장착 장비(InventorySystem) 합산 → 최종 CombatStats(불변 struct). 공속=기본×(1+공속비율합), AttackSpeedLimit 클램프. PlayerController가 Start·OnEquipmentChanged에서 Build→적용(이동속도/공격력/치명타/공격간격), CharacterBase.ApplyVitalStats로 최대체력·방어력 반영. 치명타 확률은 0~1 비율(BSER 원본 스케일). 무기군 사거리·세부 스탯(쿨감/생흡 등 소비)은 미연동 |
| 루트박스 (그레이박스) | `Scripts/World/LootBox.cs`, `LootZone.cs`, `Scripts/Data/SpawnEntry.cs`, `ZoneSpawnData.cs` | IInteractable. 스폰 그룹 1개를 5상자에 자체 알고리즘으로 분배. 상호작용 시 4x4 UI 열림 → 슬롯 클릭으로 개별 취득(가방 가득 시 취득 거부, 박스에 유지), 전부 취득 시 박스 파괴 |
| 아이템 스폰 임포터 | `Scripts/Editor/BserItemSpawnImporter.cs` | ItemSpawn.json → areaCode 10 ZoneSpawnData 5종. Common(필드 산개) 미사용 |
| 인게임 씬 빌더 | `Scripts/Editor/InGameSceneBuilder.cs`, `04_InGameScene.unity` | 바닥+NavMesh, 플레이어 프리팹, 루트박스 4구역, 카메라 자동 세팅 |
| 인게임 테스트 HUD | `Scripts/UI/InGame/*.cs`, `Scripts/Editor/InGameHudBuilder.cs` | PlayerStatusHud(HP바+상태), CombatTestPanel(데미지/회복/빈사/부활/사망/아이템/스킬 스텁 버튼), InGameInventoryBar(우하단 테두리 영역 5x2), CraftableItemBar(인벤토리 위 조합 5칸 — 제작 가능 레시피 결과물 표시, 인벤토리/열린 박스 변경 시 갱신, 클릭 시 TryCraft 실행. 열린 루트박스 내용물도 재료로 포함(IMaterialSource), 조합 시 소모 우선순위 인벤토리 > 박스. 표시 순서 정렬은 TODO). InGameSceneBuilder가 Player에 CraftingSystem 추가(InventorySystem+RecipeDatabase 연결). CharacterBase에 OnHpChanged/OnStateChanged/Heal 추가. 빌더가 Player에 참조 연결 (메뉴 Build InGame HUD) |

### 미구현 (다음 작업 대상)

| 시스템 | 우선순위 | 비고 |
|---|---|---|
| Attack/Skill 상태 | 핵심 | 상태머신에 Attack/Skill 구체 상태 추가 (현재 enum에만 존재) |
| 스킬 슬롯 | 핵심 | Q/W/E/R(액티브)·D(무기)·F(전술)·T(패시브) 인터페이스 연동. 빈사 시 전용 스킬셋 교체 포함 |
| 전투 시스템 (잔여) | 핵심 | 기본 공격/피격/사망 루프 + 데미지 계산(IDamageCalculator) + 장비 스탯 합산은 완료(위 표 참조). 잔여: 사거리 이탈 시 자동 추격(현재 Idle 복귀 후 재클릭), 대상 사망 시 공격 중단, 빈사 스탯 변경 적용(이동속도 감소 등), 무기군 데이터 기반 공격 사거리(현재 PlayerController 임시 _attackRange). 데미지 판정 시점은 코드(상태머신)가 소유 — 애니메이션 이벤트로 데미지 트리거 금지(서버 검증 원칙·공속핵 방지) |
| 공격 모션 / 타격 타이밍 | 핵심 | AttackState에 모션 연동. 공격 한 사이클 = 1/공격속도 → Animator.speed로 모션 재생속도 스케일. 클립 비율 기준 hitTimeRatio(전조→타격→후딜)로 타격 프레임에 데미지 1회 적용(현재는 간격마다 즉시 타격). 타격 후 후딜 이동 캔슬(어택땅) 여지 확보. 이동속도는 공격 모션과 무관(Locomotion Blend Tree). 클립 에셋 준비 후 작업 |
| 필드 아이템 줍기 (Common 구역) | 선택 | ItemSpawn.json areaSpawnGroup -1 데이터 기반 필드 산개 아이템, 별도 줍기 인터랙션 |
| 아이템 월드 드랍 | 핵심 | ItemDragHandler.OnEndDrag에서 슬롯(IItemDropTarget) 밖에 드롭 시 캐릭터 발 밑에 아이템 스폰 — 인게임 인벤토리 UI 구현 시 연동 |
| 가방 슬롯 간 드래그 이동 | 선택 | InventorySlotUI에 IItemDropTarget 적용 — 가방 재배치, 가방↔장비 드래그 장착/해제 |
| 이동 동기화 | 네트워크 | MoveHandler GameRoom 연동, S2C_MoveSync 브로드캐스트 |
| 전투 동기화 | 네트워크 | AttackHandler, S2C_TakeDamage/Die 패킷 |
| GameRoom | 서버 | 틱 루프, 플레이어 위치 관리 |
| 조합칸 표시 순서 정렬 (잔여) | UI | 루트 우선 정렬은 구현됨(위 "루트 인게임 우선표기"). 잔여: 루트 외 일반 정렬 기준(등급/부위 등) 추가 |
| 스탯 아이콘 | UI | 에셋 준비 후 StatDefs에 아이콘 슬롯 연결 |
| 무기 세부 필터 | UI | 캐릭터 선택 시 해당 캐릭터 무기군으로 표시 |
| 픽 화면 - 채팅/스킨 + 루트 생성/영속화 | 핵심 | 채팅창, 스킨 목록(에셋 준비 후). 루트는 선택 UI만 구현됨(위 표) — 루트 생성 UI, 계정 DB 영속화(해시 외래키), 서버 전송(C2S_SelectRoute)은 미구현. 2단계(30초) 타이머/서버 트리거로 인게임(04) 로딩 연동도 TODO |
| 픽 화면 - 멀티플레이어 연동 | 핵심 | 매칭된 다른 플레이어 정보를 플레이어 슬롯 2/3에 표시 (현재 로컬 1인만 슬롯 0에 표시) |
| 몬스터 AI | 선택 | 순찰 → 어그로 → 추격 |
| 미니맵 | 선택 | - |
| 금지구역 | 선택 | - |
