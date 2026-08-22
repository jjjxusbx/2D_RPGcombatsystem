using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyBase : Character
{
    [Header("组件引用")]
    public Animator anim;
    public Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private float knockbackSpeed = 10f;
    [SerializeField] private float knockbackDuration = 0.5f;
    
    public GameObject ATKTrigger1;
    public Transform ATKPos1;

    [Header("战斗属性（初始最大血量，仅作场景配置；运行期血量由 ChaState 统一管理）")]
    [Min(1)] public int 最大血量 = 20;
    public Slider hpBar;
    public Text hpText;
    public int ATK = 5;
    public bool canATK1 = true;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.2f;
    public UnityEvent onDeath;

    public 基础移动 player;
    public bool playerInRang;
    public bool IsGetHit => !isDead && anim != null && anim.GetBool("IsGetHit");
    //private bool hasHitParameter;

    private bool isDead;
    private Collider2D enemyCollider;
    private ChaState state;

    protected override void Awake()
    {
        base.Awake();
        anim ??= GetComponent<Animator>();
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        // 统一伤害管线：血量/受击/死亡由 ChaState 接管
        state = GetComponent<ChaState>();
        if (state == null)
        {
            state = gameObject.AddComponent<ChaState>();
        }
        state.maxHealth.SetBase(最大血量);
        // 运行期当前血量由 ChaState 统一计算：初始为满血，上限为 maxHealth
        state.currentHealth = state.maxHealth.GetValue();
        state.onDamaged += OnStateDamaged;
        state.onDeath += OnStateDeath;

        RefreshHpView();
        //hasHitParameter = anim != null && System.Array.Exists(anim.parameters,
        //    parameter => parameter.type == AnimatorControllerParameterType.Bool && parameter.name == HitParameter);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead)
        {
            return;
        }

        if (collision.CompareTag("Player"))
        {
            playerInRang = true;
            player = collision.GetComponent<基础移动>() ?? collision.GetComponentInParent<基础移动>();
        }
    }


    protected override void OnCharacterUpdate()
    {
        if (isDead)
        {
            return;
        }

        CreatATKTrigger1();
        // 移动控制已移交 MonsterPatrolController（NavMeshAgent）。
        // 未挂载巡逻控制器的怪物保持原行为：非受击时静止。
        if (GetComponent<MonsterPatrolController>() == null && anim != null && !anim.GetBool("IsGetHit"))
        {
            Physics?.Stop();
        }
    }
    //IsGetHit
    public void TakeDamage(float damage, Transform owner)
    {
        // 转发到统一伤害管线（防御减免→扣血→onDamaged/onDeath）
        if (state != null)
        {
            state.TakeDamage(damage, owner);
        }
    }

    private void OnStateDamaged(DamageInfo info)
    {
        if (info.actualDamage <= 0f)
        {
            return;
        }

        // DOT 小额伤害只刷新血条，跳过受击动画与击退
        if (info.rawDamage <= 1f)
        {
            RefreshHpView();
            return;
        }

        RefreshHpView();

        CancelInvoke(nameof(GetHitAnimEnd));
        CancelInvoke(nameof(ResetHitFlash));

        if (anim != null)
        {
            anim.SetBool("IsGetHit", true);
        }

        if (sr != null)
        {
            sr.color = hitColor;
            Invoke(nameof(ResetHitFlash), hitFlashDuration);
        }
        //远离玩家的方向
        if (rb != null)
        {
            Vector2 dir = info.owner != null
                ? ((Vector2)transform.position - (Vector2)info.owner.position).normalized
                : -GetFacingDirection();

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.right;
            }

            if (Physics != null)
            {
                Physics.SetVelocity(dir * knockbackSpeed);
            }
            else
            {
                rb.linearVelocity = dir * knockbackSpeed;
            }
        }

        if (!info.isLethal)
        {
            Invoke(nameof(GetHitAnimEnd), knockbackDuration);
        }
    }
    public void GetHitAnimEnd()
    {
        
        if (anim != null)
        {
            anim.SetBool("IsGetHit", false);
        }
    }

    private void ResetHitFlash()
    {
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }
    
    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Player"))
    //    {
    //        playerInRang = false;
    //    }
    //}

    // 若bug 则计算我们与玩家的位
    public void CreatATKTrigger1()
    {
        if (isDead || !playerInRang || player == null)
        { 
            return;
        }
            if (canATK1 && ATKTrigger1 != null && ATKPos1 != null)
            {
                CancelInvoke(nameof(ATK1End));
                GameObject go = Instantiate(ATKTrigger1, ATKPos1.position, 
                    ATKPos1.rotation,ATKPos1);
                SlimeATKTri attackTrigger = go.GetComponent<SlimeATKTri>();
            //go.GetComponent<SlimeATKTri>().
            //        SetDamage(ATK+Random.Range(0, 5),this.transform);
                if (attackTrigger != null)
                {
                    attackTrigger.SetDamage(ATK + Random.Range(0, 5), transform);
                }

                // 攻击动画：Slime 控制器通过 Attack trigger 从 Any State 切入
                if (anim != null && anim.HasParameter("Attack"))
                {
                    anim.SetTrigger("Attack");
                }
                canATK1 = false;
                Invoke(nameof(ATK1End), 0.4f);
            }
            if (Vector2.Distance(transform.position, 
                player.transform.position) > 3f)
            {
                playerInRang = false;
            }
        
    }

    public void ATK1End()
    {
        if (!isDead)
        {
            canATK1 = true;
        }
    }
    public void ReceiveDamage(float damage, Transform owner)
    {
        TakeDamage(damage, owner);
    }

    private void OnStateDeath(DamageInfo info)
    {
        Die();
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        playerInRang = false;
        canATK1 = false;
        CancelInvoke();

        if (anim != null)
        {
            anim.SetBool("IsGetHit", false);
            anim.SetBool("IsRun", false);
        }

        if (Physics != null)
        {
            Physics.Stop();
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        MonsterPatrolController patrolController = GetComponent<MonsterPatrolController>();
        if (patrolController != null)
        {
            patrolController.enabled = false;
        }

        Behaviour moveToTarget = GetComponent("MonsterMoveToTarget") as Behaviour;
        if (moveToTarget != null)
        {
            moveToTarget.enabled = false;
        }

        onDeath?.Invoke();
        Debug.Log($"[Combat] Enemy dead: {name}", this);

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void RefreshHpView()
    {
        if (state == null)
        {
            return;
        }

        float current = state.currentHealth;
        float max = state.maxHealth.GetValue();

        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = max;
            hpBar.value = current;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }
    }

    private Vector2 GetFacingDirection()
    {
        if (sr != null)
        {
            return sr.flipX ? Vector2.left : Vector2.right;
        }

        return transform.right;
    }
}
