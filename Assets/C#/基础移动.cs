using UnityEngine;

public class 基础移动 : MonoBehaviour
{
    #region 属性
    [Header("战斗属性")]
    public int 血量 = 10;

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
        //animationTrigger ??= GetComponent<PlayerCombatAnimation>();
        //animSword ??= GetComponent<Animator>(); // 可能挂载在子对象，请自行调整
        //animSlash ??= GetComponent<Animator>();
        //aimCamera ??= Camera.main;
    }

    private void Update()
    {
        PlayerMove();
        //PlayerAim();
        PlayerAttack();
    }

    public void PlayerMove()
    {
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
        CancelInvoke(nameof(GetHitAnimEnd));
        anim.SetBool("IsGetHit", true);
        Invoke(nameof(GetHitAnimEnd), 0.4f);
    }
    public void GetHitAnimEnd()
    {
        anim.SetBool("IsGetHit", false);
    }
    #endregion



}
