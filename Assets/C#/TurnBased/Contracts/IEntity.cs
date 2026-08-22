namespace TurnBased
{
    /// <summary>阵营标识：用于索敌与敌我区分。</summary>
    public enum TeamId
    {
        Neutral,
        Player,
        Monster,
    }

    /// <summary>实体基础契约：所有可存在于战斗中的对象。</summary>
    public interface IEntity
    {
        string EntityId { get; }
        string DisplayName { get; }
        TeamId Team { get; }
    }
}