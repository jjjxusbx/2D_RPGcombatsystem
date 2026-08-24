using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ============================================================
// 宿主（单位）Authoring + Baker
//
// 把场景里的 GameObject 单位转换成 ECS Entity，并挂上
// UnitTag / UnitStats / FinalStats / Health / BuffPosition /
// ActiveModifierElement buffer，从而能接收 Buff、参与属性重算与光环/AoE 判定。
// ============================================================

namespace Game.ECS.Buff
{
    /// <summary>挂到任意单位 GameObject 上即可成为 Buff 宿主。</summary>
    public class UnitEcsAuthoring : MonoBehaviour
    {
        public float attack = 10f;
        public float defense = 2f;
        public float moveSpeed = 5f;
        public float maxHealth = 100f;
        public float hpRegen;
    }

    public class UnitEcsBaker : Baker<UnitEcsAuthoring>
    {
        public override void Bake(UnitEcsAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(e, new UnitTag());
            AddComponent(e, new UnitStats
            {
                Attack = a.attack,
                Defense = a.defense,
                MoveSpeed = a.moveSpeed,
                MaxHealth = a.maxHealth,
                HpRegen = a.hpRegen,
            });
            AddComponent(e, new FinalStats
            {
                Attack = a.attack,
                Defense = a.defense,
                MoveSpeed = a.moveSpeed,
                MaxHealth = a.maxHealth,
                HpRegen = a.hpRegen,
            });
            AddComponent(e, new Health { Value = a.maxHealth, Max = a.maxHealth });
            AddComponent(e, new BuffPosition { Value = float3.zero });
            AddBuffer<ActiveModifierElement>(e);
        }
    }
}
