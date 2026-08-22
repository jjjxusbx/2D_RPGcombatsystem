# Project Overview

Unity 6 2D 横版侧视动作 RPG 原型《灰烬矿脉》。当前覆盖：玩家移动、近战/弓箭瞄准、FSM 战斗（Idle/Move/Attack/Dodge/CastBuff）、ChaState 统一属性管线、技能配置驱动执行、怪物（NavMesh）巡逻交互、回合制网格原型、UI 展示页、编辑器一键装配工具，以及 v0.1.5 极简 Rogue 局会话（进房→清怪→祭坛→BOSS→结算）。

战斗代码的分层原则（沿用四层 + 组合根）：

- Data：`SkillData`、`ProjectileProperty`、`RangeProperty`、`MagicEffectData`、`BuffData`、`Attribute`、AI JSON 资产、`PlayerConfig`。
- Decision：输入解析、AI/BT 行为选择、技能与能力决策（`PlayerInputReader` → `PlayerIntent` → `CombatDecisionComponent`）。
- Execution：`CombatStateMachine`、`SkillExecutor`、`HitBoxController`、`MonsterPatrolController`（NavMesh）、Buff 生命周期、移动控制。
- Presentation：`PlayerAnimationPresenter`、朝向/闪避、Slash/Sword 表现、音效、相机。
- Composition（组合根）：`PlayerCombatBootstrap`（唯一装配入口）+ `RuntimeDiagnostics`（运行自检）+ `RunManager`（局会话根）。

核心战斗/架构规则已确立：

- 属性走 `Attribute`（基础值 + 修饰器五段式：固定值加算 → 百分比加算 → 乘算 → 夹取），Buff/强化通过 `AttributeModifier` 定向施加/移除。
- 伤害管线统一走 `ChaState.TakeDamage`（减免 → 扣血 → `onDamaged`/`onDeath`），表现层（玩家/敌人）订阅事件做击退/动画/销毁，ChaState 不负责销毁对象。
- 技能使用主 Skill 配置 + Projectile/Range 子数据；`MagicEffect` 用配置 + `CMagicProperty` 子类；Buff 使用时长/叠层/互斥组规则。
- 新能力优先通过新增数据行、子类、函数注册实现，而非重写核心流程。
- 统一装配：玩家战斗管线只经 `PlayerCombatBootstrap` 装配，FSM 模式下旧脚本让权/禁用，避免双写 `rb.linearVelocity`。

# Tech Stack

- Unity `6000.0.47f1`
- C# 脚本位于 `Assets/C#`，分层目录见下
- Unity 2D（`com.unity.feature.2d 2.0.1`）、Tilemap、UGUI `2.0.0`、Timeline `1.8.7`
- Cinemachine `3.1.7`
- AI Navigation `2.0.14`（怪物巡逻 NavMesh）
- Unity Input System `1.14.0` 已安装；游戏脚本仍以旧 `Input` 为主，`PlayerInputReader` 键位已配置化（SerializeField）
- Unity Test Framework `1.5.1`、Visual Scripting `1.9.6`
- 编译项目：`Assembly-CSharp.csproj`、`Assembly-CSharp.Player.csproj`、`Assembly-CSharp-Editor.csproj`

# Directory Structure

- `Assets/C#`：游戏运行脚本与编辑器脚本。
  - `Character.cs`：所有可控制角色基类（输入帧缓存、能力生命周期、`PhysicsSystem2D`、FSM 引用）。
  - `基础移动.cs` / `EnemyBase.cs`：玩家 / 敌人，均继承 `Character`，受击/死亡经 ChaState 管线。
  - `PhysicsSystem2D.cs`：物理适配层（Move/Stop/SetVelocity，2D↔NavMesh XZ 坐标映射）。
  - `Attribute.cs` / `CombatSystem/ChaState.cs`：属性容器与统一伤害管线。
  - `CombatSystem/`：`CombatContext`、`ICombatState`、`State/{Idle,Move,Attack,Dodge,CastBuff}`。
  - `Execution/`：`CombatStateMachine`、`SkillExecutor`、`HitDetection/HitBoxController`、`AI/MonsterPatrol{Controller,Path}`、`AI/MonsterMoveToTarget`。
  - `Decision/`：`PlayerInputReader`、`CombatDecisionComponent`、`IPlayerIntent`。
  - `Presentation/`：`PlayerAnimationPresenter`。
  - `Composition/`：`PlayerCombatBootstrap`、`RuntimeDiagnostics`。
  - `Date/`：`PlayerConfig`。
  - `UI/`、`UiShowcase/`：UI 与展示页模块。
  - `TurnBased/`：独立的网格回合制原型（Contracts/Data/Decision/Entities/Execution/Presentation + Bootstrap/Diagnostics/DemoDriver）。
  - `Editor/`：`CombatSystemSetup`、`CompositionSetup`、`TurnBasedSetup`、`UiShowcaseSetup`、`MonsterPatrolSetup` 一键装配/烘焙工具。
  - `RunManager.cs`、`RunCurrencyStore.cs`（命名空间 `Roguelike.Run`）：v0.1.5 局会话与局外货币。
  - `CombatData.cs`、`BuffSystem.cs`（含 `BuffController`）、`AbilityBase.cs`、`CharacterAttackAbility.cs`、`InventoryManager.cs`、`跳跃射箭.cs`、`Sword.cs`、`WeaponPivot.cs`、`PlayerAttackTrigger.cs`、`SlimeATKTri.cs` 等。
- `Assets/Scenes`：`SampleScene.unity`、`UiShowcase.unity`、`Test.unity`。
- `Assets/动画/{英雄,大剑,敌人}`：Hero / Sword / Slash / Slime_sheet 控制器与动画片段。
- `Assets/Arts`：美术、音效、Tilemap 资源；`Assets/Perfab`：预制体（保留既有拼写）；`Assets/Plugins`：第三方插件。
- `Packages`、`ProjectSettings`：包与工程设置。
- `docs/`：`核心框架设计.md`、`版本计划.md`、`策划流程与双档方案.md`、`配置步骤.md`、`怪物巡逻系统使用与配置文档.md`、`测试报告.md`、`模拟经营游戏UI_UX设计研究.md`、`UI作品集设计说明.md`、`REARCHITECTURE_SPEC.md`。
- `.trae/specs/`：Spec 驱动的开发规格与任务清单；`.trae/rules/`：协作规则。
- `Library`、`Temp`、`obj`、`Logs`、`.vs`、`UserSettings`：生成/本地文件，非需勿改。

# 核心架构与分工（角色职责）

统一由 `PlayerCombatBootstrap`（组合根）在 `Awake` 按固定顺序装配玩家战斗管线：

```
数据层   ChaState（统一伤害管线）→ BuffController（Buff 生命周期）
表现层   PlayerAnimationPresenter（动画/朝向/命中盒）
决策层   PlayerInputReader（输入→意图）→ CombatDecisionComponent（体力/冷却决策）
执行层   SkillExecutor（技能配置执行）→ CombatStateMachine（FSM 调度）
仲裁     FSM：基础移动让权、跳跃射箭禁用
```

- `Character` 基类：输入帧缓存、能力注册/加载/销毁、`PhysicsSystem2D` 引用、FSM 引用；子类只覆写 `Awake`/`OnCharacterUpdate`/`OnCharacterFixedUpdate`。
- 生命/属性统一归 `ChaState`（8 属性：MaxHealth/HpRegen/Atk/Defense/MoveSpeed/AttackRate/PickupRange/Growth），Buff/升级用 `AttributeModifier` 定向增删。
- 状态机：`CombatStateMachine.Configure()` + `Start` 自绑定回退 + `ctx.Intent` 单帧单次读取 + 可观察错误（空状态/非法转换 `LogWarning`/`LogError`）。
- 怪物巡逻：`MonsterPatrolController` 状态机（Idle/Patrol/Chase）+ `MonsterPatrolPath` 路径点 + `NavMeshAgent`；2D↔NavMesh 坐标映射 `(x,y)→(x,0,z)`；受击暂停导航、结束后 `Warp` 对齐恢复。
- 回合制原型 `TurnBased/`：独立于战斗主线的网格回合制验证。
- 局会话 `Roguelike.Run`：`RunManager` 用 `_sessionSource` 实例引用实现强化定向回收；`RunCurrencyStore` 无状态、原子写、版本化。

# 当前能力盘点

以下结论基于当前代码、场景引用、Packages 与 ProjectSettings，不以文件名推断可用性。

| 能力 | 当前状态 | 可直接复用 | 主要证据与限制 |
|---|---|---:|---|
| Unity API | 可用 | 是 | Unity `6000.0.47f1`，C# 编译链可用 |
| 游戏框架（统一应用层） | 可用 | 是 | `PlayerCombatBootstrap` 装配根 + `RuntimeDiagnostics` 自检；生命周期覆盖创建→启用→更新→销毁，无跨场景单例 |
| 有限状态机 | 可用 | 条件可用 | `CombatStateMachine` + 5 状态；`Configure`/自绑定/intent 缓存/可观察错误已落地；仍用旧 `Input`，扩展需先迁输入 |
| 属性系统 | 可用 | 是 | `Attribute`（五段式 + 脏缓存）+ `ChaState`（8 属性 + `modifier` 定向） |
| 统一伤害管线 | 可用 | 是 | `ChaState.TakeDamage` → 减免 → 扣血 → `onDamaged`/`onDeath`；玩家/敌人共用 |
| 怪物巡逻 | 可用 | 条件可用 | `MonsterPatrolController` + `MonsterPatrolPath`（NavMeshAgent）；依赖 AI Navigation `2.0.14`，需烘焙环境 |
| 局会话（Rogue） | 原型 | 条件可用 | `RunManager`/`RunCurrencyStore`（无状态、原子写、版本化）；结算/房间流待场景联调 |
| 回合制网格原型 | 原型 | 否 | `TurnBased/` 完整但独立于主线；无主场景接入 |
| UI 框架 | 基础可用 | 条件可用 | UGUI `2.0.0`、Canvas、EventSystem；`UiShowcase` 展示页模块存在；无统一 UI 管理器 |
| 能力系统 | 可用 | 是 | `AbilityBase` + `CharacterAttackAbility`，`Character` 统一注册/加载 |
| 对象池 | 不存在 | 否 | 未发现池接口/生命周期/实例 |
| 数据存储 | 部分可用 | 条件可用 | `RunCurrencyStore` 可原子写版本化 JSON；无通用存档迁移、库存未接入存档 |
| 背包 | 原型可用 | 否 | `InventoryManager` 支持买卖/堆叠；全局单例、公开可变列表，未接存档 |
| Buff | 原型可用 | 否 | `BuffController`（在 `BuffSystem.cs`）支持时长/叠层/互斥；无效果回滚与正式测试 |
| AI 行为树 | 原型可用 | 否 | `AIBehaviorTree` JSON + 函数注册；无函数生命周期/节点校验/资产验证 |
| 任务系统 | 不存在 | 否 | 未发现任务数据/状态/奖励结算/管理器 |
| 输入系统 | 不可直接复用 | 否 | 两份 `.inputactions` 资产未接入，代码仍用旧 `Input`；`activeInputHandler` 双模式存在重复触发风险 |

# 可复用边界

- 可复用：`PlayerAnimationPresenter` 动画表现接口、`PlayerAnimationPresenter.SetBoolIfExists/SetTriggerIfExists`（参数存在性防御）、`PhysicsSystem2D` 物理适配、`ChaState` 属性/伤害管线、`Character`/`AbilityBase` 生命周期、`SafeAreaFitter`、`RunCurrencyStore` 无状态持久化、UGUI 基础组件。
- 暂不可复用：`CombatStateMachine` 作为稳定 FSM（需先迁输入并补测试）、`InventoryManager` 作为持久化背包、`BuffController` 作为完整 Buff 系统（需补事件/回滚）、`AIBehaviorTree` 作为生产 AI 框架。
- 不新增对象池和任务系统，直到出现明确生成/回收或任务流程验收标准。
- 新功能必须先确认现有模块的场景引用、生命周期、错误边界和最小验证，再决定复用、修复或重写。

# 当前已知问题与风险（截至本次盘点）

- 已清理（P0，本次）：移除 `CameraFollow2D.cs`/`PlayerAttackTrigger.cs` 残留的 `agent log` 空注释、`PlayerAttackTrigger` 的 `[CombatHitDebug]` 日志刷屏与 CS0219 `branch` 未用变量；删除无引用的 `CombatHitDebugProbe.cs`、`Test.cs`、`Test4.cs`（含 `.meta`）。三个 csproj 现均 0 警告 / 0 错误。
- `PlayerInputReader` 仍用旧 `Input` API，与 `activeInputHandler` 双模式存在重复触发风险（见版本计划 v0.2）。
- `EnemyBase`/`MonsterPatrolController` 存在每帧 `GetComponent` / `Physics2D.OverlapCircle`，性能待优化。
- `UiShowcase.unity`、`Test.unity` 未纳入 git（`SampleScene.unity` 已跟踪）；部分编辑器装配仍依赖人工执行菜单工具后进入 Play Mode 验证。

# 对抗式审查协议

每次涉及架构、框架或管理器的变更，必须单独检查以下问题：

- 是否把"有文件"误判为"可用能力"。
- 是否存在第二个输入入口、第二个状态入口或第二份业务规则。
- 是否能从场景启动到实际调用，而不是只通过编译。
- 生命周期是否完整：创建、启用、更新、禁用、销毁和场景切换。
- 失败是否可观察：空引用、无效数据、损坏存档、未知 AI 函数和非法状态转换。
- 是否有公开契约和最小行为验证；没有则标记为原型，不得写成稳定框架。
- 是否引入了未请求的抽象、全局单例、隐藏副作用或不可回收资源。

审查输出必须分开记录：

- 事实：代码、场景、包或编译结果直接证明的内容。
- 风险：可能导致重复触发、数据丢失、状态卡死或生命周期泄漏的问题。
- 结论：可复用、修复后可复用、仅供参考或不存在。
- 验证：编译、Unity 启动、Play Mode 或行为测试的实际结果。

# 核心工作流程（版本计划驱动）

项目按 `docs/版本计划.md` 分里程碑推进，每版有独立验收清单（两个 csproj 构建 + 对应 Play Mode 清单）：

- v0.1 核心框架（✅）：装配根 + 运行时自检 + FSM 加固 + Dodge 状态卡死修复。
- **v0.1.5 极简 Rogue（⏳ 下一目标）**：进房→清怪→掉碎片→祭坛三选一强化→BOSS→结算；强化走 `ChaState.ApplyModifier(source=局会话)`，结束 `RemoveModifiersFromSource` 清空；局外货币走 `RunCurrencyStore`。
- v0.2/v0.3/v0.4：输入系统迁移、垂直切片关卡、上线打磨。

开发协作约定：

- 涉及架构/管理器变更前，先按"对抗式审查协议"输出事实/风险/结论。
- 功能开发可走 Spec 驱动（`.trae/specs/` 下建目录：spec.md 需求、tasks.md 任务、checklist.md 验收清单），完成后再落地。
- 提交信息遵循 `.trae/rules/git-commit-message.md`（`alwaysApply:false`，`scene:git_message`）。
- 版本结束须填写"上线复盘模板"（见版本计划文末），未复盘不得进入下一版。

# Development Commands

```powershell
& "D:\Program Files\Unity 6000.0.47f1\Editor\Unity.exe" -projectPath "D:\project_unity\2D_RPGcombatsystem"
```

```powershell
dotnet build .\Assembly-CSharp.csproj
dotnet build .\Assembly-CSharp.Player.csproj
dotnet build .\Assembly-CSharp-Editor.csproj
```

注意：

- Unity 重新导入/生成 csproj 后，`Temp/obj` 的 `project.assets.json` 会丢失，此时 `--no-restore` 会报 `NETSDK1004`；改跑不带 `--no-restore` 的 `dotnet build` 以先做 restore。
- 沙箱环境（trae-sandbox）可能因无法写 `fselocallog` 日志而返回非 0 退出码；以"0 个警告 / 0 个错误"与 DLL 生成结果判定编译是否通过。
- 不要用 `2D_RPGcombatsystem.sln` 作为主要验证命令，其目前包含重复的 `Assembly-CSharp` 项目名。

# Coding Guidelines

- Keep changes small and tied to the requested behavior.
- Preserve Unity `.meta` files. Add a `.meta` file for every new Unity asset or script.
- Prefer serialized fields for Unity object references wired in the Inspector.
- UI buttons, Toggles, AudioSources, finger nodes, and the first-tile button must be assigned through the Inspector.
- Add null checks around optional scene references and camera references.
- Use `PlayerAnimationPresenter.SetBoolIfExists/SetTriggerIfExists` 做 Animator 参数存在性防御，避免不存在的参数抛错。
- Do not hard-code Animator state transitions when a controller already uses Trigger parameters.
- 匹配现有 Animator 参数（按当前控制器核实）：
  - Hero：`IsRun`（跑）、`Attack`（攻击触发器）
  - Slime：`IsRun`、`IsGetHit`、`Attack`
  - Sword：`ATK_1`
  - Slash：`ATK1`
- Do not rotate `SwordPivot` during attack clips if the animation clip already owns that rotation.
- For weapon aiming, keep mouse-world conversion camera-safe and isolated from attack animation timing.
- Avoid routine comments. Comment only non-obvious Unity lifecycle, asset, animation, or data-framework constraints.
- 抽象与数据边界：新增属性走 `Attribute`/`ChaState`；新 Buff 走 `AttributeModifier` 与 Buff 规则；新技能走 Skill/MagicEffect 数据行；新怪物走 `MonsterPatrolController` 参数与 AI 函数注册；新投射物走 launch/trajectory 正交扩展。
- Current script and asset paths include Chinese names; use exact paths and `-LiteralPath` in PowerShell.

# Testing Requirements

- 对任何 C# 脚本改动，运行以下构建并确保错误与警告均为 0：
  - `dotnet build .\Assembly-CSharp.csproj`
  - `dotnet build .\Assembly-CSharp.Player.csproj`
  - `dotnet build .\Assembly-CSharp-Editor.csproj`（Editor 脚本改动时必跑；运行时两 csproj 不含 Editor 目录，无法覆盖 Editor 编译错误）
- If a build fails because Unity, Defender, or another process locks `obj` or `Temp`, rerun once before reporting failure.
- 对动画/输入/巡逻/回合制等行为改动，在 Unity Play Mode 验证：
  - 移动切换 `IsRun`；攻击触发 Hero `Attack` 与 Sword `ATK_1`、Slash `ATK1`。
  - 怪物巡逻往返、进入检测范围追击、离开视野回归、受击暂停/恢复（`MonsterPatrolController`）。
  - `PlayerCombatBootstrap` 装配后 `[Diagnostics]` 零问题项；`跳跃射箭` 在 FSM 模式下禁用。
- There is no formal project test suite beyond Unity compile/build validation unless tests are added.

# Security Guidelines

- 本项目为本地 Unity 游戏原型，无后端、Web3、鉴权或密钥处理。
- 不要新增网络调用、凭证存储、分析埋点、遥测或外部服务集成，除非明确要求。
- 不要提交或依赖机器本地生成文件（如 `D:\.cursor\*.log` 等调试探针产物）；清理本次盘点发现的探针与死代码。
- 持久化仅允许无状态、版本化、原子写入（参见 `RunCurrencyStore`）。

# Agent Instructions

- Read existing files before modifying them.
- Inspect actual scripts, Animator controllers, scene/prefab structure, package files, and current generated project files before changing behavior.
- Modify only files needed for the task.
- Do not overwrite scenes, prefabs, controllers, animation clips, or generated Unity assets blindly; inspect serialized data first.
- Do not edit `Library`, `Temp`, `obj`, `Logs`, `.vs`, or `UserSettings` unless the user explicitly requests it.
- Prefer `rg` and `Get-Content -LiteralPath` for reads.
- 术语与命名：脚本/资产路径含中文，用精确路径与 `-LiteralPath`；保留中文资源目录名。
- Do not commit, push, or run destructive git commands unless explicitly requested.
- Report exact validation commands run and their result.
- If Unity Inspector bindings may need reassignment after a script field/name change, state that risk explicitly.
- 架构变更输出遵循"对抗式审查协议"；版本推进遵循 `docs/版本计划.md`；新功能优先走 Spec 驱动（`.trae/specs/`）。
