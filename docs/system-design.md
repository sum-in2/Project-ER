## 시스템별 설계 가이드

### 핵심 인터페이스 설계

```
IDamageable    : TakeDamage(float amount), Die()
IAttackable    : Attack(IDamageable target)

ISkill (공통 베이스)
├── float Cooldown { get; }
├── bool IsReady { get; }
└── void Activate(CharacterBase caster)

IActiveSkill   : ISkill   → Q/W/E/R (액티브 스킬 1~4)
IWeaponSkill   : ISkill   → D (무기 스킬)
ITacticalSkill : ISkill   → F (전술 스킬)

IPassiveSkill (T)
├── void OnEquip(CharacterBase owner)
└── void OnUnequip(CharacterBase owner)

IInteractable  : void Interact(PlayerController interactor)
- 월드 아이템 줍기도 Interact() 내부에서 인벤토리 추가 처리 (별도 IPickupable 불필요)

우클릭 입력 처리 우선순위 (Raycast 결과 기준):
1. IDamageable 대상   → 공격
2. IInteractable 대상 → 상호작용 (줍기 포함)
3. 그 외             → 이동 (NavMeshAgent.SetDestination)
```

### 이동 시스템 (Click-to-Move)

```
NavMesh 기반 클릭투무브
- 마우스 우클릭 → Raycast → NavMeshAgent.SetDestination()
- NavMeshAgent 설정: stoppingDistance, speed는 CharacterData(SO)에서 읽음
- 스킬 대시/순간이동: NavMeshAgent 일시 정지 후 직접 위치 이동
```

### 전투 시스템

```
CharacterBase (MonoBehaviour + IDamageable):
├── PlayerController : CharacterBase
│     - IActiveSkill x4 (Q/W/E/R), IWeaponSkill(D), ITacticalSkill(F), IPassiveSkill(T) 보유
└── MonsterController : CharacterBase, IAttackable

(인터페이스 정의는 "핵심 인터페이스 설계" 섹션 참조)

스탯: CharacterData(ScriptableObject)에서 읽어옴
상태 관리: enum CharacterState + StateMachine 구조 사용 (bool 플래그 남발 금지)
```

### 인벤토리 & 아이템

```
데이터 레이어:
- ItemData (ScriptableObject): ID, 이름, 아이콘, 타입, 등급, 스탯 보너스
- ItemDatabase (ScriptableObject): List<ItemData> + Dictionary 조회용 캐시

런타임 레이어:
- InventorySystem: 슬롯 관리, 아이템 추가/제거/이동
- InventorySlot: 아이템 참조 + 수량

이벤트: ScriptableObject 이벤트 채널 방식 권장

아이템 분류:
- 장비
-- 무기, 옷, 머리, 팔, 신발
--- 무기는 확장 가능하게 해야함 / 종류 많음
- 음식
- 재료
- 소비 / 사용가능 아이템

장비 슬롯 고정 순서 (UI 표시, 정렬, 목표 루트 슬롯 모두 동일하게 적용):
무기 → 옷(Chest) → 머리(Helmet) → 팔(Arms) → 신발(Shoes)
- EquipmentSlotType 열거 순서도 이를 따름
- 도감 아이템 정렬: 무기 → 옷 → 머리 → 팔 → 신발 → 음식 → 소비 → 재료 순

아이템 등급 (ItemGrade enum, 낮은 숫자 = 낮은 등급):
Common(0) → Uncommon(1) → Rare(2) → Epic(3) → Legend(4) → Mythic(5)
- BSER JSON의 itemGrade 문자열과 1:1 대응
- 등급별 색상은 ItemGradeColorConfig (SO) 에서 관리 → Client/Assets/ScriptableObjects/ItemGradeColorConfig.asset

장비 스탯 목록 (ItemData 필드):
공격력(AttackPower), 방어력(Defense), 최대체력(MaxHpBonus), 이동속도(MoveSpeedBonus),
공격속도(AttackSpeedBonus), 스킬증폭(SkillAmp), 쿨감(CooldownReduction),
적응형(AdaptiveForce), 치명타(CriticalStrikeChance), 생명흡수(LifeSteal),
체력재생(HpRegenRatio, 방어구 전용), 방어관통(PenetrationDefense, 방어구 전용)
```

### 도감 시스템

```
용어 정의:
- 도감: 가방·루트를 제외한 전체 아이템 탐색 화면 (중앙 패널)
- 루트: 목표 아이템 슬롯(TargetItemPanelUI)에 설정된 목표 장비 조합

도감 기능:
- 전체 아이템(ItemDatabase 786개)을 등급색 배경으로 표시
- 좌클릭 → 아이템 선택 (노란 강조)
- 획득 버튼 → 선택 아이템을 가방에 추가
- 루트 추가 버튼 → 선택 장비를 목표 루트 슬롯에 자동 배치 (부위 기준)

도감 정렬 (우선순위 순):
1. 루트 기반: 목표 아이템의 모든 재료(재귀 탐색)를 앞으로
2. 타입 기반: 무기 → 옷 → 머리 → 팔 → 신발 → 음식 → 소비 → 재료
3. 등급 기반: Common → Uncommon → Rare → Epic → Legend → Mythic

도감 필터 (좌측 1열):
전체 / 무 / 옷 / 머 / 팔 / 신 / 음 / 소 / 재
- 무기 세부 분류(23종)는 캐릭터 선택 시 해당 캐릭터 무기군으로 표시 예정

루트 필요 재료 표시:
- 목표 루트의 필요 재료인 슬롯 좌상단에 노란 삼각형(TriangleIndicator) 표시
- 루트 변경 시 실시간 갱신

스탯 아이콘:
- 현재 미구현 (에셋 준비 후 추가 예정)
- InventoryTestPanel.StatDefs에 아이콘 슬롯 자리 확보됨
```

### 목표 루트 시스템

```
TargetItemPanelUI:
- 부위별 5슬롯 (무기·옷·머리·팔·신발 고정 순서)
- TrySetItem(ItemData): 아이템 부위에 맞는 슬롯에 자동 배치
- OnTargetChanged 이벤트 → 도감 정렬/삼각형 표시 갱신

TargetItemSlotUI:
- 우클릭 → 슬롯 초기화
- 슬롯 배경에 등급색 표시
- IDropHandler 구현 (드래그 드롭 수신 가능, 현재 버튼 방식 사용)
```

### 드래그 앤 드롭 (아이템 슬롯)

```
IItemSlot       : CurrentItem(현재 아이템), IsDraggable(드래그 가능 여부)
IItemDropTarget : TryDropItem(IItemSlot source) → 처리 성공 시 true

ItemDragHandler (IBeginDragHandler/IDragHandler/IEndDragHandler):
- OnBeginDrag : IsDraggable == true && CurrentItem != null 인 슬롯만 드래그 시작 (커서 추적용 비주얼 생성)
- OnEndDrag   : 도착 지점 판정 후 처리
  1. 마우스 포인터 아래(eventData.pointerCurrentRaycast)에 IItemDropTarget 구현 슬롯이 있는 경우
     → 슬롯에 옮김: 해당 슬롯의 TryDropItem(source) 호출
  2. 포인터 아래에 IItemDropTarget이 없는 경우(= 월드맵)
     → 캐릭터 발 밑에 아이템 드랍 (TODO: 인게임 인벤토리 UI 구현 시 월드 아이템 스폰 연동)

도착 지점 판정 기준: 마우스 포인터 위치가 슬롯(IItemDropTarget) 영역 안인지 여부
- 슬롯 안  → 1번(슬롯에 옮김)
- 슬롯 밖  → 2번(월드맵 드랍)

현재 IItemDropTarget 구현: TargetItemSlotUI (도감 → 목표 루트 슬롯 배치)
- 향후 InventorySlotUI(가방/장비)에도 적용해 가방 내 재배치·장착/해제 드래그 지원 예정 (TODO)
```

### 크래프팅 시스템

```
RecipeData (ScriptableObject):
- List<ItemIngredient> ingredients (ItemData + 수량)
- ItemData resultItem
- int resultAmount

CraftingSystem:
- RecipeDatabase에서 레시피 조회
- 인벤토리에 재료 충분한지 검증 후 제작
- 조회 성능: Dictionary<string, RecipeData> 로 캐싱
```

### 네트워크 시스템

```
패킷 프로토콜:
- 헤더 4바이트: [TotalLength(2) | PacketType(2)] 리틀엔디안
- 바디: MessagePack 직렬화 (서버), MiniMsgPack 호환 구현 (클라이언트)
- 새 패킷 추가 시: PacketType 열거형 → Core 패킷 클래스 → 서버 핸들러 → 클라이언트 Serializer 순으로 작업

서버 아키텍처:
- TcpGameServer: 연결 수락, ClientSession 생성, 브로드캐스트
- ClientSession: 세션 1개의 수신/송신 루프 (async)
- PacketDispatcher: PacketType → 핸들러 라우팅
- LobbyManager: 세션을 LobbyRoom 단위로 분산 관리 (메모리 관리 목적)
- LobbyRoom: 최대 18명 단위 세션 그룹 (UI 표시 없음, 서버 내부 용도)

클라이언트 아키텍처:
- NetworkClient (MonoBehaviour, DontDestroyOnLoad): TcpClient 래퍼
  - 수신: 백그라운드 Thread → ConcurrentQueue → Update()에서 메인 스레드 처리
  - 이벤트: OnConnectSuccess, OnConnectFailed, OnDisconnected
- ClientPacketDispatcher: PacketType → 핸들러 라우팅
- MiniMsgPack: MessagePack 호환 최소 구현 (외부 라이브러리 불필요)

씬 흐름:
00_ConnectScene → 01_LoginScene → 02_LobbyScene → 03_PickScene → 04_InGameScene
- ConnectScene: IP/포트 입력, 접속 버튼, 상태 텍스트
- LoginScene: 아이디/비밀번호 입력, 로그인/회원가입 버튼
- LobbyScene: 매치 찾기 버튼, 연결 해제 버튼
- PickScene (예정): 매치 성사 후 캐릭터 선택(30초) + 루트 선택 등 마무리(30초) 진행, 종료 시 InGameScene 로딩
- NetworkClient 이벤트 구독은 OnEnable이 아닌 Start에서 수행 (Instance 보장)

서버 실행:
cd Server && dotnet run --project ProjectER.Server
- 포트: 7777 (고정)
- 버전: 0.1.0 (클라이언트와 일치해야 접속 수락)
```

### 매칭 중 데이터 잠금 규칙

```
매칭 큐 등록 시점 = 출전 정보(인벤토리/장비 구성)가 서버에 제출/확정되는 시점
- NetworkClient.IsMatchmaking == true 인 동안:
  - 인벤토리/도감의 아이템 획득·장착·교체 등 상태 변경 UI는 비활성화(interactable = false)
  - 서버도 매칭 중인 세션의 인벤토리 변경 패킷은 거부 (정합성 보장)
- 매치 취소(OnMatchCancelled) 시 잠금 해제

목적: 클라이언트가 들고 있는 인벤토리 상태와 서버에 제출된 출전 정보가
서로 어긋나는(diverge) 것을 방지 (동시성/정합성 문제)
```

### 픽 화면 진행 흐름

```
픽 씬(03_PickScene)은 매치 성사 후 60초(30초 x 2단계)로 진행:

1단계 (0~30초, 구현됨): 실험체 선택
- CharacterGridUI에서 실험체 선택 → C2S_SelectCharacter 전송 (CharacterId = CharacterData.BserCode)
  - 재선택 시마다 갱신 전송 (서버는 마지막 선택값으로 덮어씀)
- 서버 PickManager/PickSession이 매치별 선택 상태와 30초 타이머를 관리
- 타이머 종료 시:
  - 전원 선택 완료 → 2단계로 진행 (TODO)
  - 미선택자 존재 → 매치 전체 닷지: S2C_PickDodged를 참가자 전원에게 브로드캐스트
    → 클라이언트는 02_LobbyScene으로 복귀
- 연결 해제로 픽 단계 중 이탈하는 세션이 있어도 동일하게 매치 전체 닷지 처리

2단계 (30~60초, TODO): 루트/전술스킬/스킨 선택
- 1단계의 CharacterGridUI 자리에 루트 선택 UI 배치 (TargetItemPanelUI 재사용 검토)
- 전술스킬(F)은 TacticalSkillSet.json 기반, 스킨은 미구현 → 더미/스킵 처리
- 60초 종료 시 서버가 전원의 선택 정보를 모아 InGameScene 진입 트리거 (S2C_GameStart류, TODO)

인게임 진입 전 서버에 저장되는 데이터:
- 실험체 ID (구현됨, PickSession._selections)
- 루트(목표 장비 5종) / 전술스킬 ID / 스킨 ID (TODO, 2단계에서 추가)

서버 구조:
- PickManager: 매치별 PickSession 생성/조회, sessionId → PickSession 매핑
- PickSession: 참가자 목록, 선택 상태(Dictionary<sessionId, characterId>), 30초 단발 Timer
- 향후 GameRoom(틱 루프, 게임 상태 관리)으로 확장될 수 있는 최소 단위

시간 흐름(타이머)은 서버가 관리:
- PickSession 생성 시 참가자 전원에게 S2C_PickStarted(DurationSeconds) 브로드캐스트
- 클라이언트는 패킷 수신 시각(Time.realtimeSinceStartup)을 기준으로 남은 시간을 계산해
  PickTimerUI에 표시 (NetworkClient.PickPhaseRemainingSeconds)
- 닷지/타임아웃 판정은 클라이언트 타이머와 무관하게 서버 PickSession의 Timer가 단독으로 수행
  → 클라이언트 타이머는 표시(UX) 용도일 뿐, 실제 제한시간 기준은 항상 서버

테스트용 인게임 진입:
- 우측 패널 "테스트: 인게임 진입" 버튼 → 02단계/서버 트리거 없이 04_InGameScene으로 즉시 전환
- 2단계(루트 선택) 및 S2C_GameStart 구현 전까지 인게임 씬 작업을 위한 임시 진입점
```

### 서버 검증 설계 원칙 (안티치트)

```
서버를 두는 이유 = 클라이언트를 신뢰하지 않기 위함.
패킷 후킹/조작으로 클라이언트가 무엇을 보내든, 서버는 자체 데이터/상태를 기준으로
다시 검증해야 한다. "클라가 보낸 값이니 맞겠지"라고 가정하는 코드는 작성하지 않는다.

적용된 예 (픽 1단계):
- 인증 검증: PickHandler.HandleSelectCharacter에서 session.AccountId == null이면 거부
  (로그인 안 된 세션의 요청 차단 — MatchRequestHandler와 동일 패턴)
- 데이터 검증: PickManager.HandleSelectCharacter에서 CharacterId가
  CharacterCatalog.ValidCharacterIds(Server/ProjectER.Core/Data/CharacterCatalog.cs,
  Character.json의 code 1~89 기준)에 없으면 무시
  → 패킷 조작으로 존재하지 않는 실험체 ID를 보내도 서버 상태에 반영되지 않음

향후 시스템에도 동일 원칙 적용 (TODO):
- 이동: 클라이언트가 보낸 좌표/속도가 NavMesh·CharacterData의 이동속도 범위를 벗어나면 거부
  (이동속도 핵, 텔레포트 핵 방지)
- 전투: 데미지 계산은 서버가 CharacterData/ItemData 스탯으로 직접 계산,
  클라이언트가 보낸 데미지 값은 신뢰하지 않음 (데미지 조작 방지)
- 인벤토리/루트: 아이템 획득·장착·루트 구성은 서버가 보유한 ItemDatabase/RecipeDatabase
  기준으로 재검증 (인벤토리 조작 방지). "매칭 중 데이터 잠금 규칙" 섹션과 함께 적용

검증 실패 시 공통 처리:
- 패킷을 조용히 무시 + Console.WriteLine으로 거부 로그 남김 (연결 차단 등 과한 처벌은 보류)
- 클라이언트에는 별도 에러 응답을 보내지 않음 (정상 클라이언트는 발생할 일이 없는 케이스)
```

### 에디터 씬 빌더 (NetworkSceneBuilder)

```
에디터로 씬을 코드로 생성할 때 반드시 포함해야 하는 오브젝트:

1. Main Camera
   - tag: "MainCamera"
   - Camera 컴포넌트 + AudioListener 컴포넌트
   - clearFlags: SolidColor

2. EventSystem
   - EventSystem 컴포넌트
   - InputSystemUIInputModule 컴포넌트  ← 반드시 이것 사용
     (프로젝트가 New Input System을 사용하므로 StandaloneInputModule 사용 금지)
   - using UnityEngine.InputSystem.UI; 필요

누락 시 버튼/UI 클릭이 동작하지 않거나 아래 오류 발생:
InvalidOperationException: You are trying to read Input using the UnityEngine.Input class,
but you have switched active Input handling to Input System package in Player Settings.
```
