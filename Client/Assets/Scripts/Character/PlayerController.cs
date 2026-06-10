namespace ProjectER.Character
{
    /// <summary>
    /// 플레이어가 조작하는 캐릭터.
    /// TODO: NavMesh 클릭투무브, 우클릭 입력 분기(공격/상호작용/이동),
    /// 액티브(Q/W/E/R)·무기(D)·전술(F)·패시브(T) 스킬 슬롯 연동은 추후 구현 예정.
    /// </summary>
    public class PlayerController : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
