# Project-ER

이터널리턴(Eternal Return) 모작 포트폴리오 프로젝트

- **엔진**: Unity (URP)
- **언어**: C# (.NET Standard 2.1)
- **플랫폼**: PC (Windows)

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

## 구현 현황

| 시스템 | 상태 |
|---|---|
| 아이템 데이터 (BSER Open API 연동) | 완료 |
| 인벤토리 — 가방 10슬롯 + 장비 5슬롯 | 완료 |
| 크래프팅 — 재료 검증 / 차감 / 결과물 추가 | 완료 |
| 테스트 UI — 재료 획득 / 가방 / 장비 / 조합 | 완료 |
| 클릭투무브 이동 (NavMesh) | 미구현 |
| 캐릭터 / 기본 공격 / 스킬 | 미구현 |
| 전투 (공격, 피격, 사망) | 미구현 |
| 아이템 줍기 (월드 오브젝트) | 미구현 |

---

## 에디터 도구

| 메뉴 | 설명 |
|---|---|
| `Import BSER Items` | BSER JSON → ItemData / RecipeData SO 일괄 생성 |
| `Link BSER Sprites` | 팬킷 이미지 → ItemData.Icon 자동 연결 |
| `Link BSER Sprites (Force Relink)` | 기존 연결 무시하고 강제 재연결 |
| `Build LoadOut Panel Prefab` | 인벤토리 + 크래프팅 LoadOut 패널 프리팹 생성 |
| `Build Network Scenes` | 00_ConnectScene / 01_LoginScene / 02_LobbyScene 자동 생성 |
