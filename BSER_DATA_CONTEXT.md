# BSER Open API - 아이템 데이터 구조 컨텍스트

## 개요
이터널리턴(Eternal Return) 오픈 API에서 수집한 아이템 데이터.
Unity 포트폴리오 프로젝트(이터널리턴 팬 재현)에서 사용할 데이터셋.

---

## 디렉터리 구조 (권장)

```
Assets/
└── Resources/
    └── Data/
        └── BSER/
            ├── ItemWeapon.json       # 무기 (414개)
            ├── ItemArmor.json        # 방어구 (251개)
            ├── ItemConsumable.json   # 소모품 (57개)
            ├── ItemMisc.json         # 기타 아이템 (64개)
            ├── ItemSpecial.json      # 사용형 아이템
            ├── ItemSpawn.json        # 지역별 아이템 스폰 위치
            ├── DropGroup.json        # 드롭 아이템 목록
            ├── MonsterDropGroup.json # 몬스터 드롭 그룹
            └── InfusionProduct.json  # 조합 관련
```

---

## 아이템 데이터 구조

### 핵심 공통 필드 (ItemWeapon, ItemArmor, ItemConsumable, ItemMisc 공통)

```json
{
  "code": 101201,           // 아이템 고유 코드 (int)
  "name": "군용나이프",      // 아이템 이름 (한글)
  "itemType": "Weapon",     // 아이템 타입: Weapon / Armor / Consumable / Misc / Special
  "itemGrade": "Uncommon",  // 등급: Common / Uncommon / Rare / Epic / Legend
  "manufacturableType": 0,  // 0 = 조합 아이템, 1 = 기본 재료
  "isCompletedItem": false, // 완성 아이템 여부

  // 조합 재료 (0이면 기본 재료 아이템)
  "makeMaterial1": 101104,  // 재료 아이템 코드 1
  "makeMaterial2": 108101,  // 재료 아이템 코드 2
}
```

### 조합 트리 규칙

```
makeMaterial1 == 0 && makeMaterial2 == 0  →  기본 재료 (스폰으로만 획득)
makeMaterial1 != 0                         →  조합 아이템 (재귀적으로 트리 추적 가능)
```

예시:
```
군용나이프 (101201)
├── 식칼 (101104)        makeMaterial1=0, makeMaterial2=0 → 기본재료
└── 천 (108101)          makeMaterial1=0, makeMaterial2=0 → 기본재료
```

### 무기 전용 주요 필드 (ItemWeapon)

```json
{
  "weaponType": "OneHandSword",  // 무기 종류
  "attackPower": 10,
  "attackPowerByLv": 0,
  "skillAmp": 0,
  "criticalStrikeChance": 0,
  "lifeSteal": 0,
  "moveSpeed": 0
}
```

### 방어구 전용 주요 필드 (ItemArmor)

```json
{
  "armorType": "Chest",   // 방어구 부위
  "defense": 20,
  "maxHp": 0,
  "hpRegen": 0
}
```

---

## 이미지 리소스 구조

GitHub에서 별도로 받은 리소스 zip:
```
resources/
├── images/
│   ├── item/
│   │   └── {code}.png          // 아이템 코드 = 이미지 파일명
│   └── character/
│       └── ...
└── data/
    └── item.json               // { key: "101201", path: "/resources/images/item/101201.png" }
                                // 아이템 코드 ↔ 이미지 경로 매핑 인덱스
```

아이템 코드로 이미지 로드:
```csharp
// Unity에서 Resources.Load 사용 예시
Sprite icon = Resources.Load<Sprite>($"Data/BSER/images/item/{itemCode}");
```

---

## Unity 파싱 권장 구조 (C#)

```csharp
[System.Serializable]
public class ItemWeaponData
{
    public int code;
    public string name;
    public string itemType;
    public string weaponType;
    public string itemGrade;
    public bool isCompletedItem;
    public int manufacturableType;
    public int makeMaterial1;
    public int makeMaterial2;
    public int attackPower;
    public int defense;
    public int maxHp;
    public float moveSpeed;
    public float criticalStrikeChance;
    public float lifeSteal;
    // 필요한 필드만 추가
}

[System.Serializable]
public class ItemWeaponList
{
    public ItemWeaponData[] items;
}
```

JSON 로드:
```csharp
TextAsset json = Resources.Load<TextAsset>("Data/BSER/ItemWeapon");
ItemWeaponList data = JsonUtility.FromJson<ItemWeaponList>("{\"items\":" + json.text + "}");
```

## 주의사항

- `Item` 테이블은 hash에 없음 → 각 타입별 테이블(`ItemWeapon`, `ItemArmor` 등) 사용
- `ItemMakeInfo`, `ItemDropInfo`, `ItemSpawnInfo` → hash에 없는 구 API 테이블명 (사용 불가)
- 조합식은 별도 테이블 없이 **각 아이템 필드에 내장** (`makeMaterial1`, `makeMaterial2`)
