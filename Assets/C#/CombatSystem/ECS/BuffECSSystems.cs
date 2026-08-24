using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ============================================================
// Buff 相关 ECS 系统（Systems）
//
// 职责拆分：
//   - BuffPositionSyncSystem : 把 LocalTransform.Position 同步进 BuffPosition。
//   - BuffManagementSystem     : 倒计时更新 + 到期销毁（创建/绑定由工厂负责）。
//   - TickBuffSystem           : 周期性 tick（持续伤害/治疗）。
//   - AttributeRefreshSystem   : 查询宿主生效修饰器，重算 FinalStats。
//   - AuraSearchSystem         : 光环周期搜索，把自身 buff 套用给范围内目标。
//   - AOETriggerSystem         : 范围伤害/效果，按延迟/重复触发。
//   - BuffSystemGroup          : 统一更新顺序容器。
// ============================================================

namespace Game.ECS.Buff
{
    /// <summary>把 Transform 位置同步进 BuffPosition，供光环/AoE 做范围判定。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BuffPositionSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (lt, pos) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<BuffPosition>>())
            {
                pos.ValueRW.Value = lt.ValueRO.Position;
            }
        }
    }

    /// <summary>Buff 生命周期系统：倒计时 + 到期销毁。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffPositionSyncSystem))]
    public partial class BuffManagementSystem : SystemBase
    {
        readonly List<(Entity Buff, Entity Target)> _expired = new List<(Entity, Entity)>();

        protected override void OnUpdate()
        {
            _expired.Clear();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (buffRef, targetRef, entity) in
                SystemAPI.Query<RefRW<BuffComponent>, RefRO<TargetRef>>().WithEntityAccess())
            {
                ref var buff = ref buffRef.ValueRW;
                if (buff.IsPermanent)
                    continue;

                buff.Duration -= dt;
                if (buff.Duration <= 0f)
                    _expired.Add((entity, targetRef.ValueRO.Value));
            }

            // 结构性销毁必须放在查询循环之外。
            for (int i = 0; i < _expired.Count; i++)
            {
                BuffEcsFactory.DestroyBuff(EntityManager, _expired[i].Buff);
            }
        }
    }

    /// <summary>周期 tick：持续伤害 / 治疗。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffManagementSystem))]
    public partial class TickBuffSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (buffRef, tickRef, targetRef, entity) in
                SystemAPI.Query<RefRW<BuffComponent>, RefRW<TickComponent>, RefRO<TargetRef>>().WithEntityAccess())
            {
                ref var tick = ref tickRef.ValueRW;
                tick.Timer -= dt;
                if (tick.Timer > 0f)
                    continue;
                tick.Timer += tick.Interval;

                Entity target = targetRef.ValueRO.Value;
                if (!EntityManager.Exists(target) || !EntityManager.HasComponent<Health>(target))
                    continue;

                var health = EntityManager.GetComponentData<Health>(target);
                if (tick.DamagePerTick > 0f)
                    health.Value = math.max(0f, health.Value - tick.DamagePerTick);
                if (tick.HealPerTick > 0f)
                    health.Value = math.min(health.Max, health.Value + tick.HealPerTick);
                EntityManager.SetComponentData(target, health);
            }
        }
    }

    /// <summary>属性重算系统：查询宿主生效修饰器表，按五段式重算最终属性。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TickBuffSystem))]
    public partial class AttributeRefreshSystem : SystemBase
    {
        struct Accumulator
        {
            float _base, _flat, _percent;
            float _mult;

            public Accumulator(float baseVal)
            {
                _base = baseVal;
                _flat = 0f;
                _percent = 0f;
                _mult = 1f;
            }

            public void Add(ModifierMode mode, float v)
            {
                switch (mode)
                {
                    case ModifierMode.Flat: _flat += v; break;
                    case ModifierMode.Percent: _percent += v; break;
                    case ModifierMode.Multiply: _mult *= v; break;
                }
            }

            public float Resolve()
            {
                float v = (_base + _flat) * (1f + _percent) * _mult;
                return math.max(0f, v);
            }
        }

        protected override void OnUpdate()
        {
            foreach (var (statsRef, finalRef, mods, entity) in
                SystemAPI.Query<RefRO<UnitStats>, RefRW<FinalStats>, DynamicBuffer<ActiveModifierElement>>().WithEntityAccess())
            {
                var stats = statsRef.ValueRO;

                var atk = new Accumulator(stats.Attack);
                var def = new Accumulator(stats.Defense);
                var spd = new Accumulator(stats.MoveSpeed);
                var hp = new Accumulator(stats.MaxHealth);
                var regen = new Accumulator(stats.HpRegen);

                for (int i = 0; i < mods.Length; i++)
                {
                    var m = mods[i];
                    switch (m.Attribute)
                    {
                        case AttributeKind.Attack: atk.Add(m.Mode, m.Value); break;
                        case AttributeKind.Defense: def.Add(m.Mode, m.Value); break;
                        case AttributeKind.MoveSpeed: spd.Add(m.Mode, m.Value); break;
                        case AttributeKind.MaxHealth: hp.Add(m.Mode, m.Value); break;
                        case AttributeKind.HpRegen: regen.Add(m.Mode, m.Value); break;
                    }
                }

                ref var f = ref finalRef.ValueRW;
                f.Attack = atk.Resolve();
                f.Defense = def.Resolve();
                f.MoveSpeed = spd.Resolve();
                f.MaxHealth = hp.Resolve();
                f.HpRegen = regen.Resolve();

                // 若生命上限被削到低于当前值，夹取生命值。
                if (EntityManager.HasComponent<Health>(entity))
                {
                    var health = EntityManager.GetComponentData<Health>(entity);
                    if (health.Value > f.MaxHealth)
                        health.Value = f.MaxHealth;
                    if (health.Max != f.MaxHealth)
                        health.Max = f.MaxHealth;
                    EntityManager.SetComponentData(entity, health);
                }
            }
        }
    }

    /// <summary>光环系统：周期搜索半径内目标，把光环 Buff 实体克隆套用给目标。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AttributeRefreshSystem))]
    public partial class AuraSearchSystem : SystemBase
    {
        ComponentLookup<BuffPosition> _positions;
        readonly List<Entity> _candidates = new List<Entity>();
        readonly List<(Entity Template, Entity Target)> _pendingApply = new List<(Entity, Entity)>();

        protected override void OnCreate()
        {
            _positions = GetComponentLookup<BuffPosition>(true);
        }

        protected override void OnUpdate()
        {
            _positions.Update(this);
            _pendingApply.Clear();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (auraRef, buffRef, targetRef, entity) in
                SystemAPI.Query<RefRW<AuraComponent>, RefRO<BuffComponent>, RefRO<TargetRef>>().WithEntityAccess())
            {
                ref var aura = ref auraRef.ValueRW;
                aura.SearchTimer -= dt;
                if (aura.SearchTimer > 0f)
                    continue;
                aura.SearchTimer += aura.SearchInterval;

                Entity host = targetRef.ValueRO.Value;
                float3 center = _positions.HasComponent(host) ? _positions[host].Value : float3.zero;
                int hash = buffRef.ValueRO.BuffIdHash;

                _candidates.Clear();
                foreach (var (unitPos, unitEntity) in SystemAPI.Query<RefRO<BuffPosition>>().WithEntityAccess())
                {
                    // 简化：光环通常不影响施放者本身。
                    if (unitEntity == host)
                        continue;
                    if (math.distance(unitPos.ValueRO.Value, center) > aura.Radius)
                        continue;
                    // 已带该 buff 的目标不重复套用（避免叠层爆炸）。
                    if (BuffEcsFactory.HasBuffOnTarget(EntityManager, unitEntity, hash))
                        continue;
                    _candidates.Add(unitEntity);
                }

                for (int i = 0; i < _candidates.Count; i++)
                    _pendingApply.Add((entity, _candidates[i]));
            }

            // 结构变化（CloneBuffOntoTarget 会创建实体并改宿主 buffer）延后到查询外执行。
            for (int i = 0; i < _pendingApply.Count; i++)
            {
                BuffEcsFactory.CloneBuffOntoTarget(EntityManager, _pendingApply[i].Template, _pendingApply[i].Target);
            }
        }
    }

    /// <summary>范围效果系统：按延迟 / 重复触发范围伤害。</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AuraSearchSystem))]
    public partial class AOETriggerSystem : SystemBase
    {
        ComponentLookup<BuffPosition> _positions;
        ComponentLookup<FinalStats> _stats;
        ComponentLookup<Health> _health;

        protected override void OnCreate()
        {
            _positions = GetComponentLookup<BuffPosition>(true);
            _stats = GetComponentLookup<FinalStats>(true);
            _health = GetComponentLookup<Health>(false);
        }

        protected override void OnUpdate()
        {
            _positions.Update(this);
            _stats.Update(this);
            _health.Update(this);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (aoeRef, buffRef, targetRef, entity) in
                SystemAPI.Query<RefRW<AOEEffectComponent>, RefRO<BuffComponent>, RefRO<TargetRef>>().WithEntityAccess())
            {
                ref var aoe = ref aoeRef.ValueRW;
                bool fire = false;

                if (aoe.Delay > 0f)
                {
                    aoe.DelayTimer -= dt;
                    if (aoe.DelayTimer <= 0f)
                    {
                        fire = true;
                        aoe.DelayTimer = aoe.Repeat ? aoe.RepeatInterval : float.MaxValue;
                    }
                }
                else if (aoe.Repeat)
                {
                    aoe.RepeatTimer -= dt;
                    if (aoe.RepeatTimer <= 0f)
                    {
                        fire = true;
                        aoe.RepeatTimer += aoe.RepeatInterval;
                    }
                }
                else
                {
                    fire = true; // 无延迟且不重复 -> 立即一次性触发
                }

                if (!fire)
                    continue;

                Entity host = targetRef.ValueRO.Value;
                float3 center = _positions.HasComponent(host) ? _positions[host].Value : float3.zero;

                float attack = 1f;
                Entity caster = buffRef.ValueRO.Source;
                if (caster != Entity.Null && _stats.HasComponent(caster))
                    attack = _stats[caster].Attack;
                float damage = attack * aoe.DamageCoefficient;

                foreach (var (unitPos, unitEntity) in SystemAPI.Query<RefRO<BuffPosition>>().WithEntityAccess())
                {
                    if (unitEntity == host || !_health.HasComponent(unitEntity))
                        continue;

                    float3 p = unitPos.ValueRO.Value;
                    bool inRange = aoe.Shape == AoeShape.Circle
                        ? math.distance(p, center) <= aoe.Radius
                        : (math.abs(p.x - center.x) <= aoe.Width && math.abs(p.y - center.y) <= aoe.Height);

                    if (!inRange)
                        continue;

                    var h = _health[unitEntity];
                    h.Value = math.max(0f, h.Value - damage);
                    _health[unitEntity] = h;
                }
            }
        }
    }
}
