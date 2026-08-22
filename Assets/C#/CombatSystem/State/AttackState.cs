using UnityEngine;

public class AttackState : ICombatState
{
    public string GetStateName() => "Attack";

    public void Enter(CombatContext ctx)
    {
        ctx.IsAttacking = true;
        ctx.Presenter?.UpdateAimDirection(ctx.AimDirection);
        if (ctx.Character == null || !ctx.Character.TryExecuteAbility<CharacterAttackAbility>())
        {
            ctx.Presenter?.PlayAttack(ctx.ComboIndex);
            ctx.SkillExecutor?.ExecuteAttack();
        }
        ctx.StateTimer = 0f;
    }

    public void Execute(CombatContext ctx)
    {
        ctx.StateTimer += Time.deltaTime;

        PlayerIntent intent = ctx.Intent;
        bool canCombo = ctx.Decision.CanCombo() && ctx.ComboIndex < ctx.MaxCombo;

        if (ctx.StateTimer > ctx.InputBufferWindow && intent.AttackPressed && canCombo && ctx.Decision.CanAttack())
        {
            ctx.ComboIndex++;
            ctx.Decision.ConsumeAttackStamina();
            if (ctx.Character == null || !ctx.Character.TryExecuteAbility<CharacterAttackAbility>())
            {
                ctx.Presenter?.PlayAttack(ctx.ComboIndex);
                ctx.SkillExecutor?.ExecuteAttack();
            }
            ctx.StateTimer = 0f;
        }

        if (ctx.StateTimer > ctx.AttackDuration)
        {
            ctx.StateMachine.RequestExitToIdle();
        }
    }

    public void Exit(CombatContext ctx)
    {
        ctx.IsAttacking = false;
        ctx.ComboIndex = 0;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is DodgeState || newState is IdleState;
    }
}
