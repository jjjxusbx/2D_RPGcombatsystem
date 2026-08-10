using System.Collections.Generic;
using UnityEngine;

public sealed class BuffInstance
{
    public BuffData data;
    public float remaining;
    public int stacks;

    public BuffInstance(BuffData data)
    {
        this.data = data;
        remaining = data.duration;
        stacks = 1;
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
            instance.remaining -= Time.deltaTime;
            if (instance.remaining <= 0f)
            {
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
            activeBuffs.Add(new BuffInstance(data));
            return;
        }

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

    private void RemoveExclusiveBuffs(BuffData incoming)
    {
        if (string.IsNullOrEmpty(incoming.exclusiveGroup))
        {
            return;
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].data.exclusiveGroup == incoming.exclusiveGroup &&
                activeBuffs[i].data.buffId != incoming.buffId)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }
}
