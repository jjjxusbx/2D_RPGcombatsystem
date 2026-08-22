using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>
    /// 视野检测：Bresenham 直线投射判定两格之间是否有遮挡（不可行走格即墙遮挡）。
    /// 战争迷雾的三态表现（Unexplored/Explored/Visible）在后续里程碑接入 Tilemap。
    /// </summary>
    public sealed class FieldOfView
    {
        /// <summary>from 到 to 是否存在无遮挡视线（两点必须在界内且路径上无墙）。</summary>
        public bool HasLineOfSight(GridMap map, GridPosition from, GridPosition to)
        {
            if (map == null || !map.IsInside(from) || !map.IsInside(to)) return false;

            int x0 = from.X, y0 = from.Y;
            int x1 = to.X, y1 = to.Y;
            int dx = System.Math.Abs(x1 - x0);
            int dy = System.Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 == x1 && y0 == y1) return true;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }

                var cell = new GridPosition(x0, y0);
                if (!map.IsInside(cell)) return false;
                if (!map.IsWalkable(cell)) return false; // 墙遮挡
            }
        }

        /// <summary>以 from 为圆心、radius 为半径（切比雪夫距离）内的可见格集合。</summary>
        public List<GridPosition> GetVisibleCells(GridMap map, GridPosition from, int radius)
        {
            var result = new List<GridPosition>();
            if (map == null) return result;

            for (int x = from.X - radius; x <= from.X + radius; x++)
            {
                for (int y = from.Y - radius; y <= from.Y + radius; y++)
                {
                    var p = new GridPosition(x, y);
                    if (!map.IsInside(p) || !map.IsWalkable(p)) continue;
                    if (from.ChebyshevDistance(p) > radius) continue;
                    if (HasLineOfSight(map, from, p)) result.Add(p);
                }
            }
            return result;
        }
    }
}