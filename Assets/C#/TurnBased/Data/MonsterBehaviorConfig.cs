using System;
using UnityEngine;

namespace TurnBased
{
    /// <summary>怪物行为原型：决定视野、索敌、追击/拉距与攻击参数。</summary>
    public enum MonsterArchetype
    {
        MeleeBerserker, // 近战莽夫：视野小、贴脸攻击、无拉距
        RangedKiter,    // 远程风筝：视野大、射程远、被贴脸会拉开距离
    }

    /// <summary>
    /// 怪物行为参数（数据驱动）：由组合根按原型创建并注入怪物实体。
    /// 框架阶段使用普通可序列化类；后续可升级为 ScriptableObject 资产行。
    /// </summary>
    [Serializable]
    public sealed class MonsterBehaviorConfig
    {
        public string displayName = "Monster";
        public MonsterArchetype archetype = MonsterArchetype.MeleeBerserker;
        public int maxHealth = 30;
        public int speed = 10;
        public int moveRangePerTurn = 1;
        public int attackDamage = 8;
        public int attackRange = 1;
        public int visionRadius = 6;

        /// <summary>贴脸安全距离：低于该距离时执行 Away 拉距（近战怪为 1 = 永不拉距）。</summary>
        public int minRange = 1;

        /// <summary>Wander 状态中随机移动的概率（0-1）。</summary>
        [Range(0f, 1f)] public float wanderChance = 0.4f;

        public static MonsterBehaviorConfig MeleeBerserker()
        {
            return new MonsterBehaviorConfig
            {
                displayName = "近战莽夫",
                archetype = MonsterArchetype.MeleeBerserker,
                maxHealth = 30,
                speed = 10,
                attackDamage = 9,
                attackRange = 1,
                visionRadius = 6,
                minRange = 1,
            };
        }

        public static MonsterBehaviorConfig RangedKiter()
        {
            return new MonsterBehaviorConfig
            {
                displayName = "远程风筝",
                archetype = MonsterArchetype.RangedKiter,
                maxHealth = 20,
                speed = 10,
                attackDamage = 5,
                attackRange = 3,
                visionRadius = 9,
                minRange = 2,
            };
        }
    }

    /// <summary>组合根在 Inspector 中配置的怪物生成条目。</summary>
    [Serializable]
    public sealed class MonsterSpawnEntry
    {
        public MonsterArchetype archetype = MonsterArchetype.MeleeBerserker;
        public Vector2Int gridPos = new Vector2Int(11, 7);
    }
}