using System;

namespace TurnBased
{
    /// <summary>可受伤实体契约（观察者事件 OnTakeDamage，与参考仓库对齐命名）。</summary>
    public interface IDamageable : IEntity
    {
        int MaxHealth { get; }
        int Health { get; }
        bool IsDead { get; }

        /// <summary>受伤事件：目标、伤害量、伤害来源。</summary>
        event Action<IDamageable, int, IEntity> OnTakeDamage;

        void TakeDamage(int amount, IEntity source);
    }
}