namespace ProjectER.Combat
{
    /// <summary>
    /// 피해를 받을 수 있는 대상이 구현하는 인터페이스.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 피해를 입는다.
        /// </summary>
        /// <param name="amount">받을 피해량</param>
        void TakeDamage(float amount);

        /// <summary>
        /// 사망 처리를 수행한다.
        /// </summary>
        void Die();
    }
}
