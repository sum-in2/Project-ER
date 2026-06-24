using System;
using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI.Pick
{
    /// <summary>
    /// 픽 2단계 루트 선택 패널 — 계정에 저장된 루트 목록(ISavedRouteSource)을 표시하고
    /// 그중 하나를 단일 선택한다. 루트 생성은 이 패널의 책임이 아니다(추후 별도 UI).
    /// 빌더가 컨테이너/등급색을 주입하고, 컨트롤러가 Build/Show/Hide와 OnRouteChosen을 사용한다.
    /// </summary>
    public sealed class RouteSelectPanelUI : MonoBehaviour
    {
        [SerializeField] private Transform            _entryContainer; // 루트 행이 쌓이는 세로 레이아웃 컨테이너
        [SerializeField] private ItemGradeColorConfig _gradeConfig;

        private readonly List<RouteEntrySlotUI> _entries = new();
        private RouteEntrySlotUI _selectedEntry;

        public SavedRoute SelectedRoute => _selectedEntry != null ? _selectedEntry.Route : null;

        // 루트가 선택될 때 발생 (null 가능 — 선택 해제 시)
        public event Action<SavedRoute> OnRouteChosen;

        /// <summary>저장된 루트 목록으로 행 UI를 (재)생성한다.</summary>
        public void Build(IReadOnlyList<SavedRoute> routes)
        {
            Clear();
            if (_entryContainer == null)
            {
                Debug.LogError("[RouteSelectPanelUI] _entryContainer가 연결되지 않았습니다.");
                return;
            }

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (routes == null || routes.Count == 0)
            {
                Debug.LogWarning("[RouteSelectPanelUI] 저장된 루트가 없습니다.");
                return;
            }

            foreach (SavedRoute route in routes)
            {
                if (route == null) continue;
                RouteEntrySlotUI entry = RouteEntrySlotUI.Create(_entryContainer, route, builtinFont, _gradeConfig);
                entry.OnClicked += HandleEntryClicked;
                _entries.Add(entry);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void Clear()
        {
            foreach (RouteEntrySlotUI entry in _entries)
            {
                if (entry == null) continue;
                entry.OnClicked -= HandleEntryClicked;
                Destroy(entry.gameObject);
            }
            _entries.Clear();
            _selectedEntry = null;
        }

        private void HandleEntryClicked(RouteEntrySlotUI entry)
        {
            if (_selectedEntry == entry) return;

            if (_selectedEntry != null)
                _selectedEntry.SetSelected(false);

            _selectedEntry = entry;
            _selectedEntry.SetSelected(true);

            OnRouteChosen?.Invoke(_selectedEntry.Route);
        }

#if UNITY_EDITOR
        public void Editor_SetReferences(Transform entryContainer, ItemGradeColorConfig gradeConfig)
        {
            _entryContainer = entryContainer;
            _gradeConfig = gradeConfig;
        }
#endif
    }
}
