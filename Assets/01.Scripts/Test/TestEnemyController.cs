using UnityEngine;
using Akasha;
using Akasha.State;

public class TestEnemyController : BaseController
{
    private BehaviorTree behaviorTree;
    private PlayerModel targetPlayer;
    private Transform playerTransform;

    [Header("Enemy Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float moveSpeed = 3f;

    private float distanceToPlayer;

    protected override void AtInit()
    {
        var playerController = GameManager.Instance.GetModelController<TestPlayerController>();
        if (playerController != null)
        {
            targetPlayer = playerController.Model;
            playerTransform = playerController.transform;
        }

        SetupBehaviorTree();

        GetComponent<Renderer>().material.color = Color.red;
    }

    private void SetupBehaviorTree()
    {
        behaviorTree = new BehaviorTree(
            new Selector(
                new Sequence(
                    new Condition(() => targetPlayer != null && targetPlayer.Health.Value <= 0),
                    new BehaviorAction(() => {
                        Debug.Log("[Enemy] Player is dead, celebrating!");
                        return NodeStatus.Success;
                    })
                ),

                new Sequence(
                    new Condition(() => IsPlayerInRange(detectionRange)),
                    new Selector(
                        new Sequence(
                            new Condition(() => IsPlayerInRange(attackRange)),
                            new BehaviorAction(AttackPlayer)
                        ),

                        new BehaviorAction(ChasePlayer)
                    )
                ),

                new BehaviorAction(Patrol)
            )
        );
    }

    void Update()
    {
        if (behaviorTree != null)
        {
            behaviorTree.Update();
        }

        UpdateDistanceToPlayer();
    }

    private void UpdateDistanceToPlayer()
    {
        if (playerTransform != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        }
    }

    private bool IsPlayerInRange(float range)
    {
        return playerTransform != null && distanceToPlayer <= range;
    }

    private NodeStatus AttackPlayer()
    {
        Debug.Log($"[Enemy] Attacking player! Distance: {distanceToPlayer:F1}");

        if (targetPlayer != null)
        {
            targetPlayer.TakeDamage(5);
        }

        return NodeStatus.Success;
    }

    private NodeStatus ChasePlayer()
    {
        if (playerTransform == null) return NodeStatus.Failure;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        return NodeStatus.Running;
    }

    private NodeStatus Patrol()
    {
        transform.Rotate(0, 30 * Time.deltaTime, 0);
        transform.position += transform.forward * moveSpeed * 0.5f * Time.deltaTime;

        return NodeStatus.Running;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}