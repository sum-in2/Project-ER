using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.UI
{
    /// <summary>
    /// 로비 내 화면(전략, 도감, 상점 등)을 스택 방식으로 전환하는 매니저.
    /// 패널은 처음 열릴 때 Instantiate되고, 닫힐 때 IUIPanel.CacheOnClose 값에 따라
    /// SetActive(false)로 캐시되거나 Destroy되어 메모리를 회수한다.
    /// </summary>
    public class LobbyUIManager : MonoBehaviour
    {
        [SerializeField] private Transform _panelRoot;
        [SerializeField] private PanelEntry[] _panelEntries;

        private readonly Dictionary<LobbyPanelType, GameObject> _prefabMap = new();
        private readonly Dictionary<LobbyPanelType, GameObject> _instanceMap = new();
        private readonly Stack<LobbyPanelType> _history = new();

        /// <summary>
        /// 현재 화면에 표시 중인 패널. 스택이 비어있으면 null (로비 메인 화면 상태)
        /// </summary>
        public LobbyPanelType? CurrentPanel => _history.Count > 0 ? _history.Peek() : null;

        private void Awake()
        {
            foreach (PanelEntry entry in _panelEntries)
                _prefabMap[entry.Type] = entry.Prefab;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
            _prefabMap.Clear();
            _instanceMap.Clear();
            _history.Clear();
        }

        /// <summary>
        /// 패널을 열고 진입 순서를 스택에 기록한다. 이전 패널은 정책에 따라 캐시 또는 파괴된다.
        /// </summary>
        public void OpenPanel(LobbyPanelType type)
        {
            if (_history.Count > 0)
                HidePanel(_history.Peek());

            _history.Push(type);
            ShowPanel(type);
        }

        /// <summary>
        /// 직전 패널로 돌아간다. 스택이 비면 로비 메인 화면 상태가 된다.
        /// </summary>
        public void GoBack()
        {
            if (_history.Count == 0) return;

            LobbyPanelType closing = _history.Pop();
            HidePanel(closing);

            if (_history.Count > 0)
                ShowPanel(_history.Peek());
        }

        private void ShowPanel(LobbyPanelType type)
        {
            if (!_instanceMap.TryGetValue(type, out GameObject instance) || instance == null)
            {
                if (!_prefabMap.TryGetValue(type, out GameObject prefab) || prefab == null)
                {
                    Debug.LogWarning($"[LobbyUIManager] 등록되지 않은 패널 타입: {type}");
                    return;
                }

                instance = Instantiate(prefab, _panelRoot); // ⚠️ GC 주의: 패널 최초 오픈 시에만 발생
                _instanceMap[type] = instance;

                if (instance.TryGetComponent(out IUIPanel newPanel))
                    newPanel.SetCloseHandler(GoBack);
            }

            instance.SetActive(true);

            if (instance.TryGetComponent(out IUIPanel panel))
                panel.OnOpen();
        }

        private void HidePanel(LobbyPanelType type)
        {
            if (!_instanceMap.TryGetValue(type, out GameObject instance) || instance == null)
                return;

            bool cacheOnClose = false;
            if (instance.TryGetComponent(out IUIPanel panel))
            {
                panel.OnClose();
                cacheOnClose = panel.CacheOnClose;
            }

            if (cacheOnClose)
            {
                instance.SetActive(false);
            }
            else
            {
                _instanceMap.Remove(type);
                Destroy(instance);
            }
        }

        [Serializable]
        private class PanelEntry
        {
            [SerializeField] private LobbyPanelType _type;
            [SerializeField] private GameObject _prefab;

            public LobbyPanelType Type => _type;
            public GameObject Prefab => _prefab;
        }
    }
}
