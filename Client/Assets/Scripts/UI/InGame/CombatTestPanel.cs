using System.Collections.Generic;
using ProjectER.Character;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.InGame
{
    /// <summary>
    /// 인게임 테스트용 버튼 패널 — 데미지/회복/빈사/부활/사망, 아이템 추가/비우기, 스킬 스텁.
    /// 버튼은 Inspector에서 연결되며 Awake에서 onClick을 바인딩한다.
    /// </summary>
    public class CombatTestPanel : MonoBehaviour
    {
        [Header("대상")]
        [SerializeField] private CharacterBase _player;
        [SerializeField] private InventorySystem _inventory;
        [SerializeField] private ItemDatabase _itemDatabase;

        [Header("전투 테스트 버튼")]
        [SerializeField] private Button _damageSmallButton; // -25
        [SerializeField] private Button _damageLargeButton; // -50
        [SerializeField] private Button _healButton;        // +30
        [SerializeField] private Button _downButton;        // 즉시 빈사
        [SerializeField] private Button _reviveButton;      // 부활
        [SerializeField] private Button _killButton;        // 최종 사망

        [Header("아이템 테스트 버튼")]
        [SerializeField] private Button _addItemButton;
        [SerializeField] private Button _clearItemButton;

        [Header("스킬 테스트 버튼 (Q/W/E/R)")]
        [SerializeField] private Button _skillQButton;
        [SerializeField] private Button _skillWButton;
        [SerializeField] private Button _skillEButton;
        [SerializeField] private Button _skillRButton;

        private const float SmallDamage = 25f;
        private const float LargeDamage = 50f;
        private const float HealAmount  = 30f;

        // AddTestItem 호출 시마다 다음 아이템을 넣기 위한 순회 인덱스
        private int _testItemCursor;
        private List<ItemData> _testItems;

        private void Awake()
        {
            ResolveReferences();

            Bind(_damageSmallButton, () => { if (_player != null) _player.TakeDamage(SmallDamage); });
            Bind(_damageLargeButton, () => { if (_player != null) _player.TakeDamage(LargeDamage); });
            Bind(_healButton,        () => { if (_player != null) _player.Heal(HealAmount); });
            Bind(_downButton,        () => { if (_player != null) _player.TakeDamage(_player.MaxHp); }); // HP 전부 깎아 빈사 진입
            Bind(_reviveButton,      () => { if (_player != null) _player.Revive(); });
            Bind(_killButton,        () => { if (_player != null) _player.Die(); });

            Bind(_addItemButton,   AddTestItem);
            Bind(_clearItemButton, ClearInventory);

            Bind(_skillQButton, () => LogSkill("Q"));
            Bind(_skillWButton, () => LogSkill("W"));
            Bind(_skillEButton, () => LogSkill("E"));
            Bind(_skillRButton, () => LogSkill("R"));
        }

        private void OnDestroy()
        {
            // 람다로 등록한 리스너 일괄 제거 (각 버튼의 모든 onClick 해제)
            RemoveAll(_damageSmallButton);
            RemoveAll(_damageLargeButton);
            RemoveAll(_healButton);
            RemoveAll(_downButton);
            RemoveAll(_reviveButton);
            RemoveAll(_killButton);
            RemoveAll(_addItemButton);
            RemoveAll(_clearItemButton);
            RemoveAll(_skillQButton);
            RemoveAll(_skillWButton);
            RemoveAll(_skillEButton);
            RemoveAll(_skillRButton);
        }

        // 빌더가 참조를 연결하지 못한 경우를 대비한 런타임 폴백 (테스트 패널 한정).
        private void ResolveReferences()
        {
            if (_player == null)
                _player = FindFirstObjectByType<CharacterBase>();

            if (_inventory == null && _player != null)
                _player.TryGetComponent(out _inventory);

            if (_inventory == null)
                _inventory = FindFirstObjectByType<InventorySystem>();

            if (_player == null)
                Debug.LogWarning("[CombatTestPanel] Player(CharacterBase)를 찾지 못했습니다. 버튼이 동작하지 않습니다.");
            if (_itemDatabase == null)
                Debug.LogWarning("[CombatTestPanel] ItemDatabase가 연결되지 않았습니다. '아이템 추가'가 동작하지 않습니다.");
        }

        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private void RemoveAll(Button button)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        // ── 아이템 ───────────────────────────────────────────────────

        private void AddTestItem()
        {
            if (_inventory == null || _itemDatabase == null) return;

            EnsureTestItems();
            if (_testItems.Count == 0) return;

            ItemData item = _testItems[_testItemCursor % _testItems.Count];
            _testItemCursor++;
            _inventory.TryAddItem(item);
        }

        private void EnsureTestItems()
        {
            if (_testItems != null) return;

            // 아이콘이 있는 아이템만 테스트 후보로 캐싱 (한 번만 수행)
            _testItems = new List<ItemData>();
            foreach (ItemData item in _itemDatabase.Items)
            {
                if (item != null && item.Icon != null)
                    _testItems.Add(item);
            }
        }

        private void ClearInventory()
        {
            if (_inventory == null) return;

            IReadOnlyList<InventorySlot> slots = _inventory.BagSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty)
                    _inventory.TryRemoveItem(slot.Item.Id, slot.Amount);
            }
        }

        // ── 스킬 (미구현 스텁) ────────────────────────────────────────

        private void LogSkill(string key)
        {
            // TODO: 스킬 슬롯 구현 시 해당 키 스킬 Activate 호출로 교체.
            //       빈사 상태에서는 빈사 전용 스킬셋으로 분기 예정.
            Debug.Log($"[CombatTestPanel] 스킬 {key} (미구현) — 현재 상태: {_player.CurrentState}");
        }
    }
}
