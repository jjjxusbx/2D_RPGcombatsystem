public interface ICombatState
{
    void Enter(CombatContext context);
    void Execute(CombatContext context);
    void Exit(CombatContext context);
    bool CanTransitionTo(ICombatState newState);
    string GetStateName();
}
