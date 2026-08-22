# Spec：P0 代码清理（探针/死代码/警告/日志刷屏）

> 定位：清除运行时残留调试代码与死代码，使运行时两 csproj 达到 0 警告 0 错误。
> 范围：仅运行时脚本清理，不触碰 `Assets/C#/Editor/` 与 `Assets/Plugins/`（不影响 Unity 编辑器）。
> 状态：计划阶段（本 spec 用于驱动本次计划式开发）。

## 1. 需求

| # | 需求 | 验收 |
|---|---|---|
| R1 | 消除 `PlayerAttackTrigger.cs` 的 CS0219 警告（`branch` 变量赋值但未用） | `dotnet build` 该文件所在 csproj 无 CS0219 |
| R2 | 删除 `PlayerAttackTrigger.cs` 的 `[CombatHitDebug]` 高频调试日志 | Console 无 `[CombatHitDebug]` |
| R3 | 删除 CameraFollow2D / PlayerAttackTrigger 内残留的空 `#region agent log H*` 注释 | 全库无 `agent log` 残留注释 |
| R4 | 删除无引用的空壳 `CombatHitDebugProbe.cs` | 文件删除；无 GUID 引用、编译通过 |
| R5 | 删除无引用的遗留脚本 `Test.cs` / `Test4.cs` | 文件删除；无 GUID 引用、编译通过 |

## 2. 非目标（刻意不做）

- 不改 `Assets/C#/Editor/*`（CombatSystemSetup/CompositionSetup/TurnBasedSetup/UiShowcaseSetup/MonsterPatrolSetup）。
- 不改 `Assets/Plugins/ES`（第三方）。
- 不动物理/动画/行为逻辑，仅删残留代码与日志。
- 不做输入系统迁移、性能优化等 P1/P2，不纳入本次。

## 3. 事实依据（已核实）

- `D:\.cursor` 写盘探针已不存在；`#if UNITY_EDITOR` 仅剩合法 Editor 脚本（BatchSpriteSlicer + 4 个 Setup）。
- `GameoOjectHide` 类/文件不存在，仅剩 `GameOjectHide.cs`（干净，文件名匹配）。重复组件问题已解决。
- `Test.cs`(GUID d837c2bd...) / `Test4.cs`(GUID 6b4a6f7e...) / `CombatHitDebugProbe.cs`(GUID 1471c7a2...) 均无场景/预制体/脚本引用。
- `PlayerAttackTrigger.cs` 现存：`branch` 未用变量（CS0219）、`OnTriggerEnter2D` 4 条 `[CombatHitDebug]` 日志、空 `#region agent log H1/H2` 注释。

## 4. 风险与约束

- 用户约束：不影响 UnityEditor、不出问题。→ 严禁删除或改动 Editor 工具脚本；所有删除目标先确认无引用再删；每步先读后改。
- 删除脚本需同时删除对应 `.meta`，避免 Unity 报 dangling GUID。
- `GameOjectHide.cs`（被场景引用）不在本次删除范围。
