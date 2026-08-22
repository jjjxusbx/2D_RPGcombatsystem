using System.Collections.Generic;
using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 回合管理器（能量速度系统核心）：每轮为所有 ITurnTaker 累加能量，
    /// 能量达到阈值的单位取得行动权并消耗阈值能量；
    /// 按能量（同值按速度）降序决定本轮行动顺序，支持不同速度单位的差异化行动频率。
    /// </summary>
    public sealed class TurnManager
    {
        public const int EnergyThreshold = 100;

        private readonly List<ITurnTaker> _units = new List<ITurnTaker>();

        public int Round { get; private set; }
        public IReadOnlyList<ITurnTaker> Units => _units;

        public void AddUnit(ITurnTaker unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[TurnManager] AddUnit 收到 null，忽略");
                return;
            }
            if (_units.Contains(unit))
            {
                Debug.LogWarning($"[TurnManager] 重复注册 {unit.DisplayName}，忽略");
                return;
            }
            _units.Add(unit);
        }

        public void RemoveUnit(ITurnTaker unit)
        {
            if (unit != null) _units.Remove(unit);
        }

        /// <summary>
        /// 推进一轮：先清理已死亡单位，再全员积攒能量，
        /// 返回本轮取得行动权的单位列表（能量降序，能量相同按速度降序）。
        /// </summary>
        public List<ITurnTaker> AdvanceRound()
        {
            // 生命周期自清洁：移除已死亡单位
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i] is IGridEntity ge && !ge.IsAlive) _units.RemoveAt(i);
            }

            Round++;
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].GainEnergy(_units[i].GetSpeed());
            }

            var actors = new List<ITurnTaker>();
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Energy >= EnergyThreshold) actors.Add(_units[i]);
            }

            actors.Sort((a, b) =>
            {
                int byEnergy = b.Energy.CompareTo(a.Energy);
                if (byEnergy != 0) return byEnergy;
                return b.GetSpeed().CompareTo(a.GetSpeed());
            });

            for (int i = 0; i < actors.Count; i++)
            {
                if (!actors[i].TryConsumeEnergy(EnergyThreshold))
                {
                    Debug.LogError($"[TurnManager] {actors[i].DisplayName} 消耗能量失败（能量 {actors[i].Energy}），回合状态异常");
                }
                actors[i].OnTurnTaken();
            }
            return actors;
        }
    }
}