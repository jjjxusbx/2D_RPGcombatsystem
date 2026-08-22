using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyBase : MonoBehaviour
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

    [Header("战斗属性")]
    [Min(1)] public int 最大血量 = 20;
    public int 血量 = 20;
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

    void Awake()
    {
        anim ??= GetComponent<Animator>();
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        血量 = Mathf.Clamp(血量 <= 0 ? 最大血量 : 血量, 0, 最大血量);
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


    void Update()
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
            rb.linearVelocity = Vector2.zero;
        }
    }
    //IsGetHit
    public void TakeDamage(float damage, Transform owner)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        //Debug.Log($"[CombatHitDebug] TakeDamage enemy={name} damage={damage} owner={(owner != null ? owner.name : "null")}", this);
        血量 = Mathf.Max(0, 血量 - Mathf.RoundToInt(damage));
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
            Vector2 dir = owner != null
                ? ((Vector2)transform.position - (Vector2)owner.position).normalized
                : -GetFacingDirection();

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.right;
            }

            rb.linearVelocity = dir * knockbackSpeed;
        }

        if (血量 <= 0)
        {
            Die();
            return;
        }
        
        Invoke(nameof(GetHitAnimEnd), knockbackDuration);
        
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
        if (isDead)
        {
            return;
        }

        Debug.Log($"[CombatHitDebug] ReceiveDamage enemy={name} damage={damage} owner={(owner != null ? owner.name : "null")}", this);
        TakeDamage(damage, owner);
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

        if (rb != null)
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
        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = 最大血量;
            hpBar.value = 血量;
        }

        if (hpText != null)
        {
            hpText.text = $"{血量}/{最大血量}";
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
