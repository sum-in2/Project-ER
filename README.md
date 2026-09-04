# Project-ER

이터널리턴(Eternal Return) 모작 포트폴리오 프로젝트.
클릭투무브 이동, 인벤토리/크래프팅, TCP 소켓 기반 서버 연동을 직접 구현한다.

- **엔진**: Unity 6 (URP)
- **클라이언트 언어**: C# (.NET Standard 2.1)
- **서버 언어**: C# (.NET 9.0)
- **플랫폼**: PC (Windows)

---

## 핵심 기술 구현

### 1. 데미지 계산 — 다형 프로파일 + 순수 함수 파이프라인

평타와 스킬은 "적용 방어력 → 증폭 → 최종피해 → 모드 보정" 순서가 동일하고 항의 출처만 다르다.
`IDamageProfile`로 분리해 타입 분기 없이 확장 가능하게 하고, 계산기는 상태 없는 순수 함수로 유지했다.

```csharp
// CombatMath.cs — 방어력 → 피해 감소 배율 (평타/스킬 공통)
public static float DefenseMultiplier(float effectiveDefense)
{
    float denominator = Mathf.Max(1f, effectiveDefense + CombatConstants.DefenseConstant);
    return CombatConstants.DefenseConstant / denominator;
}
```

```csharp
// DamageCalculator.cs — 제네릭 프로파일을 받아 공통 파이프라인 적용, GC 없음
public float Calculate<TProfile>(in TProfile profile, in DamageModifiers modifiers)
    where TProfile : IDamageProfile
{
    float baseDamage = profile.ComputeBaseDamage();
    float amplified  = baseDamage * Mathf.Max(CombatConstants.MinAmp, modifiers.Amp) + modifiers.FixedBonus;
    return Mathf.Max(CombatConstants.MinDamage, amplified * modifiers.FinalAmp * modifiers.ModeAmp);
}
```

공식/연산 순서는 [docs/combat-damage-formula.md](docs/combat-damage-formula.md) 참고 — 고정 추가피해는 방어력 계산 후에 더해지므로 사실상 방어 무시 효과가 있다는 점을 문서화해뒀다.

### 2. BSER 데이터 임포터 — JSON 786종 → ScriptableObject 자동 생성

이터널리턴 공식 오픈 API(BSER) JSON을 파싱해 아이템 786개, 레시피 666개를 SO로 일괄 생성하는 에디터 툴.
`JsonUtility`가 최상위 배열(`[...]`)을 못 읽는 Unity 제약을 `{"items": ...}` 래핑으로 우회했고, 무기/방어구/소비 아이템처럼 필드 구성이 다른 4개 카테고리의 레시피 생성 로직은 제네릭 + 델리게이트로 공통화했다.

```csharp
// BserItemImporter.cs — 최상위 배열 JSON을 감싸서 JsonUtility로 파싱
BserItemWeaponList list = JsonUtility.FromJson<BserItemWeaponList>(
    "{\"items\":" + json.text + "}");
```

```csharp
// 무기/방어구/소비/재료 4종 카테고리에서 재료 코드만 뽑아 레시피 SO를 만드는 공통 로직
private static void BuildRecipesFromJson<TList, TItem>(
    string jsonPath, Dictionary<int, ItemData> allItems, string savePath,
    Func<TItem, int> getCode, Func<TItem, int> getMat1, Func<TItem, int> getMat2)
```

### 3. 서버 패킷 디스패처 — OCP 기반 라우팅

패킷 타입이 늘어나도 `PacketDispatcher` 자체는 수정하지 않고 `Register` 호출만 추가하면 되도록 설계했다.
미등록 타입은 예외 대신 로그로만 남겨 한 세션의 잘못된 패킷이 서버 전체에 영향을 주지 않게 했다.

```csharp
// PacketDispatcher.cs
public void Register(PacketType type, Action<ClientSession, byte[]> handler)
    => _handlers[type] = handler;

public void Dispatch(ClientSession session, PacketType type, byte[] body)
{
    if (_handlers.TryGetValue(type, out var handler)) handler.Invoke(session, body);
    else Console.WriteLine($"[Dispatcher] 미등록 패킷 타입: {type}");
}
```

### 4. 크래프팅 — 재료 소모 우선순위 처리

인벤토리 재료가 부족하면 열린 루트박스(`IMaterialSource`) 재료로 보충하되, 소모 우선순위는 항상 인벤토리 먼저다.
제작 후 가방 공간이 부족하면 이미 차감한 재료를 자동 환불한다.

```csharp
// CraftingSystem.cs — 인벤토리 우선 소모, 부족분만 extra(루트박스)에서 차감
int fromInv = Mathf.Min(_inventorySystem.GetItemCount(id), need);
if (fromInv > 0) { _inventorySystem.TryRemoveItem(id, fromInv); need -= fromInv; }
if (need > 0 && extra != null) extra.Remove(id, need);
```

---

## 아키텍처

```
Project-ER/
├── Client/   ← Unity 프로젝트 루트 (Character/Combat/Skill/Inventory/Crafting/Network/UI)
└── Server/   ← .NET 9.0 (Core: 공유 패킷 정의 / Server: TcpGameServer, Handlers, Lobby, Pick)
```

- 패킷은 서버 `ProjectER.Core/Packets`, 클라 `Scripts/Network/Protocol`, `PacketType` 열거값 3곳을 동기화한다.
- 서버는 클라이언트를 신뢰하지 않는다 — 이동/데미지/인벤토리 변경은 전부 서버가 자체 데이터로 재검증한다.
- 상세 설계는 [docs/system-design.md](docs/system-design.md), 구현 현황 전체는 [docs/implementation-status.md](docs/implementation-status.md) 참고.

---

## 구현 현황

| 시스템 | 상태 |
|---|---|
| 클릭투무브 이동 (NavMesh) + 상태머신(Idle/Move/Downed/Dead) | 완료 |
| 데미지 계산 (방어력/치명타/증폭) + 장비 스탯 합산 | 완료 |
| 인벤토리 — 가방 10슬롯 + 장비 5슬롯 | 완료 |
| 크래프팅 — 재료 검증 / 차감 / 환불 | 완료 |
| 도감 / 목표 루트 시스템 | 완료 |
| 서버 접속·로그인·로비·매치메이킹·픽 1단계 | 완료 |
| 루트박스 (필드 아이템 획득) | 완료 |
| 공격/스킬 상태(Attack/Skill) + 공격 모션 연동 | 미구현 |
| 이동/전투 서버 동기화 (S2C 브로드캐스트) | 미구현 |
| 몬스터 AI / 미니맵 / 금지구역 | 미구현 (선택 스코프) |

---

## 테스트 씬 실행 순서

**1.** `ProjectER > Import BSER Items`
- BSER JSON → ItemData / RecipeData ScriptableObject 일괄 생성
- 이미 있으면 덮어씀

**2.** `ProjectER > Link BSER Sprites` *(선택)*
- 팬킷 이미지를 ItemData.Icon에 자동 연결
- 없어도 텍스트만으로 UI 동작함

**3.** `ProjectER > Build LoadOut Panel Prefab`
- LoadOut 패널 프리팹 생성/갱신

**4.** `ProjectER > Build Network Scenes`
- 00_ConnectScene / 01_LoginScene / 02_LobbyScene 자동 생성 (LoadOut 패널 포함)

**5.** Play (00_ConnectScene부터 시작)

---

## 조작법

### 재료 획득 패널 (좌측 세 번째)

| 조작 | 동작 |
|---|---|
| 버튼 클릭 | 해당 재료 1개 가방에 추가 |

기본 재료 37종 목록, 스크롤 가능. 가방 꽉 차면 실패.

---

### 가방 패널 (좌측 첫 번째)

| 조작 | 동작 |
|---|---|
| 좌클릭 | 장비 아이템이면 해당 슬롯에 장착 |
| 우클릭 | 슬롯 아이템 전체 버리기 |

- 슬롯 10개, 스택 가능한 아이템은 수량 표시됨
- 장착 시 해당 슬롯에 이미 장비 있으면 가방으로 교체됨 (가방 공간 없으면 장착 실패)

---

### 장비 패널 (좌측 두 번째)

| 조작 | 동작 |
|---|---|
| 클릭 | 장비 해제 → 가방으로 이동 |

슬롯 종류: 무기 / 옷 / 머리 / 팔 / 신발. 가방 공간 없으면 해제 실패.

---

### 조합 패널 (우측)

| 조작 | 동작 |
|---|---|
| 제작 버튼 클릭 | 재료 차감 후 결과물 가방에 추가 |

- 재료가 전부 갖춰진 레시피만 목록에 표시됨
- 재료 변경 시 목록 즉시 갱신됨
- 스크롤 가능
- 가방 공간 없으면 제작 실패, 차감된 재료 자동 환불됨

---

## 에디터 도구

| 메뉴 | 설명 |
|---|---|
| `Import BSER Items` | BSER JSON → ItemData / RecipeData SO 일괄 생성 |
| `Link BSER Sprites` | 팬킷 이미지 → ItemData.Icon 자동 연결 |
| `Link BSER Sprites (Force Relink)` | 기존 연결 무시하고 강제 재연결 |
| `Build LoadOut Panel Prefab` | 인벤토리 + 크래프팅 LoadOut 패널 프리팹 생성 |
| `Build Network Scenes` | 00_ConnectScene / 01_LoginScene / 02_LobbyScene 자동 생성 |
