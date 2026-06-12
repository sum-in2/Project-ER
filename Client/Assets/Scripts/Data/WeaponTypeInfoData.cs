using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 무기군별 기본 스탯 — ScriptableObject 1개 = 무기군(WeaponType) 1종
    /// BSER WeaponTypeInfo.json 연동
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponTypeInfo_New", menuName = "ProjectER/Data/WeaponTypeInfo")]
    public class WeaponTypeInfoData : ScriptableObject
    {
        [Header("BSER 연동")]
        [SerializeField] private WeaponType _weaponType;

        [Header("기본 스탯")]
        [SerializeField] private float _attackSpeed; // 기본 공격속도
        [SerializeField] private float _attackRange; // 기본 공격사거리
        [SerializeField] private int   _shopFilter;  // 상점 필터 그룹

        public WeaponType WeaponType => _weaponType;
        public float AttackSpeed => _attackSpeed;
        public float AttackRange => _attackRange;
        public int   ShopFilter  => _shopFilter;
    }
}
