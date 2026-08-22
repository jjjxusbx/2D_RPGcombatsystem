using System;

namespace TurnBased
{
    /// <summary>
    /// 回合参与者契约（能量速度系统）：
    /// 每轮由 TurnManager 调用 GainEnergy(GetSpeed()) 积攒能量，
    /// 能量达到阈值后取得行动权并消耗阈值能量，支持不同速度单位的差异化行动频率。
    /// </summary>
    public interface ITurnTaker : IEntity
    {
        /// <summary>当前能量。</summary>
        int Energy { get; }

        /// <summary>每轮能量积攒速度。</summary>
        int GetSpeed();

        void GainEnergy(int amount);

        bool TryConsumeEnergy(int cost);

        /// <summary>取得行动权时被 TurnManager 调用。</summary>
        void OnTurnTaken();

        /// <summary>回合结束观察者事件。</summary>
        event Action<ITurnTaker> OnTurnEnd;
    }
}