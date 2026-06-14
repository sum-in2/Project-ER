using System.Collections.Generic;
using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 실험체 정적 데이터 — ScriptableObject 1개 = 실험체 1종
    /// </summary>
    [CreateAssetMenu(fileName = "Character_New", menuName = "ProjectER/Data/Character")]
    public class CharacterData : ScriptableObject
    {
        // ── BSER 연동 ────────────────────────────────────────────────
        [Header("BSER 연동")]
        [SerializeField] private int _bserCode; // BSER 실험체 고유 코드 (Character.json code 필드)

        // ── 기본 정보 ────────────────────────────────────────────────
        [Header("기본 정보")]
        [SerializeField] private string _name;        // 영문 코드명 (resource 키, 예: "Jackie")
        [SerializeField] private string _displayName; // 한국어 표시명 (l10n_Korean_character)

        // ── 초상화 (팬킷 02. Default) ──────────────────────────────────
        [Header("초상화")]
        [SerializeField] private Sprite _portraitFull; // 전신
        [SerializeField] private Sprite _portraitHalf; // 반신
        [SerializeField] private Sprite _portraitMini; // 아이콘

        // ── 분류 ─────────────────────────────────────────────────────
        [Header("분류")]
        [SerializeField] private CharacterArcheType _archeType1;
        [SerializeField] private CharacterArcheType _archeType2;
        [SerializeField] private WeaponRangeType _weaponRangeType;
        [SerializeField] private List<WeaponType> _weaponMasteries = new(); // 사용 가능 무기군

        // ── 전투 스탯 ────────────────────────────────────────────────
        [Header("전투 스탯")]
        [SerializeField] private float _maxHp;
        [SerializeField] private float _attackPower;
        [SerializeField] private float _defense;
        [SerializeField] private float _hpRegen;
        [SerializeField] private float _attackSpeed;
        [SerializeField] private float _attackSpeedLimit;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _stoppingDistance = 0.1f; // NavMeshAgent.stoppingDistance
        [SerializeField] private float _sightRange;
        [SerializeField] private float _skillAmp;
        [SerializeField] private float _adaptiveForce;
        [SerializeField] private float _criticalStrikeChance;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public int BserCode => _bserCode;

        public string Name        => _name;
        public string DisplayName => _displayName;

        public Sprite PortraitFull => _portraitFull;
        public Sprite PortraitHalf => _portraitHalf;
        public Sprite PortraitMini => _portraitMini;

        public CharacterArcheType ArcheType1      => _archeType1;
        public CharacterArcheType ArcheType2      => _archeType2;
        public WeaponRangeType    WeaponRangeType => _weaponRangeType;
        public IReadOnlyList<WeaponType> WeaponMasteries => _weaponMasteries;

        public float MaxHp                => _maxHp;
        public float AttackPower          => _attackPower;
        public float Defense              => _defense;
        public float HpRegen              => _hpRegen;
        public float AttackSpeed          => _attackSpeed;
        public float AttackSpeedLimit     => _attackSpeedLimit;
        public float MoveSpeed            => _moveSpeed;
        public float StoppingDistance     => _stoppingDistance;
        public float SightRange           => _sightRange;
        public float SkillAmp             => _skillAmp;
        public float AdaptiveForce        => _adaptiveForce;
        public float CriticalStrikeChance => _criticalStrikeChance;

#if UNITY_EDITOR
        // Inspector에서 DisplayName이 비어있으면 영문 이름으로 채워줌
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_displayName))
                _displayName = _name;
        }
#endif
    }
}
