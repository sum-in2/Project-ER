namespace ProjectER.Combat
{
    /// <summary>
    /// 공격을 수행할 수 있는 주체가 구현하는 인터페이스.
    /// </summary>
    public interface IAttackable
    {
        /// <summary>
        /// 대상에게 기본 공격을 수행한다.
        /// </summary>
        /// <param name="target">공격 대상</param>
        void Attack(IDamageable target);
    }
}
