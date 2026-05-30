using ProjectER.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템 브라우저 슬롯에 부착 — 드래그 시작 시 비주얼 복사본을 생성하고 커서를 따라 이동.
    /// 원본 슬롯의 데이터는 그대로 유지된다.
    /// </summary>
    public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // 드래그 중인 아이템 — TargetItemSlotUI가 OnDrop에서 읽음
        public static ItemData CurrentDraggedItem { get; private set; }

        [SerializeField] private ItemData _itemData;

        private Canvas        _rootCanvas;
        private RectTransform _rootCanvasRect;
        private GameObject    _dragVisual;

        private void Awake()
        {
            // 씬의 루트 Canvas를 찾아 드래그 비주얼의 부모로 사용
            _rootCanvas     = GetComponentInParent<Canvas>().rootCanvas;
            _rootCanvasRect = _rootCanvas.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 런타임 생성 시 ItemData 주입
        /// </summary>
        public void Initialize(ItemData itemData)
        {
            _itemData = itemData;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_itemData == null) return;
            CurrentDraggedItem = _itemData;
            CreateDragVisual(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragVisual == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
                _dragVisual.GetComponent<RectTransform>().anchoredPosition = localPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentDraggedItem = null;
            if (_dragVisual != null)
                Destroy(_dragVisual);
        }

        private void CreateDragVisual(PointerEventData eventData)
        {
            _dragVisual = new GameObject("DragVisual");
            _dragVisual.transform.SetParent(_rootCanvas.transform, false);

            Image img          = _dragVisual.AddComponent<Image>();
            img.raycastTarget  = false; // 드래그 비주얼이 드롭 이벤트를 가로채면 안 됨
            img.preserveAspect = true;
            if (_itemData.Icon != null)
                img.sprite = _itemData.Icon;
            else
                img.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform rt = _dragVisual.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(52f, 52f);
            rt.SetAsLastSibling(); // 항상 최상단에 렌더링

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
                rt.anchoredPosition = localPos;
        }
    }
}
