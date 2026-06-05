using ProjectER.Data;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 아이템 등급별 UI 표시 색상 설정
    /// 메뉴: ProjectER/Config/ItemGradeColor
    /// </summary>
    [CreateAssetMenu(fileName = "ItemGradeColorConfig", menuName = "ProjectER/Config/ItemGradeColor")]
    public class ItemGradeColorConfig : ScriptableObject
    {
        [SerializeField] private Color _common   = new Color(0.78f, 0.78f, 0.78f); // 회색
        [SerializeField] private Color _uncommon = new Color(0.29f, 0.73f, 0.29f); // 녹색
        [SerializeField] private Color _rare     = new Color(0.25f, 0.60f, 1.00f); // 파랑
        [SerializeField] private Color _epic     = new Color(0.70f, 0.30f, 1.00f); // 보라
        [SerializeField] private Color _legend   = new Color(1.00f, 0.60f, 0.10f); // 주황
        [SerializeField] private Color _mythic   = new Color(1.00f, 0.20f, 0.20f); // 빨강

        public Color GetColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common   => _common,
                ItemGrade.Uncommon => _uncommon,
                ItemGrade.Rare     => _rare,
                ItemGrade.Epic     => _epic,
                ItemGrade.Legend   => _legend,
                ItemGrade.Mythic   => _mythic,
                _                  => Color.white,
            };
        }
    }
}
