using System;
using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 로드아웃 패널 — 아이템 브라우저(선택/드래그) + 획득/루트추가 버튼 + 장착 스탯 합계
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

        private Action _onCloseRequested;

        // 중앙 패널 좌측 필터 컨테이너 — 빌더에서 연결
        [SerializeField] private Transform _filterContainer;

        // 중앙 패널 상단 필터 바 — 빌더에서 연결
        [SerializeField] private KoreanInputFieldAdapter _searchAdapter;
        [SerializeField] private Transform               _specialMaterialContainer;
        [SerializeField] private Transform               _statFilterContainer;

        private Text[]    _statValueTexts;
        private ItemData  _selectedItem;
        private Image     _selectedButtonBg;   // 현재 선택된 브라우저 버튼의 배경 Image
        private ItemType? _currentFilter;       // null = 전체
        private Image[]   _filterButtonImages;  // 필터 버튼 배경 (선택 강조용)
        private string    _currentSearchQuery = string.Empty; // 소문자 변환 후 저장 (매칭용)

        // 버튼 순서 + 삼각형 인디케이터 관리 — 루트 기반 정렬/표시에 사용
        private readonly List<(ItemData Item, Transform Button, TriangleIndicator Indicator)> _browserButtons = new();

        // 선택 강조색 — 등급색보다 밝게
        private static readonly Color SelectedHighlight = new Color(0.85f, 0.85f, 0.50f, 1f);

        private static readonly Color[] GradeColors =
        {
            new Color(0.30f, 0.30f, 0.30f), // Common   — 일반
            new Color(0.18f, 0.38f, 0.18f), // Uncommon — 고급
            new Color(0.15f, 0.28f, 0.50f), // Rare     — 희귀
            new Color(0.38f, 0.18f, 0.50f), // Epic     — 영웅
            new Color(0.55f, 0.38f, 0.08f), // Legend   — 전설
            new Color(0.55f, 0.15f, 0.15f), // Mythic   — 초월
        };

        // 스탯 정의: (라벨, 값 추출 함수, 퍼센트 여부)
        private static readonly (string Label, Func<ItemData, float> Getter, bool IsPercent)[] StatDefs =
        {
            ("공격력",   item => item.AttackPower,          false),
            ("최대체력", item => item.MaxHpBonus,           false),
            ("방어력",   item => item.Defense,              false),
            ("이동속도", item => item.MoveSpeedBonus,       false),
            ("공격속도", item => item.AttackSpeedBonus,     true),
            ("스킬증폭", item => item.SkillAmp,             false),
            ("쿨감",     item => item.CooldownReduction,    true),
            ("적응형",   item => item.AdaptiveForce,        false),
            ("치명타",   item => item.CriticalStrikeChance, true),
            ("생명흡수", item => item.LifeSteal,            true),
            ("체력재생", item => item.HpRegenRatio,         true),
            ("방어관통", item => item.PenetrationDefense,   false),
        };

        // ── IUIPanel ─────────────────────────────────────────────────

        public LobbyPanelType PanelType => LobbyPanelType.LoadOut;

        // 786개 아이템 브라우저 — 닫을 때 Destroy하여 메모리 회수 (LobbyUIManager가 처리)
        public bool CacheOnClose => false;

        // CacheOnClose가 false이므로 매번 새로 Instantiate됨 → Start()에서 이미 초기화 처리됨
        public void OnOpen()
        {
        }

        public void OnClose()
        {
        }

        public void SetCloseHandler(Action onCloseRequested)
        {
            _onCloseRequested = onCloseRequested;
        }

        private void OnEnable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged += HandleEquipmentChanged;
            if (_targetItemPanel != null)
                _targetItemPanel.OnTargetChanged += HandleRouteChanged;
            if (_searchAdapter != null)
                _searchAdapter.OnTextChanged += OnSearchChanged;
        }

        private void OnDisable()
        {
            if (_inventorySystem != null)
                _inventorySystem.OnEquipmentChanged -= HandleEquipmentChanged;
            if (_targetItemPanel != null)
                _targetItemPanel.OnTargetChanged -= HandleRouteChanged;
            if (_searchAdapter != null)
                _searchAdapter.OnTextChanged -= OnSearchChanged;
        }

        private void Start()
        {
            if (_inventorySystem == null)
            {
                Debug.LogError("[LoadOutPanel] InventorySystem이 연결되지 않았습니다.");
                return;
            }

            // 우측 패널 버튼 바인딩
            if (_acquireButton != null)
                _acquireButton.onClick.AddListener(AcquireSelected);
            if (_addRouteButton != null)
                _addRouteButton.onClick.AddListener(AddToRoute);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => _onCloseRequested?.Invoke());

            // 검색 구독은 OnEnable에서 처리 (_searchAdapter.OnTextChanged)

            if (_acquisitionItems != null && _acquisitionItems.Count > 0)
                BuildButtons();
            else
                Debug.LogWarning("[LoadOutPanel] _acquisitionItems가 비어있습니다.");

            BuildFilterButtons();
            BuildSpecialMaterialButtons();
            BuildStatFilterButtons();
            BuildStatPanel();
            RefreshStats();
            SortButtonsByRoute();
        }


        // ── 브라우저 버튼 ─────────────────────────────────────────────

        private void BuildButtons()
        {
            Transform parent = _buttonContainer != null ? _buttonContainer : transform;
            int created = 0;
            foreach (ItemData item in _acquisitionItems)
            {
                if (item == null) continue;
                CreateButton(item, parent);
                created++;
            }
            Debug.Log($"[LoadOutPanel] 버튼 {created}개 생성 완료");
        }

        private void CreateButton(ItemData item, Transform parent)
        {
            GameObject go = new($"Btn_{item.Id}");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = GradeColors[(int)item.ItemGrade];

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.pressedColor     = new Color(0.20f, 0.20f, 0.20f);
            cb.normalColor      = Color.white; // tint 없이 bg.color 그대로 사용
            btn.colors          = cb;

            // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
            ItemData captured   = item;
            Image    bgCaptured = bg;
            btn.onClick.AddListener(() => SelectItem(captured, bgCaptured));

            // 삼각형 인디케이터 — 목표 루트의 필요 재료일 때만 활성화 (초기엔 비활성)
            GameObject    indGo  = new("RouteIndicator");
            indGo.transform.SetParent(go.transform, false);
            TriangleIndicator tri  = indGo.AddComponent<TriangleIndicator>();
            tri.color              = Color.yellow;
            tri.raycastTarget      = false;
            tri.enabled            = false; // 루트 변경 시 SortButtonsByRoute에서 갱신
            RectTransform indRt    = indGo.GetComponent<RectTransform>();
            indRt.anchorMin        = new Vector2(0f, 1f);
            indRt.anchorMax        = new Vector2(0f, 1f);
            indRt.pivot            = new Vector2(0f, 1f);
            indRt.sizeDelta        = new Vector2(16f, 16f);
            indRt.anchoredPosition = Vector2.zero;

            GameObject iconGo   = new("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon          = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget  = false;
            if (item.Icon != null)
                icon.sprite = item.Icon;
            else
                icon.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            // 실제 크기는 GridLayoutGroup.cellSize(LoadOutPanelBuilder.GridCellWidth/Height)가 결정
            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.preferredWidth  = 67.5f;

            // 드래그로 목표 루트 슬롯에 배치 — IItemSlot 구현 + 드래그 핸들러
            ItemBrowserSlotUI browserSlot = go.AddComponent<ItemBrowserSlotUI>();
            browserSlot.SetItem(item);
            go.AddComponent<ItemDragHandler>();

            _browserButtons.Add((item, go.transform, tri));
        }

        // ── 선택 ─────────────────────────────────────────────────────

        private void SelectItem(ItemData item, Image buttonBg)
        {
            // 이전 선택 버튼 색 복원
            if (_selectedButtonBg != null && _selectedItem != null)
                _selectedButtonBg.color = GradeColors[(int)_selectedItem.ItemGrade];

            _selectedItem     = item;
            _selectedButtonBg = buttonBg;
            buttonBg.color    = SelectedHighlight;

            Debug.Log($"[LoadOutPanel] 선택: {item.DisplayName} ({item.ItemGrade})");
        }

        // ── 외부 버튼 액션 ───────────────────────────────────────────

        /// <summary>
        /// 획득 버튼 — 선택된 아이템을 인벤토리에 추가
        /// </summary>
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

        /// <summary>
        /// 루트 추가 버튼 — 선택된 장비 아이템을 목표 슬롯에 설정
        /// </summary>
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

        // ── 루트 기반 정렬 ───────────────────────────────────────────

        private void HandleRouteChanged()
        {
            SortButtonsByRoute();
        }

        /// <summary>
        /// 루트에 설정된 목표 아이템의 필요 재료를 앞으로, 나머지는 뒤로 정렬.
        /// 각 그룹 내 순서(타입→등급)는 유지된다.
        /// </summary>
        private void SortButtonsByRoute()
        {
            if (_browserButtons.Count == 0) return;

            // ⚠️ GC 주의: HashSet 할당 — 루트 변경 시에만 호출되므로 허용
            HashSet<string> neededIds = CollectAllNeededItemIds();

            int index = 0;
            // 1패스: 필요한 아이템을 앞으로 + 삼각형 활성화
            foreach ((ItemData item, Transform btn, TriangleIndicator indicator) in _browserButtons)
            {
                bool needed = neededIds.Contains(item.Id);
                indicator.enabled = needed;
                if (needed) btn.SetSiblingIndex(index++);
            }

            // 2패스: 나머지는 뒤로
            foreach ((ItemData item, Transform btn, TriangleIndicator indicator) in _browserButtons)
                if (!neededIds.Contains(item.Id))
                    btn.SetSiblingIndex(index++);

            ApplyFilter();
        }

        /// <summary>
        /// 루트의 목표 아이템 전체에 대해 필요한 재료 ID를 재귀적으로 수집.
        /// 루트가 없거나 RecipeDatabase가 없으면 빈 셋 반환.
        /// </summary>
        private HashSet<string> CollectAllNeededItemIds()
        {
            // ⚠️ GC 주의: HashSet 할당 — 루트 변경 시에만 호출
            HashSet<string> result = new HashSet<string>();

            if (_targetItemPanel == null || _recipeDatabase == null)
                return result;

            foreach (KeyValuePair<EquipmentSlotType, ItemData> pair in _targetItemPanel.GetTargetItems())
                CollectIngredients(pair.Value.Id, result);

            return result;
        }

        /// <summary>
        /// 대상 아이템의 레시피를 재귀 탐색하여 필요한 모든 재료 ID를 result에 추가.
        /// 이미 추가된 ID는 재탐색하지 않아 순환 참조를 방지한다.
        /// </summary>
        private void CollectIngredients(string itemId, HashSet<string> result)
        {
            RecipeData recipe = _recipeDatabase.GetByResultId(itemId);
            if (recipe == null) return;

            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (result.Add(ingredient.Item.Id)) // Add가 true면 새로 추가된 항목 → 재귀
                    CollectIngredients(ingredient.Item.Id, result);
            }
        }

        // ── 좌측 필터 ────────────────────────────────────────────────

        private static readonly Color FilterNormalColor   = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color FilterSelectedColor = new Color(0.25f, 0.50f, 0.30f); // 타입 필터 — 초록

        // 스탯 필터 색상 (타입 필터와 구분: 파랑 계열)
        private static readonly Color StatFilterSelectedColor = new Color(0.15f, 0.38f, 0.58f);

        // 스탯 필터 상태 — 선택된 StatFilterDefs 인덱스 집합 (OR 적용)
        private readonly HashSet<int> _activeStatFilters     = new HashSet<int>();
        private          Image[]      _statFilterButtonImages = null;

        // (버튼 레이블, 필터 타입 — null이면 전체)
        private static readonly (string Label, ItemType? Filter)[] FilterDefs =
        {
            ("전", null),
            ("무", ItemType.Weapon),
            ("옷", ItemType.Chest),
            ("머", ItemType.Helmet),
            ("팔", ItemType.Arms),
            ("신", ItemType.Shoes),
            ("음", ItemType.Food),
            ("소", ItemType.Consumable),
            ("재", ItemType.Material),
        };

        private void BuildFilterButtons()
        {
            if (_filterContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            VerticalLayoutGroup vlg    = _filterContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 4f;
            vlg.padding                = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true; // false면 LayoutElement.preferredHeight가 무시되어 버튼 높이가 적용되지 않음

            _filterButtonImages = new Image[FilterDefs.Length];

            for (int i = 0; i < FilterDefs.Length; i++)
            {
                (string label, ItemType? filter) = FilterDefs[i];

                GameObject go = new($"FilterBtn_{label}");
                go.transform.SetParent(_filterContainer, false);

                Image bg  = go.AddComponent<Image>();
                bg.color  = i == 0 ? FilterSelectedColor : FilterNormalColor;
                _filterButtonImages[i] = bg;

                Button btn          = go.AddComponent<Button>();
                ColorBlock cb       = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
                cb.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
                btn.colors          = cb;

                // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
                int       indexCapture  = i;
                ItemType? filterCapture = filter;
                btn.onClick.AddListener(() => OnFilterClicked(indexCapture, filterCapture));

                // Text는 자식 GO로 분리 — Image·Button과 같은 GO에 두면 AddComponent<Text>가 실패하는 경우 있음
                GameObject textGo      = new("Label");
                textGo.transform.SetParent(go.transform, false);
                Text text              = textGo.AddComponent<Text>();
                text.text              = label;
                text.font              = builtinFont;
                text.fontSize          = 11;
                text.fontStyle         = FontStyle.Bold;
                text.color             = Color.white;
                text.alignment         = TextAnchor.MiddleCenter;
                text.raycastTarget     = false;
                RectTransform textRt   = textGo.GetComponent<RectTransform>();
                textRt.anchorMin       = Vector2.zero;
                textRt.anchorMax       = Vector2.one;
                textRt.offsetMin       = Vector2.zero;
                textRt.offsetMax       = Vector2.zero;

                LayoutElement le   = go.AddComponent<LayoutElement>();
                le.preferredHeight = 22f;
            }
        }

        // ── 특수 재료 필터 ──────────────────────────────────────────

        // 전설재료 5종: (버튼 레이블, BSER 아이템 코드 ID)
        private static readonly (string Label, string MaterialId)[] SpecialMaterialDefs =
        {
            ("생", "401208"), // 생명의나무
            ("운", "401209"), // 운석
            ("미", "401304"), // 미스릴
            ("포", "401403"), // 포스코어
            ("VF", "401401"), // VF혈액샘플
        };

        private static readonly Color SpecialMaterialNormalColor   = new Color(0.28f, 0.18f, 0.05f);
        private static readonly Color SpecialMaterialSelectedColor = new Color(0.62f, 0.42f, 0.08f);

        private string          _currentSpecialMaterialFilter = null; // null = 미적용
        private HashSet<string> _specialMaterialFilterCache   = null; // 재귀 탐색 결과 캐시
        private Image[]         _specialMaterialButtonImages  = null;

        private void BuildSpecialMaterialButtons()
        {
            if (_specialMaterialContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            HorizontalLayoutGroup hlg  = _specialMaterialContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 3f;
            hlg.padding                = new RectOffset(4, 4, 4, 4);
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true; // false면 LayoutElement.preferredWidth가 무시되어 버튼 너비가 적용되지 않음
            hlg.childControlHeight     = true;

            _specialMaterialButtonImages = new Image[SpecialMaterialDefs.Length];

            for (int i = 0; i < SpecialMaterialDefs.Length; i++)
            {
                (string label, string matId) = SpecialMaterialDefs[i];

                GameObject go = new($"SpecialMatBtn_{label}");
                go.transform.SetParent(_specialMaterialContainer, false);

                Image bg  = go.AddComponent<Image>();
                bg.color  = SpecialMaterialNormalColor;
                _specialMaterialButtonImages[i] = bg;

                Button btn          = go.AddComponent<Button>();
                ColorBlock cb       = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
                cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
                btn.colors          = cb;

                // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
                int    indexCapture = i;
                string matCapture   = matId;
                btn.onClick.AddListener(() => OnSpecialMaterialFilterClicked(indexCapture, matCapture));

                GameObject textGo      = new("Label");
                textGo.transform.SetParent(go.transform, false);
                Text text              = textGo.AddComponent<Text>();
                text.text              = label;
                text.font              = builtinFont;
                text.fontSize          = 10;
                text.fontStyle         = FontStyle.Bold;
                text.color             = Color.white;
                text.alignment         = TextAnchor.MiddleCenter;
                text.raycastTarget     = false;
                RectTransform textRt   = textGo.GetComponent<RectTransform>();
                textRt.anchorMin       = Vector2.zero;
                textRt.anchorMax       = Vector2.one;
                textRt.offsetMin       = Vector2.zero;
                textRt.offsetMax       = Vector2.zero;

                LayoutElement le   = go.AddComponent<LayoutElement>();
                le.preferredWidth  = label.Length > 1 ? 26f : 20f; // "VF"는 2글자
                le.preferredHeight = 20f;
            }
        }

        private void OnSpecialMaterialFilterClicked(int index, string materialId)
        {
            // 이미 선택된 버튼 재클릭 시 해제 (토글)
            bool deselect       = _currentSpecialMaterialFilter == materialId;
            string newMaterialId = deselect ? null : materialId;
            int?   newIndex      = deselect ? (int?)null : index;

            // 버튼 하이라이트 갱신
            if (_specialMaterialButtonImages != null)
            {
                for (int i = 0; i < _specialMaterialButtonImages.Length; i++)
                    _specialMaterialButtonImages[i].color = (newIndex.HasValue && i == newIndex.Value)
                        ? SpecialMaterialSelectedColor
                        : SpecialMaterialNormalColor;
            }

            _currentSpecialMaterialFilter = newMaterialId;

            // ⚠️ GC 주의: HashSet 두 개 할당 — 필터 변경 시에만 호출되므로 허용
            if (newMaterialId == null)
            {
                _specialMaterialFilterCache = null;
            }
            else
            {
                _specialMaterialFilterCache = new HashSet<string>();
                foreach ((ItemData item, Transform _, TriangleIndicator _) in _browserButtons)
                {
                    HashSet<string> visited = new HashSet<string>();
                    if (ItemNeedsMaterial(item.Id, newMaterialId, visited))
                        _specialMaterialFilterCache.Add(item.Id);
                }
            }

            ApplyFilter();
        }

        /// <summary>
        /// itemId의 레시피 트리를 재귀 탐색해 targetMaterialId가 포함되어 있으면 true 반환.
        /// visited로 순환 참조를 방지한다.
        /// </summary>
        private bool ItemNeedsMaterial(string itemId, string targetMaterialId, HashSet<string> visited)
        {
            if (itemId == targetMaterialId) return true;
            if (!visited.Add(itemId)) return false;
            if (_recipeDatabase == null) return false;

            RecipeData recipe = _recipeDatabase.GetByResultId(itemId);
            if (recipe == null) return false;

            foreach (ItemIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (ItemNeedsMaterial(ingredient.Item.Id, targetMaterialId, visited))
                    return true;
            }
            return false;
        }

        // 스탯 필터 약어 정의: (버튼 레이블, 스탯 추출 함수)
        // 공(공격력), 최(최대체력), 방(방어력), 이(이동속도), 속(공격속도),
        // 증(스킬증폭), 쿨(쿨감), 적(적응형), 치(치명타), 흡(생명흡수), 재(체력재생), 관(방어관통)
        private static readonly (string Label, Func<ItemData, float> Getter)[] StatFilterDefs =
        {
            ("공", item => item.AttackPower),
            ("최", item => item.MaxHpBonus),
            ("방", item => item.Defense),
            ("이", item => item.MoveSpeedBonus),
            ("속", item => item.AttackSpeedBonus),
            ("증", item => item.SkillAmp),
            ("쿨", item => item.CooldownReduction),
            ("적", item => item.AdaptiveForce),
            ("치", item => item.CriticalStrikeChance),
            ("흡", item => item.LifeSteal),
            ("재", item => item.HpRegenRatio),
            ("관", item => item.PenetrationDefense),
        };

        private void BuildStatFilterButtons()
        {
            if (_statFilterContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            HorizontalLayoutGroup hlg   = _statFilterContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                 = 3f;
            hlg.padding                 = new RectOffset(4, 4, 4, 4);
            hlg.childForceExpandWidth   = false;
            hlg.childForceExpandHeight  = true;
            hlg.childControlWidth       = true; // false면 LayoutElement.preferredWidth가 무시되어 버튼 너비가 적용되지 않음
            hlg.childControlHeight      = true;

            _statFilterButtonImages = new Image[StatFilterDefs.Length];

            for (int i = 0; i < StatFilterDefs.Length; i++)
            {
                string label = StatFilterDefs[i].Label;

                GameObject go = new($"StatFilterBtn_{label}");
                go.transform.SetParent(_statFilterContainer, false);

                Image bg  = go.AddComponent<Image>();
                bg.color  = FilterNormalColor;
                _statFilterButtonImages[i] = bg;

                Button btn          = go.AddComponent<Button>();
                ColorBlock cb       = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
                cb.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
                btn.colors          = cb;

                // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
                int indexCapture = i;
                btn.onClick.AddListener(() => OnStatFilterClicked(indexCapture));

                GameObject textGo      = new("Label");
                textGo.transform.SetParent(go.transform, false);
                Text text              = textGo.AddComponent<Text>();
                text.text              = label;
                text.font              = builtinFont;
                text.fontSize          = 10;
                text.fontStyle         = FontStyle.Bold;
                text.color             = Color.white;
                text.alignment         = TextAnchor.MiddleCenter;
                text.raycastTarget     = false;
                RectTransform textRt   = textGo.GetComponent<RectTransform>();
                textRt.anchorMin       = Vector2.zero;
                textRt.anchorMax       = Vector2.one;
                textRt.offsetMin       = Vector2.zero;
                textRt.offsetMax       = Vector2.zero;

                LayoutElement le   = go.AddComponent<LayoutElement>();
                le.preferredWidth  = label.Length > 1 ? 26f : 20f; // "쿨" 등 2글자 여유
                le.preferredHeight = 20f;
            }
        }

        private void OnStatFilterClicked(int index)
        {
            // 토글: 이미 선택된 경우 해제, 아니면 추가 (다중 선택 → OR 적용)
            if (_activeStatFilters.Contains(index))
                _activeStatFilters.Remove(index);
            else
                _activeStatFilters.Add(index);

            // 버튼 하이라이트 갱신
            if (_statFilterButtonImages != null)
            {
                for (int i = 0; i < _statFilterButtonImages.Length; i++)
                    _statFilterButtonImages[i].color = _activeStatFilters.Contains(i)
                        ? StatFilterSelectedColor
                        : FilterNormalColor;
            }

            ApplyFilter();
        }

        /// <summary>
        /// 선택된 스탯 필터 중 하나라도 0보다 큰 값을 가지면 true (OR 조건)
        /// </summary>
        private bool HasAnySelectedStat(ItemData item)
        {
            foreach (int index in _activeStatFilters)
                if (StatFilterDefs[index].Getter(item) > 0f)
                    return true;
            return false;
        }

        private void OnFilterClicked(int index, ItemType? filter)
        {
            if (_filterButtonImages == null) return;

            // 선택 강조 갱신
            for (int i = 0; i < _filterButtonImages.Length; i++)
                _filterButtonImages[i].color = i == index ? FilterSelectedColor : FilterNormalColor;

            _currentFilter = filter;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            foreach ((ItemData item, Transform btn, TriangleIndicator _) in _browserButtons)
            {
                // 타입 필터: 단일 선택, AND
                bool typeMatch    = _currentFilter == null || item.ItemType == _currentFilter;
                // 특수재료 필터: 단일 선택, AND
                bool specialMatch = _specialMaterialFilterCache == null
                                    || _specialMaterialFilterCache.Contains(item.Id);
                // 스탯 필터: 다중 선택 OR, 나머지와 AND
                bool statMatch    = _activeStatFilters.Count == 0 || HasAnySelectedStat(item);
                // 검색 필터: AND
                bool searchMatch  = KoreanSearchUtil.IsMatch(item.DisplayName, _currentSearchQuery);

                btn.gameObject.SetActive(typeMatch && specialMatch && statMatch && searchMatch);
            }
        }

        // ── 검색 ─────────────────────────────────────────────────────

        // KoreanInputFieldAdapter.OnTextChanged 핸들러 — 중복 호출은 어댑터가 차단함
        private void OnSearchChanged(string query)
        {
            // ⚠️ GC 주의: ToLowerInvariant 할당 — 검색어 변화 시에만 호출됨
            _currentSearchQuery = string.IsNullOrEmpty(query)
                ? string.Empty
                : query.ToLowerInvariant();
            ApplyFilter();
        }

        // ── 스탯 패널 ────────────────────────────────────────────────

        private void BuildStatPanel()
        {
            if (_statContainer == null) return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GridLayoutGroup grid = _statContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(88f, 22f);
            grid.spacing         = new Vector2(4f,  2f);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            _statValueTexts = new Text[StatDefs.Length];
            for (int i = 0; i < StatDefs.Length; i++)
            {
                CreateStatLabel(_statContainer, StatDefs[i].Label, builtinFont);
                _statValueTexts[i] = CreateStatValue(_statContainer, builtinFont);
            }
        }

        private void CreateStatLabel(Transform parent, string label, Font font)
        {
            GameObject go  = new($"Label_{label}");
            go.transform.SetParent(parent, false);
            Text text      = go.AddComponent<Text>();
            text.text      = label;
            text.font      = font;
            text.fontSize  = 13;
            text.color     = new Color(0.75f, 0.75f, 0.75f);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private Text CreateStatValue(Transform parent, Font font)
        {
            GameObject go  = new("Value");
            go.transform.SetParent(parent, false);
            Text text      = go.AddComponent<Text>();
            text.text      = "0";
            text.font      = font;
            text.fontSize  = 13;
            text.color     = Color.white;
            text.alignment = TextAnchor.MiddleRight;
            return text;
        }

        private void RefreshStats()
        {
            if (_statValueTexts == null || _inventorySystem == null) return;

            float[] totals = new float[StatDefs.Length];

            // ⚠️ GC 주의: Enum.GetValues는 Array를 할당함 — 장비 변경 시에만 호출되므로 허용
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                ItemData item = _inventorySystem.GetEquippedItem(slotType);
                if (item == null) continue;
                for (int i = 0; i < StatDefs.Length; i++)
                    totals[i] += StatDefs[i].Getter(item);
            }

            for (int i = 0; i < StatDefs.Length; i++)
            {
                float val = totals[i];
                // ⚠️ GC 주의: string 생성 — 장비 변경 시에만 호출되므로 허용
                _statValueTexts[i].text = StatDefs[i].IsPercent
                    ? $"{val * 100f:0.#}%"
                    : $"{val:0.##}";
            }
        }

        private void HandleEquipmentChanged(EquipmentSlotType slotType, ItemData item)
        {
            RefreshStats();
        }
    }
}
