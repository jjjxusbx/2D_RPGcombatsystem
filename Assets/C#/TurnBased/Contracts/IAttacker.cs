namespace TurnBased
{
    /// <summary>可发起攻击的实体契约。</summary>
    public interface IAttacker : IEntity
    {
        int AttackDamage { get; }
        int AttackRange { get; }
        bool CanAttack(IDamageable target);
    }
}