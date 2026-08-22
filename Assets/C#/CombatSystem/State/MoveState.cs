using UnityEngine;

public class MoveState : ICombatState
{
    public string GetStateName() => "Move";

    public void Enter(CombatContext ctx)
    {
        ctx.Presenter?.SetMove(true);
    }

    public void Execute(CombatContext ctx)
    {
        if (ctx.MoveInput.sqrMagnitude <= 0.001f)
        {
            ctx.StateMachine.RequestExitToIdle();
            return;
        }

        ctx.Character?.Move(ctx.MoveInput * ctx.CurrentSpeed);
        if (ctx.Character == null && ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = ctx.MoveInput * ctx.CurrentSpeed;

        ctx.Presenter?.UpdateAimDirection(ctx.AimDirection);

        PlayerIntent intent = ctx.Intent;
        if (intent.AttackPressed && ctx.Decision.CanAttack())
        {
            ctx.Decision.ConsumeAttackStamina();
            ctx.StateMachine.ChangeState(new AttackState());
        }
        else if (intent.SkillPressed)
        {
            ctx.StateMachine.ChangeState(new CastBuffState());
        }
        else if (intent.DodgePressed && ctx.Decision.CanDodge())
        {
            ctx.Decision.ConsumeDodgeStamina();
            ctx.StateMachine.ChangeState(new DodgeState());
        }
    }

    public void Exit(CombatContext ctx)
    {
        ctx.Character?.StopMoving();
        if (ctx.Character == null && ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = Vector2.zero;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is AttackState || newState is DodgeState || newState is IdleState || newState is CastBuffState;
    }
}
