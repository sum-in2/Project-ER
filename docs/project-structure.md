## 프로젝트 구조

모노레포 루트(`Project-ER/`) 아래 클라이언트와 서버가 분리돼 있다.
세부 파일 목록은 파일시스템 탐색으로 확인할 것 (이 문서는 디렉터리 레벨 + 비자명한 규칙만 기술).

```
Project-ER/
├── Client/                  ← Unity 프로젝트 루트 (Unity Hub에서 이 폴더를 열 것)
│   ├── Assets/
│   │   ├── Scenes/          ← 00_Connect → 01_Login → 02_Lobby → 03_Pick → 04_InGame
│   │   ├── Scripts/
│   │   │   ├── Character/   ← CharacterBase, PlayerController (이동/우클릭 분기)
│   │   │   ├── Combat/      ← IDamageable, IAttackable
│   │   │   ├── Skill/       ← ISkill 및 Active/Weapon/Tactical/Passive 인터페이스
│   │   │   ├── Interaction/ ← IInteractable
│   │   │   ├── Crafting/    ← CraftingSystem
│   │   │   ├── Inventory/   ← InventorySystem, InventorySlot, EquipmentSlotType
│   │   │   ├── Data/        ← ScriptableObject 정의 + 열거형 (ItemData, CharacterData, RecipeData 등)
│   │   │   ├── Network/     ← NetworkClient, MiniMsgPack, Serializer, Dispatcher
│   │   │   │   └── Protocol/ ← PacketType/Header + C2S*/S2C* 패킷 (서버 Core와 값 일치 유지)
│   │   │   ├── Scene/       ← 씬별 Controller (Connect/Login/Lobby/Pick)
│   │   │   ├── UI/          ← 인벤토리/크래프팅/도감/루트/루트박스 UI + 슬롯 추상화
│   │   │   │   └── Pick/    ← 캐릭터 선택 UI (그리드/필터/타이머/플레이어 슬롯)
│   │   │   └── Editor/      ← BSER 임포터, 스프라이트 링커, 씬/패널 빌더
│   │   ├── Prefabs/, ScriptableObjects/, Resources/
│   │   │   └── ScriptableObjects: Characters(89) / Items(786) / Recipes(666) / WeaponTypes(23)
│   │   │      Resources/Data/BSER: 원본 JSON, Resources/Image: 초상화·아이콘
│   ├── Packages/, ProjectSettings/
└── Server/                  ← C# .NET 9.0
    ├── ProjectER.Core/      ← netstandard2.1 (패킷 정의, 클라/서버 공유 가능)
    │   ├── Data/            ← CharacterCatalog (서버 검증용 유효 ID 목록)
    │   └── Packets/         ← PacketType/Header + C2S/ + S2C/
    ├── ProjectER.Server/    ← net9.0 실행 프로젝트
    │   ├── Network/         ← TcpGameServer, ClientSession, PacketDispatcher, PacketBuilder
    │   ├── Handlers/        ← 패킷 타입별 핸들러 (Connect/Login/Move/MatchRequest/Pick)
    │   ├── Database/        ← AccountDb, AccountRepository (계정 영속화)
    │   ├── Lobby/           ← LobbyRoom(18명), LobbyManager
    │   ├── Matchmaking/     ← Config/Queue/Manager
    │   └── Pick/            ← PickSession(30초 타이머), PickManager
    └── ProjectER.Tests/     ← net9.0, xUnit
```

### 비자명한 구조 규칙

- `Client/`가 Unity 프로젝트 루트다. Unity Hub로 열 때 저장소 루트가 아닌 이 폴더를 연다.
- 패킷은 3곳을 동기화해야 한다: 서버 `ProjectER.Core/Packets`, 클라 `Scripts/Network/Protocol`,
  그리고 `PacketType` 열거값. 새 패킷은 이 셋 + 서버 핸들러 + 클라 Serializer를 함께 작성.
- `ProjectER.Core`는 netstandard2.1로, 클라이언트와 코드 공유가 가능하도록 의도된 경계다.
- ScriptableObject 에셋(SO 1500여 개)은 `Scripts/Editor`의 BSER 임포터가 원본 JSON에서 자동 생성한다.
  수기로 만들지 말고 임포터를 통할 것.
- 씬은 코드(`Editor`의 빌더)로 생성한다. 빌더가 만족해야 하는 오브젝트 규칙은
  system-design.md "에디터 씬 빌더" 섹션 참조.
