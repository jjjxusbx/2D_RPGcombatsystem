using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>
    /// BFS 距离场：对给定目标格实时计算全图最短距离（只沿可行走格传播），
    /// 供 AI 决策（追击/拉距）与 A* 启发函数复用。
    /// </summary>
    public sealed class DistanceField
    {
        private int[,] _dist;
        private bool _valid;

        public bool IsValid => _valid;

        public void Compute(GridMap map, GridPosition target)
        {
            if (map == null || !map.IsInside(target))
            {
                _valid = false;
                return;
            }

            _dist = new int[map.Width, map.Height];
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    _dist[x, y] = int.MaxValue;
                }
            }

            var queue = new Queue<GridPosition>();
            _dist[target.X, target.Y] = 0;
            queue.Enqueue(target);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int nextDist = _dist[cur.X, cur.Y] + 1;
                foreach (var n in cur.OrthogonalNeighbors())
                {
                    if (!map.IsInside(n) || !map.IsWalkable(n)) continue;
                    if (_dist[n.X, n.Y] != int.MaxValue) continue;
                    _dist[n.X, n.Y] = nextDist;
                    queue.Enqueue(n);
                }
            }
            _valid = true;
        }

        public bool TryGetDistance(GridPosition p, out int distance)
        {
            if (_valid && _dist != null && p.X >= 0 && p.X < _dist.GetLength(0) && p.Y >= 0 && p.Y < _dist.GetLength(1))
            {
                distance = _dist[p.X, p.Y];
                return true;
            }
            distance = int.MaxValue;
            return false;
        }

        public bool IsReachable(GridPosition p) => TryGetDistance(p, out int d) && d != int.MaxValue;
    }
}