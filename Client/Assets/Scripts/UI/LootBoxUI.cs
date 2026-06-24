using System;
using System.Collections.Generic;
using ProjectER.Crafting;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectER.UI
{
    /// <summary>
    /// 루트박스 열기 UI — 화면 중앙 4x4(16슬롯) 그리드.
    ///
    /// Optimistic Update 흐름:
    ///   슬롯 클릭 → 즉시 UI 제거 + 인벤토리 추가 → 서버에 C2S_TakeLootItem 전송
    ///   서버 OK  → 아무것도 안 함 (이미 반영됨)
    ///   서버 거부 → Rollback(slotIndex) 호출 → UI 복원 + 인벤토리 취소
    /// </summary>
    public class LootBoxUI : MonoBehaviour, IMaterialSource
    {
        private const int SlotCount = 16;

        [SerializeField] private LootBoxSlotUI[] _slots; // Inspector에서 16개 연결

        // 루트 우선표기용 — 선택 루트(MatchSelectionData) + 레시피 트리(RecipeDatabase)
        [SerializeField] private MatchSelectionData _matchSelection;
        [SerializeField] private RecipeDatabase     _recipeDatabase;

        // 선택 루트의 필요 아이템 ID 집합 (목표 장비 + 하위 재료). 루트 확정 후 1회 계산.
        private HashSet<string> _routeNeeded;
        private bool            _routeNeededBuilt;

        // 현재 열려 있는 박스 UI (동시에 하나만 열림 — 거리 이탈 시 자동 닫힘).
        // 조합 패널이 "열린 박스 내용물"을 조합 재료로 포함하기 위해 참조한다.
        public static LootBoxUI ActiveBox { get; private set; }

        // 열린 박스가 바뀌거나(열림/닫힘) 내용물이 바뀔 때 발생
        public static event Action OnActiveBoxChanged;

        private InventorySystem _inventory;
        private Action          _onAllTaken;

        // 슬롯별 잔여 아이템 상태 (Optimistic 롤백 기준값)
        private readonly ItemData[] _items  = new ItemData[SlotCount];
        private readonly int[]      _counts = new int[SlotCount];

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Initialize(i, OnSlotClicked);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        private void OnEnable()  { }

        private void OnDisable()
        {
            // 닫히면(거리 이탈/ESC/전부 취득) 활성 박스 해제
            if (ActiveBox == this)
            {
                ActiveBox = null;
                OnActiveBoxChanged?.Invoke();
            }
        }

        private void OnDestroy() { }

        /// <summary>ESC 또는 캐릭터 이탈 시 외부에서 호출 — 내용물은 유지된다.</summary>
        public void Close() => gameObject.SetActive(false);

        // ── IMaterialSource (조합 재료 공급원) ──────────────────────────

        public int GetCount(string itemId)
        {
            int total = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_items[i] != null && _counts[i] > 0 && _items[i].Id == itemId)
                    total += _counts[i];
            }
            return total;
        }

        /// <summary>박스에서 재료 차감 (조합 시 인벤토리 부족분 보충). UI/상태 갱신 포함.</summary>
        public void Remove(string itemId, int amount)
        {
            for (int i = 0; i < SlotCount && amount > 0; i++)
            {
                if (_items[i] == null || _items[i].Id != itemId) continue;

                int take = Mathf.Min(_counts[i], amount);
                _counts[i] -= take;
                amount     -= take;
                if (_counts[i] <= 0) _items[i] = null;
            }

            CompactSlots();
            OnActiveBoxChanged?.Invoke(); // 조합 패널 갱신
            CheckAllTaken();              // 비었으면 박스 닫힘/제거
        }

        /// <summary>
        /// 박스 내용물을 받아 UI를 열고 표시한다.
        /// </summary>
        /// <param name="contents">박스 초기 내용물 (최대 16개 슬롯)</param>
        /// <param name="inventory">아이템을 이전할 인벤토리</param>
        /// <param name="onAllTaken">모든 아이템이 취득됐을 때 호출</param>
        public void Open(
            IReadOnlyList<SpawnEntry> contents,
            InventorySystem           inventory,
            Action                    onAllTaken)
        {
            _inventory  = inventory;
            _onAllTaken = onAllTaken;

            for (int i = 0; i < SlotCount; i++)
            {
                if (i < contents.Count && contents[i].Item != null && contents[i].DropCount > 0)
                {
                    _items[i]  = contents[i].Item;
                    _counts[i] = contents[i].DropCount;
                    _slots[i].Refresh(_items[i], _counts[i]);
                    MarkRouteSlot(i);
                }
                else
                {
                    _items[i]  = null;
                    _counts[i] = 0;
                    _slots[i].Clear();
                }
            }

            gameObject.SetActive(true);

            // 활성 박스 등록 → 조합 패널이 이 박스 내용물을 재료로 포함
            ActiveBox = this;
            OnActiveBoxChanged?.Invoke();
        }

        // ── Optimistic Update ──────────────────────────────────────────

        private void OnSlotClicked(int slotIndex)
        {
            ItemData item  = _items[slotIndex];
            int      count = _counts[slotIndex];
            if (item == null || count <= 0) return;

            // ── 1단계: 슬롯만 즉시 흐리게 (Pending) ─────────────────
            _slots[slotIndex].SetPending();

            // ── 2단계: 서버 요청 (네트워크 연동 시 아래 주석 해제) ────
            // NetworkClient.Instance.Send(
            //     PacketSerializer.Serialize(new C2S_TakeLootItem { BoxId = _boxId, SlotIndex = slotIndex }));
            //
            // 서버 OK  → ConfirmTake(slotIndex) 호출
            // 서버 거부 → Rollback(slotIndex) 호출

            // ── 로컬 전용 (서버 없는 그레이박스): 즉시 확정 처리 ─────
            ConfirmTake(slotIndex);
        }

        /// <summary>
        /// 서버가 취득을 승인했을 때 호출 — 슬롯을 완전히 비우고 인벤토리에 추가한다.
        /// </summary>
        public void ConfirmTake(int slotIndex)
        {
            ItemData item  = _items[slotIndex];
            int      count = _counts[slotIndex];
            if (item == null) return;

            // 가방이 가득 차 추가에 실패하면 취득하지 않는다 (박스에 그대로 둠)
            if (!_inventory.TryAddItem(item, count))
            {
                _slots[slotIndex].ClearPending();
                return;
            }

            _items[slotIndex]  = null;
            _counts[slotIndex] = 0;

            CompactSlots();

            // 내용물이 줄었으므로 조합 패널 갱신 (열린 박스 재료 반영)
            OnActiveBoxChanged?.Invoke();

            CheckAllTaken();
        }

        // 빈 슬롯을 제거하고 남은 아이템을 앞으로 당긴다.
        private void CompactSlots()
        {
            int write = 0;
            for (int read = 0; read < SlotCount; read++)
            {
                if (_items[read] == null) continue;
                _items[write]  = _items[read];
                _counts[write] = _counts[read];
                write++;
            }
            for (int i = write; i < SlotCount; i++)
            {
                _items[i]  = null;
                _counts[i] = 0;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (_items[i] != null)
                {
                    _slots[i].Refresh(_items[i], _counts[i]);
                    MarkRouteSlot(i);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
        }

        // 슬롯 아이템이 선택 루트의 필요 재료면 삼각형 표시
        private void MarkRouteSlot(int slotIndex)
        {
            HashSet<string> needed = GetRouteNeeded();
            ItemData item = _items[slotIndex];
            bool marked = needed != null && item != null && needed.Contains(item.Id);
            _slots[slotIndex].SetRouteMarked(marked);
        }

        // 선택 루트의 필요 아이템 집합 (1회 계산 후 캐시). 루트 미선택이면 null.
        private HashSet<string> GetRouteNeeded()
        {
            if (_routeNeededBuilt) return _routeNeeded;
            _routeNeededBuilt = true;

            SavedRoute route = _matchSelection != null ? _matchSelection.SelectedRoute : null;
            if (route != null && _recipeDatabase != null)
                _routeNeeded = RouteSorter.CollectAllNeededItemIds(route, _recipeDatabase);

            return _routeNeeded;
        }

        /// <summary>
        /// 서버가 슬롯 취득을 거부했을 때 호출 — Pending 상태를 되돌린다.
        /// </summary>
        public void Rollback(int slotIndex)
        {
            _slots[slotIndex].ClearPending();
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────

        private void CheckAllTaken()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_items[i] != null && _counts[i] > 0) return;
            }

            gameObject.SetActive(false);
            _onAllTaken?.Invoke();
        }
    }
}
