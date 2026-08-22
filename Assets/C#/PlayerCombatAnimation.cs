using System.Collections;
using UnityEngine;

public class PlayerCombatAnimation : AbilityBase
{
    [Header("==组件引用==")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator swordAnimator;
    [SerializeField] private Animator slashAnimator;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Transform weaponRoot;

    [SerializeField] private Transform target;//锁定敌人
    [SerializeField] private Collider2D attackRangeTrigger;
    private Vector2 aimDirection = Vector2.right;
    private Coroutine attackRangeRoutine;
    public GameObject swordTri;
    public Transform swordTriPos;

    [Header("动画参数")]
    [SerializeField] private string runParameter = "IsRun";
    [SerializeField] private string playerAttackTrigger = "Attack";
    [SerializeField] private string swordAttackTrigger = "ATK_1";
    [SerializeField] private string slashAttackTrigger = "ATK1";

    [SerializeField] private float attackRangeActiveTime = 0.18f;


    protected override void OnInitialize()
    {
        BindMissingReferences();
        SetAttackRangeActive(false);
    }

    protected override void OnActivate()
    {
        PlayAttackInternal();
    }

    // 调用外部接口
    public void SetMove(Vector2 moveInput)
    {
        PlayerAnimationPresenter.SetBoolIfExists(playerAnimator, runParameter,
            moveInput.sqrMagnitude > 0.001f);
    }
    public void AimAt(Vector2 worldPosition)
    {
        Vector2 direction = worldPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        aimDirection = direction.normalized;
        PlayerAnimationPresenter.ApplyFacing(aimDirection, playerRenderer, weaponRenderer, weaponRoot);
    }
    public void PlayAttack()
    {
        if (!IsInitialized)
        {
            Initialize(GetComponentInParent<Character>());
        }

        if (IsActive)
        {
            PlayAttackInternal();
            return;
        }

        Activate();
        Deactivate();
    }

    private void PlayAttackInternal()
    {
        if (target != null)
            AimAt(target.position);

        PlayerAnimationPresenter.SetTriggerIfExists(playerAnimator, playerAttackTrigger);
        PlayerAnimationPresenter.SetTriggerIfExists(swordAnimator, swordAttackTrigger);
        PlayerAnimationPresenter.SetTriggerIfExists(slashAnimator, slashAttackTrigger);
        RestartAttackRange();
    }
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        //AimAtTarget();
    }
    /*
    private void AimAtTarget()
    {
        if (target != null)
        {
            AimAt(target.position);
        }
    }
    */

    private void BindMissingReferences()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerRenderer == null) playerRenderer = GetComponent<SpriteRenderer>();

        Transform swordPivot = transform.Find("WeaponPivot/SwordPivot");
        if (swordPivot != null)
        {
            if (weaponRoot == null) weaponRoot = swordPivot;
            if (swordAnimator == null) swordAnimator = swordPivot.GetComponent<Animator>();
            if (weaponRenderer == null) weaponRenderer = swordPivot.GetComponent<SpriteRenderer>();
        }

        Transform slashEffect = transform.Find("WeaponPivot/SlashEffect");
        if (slashEffect != null && slashAnimator == null)
            slashAnimator = slashEffect.GetComponent<Animator>();
    }

    private void RestartAttackRange()
    {
        if (attackRangeRoutine != null)
        {
            StopCoroutine(attackRangeRoutine);
        }

        attackRangeRoutine = StartCoroutine(AttackRangeRoutine());
    }

    private IEnumerator AttackRangeRoutine()
    {
        SetAttackRangeActive(true);
        yield return new WaitForSeconds(attackRangeActiveTime);
        SetAttackRangeActive(false);
        attackRangeRoutine = null;
    }

    private void SetAttackRangeActive(bool active)
    {
        if (attackRangeTrigger != null)
        {
            attackRangeTrigger.enabled = active;
            attackRangeTrigger.isTrigger = true;
        }
    }
}
