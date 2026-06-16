using System;
using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템 브라우저의 3종 필터(타입/특수재료/스탯) + 검색 상태를 관리한다.
    /// 상태 변경 시 OnFilterChanged를 발행하며, 호출자는 Matches()로 가시성을 판단한다.
    /// </summary>
    public class ItemFilterController
    {
        public event Action OnFilterChanged;

        // ── 색상 상수 ─────────────────────────────────────────────────

        private static readonly Color FilterNormalColor       = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color FilterSelectedColor     = new Color(0.25f, 0.50f, 0.30f);
        private static readonly Color StatFilterSelectedColor = new Color(0.15f, 0.38f, 0.58f);
        private static readonly Color SpecialNormalColor      = new Color(0.28f, 0.18f, 0.05f);
        private static readonly Color SpecialSelectedColor    = new Color(0.62f, 0.42f, 0.08f);

        // ── 필터 정의 ─────────────────────────────────────────────────

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

        // 전설재료 5종
        private static readonly (string Label, string MaterialId)[] SpecialMaterialDefs =
        {
            ("생", "401208"), // 생명의나무
            ("운", "401209"), // 운석
            ("미", "401304"), // 미스릴
            ("포", "401403"), // 포스코어
            ("VF", "401401"), // VF혈액샘플
        };

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

        // ── 의존성 ────────────────────────────────────────────────────

        private readonly Transform               _filterContainer;
        private readonly Transform               _specialMaterialContainer;
        private readonly Transform               _statFilterContainer;
        private readonly RecipeDatabase          _recipeDatabase;
        private readonly IReadOnlyList<ItemData> _allItems;

        // ── 필터 상태 ─────────────────────────────────────────────────

        private ItemType?       _currentFilter;
        private string          _currentSpecialMaterialFilter;
        private HashSet<string> _specialMaterialFilterCache;
        private readonly HashSet<int> _activeStatFilters = new HashSet<int>();
        private string          _currentSearchQuery = string.Empty;

        // ── 버튼 이미지 (강조 갱신용) ────────────────────────────────

        private Image[] _filterButtonImages;
        private Image[] _specialMaterialButtonImages;
        private Image[] _statFilterButtonImages;

        public ItemFilterController(
            Transform               filterContainer,
            Transform               specialMaterialContainer,
            Transform               statFilterContainer,
            RecipeDatabase          recipeDatabase,
            IReadOnlyList<ItemData> allItems)
        {
            _filterContainer          = filterContainer;
            _specialMaterialContainer = specialMaterialContainer;
            _statFilterContainer      = statFilterContainer;
            _recipeDatabase           = recipeDatabase;
            _allItems                 = allItems;
        }

        // ── 공개 API ─────────────────────────────────────────────────

        public void Build()
        {
            BuildFilterButtons();
            BuildSpecialMaterialButtons();
            BuildStatFilterButtons();
        }

        public void SetSearchQuery(string query)
        {
            // ⚠️ GC 주의: ToLowerInvariant 할당 — 검색어 변화 시에만 호출됨
            _currentSearchQuery = string.IsNullOrEmpty(query)
                ? string.Empty
                : query.ToLowerInvariant();
            OnFilterChanged?.Invoke();
        }

        /// <summary>현재 모든 필터 조건을 만족하는지 반환한다.</summary>
        public bool Matches(ItemData item)
        {
            bool typeMatch    = _currentFilter == null || item.ItemType == _currentFilter;
            bool specialMatch = _specialMaterialFilterCache == null
                                || _specialMaterialFilterCache.Contains(item.Id);
            bool statMatch    = _activeStatFilters.Count == 0 || HasAnySelectedStat(item);
            bool searchMatch  = KoreanSearchUtil.IsMatch(item.DisplayName, _currentSearchQuery);
            return typeMatch && specialMatch && statMatch && searchMatch;
        }

        // ── UI 빌드 ──────────────────────────────────────────────────

        private void BuildFilterButtons()
        {
            if (_filterContainer == null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

                (Image bg, Button btn) = CreateFilterButton(_filterContainer, $"FilterBtn_{label}", font, label, 11);
                bg.color = i == 0 ? FilterSelectedColor : FilterNormalColor;
                _filterButtonImages[i] = bg;

                // ⚠️ GC 주의: 클로저 캡처
                int       indexCap  = i;
                ItemType? filterCap = filter;
                btn.onClick.AddListener(() => OnTypeFilterClicked(indexCap, filterCap));

                bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            }
        }

        private void BuildSpecialMaterialButtons()
        {
            if (_specialMaterialContainer == null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

                (Image bg, Button btn) = CreateFilterButton(_specialMaterialContainer, $"SpecialMatBtn_{label}", font, label, 10);
                bg.color = SpecialNormalColor;
                _specialMaterialButtonImages[i] = bg;

                // ⚠️ GC 주의: 클로저 캡처
                int    indexCap = i;
                string matCap   = matId;
                btn.onClick.AddListener(() => OnSpecialMaterialClicked(indexCap, matCap));

                LayoutElement le   = bg.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth  = label.Length > 1 ? 26f : 20f;
                le.preferredHeight = 20f;
            }
        }

        private void BuildStatFilterButtons()
        {
            if (_statFilterContainer == null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

                (Image bg, Button btn) = CreateFilterButton(_statFilterContainer, $"StatFilterBtn_{label}", font, label, 10);
                bg.color = FilterNormalColor;
                _statFilterButtonImages[i] = bg;

                // ⚠️ GC 주의: 클로저 캡처
                int indexCap = i;
                btn.onClick.AddListener(() => OnStatFilterClicked(indexCap));

                LayoutElement le   = bg.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth  = label.Length > 1 ? 26f : 20f;
                le.preferredHeight = 20f;
            }
        }

        /// <summary>
        /// 필터 버튼 GameObject 공통 생성 패턴.
        /// 반환값은 (배경 Image, Button) — 색상 설정 및 클릭 핸들러 등록에 사용.
        /// </summary>
        private static (Image Bg, Button Btn) CreateFilterButton(Transform parent, string goName, Font font, string label, int fontSize)
        {
            GameObject go = new(goName);
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cb.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
            btn.colors          = cb;

            // Text는 자식 GO로 분리 — Image·Button과 같은 GO에 두면 AddComponent<Text>가 실패하는 경우 있음
            GameObject textGo      = new("Label");
            textGo.transform.SetParent(go.transform, false);
            Text text              = textGo.AddComponent<Text>();
            text.text              = label;
            text.font              = font;
            text.fontSize          = fontSize;
            text.fontStyle         = FontStyle.Bold;
            text.color             = Color.white;
            text.alignment         = TextAnchor.MiddleCenter;
            text.raycastTarget     = false;
            RectTransform textRt   = textGo.GetComponent<RectTransform>();
            textRt.anchorMin       = Vector2.zero;
            textRt.anchorMax       = Vector2.one;
            textRt.offsetMin       = Vector2.zero;
            textRt.offsetMax       = Vector2.zero;

            return (bg, btn);
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────────

        private void OnTypeFilterClicked(int index, ItemType? filter)
        {
            if (_filterButtonImages != null)
            {
                for (int i = 0; i < _filterButtonImages.Length; i++)
                    _filterButtonImages[i].color = i == index ? FilterSelectedColor : FilterNormalColor;
            }
            _currentFilter = filter;
            OnFilterChanged?.Invoke();
        }

        private void OnSpecialMaterialClicked(int index, string materialId)
        {
            bool   deselect = _currentSpecialMaterialFilter == materialId;
            string newId    = deselect ? null : materialId;
            int?   newIndex = deselect ? (int?)null : index;

            if (_specialMaterialButtonImages != null)
            {
                for (int i = 0; i < _specialMaterialButtonImages.Length; i++)
                    _specialMaterialButtonImages[i].color = (newIndex.HasValue && i == newIndex.Value)
                        ? SpecialSelectedColor
                        : SpecialNormalColor;
            }

            _currentSpecialMaterialFilter = newId;

            // ⚠️ GC 주의: HashSet 두 개 할당 — 필터 변경 시에만 호출되므로 허용
            if (newId == null)
            {
                _specialMaterialFilterCache = null;
            }
            else
            {
                _specialMaterialFilterCache = new HashSet<string>();
                foreach (ItemData item in _allItems)
                {
                    if (item == null) continue;
                    HashSet<string> visited = new HashSet<string>();
                    if (RouteSorter.ItemNeedsMaterial(item.Id, newId, _recipeDatabase, visited))
                        _specialMaterialFilterCache.Add(item.Id);
                }
            }

            OnFilterChanged?.Invoke();
        }

        private void OnStatFilterClicked(int index)
        {
            if (_activeStatFilters.Contains(index))
                _activeStatFilters.Remove(index);
            else
                _activeStatFilters.Add(index);

            if (_statFilterButtonImages != null)
            {
                for (int i = 0; i < _statFilterButtonImages.Length; i++)
                    _statFilterButtonImages[i].color = _activeStatFilters.Contains(i)
                        ? StatFilterSelectedColor
                        : FilterNormalColor;
            }

            OnFilterChanged?.Invoke();
        }

        private bool HasAnySelectedStat(ItemData item)
        {
            foreach (int index in _activeStatFilters)
                if (StatFilterDefs[index].Getter(item) > 0f)
                    return true;
            return false;
        }
    }
}
