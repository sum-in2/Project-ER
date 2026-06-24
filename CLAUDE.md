# CLAUDE.md — project-er (이터널리턴 모작 포트폴리오)

## 프로젝트 개요

이터널리턴(Eternal Return)을 레퍼런스로 한 포트폴리오용 Unity 모작 프로젝트.
클라이언트 프로그래머 기술 어필을 목적으로 한다.

- **엔진**: Unity 6 (6000.0.4f1, URP)
- **클라이언트 언어**: C# (.NET Standard 2.1)
- **서버 언어**: C# (.NET 9.0)
- **플랫폼**: PC (Windows)
- **라이센스**: MIT

### 구현 범위 (스코프)

**핵심 구현 (반드시)**

- 클릭투무브 이동 (NavMesh 기반)
- 캐릭터 1~2종 (기본 공격 + 스킬 포함)
- 기본 전투 (공격, 피격, 사망)
- 아이템 줍기 + 인벤토리 시스템
- 크래프팅 시스템 (재료 조합 → 아이템 제작)
- 서버 연동 (TCP 소켓, 접속/로비/이동/전투 동기화)

**선택 구현 (여유 시 추가)**

- 몬스터 AI (순찰 → 어그로 → 추격)
- 미니맵
- 금지구역 시스템

**제외 (스코프 아웃)**

- 전체 맵 16구역 재현
- 캐릭터 40종 이상 구현

---

## 참고 문서 (분리된 상세 가이드)

@docs/coding-conventions.md
@docs/system-design.md
@docs/project-structure.md
@docs/implementation-status.md
@docs/combat-damage-formula.md

---

## Claude에게 지시사항

### 코드 생성 시

1. var 변수형 금지
2. 새 클래스 작성 전 인터페이스 설계 먼저 제안
3. MonoBehaviour 생성 시 생명주기 스텁(`Awake`, `OnEnable`, `OnDisable`, `OnDestroy`) 포함
4. GC 할당 가능성 있는 코드엔 `// ⚠️ GC 주의` 코멘트 추가
5. Unity 버전 종속 API 사용 시 버전 명시
6. 새 패킷 추가 시 서버(Core + Handler)와 클라이언트(Protocol + PacketSerializer) 양쪽 모두 작성

### 리팩터링 제안 시

- `[SerializeField]` 필드명 임의 변경 금지 (Inspector 직렬화 데이터 손실)
- 동일 패턴이 3회 이상 반복되면 반드시 추상화 제안
- 타입 분기(`if type == ...`)가 보이면 다형성 리팩터링 제안

### 응답 언어

- 코드 내 주석: 한국어
- 설명 텍스트: 한국어
- 변수/클래스/메서드명: 영어
- 텍스트에 이모티콘 사용 금지
