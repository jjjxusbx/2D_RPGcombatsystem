namespace TurnBased
{
    /// <summary>可使用道具/技能契约（预留扩展通道，与参考仓库对齐命名）。</summary>
    public interface IUseable : IEntity
    {
        void Use(IEntity user);
    }
}