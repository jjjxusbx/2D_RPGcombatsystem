using UnityEngine;

public class DodgeState : ICombatState
{
    private Vector2 dodgeDirection;
    private float stateTimer;

    public string GetStateName() => "Dodge";

    public void Enter(CombatContext ctx)
    {
        ctx.IsInvincible = true;
        ctx.Presenter?.PlayDodge();

        dodgeDirection = ctx.MoveInput.sqrMagnitude > 0.001f ? ctx.MoveInput.normalized : ctx.AimDirection;
        stateTimer = 0f;

        ctx.Character?.Move(dodgeDirection * ctx.DodgeDistance / ctx.DodgeDuration);
        if (ctx.Character == null && ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = dodgeDirection * ctx.DodgeDistance / ctx.DodgeDuration;
    }

    public void Execute(CombatContext ctx)
    {
        stateTimer += Time.deltaTime;

        if (stateTimer > ctx.InvincibleDuration)
            ctx.IsInvincible = false;

        if (stateTimer > ctx.DodgeDuration)
            ctx.StateMachine.RequestExitToIdle();
    }

    public void Exit(CombatContext ctx)
    {
        ctx.IsInvincible = false;
        ctx.Character?.StopMoving();
        if (ctx.Character == null && ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = Vector2.zero;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is AttackState || newState is IdleState;
    }
}
