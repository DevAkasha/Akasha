using System.Collections;
using UnityEngine;

/// <summary>
/// CharacterModel의 모든 기능을 테스트하는 스크립트
/// 1단계: 데이터 계층 기능 검증
/// </summary>
public class CharacterModelTester : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool autoRunTests = true;
    [SerializeField] private float testInterval = 2f;

    private CharacterModel testCharacter;
    private int currentTestIndex = 0;

    void Start()
    {
        Debug.Log("=== PowerModel Framework 테스트 시작 ===");
        Debug.Log("1단계: 데이터 계층 (CharacterModel) 기능 테스트");

        // 테스트 캐릭터 생성
        testCharacter = new CharacterModel("테스트히어로");

        // 초기 상태 출력
        testCharacter.PrintStatus();

        if (autoRunTests)
        {
            StartCoroutine(RunAutomatedTests());
        }
    }

    /// <summary>자동화된 테스트 실행</summary>
    private IEnumerator RunAutomatedTests()
    {
        yield return new WaitForSeconds(1f);

        // 1. RxVar 기능 테스트
        Debug.Log("\n--- 1. RxVar 기능 테스트 ---");
        TestRxVarFunctionality();
        yield return new WaitForSeconds(testInterval);

        // 2. RxMod 기능 테스트  
        Debug.Log("\n--- 2. RxMod 기능 테스트 ---");
        TestRxModFunctionality();
        yield return new WaitForSeconds(testInterval);

        // 3. RxStateFlagSet 기능 테스트
        Debug.Log("\n--- 3. RxStateFlagSet 기능 테스트 ---");
        TestRxStateFlagSetFunctionality();
        yield return new WaitForSeconds(testInterval);

        // 4. FSM 기능 테스트
        Debug.Log("\n--- 4. FSM 기능 테스트 ---");
        TestFSMFunctionality();
        yield return new WaitForSeconds(testInterval);

        // 5. 반응형 시스템 통합 테스트
        Debug.Log("\n--- 5. 반응형 시스템 통합 테스트 ---");
        TestReactiveSystemIntegration();
        yield return new WaitForSeconds(testInterval);

        // 6. 스트레스 테스트
        Debug.Log("\n--- 6. 스트레스 테스트 ---");
        TestStressScenarios();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n=== 1단계 테스트 완료 ===");
        testCharacter.PrintStatus();
    }

    /// <summary>RxVar 기능 테스트</summary>
    private void TestRxVarFunctionality()
    {
        Debug.Log("RxVar 값 변경 및 알림 테스트...");

        // 이름 변경 테스트
        testCharacter.Name.Set("파워히어로");

        // 골드 변경 테스트  
        testCharacter.GainGold(100);
        testCharacter.GainGold(-30);

        // 경험치 및 자동 레벨업 테스트
        Debug.Log("경험치 획득으로 자동 레벨업 테스트...");
        testCharacter.GainExperience(80);  // 총 80
        testCharacter.GainExperience(50);  // 총 130 -> 레벨업 발생!
        testCharacter.GainExperience(200); // 또 다른 레벨업 테스트
    }

    /// <summary>RxMod 기능 테스트</summary>
    private void TestRxModFunctionality()
    {
        Debug.Log("RxMod 수정자 시스템 테스트...");

        // 현재 공격력 확인
        Debug.Log($"기본 공격력: {testCharacter.Attack.Value}");

        // 무기 효과 시뮬레이션 (Additive)
        var weaponKey = new ModifierKey(TestEffectId.IronSword);
        testCharacter.Attack.SetModifier(ModifierType.OriginAdd, weaponKey, 15);
        Debug.Log($"무기 장착 후 공격력: {testCharacter.Attack.Value}");

        // 버프 효과 시뮬레이션 (Multiplier)
        var buffKey = new ModifierKey(TestEffectId.StrengthBuff);
        testCharacter.Attack.SetModifier(ModifierType.Multiplier, buffKey, 2);
        Debug.Log($"버프 적용 후 공격력: {testCharacter.Attack.Value}");

        // 최종 보너스 시뮬레이션 (FinalAdd)
        var bonusKey = new ModifierKey(TestEffectId.CombatBonus);
        testCharacter.Attack.SetModifier(ModifierType.FinalAdd, bonusKey, 10);
        Debug.Log($"최종 보너스 후 공격력: {testCharacter.Attack.Value}");

        // 공식 확인
        Debug.Log($"계산 공식: {testCharacter.Attack.DebugFormula}");

        // 수정자 제거 테스트
        testCharacter.Attack.RemoveModifier(ModifierType.Multiplier, buffKey);
        Debug.Log($"버프 해제 후 공격력: {testCharacter.Attack.Value}");

        // 이동속도 테스트 (float)
        Debug.Log($"기본 이동속도: {testCharacter.MoveSpeed.Value}");
        var bootsKey = new ModifierKey(TestEffectId.LeatherBoots);
        testCharacter.MoveSpeed.SetModifier(ModifierType.AddMultiplier, bootsKey, 0.3f); // 30% 증가
        Debug.Log($"부츠 착용 후 이동속도: {testCharacter.MoveSpeed.Value}");
    }

    /// <summary>RxStateFlagSet 기능 테스트</summary>
    private void TestRxStateFlagSetFunctionality()
    {
        Debug.Log("RxStateFlagSet 상태 플래그 테스트...");

        // 현재 활성 플래그들 출력
        Debug.Log("현재 활성 플래그들:");
        foreach (var flag in testCharacter.Flags.ActiveFlags())
        {
            Debug.Log($"  - {flag}");
        }

        // 독 상태 적용
        testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, true);

        // 기절 상태 적용
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, true);

        // 조건부 검사 테스트
        if (testCharacter.Flags.AnyActive(CharacterFlags.IsPoisoned, CharacterFlags.IsStunned))
        {
            Debug.Log("캐릭터가 상태이상에 걸려있습니다!");
        }

        // 무적 상태 적용 후 모든 디버프 무시 테스트
        testCharacter.ApplyStatusEffect(CharacterFlags.IsInvincible, true);
        Debug.Log("무적 상태 활성화!");

        // 상태 해제
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, false);
        testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, false);
    }

    /// <summary>FSM 기능 테스트</summary>
    private void TestFSMFunctionality()
    {
        Debug.Log("FSM 상태 머신 테스트...");

        // 현재 상태 확인
        Debug.Log($"현재 상태: {testCharacter.StateMachine.Value}");

        // 수동 상태 전이 테스트
        Debug.Log("공격 상태로 전이 시도...");
        testCharacter.StateMachine.Request(CharacterState.Attacking);
        Debug.Log($"전이 후 상태: {testCharacter.StateMachine.Value}");

        // 조건 검사 테스트
        Debug.Log($"이동 가능한가? {testCharacter.StateMachine.CanTransitTo(CharacterState.Moving)}");
        Debug.Log($"시전 가능한가? {testCharacter.StateMachine.CanTransitTo(CharacterState.Casting)}");

        // 플래그 기반 자동 전이 테스트
        Debug.Log("전투 상태 진입으로 자동 상태 전이 테스트...");
        testCharacter.EnterCombat();

        // 기절 상태로 강제 전이
        Debug.Log("기절 상태 적용...");
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, true);

        // 기절 해제 후 자동 복구
        Debug.Log("기절 해제...");
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, false);

        // 전투 종료
        testCharacter.ExitCombat();
        Debug.Log($"최종 상태: {testCharacter.StateMachine.Value}");
    }

    /// <summary>반응형 시스템 통합 테스트</summary>
    private void TestReactiveSystemIntegration()
    {
        Debug.Log("반응형 시스템 통합 테스트 - 연쇄 반응 확인...");

        // 체력을 1로 설정하여 위험 상황 시뮬레이션
        testCharacter.Health.Set(1);
        Debug.Log("체력을 1로 설정 - 위험 상황 시뮬레이션");

        // 치명적 데미지로 사망 처리 테스트
        Debug.Log("치명적 데미지 적용...");
        testCharacter.TakeDamage(50);

        // 사망 상태에서의 제한 확인
        Debug.Log($"사망 후 상태: {testCharacter.StateMachine.Value}");
        Debug.Log($"이동 가능? {testCharacter.Flags.GetValue(CharacterFlags.CanMove)}");
        Debug.Log($"공격 가능? {testCharacter.Flags.GetValue(CharacterFlags.CanAttack)}");

        // 부활 시뮬레이션
        Debug.Log("부활 시뮬레이션...");
        testCharacter.Health.Set(testCharacter.MaxHealth.Value);
        testCharacter.ApplyStatusEffect(CharacterFlags.IsAlive, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanMove, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanAttack, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanCast, true);

        Debug.Log($"부활 후 상태: {testCharacter.StateMachine.Value}");

        // 레벨업으로 인한 자동 스탯 증가 테스트
        Debug.Log("레벨업으로 인한 자동 스탯 증가 테스트...");
        var oldMaxHealth = testCharacter.MaxHealth.Value;
        var oldAttack = testCharacter.Attack.Value;

        testCharacter.GainExperience(500); // 대량 경험치로 여러 레벨업

        Debug.Log($"체력 증가: {oldMaxHealth} -> {testCharacter.MaxHealth.Value}");
        Debug.Log($"공격력 증가: {oldAttack} -> {testCharacter.Attack.Value}");
    }

    /// <summary>스트레스 테스트</summary>
    private void TestStressScenarios()
    {
        Debug.Log("스트레스 테스트 - 대량 데이터 처리...");

        // 대량 수정자 적용 테스트
        Debug.Log("대량 수정자 적용 테스트...");
        for (int i = 0; i < 100; i++)
        {
            var key = new ModifierKey((TestEffectId)(i % 10 + 1));
            testCharacter.Attack.SetModifier(ModifierType.OriginAdd, key, i % 10);
        }
        Debug.Log($"100개 수정자 적용 후 공격력: {testCharacter.Attack.Value}");

        // 수정자 대량 제거
        Debug.Log("수정자 대량 제거 테스트...");
        for (int i = 0; i < 100; i++)
        {
            var key = new ModifierKey((TestEffectId)(i % 10 + 1));
            testCharacter.Attack.RemoveModifier(ModifierType.OriginAdd, key);
        }
        Debug.Log($"수정자 제거 후 공격력: {testCharacter.Attack.Value}");

        // 빠른 상태 변화 테스트
        Debug.Log("빠른 상태 변화 테스트...");
        for (int i = 0; i < 20; i++)
        {
            testCharacter.ApplyStatusEffect(CharacterFlags.IsInCombat, i % 2 == 0);
            testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, i % 3 == 0);
        }

        // 메모리 누수 체크를 위한 반복 생성/삭제
        Debug.Log("메모리 누수 테스트 - 모델 생성/해제 반복...");
        for (int i = 0; i < 10; i++)
        {
            var tempCharacter = new CharacterModel($"임시캐릭터{i}");
            tempCharacter.GainExperience(100);
            tempCharacter.TakeDamage(50);
            tempCharacter.Unload(); // 명시적 정리
        }

        Debug.Log("스트레스 테스트 완료!");
    }

    #region 수동 테스트 메서드들 (인스펙터에서 호출 가능)

    [ContextMenu("기본 상태 출력")]
    public void PrintCurrentStatus()
    {
        if (testCharacter != null)
        {
            testCharacter.PrintStatus();
        }
    }

    [ContextMenu("데미지 받기 (30)")]
    public void TakeDamage30()
    {
        testCharacter?.TakeDamage(30);
    }

    [ContextMenu("체력 회복 (50)")]
    public void Heal50()
    {
        testCharacter?.Heal(50);
    }

    [ContextMenu("경험치 획득 (100)")]
    public void GainExp100()
    {
        testCharacter?.GainExperience(100);
    }

    [ContextMenu("전투 상태 토글")]
    public void ToggleCombat()
    {
        if (testCharacter != null)
        {
            bool inCombat = testCharacter.Flags.GetValue(CharacterFlags.IsInCombat);
            if (inCombat)
                testCharacter.ExitCombat();
            else
                testCharacter.EnterCombat();
        }
    }

    [ContextMenu("독 상태 토글")]
    public void TogglePoison()
    {
        if (testCharacter != null)
        {
            bool isPoisoned = testCharacter.Flags.GetValue(CharacterFlags.IsPoisoned);
            testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, !isPoisoned);
        }
    }

    [ContextMenu("무기 효과 토글")]
    public void ToggleWeaponEffect()
    {
        if (testCharacter != null)
        {
            var weaponKey = new ModifierKey(TestEffectId.IronSword);

            // 현재 무기 효과가 있는지 확인하기 위해 임시로 적용해보기
            var originalAttack = testCharacter.Attack.Value;
            testCharacter.Attack.SetModifier(ModifierType.OriginAdd, weaponKey, 15);
            var modifiedAttack = testCharacter.Attack.Value;

            if (originalAttack == modifiedAttack)
            {
                // 이미 적용되어 있었음 - 제거
                testCharacter.Attack.RemoveModifier(ModifierType.OriginAdd, weaponKey);
                Debug.Log("무기 효과 제거됨");
            }
            else
            {
                // 새로 적용됨
                Debug.Log("무기 효과 적용됨");
            }
        }
    }

    [ContextMenu("모든 수정자 제거")]
    public void ClearAllModifiers()
    {
        if (testCharacter != null)
        {
            testCharacter.Attack.ClearAll();
            testCharacter.Defense.ClearAll();
            testCharacter.MoveSpeed.ClearAll();
            testCharacter.AttackSpeed.ClearAll();
            testCharacter.CriticalChance.ClearAll();
            testCharacter.CriticalDamage.ClearAll();
            Debug.Log("모든 수정자가 제거되었습니다.");
        }
    }

    [ContextMenu("FSM 상태 출력")]
    public void PrintFSMStatus()
    {
        if (testCharacter != null)
        {
            Debug.Log($"현재 FSM 상태: {testCharacter.StateMachine.Value}");
            Debug.Log("가능한 전이:");
            foreach (CharacterState state in System.Enum.GetValues(typeof(CharacterState)))
            {
                if (state != testCharacter.StateMachine.Value)
                {
                    bool canTransit = testCharacter.StateMachine.CanTransitTo(state);
                    Debug.Log($"  {state}: {(canTransit ? "가능" : "불가능")}");
                }
            }
        }
    }

    #endregion

    void OnDestroy()
    {
        // 모델 정리
        testCharacter?.Unload();
        Debug.Log("CharacterModelTester 정리 완료");
    }
}