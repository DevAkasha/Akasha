using System.Collections;
using UnityEngine;

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

        testCharacter = new CharacterModel("테스트히어로");

        testCharacter.PrintStatus();

        if (autoRunTests)
        {
            StartCoroutine(RunAutomatedTests());
        }
    }

    private IEnumerator RunAutomatedTests()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("\n--- 1. RxVar 기능 테스트 ---");
        TestRxVarFunctionality();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n--- 2. RxMod 기능 테스트 ---");
        TestRxModFunctionality();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n--- 3. RxStateFlagSet 기능 테스트 ---");
        TestRxStateFlagSetFunctionality();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n--- 4. FSM 기능 테스트 ---");
        TestFSMFunctionality();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n--- 5. 반응형 시스템 통합 테스트 ---");
        TestReactiveSystemIntegration();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n--- 6. 스트레스 테스트 ---");
        TestStressScenarios();
        yield return new WaitForSeconds(testInterval);

        Debug.Log("\n=== 1단계 테스트 완료 ===");
        testCharacter.PrintStatus();
    }

    private void TestRxVarFunctionality()
    {
        Debug.Log("RxVar 값 변경 및 알림 테스트...");

        testCharacter.Name.Set("파워히어로");

        testCharacter.GainGold(100);
        testCharacter.GainGold(-30);

        Debug.Log("경험치 획득으로 자동 레벨업 테스트...");
        testCharacter.GainExperience(80);
        testCharacter.GainExperience(50);
        testCharacter.GainExperience(200);
    }

    private void TestRxModFunctionality()
    {
        Debug.Log("RxMod 수정자 시스템 테스트...");

        Debug.Log($"기본 공격력: {testCharacter.Attack.Value}");

        var weaponKey = ModifierKey.Create(TestEffectId.IronSword);
        testCharacter.Attack.SetModifier(weaponKey, ModifierType.OriginAdd, 15);
        Debug.Log($"무기 장착 후 공격력: {testCharacter.Attack.Value}");

        var buffKey = ModifierKey.Create(TestEffectId.StrengthBuff);
        testCharacter.Attack.SetModifier(buffKey, ModifierType.Multiplier, 2);
        Debug.Log($"버프 적용 후 공격력: {testCharacter.Attack.Value}");

        var bonusKey = ModifierKey.Create(TestEffectId.CombatBonus);
        testCharacter.Attack.SetModifier(bonusKey, ModifierType.FinalAdd, 10);
        Debug.Log($"최종 보너스 후 공격력: {testCharacter.Attack.Value}");

        testCharacter.Attack.RemoveModifier(buffKey, 0);
        Debug.Log($"버프 해제 후 공격력: {testCharacter.Attack.Value}");

        Debug.Log($"기본 이동속도: {testCharacter.MoveSpeed.Value}");
        var bootsKey = ModifierKey.Create(TestEffectId.LeatherBoots);
        testCharacter.MoveSpeed.SetModifier(bootsKey, ModifierType.AddMultiplier, 0.3f);
        Debug.Log($"부츠 착용 후 이동속도: {testCharacter.MoveSpeed.Value}");
    }

    private void TestRxStateFlagSetFunctionality()
    {
        Debug.Log("RxStateFlagSet 상태 플래그 테스트...");

        Debug.Log("현재 활성 플래그들:");
        foreach (var flag in testCharacter.Flags.ActiveFlags())
        {
            Debug.Log($"  - {flag}");
        }

        testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, true);

        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, true);

        if (testCharacter.Flags.AnyActive(CharacterFlags.IsPoisoned, CharacterFlags.IsStunned))
        {
            Debug.Log("캐릭터가 상태이상에 걸려있습니다!");
        }

        testCharacter.ApplyStatusEffect(CharacterFlags.IsInvincible, true);
        Debug.Log("무적 상태 활성화!");

        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, false);
        testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, false);
    }

    private void TestFSMFunctionality()
    {
        Debug.Log("FSM 상태 머신 테스트...");

        Debug.Log($"현재 상태: {testCharacter.StateMachine.Value}");

        Debug.Log("공격 상태로 전이 시도...");
        testCharacter.StateMachine.Request(CharacterState.Attacking);
        Debug.Log($"전이 후 상태: {testCharacter.StateMachine.Value}");

        Debug.Log($"이동 가능한가? {testCharacter.StateMachine.CanTransitTo(CharacterState.Moving)}");
        Debug.Log($"시전 가능한가? {testCharacter.StateMachine.CanTransitTo(CharacterState.Casting)}");

        Debug.Log("전투 상태 진입으로 자동 상태 전이 테스트...");
        testCharacter.EnterCombat();

        Debug.Log("기절 상태 적용...");
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, true);

        Debug.Log("기절 해제...");
        testCharacter.ApplyStatusEffect(CharacterFlags.IsStunned, false);

        testCharacter.ExitCombat();
        Debug.Log($"최종 상태: {testCharacter.StateMachine.Value}");
    }

    private void TestReactiveSystemIntegration()
    {
        Debug.Log("반응형 시스템 통합 테스트 - 연쇄 반응 확인...");

        testCharacter.Health.Set(1);
        Debug.Log("체력을 1로 설정 - 위험 상황 시뮬레이션");

        Debug.Log("치명적 데미지 적용...");
        testCharacter.TakeDamage(50);

        Debug.Log($"사망 후 상태: {testCharacter.StateMachine.Value}");
        Debug.Log($"이동 가능? {testCharacter.Flags.GetValue(CharacterFlags.CanMove)}");
        Debug.Log($"공격 가능? {testCharacter.Flags.GetValue(CharacterFlags.CanAttack)}");

        Debug.Log("부활 시뮬레이션...");
        testCharacter.Health.Set(testCharacter.MaxHealth.Value);
        testCharacter.ApplyStatusEffect(CharacterFlags.IsAlive, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanMove, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanAttack, true);
        testCharacter.ApplyStatusEffect(CharacterFlags.CanCast, true);

        Debug.Log($"부활 후 상태: {testCharacter.StateMachine.Value}");

        Debug.Log("레벨업으로 인한 자동 스탯 증가 테스트...");
        var oldMaxHealth = testCharacter.MaxHealth.Value;
        var oldAttack = testCharacter.Attack.Value;

        testCharacter.GainExperience(500);

        Debug.Log($"체력 증가: {oldMaxHealth} -> {testCharacter.MaxHealth.Value}");
        Debug.Log($"공격력 증가: {oldAttack} -> {testCharacter.Attack.Value}");
    }

    private void TestStressScenarios()
    {
        Debug.Log("스트레스 테스트 - 대량 데이터 처리...");

        Debug.Log("대량 수정자 적용 테스트...");
        for (int i = 0; i < 100; i++)
        {
            var key = ModifierKey.Create($"StressTest_{i}");
            testCharacter.Attack.SetModifier(key, ModifierType.OriginAdd, i % 10);
        }
        Debug.Log($"100개 수정자 적용 후 공격력: {testCharacter.Attack.Value}");

        Debug.Log("수정자 대량 제거 테스트...");
        testCharacter.Attack.ClearAll();
        Debug.Log($"수정자 제거 후 공격력: {testCharacter.Attack.Value}");

        Debug.Log("빠른 상태 변화 테스트...");
        for (int i = 0; i < 20; i++)
        {
            testCharacter.ApplyStatusEffect(CharacterFlags.IsInCombat, i % 2 == 0);
            testCharacter.ApplyStatusEffect(CharacterFlags.IsPoisoned, i % 3 == 0);
        }

        Debug.Log("메모리 누수 테스트 - 모델 생성/해제 반복...");
        for (int i = 0; i < 10; i++)
        {
            var tempCharacter = new CharacterModel($"임시캐릭터{i}");
            tempCharacter.GainExperience(100);
            tempCharacter.TakeDamage(50);
            tempCharacter.Unload();
        }

        Debug.Log("스트레스 테스트 완료!");
    }

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
            var weaponKey = ModifierKey.Create(TestEffectId.IronSword);

            if (testCharacter.Attack.HasModifier(weaponKey))
            {
                testCharacter.Attack.RemoveModifier(weaponKey, 0);
                Debug.Log("무기 효과 제거됨");
            }
            else
            {
                testCharacter.Attack.SetModifier(weaponKey, ModifierType.OriginAdd, 15);
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

    [ContextMenu("수정자 상세 정보 출력")]
    public void PrintModifierDetails()
    {
        if (testCharacter != null)
        {
            Debug.Log("=== 공격력 수정자 상세 정보 ===");
            Debug.Log($"현재 값: {testCharacter.Attack.Value}");
            Debug.Log($"스택 수 체크 예시:");

            var weaponKey = ModifierKey.Create(TestEffectId.IronSword);
            var buffKey = ModifierKey.Create(TestEffectId.StrengthBuff);
            var bonusKey = ModifierKey.Create(TestEffectId.CombatBonus);

            Debug.Log($"  무기 효과 ({weaponKey}): {(testCharacter.Attack.HasModifier(weaponKey) ? "적용됨" : "없음")} - 스택수: {testCharacter.Attack.GetStackCount(weaponKey)}");
            Debug.Log($"  버프 효과 ({buffKey}): {(testCharacter.Attack.HasModifier(buffKey) ? "적용됨" : "없음")} - 스택수: {testCharacter.Attack.GetStackCount(buffKey)}");
            Debug.Log($"  보너스 효과 ({bonusKey}): {(testCharacter.Attack.HasModifier(bonusKey) ? "적용됨" : "없음")} - 스택수: {testCharacter.Attack.GetStackCount(bonusKey)}");

            Debug.Log("=== 이동속도 수정자 상세 정보 ===");
            Debug.Log($"현재 값: {testCharacter.MoveSpeed.Value}");
            Debug.Log($"스택 수 체크 예시:");

            var bootsKey = ModifierKey.Create(TestEffectId.LeatherBoots);
            Debug.Log($"  부츠 효과 ({bootsKey}): {(testCharacter.MoveSpeed.HasModifier(bootsKey) ? "적용됨" : "없음")} - 스택수: {testCharacter.MoveSpeed.GetStackCount(bootsKey)}");
        }
    }

    [ContextMenu("ModifierManager 통계")]
    public void PrintModifierManagerStats()
    {
        var modifierManager = GameManager.Instance?.GetManager<ModifierManager>();
        if (modifierManager != null)
        {
            var stats = modifierManager.GetInstanceStatistics();
            Debug.Log($"=== ModifierManager 통계 ===");
            Debug.Log($"총 인스턴스: {modifierManager.TotalContainers}");
            Debug.Log($"총 수정자: {modifierManager.TotalModifiers}");
            Debug.Log($"등록된 키: {ModifierKey.GetRegisteredCount()}");

            foreach (var kvp in stats)
            {
                Debug.Log($"  인스턴스 {kvp.Key}: {kvp.Value}개 수정자");
            }
        }
        else
        {
            Debug.LogWarning("ModifierManager를 찾을 수 없습니다.");
        }
    }

    void OnDestroy()
    {
        testCharacter?.Unload();
        Debug.Log("CharacterModelTester 정리 완료");
    }
}