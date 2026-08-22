using UnityEngine;

namespace TurnBased
{
    /// <summary>怪物 AI 状态。</summary>
    public enum MonsterAiState
    {
        Wander, // 无目标：巡逻/待机
        Chase,  // 看到玩家且不在射程/安全距离内：沿最短路径接近
        Attack, // 射程内：发起攻击
        Away,   // 被贴脸：拉距（风筝型）
    }

    /// <summary>
    /// 怪物状态机 AI：Wander → Chase → Attack / Away 流转。
    /// 索敌 = 视野半径（切比雪夫）+ Bresenham LOS；
    /// 追击 = 距离场（BFS，以玩家为目标）+ A* 取路径首步；
    /// 拉距 = 选择距玩家最远的可行走邻格。
    /// 全部行为参数来自 MonsterBehaviorConfig，不同原型可配出不同策略。
    /// </summary>
    public sealed class MonsterBrainStateMachine : IMonsterBrain
    {
        public MonsterAiState CurrentState { get; private set; } = MonsterAiState.Wander;

        public MonsterDecision Decide(MonsterContext ctx)
        {
            if (ctx.Self == null || ctx.Player == null || ctx.Player.IsDead)
            {
                return MonsterDecision.Wait("无有效目标");
            }

            GridPosition selfPos = ctx.Self.GridPosition;
            GridPosition playerPos = ctx.Player.GridPosition;
            int dist = selfPos.ManhattanDistance(playerPos);

            // 索敌：半径内 + 直线无遮挡
            bool seesPlayer = ctx.Config.visionRadius <= 0 || dist <= ctx.Config.visionRadius;
            if (seesPlayer && ctx.Fov != null && ctx.Map != null)
            {
                seesPlayer = ctx.Fov.HasLineOfSight(ctx.Map, selfPos, playerPos);
            }

            if (seesPlayer)
            {
                // 1) 被贴脸且低于安全距离 → 拉距（Away）
                if (ctx.Config.minRange > 1 && dist <= ctx.Config.minRange)
                {
                    GridPosition away = PickStepAway(ctx, playerPos);
                    if (away != selfPos)
                    {
                        CurrentState = MonsterAiState.Away;
                        return MonsterDecision.MoveTo(away, $"被贴脸拉距（距离 {dist} ≤ {ctx.Config.minRange}）");
                    }
                }

                // 2) 射程内 → 攻击（Attack）
                if (dist <= ctx.Config.attackRange)
                {
                    CurrentState = MonsterAiState.Attack;
                    return MonsterDecision.Attack(ctx.Player, $"射程内攻击（距离 {dist}）");
                }

                // 3) 其余情况 → 追击（Chase）
                GridPosition step = PickChaseStep(ctx, playerPos);
                if (step != selfPos)
                {
                    CurrentState = MonsterAiState.Chase;
                    return MonsterDecision.MoveTo(step, $"追击玩家（距离 {dist}）");
                }
            }

            // 4) 视野内无玩家 → Wander
            return DecideWander(ctx);
        }

        private MonsterDecision DecideWander(MonsterContext ctx)
        {
            if (Random.value > ctx.Config.wanderChance)
            {
                return MonsterDecision.Wait("视野内无玩家，待机");
            }

            foreach (var n in ctx.Self.GridPosition.OrthogonalNeighbors())
            {
                if (ctx.Map.IsCellFree(n, ctx.Self, n))
                {
                    CurrentState = MonsterAiState.Wander;
                    return MonsterDecision.MoveTo(n, "视野内无玩家，随机游走");
                }
            }
            return MonsterDecision.Wait("视野内无玩家，无可行走邻格");
        }

        /// <summary>追击：距离场（以玩家为目标）作为启发 + A* 取路径首步。</summary>
        private GridPosition PickChaseStep(MonsterContext ctx, GridPosition playerPos)
        {
            if (ctx.DistanceField != null) ctx.DistanceField.Compute(ctx.Map, playerPos);
            var path = ctx.Pathfinder.FindPath(ctx.Map, ctx.Self.GridPosition, playerPos, ctx.Self, ctx.DistanceField);
            if (path.Count > 0) return path[0];
            return ctx.Self.GridPosition;
        }

        /// <summary>拉距：选择距玩家最远且可行走的邻格。</summary>
        private GridPosition PickStepAway(MonsterContext ctx, GridPosition playerPos)
        {
            if (ctx.DistanceField != null) ctx.DistanceField.Compute(ctx.Map, playerPos);

            GridPosition best = ctx.Self.GridPosition;
            int bestDist = int.MinValue;
            foreach (var n in ctx.Self.GridPosition.OrthogonalNeighbors())
            {
                if (!ctx.Map.IsCellFree(n, ctx.Self, n)) continue;

                int d;
                if (ctx.DistanceField != null && ctx.DistanceField.TryGetDistance(n, out int dfd))
                {
                    d = dfd;
                }
                else
                {
                    d = n.ManhattanDistance(playerPos);
                }
                if (d > bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
            return best;
        }
    }
}