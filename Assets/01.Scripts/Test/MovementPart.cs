using UnityEngine;
using Akasha;

public class MovementPart : BasePart<PlayerEntity, PlayerModel>
{
    private CharacterController characterController;
    private Vector3 velocity;

    protected override void AtAwake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        Debug.Log("[MovementPart] Awake - CharacterController ready");
    }

    protected override void AtStart()
    {
        Model.MoveSpeed.AddListener(speed => {
            Debug.Log($"[MovementPart] Movement speed changed to: {speed}");
        });

        Model.StateMachine.AddListener(state => {
            switch (state)
            {
                case PlayerState.Moving:
                    Debug.Log("[MovementPart] Started moving");
                    break;
                case PlayerState.Idle:
                    Debug.Log("[MovementPart] Stopped moving");
                    velocity = Vector3.zero;
                    break;
            }
        });
    }

    void Update()
    {
        if (Model.StateMachine.Value == PlayerState.Dead) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical);

        if (move.magnitude > 0.1f)
        {
            if (Model.StateMachine.Value == PlayerState.Idle)
            {
                Model.StateMachine.Request(PlayerState.Moving);
            }

            characterController.Move(move * Model.MoveSpeed.Value * Time.deltaTime);
        }
        else if (Model.StateMachine.Value == PlayerState.Moving)
        {
            Model.StateMachine.Request(PlayerState.Idle);
        }

        if (!characterController.isGrounded)
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
            Model.Flags.SetValue(PlayerFlag.IsGrounded, false);
        }
        else
        {
            velocity.y = 0;
            Model.Flags.SetValue(PlayerFlag.IsGrounded, true);
        }

        characterController.Move(velocity * Time.deltaTime);
    }
}