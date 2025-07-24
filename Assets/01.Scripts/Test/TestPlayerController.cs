using UnityEngine;
using Akasha;
using Akasha.Modifier;

public class TestPlayerController : EMController<PlayerEntity, PlayerModel>
{
    protected override void AtAwake()
    {
        Debug.Log("[TestPlayerController] Awake called");
    }

    protected override void AtStart()
    {
        Debug.Log("[TestPlayerController] Start called");
    }

    protected override void SetModel()
    {
        Debug.Log("[TestPlayerController] SetModel called - Model setup by Entity");
    }

    protected override void AtInit()
    {
        Debug.Log("[TestPlayerController] Init called");
        TestModifierSystem();
        TestEffectSystem();
    }

    protected override void AtLoad()
    {
        Debug.Log("[TestPlayerController] Load called - Data loaded");
    }

    protected override void AtReadyModel()
    {
        Debug.Log("[TestPlayerController] ReadyModel called - Ready to start gameplay");
    }

    protected override void AtSave()
    {
        Debug.Log("[TestPlayerController] Save called - Saving data");
    }

    protected override void AtDeinit()
    {
        Debug.Log("[TestPlayerController] Deinit called");
    }

    protected override void AtDestroy()
    {
        Debug.Log("[TestPlayerController] Destroy called");
    }

    private void TestModifierSystem()
    {
        Debug.Log("=== Testing Modifier System ===");

        Model.Health.AddListener(health => Debug.Log($"Health changed: {health}"));
        Model.Attack.AddListener(attack => Debug.Log($"Attack changed: {attack}"));

        Debug.Log($"Initial Health: {Model.Health.Value}");
        Debug.Log($"Initial Attack: {Model.Attack.Value}");

        Model.Health.SetModifier("TestBuff", ModifierType.OriginAdd, 50);
        Model.Attack.SetModifier("TestBuff", ModifierType.Multiplier, 1.5f);

        Model.Health.SetModifier("TestDebuff", ModifierType.FinalAdd, -20);

        UnityTimer.ScheduleOnce(3f, () => {
            Debug.Log("Removing test modifiers...");
            Model.Health.RemoveModifier("TestBuff");
            Model.Attack.RemoveModifier("TestBuff");
            Model.Health.RemoveModifier("TestDebuff");
        });
    }

    private void TestEffectSystem()
    {
        Debug.Log("=== Testing Effect System ===");

        var buffEffect = EffectBuilder.DefineModifier(TestEffectId.StrengthBuff, EffectApplyMode.Timed, 5f)
            .Add<int>("Attack", ModifierType.Multiplier, 2f)
            .Add<float>("MoveSpeed", ModifierType.AddMultiplier, 0.3f)
            .Build();

        GameManager.Effect.Register(buffEffect);

        UnityTimer.ScheduleOnce(1f, () => {
            Debug.Log("Applying strength buff...");
            GameManager.Effect.ApplyEffect(TestEffectId.StrengthBuff, this);
        });
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestStateChange();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Model.TakeDamage(10);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Model.Heal(20);
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            Load();
        }
    }

    private void TestStateChange()
    {
        var currentState = Model.StateMachine.Value;
        Debug.Log($"Current State: {currentState}");

        switch (currentState)
        {
            case PlayerState.Idle:
                Model.StateMachine.Request(PlayerState.Moving);
                break;
            case PlayerState.Moving:
                Model.StateMachine.Request(PlayerState.Combat);
                break;
            case PlayerState.Combat:
                Model.StateMachine.Request(PlayerState.Idle);
                break;
        }
    }
}

public enum TestEffectId
{
    StrengthBuff,
    SpeedBoost,
    Poison,
    Shield
}