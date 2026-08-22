using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>
    /// A* 寻路：BFS 距离场作为启发函数（未提供时退回曼哈顿距离），
    /// 其他实体占据格作为 extraCost 罚分避免拥堵，目标格允许进入。
    /// </summary>
    public sealed class PathfinderAStar
    {
        private sealed class Node
        {
            public GridPosition Pos;
            public int G;
            public int F;
            public Node Parent;
        }

        /// <summary>返回从起点到终点的路径（不含起点，含终点）；失败返回空列表。</summary>
        public List<GridPosition> FindPath(GridMap map, GridPosition start, GridPosition goal, IGridEntity mover, DistanceField heuristic, int maxExpansions = 512)
        {
            var result = new List<GridPosition>();
            if (map == null || !map.IsInside(start) || !map.IsInside(goal))
            {
                TurnLog.Warn($"[A*] 起点 {start} 或终点 {goal} 越界，无法寻路");
                return result;
            }
            if (start == goal) return result;

            var open = new List<Node>();
            var closed = new HashSet<GridPosition>();
            var gScore = new Dictionary<GridPosition, int>();

            open.Add(new Node { Pos = start, G = 0, F = Heuristic(start, goal, heuristic) });
            gScore[start] = 0;

            int expansions = 0;
            while (open.Count > 0)
            {
                // 线性扫描取最小 F（地图小，网格 ≤ 200 格，无需堆）
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < open[best].F) best = i;
                }

                var cur = open[best];
                if (cur.Pos == goal)
                {
                    Reconstruct(cur, result);
                    return result;
                }

                open.RemoveAt(best);
                closed.Add(cur.Pos);

                if (++expansions > maxExpansions)
                {
                    TurnLog.Warn($"[A*] 超出最大扩展次数 {maxExpansions}，放弃");
                    return result;
                }

                foreach (var n in cur.Pos.OrthogonalNeighbors())
                {
                    if (closed.Contains(n)) continue;
                    if (!map.IsCellFree(n, mover, goal)) continue;

                    int extraCost = 0;
                    if (n != goal)
                    {
                        var occ = map.GetEntityAt(n);
                        if (occ != null && occ != mover) extraCost = 6; // 动态拥堵罚分
                    }

                    int tentativeG = cur.G + 1 + extraCost;
                    if (gScore.TryGetValue(n, out int existing) && existing <= tentativeG) continue;

                    gScore[n] = tentativeG;
                    var node = new Node
                    {
                        Pos = n,
                        G = tentativeG,
                        F = tentativeG + Heuristic(n, goal, heuristic),
                        Parent = cur,
                    };

                    // 替换 open 中同格旧节点（若有）
                    bool replaced = false;
                    for (int i = 0; i < open.Count; i++)
                    {
                        if (open[i].Pos == n)
                        {
                            open[i] = node;
                            replaced = true;
                            break;
                        }
                    }
                    if (!replaced) open.Add(node);
                }
            }

            TurnLog.Warn($"[A*] 未找到从 {start} 到 {goal} 的路径");
            return result;
        }

        private static int Heuristic(GridPosition from, GridPosition goal, DistanceField heuristic)
        {
            if (heuristic != null && heuristic.TryGetDistance(from, out int d) && d != int.MaxValue) return d;
            return from.ManhattanDistance(goal);
        }

        private static void Reconstruct(Node end, List<GridPosition> path)
        {
            var stack = new List<GridPosition>();
            var cur = end;
            while (cur != null && cur.Parent != null)
            {
                stack.Add(cur.Pos);
                cur = cur.Parent;
            }
            for (int i = stack.Count - 1; i >= 0; i--) path.Add(stack[i]); // 不含起点
        }
    }
}