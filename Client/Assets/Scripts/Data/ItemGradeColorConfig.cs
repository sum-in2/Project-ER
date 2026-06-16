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
        [Header("테두리/하이라이트색 (밝음)")]
        [SerializeField] private Color _common   = new Color(0.78f, 0.78f, 0.78f); // 회색
        [SerializeField] private Color _uncommon = new Color(0.29f, 0.73f, 0.29f); // 녹색
        [SerializeField] private Color _rare     = new Color(0.25f, 0.60f, 1.00f); // 파랑
        [SerializeField] private Color _epic     = new Color(0.70f, 0.30f, 1.00f); // 보라
        [SerializeField] private Color _legend   = new Color(1.00f, 0.60f, 0.10f); // 주황
        [SerializeField] private Color _mythic   = new Color(1.00f, 0.20f, 0.20f); // 빨강

        [Header("배경색 (어두움, 슬롯/버튼 배경 전용)")]
        [SerializeField] private Color _commonBg   = new Color(0.30f, 0.30f, 0.30f);
        [SerializeField] private Color _uncommonBg = new Color(0.18f, 0.38f, 0.18f);
        [SerializeField] private Color _rareBg     = new Color(0.15f, 0.28f, 0.50f);
        [SerializeField] private Color _epicBg     = new Color(0.38f, 0.18f, 0.50f);
        [SerializeField] private Color _legendBg   = new Color(0.55f, 0.38f, 0.08f);
        [SerializeField] private Color _mythicBg   = new Color(0.55f, 0.15f, 0.15f);

        /// <summary>테두리/하이라이트용 밝은 등급색</summary>
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

        /// <summary>슬롯/버튼 배경용 어두운 등급색</summary>
        public Color GetBackgroundColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common   => _commonBg,
                ItemGrade.Uncommon => _uncommonBg,
                ItemGrade.Rare     => _rareBg,
                ItemGrade.Epic     => _epicBg,
                ItemGrade.Legend   => _legendBg,
                ItemGrade.Mythic   => _mythicBg,
                _                  => _commonBg,
            };
        }
    }
}
