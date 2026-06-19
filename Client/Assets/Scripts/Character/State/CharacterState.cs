namespace ProjectER.Character.State
{
    /// <summary>
    /// 캐릭터 상태 식별자. 조회·네트워크 동기화·Inspector 표시용.
    /// 실제 행동은 ICharacterState 구현체가 다형으로 처리한다.
    /// Attack/Skill은 전투·스킬 작업 때 구체 상태를 추가한다 (현재 enum에만 존재).
    /// </summary>
    public enum CharacterState
    {
        Idle,
        Move,
        Attack,
        Skill,
        Downed, // 빈사: HP 0 도달 시 진입, 부활 가능
        Dead
    }
}
