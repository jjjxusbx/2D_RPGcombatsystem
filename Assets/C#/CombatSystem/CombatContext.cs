using UnityEngine;

public class CombatContext
{
    public Animator PlayerAnimator;
    public Animator SwordAnimator;
    public Animator SlashAnimator;
    public Rigidbody2D Rigidbody;
    public SpriteRenderer PlayerRenderer;
    public SpriteRenderer WeaponRenderer;
    public Transform WeaponRoot;
    public Collider2D AttackHitBox;

    public Vector2 MoveInput;
    public Vector2 AimDirection = Vector2.right;
    public bool IsAttacking;
    public bool IsInvincible;
    public bool IsHurt;
    public float CurrentSpeed = 5f;
    public float Health = 100f;
    public float MaxHealth = 100f;
    public int ComboIndex;
    public float StateTimer;

    public PlayerAnimationPresenter Presenter;
    public PlayerInputReader InputReader;
    public CombatDecisionComponent Decision;
    public CombatStateMachine StateMachine;
}
