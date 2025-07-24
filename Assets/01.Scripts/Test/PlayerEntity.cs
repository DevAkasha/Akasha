using UnityEngine;
using Akasha;

public class PlayerEntity : BaseEntity<PlayerModel>
{
    private MovementPart movementPart;
    private CombatPart combatPart;

    protected override void SetupModel()
    {
        Model = new PlayerModel();
        Debug.Log("[PlayerEntity] Model setup complete");
    }

    protected override void AtAwake()
    {
        movementPart = GetPart<MovementPart>();
        combatPart = GetPart<CombatPart>();

        Debug.Log($"[PlayerEntity] Parts found - Movement: {movementPart != null}, Combat: {combatPart != null}");
    }

    protected override void AtStart()
    {
        Debug.Log("[PlayerEntity] Entity started");
    }

    protected override void AtInit()
    {
        Debug.Log("[PlayerEntity] Entity initialized");
    }
}