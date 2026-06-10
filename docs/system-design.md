## 시스템별 설계 가이드

### 이동 시스템 (Click-to-Move)

```
NavMesh 기반 클릭투무브
- 마우스 우클릭 → Raycast → NavMeshAgent.SetDestination()
- NavMeshAgent 설정: stoppingDistance, speed는 CharacterData(SO)에서 읽음
- 스킬 대시/순간이동: NavMeshAgent 일시 정지 후 직접 위치 이동
```

### 전투 시스템

```
인터페이스 설계:
- IDamageable   : TakeDamage(float amount)
- IAttackable   : Attack(IDamageable target)
- ISkillUser    : UseSkill(int skillIndex)

CharacterBase (MonoBehaviour + IDamageable):
├── PlayerController : CharacterBase, ISkillUser
└── MonsterController : CharacterBase, IAttackable

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
ConnectScene (0) → LoginScene (1) → LobbyScene (2) → GameScene
- ConnectScene: IP/포트 입력, 접속 버튼, 상태 텍스트
- LoginScene: 아이디/비밀번호 입력, 로그인/회원가입 버튼
- LobbyScene: 매치 찾기 버튼, 연결 해제 버튼
- NetworkClient 이벤트 구독은 OnEnable이 아닌 Start에서 수행 (Instance 보장)

서버 실행:
cd Server && dotnet run --project ProjectER.Server
- 포트: 7777 (고정)
- 버전: 0.1.0 (클라이언트와 일치해야 접속 수락)
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
