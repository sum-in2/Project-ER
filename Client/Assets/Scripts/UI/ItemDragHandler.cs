using ProjectER.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// IItemSlot 구현체에 부착 — 드래그 시작 시 비주얼 복사본을 생성하고 커서를 따라 이동.
    /// 슬롯의 IsDraggable이 false거나 CurrentItem이 없으면 드래그를 시작하지 않는다.
    /// 원본 슬롯의 데이터는 그대로 유지된다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DragVisualSize = 52f;

        private IItemSlot     _slot;
        private Canvas        _rootCanvas;
        private RectTransform _rootCanvasRect;
        private GameObject    _dragVisual;

        private void Awake()
        {
            TryGetComponent(out _slot);
            _rootCanvas     = GetComponentInParent<Canvas>().rootCanvas;
            _rootCanvasRect = _rootCanvas.GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_slot == null || !_slot.IsDraggable || _slot.CurrentItem == null) return;

            CreateDragVisual(eventData, _slot.CurrentItem);
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
            if (_dragVisual != null)
            {
                Destroy(_dragVisual);

                // 도착 지점 판정: 마우스 포인터 아래에 IItemDropTarget 슬롯이 있으면 그 슬롯으로 이동
                IItemDropTarget dropTarget = FindDropTarget(eventData);
                if (dropTarget == null || !dropTarget.TryDropItem(_slot))
                {
                    // 슬롯이 아닌 곳(월드맵)에 드롭 — 캐릭터 발 밑에 아이템 드랍
                    // TODO: 인게임 인벤토리 UI 연동 시 플레이어 위치에 월드 아이템 스폰 처리
                    Debug.Log($"[ItemDragHandler] {_slot.CurrentItem.DisplayName}을 월드(캐릭터 발 밑)에 드랍합니다. (TODO: 월드 드랍 미구현)");
                }
            }
        }

        // 마우스 포인터 아래에 있는 UI 요소에서 IItemDropTarget을 탐색
        private static IItemDropTarget FindDropTarget(PointerEventData eventData)
        {
            GameObject hover = eventData.pointerCurrentRaycast.gameObject;
            return hover != null ? hover.GetComponentInParent<IItemDropTarget>() : null;
        }

        private void CreateDragVisual(PointerEventData eventData, ItemData item)
        {
            _dragVisual = new GameObject("DragVisual");
            _dragVisual.transform.SetParent(_rootCanvas.transform, false);

            Image img          = _dragVisual.AddComponent<Image>();
            img.raycastTarget  = false; // 드래그 비주얼이 드롭 이벤트를 가로채면 안 됨
            img.preserveAspect = true;
            if (item.Icon != null)
                img.sprite = item.Icon;
            else
                img.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform rt = _dragVisual.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(DragVisualSize, DragVisualSize);
            rt.SetAsLastSibling(); // 항상 최상단에 렌더링

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
                rt.anchoredPosition = localPos;
        }
    }
}
