using System.Collections;
using UnityEngine;

public class PlayerAnimationPresenter : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator swordAnimator;
    [SerializeField] private Animator slashAnimator;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Transform weaponRoot;

    [Header("Combat FX")]
    [SerializeField] private Collider2D attackHitBox;
    [SerializeField] private float attackHitBoxActiveTime = 0.18f;

    [Header("Parameters")]
    [SerializeField] private string runParameter = "IsRun";
    [SerializeField] private string playerAttackTrigger = "Attack";
    [SerializeField] private string swordAttackTrigger = "ATK_1";
    [SerializeField] private string slashAttackTrigger = "ATK1";

    private Vector2 currentAimDirection = Vector2.right;
    private Coroutine hitBoxRoutine;

    private void Awake()
    {
        BindMissingReferences();
        DeactivateHitBox();
    }

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

        if (attackHitBox == null)
        {
            var colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col.name.Contains("Attack") || col.gameObject.name.Contains("HitBox"))
                {
                    attackHitBox = col;
                    break;
                }
            }
        }
    }

    public void SetMove(bool isMoving)
    {
        if (playerAnimator != null)
            playerAnimator.SetBool(runParameter, isMoving);
    }

    public void UpdateAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        currentAimDirection = direction.normalized;
        ApplyFacing(currentAimDirection, playerRenderer, weaponRenderer, weaponRoot);
    }

    public void PlayAttack(int comboIndex)
    {
        SetTriggerIfExists(playerAnimator, playerAttackTrigger);
        SetTriggerIfExists(swordAnimator, swordAttackTrigger);
        SetTriggerIfExists(slashAnimator, slashAttackTrigger);
        RestartHitBox();
    }

    public void PlayDodge()
    {
        SetTriggerIfExists(playerAnimator, "Dodge");
    }

    public void PlayHurt()
    {
        SetTriggerIfExists(playerAnimator, "Hurt");
    }

    public void ActivateHitBox()
    {
        if (hitBoxRoutine != null) StopCoroutine(hitBoxRoutine);
        hitBoxRoutine = StartCoroutine(HitBoxRoutine());
    }

    public void DeactivateHitBox()
    {
        if (attackHitBox != null)
        {
            attackHitBox.enabled = false;
            attackHitBox.isTrigger = true;
        }
    }

    /// <summary>统一处理角色与武器朝向翻转，供 PlayerCombatAnimation / 基础移动 共享调用</summary>
    public static void ApplyFacing(Vector2 aimDirection, SpriteRenderer playerRenderer, SpriteRenderer weaponRenderer, Transform weaponRoot)
    {
        bool faceLeft = aimDirection.x < -0.001f;
        if (playerRenderer != null) playerRenderer.flipX = faceLeft;
        if (weaponRenderer != null) weaponRenderer.flipY = faceLeft;
        if (weaponRoot != null)
        {
            Vector3 scale = weaponRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceLeft ? -1f : 1f);
            weaponRoot.localScale = scale;
        }
    }

    private void RestartHitBox()
    {
        if (hitBoxRoutine != null) StopCoroutine(hitBoxRoutine);
        hitBoxRoutine = StartCoroutine(HitBoxRoutine());
    }

    private IEnumerator HitBoxRoutine()
    {
        if (attackHitBox != null) attackHitBox.enabled = true;
        yield return new WaitForSeconds(attackHitBoxActiveTime);
        DeactivateHitBox();
        hitBoxRoutine = null;
    }

    /// <summary>安全触发 Animator Trigger 参数，供 PlayerCombatAnimation / 基础移动 共享调用</summary>
    public static void SetTriggerIfExists(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(triggerName);
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }
}
