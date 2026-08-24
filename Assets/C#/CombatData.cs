using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillEffectKind
{
    Projectile,
    Range,
    MagicEffect
}

public enum MagicPropertyKind
{
    Damage,
    ApplyBuff
}

public enum BuffStackRule
{
    Replace,
    AddStack,
    RefreshDuration,
    Ignore
}

[Serializable]
public struct AttributeValue
{
    public float baseValue;
    public float flatBonus;
    public float percentBonus;
    public float overrideValue;
    public float finalBonus;

    public float Evaluate()
    {
        float value = overrideValue == 0f ? baseValue + flatBonus : overrideValue;
        return value * (1f + percentBonus) + finalBonus;
    }
}

[CreateAssetMenu(menuName = "战斗/属性定义")]
public class AttributeDefinition : ScriptableObject
{
    public string attributeId;
    public string displayName;
    public AttributeValue defaultValue;
}

[CreateAssetMenu(menuName = "战斗/Buff")]
public class BuffData : ScriptableObject
{
    /// <summary>Buff 唯一标识；同时作为施加到 ChaState 属性上的 modifier.source，用于到期定向回收。</summary>
    public string buffId;
    /// <summary>总时长（秒）。0 表示瞬时生效（下一帧到期）。</summary>
    public float duration = 1f;
    [Min(1)] public int maxStacks = 1;
    public BuffStackRule stackRule = BuffStackRule.Replace;
    public string group;
    /// <summary>互斥组：同组且不同 buffId 的 Buff 只保留最新一个。</summary>
    public string exclusiveGroup;
    /// <summary>
    /// Buff 行为组件（ECS 组合）：基础为 BuffStatComponent（属性修饰器）/BuffDotComponent（持续伤害），
    /// 若同时是光环追加 BuffAuraComponent，若同时是范围效果追加 BuffAoeComponent。
    /// 组件按实体克隆，多宿主互不共享运行时状态。
    /// </summary>
    [SerializeReference] public List<BuffComponent> components = new List<BuffComponent>();
}

[Serializable]
public class ProjectileProperty
{
    public GameObject prefab;
    public float speed = 8f;
    public float lifetime = 3f;
    public float damageMultiplier = 1f;
}

[Serializable]
public class RangeProperty
{
    public float radius = 1f;
    public float duration = 0.2f;
    public float damageMultiplier = 1f;
    public LayerMask targetLayers;
}

[Serializable]
public abstract class CMagicProperty
{
    public MagicPropertyKind kind;
    public abstract void Apply(GameObject caster, GameObject target, int stacks);
}

[Serializable]
public class DamageMagicProperty : CMagicProperty
{
    /// <summary>伤害倍率：最终伤害 = 施法者 Atk × damageMultiplier</summary>
    public float damageMultiplier = 1f;

    public override void Apply(GameObject caster, GameObject target, int stacks)
    {
        if (target == null)
        {
            return;
        }

        // 从施法者 ChaState 读取 Atk，无 ChaState 时回退 10f
        float atk = 10f;
        if (caster != null)
        {
            ChaState casterState = caster.GetComponent<ChaState>();
            if (casterState != null)
            {
                atk = casterState.GetStat("Atk");
            }
        }

        float damage = atk * damageMultiplier;

        ChaState targetState = target.GetComponent<ChaState>();
        if (targetState == null)
        {
            Debug.LogWarning($"[Combat] DamageMagicProperty 目标 {target.name} 没有 ChaState，跳过伤害。");
            return;
        }

        targetState.TakeDamage(damage, caster != null ? caster.transform : null);
    }
}

[Serializable]
public class ApplyBuffMagicProperty : CMagicProperty
{
    public BuffData buff;

    public override void Apply(GameObject caster, GameObject target, int stacks)
    {
        if (target != null && buff != null)
        {
            BuffController controller = target.GetComponent<BuffController>();
            if (controller != null)
            {
                controller.Apply(buff);
            }
        }
    }
}

[CreateAssetMenu(menuName = "战斗/MagicEffect")]
public class MagicEffectData : ScriptableObject
{
    public string effectId;
    [SerializeReference] public List<CMagicProperty> properties = new List<CMagicProperty>();

    public void Apply(GameObject caster, GameObject target)
    {
        for (int i = 0; i < properties.Count; i++)
        {
            if (properties[i] != null)
            {
                properties[i].Apply(caster, target, 1);
            }
        }
    }
}

[CreateAssetMenu(menuName = "战斗/Skill")]
public class SkillData : ScriptableObject
{
    public string skillId;
    public float cooldown;
    public SkillEffectKind effectKind;
    public List<ProjectileProperty> projectiles = new List<ProjectileProperty>();
    public List<RangeProperty> ranges = new List<RangeProperty>();
    public MagicEffectData magicEffect;
}

