using System;
using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>不可变格子坐标（网格数据模型的基础值对象）。</summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanDistance(GridPosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public int ChebyshevDistance(GridPosition other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

        public bool IsAdjacentTo(GridPosition other) => ManhattanDistance(other) == 1;

        /// <summary>四方向正交邻居（回合制走格为 4 方向）。</summary>
        public IEnumerable<GridPosition> OrthogonalNeighbors()
        {
            yield return new GridPosition(X + 1, Y);
            yield return new GridPosition(X - 1, Y);
            yield return new GridPosition(X, Y + 1);
            yield return new GridPosition(X, Y - 1);
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition p && Equals(p);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
        public override string ToString() => $"({X},{Y})";
    }
}