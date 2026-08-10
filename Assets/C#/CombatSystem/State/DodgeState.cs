using UnityEngine;

public class DodgeState : ICombatState
{
    private float dodgeDuration = 0.25f;
    private float invincibleDuration = 0.2f;
    private float dodgeDistance = 3f;

    private Vector2 dodgeDirection;
    private float stateTimer;

    public string GetStateName() => "Dodge";

    public void Enter(CombatContext ctx)
    {
        ctx.IsInvincible = true;
        ctx.Presenter?.PlayDodge();

        dodgeDirection = ctx.MoveInput.sqrMagnitude > 0.001f ? ctx.MoveInput.normalized : ctx.AimDirection;
        stateTimer = 0f;

        if (ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = dodgeDirection * dodgeDistance / dodgeDuration;
    }

    public void Execute(CombatContext ctx)
    {
        stateTimer += Time.deltaTime;

        if (stateTimer > invincibleDuration)
            ctx.IsInvincible = false;

        if (stateTimer > dodgeDuration)
            ctx.StateMachine.RequestExitToIdle();
    }

    public void Exit(CombatContext ctx)
    {
        ctx.IsInvincible = false;
        if (ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = Vector2.zero;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is AttackState;
    }
}
