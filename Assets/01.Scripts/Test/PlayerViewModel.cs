using System;
using UnityEngine;
using Akasha;
using Akasha.State;
using Akasha.Modifier;

public class PlayerViewModel : BaseViewModel<PlayerModel>
{
    public override Type ModelType => typeof(PlayerModel);

    public RxModIntSlot HealthSlot { get; private set; }
    public RxModIntSlot AttackSlot { get; private set; }
    public RxModFloatSlot SpeedSlot { get; private set; }
    public RxVarSlot<int> LevelSlot { get; private set; }
    public RxVarSlot<string> NameSlot { get; private set; }
    public FSMSlot<PlayerState> StateSlot { get; private set; }
    public FlagSetSlot<PlayerFlag> FlagsSlot { get; private set; }

    public PlayerViewModel()
    {
        HealthSlot = ViewSlotFactory.CreateRxModInt("Health");
        AttackSlot = ViewSlotFactory.CreateRxModInt("Attack");
        SpeedSlot = ViewSlotFactory.CreateRxModFloat("MoveSpeed");
        LevelSlot = ViewSlotFactory.CreateRxVar<int>("Level");
        NameSlot = ViewSlotFactory.CreateRxVar<string>("PlayerName");
        StateSlot = ViewSlotFactory.CreateFSM<PlayerState>("PlayerState");
        FlagsSlot = ViewSlotFactory.CreateFlagSet<PlayerFlag>("PlayerFlags");
    }

    protected override void OnTypedModelBound(PlayerModel model)
    {
        Debug.Log("[PlayerViewModel] Binding to player model");

        HealthSlot.SetOwner(owner);
        AttackSlot.SetOwner(owner);
        SpeedSlot.SetOwner(owner);
        LevelSlot.SetOwner(owner);
        NameSlot.SetOwner(owner);
        StateSlot.SetOwner(owner);
        FlagsSlot.SetOwner(owner);

        HealthSlot.BindToField(model.Health);
        AttackSlot.BindToField(model.Attack);
        SpeedSlot.BindToField(model.MoveSpeed);
        LevelSlot.BindToField(model.Level);
        NameSlot.BindToField(model.PlayerName);
        StateSlot.BindToField(model.StateMachine);
        FlagsSlot.BindToField(model.Flags);

        Debug.Log("[PlayerViewModel] Binding complete");
    }

    public override void Cleanup()
    {
        HealthSlot?.Cleanup();
        AttackSlot?.Cleanup();
        SpeedSlot?.Cleanup();
        LevelSlot?.Cleanup();
        NameSlot?.Cleanup();
        StateSlot?.Cleanup();
        FlagsSlot?.Cleanup();

        base.Cleanup();
    }
}