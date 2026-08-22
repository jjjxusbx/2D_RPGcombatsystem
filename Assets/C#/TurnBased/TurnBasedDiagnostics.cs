using System.Collections.Generic;
using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 回合域装配自检：由 TurnBasedBootstrap.Start 调用，输出可观察诊断报告。
    /// 覆盖：地图/回合管理器/队列装配、实体注册与出生点合法性、重复占位、
    /// BFS 距离场可达性与 A* 寻路。只读，不修改任何行为。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurnBasedDiagnostics : MonoBehaviour
    {
        private readonly List<string> _issues = new List<string>();
        private readonly List<string> _infos = new List<string>();

        public void Run(TurnBasedBootstrap bootstrap)
        {
            _issues.Clear();
            _infos.Clear();

            if (bootstrap == null)
            {
                _issues.Add("bootstrap 为空，无法自检");
                Report();
                return;
            }

            CheckCore(bootstrap);
            CheckEntities(bootstrap);
            CheckPathfinding(bootstrap);

            Report();
        }

        private void CheckCore(TurnBasedBootstrap b)
        {
            if (b.Map == null)
            {
                _issues.Add("GridMap 未装配");
            }
            else
            {
                _infos.Add($"GridMap {b.Map.Width}x{b.Map.Height} 已装配，实体数 {b.Map.Entities.Count}");
            }

            if (b.TurnManager == null)
            {
                _issues.Add("TurnManager 未装配");
            }
            else
            {
                _infos.Add($"TurnManager 已注册 {b.TurnManager.Units.Count} 个单位");
            }

            if (b.TurnQueue == null) _issues.Add("TurnEventQueue 未装配");
            if (b.Context == null) _issues.Add("TurnExecutionContext 未装配");
        }

        private void CheckEntities(TurnBasedBootstrap b)
        {
            if (b.Player == null)
            {
                _issues.Add("玩家实体未生成");
            }
            else if (b.Map != null && !b.Map.IsInside(b.Player.GridPosition))
            {
                _issues.Add("玩家出生点越界");
            }

            if (b.Monsters.Count == 0) _issues.Add("未生成任何怪物");

            for (int i = 0; i < b.Monsters.Count; i++)
            {
                var m = b.Monsters[i];
                if (m == null)
                {
                    _issues.Add($"怪物 #{i + 1} 为 null");
                    continue;
                }
                if (b.Map != null && !b.Map.IsInside(m.GridPosition)) _issues.Add($"{m.DisplayName} 出生点越界");
                if (m.Config == null) _issues.Add($"{m.DisplayName} 缺少行为配置");
            }

            if (b.Map != null)
            {
                var seen = new HashSet<GridPosition>();
                foreach (var e in b.Map.Entities)
                {
                    if (!seen.Add(e.GridPosition))
                    {
                        _issues.Add($"格子 {e.GridPosition} 被多个实体同时占据（{e.DisplayName}）");
                    }
                }
            }
        }

        private void CheckPathfinding(TurnBasedBootstrap b)
        {
            if (b.Map == null || b.Player == null || b.Monsters.Count == 0) return;

            var df = new DistanceField();
            GridPosition goal = b.Monsters[0].GridPosition;
            df.Compute(b.Map, goal);

            if (!df.TryGetDistance(b.Player.GridPosition, out int d) || d == int.MaxValue)
            {
                _issues.Add($"玩家到 {b.Monsters[0].DisplayName} 的距离场不可达（地图可能不连通）");
            }
            else
            {
                _infos.Add($"玩家 → {b.Monsters[0].DisplayName} 最短步数 {d}（BFS 距离场校验通过）");
            }

            var path = new PathfinderAStar().FindPath(b.Map, b.Player.GridPosition, goal, b.Player, df);
            if (path.Count == 0 && b.Player.GridPosition != goal)
            {
                _issues.Add($"A* 未能找到玩家到 {b.Monsters[0].DisplayName} 的路径");
            }
            else if (path.Count > 0)
            {
                _infos.Add($"A* 示例路径：{string.Join(" -> ", path.ConvertAll(p => p.ToString()))}");
            }
        }

        private void Report()
        {
            if (_issues.Count > 0)
            {
                Debug.LogWarning($"[TurnBased.Diagnostics] {name} 装配发现 {_issues.Count} 个问题：\n- " + string.Join("\n- ", _issues), this);
            }
            if (_infos.Count > 0)
            {
                Debug.Log($"[TurnBased.Diagnostics] {name} 装配信息：\n- " + string.Join("\n- ", _infos), this);
            }
            if (_issues.Count == 0 && _infos.Count == 0)
            {
                Debug.Log($"[TurnBased.Diagnostics] {name} 装配检查通过。", this);
            }
        }
    }
}