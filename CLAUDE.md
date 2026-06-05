# CLAUDE.md — project-er (이터널리턴 모작 포트폴리오)

## 프로젝트 개요

이터널리턴(Eternal Return)을 레퍼런스로 한 포트폴리오용 Unity 모작 프로젝트.
클라이언트 프로그래머 기술 어필을 목적으로 한다.

- **엔진**: Unity (URP)
- **언어**: C# (.NET Standard 2.1)
- **플랫폼**: PC (Windows)
- **라이센스**: MIT

### 구현 범위 (스코프)

**핵심 구현 (반드시)**

- 클릭투무브 이동 (NavMesh 기반)
- 캐릭터 1~2종 (기본 공격 + 스킬 포함)
- 기본 전투 (공격, 피격, 사망)
- 아이템 줍기 + 인벤토리 시스템
- 크래프팅 시스템 (재료 조합 → 아이템 제작)

**선택 구현 (여유 시 추가)**

- 몬스터 AI (순찰 → 어그로 → 추격)
- 미니맵
- 금지구역 시스템

**제외 (스코프 아웃)**

- 멀티플레이어 / 네트워킹
- 전체 맵 16구역 재현
- 캐릭터 40종 이상 구현

---

## 코딩 원칙

### OOP 4대 원칙

- **캡슐화**: 필드는 `private` + `[SerializeField]`, 외부 접근은 프로퍼티로
- **상속보다 컴포지션** 우선 고려
- **다형성**: `if/else` 타입 분기 대신 인터페이스 + 오버라이딩 활용
- **추상화**: 구현 세부사항은 숨기고 인터페이스만 노출

### SOLID 원칙

- **S (단일책임)**: 클래스 하나 = 책임 하나. 역할이 늘어나면 클래스 분리
- **O (개방/폐쇄)**: 기존 코드 수정 없이 확장 가능하도록 인터페이스 기반 설계
- **L (리스코프)**: 자식 클래스는 부모를 완전히 대체 가능해야 함
- **I (인터페이스 분리)**: 인터페이스는 작게 분리, 불필요한 메서드 강제 구현 금지
- **D (의존성 역전)**: 구체 클래스 직접 의존 금지, 생성자 주입(DI) 방식 선호

---

## 코드 작성 규칙

### 네이밍 컨벤션

```
인터페이스       : I 접두사        → IDamageable, IAttackable, ICraftable
추상 클래스      : Base 접미사     → CharacterBase, ItemBase
컴포넌트 클래스  : 역할 명사       → PlayerController, InventorySystem
ScriptableObject : Data 접미사    → ItemData, RecipeData, CharacterData
이벤트           : On 접두사      → OnDeath, OnItemPickup, OnCraftComplete
```

### 필수 컨벤션

- `public` 필드 금지 → `[SerializeField] private` 사용
- `GetComponent<T>()` 대신 `TryGetComponent<T>()` 사용
- 런타임에서 `Find()`, `FindObjectOfType()` 호출 금지 → 직접 참조 또는 DI
- 태그 비교는 `CompareTag()` 사용 (string 할당 방지)
- 이벤트 구독/해제는 `OnEnable` / `OnDisable` 쌍으로
- 매직 넘버 금지 → `const` 또는 `[SerializeField]` 상수로 선언

### 하지 말 것

- **God Class 금지**: GameManager 하나에 모든 로직 몰아넣기 금지
- **static 남용 금지**: 전역 상태가 필요하면 ScriptableObject 이벤트 채널 사용
- **Update() 내 GC 유발 코드 금지**: LINQ, string 연결, `new` 컬렉션 생성 금지
- **컴포넌트 캐싱 누락 금지**: 참조는 `Awake()`/`Start()`에서 캐싱

---

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

---

## Claude에게 지시사항

### 코드 생성 시

1. var 변수형 금지
2. 새 클래스 작성 전 인터페이스 설계 먼저 제안
3. MonoBehaviour 생성 시 생명주기 스텁(`Awake`, `OnEnable`, `OnDisable`, `OnDestroy`) 포함
4. GC 할당 가능성 있는 코드엔 `// ⚠️ GC 주의` 코멘트 추가
5. Unity 버전 종속 API 사용 시 버전 명시

### 리팩터링 제안 시

- `[SerializeField]` 필드명 임의 변경 금지 (Inspector 직렬화 데이터 손실)
- 동일 패턴이 3회 이상 반복되면 반드시 추상화 제안
- 타입 분기(`if type == ...`)가 보이면 다형성 리팩터링 제안

### 응답 언어

- 코드 내 주석: 한국어
- 설명 텍스트: 한국어
- 변수/클래스/메서드명: 영어
- 텍스트에 이모티콘 사용 금지

---

## 프로젝트 구조 (실제 현황)

```
Project-ER/                           ← 모노레포 루트
├── Client/                           ← Unity 프로젝트 루트 (Unity Hub에서 이 폴더를 열 것)
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Crafting/
│   │   │   │   └── CraftingSystem.cs
│   │   │   ├── Data/
│   │   │   │   ├── ItemData.cs
│   │   │   │   ├── ItemDatabase.cs
│   │   │   │   ├── ItemGrade.cs              ← 등급 열거형 (Common~Mythic)
│   │   │   │   ├── ItemGradeColorConfig.cs   ← 등급별 색상 SO
│   │   │   │   ├── ItemIngredient.cs
│   │   │   │   ├── ItemType.cs
│   │   │   │   ├── RecipeData.cs
│   │   │   │   ├── RecipeDatabase.cs
│   │   │   │   └── WeaponType.cs
│   │   │   ├── Editor/
│   │   │   │   ├── BserItemImporter.cs       ← BSER API JSON → SO 일괄 임포터
│   │   │   │   ├── BserSpriteLinker.cs       ← 스프라이트 자동 연결
│   │   │   │   └── InventoryTestUIBuilder.cs ← 1920×1080 3패널 테스트 UI 자동 생성
│   │   │   ├── Inventory/
│   │   │   │   ├── EquipmentSlotType.cs
│   │   │   │   ├── InventorySlot.cs
│   │   │   │   └── InventorySystem.cs
│   │   │   └── UI/
│   │   │       ├── CraftingSlotUI.cs
│   │   │       ├── CraftingUI.cs
│   │   │       ├── InventorySlotUI.cs
│   │   │       ├── InventoryTestPanel.cs     ← 도감 + 스탯창 + 필터 + 루트 정렬
│   │   │       ├── InventoryUI.cs
│   │   │       ├── TargetItemPanelUI.cs      ← 목표 루트 패널 (5슬롯)
│   │   │       ├── TargetItemSlotUI.cs       ← 목표 슬롯 (드롭 수신, 우클릭 제거)
│   │   │       └── TriangleIndicator.cs      ← 재료 표시용 삼각형 Graphic
│   │   ├── ScriptableObjects/
│   │   │   ├── ItemDatabase.asset
│   │   │   ├── ItemGradeColorConfig.asset    ← 등급 색상 설정
│   │   │   ├── RecipeDatabase.asset
│   │   │   ├── Items/BSER/                   ← ItemData SO 786개
│   │   │   └── Recipes/BSER/                 ← RecipeData SO 666개
│   │   └── Resources/
│   │       └── Image/Item/                   ← 아이템 아이콘 스프라이트
│   ├── Packages/
│   └── ProjectSettings/
└── Server/                           ← 서버 코드 (추후 추가)
```

---

## 구현 현황 (2026-06-01 기준)

### 완료

경로 기준: `Client/Assets/Scripts/` (스크립트), `Client/Assets/ScriptableObjects/` (에셋)

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

### 미구현 (다음 작업 대상)

| 시스템 | 우선순위 | 비고 |
|---|---|---|
| 이동 시스템 | 핵심 | NavMesh 클릭투무브, CharacterData SO |
| 캐릭터 시스템 | 핵심 | CharacterBase, PlayerController, CharacterState 머신 |
| 전투 시스템 | 핵심 | IDamageable, IAttackable, 기본 공격/피격/사망 |
| 아이템 줍기 | 핵심 | 월드 아이템 프리팹, 줍기 인터랙션 |
| 스탯 아이콘 | UI | 에셋 준비 후 StatDefs에 아이콘 슬롯 연결 |
| 무기 세부 필터 | UI | 캐릭터 선택 시 해당 캐릭터 무기군으로 표시 |
| 몬스터 AI | 선택 | 순찰 → 어그로 → 추격 |
| 미니맵 | 선택 | - |
| 금지구역 | 선택 | - |
