using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 아이템 정적 데이터 — ScriptableObject 1개 = 아이템 종류 1개
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "ProjectER/Data/Item")]
    public class ItemData : ScriptableObject
    {
        // ── BSER 연동 ────────────────────────────────────────────────
        [Header("BSER 연동")]
        [SerializeField] private int  _bserCode;            // BSER 아이템 고유 코드 (ItemWeapon.json 등의 code 필드)
        [SerializeField] private bool _isCompletedItem;     // 완성 아이템 여부 (isCompletedItem)
        [SerializeField] private int  _manufacturableType;  // 0=조합, 1=기본재료, 2=스폰전용(제작불가)

        // ── 기본 정보 ────────────────────────────────────────────────
        [Header("기본 정보")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField][TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ItemType   _itemType;
        [SerializeField] private WeaponType _weaponType; // 무기 세부 종류 (ItemType.Weapon 일 때만 유효)
        [SerializeField] private ItemGrade  _itemGrade;

        // ── 인벤토리 ─────────────────────────────────────────────────
        [Header("인벤토리")]
        [SerializeField] private bool _isStackable;
        [SerializeField] private int _maxStack = 1;

        // ── 전투 스탯 보너스 (장비 전용) ─────────────────────────────
        [Header("스탯 보너스 (장비 전용)")]
        // 공통
        [SerializeField] private float _attackPower;        // attackPower
        [SerializeField] private float _defense;            // defense
        [SerializeField] private float _maxHpBonus;         // maxHp
        [SerializeField] private float _moveSpeedBonus;     // moveSpeed
        [SerializeField] private float _attackSpeedBonus;   // attackSpeedRatio
        [SerializeField] private float _skillAmp;           // skillAmp
        [SerializeField] private float _cooldownReduction;  // cooldownReduction
        [SerializeField] private float _adaptiveForce;      // adaptiveForce
        [SerializeField] private float _criticalStrikeChance; // criticalStrikeChance
        [SerializeField] private float _lifeSteal;          // lifeSteal
        // 방어구 전용
        [SerializeField] private float _hpRegenRatio;       // hpRegenRatio
        [SerializeField] private float _penetrationDefense; // penetrationDefense

        // ── 소비 효과 (소비 아이템 전용) ────────────────────────────
        [Header("소비 효과 (소비 아이템 전용)")]
        [SerializeField] private float _hpRestore;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public int    BserCode           => _bserCode;
        public bool   IsCompletedItem    => _isCompletedItem;
        public int    ManufacturableType => _manufacturableType;
        public string Id                 => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon        => _icon;
        public ItemType   ItemType   => _itemType;
        public WeaponType WeaponType => _weaponType;
        public ItemGrade  ItemGrade  => _itemGrade;

        public bool IsStackable => _isStackable;
        public int MaxStack => _maxStack;

        public float AttackPower          => _attackPower;
        public float Defense              => _defense;
        public float MaxHpBonus           => _maxHpBonus;
        public float MoveSpeedBonus       => _moveSpeedBonus;
        public float AttackSpeedBonus     => _attackSpeedBonus;
        public float SkillAmp             => _skillAmp;
        public float CooldownReduction    => _cooldownReduction;
        public float AdaptiveForce        => _adaptiveForce;
        public float CriticalStrikeChance => _criticalStrikeChance;
        public float LifeSteal            => _lifeSteal;
        public float HpRegenRatio         => _hpRegenRatio;
        public float PenetrationDefense   => _penetrationDefense;

        public float HpRestore => _hpRestore;

#if UNITY_EDITOR
        // Inspector에서 Id가 비어있으면 에셋 이름을 자동으로 채워줌
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
                _id = name;

            if (_maxStack < 1)
                _maxStack = 1;

            // 비장비 타입인데 스탯이 설정돼 있으면 경고
            bool isEquipment = _itemType == ItemType.Weapon ||
                               _itemType == ItemType.Chest  ||
                               _itemType == ItemType.Helmet ||
                               _itemType == ItemType.Arms   ||
                               _itemType == ItemType.Shoes;

            bool hasStatBonus = _attackPower       != 0f || _defense             != 0f ||
                                _maxHpBonus        != 0f || _skillAmp            != 0f ||
                                _cooldownReduction != 0f || _criticalStrikeChance != 0f ||
                                _lifeSteal         != 0f || _penetrationDefense  != 0f;
            if (!isEquipment && hasStatBonus)
                Debug.LogWarning($"[ItemData] {name}: 장비 타입이 아닌데 스탯 보너스가 설정되어 있습니다.", this);

            // 무기 타입이 아닌데 WeaponType이 설정돼 있으면 경고
            if (_itemType != ItemType.Weapon && _weaponType != WeaponType.None)
                Debug.LogWarning($"[ItemData] {name}: Weapon 타입이 아닌데 WeaponType이 설정되어 있습니다.", this);
        }
#endif
    }
}
