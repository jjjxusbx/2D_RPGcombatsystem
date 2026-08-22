namespace TurnBased
{
    /// <summary>
    /// 网格实体契约：可被放置进 GridMap 的实体，持有格子坐标。
    /// 格子坐标与实际 Transform 的同步由具体实现（TurnUnit）保证。
    /// </summary>
    public interface IGridEntity : IEntity
    {
        GridPosition GridPosition { get; }
        bool IsAlive { get; }
    }
}