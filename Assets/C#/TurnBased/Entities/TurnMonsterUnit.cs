using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 怪物回合单位：状态机 AI 决策 + 行动入队。
    /// 行为参数由组合根注入（不同原型：近战莽夫 / 远程风筝）。
    /// </summary>
    public sealed class TurnMonsterUnit : TurnUnit
    {
        public MonsterBehaviorConfig Config { get; private set; }

        private readonly MonsterBrainStateMachine _brain = new MonsterBrainStateMachine();

        public void ConfigureMonster(UnitConfig unitConfig, MonsterBehaviorConfig behavior, GridMap map, GridPosition start, Color color)
        {
            Config = behavior ?? MonsterBehaviorConfig.MeleeBerserker();
            base.Configure(unitConfig, map, start, color);
        }

        public override void TakeTurn(TurnExecutionContext context)
        {
            if (Config == null)
            {
                TurnLog.Error($"{DisplayName} 缺少行为配置，跳过行动");
                context.Queue.Enqueue(new WaitAction(this));
                return;
            }

            var decision = _brain.Decide(new MonsterContext
            {
                Self = this,
                Player = context.Player,
                Map = context.Map,
                DistanceField = context.DistanceField,
                Pathfinder = context.Pathfinder,
                Fov = context.Fov,
                Config = Config,
            });
            TurnLog.Log($"{DisplayName} 决策[{_brain.CurrentState}]：{decision.Reason}");

            switch (decision.Kind)
            {
                case MonsterActionKind.Attack:
                    context.Queue.Enqueue(new AttackAction(this, decision.AttackTarget,
                        decision.AttackTarget is IGridEntity ge ? ge.GridPosition : GridPosition));
                    break;
                case MonsterActionKind.Move:
                    context.Queue.Enqueue(new MoveAction(this, decision.MoveTarget, context.MovePresentationSeconds));
                    break;
                default:
                    context.Queue.Enqueue(new WaitAction(this));
                    break;
            }
        }
    }
}