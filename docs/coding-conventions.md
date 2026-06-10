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
패킷 (C→S)      : C2S 접두사     → C2SMovePacket, C2SConnectPacket
패킷 (S→C)      : S2C 접두사     → S2CMoveSyncPacket, S2CConnectedPacket
```

### 필수 컨벤션

- `public` 필드 금지 → `[SerializeField] private` 사용
- `GetComponent<T>()` 대신 `TryGetComponent<T>()` 사용
- 런타임에서 `Find()`, `FindObjectOfType()` 호출 금지 → 직접 참조 또는 DI
- 태그 비교는 `CompareTag()` 사용 (string 할당 방지)
- 이벤트 구독/해제는 `OnEnable` / `OnDisable` 쌍으로 (단, NetworkClient 이벤트는 `Start`에서 구독)
- 매직 넘버 금지 → `const` 또는 `[SerializeField]` 상수로 선언

### 하지 말 것

- **God Class 금지**: GameManager 하나에 모든 로직 몰아넣기 금지
- **static 남용 금지**: 전역 상태가 필요하면 ScriptableObject 이벤트 채널 사용
  - 예외: `NetworkClient.Instance` (DontDestroyOnLoad 씬 간 지속 목적)
- **Update() 내 GC 유발 코드 금지**: LINQ, string 연결, `new` 컬렉션 생성 금지
- **컴포넌트 캐싱 누락 금지**: 참조는 `Awake()`/`Start()`에서 캐싱
