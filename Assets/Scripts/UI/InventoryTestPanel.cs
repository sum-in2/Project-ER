using System.Collections.Generic;
using ProjectER.Data;
using ProjectER.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 기본 재료 획득 테스트 패널 — 지정된 아이템마다 아이콘 버튼 1개 생성
    /// </summary>
    public class InventoryTestPanel : MonoBehaviour
    {
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private List<ItemData>  _acquisitionItems;
        [SerializeField] private Transform       _buttonContainer;

        private void Start()
        {
            Debug.Log($"[InventoryTestPanel] Start — " +
                      $"InventorySystem={_inventorySystem != null}, " +
                      $"ButtonContainer={_buttonContainer != null}, " +
                      $"아이템 수={_acquisitionItems?.Count ?? 0}");

            if (_inventorySystem == null)
            {
                Debug.LogError("[InventoryTestPanel] InventorySystem이 연결되지 않았습니다.");
                return;
            }
            if (_acquisitionItems == null || _acquisitionItems.Count == 0)
            {
                Debug.LogWarning("[InventoryTestPanel] _acquisitionItems가 비어있습니다.");
                return;
            }

            BuildButtons();
        }

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
            Debug.Log($"[InventoryTestPanel] 버튼 {created}개 생성 완료 — 부모: {parent.name}");
        }

        private void CreateButton(ItemData item, Transform parent)
        {
            GameObject go = new($"Btn_{item.Id}");
            go.transform.SetParent(parent, false);

            // 버튼 배경
            Image bg   = go.AddComponent<Image>();
            bg.color   = new Color(0.25f, 0.25f, 0.25f);

            Button btn          = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
            btn.colors          = cb;

            // ⚠️ GC 주의: 클로저 캡처 — 루프 변수 대신 로컬 변수 사용
            ItemData captured = item;
            btn.onClick.AddListener(() => OnAddItem(captured));

            // 아이템 아이콘
            GameObject iconGo   = new("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon          = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            if (item.Icon != null)
                icon.sprite = item.Icon;
            else
                icon.color = new Color(0.5f, 0.5f, 0.5f); // 아이콘 없을 때 회색
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin     = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax     = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin     = Vector2.zero;
            iconRt.offsetMax     = Vector2.zero;

            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.preferredWidth  = 44f;
        }

        private void OnAddItem(ItemData item)
        {
            Debug.Log($"[InventoryTestPanel] 버튼 클릭 — {item.DisplayName} 획득 시도");
            if (_inventorySystem.TryAddItem(item))
                Debug.Log($"[InventoryTestPanel] {item.DisplayName} 획득 성공");
            else
                Debug.LogWarning($"[InventoryTestPanel] 가방이 꽉 참 — {item.DisplayName}");
        }
    }
}
