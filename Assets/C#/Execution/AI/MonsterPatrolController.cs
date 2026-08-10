using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(NavMeshAgent))]
public class MonsterPatrolController : MonoBehaviour
{
    private enum MonsterState
    {
        Idle,
        Patrol,
        Chase
    }

    [Header("组件")]
    [SerializeField] private EnemyBase enemy;
    [SerializeField] private MonsterPatrolPath patrolPath;
    [SerializeField] private Transform target;

    [Header("移动")]
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 3.2f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float loseTargetDistance = 7f;
    [SerializeField] private float arrivalDistance = 0.25f;
    [SerializeField] private float idleTime = 1f;
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private LayerMask playerMask;

    private Rigidbody2D body;
    private NavMeshAgent agent;
    private Animator animator;
    private MonsterState state;
    private int patrolIndex;
    private int patrolDirection = 1;
    private float waitUntil;
    private float nextRepathTime;
    private Vector2 lastPosition;
    private bool wasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();
        enemy ??= GetComponent<EnemyBase>();
        animator = GetComponent<Animator>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = arrivalDistance;
        lastPosition = body.position;
    }

    private void Start()
    {
        // 2D 适配：把 Agent 对齐到怪物当前 2D 位置（X 映射 X，2D 的 Y 映射到 NavMesh 的 Z）
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(ToNavPosition(body.position));
        }

        patrolIndex = patrolPath != null ? patrolPath.GetNearestPointIndex(transform.position) : -1;
        ChangeState(patrolIndex >= 0 ? MonsterState.Patrol : MonsterState.Idle);
    }

    private void Update()
    {
        if (enemy != null && enemy.IsGetHit)
        {
            PauseNavigation();
            SetRun(false);
            return;
        }

        DetectPlayer();

        switch (state)
        {
            case MonsterState.Idle:
                TickIdle();
                break;
            case MonsterState.Patrol:
                TickPatrol();
                break;
            case MonsterState.Chase:
                TickChase();
                break;
        }

        SyncAnimator();
    }

    private void FixedUpdate()
    {
        if (enemy != null && enemy.IsGetHit)
        {
            // 受击中：完全暂停导航（击退由 EnemyBase 物理驱动）
            if (!wasHit)
            {
                PauseNavigation();
                wasHit = true;
            }
            lastPosition = body.position;
            return;
        }

        if (wasHit)
        {
            // 受击结束：把 Agent 对齐到击退后的 2D 位置，再恢复寻路
            wasHit = false;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(ToNavPosition(body.position));
            }
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            // 2D 适配：NavMesh 在 XZ 平面（Y=0），映射回 2D XY 平面
            Vector3 next = agent.nextPosition;
            body.MovePosition(new Vector2(next.x, next.z));
        }

        lastPosition = body.position;
    }

    private void TickIdle()
    {
        PauseNavigation();
        if (patrolPath != null && patrolPath.Count > 0 && Time.time >= waitUntil)
        {
            patrolIndex = patrolPath.GetNearestPointIndex(transform.position);
            ChangeState(patrolIndex >= 0 ? MonsterState.Patrol : MonsterState.Idle);
        }
    }

    private void TickPatrol()
    {
        Transform point = patrolPath != null ? patrolPath.GetPoint(patrolIndex) : null;
        if (point == null)
        {
            ChangeState(MonsterState.Idle);
            return;
        }

        MoveTo(point.position, patrolSpeed);
        if (Vector2.Distance(transform.position, point.position) <= arrivalDistance)
        {
            AdvancePatrolPoint();
            waitUntil = Time.time + idleTime;
            ChangeState(MonsterState.Idle);
        }
    }

    private void TickChase()
    {
        if (target == null || Vector2.Distance(transform.position, target.position) > loseTargetDistance)
        {
            ResumePatrolFromNearestPoint();
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= attackRange)
        {
            PauseNavigation();
            enemy?.CreatATKTrigger1();
            return;
        }

        MoveTo(target.position, chaseSpeed);
    }

    private void DetectPlayer()
    {
        if (target != null && Vector2.Distance(transform.position, target.position) <= loseTargetDistance)
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerMask);
        if (hit == null || !hit.CompareTag("Player"))
        {
            return;
        }

        target = hit.transform;
        enemy.player = hit.GetComponent<基础移动>();
        enemy.playerInRang = true;
        ChangeState(MonsterState.Chase);
    }

    private void MoveTo(Vector3 destination, float speed)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            MoveDirect(destination, speed);
            return;
        }

        agent.isStopped = false;
        agent.speed = speed;
        if (Time.time >= nextRepathTime)
        {
            // 2D 适配：世界 (x, y) → NavMesh (x, 0, z=y)
            agent.SetDestination(ToNavPosition(destination));
            nextRepathTime = Time.time + repathInterval;
        }
    }

    private static Vector3 ToNavPosition(Vector3 worldPos)
    {
        return new Vector3(worldPos.x, 0f, worldPos.y);
    }

    private void MoveDirect(Vector3 destination, float speed)
    {
        Vector2 direction = ((Vector2)destination - body.position).normalized;
        body.linearVelocity = direction * speed;
    }

    private void PauseNavigation()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (body != null && (enemy == null || !enemy.IsGetHit))
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void ResumePatrolFromNearestPoint()
    {
        target = null;
        if (patrolPath == null || patrolPath.Count == 0)
        {
            ChangeState(MonsterState.Idle);
            return;
        }

        patrolIndex = patrolPath.GetNearestPointIndex(transform.position);
        ChangeState(patrolIndex >= 0 ? MonsterState.Patrol : MonsterState.Idle);
    }

    private void AdvancePatrolPoint()
    {
        if (patrolPath == null || patrolPath.Count == 0)
        {
            patrolIndex = -1;
            return;
        }

        if (patrolPath.Loop)
        {
            patrolIndex++;
            return;
        }

        patrolIndex += patrolDirection;
        if (patrolIndex >= patrolPath.Count)
        {
            patrolDirection = -1;
            patrolIndex = Mathf.Max(0, patrolPath.Count - 2);
        }
        else if (patrolIndex < 0)
        {
            patrolDirection = 1;
            patrolIndex = Mathf.Min(1, patrolPath.Count - 1);
        }
    }

    private void ChangeState(MonsterState next)
    {
        state = next;
        nextRepathTime = 0f;
    }

    private void SyncAnimator()
    {
        Vector2 moved = body.position - lastPosition;
        SetRun(moved.sqrMagnitude > 0.0001f || body.linearVelocity.sqrMagnitude > 0.0001f);
    }

    private void SetRun(bool isRun)
    {
        if (animator != null && animator.HasParameter("IsRun"))
        {
            animator.SetBool("IsRun", isRun);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetDistance);
    }
}

/// <summary>Animator 参数存在性判断扩展。</summary>
public static class AnimatorParameterExtensions
{
    public static bool HasParameter(this Animator animator, string parameterName)
    {
        if (animator == null || animator.parameters == null)
        {
            return false;
        }

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            if (animator.parameters[i].name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
