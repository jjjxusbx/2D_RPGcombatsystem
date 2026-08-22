using UnityEngine;
using UnityEngine.UI;

public enum WeaponType { Sword, Bow, Staff }
public class 基础移动 : Character
{
    #region 属性
    [Header("战斗属性（初始最大血量，仅作场景配置；运行期血量由 ChaState 统一管理）")]
    [Min(1)] public int HP = 10;
    
    [SerializeField] private float hitLockDuration = 0.25f;
    [SerializeField] private float knockbackSpeed = 8f;

    [Header("移动")]
    public float speed = 5f;

    [Header("战斗系统")]
    [Tooltip("勾选后由 FSM 接管移动与攻击，本脚本仅保留受击/死亡/回血等逻辑")]
    public bool useCombatStateMachine;

    //[Header("跳跃")]
    //public float jumpForce = 10f;
    //public float gravityScale = 1f;
    //public LayerMask groundLayers = ~0;
    //public float groundCheckDistance = 0.3f;

    [Header("组件引用")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Animator anim;//玩家动画
    public Slider hpBar;
    public Text hpText;
    
    public Animator animSword;
    public Animator animSlash;
    //public Camera aimCamera;
    //public PlayerCombatAnimation animationTrigger;
    public GameObject swordTrigger;
    public Transform swordTriPos;

    private float 水平;
    private float hitLockUntil;
    private bool isDead;
    private Collider2D col;
    private ChaState state;
    #endregion


    private void Start()
    {
        //HPNow = HP;
    //    if (animationTrigger == null)
    //    {
    //        animationTrigger = GetComponent<PlayerCombatAnimation>();
    //    }

    //        aimCamera = Camera.main;
    }
    protected override void Awake()
    {
        base.Awake();
        // 自动获取缺失的组件引用
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        anim ??= GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        // 统一伤害管线：血量/受击/死亡由 ChaState 接管
        state = GetComponent<ChaState>();
        if (state == null)
        {
            state = gameObject.AddComponent<ChaState>();
        }
        state.maxHealth.SetBase(HP);
        // 运行期当前血量由 ChaState 统一计算：初始为满血，上限为 maxHealth
        state.currentHealth = state.maxHealth.GetValue();
        state.onDamaged += OnStateDamaged;
        state.onDeath += OnStateDeath;

        RefreshHpView();
        // 场景中玩家 Rigidbody2D 重力为 0，启用跳跃需要补回重力（跳跃已移除，注释掉）
        //if (rb != null && Mathf.Abs(rb.gravityScale) < 0.001f)
        //{
        //    rb.gravityScale = gravityScale;
        //}
        //animationTrigger ??= GetComponent<PlayerCombatAnimation>();
        //animSword ??= GetComponent<Animator>(); // 可能挂载在子对象，请自行调整
        //animSlash ??= GetComponent<Animator>();
        //aimCamera ??= Camera.main;
    }

    protected override void OnCharacterUpdate()
    {
        if (isDead)
        {
            StopMoveAnimation();
            return;
        }

        // FSM 接管移动与攻击：勾选 useCombatStateMachine 后，本脚本不再执行 PlayerMove/PlayerAttack
        if (useCombatStateMachine)
        {
            return;
        }

        PlayerMove();
        //PlayerAim();
        PlayerAttack();
    }

    public void PlayerMove()
    {
        if (Time.time < hitLockUntil)
        {
            StopMoveAnimation();
            return;
        }

        水平 = Input.GetAxis("Horizontal");

        // 受击硬直(hitLockUntil)期间禁止起跳：与移动一致，硬直内交出控制权（跳跃已移除，注释掉）
        //if (rb != null && Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        //{
        //    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        //}

        #region 移动、动画处理、翻转精灵
        Vector2 velocity = new Vector2(水平 * speed, rb != null ? rb.linearVelocity.y : 0f);
        if (Physics != null)
        {
            Physics.Move(velocity);
        }
        else if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        /* 动画状态*/
        if (anim != null)
        {
            anim.SetBool("IsRun", Mathf.Abs(水平) > 0.001f);
            return;
        }


        if (sr == null)
        {
            return;
        }

        if (水平 > 0)
        {
            sr.flipX = false;
        }
        else if (水平 < 0)
        {
            sr.flipX = true;
        }
        #endregion
    }

    //private bool IsGrounded()
    //{
    //    if (rb == null)
    //    {
    //        return false;
    //    }

    //    Vector2 origin = col != null
    //        ? new Vector2(col.bounds.center.x, col.bounds.min.y)
    //        : rb.position + Vector2.down * 0.55f;

    //    RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
    //    if (hit.collider == null || hit.collider.isTrigger)
    //    {
    //        return false;
    //    }

    //    return hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform);
    //}
    
    /*
    private void PlayerAim()
    {
        if (animationTrigger == null || aimCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition = aimCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = transform.position.z;
        animationTrigger.AimAt(mouseWorldPosition);
    }
    */

    private void PlayerAttack()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (anim != null)
        {
            PlayerAnimationPresenter.SetTriggerIfExists(anim, "Attack");
            return;
        }

    }
    #region GetHit
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

        RefreshHpView();

        CancelInvoke(nameof(GetHitAnimEnd));
        if (anim != null)
        {
            PlayerAnimationPresenter.SetBoolIfExists(anim, "IsGetHit", true);
        }

        ApplyKnockback(info.owner);

        if (!info.isLethal)
        {
            Invoke(nameof(GetHitAnimEnd), 0.4f);
        }
    }

    private void OnStateDeath(DamageInfo info)
    {
        Die();
    }

    public void GetHitAnimEnd()
    {
        if (anim != null)
        {
            PlayerAnimationPresenter.SetBoolIfExists(anim, "IsGetHit", false);
        }
    }
    #endregion

    private void ApplyKnockback(Transform owner)
    {
        if (rb == null)
        {
            return;
        }

        Vector2 direction = owner != null
            ? ((Vector2)transform.position - (Vector2)owner.position).normalized
            : Vector2.zero;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = sr != null && sr.flipX ? Vector2.right : Vector2.left;
        }

        hitLockUntil = Time.time + hitLockDuration;
        if (Physics != null)
        {
            Physics.SetVelocity(direction * knockbackSpeed);
        }
        else
        {
            rb.linearVelocity = direction * knockbackSpeed;
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        CancelInvoke(nameof(GetHitAnimEnd));
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        StopMoveAnimation();
        Debug.Log($"[Combat] Player dead: {name}", this);
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

    private void StopMoveAnimation()
    {
        if (anim != null)
        {
            anim.SetBool("IsRun", false);
        }
    }
}
