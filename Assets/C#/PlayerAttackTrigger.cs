using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PlayerAttackTrigger : MonoBehaviour
{
    [Header("==========攻击==========")]
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float attackActiveTime = 0.18f;
    [SerializeField] private string attackTrigger = "Attack";
    [Header("==========组件==========")]
    [SerializeField] private PlayerCombatAnimation combatAnimation;
    [SerializeField] private PlayerAnimationPresenter animationPresenter;
    [SerializeField] private Animator fallbackAnimator;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Collider2D attackCollider;
    
    private int damage = 10;
    public Transform owner;

    private float nextAttackTime;
    // 保存攻击碰撞体开关的协程引用，方便随时中断
    private Coroutine attackColliderRoutine;
    
    #region 如果设计时没拖拽赋值，就在运行时自动找组件
    private void Awake()
    {
        // 使用 null 合并运算符简化空值检查和组件获取
        attackCollider ??= GetComponent<Collider2D>();
        combatAnimation ??= GetComponent<PlayerCombatAnimation>() ?? GetComponentInParent<PlayerCombatAnimation>();
        animationPresenter ??= GetComponent<PlayerAnimationPresenter>() ?? GetComponentInParent<PlayerAnimationPresenter>();
        fallbackAnimator ??= GetComponent<Animator>() ?? GetComponentInParent<Animator>();
        aimCamera ??= Camera.main;
        owner ??= transform.root;

        SetAttackColliderActive(false);
    }
    #endregion

    private void Update()
    {
        AimAtMouse();
        //如果没点鼠标左键，或者还在冷却中，就啥也不干
        if (!Input.GetMouseButtonDown(0) || Time.time < nextAttackTime)
        {
            return;
        }
        //满足条件 发起攻击
        TriggerAttack();
    }

    public void TriggerAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        // 攻击前再瞄准一次，确保方向正确
        AimAtMouse();
        #region 高级动画组件播放攻击,没有就用普通的Animator
        if (combatAnimation != null)
            combatAnimation.PlayAttack();
        else if (animationPresenter != null)
            animationPresenter.PlayAttack(0);
        else if (fallbackAnimator != null)
        {
            fallbackAnimator.ResetTrigger(attackTrigger);
            fallbackAnimator.SetTrigger(attackTrigger);
        }
        // 开启攻击判定框
        RestartAttackCollider();
        #endregion
    }

    private void AimAtMouse()
    {
        if (aimCamera == null) return; 

        Vector3 mouseWorldPos = aimCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        if (combatAnimation != null)
            combatAnimation.AimAt(mouseWorldPos);
        else if (animationPresenter != null)
        {
            Vector2 dir = mouseWorldPos - transform.position;
            animationPresenter.UpdateAimDirection(dir);
        }
    }

    private void RestartAttackCollider()
    {
        if (attackColliderRoutine != null)
            StopCoroutine(attackColliderRoutine);

        attackColliderRoutine = StartCoroutine(AttackColliderRoutine());
    }

    private IEnumerator AttackColliderRoutine()
    {
        //砍出去，开启碰撞体开始判定伤害
        SetAttackColliderActive(true);
        // 等待一小段时间
        yield return new WaitForSeconds(attackActiveTime);
        // 收招，关闭碰撞体不再判定伤害
        SetAttackColliderActive(false);
        attackColliderRoutine = null;
    }

    private void SetAttackColliderActive(bool active)
    {
        if (attackCollider != null)
        {
            // 启用或禁用碰撞体，并确保它始终是触发器（不会产生物理推力）
            attackCollider.enabled = active;
            attackCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[CombatHitDebug] OnTriggerEnter2D attack={name} other={other.name}", this);
        bool colliderEnabled = attackCollider != null && attackCollider.enabled;
        Debug.Log($"[CombatHitDebug] AttackCollider attack={name} exists={attackCollider != null} enabled={colliderEnabled}", this);
        if (attackCollider == null || !attackCollider.enabled) return;
        EnemyBase enemy = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>();
        Debug.Log($"[CombatHitDebug] EnemyLookup other={other.name} enemy={(enemy != null ? enemy.name : "null")}", this);
        if (enemy != null)
        {
            Transform damageOwner = owner != null ? owner : transform;
            Debug.Log($"[CombatHitDebug] BeforeReceiveDamage enemy={enemy.name} damage={damage} owner={damageOwner.name}", this);
            enemy.ReceiveDamage(damage, damageOwner);
        }
        
        
    }

    public void SetDamage(int Num, Transform Owner)
    {
        damage = Num;
        owner = Owner;
    }

	
}
