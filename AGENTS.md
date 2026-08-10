# Project Overview

Unity 6 2D RPG combat prototype. Current gameplay covers player movement, weapon attack animation, slash effect playback, camera follow, tilemap scene setup, and an early config-driven combat framework.

Combat code should stay layered:

- Data: `SkillData`, `ProjectileProperty`, `RangeProperty`, `MagicEffectData`, `BuffData`, attribute definitions, AI JSON assets.
- Decision: input parsing, AI behavior tree selection, skill selection.
- Execution: movement, hit detection, projectile/range execution, Buff lifetime and stack logic.
- Presentation: Animator parameters, sprite facing, slash effects, audio, camera behavior.

Core combat rules already established:

- Skill uses a main Skill config plus Projectile/Range child data.
- MagicEffect uses config plus `CMagicProperty` subclasses.
- Buff uses duration, stack rule, group/exclusive-group rules.
- AI uses behavior-tree JSON plus registered AI function library entries.
- Attributes use a five-part value structure.
- Projectile launch mode and flight trajectory should stay orthogonal.
- Buff expansion should use explicit effect channels, not ad hoc state changes.
- New gameplay capability should be added by new data rows, subclasses, or function registration where possible, not by rewriting core flow.

# Tech Stack

- Unity `6000.0.47f1`
- C# runtime/editor scripts under `Assets/C#`
- Unity 2D, Tilemap, UGUI, Timeline
- Cinemachine `3.1.7`
- Unity Input System `1.14.0` is installed; gameplay scripts still mix legacy `Input` and Input System assets
- Unity Test Framework `1.5.1`
- Visual Studio Unity project files: `Assembly-CSharp.csproj`, `Assembly-CSharp.Player.csproj`

# Directory Structure

- `Assets/C#`: gameplay, editor utility, and combat framework C# scripts.
- `Assets/Scenes`: Unity scenes, including `SampleScene.unity`.
- `Assets/动画/英雄`: hero Animator controller and idle/run/attack clips.
- `Assets/动画/大剑`: sword and slash Animator controllers and clips.
- `Assets/Arts`: imported art, sprites, audio, tilemap resources.
- `Assets/Perfab`: prefabs. Keep the existing misspelled directory name.
- `Assets/Plugins`: third-party/editor plugins.
- `Packages`: Unity package manifest and lock file.
- `ProjectSettings`: Unity project settings.
- `Library`, `Temp`, `obj`, `Logs`, `.vs`, `UserSettings`: generated or local files; do not edit unless explicitly required.

# 当前能力盘点

以下结论基于当前项目代码、场景引用、Packages 和 ProjectSettings，不以文件名推断可用性。

| 能力 | 当前状态 | 可直接复用 | 主要证据与限制 |
|---|---|---:|---|
| Unity API | 可用 | 是 | Unity `6000.0.47f1`，C# 编译链可用 |
| 游戏框架 | 雏形 | 否 | 分层目录和战斗脚本存在，但没有统一应用层、生命周期契约和场景装配入口 |
| 有限状态机 | 部分可用 | 否 | `CombatStateMachine`、`ICombatState` 和 Idle/Move/Attack/Dodge 存在；状态直接调用切换，且 Attack 的退出约束会拒绝 Idle |
| UI 框架 | 基础可用 | 条件可用 | UGUI `2.0.0`、Canvas、EventSystem 存在；没有统一 UI 管理器，Demo UI 为运行时生成 |
| 对象池 | 不存在 | 否 | 未发现池接口、池生命周期或池实例 |
| 数据存储 | 部分可用 | 条件可用 | `SaveManager` 可写 JSON、支持农田/金币/天数/现实时间；无版本迁移、原子写入，库存未接入存档 |
| 背包 | 原型可用 | 否 | `InventoryManager` 支持买卖和堆叠，但使用全局单例、公开可变列表，未发现稳定场景装配和持久化 |
| Buff | 原型可用 | 否 | 支持持续时间、叠层和互斥组；没有效果回滚、属性应用、事件和测试 |
| AI 行为树 | 原型可用 | 否 | JSON 解析和函数注册存在；无函数生命周期管理、节点校验和行为树资产验证 |
| 任务系统 | 不存在 | 否 | 未发现任务数据、任务状态、奖励结算或任务管理器 |
| 输入系统 | 不可直接复用 | 否 | 两份 `.inputactions` 资产存在，代码仍使用旧 `Input`，`activeInputHandler` 为双模式，存在重复触发风险 |

# 可复用边界

- 可复用：`PlayerAnimationPresenter` 的动画表现接口、`SafeAreaFitter` 的安全区适配、`SaveManager` 的 Demo 存档模型、UGUI 基础组件。
- 暂不可复用：`CombatStateMachine` 作为稳定 FSM、`InventoryManager` 作为持久化背包、`BuffController` 作为完整 Buff 系统、`AIBehaviorTreeRunner` 作为生产 AI 框架。
- 不新增对象池和任务系统，直到出现明确的生成/回收或任务流程验收标准。
- 新功能必须先确认现有模块的场景引用、生命周期、错误边界和最小验证，再决定复用、修复或重写。

# 对抗式审查协议

每次涉及架构、框架或管理器的变更，必须单独检查以下问题：

- 是否把“有文件”误判为“可用能力”。
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

# Development Commands

```powershell
& "D:\Program Files\Unity 6000.0.47f1\Editor\Unity.exe" -projectPath "D:\project_unity\2D_RPGcombatsystem"
```

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore
```

```powershell
dotnet build .\Assembly-CSharp.Player.csproj --no-restore
```

Do not use `2D_RPGcombatsystem.sln` as the primary validation command; it currently contains duplicate `Assembly-CSharp` project names.

# Coding Guidelines

- Keep changes small and tied to the requested behavior.
- Preserve Unity `.meta` files. Add a `.meta` file for every new Unity asset or script.
- Prefer serialized fields for Unity object references wired in the Inspector.
- UI buttons, Toggles, AudioSources, finger nodes, and the first-tile button must be assigned through the Inspector.
- Add null checks around optional scene references and camera references.
- Do not hard-code Animator state transitions when a controller already uses Trigger parameters.
- Match existing Animator parameters exactly:
  - Hero run: `IsRun`
  - Sword attack trigger: `ATK_1`
  - Slash attack trigger: `ATK1`
- Do not rotate `SwordPivot` during attack clips if the animation clip already owns that rotation.
- For weapon aiming, keep mouse-world conversion camera-safe and isolated from attack animation timing.
- Avoid routine comments. Comment only non-obvious Unity lifecycle, asset, animation, or data-framework constraints.
- Keep combat extension points open:
  - New Magic behavior: add a `CMagicProperty` subclass and register/use it through config.
  - New Buff behavior: add explicit data/effect handling without bypassing duration, stack, and exclusivity rules.
  - New AI behavior: add/register an AI function; behavior-tree JSON composes it.
  - New projectile behavior: add launch/trajectory capability without coupling those two axes.
- Current script and asset paths include Chinese names; use exact paths and `-LiteralPath` in PowerShell.

# Testing Requirements

- For any C# script change, run both:
  - `dotnet build .\Assembly-CSharp.csproj --no-restore`
  - `dotnet build .\Assembly-CSharp.Player.csproj --no-restore`
- If a build fails because Unity, Defender, or another process locks `obj` or `Temp`, rerun once before reporting failure.
- For animation/input changes, verify in Unity Play Mode when possible:
  - movement toggles `IsRun`
  - attack input triggers sword `ATK_1`
  - attack input triggers slash `ATK1`
  - weapon, slash, camera, and Animator references are bound on the player hierarchy
- There is no formal project test suite beyond Unity compile/build validation unless tests are added.

# Security Guidelines

- This is a local Unity game prototype with no backend, Web3, authentication, or secret handling.
- Do not add network calls, credential storage, analytics, telemetry, or external service integrations unless explicitly requested.
- Do not commit or depend on machine-local generated files.

# Agent Instructions

- Read existing files before modifying them.
- Inspect actual scripts, Animator controllers, scene/prefab structure, package files, and current generated project files before changing behavior.
- Modify only files needed for the task.
- Do not overwrite scenes, prefabs, controllers, animation clips, or generated Unity assets blindly; inspect serialized data first.
- Do not edit `Library`, `Temp`, `obj`, `Logs`, `.vs`, or `UserSettings` unless the user explicitly requests it.
- Prefer `rg` and `Get-Content -LiteralPath` for reads.
- Use `apply_patch` for manual edits.
- Do not commit, push, or run destructive git commands unless explicitly requested.
- Report exact validation commands run and their result.
- If Unity Inspector bindings may need reassignment after a script field/name change, state that risk explicitly.
