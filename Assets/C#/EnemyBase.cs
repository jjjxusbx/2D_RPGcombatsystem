using UnityEngine;

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

    [Header("=======================")]
    public int ATK = 5;
    public bool canATK1 = true;

    public 基础移动 player;
    public bool playerInRang;
    public bool IsGetHit => anim != null && anim.GetBool("IsGetHit");
    //private bool hasHitParameter;

    void Awake()
    {
        anim ??= GetComponent<Animator>();
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        //hasHitParameter = anim != null && System.Array.Exists(anim.parameters,
        //    parameter => parameter.type == AnimatorControllerParameterType.Bool && parameter.name == HitParameter);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRang = true;
            player = collision.GetComponent<基础移动>();
        }
    }


    void Update()
    {
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
        //Debug.Log($"[CombatHitDebug] TakeDamage enemy={name} damage={damage} owner={(owner != null ? owner.name : "null")}", this);
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
        if (!playerInRang || player == null)
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

    }
    public void ReceiveDamage(float damage, Transform owner)
    {
        Debug.Log($"[CombatHitDebug] ReceiveDamage enemy={name} damage={damage} owner={(owner != null ? owner.name : "null")}", this);
        TakeDamage(damage, owner);
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
