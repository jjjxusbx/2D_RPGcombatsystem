# 战斗属性改造 · ECS(DOTS) 框架

> 模块路径：`Assets/C#/CombatSystem/ECS/`
> 程序集：`Game.ECS.Buff`（自包含，`autoReferenced:false`，不干扰现有非 DOTS 战斗管线）

## 1. 架构总览（Buff 即实体，组合而非继承）

在 ECS 中，每一个 Buff 都是 World 中的一个**独立 Entity**，而不是挂在宿主身上的纯数据组件。

```
                Buff Entity（World 中的实体）
                ┌──────────────────────────────┐
                │ BuffComponent      (必须)      │  唯一标识 + Duration/Stacks/是否永久/互斥组
                │ TargetRef          (必须)      │  绑定到宿主
                │ BuffModifierElement buffer      │  针对宿主属性的修正值（Attack/Speed…）
                ├──────────────────────────────┤
                │ AuraComponent       (可选，加组件)│  光环：范围半径、筛选、周期搜索
                │ AOEEffectComponent  (可选，加组件)│  AoE：形状、范围、伤害系数、延迟/重复
                │ TickComponent       (可选，加组件)│  周期 tick：持续伤害/治疗
                └──────────────────────────────┘
```

**禁止通过继承扩展。** 需要“同时是光环”就追加 `AuraComponent`，需要“同时是 AoE”就追加 `AOEEffectComponent`，需要周期触发就追加 `TickComponent`。

## 2. 文件与职责

| 文件 | 职责 |
|---|---|
| `BuffECSComponents.cs` | 所有 `IComponentData` / `IBufferElementData`（Buff、Aura、AoE、Tick、宿主、修饰器） |
| `BuffECSQueries.cs` | 区分“普通 / 光环 / AoE / Tick” Buff 的 `EntityQuery` 与 `SystemAPI.Query` 示例 |
| `BuffECSConfig.cs` | `BuffConfigData`（序列化创建描述）+ `BuffEcsFactory`（创建/克隆/回收，维护宿主修饰器表） |
| `BuffECSSystems.cs` | `BuffManagementSystem` / `TickBuffSystem` / `AttributeRefreshSystem` / `AuraSearchSystem` / `AOETriggerSystem` |
| `BuffECSAuthoring.cs` | 单位 `Baker`，把 GameObject 转成 Buff 宿主 ECS 实体 |
| `BuffECSRuntime.cs` | 运行时装配：把系统注册进默认 `World` 的 `SimulationSystemGroup` |

## 3. 系统职责

- **BuffManagementSystem**：Buff 实体倒计时更新、到期销毁（创建/绑定由工厂完成）。
- **TickBuffSystem**：周期性 tick，直接对宿主 `Health` 结算伤害/治疗。
- **AttributeRefreshSystem**：每帧查询宿主 `ActiveModifierElement` 表，按五段式（固定加算 → 百分比加算 → 乘算 → 夹取）重算 `FinalStats`。
- **AuraSearchSystem**：光环周期搜索半径内目标，把光环 Buff 实体**克隆套用**给目标（已带同 buff 的不重复套用）。
- **AOETriggerSystem**：按延迟 / 重复触发范围伤害，以施法者攻击 * 伤害系数结算。

## 4. 查询示例（区分 Buff 类型）

```csharp
// 普通 Buff（无光环 / 无 AoE / 无 Tick）
var q = em.CreateEntityQuery(
    ComponentType.ReadWrite<BuffComponent>(),
    ComponentType.ReadOnly<TargetRef>(),
    ComponentType.Exclude<AuraComponent>(),
    ComponentType.Exclude<AOEEffectComponent>(),
    ComponentType.Exclude<TickComponent>());

// 光环 Buff
SystemAPI.Query<RefRW<BuffComponent>, RefRO<AuraComponent>>().WithEntityAccess();

// AoE Buff
SystemAPI.Query<RefRW<BuffComponent>, RefRO<AOEEffectComponent>>().WithEntityAccess();

// Tick Buff
SystemAPI.Query<RefRW<BuffComponent>, RefRO<TickComponent>>().WithEntityAccess();
```

## 5. 使用方法

1. **安装包**：本项目当前「未安装 Unity Entities」。需要先在 Package Manager 添加 `com.unity.entities`（Unity 6 配套的 Entities 1.x）。未安装前，`Game.ECS.Buff` 程序集不会编译（已用 `autoReferenced:false` 隔离，不影响你现有的非 DOTS 战斗代码）。
2. **宿主**：给任意单位 GameObject 挂 `UnitEcsAuthoring`，Baker 会产出具备 `UnitTag/UnitStats/FinalStats/Health/ActiveModifierElement` 的 ECS 实体。
3. **施加 Buff**：在运行时调用
   ```csharp
   BuffEcsFactory.CreateBuff(world.EntityManager, myBuffConfig, targetEntity, sourceEntity);
   ```
   `BuffConfigData` 里的 `aura` / `aoe` / `tick` 字段决定是否追加对应组件；只填 `modifiers` 就是普通属性改造 Buff。
4. **光环**：配置 `aura` 后，光环实体挂到宿主上，系统会自动把该 Buff 克隆套用给范围内目标。

## 6. 与现有（非 DOTS）系统的关系

现有 `BuffSystem.cs`（MonoBehaviour 组合式：`BuffEntity` + `BuffComponent/BuffAuraComponent/BuffAoeComponent/...`）在架构目标上与 DOTS 版一致，只是未引入 Unity Entities。二者互不冲突、独立成块。若决定采用 DOTS 版本，建议以 `Game.ECS.Buff` 为准，旧组合式系统可作为保留/过渡方案。
