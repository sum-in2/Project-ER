using UnityEngine;
using UnityEngine.UI;

namespace ProjectER.UI
{
    /// <summary>
    /// 아이템 슬롯 좌상단에 표시하는 직각 삼각형 UI 그래픽.
    /// RectTransform 크기 = 삼각형의 두 직각변 길이.
    /// 꼭짓점: 좌상(직각) → 우상 → 좌하
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TriangleIndicator : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;

            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = new Vector3(r.xMin, r.yMax); // 좌상 (직각)
            vh.AddVert(v);

            v.position = new Vector3(r.xMax, r.yMax); // 우상
            vh.AddVert(v);

            v.position = new Vector3(r.xMin, r.yMin); // 좌하
            vh.AddVert(v);

            vh.AddTriangle(0, 1, 2);
        }
    }
}
