using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一次伤害结算的结果快照，供 onDamaged/onDeath 订阅者读取。</summary>
public class DamageInfo
{
    public float rawDamage;      // 原始伤害（未减免）
    public float actualDamage;   // 实际扣除（防御减免后）
    public Transform owner;      // 伤害来源，可能为 null
    public bool isLethal;        // 是否致死

    public DamageInfo(float rawDamage, float actualDamage, Transform owner, bool isLethal)
    {
        this.rawDamage = rawDamage;
        this.actualDamage = actualDamage;
        this.owner = owner;
        this.isLethal = isLethal;
    }
}

/// <summary>
/// 角色属性容器：所有数值属性都走 Attribute（基础值 + 修饰器）。
/// Buff/升级通过 AttributeModifier 动态增减，到期后按 source 定向移除，不直接改字段。
/// statId 约定见 Awake 注册表，BuffData.components 里的 BuffStatComponent.modifiers 用相同 id 定向施加。
/// 统一伤害管线：所有伤害经 TakeDamage → 防御减免 → 扣血 → onDamaged/onDeath 事件，
/// 表现层（玩家/敌人）订阅事件做击退/动画/销毁，本类不负责销毁对象。
/// </summary>
public class ChaState : MonoBehaviour
{
    [Header("========== 基础属性(Buff 按 statId 修改) ==========")]
    public Attribute maxHealth = new Attribute(100f) { minValue = 1f };   // MaxHealth
    public Attribute hpRegen = new Attribute(0f) { minValue = 0f };       // HpRegen 每秒回复
    public Attribute atk = new Attribute(10f) { minValue = 0f };          // Atk 攻击力
    public Attribute defense = new Attribute(0f) { minValue = 0f };       // Defense 防御（伤害减免）
    public Attribute moveSpeed = new Attribute(5f) { minValue = 0f };     // MoveSpeed 移速
    public Attribute attackRate = new Attribute(1f) { minValue = 0.1f };  // AttackRate 攻速倍率
    public Attribute pickupRange = new Attribute(2f) { minValue = 0f };   // PickupRange 拾取范围
    public Attribute growth = new Attribute(1f) { minValue = 0f };        // Growth 经验获取倍率

    public float currentHealth;

    /// <summary>受击事件（含致死一击）。订阅者做表现：击退/受击动画/闪白/HP UI。</summary>
    public event Action<DamageInfo> onDamaged;

    /// <summary>死亡事件（只触发一次）。订阅者做清理：禁用组件/销毁/转发 UnityEvent。</summary>
    public event Action<DamageInfo> onDeath;

    /// <summary>是否存活。</summary>
    public bool IsAlive => currentHealth > 0f;

    private readonly Dictionary<string, Attribute> _statMap = new Dictionary<string, Attribute>();
    private bool _deathEmitted;

    private void Awake()
    {
        Register("MaxHealth", maxHealth);
        Register("HpRegen", hpRegen);
        Register("Atk", atk);
        Register("Defense", defense);
        Register("MoveSpeed", moveSpeed);
        Register("AttackRate", attackRate);
        Register("PickupRange", pickupRange);
        Register("Growth", growth);

        currentHealth = maxHealth.GetValue();
    }

    private void Update()
    {
        // 自动回血：不超过当前上限
        float regen = hpRegen.GetValue();
        if (regen > 0f && currentHealth < maxHealth.GetValue())
        {
            currentHealth = Mathf.Min(currentHealth + regen * Time.deltaTime, maxHealth.GetValue());
        }
    }

    /// <summary>按 statId 读取当前最终值（Buff/武器/Camera 等外部统一入口）。</summary>
    public float GetStat(string statId)
    {
        return _statMap.TryGetValue(statId, out Attribute attr) ? attr.GetValue() : 0f;
    }

    /// <summary>按 id 查属性对象（需要读 BaseValue/Modifiers 时用）。</summary>
    public bool TryGetAttribute(string statId, out Attribute attr)
    {
        return _statMap.TryGetValue(statId, out attr);
    }

    /// <summary>施加一条修饰器（Buff/升级共用入口）。</summary>
    public void ApplyModifier(AttributeModifier modifier)
    {
        if (modifier == null || string.IsNullOrEmpty(modifier.statId)) return;
        if (_statMap.TryGetValue(modifier.statId, out Attribute attr))
        {
            attr.AddModifier(modifier);
        }
    }

    /// <summary>移除某来源（如 Buff id）施加的全部修饰器。</summary>
    public void RemoveModifiersFromSource(object source)
    {
        foreach (Attribute attr in _statMap.Values)
        {
            attr.RemoveAllFromSource(source);
        }
    }

    /// <summary>
    /// 统一伤害管线入口：防御减免 → 扣血 → onDamaged → 死亡时 onDeath。
    /// 玩家/敌人共用；owner 为伤害来源（可 null）。
    /// </summary>
    public void TakeDamage(float damage, Transform owner = null)
    {
        if (!IsAlive || damage <= 0f) return;

        float actual = Mathf.Max(0f, damage - defense.GetValue());
        currentHealth = Mathf.Max(0f, currentHealth - actual);

        if (actual > 0f)
        {
            Debug.Log($"{gameObject.name} 受到伤害 {actual:0.#}（原始 {damage:0.#}）剩余 {currentHealth:0.#}");
        }

        bool lethal = currentHealth <= 0f;
        onDamaged?.Invoke(new DamageInfo(damage, actual, owner, lethal));

        if (lethal)
        {
            Die(new DamageInfo(damage, actual, owner, true));
        }
    }

    /// <summary>治疗（不触发死亡检查）。</summary>
    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth.GetValue());
    }

    private void Register(string statId, Attribute attr)
    {
        _statMap[statId] = attr;
    }

    private void Die(DamageInfo info)
    {
        if (_deathEmitted) return;
        _deathEmitted = true;

        Debug.Log($"{gameObject.name} 死亡了");
        onDeath?.Invoke(info);
    }
}
