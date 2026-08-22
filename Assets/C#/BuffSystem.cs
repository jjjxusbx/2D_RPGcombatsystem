using System.Collections.Generic;
using UnityEngine;

public sealed class BuffInstance
{
    public BuffData data;
    public float remaining;
    public int stacks;
    public ChaState host; // 受影响的角色属性容器，用于施加/回收修饰器
    public float dotAccumulator; // DOT 计时累计器，每满 1s 触发一次 TakeDamage

    public BuffInstance(BuffData data)
    {
        this.data = data;
        remaining = data.duration;
        stacks = 1;
        dotAccumulator = 0f;
    }
}

public class BuffController : MonoBehaviour
{
    private readonly List<BuffInstance> activeBuffs = new List<BuffInstance>();

    public IReadOnlyList<BuffInstance> ActiveBuffs => activeBuffs;

    private void Update()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance instance = activeBuffs[i];

            // DOT 每满 1 秒 tick 一次，走 host.TakeDamage（小额伤害，表现层会过滤受击动画）
            if (instance.data.damagePerSecond > 0f && instance.host != null && instance.host.IsAlive)
            {
                instance.dotAccumulator += Time.deltaTime;
                while (instance.dotAccumulator >= 1f)
                {
                    instance.dotAccumulator -= 1f;
                    instance.host.TakeDamage(instance.data.damagePerSecond);
                }
            }

            instance.remaining -= Time.deltaTime;
            if (instance.remaining <= 0f)
            {
                // 到期：回收该 Buff 施加到属性上的全部修饰器
                if (instance.host != null)
                {
                    instance.host.RemoveModifiersFromSource(instance.data.buffId);
                }

                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void Apply(BuffData data)
    {
        if (data == null)
        {
            return;
        }

        RemoveExclusiveBuffs(data);
        BuffInstance existing = Find(data.buffId);
        if (existing == null)
        {
            BuffInstance instance = new BuffInstance(data);
            instance.host = GetComponent<ChaState>();
            activeBuffs.Add(instance);
            ApplyModifiers(instance);
            return;
        }

        // 重复施加：先回收旧效果再重放，避免同一 buffId 的修饰器重复叠加
        existing.host?.RemoveModifiersFromSource(data.buffId);

        switch (data.stackRule)
        {
            case BuffStackRule.Replace:
                existing.remaining = data.duration;
                existing.stacks = 1;
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

        ApplyModifiers(existing);
    }

    public bool Has(string buffId)
    {
        return Find(buffId) != null;
    }

    private BuffInstance Find(string buffId)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].data.buffId == buffId)
            {
                return activeBuffs[i];
            }
        }

        return null;
    }

    // 将 BuffData.modifiers 逐条施加到角色的 ChaState 属性上，来源记为 buffId
    private void ApplyModifiers(BuffInstance instance)
    {
        if (instance.host == null || instance.data.modifiers == null) return;

        foreach (AttributeModifier mod in instance.data.modifiers)
        {
            if (mod == null || string.IsNullOrEmpty(mod.statId)) continue;
            mod.source = instance.data.buffId;
            instance.host.ApplyModifier(mod);
        }
    }

    private void RemoveExclusiveBuffs(BuffData incoming)
    {
        if (string.IsNullOrEmpty(incoming.exclusiveGroup))
        {
            return;
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance removed = activeBuffs[i];
            if (removed.data.exclusiveGroup == incoming.exclusiveGroup &&
                removed.data.buffId != incoming.buffId)
            {
                // 与到期/重放路径一致：先回收该 Buff 施加的属性修饰器，再移除实例，避免属性加成残留
                removed.host?.RemoveModifiersFromSource(removed.data.buffId);
                activeBuffs.RemoveAt(i);
            }
        }
    }
}
