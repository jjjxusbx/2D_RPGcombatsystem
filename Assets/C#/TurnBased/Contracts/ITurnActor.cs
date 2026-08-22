using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>行动者契约：拥有行动权时由回合调度器调用，向行动队列入队行动。</summary>
    public interface ITurnActor : ITurnTaker
    {
        void TakeTurn(TurnExecutionContext context);
    }

    /// <summary>
    /// 回合执行上下文：组合根装配后分发给所有行动者。
    /// 显式注入，不依赖全局单例。
    /// </summary>
    public sealed class TurnExecutionContext
    {
        public GridMap Map;
        public TurnEventQueue Queue;
        public DistanceField DistanceField;
        public PathfinderAStar Pathfinder;
        public FieldOfView Fov;
        public TurnUnit Player;
        public List<TurnUnit> Units;
        public float MovePresentationSeconds;

        /// <summary>与我方敌对且存活的所有单位。</summary>
        public List<TurnUnit> EnemiesOf(TurnUnit me)
        {
            var list = new List<TurnUnit>();
            if (Units == null) return list;
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u != null && u != me && u.Team != me.Team && !u.IsDead) list.Add(u);
            }
            return list;
        }
    }
}