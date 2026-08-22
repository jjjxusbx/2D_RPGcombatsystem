public class IdleState : ICombatState
{
    public string GetStateName() => "Idle";

    public void Enter(CombatContext ctx)
    {
        ctx.Presenter?.SetMove(false);
        ctx.IsAttacking = false;
        ctx.IsInvincible = false;
    }

    public void Execute(CombatContext ctx)
    {
        if (ctx.MoveInput.sqrMagnitude > 0.001f)
        {
            ctx.StateMachine.ChangeState(new MoveState());
            return;
        }

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

    public void Exit(CombatContext ctx) { }

    public bool CanTransitionTo(ICombatState newState) => true;
}
