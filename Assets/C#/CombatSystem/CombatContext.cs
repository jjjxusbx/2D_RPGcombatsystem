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

    public PlayerIntent Intent;
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
    public SkillExecutor SkillExecutor;
    public Character Character;

    public float AttackDuration = 0.4f;
    public float InputBufferWindow = 0.3f;
    public int MaxCombo = 3;
    public float DodgeDuration = 0.25f;
    public float InvincibleDuration = 0.2f;
    public float DodgeDistance = 3f;
}
