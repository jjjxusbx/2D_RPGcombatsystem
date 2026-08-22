using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>
    /// 玩家回合单位：自动演示脑——射程内攻击，否则沿 A* 路径朝最近敌人移动一格。
    /// 后续 v0.1.5 里程碑替换为玩家输入驱动（点格/方向键）的决策。
    /// </summary>
    public sealed class TurnPlayerUnit : TurnUnit
    {
        public override void TakeTurn(TurnExecutionContext context)
        {
            var enemies = context.EnemiesOf(this);
            if (enemies.Count == 0)
            {
                TurnLog.Log("玩家：没有可见敌人，待机");
                context.Queue.Enqueue(new WaitAction(this));
                return;
            }

            TurnUnit target = FindNearest(enemies);
            int dist = GridPosition.ManhattanDistance(target.GridPosition);

            if (dist <= AttackRange)
            {
                context.Queue.Enqueue(new AttackAction(this, target, target.GridPosition));
                return;
            }

            context.DistanceField.Compute(context.Map, target.GridPosition);
            var path = context.Pathfinder.FindPath(context.Map, GridPosition, target.GridPosition, this, context.DistanceField);
            if (path.Count > 0)
            {
                context.Queue.Enqueue(new MoveAction(this, path[0], context.MovePresentationSeconds));
                return;
            }

            context.Queue.Enqueue(new WaitAction(this));
        }

        private TurnUnit FindNearest(List<TurnUnit> enemies)
        {
            TurnUnit best = enemies[0];
            int bestDist = int.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                int d = GridPosition.ManhattanDistance(enemies[i].GridPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = enemies[i];
                }
            }
            return best;
        }
    }
}