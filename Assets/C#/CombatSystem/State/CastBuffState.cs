using UnityEngine;

/// <summary>
/// 施放自身增益技能（Q 键狂暴）的状态：复用攻击动画表现，施放后短暂停留再回 Idle。
/// </summary>
public class CastBuffState : ICombatState
{
    private const float CastDuration = 0.3f;

    public string GetStateName() => "CastBuff";

    public void Enter(CombatContext ctx)
    {
        ctx.IsAttacking = true;
        ctx.Presenter?.PlayAttack(0);
        ctx.SkillExecutor?.CastSelfBuff();
        ctx.StateTimer = 0f;
    }

    public void Execute(CombatContext ctx)
    {
        ctx.StateTimer += Time.deltaTime;
        if (ctx.StateTimer >= CastDuration)
            ctx.StateMachine.RequestExitToIdle();
    }

    public void Exit(CombatContext ctx)
    {
        ctx.IsAttacking = false;
    }

    public bool CanTransitionTo(ICombatState newState) => true;
}