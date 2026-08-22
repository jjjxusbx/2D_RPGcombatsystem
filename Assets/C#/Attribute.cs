using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 修饰器类型：固定值加算 → 百分比加算 → 乘算，按此顺序参与最终值计算。
/// </summary>
public enum ModifierType
{
    AddValue,     // 固定值加算，如 +20 攻击
    PercentAdd,   // 百分比加算（同类百分比先加总再乘），如 +10% 攻击
    Multiply      // 乘算（最后统一连乘），如 ×1.5 伤害
}

/// <summary>
/// 一条属性修饰器。statId 用于 Buff/升级按 id 定向施加；source 用于到期时定向移除。
/// </summary>
[System.Serializable]
public class AttributeModifier
{
    public string statId;        // 目标属性标识，如 "Atk"
    public ModifierType type;    // 修饰方式
    public float value;          // 数值：加值为增量，百分比为 0.1 表示 10%，乘算为倍率
    [System.NonSerialized] public object source; // 来源（Buff id / 装备），序列化忽略

    public AttributeModifier() { }

    public AttributeModifier(string statId, ModifierType type, float value, object source = null)
    {
        this.statId = statId;
        this.type = type;
        this.value = value;
        this.source = source;
    }
}

/// <summary>
/// 可序列化属性容器：基础值永不被动，最终值 = (BaseValue + Σ固定值) × (1 + Σ百分比) × Π乘数，
/// 结果夹在 [minValue, maxValue] 之间。带脏标记缓存，GetValue 高频调用不重复计算。
/// </summary>
[System.Serializable]
public class Attribute
{
    public float BaseValue; // 基础值，永远不动

    [Tooltip("最终值下限；默认 -∞ 不限制")]
    public float minValue = float.NegativeInfinity;

    [Tooltip("最终值上限；默认 +∞ 不限制")]
    public float maxValue = float.PositiveInfinity;

    private readonly List<AttributeModifier> _modifiers = new List<AttributeModifier>();
    private bool _isDirty = true; // 脏标记：数值变了就标记一下
    private float _finalValue;    // 缓存的最终值

    public Attribute() { }

    public Attribute(float baseValue)
    {
        BaseValue = baseValue;
    }

    /// <summary>当前生效的修饰器（只读，用于调试/面板展示）。</summary>
    public IReadOnlyList<AttributeModifier> Modifiers => _modifiers;

    /// <summary>获取最终计算后的属性值（脏时重算并缓存）。</summary>
    public float GetValue()
    {
        if (_isDirty)
        {
            RecalculateFinalValue();
            _isDirty = false;
        }
        return _finalValue;
    }

    /// <summary>修改基础值（升级加点等场景）。</summary>
    public void SetBase(float baseValue)
    {
        if (Mathf.Approximately(BaseValue, baseValue)) return;
        BaseValue = baseValue;
        _isDirty = true;
    }

    /// <summary>添加一个修饰器（比如加攻击Buff）。</summary>
    public void AddModifier(AttributeModifier modifier)
    {
        if (modifier == null) return;
        _modifiers.Add(modifier);
        _isDirty = true;
    }

    /// <summary>移除指定来源的所有修饰器（比如Buff到期了）。</summary>
    public void RemoveAllFromSource(object source)
    {
        if (source == null) return;
        int removed = _modifiers.RemoveAll(m => ReferenceEquals(m.source, source));
        if (removed > 0) _isDirty = true;
    }

    /// <summary>清空全部修饰器。</summary>
    public void ClearModifiers()
    {
        if (_modifiers.Count == 0) return;
        _modifiers.Clear();
        _isDirty = true;
    }

    // 重新计算最终值：固定值 → 百分比加总 → 乘算 → 夹取
    private void RecalculateFinalValue()
    {
        float flat = BaseValue;
        float sumPercent = 0f;
        float mult = 1f;

        foreach (var mod in _modifiers)
        {
            switch (mod.type)
            {
                case ModifierType.AddValue:
                    flat += mod.value;
                    break;
                case ModifierType.PercentAdd:
                    sumPercent += mod.value;
                    break;
                case ModifierType.Multiply:
                    mult *= mod.value;
                    break;
            }
        }

        _finalValue = Mathf.Clamp(flat * (1f + sumPercent) * mult, minValue, maxValue);
    }
}