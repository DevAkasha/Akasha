using UnityEngine;
using Akasha;
using Akasha.Modifier;

public class CombatPart : BasePart<PlayerEntity, PlayerModel>
{
    private float lastAttackTime;
    private float attackCooldown = 1f;

    protected override void AtAwake()
    {
        Debug.Log("[CombatPart] Combat system initialized");
    }

    protected override void AtStart()
    {
        Model.Attack.AddListener(attack => {
            Debug.Log($"[CombatPart] Attack power changed to: {attack}");
        });

        Model.Flags.AddListener(PlayerFlag.CanAttack, canAttack => {
            Debug.Log($"[CombatPart] Can attack: {canAttack}");
        });
    }

    void Update()
    {
        if (Model.StateMachine.Value == PlayerState.Dead) return;

        if (Input.GetMouseButtonDown(0) && CanAttack())
        {
            PerformAttack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ApplyWeaponBuff();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ApplyPoisonDebuff();
        }
    }

    private bool CanAttack()
    {
        return Model.Flags.GetValue(PlayerFlag.CanAttack) &&
               Time.time - lastAttackTime >= attackCooldown;
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        Model.StateMachine.Request(PlayerState.Combat);

        Debug.Log($"[CombatPart] Attacking with power: {Model.Attack.Value}");

        Model.Flags.SetValue(PlayerFlag.CanAttack, false);

        UnityTimer.ScheduleOnce(attackCooldown, () => {
            Model.Flags.SetValue(PlayerFlag.CanAttack, true);
            if (Model.StateMachine.Value == PlayerState.Combat)
            {
                Model.StateMachine.Request(PlayerState.Idle);
            }
        });
    }

    private void ApplyWeaponBuff()
    {
        Debug.Log("[CombatPart] Applying weapon buff");

        Model.Attack.SetModifier("WeaponBuff", ModifierType.OriginAdd, 15);
        Model.Attack.SetModifier("WeaponMultiplier", ModifierType.Multiplier, 1.2f);

        UnityTimer.ScheduleOnce(10f, () => {
            Debug.Log("[CombatPart] Weapon buff expired");
            Model.Attack.RemoveModifier("WeaponBuff");
            Model.Attack.RemoveModifier("WeaponMultiplier");
        });
    }

    private void ApplyPoisonDebuff()
    {
        Debug.Log("[CombatPart] Applying poison debuff");

        var poisonEffect = EffectBuilder.DefineModifier(TestEffectId.Poison, EffectApplyMode.Timed, 5f)
            .Add<int>("Health", ModifierType.OriginAdd, -2)
            .Stackable()
            .Build();

        GameManager.Effect.Register(poisonEffect);
        GameManager.Effect.ApplyEffect(TestEffectId.Poison, Entity);
    }
}