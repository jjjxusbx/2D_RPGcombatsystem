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

        if (ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = ctx.MoveInput * ctx.CurrentSpeed;

        ctx.Presenter?.UpdateAimDirection(ctx.AimDirection);

        PlayerIntent intent = ctx.InputReader.GetIntent();
        if (intent.AttackPressed && ctx.Decision.CanAttack())
        {
            ctx.Decision.ConsumeAttackStamina();
            ctx.StateMachine.ChangeState(new AttackState());
        }
        else if (intent.DodgePressed && ctx.Decision.CanDodge())
        {
            ctx.Decision.ConsumeDodgeStamina();
            ctx.StateMachine.ChangeState(new DodgeState());
        }
    }

    public void Exit(CombatContext ctx)
    {
        if (ctx.Rigidbody != null)
            ctx.Rigidbody.linearVelocity = Vector2.zero;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is AttackState || newState is DodgeState || newState is IdleState;
    }
}
