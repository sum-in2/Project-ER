using System.Collections.Generic;
using ProjectER.Character;
using ProjectER.Data;
using ProjectER.Interaction;
using ProjectER.Inventory;
using ProjectER.UI;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectER.World
{
    /// <summary>
    /// 월드에 배치되는 박스 — 상호작용 시 4x4 루트박스 UI를 열어 아이템을 개별 취득한다.
    /// 모든 아이템이 취득되면 박스가 사라진다. 닫기만 하면 박스는 유지된다(재접근 가능).
    /// </summary>
    public class LootBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private LootBoxUI _lootBoxUIPrefab;

        private readonly List<SpawnEntry> _contents = new();
        private LootBoxUI                 _activeUI;

        private void Awake()
        {
            // 런타임 스폰된 박스는 NavMesh에 자동 등록되지 않으므로 NavMeshObstacle로 동적 장애물 처리
            if (!TryGetComponent(out NavMeshObstacle obstacle))
                obstacle = gameObject.AddComponent<NavMeshObstacle>();

            obstacle.carving = true; // carving=true: NavMesh를 실시간으로 재계산해 에이전트가 우회
            obstacle.shape   = NavMeshObstacleShape.Box;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
            if (_activeUI != null)
                Destroy(_activeUI.gameObject);
        }

        /// <summary>박스 보유 아이템 목록을 설정한다 (LootZone이 스폰 시 호출).</summary>
        public void Initialize(IReadOnlyList<SpawnEntry> contents)
        {
            _contents.Clear();
            _contents.AddRange(contents);
        }

        public void Interact(PlayerController interactor)
        {
            // 이미 UI가 열려 있으면 재열기 무시
            if (_activeUI != null && _activeUI.gameObject.activeSelf) return;
            if (!interactor.TryGetComponent(out InventorySystem inventory)) return;
            if (_lootBoxUIPrefab == null)
            {
                LogContentsDebug(inventory);
                return;
            }

            if (_activeUI == null)
                _activeUI = Object.Instantiate(_lootBoxUIPrefab);

            _activeUI.Open(_contents, inventory, OnAllItemsTaken, OnUIClosed);
        }

        // 모든 아이템이 취득되면 박스 제거
        private void OnAllItemsTaken() => Destroy(gameObject);

        // 닫기만 한 경우 — 박스는 유지, UI 인스턴스는 재사용
        private void OnUIClosed() { }

        // UI 프리팹 미연결 시 콘솔로 내용물 출력 (임시)
        private void LogContentsDebug(InventorySystem inventory)
        {
            // ⚠️ GC 주의: 디버그 전용 string 생성, 프로덕션에서는 제거
            System.Text.StringBuilder sb = new();
            sb.AppendLine($"[LootBox] '{name}' 상호작용 — UI 프리팹 미연결, 내용물 출력 (아이템 {_contents.Count}종)");
            for (int i = 0; i < _contents.Count; i++)
            {
                SpawnEntry entry = _contents[i];
                sb.AppendLine($"  [{i}] {entry.Item?.DisplayName ?? "null"} x{entry.DropCount}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
