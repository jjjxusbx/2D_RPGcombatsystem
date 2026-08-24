using Unity.Entities;

// ============================================================
// ECS 查询过滤器（Queries）
//
// 用于区分三种 Buff 实体的典型查询写法。既提供“基于组件存在性”的
// EntityQuery 构造（适合需要 ToEntityArray / 统计的场景），也提供
// 基于 SystemAPI.Query 的 LINQ 式写法（适合系统内直接遍历）。
//
// 核心思想：通过“== 是否拥有某个组件 ==”来给 Buff 分型，而不是靠继承。
//   普通 Buff  : 只含 BuffComponent（+ TargetRef）。
//   光环 Buff  : 额外含 AuraComponent。
//   AoE  Buff  : 额外含 AOEEffectComponent。
//   Tick Buff  : 额外含 TickComponent。
// ============================================================

namespace Game.ECS.Buff
{
    public static class BuffQueries
    {
        // ---------- EntityQuery 式（通用、可用于任意系统） ----------

        /// <summary>全部 Buff 实体（必须拥有 BuffComponent 与 TargetRef）。</summary>
        public static EntityQuery AllBuffs(EntityManager em)
            => em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>());

        /// <summary>普通 Buff（有 BuffComponent，但不是光环、不是 AoE、不是 Tick）。</summary>
        public static EntityQuery PlainBuffs(EntityManager em)
            => em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>(),
                ComponentType.Exclude<AuraComponent>(),
                ComponentType.Exclude<AOEEffectComponent>(),
                ComponentType.Exclude<TickComponent>());

        /// <summary>带光环的 Buff（有 AuraComponent）。</summary>
        public static EntityQuery AuraBuffs(EntityManager em)
            => em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>(),
                ComponentType.ReadOnly<AuraComponent>());

        /// <summary>带 AoE 的 Buff（有 AOEEffectComponent）。</summary>
        public static EntityQuery AoeBuffs(EntityManager em)
            => em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>(),
                ComponentType.ReadOnly<AOEEffectComponent>());

        /// <summary>带周期 tick 的 Buff（有 TickComponent）。</summary>
        public static EntityQuery TickBuffs(EntityManager em)
            => em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>(),
                ComponentType.ReadOnly<TickComponent>());

        /// <summary>所有可被 Buff 的宿主单位。</summary>
        public static EntityQuery AllUnits(EntityManager em)
            => em.CreateEntityQuery(ComponentType.ReadOnly<UnitTag>());

        // ---------- SystemAPI.Query 式 ----------

        // 遍历所有 Buff 实体：需要同时拿到实体 + 组件：
        //   foreach (var (buff, target, entity) in SystemAPI.Query<RefRW<BuffComponent>, RefRO<TargetRef>>().WithEntityAccess())
        //
        // 仅遍历光环 Buff：
        //   foreach (var (buff, aura, entity) in SystemAPI.Query<RefRW<BuffComponent>, RefRO<AuraComponent>>().WithEntityAccess())
        //
        // 仅遍历 AoE Buff：
        //   foreach (var (buff, aoe, entity) in SystemAPI.Query<RefRW<BuffComponent>, RefRO<AOEEffectComponent>>().WithEntityAccess())
        //
        // 仅遍历 Tick Buff：
        //   foreach (var (buff, tick, entity) in SystemAPI.Query<RefRW<BuffComponent>, RefRO<TickComponent>>().WithEntityAccess())
        //
        // 若更偏好“标记组件”而非具名组件，也可用 WithAll<>：
        //   SystemAPI.Query<RefRW<BuffComponent>>().WithAll<AuraBuffTag>()
    }
}
