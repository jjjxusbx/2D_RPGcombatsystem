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
    public string buffId;
    public float duration = 1f;
    [Min(1)] public int maxStacks = 1;
    public BuffStackRule stackRule = BuffStackRule.Replace;
    public string group;
    public string exclusiveGroup;
    /// <summary>每秒伤害（DOT），0 = 无 DOT。由 BuffController.Update 每 1 秒 tick 一次走 host.TakeDamage。</summary>
    public float damagePerSecond;
    public List<AttributeModifier> modifiers = new List<AttributeModifier>();
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
