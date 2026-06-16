using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 로드아웃 패널 Facade — ItemBrowserView, ItemFilterController, EquippedStatsPanel, RouteSorter를 조율한다.
    /// SerializeField 필드는 LoadOutPanelBuilder가 에디터에서 연결한다.
    /// </summary>
    public class LoadOutPanel : MonoBehaviour, IUIPanel
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private List<ItemData>  _acquisitionItems;
        [SerializeField] private Transform       _buttonContainer;
        [SerializeField] private Transform       _statContainer;
        [SerializeField] private RecipeDatabase  _recipeDatabase;

        // 우측 패널 — 빌더에서 연결
        [SerializeField] private Button            _acquireButton;
        [SerializeField] private Button            _addRouteButton;
        [SerializeField] private TargetItemPanelUI _targetItemPanel;

        // 닫기 버튼 — 빌더에서 연결
        [SerializeField] private Button _closeButton;

        // 중앙 패널 좌측 필터 컨테이너 — 빌더에서 연결
        [SerializeField] private Transform _filterContainer;

        // 중앙 패널 상단 필터 바 — 빌더에서 연결
        [SerializeField] private KoreanInputFieldAdapter _searchAdapter;
        [SerializeField] private Transform               _specialMaterialContainer;
        [SerializeField] private Transform               _statFilterContainer;

        [SerializeField] private ItemGradeColorConfig _gradeConfig;

        private Action   _onCloseRequested;
        private ItemData _selectedItem;

        private ItemBrowserView      _browserView;
        private ItemFilterController _filterController;
        private EquippedStatsPanel   _statsPanel;

        // ── IUIPanel ─────────────────────────────────────────────────

        public LobbyPanelType PanelType  => LobbyPanelType.LoadOut;
        // 786개 아이템 브라우저 — 닫을 때 Destroy하여 메모리 회수 (LobbyUIManager가 처리)
        public bool           CacheOnClose => false;

        public void OnOpen()  { }
        public void OnClose() { }

        public void SetCloseHandler(Action onCloseRequested)
        {
            _onCloseRequested = onCloseRequested;
        }

        // ── 생명주기 ─────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
            if (_targetItemPanel != null)
                _targetItemPanel.OnTargetChanged += HandleRouteChanged;
            if (_searchAdapter != null)
                _searchAdapter.OnTextChanged += HandleSearchChanged;
        }

        private void OnDisable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
            if (_targetItemPanel != null)
                _targetItemPanel.OnTargetChanged -= HandleRouteChanged;
            if (_searchAdapter != null)
                _searchAdapter.OnTextChanged -= HandleSearchChanged;
        }

        private void Start()
        {
            if (_inventorySystem == null)
            {
                Debug.LogError("[LoadOutPanel] InventorySystem이 연결되지 않았습니다.");
                return;
            }

            if (_acquireButton  != null) _acquireButton.onClick.AddListener(AcquireSelected);
            if (_addRouteButton != null) _addRouteButton.onClick.AddListener(AddToRoute);
            if (_closeButton    != null) _closeButton.onClick.AddListener(() => _onCloseRequested?.Invoke());

            if (_acquisitionItems == null || _acquisitionItems.Count == 0)
            {
                Debug.LogWarning("[LoadOutPanel] _acquisitionItems가 비어있습니다.");
                return;
            }

            _browserView = new ItemBrowserView(_acquisitionItems, _buttonContainer, _gradeConfig);
            _browserView.OnItemSelected += item => _selectedItem = item;
            _browserView.Build();

            _filterController = new ItemFilterController(
                _filterContainer, _specialMaterialContainer, _statFilterContainer,
                _recipeDatabase, _acquisitionItems);
            _filterController.OnFilterChanged += RefreshVisibility;
            _filterController.Build();

            _statsPanel = new EquippedStatsPanel(_statContainer);
            _statsPanel.Build();
            _statsPanel.Refresh(_inventorySystem);

            SortAndRefresh();
        }

        private void OnDestroy() { }

        // ── 내부 흐름 ─────────────────────────────────────────────────

        private void HandleRouteChanged()    => SortAndRefresh();
        private void HandleSearchChanged(string query) => _filterController?.SetSearchQuery(query);
        private void HandleEquipmentChanged(EquipmentSlotType _, ItemData __) => _statsPanel?.Refresh(_inventorySystem);

        private void SortAndRefresh()
        {
            // ⚠️ GC 주의: HashSet 할당 — 루트 변경 시에만 호출되므로 허용
            HashSet<string> neededIds = RouteSorter.CollectAllNeededItemIds(_targetItemPanel, _recipeDatabase);
            _browserView?.SortByRoute(neededIds);
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_browserView == null || _filterController == null) return;
            _browserView.ApplyVisibility(_filterController.Matches);
        }

        // ── 버튼 액션 ────────────────────────────────────────────────

        public void AcquireSelected()
        {
            if (_selectedItem == null)
            {
                Debug.LogWarning("[LoadOutPanel] 획득할 아이템을 선택하세요.");
                return;
            }
            if (_inventorySystem.TryAddItem(_selectedItem))
                Debug.Log($"[LoadOutPanel] 획득: {_selectedItem.DisplayName}");
            else
                Debug.LogWarning($"[LoadOutPanel] 가방이 꽉 참 — {_selectedItem.DisplayName}");
        }

        public void AddToRoute()
        {
            if (_selectedItem == null)
            {
                Debug.LogWarning("[LoadOutPanel] 루트에 추가할 아이템을 선택하세요.");
                return;
            }
            if (_targetItemPanel == null)
            {
                Debug.LogWarning("[LoadOutPanel] TargetItemPanelUI가 연결되지 않았습니다.");
                return;
            }
            if (_targetItemPanel.TrySetItem(_selectedItem))
                Debug.Log($"[LoadOutPanel] 루트 추가: {_selectedItem.DisplayName}");
            else
                Debug.LogWarning($"[LoadOutPanel] {_selectedItem.DisplayName}은 장비 아이템이 아닙니다.");
        }
    }
}
