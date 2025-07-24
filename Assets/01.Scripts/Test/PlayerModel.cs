using UnityEngine;
using Akasha;
using Akasha.State;
using Akasha.Modifier;

public enum PlayerState
{
    Idle,
    Moving,
    Combat,
    Dead
}

public enum PlayerFlag
{
    IsGrounded,
    CanAttack,
    IsInvulnerable,
    HasWeapon
}

public class PlayerModel : BaseModel
{
    public RxMod<int> Health { get; private set; }
    public RxMod<int> Attack { get; private set; }
    public RxMod<float> MoveSpeed { get; private set; }
    public RxVar<int> Level { get; private set; }
    public RxVar<string> PlayerName { get; private set; }

    public FSM<PlayerState> StateMachine { get; private set; }
    public RxStateFlagSet<PlayerFlag> Flags { get; private set; }

    public PlayerModel()
    {
        Health = new RxMod<int>(100, "Health", this);
        Attack = new RxMod<int>(10, "Attack", this);
        MoveSpeed = new RxMod<float>(5f, "MoveSpeed", this);
        Level = new RxVar<int>(1, this);
        PlayerName = new RxVar<string>("TestPlayer", this);

        StateMachine = new FSM<PlayerState>(PlayerState.Idle, this)
            .AddTransitionRule(PlayerState.Dead, from => Health.Value <= 0)
            .AddTransitionRule(PlayerState.Combat, from => from != PlayerState.Dead)
            .AddTransitionRule(PlayerState.Moving, from => from != PlayerState.Dead && from != PlayerState.Combat)
            .OnEnter(PlayerState.Dead, () => Debug.Log("Player died!"))
            .OnExit(PlayerState.Combat, () => Debug.Log("Exiting combat"))
            .WithDebug("[PlayerFSM]");

        Flags = new RxStateFlagSet<PlayerFlag>(this)
            .WithDebug("[PlayerFlags]");

        Flags.SetValue(PlayerFlag.IsGrounded, true);
        Flags.SetValue(PlayerFlag.CanAttack, true);

        Health.AddListener(hp => {
            if (hp <= 0)
            {
                StateMachine.Request(PlayerState.Dead);
            }
        });
    }

    public void TakeDamage(int damage)
    {
        var currentHealth = Health.Value;
        Health.Set(Mathf.Max(0, currentHealth - damage));
        Debug.Log($"Took {damage} damage. Health: {Health.Value}");
    }

    public void Heal(int amount)
    {
        var currentHealth = Health.Value;
        Health.Set(currentHealth + amount);
        Debug.Log($"Healed {amount}. Health: {Health.Value}");
    }
}