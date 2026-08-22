namespace TurnBased
{
    /// <summary>AI 决策结果类型。</summary>
    public enum MonsterActionKind
    {
        Wait,
        Move,
        Attack,
    }

    /// <summary>AI 决策结果：攻击或移动的目标，附决策原因便于日志观察。</summary>
    public struct MonsterDecision
    {
        public MonsterActionKind Kind;
        public GridPosition MoveTarget;
        public IDamageable AttackTarget;
        public string Reason;

        public static MonsterDecision Wait(string reason) => new MonsterDecision { Kind = MonsterActionKind.Wait, Reason = reason };
        public static MonsterDecision MoveTo(GridPosition target, string reason) => new MonsterDecision { Kind = MonsterActionKind.Move, MoveTarget = target, Reason = reason };
        public static MonsterDecision Attack(IDamageable target, string reason) => new MonsterDecision { Kind = MonsterActionKind.Attack, AttackTarget = target, Reason = reason };
    }

    /// <summary>怪物大脑契约：输入上下文与行为参数，输出本回合决策。</summary>
    public interface IMonsterBrain
    {
        MonsterDecision Decide(MonsterContext context);
    }

    /// <summary>AI 决策上下文：由回合执行上下文裁剪而来，组合根注入。</summary>
    public sealed class MonsterContext
    {
        public TurnUnit Self;
        public TurnUnit Player;
        public GridMap Map;
        public DistanceField DistanceField;
        public PathfinderAStar Pathfinder;
        public FieldOfView Fov;
        public MonsterBehaviorConfig Config;
    }
}