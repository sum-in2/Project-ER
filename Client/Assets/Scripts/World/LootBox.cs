using System.Collections.Generic;
using ProjectER.Character;
using ProjectER.Data;
using ProjectER.Interaction;
using ProjectER.Inventory;
using UnityEngine;

namespace ProjectER.World
{
    /// <summary>
    /// 월드에 배치되는 박스 — 상호작용 시 보유 아이템을 인벤토리로 옮기고 사라진다.
    /// </summary>
    public class LootBox : MonoBehaviour, IInteractable
    {
        private readonly List<SpawnEntry> _contents = new();

        private void Awake()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }

        /// <summary>박스 보유 아이템 목록을 설정한다 (LootZone이 스폰 시 호출).</summary>
        public void Initialize(IReadOnlyList<SpawnEntry> contents)
        {
            _contents.Clear();
            _contents.AddRange(contents);
        }

        public void Interact(PlayerController interactor)
        {
            if (!interactor.TryGetComponent(out InventorySystem inventory)) return;

            foreach (SpawnEntry entry in _contents)
                inventory.TryAddItem(entry.Item, entry.DropCount);

            Destroy(gameObject);
        }
    }
}
