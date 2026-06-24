## 다음 작업 로드맵 (2026-06-20 작성)

구현 현황은 implementation-status.md, 설계 의도는 system-design.md 참조.
이 문서는 "미구현 항목을 어떤 순서로 작업할지(의존성 + 우선순위)"만 기록한다.
완료 시 implementation-status.md로 항목을 이관하고 여기서는 체크 표시한다.

### 작업 순서 원칙

1. 핵심(전투/스킬) 루프를 먼저 닫아 "혼자서 플레이 가능한 인게임"을 완성한다.
2. 단일 클라이언트에서 동작 검증 후 네트워크 동기화로 확장한다.
3. 선택/UI 항목은 핵심 루프 사이사이 여유 작업으로 배치한다.

---

### 1단계 — 인게임 전투 루프 (핵심, 최우선)

목표: 공격 → 피격 → 빈사 → 사망까지 단일 클라이언트로 완결.

- [x] **Attack 상태 추가** — `AttackState`(사거리·공격간격·대상 유지) 구현,
  PlayerController 우클릭 IDamageable 분기 + pending attack 접근/공격 연결. `Scripts/Character/State/AttackState.cs`
- [ ] **Skill 상태 추가** — 상태머신에 Skill 구체 상태 구현 (현재 enum에만 존재)
- [~] **전투 시스템** — 기본 공격/피격/사망 루프는 닫힘(공격력 고정 데미지 → TakeDamage → 빈사/사망).
  남은 작업:
  - [x] 데미지 계산기(IDamageCalculator) — 방어력/치명타/증폭 공식 분리 (ER 공식 기반).
    `Scripts/Combat/`: CombatConstants/CombatMath/IDamageProfile(Basic·Skill)/DamageModifiers/DamageCalculator,
    방어자 방어력은 ICombatStats(CharacterBase 구현). AttackState가 평타 프로파일로 계산.
    증폭/고정추가/모드/방관/치피는 미보유 스탯 → 중립(0/1)로 시작. 서버 측 동일 공식 재계산은 전투 동기화 때 TODO
  - [x] 런타임 전투 스탯 집계 — 기본 스탯(CharacterData) + 장착 장비(InventorySystem) 합산.
    `Scripts/Combat/CombatStats.cs`, `CombatStatsBuilder.cs`. PlayerController가 Start·OnEquipmentChanged에
    Build→적용, CharacterBase.ApplyVitalStats로 최대체력·방어력 반영. 치명타 확률은 0~1 비율(장비에서만 획득)
  - [ ] 사거리 이탈 시 자동 추격 (현재 Idle 복귀 후 재클릭)
  - [ ] 대상 사망 시 공격 중단 (현재 TakeDamage가 no-op 처리)
  - [ ] 빈사 스탯 변경 적용 (이동속도 감소 등)
  - [ ] 무기군 데이터 기반 공격 사거리 (현재 PlayerController 임시 _attackRange)
- [ ] **스킬 슬롯** — Q/W/E/R(액티브)·D(무기)·F(전술)·T(패시브) 인터페이스 연동
  - 빈사 시 전용 스킬셋 교체 포함
  - 의존: Skill 상태 선행

검증: 인게임 테스트 HUD(CombatTestPanel)로 데미지/회복/빈사/부활/사망 흐름 확인.

### 2단계 — 아이템 인게임 연동 (핵심)

목표: 줍기/드랍/재배치까지 인게임 인벤토리 상호작용 완결.

- [ ] **아이템 월드 드랍** — ItemDragHandler.OnEndDrag에서 슬롯 밖 드롭 시
  캐릭터 발 밑에 아이템 스폰 (LootBox 스폰 로직 재사용 검토)
- [ ] **가방 슬롯 간 드래그 이동** — InventorySlotUI에 IItemDropTarget 적용
  (가방 재배치, 가방↔장비 드래그 장착/해제)
- [ ] **조합칸 표시 순서 정렬** — CraftableItemBar Refresh의 _recipeBuffer 정렬 로직

### 3단계 — 네트워크 동기화 (네트워크)

목표: 단일 클라 검증 완료된 이동/전투를 서버 권위로 동기화.

- [ ] **GameRoom** — 틱 루프, 플레이어 위치 관리 (서버 측 선행 인프라)
- [ ] **이동 동기화** — MoveHandler GameRoom 연동, S2C_MoveSync 브로드캐스트
  - 서버 검증: 좌표/속도가 NavMesh·이동속도 범위 벗어나면 거부
- [ ] **전투 동기화** — AttackHandler, S2C_TakeDamage/Die 패킷
  - 데미지는 서버가 직접 계산 (클라 값 불신)

### 4단계 — 픽 화면 마무리 (핵심)

- [~] **픽 2단계** — 루트 선택(저장된 루트 선택만 구현, RouteSelectPanelUI). 선택 실험체+루트는
  MatchSelectionData(SO)로 인게임 캐리. 잔여: 루트 생성 UI/계정 DB 영속화(해시 외래키)/C2S_SelectRoute
  서버 전송/전술스킬/스킨, 2단계 타이머·S2C_GameStart류 인게임(04) 로딩 연동
- [ ] **루트 인게임 우선표기** — 선택 루트 재료를 루트박스·조합칸에서 우선 정렬/표기
  (RouteSorter 재사용, 기존 "조합칸 표시 순서 정렬" 통합)
- [ ] **멀티플레이어 연동** — 매칭된 다른 플레이어를 플레이어 슬롯 2/3에 표시
  - 의존: GameRoom

### 여유 작업 (선택, 단계 사이 배치)

- [ ] 필드 아이템 줍기 (Common 구역) — ItemSpawn.json areaSpawnGroup -1 기반 산개
- [ ] 스탯 아이콘 — 에셋 준비 후 StatDefs 연결
- [ ] 무기 세부 필터 — 캐릭터 선택 시 해당 무기군 표시
- [ ] 몬스터 AI (순찰→어그로→추격) / 미니맵 / 금지구역
