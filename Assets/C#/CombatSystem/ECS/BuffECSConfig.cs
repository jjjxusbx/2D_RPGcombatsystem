using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ============================================================
// Buff 配置 + 运行时工厂
//
// BuffConfigData 是一个“可序列化的创建描述”，仅在运行时用于构造
// 真正的 ECS Buff 实体。它不会被写进任何 IComponentData（组件必须
// 是 unmanaged），因此与 DOTS 的 blittable 约束不冲突。
//
// BuffEcsFactory 负责：
//   - 根据配置创建 Buff 实体（唯一标识 + 绑定宿主 + 附加光环/AoE/Tick 组件）。
//   - 处理叠层与互斥组。
//   - 维护“宿主生效修饰器表”(ActiveModifierElement)，供 AttributeRefreshSystem 重算。
// ============================================================

namespace Game.ECS.Buff
{
    [Serializable]
    public class ModifierData
    {
        public AttributeKind attribute;
        public ModifierMode mode;
        public float value; // 每层数值
    }

    [Serializable]
    public class AuraData
    {
        public float radius = 3f;
        public float searchInterval = 0.5f;
        public TargetFilter filter = TargetFilter.Enemy;
        public int layerMask;
        public int maxAffected;
    }

    [Serializable]
    public class AoeData
    {
        public AoeShape shape = AoeShape.Circle;
        public float radius = 3f;
        public float width = 2f;
        public float height = 2f;
        public TargetFilter filter = TargetFilter.Enemy;
        public int layerMask;
        public float delay;
        public bool repeat;
        public float repeatInterval = 1f;
        public float damageCoefficient = 1f;
    }

    [Serializable]
    public class TickData
    {
        public float interval = 1f;
        public float damagePerTick;
        public float healPerTick;
    }

    [Serializable]
    public class BuffConfigData
    {
        public string buffId;
        public BuffKind kind;
        public float duration;
        public float tickInterval;
        public int maxStacks = 1;
        public bool isPermanent;
        public int exclusiveGroup;
        public List<ModifierData> modifiers = new List<ModifierData>();
        public AuraData aura;
        public AoeData aoe;
        public TickData tick;

        public int Hash => string.IsNullOrEmpty(buffId) ? 0 : buffId.GetHashCode();
    }

    /// <summary>运行时 Buff 实体工厂：创建 / 克隆 / 回收，并维护宿主修饰器表。</summary>
    public static class BuffEcsFactory
    {
        // ---------- 宿主修饰器表的维护 ----------

        /// <summary>把一个 Buff 实体的修饰器（按层数折算后）写入宿主表。</summary>
        static void ApplyModifiersToHost(EntityManager em, Entity host, Entity buffEntity,
            int stackCount, DynamicBuffer<BuffModifierElement> buffMods)
        {
            var hostBuf = em.GetBuffer<ActiveModifierElement>(host);
            for (int i = 0; i < buffMods.Length; i++)
            {
                var m = buffMods[i];
                hostBuf.Add(new ActiveModifierElement
                {
                    SourceBuff = buffEntity,
                    Attribute = m.Attribute,
                    Mode = m.Mode,
                    Value = m.ValuePerStack * stackCount,
                });
            }
        }

        /// <summary>从宿主表移除某个 Buff 实体的全部修饰器（逆序逐个移除，保证精确）。</summary>
        public static void RemoveModifiersFromHost(EntityManager em, Entity host, Entity buffEntity)
        {
            if (!em.Exists(host) || !em.HasBuffer<ActiveModifierElement>(host))
                return;

            var hostBuf = em.GetBuffer<ActiveModifierElement>(host);
            for (int i = hostBuf.Length - 1; i >= 0; i--)
            {
                if (hostBuf[i].SourceBuff == buffEntity)
                    hostBuf.RemoveAt(i);
            }
        }

        // ---------- 创建 ----------

        /// <summary>根据配置在 target 上创建一个新的 Buff 实体。</summary>
        public static Entity CreateBuff(EntityManager em, in BuffConfigData cfg, Entity target, Entity source)
        {
            int hash = cfg.Hash;
            int stacks = 1;

            // 1) 互斥组：先移除 target 上同组的其它 Buff。
            if (cfg.exclusiveGroup != 0)
            {
                var q = em.CreateEntityQuery(
                    ComponentType.ReadWrite<BuffComponent>(),
                    ComponentType.ReadOnly<TargetRef>());
                var ents = q.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < ents.Length; i++)
                {
                    var b = em.GetComponentData<BuffComponent>(ents[i]);
                    var t = em.GetComponentData<TargetRef>(ents[i]).Value;
                    if (t == target && b.ExclusiveGroup == cfg.exclusiveGroup && b.BuffIdHash != hash)
                        DestroyBuff(em, ents[i]);
                }
                ents.Dispose();
            }

            // 2) 同 ID 叠层：找到已存在的同 buff，只刷新层数与时长。
            Entity? existing = FindBuffOnTarget(em, target, hash);
            if (existing.HasValue)
            {
                var buffEntity = existing.Value;
                var buff = em.GetComponentData<BuffComponent>(buffEntity);
                if (!buff.IsPermanent)
                    buff.Duration = cfg.duration;
                buff.StackCount = Math.Min(buff.StackCount + 1, buff.MaxStacks > 0 ? buff.MaxStacks : 1);
                em.SetComponentData(buffEntity, buff);

                // 刷新宿主修饰器表：先清旧再按新层数写入。
                RemoveModifiersFromHost(em, target, buffEntity);
                var buffMods = em.GetBuffer<BuffModifierElement>(buffEntity);
                ApplyModifiersToHost(em, target, buffEntity, buff.StackCount, buffMods);
                return buffEntity;
            }

            // 3) 没有同 ID -> 新建 Buff 实体。
            var created = em.CreateEntity();
            em.SetName(created, cfg.buffId ?? "Buff");

            em.AddComponentData(created, new BuffComponent
            {
                Kind = cfg.kind,
                BuffIdHash = hash,
                Duration = cfg.isPermanent ? float.MaxValue : cfg.duration,
                MaxDuration = cfg.duration,
                TickInterval = cfg.tickInterval,
                StackCount = stacks,
                MaxStacks = cfg.maxStacks > 0 ? cfg.maxStacks : 1,
                IsPermanent = cfg.isPermanent,
                ExclusiveGroup = cfg.exclusiveGroup,
                Source = source,
            });
            em.AddComponentData(created, new TargetRef { Value = target });

            // 修饰器 buffer
            var mods = em.AddBuffer<BuffModifierElement>(created);
            if (cfg.modifiers != null)
            {
                for (int i = 0; i < cfg.modifiers.Count; i++)
                {
                    var d = cfg.modifiers[i];
                    mods.Add(new BuffModifierElement
                    {
                        Attribute = d.attribute,
                        Mode = d.mode,
                        ValuePerStack = d.value,
                    });
                }
            }
            ApplyModifiersToHost(em, target, created, stacks, mods);

            // 光环 / AoE / Tick 作为“附加组件”组合上去
            if (cfg.aura != null)
            {
                em.AddComponentData(created, new AuraComponent
                {
                    Radius = cfg.aura.radius,
                    SearchInterval = cfg.aura.searchInterval,
                    SearchTimer = cfg.aura.searchInterval,
                    Filter = cfg.aura.filter,
                    LayerMask = cfg.aura.layerMask,
                    MaxAffected = cfg.aura.maxAffected,
                });
                em.AddComponentData(created, new AuraBuffTag());
            }
            if (cfg.aoe != null)
            {
                em.AddComponentData(created, new AOEEffectComponent
                {
                    Shape = cfg.aoe.shape,
                    Radius = cfg.aoe.radius,
                    Width = cfg.aoe.width,
                    Height = cfg.aoe.height,
                    Filter = cfg.aoe.filter,
                    LayerMask = cfg.aoe.layerMask,
                    Delay = cfg.aoe.delay,
                    DelayTimer = cfg.aoe.delay,
                    Repeat = cfg.aoe.repeat,
                    RepeatInterval = cfg.aoe.repeatInterval,
                    RepeatTimer = cfg.aoe.repeatInterval,
                    DamageCoefficient = cfg.aoe.damageCoefficient,
                });
                em.AddComponentData(created, new AoeBuffTag());
            }
            if (cfg.tick != null)
            {
                em.AddComponentData(created, new TickComponent
                {
                    Interval = cfg.tick.interval,
                    Timer = cfg.tick.interval,
                    DamagePerTick = cfg.tick.damagePerTick,
                    HealPerTick = cfg.tick.healPerTick,
                });
                em.AddComponentData(created, new TickBuffTag());
            }

            return created;
        }

        // ---------- 光环：把“模板 Buff 实体”克隆到 target ----------

        /// <summary>
        /// 光环找到范围内新目标时，把光环 Buff 实体当作模板，克隆一份到 target 上。
        /// 这样光环/AoE 上的行为（修饰器/Tick/时长）会原样传到目标，实现“套用自身 buff”。
        /// </summary>
        public static Entity CloneBuffOntoTarget(EntityManager em, Entity templateBuff, Entity target)
        {
            if (!em.Exists(templateBuff) || !em.Exists(target))
                return Entity.Null;

            var template = em.GetComponentData<BuffComponent>(templateBuff);
            var created = em.CreateEntity();
            em.SetName(created, "AuraApplied:" + template.BuffIdHash);

            var newBuff = template;
            newBuff.StackCount = 1;
            newBuff.Duration = template.IsPermanent ? float.MaxValue : template.Duration;
            if (template.Source == Entity.Null)
                newBuff.Source = templateBuff;
            em.AddComponentData(created, newBuff);
            em.AddComponentData(created, new TargetRef { Value = target });

            // 复制修饰器 buffer
            if (em.HasBuffer<BuffModifierElement>(templateBuff))
            {
                var srcMods = em.GetBuffer<BuffModifierElement>(templateBuff);
                var dstMods = em.AddBuffer<BuffModifierElement>(created);
                dstMods.CopyFrom(srcMods.AsNativeArray());
                ApplyModifiersToHost(em, target, created, 1, dstMods);
            }

            // 复制 Tick（光环施加持续效果时常用）
            if (em.HasComponent<TickComponent>(templateBuff))
            {
                em.AddComponentData(created, em.GetComponentData<TickComponent>(templateBuff));
                em.AddComponentData(created, new TickBuffTag());
            }

            return created;
        }

        // ---------- 回收 ----------

        /// <summary>销毁 Buff 实体并回收其对宿主修饰器表的影响。</summary>
        public static void DestroyBuff(EntityManager em, Entity buffEntity)
        {
            if (!em.Exists(buffEntity))
                return;

            if (em.HasComponent<TargetRef>(buffEntity))
            {
                var target = em.GetComponentData<TargetRef>(buffEntity).Value;
                RemoveModifiersFromHost(em, target, buffEntity);
            }

            em.DestroyEntity(buffEntity);
        }

        // ---------- 查询辅助 ----------

        static Entity? FindBuffOnTarget(EntityManager em, Entity target, int buffHash)
        {
            var q = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuffComponent>(),
                ComponentType.ReadOnly<TargetRef>());
            var ents = q.ToEntityArray(Allocator.Temp);
            Entity? found = null;
            for (int i = 0; i < ents.Length; i++)
            {
                var b = em.GetComponentData<BuffComponent>(ents[i]);
                var t = em.GetComponentData<TargetRef>(ents[i]).Value;
                if (t == target && b.BuffIdHash == buffHash)
                {
                    found = ents[i];
                    break;
                }
            }
            ents.Dispose();
            return found;
        }

        /// <summary>判断 target 是否已经拥有指定 hash 的 Buff（光环防重复套用用）。</summary>
        public static bool HasBuffOnTarget(EntityManager em, Entity target, int buffHash)
            => FindBuffOnTarget(em, target, buffHash).HasValue;
    }
}


