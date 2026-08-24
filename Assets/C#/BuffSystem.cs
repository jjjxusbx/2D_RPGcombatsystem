using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 战斗属性 · Buff 实体 + 组件（ECS 风格组合）
//
// 说明：本模块把 Buff 建模为「实体 + 组件」的组合模型。
//   - 实体（BuffEntity）：一个正在生效的 Buff 实例，拥有若干 Buff 组件。
//   - 组件（BuffComponent）：可复用的行为片段。一个 Buff 最少拥有「基础」行为
//     （施加属性修饰器 / 持续伤害 / 时长）；若它同时是光环，就追加 BuffAuraComponent；
//     若它同时是范围效果，就追加 BuffAoeComponent。
//   好处：新增行为（光环、AoE、未来的词条等）不再往 BuffData 里堆字段，
//   而是「给该 Buff 添加组件」。组件按实体克隆，多宿主互不共享运行时状态。
//
// 本层只做 Buff 组合与生命周期；属性数值与统一伤害管线仍由 Attribute / ChaState 承担。
// ============================================================

/// <summary>
/// 一个正在生效的 Buff 实体。它由若干 BuffComponent 组合而成：
/// 基础组件负责把修饰器施加到宿主属性，可选的光环/AoE 组件负责影响周围目标。
/// 生命周期由 BuffController（系统）统一调度：Tick → 到期 → DetachAll 回收副作用。
/// </summary>
public sealed class BuffEntity
{
    public string buffId;
    public ChaState host;          // 宿主属性容器，用于施加/回收修饰器
    public int stacks = 1;
    public float remaining;        // 剩余时长（由 BuffData.duration 驱动）
    public bool expired;           // 组件可标记提前终止
    public string exclusiveGroup;  // 互斥组（来自 BuffData），供系统定向移除

    private readonly List<BuffComponent> _components = new List<BuffComponent>();

    public IReadOnlyList<BuffComponent> Components => _components;

    public void AddComponent(BuffComponent component)
    {
        if (component != null)
        {
            _components.Add(component);
        }
    }

    /// <summary>附加全部组件：组件把自身行为落地到宿主（如施加修饰器）。</summary>
    public void AttachAll()
    {
        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].OnAttach(this);
        }
    }

    /// <summary>摘除全部组件（逆序）：回收组件施加到宿主的副作用（如移除修饰器），避免属性残留。</summary>
    public void DetachAll()
    {
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            _components[i].OnDetach(this);
        }
    }

    /// <summary>每帧推进：把 deltaTime 分发给各组件（持续伤害 / 光环脉冲 / AoE 重复等）。</summary>
    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].OnTick(this, deltaTime);
        }
    }

    public T GetComponent<T>() where T : BuffComponent
    {
        for (int i = 0; i < _components.Count; i++)
        {
            if (_components[i] is T typed)
            {
                return typed;
            }
        }

        return null;
    }
}

/// <summary>
/// Buff 组件基类：一个可组合、可复用的行为片段。
/// 每个实体持有自己的组件实例（自配置克隆），因此同一 BuffData 施加到多个宿主时，
/// 运行时状态（如持续伤害计时、光环脉冲计时）互不干扰。
/// </summary>
[Serializable]
public abstract class BuffComponent
{
    /// <summary>组件附加到实体时调用（落地行为）。</summary>
    public abstract void OnAttach(BuffEntity entity);

    /// <summary>组件从实体摘除时调用（回收副作用）。</summary>
    public abstract void OnDetach(BuffEntity entity);

    /// <summary>每帧推进（可选）。</summary>
    public virtual void OnTick(BuffEntity entity, float deltaTime)
    {
    }

    /// <summary>克隆一份运行时组件实例（配置可作为原型）。</summary>
    public abstract BuffComponent Clone();
}

/// <summary>
/// 基础 Buff 组件：把一组 AttributeModifier 定向施加到宿主属性。
/// 通过 source=buffId 保证到期/移除时能精确回收，不残留加成。
/// </summary>
[Serializable]
public sealed class BuffStatComponent : BuffComponent
{
    public List<AttributeModifier> modifiers = new List<AttributeModifier>();

    public override void OnAttach(BuffEntity entity)
    {
        if (entity?.host == null || modifiers == null)
        {
            return;
        }

        foreach (AttributeModifier mod in modifiers)
        {
            if (mod == null || string.IsNullOrEmpty(mod.statId))
            {
                continue;
            }

            mod.source = entity.buffId;
            entity.host.ApplyModifier(mod);
        }
    }

    public override void OnDetach(BuffEntity entity)
    {
        entity?.host?.RemoveModifiersFromSource(entity.buffId);
    }

    public override BuffComponent Clone()
    {
        BuffStatComponent copy = new BuffStatComponent();
        if (modifiers == null)
        {
            return copy;
        }

        foreach (AttributeModifier mod in modifiers)
        {
            if (mod == null)
            {
                continue;
            }

            copy.modifiers.Add(new AttributeModifier(mod.statId, mod.type, mod.value));
        }

        return copy;
    }
}

/// <summary>
/// 持续伤害组件：每满 1 秒对宿主走一次 TakeDamage（小额伤害，表现层负责过滤受击动画）。
/// </summary>
[Serializable]
public sealed class BuffDotComponent : BuffComponent
{
    public float damagePerSecond;

    private float _accumulator;

    public override void OnAttach(BuffEntity entity)
    {
        _accumulator = 0f;
    }

    public override void OnDetach(BuffEntity entity)
    {
        _accumulator = 0f;
    }

    public override void OnTick(BuffEntity entity, float deltaTime)
    {
        if (damagePerSecond <= 0f || entity?.host == null || !entity.host.IsAlive)
        {
            return;
        }

        _accumulator += deltaTime;
        while (_accumulator >= 1f)
        {
            _accumulator -= 1f;
            entity.host.TakeDamage(damagePerSecond);
        }
    }

    public override BuffComponent Clone()
    {
        return new BuffDotComponent { damagePerSecond = damagePerSecond };
    }
}

/// <summary>
/// 光环组件：以宿主为中心，按 interval 周期性把 payload（MagicEffectData）施加到半径内目标。
/// 若一个 Buff 同时是光环，就给它追加本组件（例如「战鼓」光环周期给盟友 +攻击 buff）。
/// </summary>
[Serializable]
public sealed class BuffAuraComponent : BuffComponent
{
    public float radius = 3f;
    public float interval = 1f;
    public LayerMask targetLayers = ~0;
    public MagicEffectData payload;

    private float _timer;

    public override void OnAttach(BuffEntity entity)
    {
        _timer = interval; // 附加后延后一个周期再首次脉冲，避免立即刷屏
    }

    public override void OnDetach(BuffEntity entity)
    {
        _timer = 0f;
    }

    public override void OnTick(BuffEntity entity, float deltaTime)
    {
        if (entity?.host == null || payload == null || interval <= 0f)
        {
            return;
        }

        _timer += deltaTime;
        if (_timer < interval)
        {
            return;
        }

        _timer = 0f;

        Vector2 center = entity.host.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            payload.Apply(entity.host.gameObject, hits[i].gameObject);
        }
    }

    public override BuffComponent Clone()
    {
        return new BuffAuraComponent
        {
            radius = radius,
            interval = interval,
            targetLayers = targetLayers,
            payload = payload
        };
    }
}

/// <summary>
/// 范围效果组件：施加时（以及可选按 interval 重复）以宿主为中心，命中半径内所有目标。
/// 若一个 Buff 同时是范围效果，就给它追加本组件（例如「毒雾」施加时覆盖整片区域）。
/// </summary>
[Serializable]
public sealed class BuffAoeComponent : BuffComponent
{
    public float radius = 2f;
    public LayerMask targetLayers = ~0;
    public MagicEffectData payload;
    public bool repeat;
    public float interval = 1f;

    private float _timer;

    public override void OnAttach(BuffEntity entity)
    {
        HitArea(entity);
        _timer = 0f;
    }

    public override void OnDetach(BuffEntity entity)
    {
        _timer = 0f;
    }

    public override void OnTick(BuffEntity entity, float deltaTime)
    {
        if (!repeat || entity?.host == null || payload == null || interval <= 0f)
        {
            return;
        }

        _timer += deltaTime;
        if (_timer < interval)
        {
            return;
        }

        _timer = 0f;
        HitArea(entity);
    }

    private void HitArea(BuffEntity entity)
    {
        if (entity?.host == null || payload == null)
        {
            return;
        }

        Vector2 center = entity.host.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            payload.Apply(entity.host.gameObject, hits[i].gameObject);
        }
    }

    public override BuffComponent Clone()
    {
        return new BuffAoeComponent
        {
            radius = radius,
            targetLayers = targetLayers,
            payload = payload,
            repeat = repeat,
            interval = interval
        };
    }
}

/// <summary>
/// Buff 系统（宿主上挂载）：负责 Buff 实体的创建、堆叠、互斥与到期回收。
/// 它不直接知道「某 Buff 是光环还是 AoE」——那是 BuffData.components 里追加的组件决定的。
/// </summary>
public class BuffController : MonoBehaviour
{
    private readonly List<BuffEntity> activeBuffs = new List<BuffEntity>();

    public IReadOnlyList<BuffEntity> ActiveBuffs => activeBuffs;

    private void Update()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffEntity entity = activeBuffs[i];
            if (entity == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

            entity.remaining -= Time.deltaTime;
            entity.Tick(Time.deltaTime);

            if (entity.remaining <= 0f || entity.expired)
            {
                Dispose(entity);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 施加一个 Buff：按 BuffData 配置构建 Buff 实体并附加组件。
    /// 若是光环/AoE，BuffData.components 里已含有对应组件，这里无需特判。
    /// </summary>
    public void Apply(BuffData data)
    {
        if (data == null)
        {
            return;
        }

        RemoveExclusiveBuffs(data);

        BuffEntity existing = Find(data.buffId);
        if (existing == null)
        {
            BuffEntity entity = CreateEntity(data);
            activeBuffs.Add(entity);
            return;
        }

        // 重复施加：先摘除旧组件（回收副作用，避免同一 buffId 的修饰器重复叠加），
        // 再按堆叠规则更新并重新落地。
        existing.DetachAll();

        switch (data.stackRule)
        {
            case BuffStackRule.Replace:
                existing.stacks = 1;
                existing.remaining = data.duration;
                break;
            case BuffStackRule.AddStack:
                existing.stacks = Mathf.Min(existing.stacks + 1, data.maxStacks);
                existing.remaining = data.duration;
                break;
            case BuffStackRule.RefreshDuration:
                existing.remaining = data.duration;
                break;
            case BuffStackRule.Ignore:
                break;
        }

        existing.expired = false;
        existing.AttachAll();
    }

    public bool Has(string buffId)
    {
        return Find(buffId) != null;
    }

    private BuffEntity CreateEntity(BuffData data)
    {
        BuffEntity entity = new BuffEntity
        {
            buffId = data.buffId,
            host = GetComponent<ChaState>(),
            remaining = data.duration,
            stacks = 1,
            exclusiveGroup = data.exclusiveGroup
        };

        if (data.components != null)
        {
            for (int i = 0; i < data.components.Count; i++)
            {
                if (data.components[i] != null)
                {
                    entity.AddComponent(data.components[i].Clone());
                }
            }
        }

        entity.AttachAll();
        return entity;
    }

    /// <summary>回收实体：摘除全部组件，使施加到宿主的副作用（属性/计时）无残留。</summary>
    private void Dispose(BuffEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        entity.DetachAll();
    }

    private BuffEntity Find(string buffId)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buffId == buffId)
            {
                return activeBuffs[i];
            }
        }

        return null;
    }

    private void RemoveExclusiveBuffs(BuffData incoming)
    {
        if (string.IsNullOrEmpty(incoming.exclusiveGroup))
        {
            return;
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffEntity removed = activeBuffs[i];
            if (removed.exclusiveGroup == incoming.exclusiveGroup && removed.buffId != incoming.buffId)
            {
                Dispose(removed);
                activeBuffs.RemoveAt(i);
            }
        }
    }
}
