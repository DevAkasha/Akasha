using System;

/// <summary>
/// 테스트용 효과 ID 열거형
/// EffectManager에서 사용할 다양한 효과들을 정의
/// </summary>
public enum TestEffectId
{
    // === 즉시 효과 (DirectEffect) ===
    HealingPotion,          // 체력 회복 포션
    ManaPotion,             // 마나 회복 포션
    ExperienceBoost,        // 경험치 부스트
    GoldBonus,              // 골드 보너스
    FullRestore,            // 완전 회복

    // === 버프 효과 (ModifierEffect) ===
    StrengthBuff,           // 힘 증가 버프
    DefenseBuff,            // 방어력 증가 버프
    SpeedBuff,              // 이동속도 증가 버프
    AttackSpeedBuff,        // 공격속도 증가 버프
    CriticalBuff,           // 치명타 확률 증가 버프
    BerserkMode,            // 광폭화 모드 (공격력↑, 방어력↓)

    // === 디버프 효과 (ModifierEffect) ===
    Poison,                 // 독 상태 (지속 데미지)
    Weakness,               // 약화 (공격력 감소)
    Slow,                   // 둔화 (이동속도 감소)
    Curse,                  // 저주 (모든 스탯 감소)

    // === 장비 효과 (ModifierEffect) ===
    IronSword,              // 철검 (공격력 +10)
    SteelArmor,             // 강철 갑옷 (방어력 +15, 이동속도 -10%)
    LeatherBoots,           // 가죽 부츠 (이동속도 +20%)
    MagicRing,              // 마법 반지 (마나 +30, 마나 재생)
    LegendaryWeapon,        // 전설의 무기 (복합 효과)

    // === 특수 효과 ===
    Invincibility,          // 무적 상태
    Teleport,               // 순간이동
    TimeStop,               // 시간 정지
    Regeneration,           // 체력 재생
    ManaRegeneration,       // 마나 재생

    // === 조건부 효과 ===
    CombatBonus,            // 전투 중에만 활성화되는 보너스
    LowHealthBuff,          // 체력이 낮을 때 활성화되는 버프
    CriticalConditional,    // 치명타 발생 시 추가 효과

    // === 보간 효과 (Interpolated) ===
    FadeOut,                // 서서히 사라지는 효과
    PowerCharge,            // 점진적으로 파워 충전
    SlowMotion,             // 슬로우 모션 효과

    // === 복합 효과 (ComplexEffect) ===
    LevelUpBonus,           // 레벨업 시 여러 효과 동시 적용
    DeathCurse,             // 사망 시 발동되는 복합 저주
    VictoryReward,          // 승리 시 보상 효과

    // === 테스트용 특수 효과 ===
    TestStack,              // 스택 테스트용
    TestRefresh,            // 갱신 테스트용
    TestConditional,        // 조건부 제거 테스트용
    TestInterpolation,      // 보간 테스트용
    TestSignFlip,           // 부호 반전 테스트용
}
