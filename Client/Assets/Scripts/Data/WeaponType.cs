namespace ProjectER.Data
{
    /// <summary>
    /// 무기 세부 종류 — BSER ItemWeapon.json weaponType 필드 기반
    /// </summary>
    public enum WeaponType
    {
        None,

        // 근접 — 검류
        OneHandSword,   // 한손검
        TwoHandSword,   // 양손검
        DualSword,      // 쌍검
        Rapier,         // 레이피어

        // 근접 — 타격류
        Hammer,         // 해머
        Axe,            // 도끼
        Bat,            // 배트
        Nunchaku,       // 쌍절곤
        Tonfa,          // 경찰봉

        // 근접 — 기타
        Spear,          // 창
        Glove,          // 글러브
        Whip,           // 채찍
        VFArm,          // 가상 팔

        // 원거리 — 활류
        Bow,            // 활
        CrossBow,       // 석궁

        // 원거리 — 총기류
        Pistol,         // 권총
        AssaultRifle,   // 돌격소총
        SniperRifle,    // 저격소총
        HighAngleFire,  // 박격포
        DirectFire,     // 직사포

        // 특수
        Guitar,         // 기타
        Camera,         // 카메라
        Arcana,         // 아르카나
    }
}
