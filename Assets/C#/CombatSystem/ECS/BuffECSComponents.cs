using Unity.Entities;
using Unity.Mathematics;

// ============================================================
// 战斗属性改造 · ECS 组件定义（Unity DOTS / Entities 1.x）
//
// 核心建模：一个 Buff = World 中的一个独立 Entity。
//   - 每个 Buff 实体至少拥有 BuffComponent（唯一标识 + 基础字段）。
//   - 若它同时是光环  -> 追加 AuraComponent。
//   - 若它同时是 AoE   -> 追加 AOEEffectComponent。
//   - 若它需要周期性 tick -> 追加 TickComponent。
// 禁止通过继承扩展 Buff；一切额外能力通过“加组件”实现。
//
// 属性改造逻辑：BuffComponent 只存“修正常数”，真正的最终属性重算
// 由 AttributeRefreshSystem 查询所有绑定宿主的 Buff 实体完成。
// ============================================================

namespace Game.ECS.Buff
{
    /// <summary>Buff 类型。</summary>
    public enum BuffKind : byte
    {
        None = 0,
        Buff,       // 增益
        Debuff,     // 减益
        Duration,   // 带持续时长
        Permanent,  // 永久
    }

    /// <summary>可被 Buff 修改的属性。</summary>
    public enum AttributeKind : byte
    {
        Attack = 0,
        Defense,
        MoveSpeed,
        MaxHealth,
        HpRegen,
    }

    /// <summary>修饰器叠加方式（与 ChaState 五段式一致）。</summary>
    public enum ModifierMode : byte
    {
        Flat = 0,       // 固定值加算
        Percent,        // 百分比加算
        Multiply,       // 乘算
    }

    /// <summary>光环 / AoE 的目标筛选。</summary>
    public enum TargetFilter : byte
    {
        Self = 0,
        Friendly,
        Enemy,
        All,
    }

    /// <summary>AoE 形状。</summary>
    public enum AoeShape : byte
    {
        Circle = 0,
        Box,
    }

    // --------------------------------------------------------
    // 宿主（单位）相关组件
    // --------------------------------------------------------

    /// <summary>宿主标记：可用于查询“所有可被 Buff 的单位”。</summary>
    public struct UnitTag : IComponentData { }

    /// <summary>宿主在场景中的位置（供光环 / AoE 做距离与范围判定）。</summary>
    public struct BuffPosition : IComponentData
    {
        public float3 Value;
    }

    /// <summary>宿主基础属性（Buff 施加前的原始值）。</summary>
    public struct UnitStats : IComponentData
    {
        public float Attack;
        public float Defense;
        public float MoveSpeed;
        public float MaxHealth;
        public float HpRegen;
    }

    /// <summary>宿主最终属性（由 AttributeRefreshSystem 每帧从修饰器重算）。</summary>
    public struct FinalStats : IComponentData
    {
        public float Attack;
        public float Defense;
        public float MoveSpeed;
        public float MaxHealth;
        public float HpRegen;
    }

    /// <summary>生命值（AoE / Tick 直接在此结算）。</summary>
    public struct Health : IComponentData
    {
        public float Value;
        public float Max;
    }

    // --------------------------------------------------------
    // Buff 实体相关组件
    // --------------------------------------------------------

    /// <summary>
    /// Buff 实体的“唯一标识”，任何 Buff 实体都必须拥有。
    /// 只负责描述 buff 本体与绑定关系，不携带行为实现。
    /// </summary>
    public struct BuffComponent : IComponentData
    {
        public BuffKind Kind;
        public int BuffIdHash;        // 用于在宿主上做“同 buff 叠层”判定
        public float Duration;        // 剩余时长（<=0 且非永久则到期销毁）
        public float MaxDuration;     // 初始时长
        public float TickInterval;    // 基础 tick 间隔（若存在 TickComponent 以其为准）
        public int StackCount;
        public int MaxStacks;
        public bool IsPermanent;
        public int ExclusiveGroup;    // 互斥组 ID（0 = 不互斥）
        public Entity Source;         // 施加者（optional, Entity.Null 表示无）
    }

    /// <summary>
    /// 宿主绑定：把 Buff 实体链接到它作用的目标单位。
    /// </summary>
    public struct TargetRef : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// 光环：周期搜索半径内目标并套用自身 buff。
    /// </summary>
    public struct AuraComponent : IComponentData
    {
        public float Radius;
        public float SearchInterval;
        public float SearchTimer;
        public TargetFilter Filter;
        public int LayerMask;          // 物理 Layer，0 = 不过滤
        public int MaxAffected;        // 0 = 不限
    }

    /// <summary>
    /// 范围效果：在延迟后 / 重复触发时对范围内单位造成伤害（或施加效果）。
    /// </summary>
    public struct AOEEffectComponent : IComponentData
    {
        public AoeShape Shape;
        public float Radius;           // Circle 半径
        public float Width;            // Box 半宽
        public float Height;           // Box 半高
        public TargetFilter Filter;
        public int LayerMask;
        public float Delay;            // 首段延迟
        public float DelayTimer;
        public bool Repeat;
        public float RepeatInterval;
        public float RepeatTimer;
        public float DamageCoefficient; // 伤害 = caster 攻击 * 系数
    }

    /// <summary>周期 tick（持续伤害/治疗）。</summary>
    public struct TickComponent : IComponentData
    {
        public float Interval;
        public float Timer;
        public float DamagePerTick;    // >0 表示每次 tick 造成的伤害
        public float HealPerTick;      // >0 表示每次 tick 治疗
    }

    // --------------------------------------------------------
    // 标记组件：用于查询区分“普通 / 光环 / AoE / Tick” Buff 实体
    // --------------------------------------------------------

    public struct AuraBuffTag : IComponentData { }
    public struct AoeBuffTag : IComponentData { }
    public struct TickBuffTag : IComponentData { }

    // --------------------------------------------------------
    // Buffer（IBufferElementData）
    // --------------------------------------------------------

    /// <summary>
    /// 存放在“Buff 实体”上的修饰器列表（每一条是一个属性修正）。
    /// 数值按层数缩放，由管理/工厂写入。
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct BuffModifierElement : IBufferElementData
    {
        public AttributeKind Attribute;
        public ModifierMode Mode;
        public float ValuePerStack;
    }

    /// <summary>
    /// 存放在“宿主单位”上的当前生效修饰器总表。
    /// AttributeRefreshSystem 扫描它来重算 FinalStats。
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct ActiveModifierElement : IBufferElementData
    {
        public Entity SourceBuff;      // 来源 Buff 实体（用于精确回收）
        public AttributeKind Attribute;
        public ModifierMode Mode;
        public float Value;            // 已按层数折算后的最终值
    }
}
