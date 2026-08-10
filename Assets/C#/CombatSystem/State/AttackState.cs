using UnityEngine;

public class AttackState : ICombatState
{
    private float attackDuration = 0.4f;
    private float inputBufferWindow = 0.3f;
    private int maxCombo = 3;

    public string GetStateName() => "Attack";

    public void Enter(CombatContext ctx)
    {
        ctx.IsAttacking = true;
        ctx.Presenter?.PlayAttack(ctx.ComboIndex);
        ctx.Presenter?.UpdateAimDirection(ctx.AimDirection);
        ctx.StateTimer = 0f;
    }

    public void Execute(CombatContext ctx)
    {
        ctx.StateTimer += Time.deltaTime;

        PlayerIntent intent = ctx.InputReader.GetIntent();
        bool canCombo = ctx.Decision.CanCombo() && ctx.ComboIndex < maxCombo;

        if (ctx.StateTimer > inputBufferWindow && intent.AttackPressed && canCombo && ctx.Decision.CanAttack())
        {
            ctx.ComboIndex++;
            ctx.Decision.ConsumeAttackStamina();
            ctx.Presenter?.PlayAttack(ctx.ComboIndex);
            ctx.StateTimer = 0f;
        }

        if (ctx.StateTimer > attackDuration)
            ctx.StateMachine.RequestExitToIdle();
    }

    public void Exit(CombatContext ctx)
    {
        ctx.IsAttacking = false;
        ctx.ComboIndex = 0;
    }

    public bool CanTransitionTo(ICombatState newState)
    {
        return newState is DodgeState;
    }
    public void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // animSword.SetTrigger("Attack");
            // animSlash.SetTrigger("Attack");
            // GameObject go = Instantiate(SwordTri, swordTriPos.position,swordTriPos.rotation,swordTriPos);
            // go.GetComponent<PlayerAttackTrigger>().PlayAttack();
            // setDamage(ATK + Random.Range(0, 10)); //随机伤害
        }
    }
}
