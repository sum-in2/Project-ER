using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 아이템 정적 데이터 — ScriptableObject 1개 = 아이템 종류 1개
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "ProjectER/Data/Item")]
    public class ItemData : ScriptableObject
    {
        // ── 기본 정보 ────────────────────────────────────────────────
        [Header("기본 정보")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField][TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ItemType _itemType;

        // ── 인벤토리 ─────────────────────────────────────────────────
        [Header("인벤토리")]
        [SerializeField] private bool _isStackable;
        [SerializeField] private int _maxStack = 1;

        // ── 전투 스탯 보너스 (장비 전용) ─────────────────────────────
        [Header("스탯 보너스 (장비 전용)")]
        [SerializeField] private float _attackPower;
        [SerializeField] private float _defense;
        [SerializeField] private float _maxHpBonus;
        [SerializeField] private float _moveSpeedBonus;
        [SerializeField] private float _attackSpeedBonus;

        // ── 소비 효과 (소비 아이템 전용) ────────────────────────────
        [Header("소비 효과 (소비 아이템 전용)")]
        [SerializeField] private float _hpRestore;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ItemType ItemType => _itemType;

        public bool IsStackable => _isStackable;
        public int MaxStack => _maxStack;

        public float AttackPower => _attackPower;
        public float Defense => _defense;
        public float MaxHpBonus => _maxHpBonus;
        public float MoveSpeedBonus => _moveSpeedBonus;
        public float AttackSpeedBonus => _attackSpeedBonus;

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
            if (_itemType != ItemType.Weapon &&
                _itemType != ItemType.Chest &&
                _itemType != ItemType.Helmet &&
                _itemType != ItemType.Arms &&
                _itemType != ItemType.Shoes)
            {
                if (_attackPower != 0f || _defense != 0f || _maxHpBonus != 0f)
                    Debug.LogWarning($"[ItemData] {name}: 장비 타입이 아닌데 스탯 보너스가 설정되어 있습니다.", this);
            }
        }
#endif
    }
}
