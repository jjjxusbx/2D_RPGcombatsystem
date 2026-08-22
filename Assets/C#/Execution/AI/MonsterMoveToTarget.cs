using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMoveToTarget : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private bool stopWhenHasPatrolController = true;

    private Rigidbody2D body;
    private MonsterPatrolController patrolController;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        patrolController = GetComponent<MonsterPatrolController>();
    }

    private void FixedUpdate()
    {
        if (!CanMove())
        {
            StopVelocity();
            return;
        }

        Vector2 current = body.position;
        Vector2 target = targetPoint.position;
        Vector2 delta = target - current;
        if (delta.sqrMagnitude <= stopDistance * stopDistance)
        {
            StopVelocity();
            return;
        }

        Vector2 next = Vector2.MoveTowards(current, target, moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(next);
    }

    public void SetTarget(Transform target)
    {
        targetPoint = target;
    }

    private bool CanMove()
    {
        if (body == null || targetPoint == null)
        {
            return false;
        }

        if (!stopWhenHasPatrolController)
        {
            return true;
        }

        return patrolController == null || !patrolController.enabled;
    }

    private void StopVelocity()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}