using System;

/// <summary>
/// 테스트용 캐릭터 상태 열거형
/// </summary>
public enum CharacterState
{
    Idle,       // 대기
    Moving,     // 이동 중
    Attacking,  // 공격 중
    Casting,    // 스킬 시전 중
    Stunned,    // 기절
    Dead        // 사망
}

/// <summary>
/// 테스트용 캐릭터 플래그 열거형
/// </summary>
public enum CharacterFlags
{
    IsAlive,        // 생존 상태
    CanMove,        // 이동 가능
    CanAttack,      // 공격 가능
    CanCast,        // 스킬 시전 가능
    IsInCombat,     // 전투 중
    IsPoisoned,     // 독 상태
    IsStunned,      // 기절 상태
    HasShield,      // 방어막 보유
    IsInvincible,   // 무적 상태
    IsBerserk       // 광폭화 상태
}

/// <summary>
/// 프레임워크 테스트를 위한 캐릭터 모델
/// PowerModel의 모든 Reactive 기능을 테스트할 수 있도록 설계
/// </summary>
public class CharacterModel : BaseModel
{
    #region 기본 정보 (RxVar) - 단순 값 저장

    /// <summary>캐릭터 이름</summary>
    public RxVar<string> Name { get; private set; }

    /// <summary>캐릭터 레벨</summary>
    public RxVar<int> Level { get; private set; }

    /// <summary>현재 경험치</summary>
    public RxVar<int> Experience { get; private set; }

    /// <summary>레벨업에 필요한 경험치</summary>
    public RxVar<int> ExperienceToNext { get; private set; }

    /// <summary>보유 골드</summary>
    public RxVar<int> Gold { get; private set; }

    #endregion

    #region 전투 스탯 (RxMod) - 수정자 시스템 테스트용

    /// <summary>현재 체력 (회복/데미지로 변경)</summary>
    public RxMod<int> Health { get; private set; }

    /// <summary>최대 체력 (장비/버프로 수정)</summary>
    public RxMod<int> MaxHealth { get; private set; }

    /// <summary>현재 마나</summary>
    public RxMod<int> Mana { get; private set; }

    /// <summary>최대 마나</summary>
    public RxMod<int> MaxMana { get; private set; }

    /// <summary>공격력 (무기/버프로 수정)</summary>
    public RxMod<int> Attack { get; private set; }

    /// <summary>방어력 (방어구/버프로 수정)</summary>
    public RxMod<int> Defense { get; private set; }

    /// <summary>이동 속도 (장비/상태로 수정)</summary>
    public RxMod<float> MoveSpeed { get; private set; }

    /// <summary>공격 속도 (무기/버프로 수정)</summary>
    public RxMod<float> AttackSpeed { get; private set; }

    /// <summary>치명타 확률 (0.0 ~ 1.0)</summary>
    public RxMod<float> CriticalChance { get; private set; }

    /// <summary>치명타 데미지 배율</summary>
    public RxMod<float> CriticalDamage { get; private set; }

    #endregion

    #region 상태 관리 (RxStateFlagSet & FSM)

    /// <summary>캐릭터 상태 플래그들</summary>
    public RxStateFlagSet<CharacterFlags> Flags { get; private set; }

    /// <summary>캐릭터 상태 머신</summary>
    public FSM<CharacterState> StateMachine { get; private set; }

    #endregion

    #region 생성자 및 초기화

    public CharacterModel(string name = "TestCharacter")
    {
        InitializeBasicStats(name);
        InitializeCombatStats();
        InitializeStateManagement();
        SetupReactiveLogic();
    }

    /// <summary>기본 스탯 초기화</summary>
    private void InitializeBasicStats(string name)
    {
        Name = new RxVar<string>(name, this);
        Level = new RxVar<int>(1, this);
        Experience = new RxVar<int>(0, this);
        ExperienceToNext = new RxVar<int>(100, this);
        Gold = new RxVar<int>(50, this);
    }

    /// <summary>전투 스탯 초기화</summary>
    private void InitializeCombatStats()
    {
        // 체력 관련
        MaxHealth = new RxMod<int>(100, "MaxHealth", this);
        Health = new RxMod<int>(100, "Health", this);

        // 마나 관련
        MaxMana = new RxMod<int>(50, "MaxMana", this);
        Mana = new RxMod<int>(50, "Mana", this);

        // 공격 관련
        Attack = new RxMod<int>(20, "Attack", this);
        Defense = new RxMod<int>(10, "Defense", this);

        // 속도 관련
        MoveSpeed = new RxMod<float>(5.0f, "MoveSpeed", this);
        AttackSpeed = new RxMod<float>(1.0f, "AttackSpeed", this);

        // 치명타 관련
        CriticalChance = new RxMod<float>(0.1f, "CriticalChance", this);
        CriticalDamage = new RxMod<float>(1.5f, "CriticalDamage", this);
    }

    /// <summary>상태 관리 초기화</summary>
    private void InitializeStateManagement()
    {
        // 플래그 초기화
        Flags = new RxStateFlagSet<CharacterFlags>(this);

        // 기본 플래그 설정
        Flags.SetValue(CharacterFlags.IsAlive, true);
        Flags.SetValue(CharacterFlags.CanMove, true);
        Flags.SetValue(CharacterFlags.CanAttack, true);
        Flags.SetValue(CharacterFlags.CanCast, true);

        // FSM 초기화
        StateMachine = new FSM<CharacterState>(CharacterState.Idle, this);

        // FSM 전이 규칙 설정
        SetupStateMachineRules();
    }

    /// <summary>상태 머신 규칙 설정</summary>
    private void SetupStateMachineRules()
    {
        StateMachine
            // 이동 가능 조건
            .AddTransitionRule(CharacterState.Moving,
                from => Flags.GetValue(CharacterFlags.CanMove) &&
                       Flags.GetValue(CharacterFlags.IsAlive))

            // 공격 가능 조건  
            .AddTransitionRule(CharacterState.Attacking,
                from => Flags.GetValue(CharacterFlags.CanAttack) &&
                       Flags.GetValue(CharacterFlags.IsAlive))

            // 스킬 시전 가능 조건
            .AddTransitionRule(CharacterState.Casting,
                from => Flags.GetValue(CharacterFlags.CanCast) &&
                       Flags.GetValue(CharacterFlags.IsAlive) &&
                       Mana.Value > 0)

            // 기절 상태 조건
            .AddTransitionRule(CharacterState.Stunned,
                from => Flags.GetValue(CharacterFlags.IsStunned))

            // 사망 상태 조건
            .AddTransitionRule(CharacterState.Dead,
                from => !Flags.GetValue(CharacterFlags.IsAlive))

            // 대기 상태로 복귀 조건
            .AddTransitionRule(CharacterState.Idle,
                from => Flags.GetValue(CharacterFlags.IsAlive) &&
                       !Flags.GetValue(CharacterFlags.IsStunned));
    }

    /// <summary>반응형 로직 설정</summary>
    private void SetupReactiveLogic()
    {
        // 체력이 0 이하가 되면 사망 처리
        Health.AddListener(hp =>
        {
            if (hp <= 0)
            {
                Flags.SetValue(CharacterFlags.IsAlive, false);
                Flags.SetValue(CharacterFlags.CanMove, false);
                Flags.SetValue(CharacterFlags.CanAttack, false);
                Flags.SetValue(CharacterFlags.CanCast, false);
            }
        });

        // 레벨 변경 시 스탯 증가
        Level.AddListener(level =>
        {
            var healthBonus = (level - 1) * 20;
            var manaBonus = (level - 1) * 10;
            var attackBonus = (level - 1) * 5;
            var defenseBonus = (level - 1) * 3;

            MaxHealth.Set(100 + healthBonus);
            MaxMana.Set(50 + manaBonus);
            Attack.Set(20 + attackBonus);
            Defense.Set(10 + defenseBonus);

            // 레벨업 시 체력과 마나 회복
            if (Flags.GetValue(CharacterFlags.IsAlive))
            {
                Health.Set(MaxHealth.Value);
                Mana.Set(MaxMana.Value);
            }
        });

        // 경험치가 목표치에 도달하면 레벨업
        Experience.AddListener(exp =>
        {
            while (exp >= ExperienceToNext.Value)
            {
                var remaining = exp - ExperienceToNext.Value;
                Level.Set(Level.Value + 1);
                ExperienceToNext.Set(ExperienceToNext.Value + 50); // 레벨당 50씩 증가
                Experience.Set(remaining);
            }
        });

        // FSM을 플래그 변화에 따라 자동 구동
        StateMachine.DriveByFlags(Flags, flags =>
        {
            // 사망 상태 우선
            if (!flags.GetValue(CharacterFlags.IsAlive))
                return CharacterState.Dead;

            // 기절 상태
            if (flags.GetValue(CharacterFlags.IsStunned))
                return CharacterState.Stunned;

            // 전투 중이면서 공격 가능하면 공격
            if (flags.GetValue(CharacterFlags.IsInCombat) &&
                flags.GetValue(CharacterFlags.CanAttack))
                return CharacterState.Attacking;

            // 기본적으로 대기
            return CharacterState.Idle;
        });

        // 디버그용 로그
        SetupDebugLogging();
    }

    /// <summary>디버그 로깅 설정</summary>
    private void SetupDebugLogging()
    {
        Name.WithDebug("캐릭터 이름");
        Level.WithDebug("레벨");
        Health.WithDebug("체력");
        StateMachine.WithDebug("[캐릭터FSM]");
        Flags.WithDebug("[캐릭터플래그]");
    }

    #endregion

    #region 테스트용 헬퍼 메서드

    /// <summary>데미지 적용</summary>
    public void TakeDamage(int damage)
    {
        var actualDamage = Math.Max(1, damage - Defense.Value);
        Health.Set(Math.Max(0, Health.Value - actualDamage));

        UnityEngine.Debug.Log($"{Name.Value}이(가) {actualDamage} 데미지를 받았습니다. (체력: {Health.Value}/{MaxHealth.Value})");
    }

    /// <summary>체력 회복</summary>
    public void Heal(int amount)
    {
        var newHealth = Math.Min(MaxHealth.Value, Health.Value + amount);
        Health.Set(newHealth);

        UnityEngine.Debug.Log($"{Name.Value}이(가) {amount} 체력을 회복했습니다. (체력: {Health.Value}/{MaxHealth.Value})");
    }

    /// <summary>경험치 획득</summary>
    public void GainExperience(int exp)
    {
        Experience.Set(Experience.Value + exp);
        UnityEngine.Debug.Log($"{Name.Value}이(가) {exp} 경험치를 획득했습니다.");
    }

    /// <summary>골드 획득</summary>
    public void GainGold(int amount)
    {
        Gold.Set(Gold.Value + amount);
        UnityEngine.Debug.Log($"{Name.Value}이(가) {amount} 골드를 획득했습니다. (총 골드: {Gold.Value})");
    }

    /// <summary>전투 상태 설정</summary>
    public void EnterCombat()
    {
        Flags.SetValue(CharacterFlags.IsInCombat, true);
        UnityEngine.Debug.Log($"{Name.Value}이(가) 전투에 돌입했습니다!");
    }

    /// <summary>전투 종료</summary>
    public void ExitCombat()
    {
        Flags.SetValue(CharacterFlags.IsInCombat, false);
        UnityEngine.Debug.Log($"{Name.Value}의 전투가 종료되었습니다.");
    }

    /// <summary>상태 이상 적용</summary>
    public void ApplyStatusEffect(CharacterFlags flag, bool value)
    {
        Flags.SetValue(flag, value);
        UnityEngine.Debug.Log($"{Name.Value}에게 {flag} 상태가 {(value ? "적용" : "해제")}되었습니다.");
    }

    /// <summary>현재 상태 요약 출력</summary>
    public void PrintStatus()
    {
        UnityEngine.Debug.Log($"=== {Name.Value} 상태 ===");
        UnityEngine.Debug.Log($"레벨: {Level.Value} | 경험치: {Experience.Value}/{ExperienceToNext.Value}");
        UnityEngine.Debug.Log($"체력: {Health.Value}/{MaxHealth.Value} | 마나: {Mana.Value}/{MaxMana.Value}");
        UnityEngine.Debug.Log($"공격력: {Attack.Value} | 방어력: {Defense.Value}");
        UnityEngine.Debug.Log($"이동속도: {MoveSpeed.Value} | 공격속도: {AttackSpeed.Value}");
        UnityEngine.Debug.Log($"상태: {StateMachine.Value}");
        UnityEngine.Debug.Log($"골드: {Gold.Value}");
    }

    #endregion
}