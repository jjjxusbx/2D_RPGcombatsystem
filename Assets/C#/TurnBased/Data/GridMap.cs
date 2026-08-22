using System.Collections.Generic;
using UnityEngine;

namespace TurnBased
{
    /// <summary>单格数据：是否可行走 + 占据实体引用。</summary>
    public sealed class GridCell
    {
        public bool walkable;
        public IGridEntity occupier;
    }

    /// <summary>
    /// 网格地图数据模型：GridCell[,] 二维数组单格索引，
    /// 负责可行走判定、格内实体查询、世界坐标↔格子坐标换算。
    /// </summary>
    public sealed class GridMap
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; } = 1f;

        private readonly GridCell[,] _cells;
        private readonly List<IGridEntity> _entities = new List<IGridEntity>();

        public IReadOnlyList<IGridEntity> Entities => _entities;

        public GridMap(int width, int height, bool[,] walkable)
        {
            Width = width;
            Height = height;
            _cells = new GridCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new GridCell { walkable = walkable == null || walkable[x, y] };
                }
            }
        }

        public bool IsInside(GridPosition p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

        public bool IsWalkable(GridPosition p) => IsInside(p) && _cells[p.X, p.Y].walkable;

        public IGridEntity GetEntityAt(GridPosition p) => IsInside(p) ? _cells[p.X, p.Y].occupier : null;

        /// <summary>
        /// 该格是否对 mover 可通行：可行走且未被其他实体占据（目标格除外）。
        /// 允许进入目标格，支撑“追击到贴脸”语义。
        /// </summary>
        public bool IsCellFree(GridPosition p, IGridEntity mover, GridPosition goal)
        {
            if (!IsWalkable(p)) return false;
            var occ = GetEntityAt(p);
            if (occ == null || occ == mover) return true;
            return p == goal;
        }

        public bool TryPlaceEntity(IGridEntity entity, GridPosition p)
        {
            if (!IsInside(p))
            {
                Debug.LogWarning($"[GridMap] 放置失败：坐标越界 {p}");
                return false;
            }
            if (!_cells[p.X, p.Y].walkable)
            {
                Debug.LogWarning($"[GridMap] 放置失败：不可行走格 {p}");
                return false;
            }
            if (_cells[p.X, p.Y].occupier != null)
            {
                Debug.LogWarning($"[GridMap] 放置失败：格子已被占据 {p}");
                return false;
            }
            _cells[p.X, p.Y].occupier = entity;
            _entities.Add(entity);
            return true;
        }

        public bool TryRemoveEntity(IGridEntity entity, GridPosition p)
        {
            if (!IsInside(p)) return false;
            if (_cells[p.X, p.Y].occupier != entity) return false;
            _cells[p.X, p.Y].occupier = null;
            _entities.Remove(entity);
            return true;
        }

        public bool TryMoveEntity(IGridEntity entity, GridPosition from, GridPosition to)
        {
            if (!IsInside(from) || _cells[from.X, from.Y].occupier != entity)
            {
                Debug.LogWarning($"[GridMap] 移动失败：起点 {from} 无实体 {entity?.DisplayName}");
                return false;
            }
            if (!IsCellFree(to, entity, to)) return false;
            _cells[from.X, from.Y].occupier = null;
            _cells[to.X, to.Y].occupier = entity;
            return true;
        }

        /// <summary>格子 → 世界坐标（格子中心）。</summary>
        public Vector3 GridToWorld(GridPosition p) => new Vector3((p.X + 0.5f) * CellSize, (p.Y + 0.5f) * CellSize, 0f);

        /// <summary>世界坐标 → 格子坐标（向下取整）。</summary>
        public GridPosition WorldToGrid(Vector3 world)
        {
            return new GridPosition(Mathf.FloorToInt(world.x / CellSize), Mathf.FloorToInt(world.y / CellSize));
        }
    }
}