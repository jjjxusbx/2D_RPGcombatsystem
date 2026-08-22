using UnityEngine;
using UnityEngine.UI;

public class 基础移动 : MonoBehaviour
{
    #region 属性
    [Header("战斗属性")]
    [Min(1)] public int 最大血量 = 10;
    public int 血量 = 10;
    public Slider hpBar;
    public Text hpText;
    [SerializeField] private float hitLockDuration = 0.25f;
    [SerializeField] private float knockbackSpeed = 8f;

    [Header("移动")]
    public float 移动速度 = 5f;

    [Header("组件引用")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Animator anim;
    //public PlayerCombatAnimation animationTrigger;
    //public Animator animSword;
    //public Animator animSlash;
    //public Camera aimCamera;
    
    private float 上下;
    private float 水平;
    private float hitLockUntil;
    private bool isDead;
    #endregion


    //private void Start()
    //{
    //    if (animationTrigger == null)
    //    {
    //        animationTrigger = GetComponent<PlayerCombatAnimation>();
    //    }

    //        aimCamera = Camera.main;
    //}
    private void Awake()
    {
        // 自动获取缺失的组件引用
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        anim ??= GetComponent<Animator>();
        血量 = Mathf.Clamp(血量 <= 0 ? 最大血量 : 血量, 0, 最大血量);
        RefreshHpView();
        //animationTrigger ??= GetComponent<PlayerCombatAnimation>();
        //animSword ??= GetComponent<Animator>(); // 可能挂载在子对象，请自行调整
        //animSlash ??= GetComponent<Animator>();
        //aimCamera ??= Camera.main;
    }

    private void Update()
    {
        if (isDead)
        {
            StopMoveAnimation();
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

        上下 = Input.GetAxis("Vertical");
        水平 = Input.GetAxis("Horizontal");

        Vector2 moveInput = new Vector2(水平, 上下);
        #region 移动、动画处理、翻转精灵
        if (rb != null)
        {
            rb.linearVelocity = moveInput * 移动速度;
        }
        /* 动画状态*/
        if (anim != null)
        {
            anim.SetBool("IsRun", moveInput.sqrMagnitude > 0.001f);
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
        if (isDead || damage <= 0f)
        {
            return;
        }

        血量 = Mathf.Max(0, 血量 - Mathf.RoundToInt(damage));
        RefreshHpView();

        CancelInvoke(nameof(GetHitAnimEnd));
        if (anim != null)
        {
            anim.SetBool("IsGetHit", true);
        }

        ApplyKnockback(owner);

        if (血量 <= 0)
        {
            Die();
            return;
        }

        Invoke(nameof(GetHitAnimEnd), 0.4f);
    }

    public void GetHitAnimEnd()
    {
        if (anim != null)
        {
            anim.SetBool("IsGetHit", false);
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
        rb.linearVelocity = direction * knockbackSpeed;
    }

    private void Die()
    {
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

    private void StopMoveAnimation()
    {
        if (anim != null)
        {
            anim.SetBool("IsRun", false);
        }
    }
}
