---
alwaysApply: true
description: ARPG可操控角色移动模块（Playable动画+FSM）开发约定，当前为规划中/未实现
---
# 移动模块开发约定（arog-playable-movement-controller）

> 状态：**规划中 / 未实现**。当前仅有规格三件套（`.trae/specs/arog-playable-movement-controller/` 的 spec.md/tasks.md/checklist.md），尚无运行代码、场景与动画资产。任何人在此模块落地前，不得把其写入 AGENTS.md 的"当前能力盘点"当作已可用能力。

## 目标与边界

- 为 ARPG 玩家构建**纯移动演示原型**：六态 `Idle / Walk / Run / Jump / Skid(急停) / Turn(转身)`，用 **Playable API**（`AnimationPlayableGraph` + `AnimationMixerPlayable`）做动画融合，不用 Animator Controller 状态机。
- 与现有战斗管线**双套并存**：不改动 `CombatStateMachine`、`PlayerCombatBootstrap`、`基础移动`、`跳跃射箭`；默认不同时挂载到同一玩家对象，避免**双写 `rb.linearVelocity`**。
- 渲染管线为 **2D URP**，物理基于 `Rigidbody2D`。

## 分层（沿用四层 + 组合根）

- 数据：`Movement/PlayerMovementConfig`（ScriptableObject，速度/加速/跳跃/重力/落地掩码/阈值/转身时长/blendTime）。
- 决策：`Movement/PlayerMovementInputReader`（输入→平滑移动强度/冲刺/跳跃/急停/转向输入）→ `PlayerMovementStateMachine`。
- 执行：`Movement/States/{Idle,Walk,Run,Jump,Skid,Turn}State` + `PlayerMovementContext`；物理（加减速、重力、落地判定）由 `PlayerMovementController` 在 `FixedUpdate` 驱动。
- 表现：`Movement/PlayerAnimationGraph`（Playable 融合）+ Sprite flipX 朝向翻转。
- 组合：编辑器一键装配工具 `Editor/PlayerMovementSetup` + `MovementDemo` 场景。

## 关键状态约束（转换表为准）

- **Jump 仅地面态（Idle/Walk/Run）可触发**，且 `IsGrounded` 为真；Jump 中再次跳跃无效（无二段跳）；**仅落地判定命中可出** Jump，回归当前输入的 Walk/Run/Idle。
- **Skid 仅从高速 Run/Walk**（速率 >= `skidThresholdSpeed`）响应急停指令进入；`skidDuration` 内强减速归零，完成后回 Idle；恢复输入可回 Walk/Run。
- **Turn 依据期望朝向与当前朝向的二维夹角**：夹角 > `turnThresholdAngle` 且在地面、非转向中时进入；`turnDuration` 内平滑翻转与播放转身动画后回归 Walk/Run；同向输入不进入。项目为 2D 侧视，故"镜头朝向"按"期望移动方向映射到角色朝向"实现。

## 动画占位策略（现状）

现有 Hero 片段仅 `Hero Idle.anim / Hero Run.anim / Hero Attack.anim`，**无 Walk/Jump/Skid/Turn 专用片段**。原型阶段：Walk 复用 `Hero Run`（降 timeScale），Jump/Skid/Turn 复用 `Hero Idle` 占位；Playable 图必须预留真实片段接口，避免后续替换改动状态机。

## 验证命令（改动必跑，0 警告 / 0 错误）

```powershell
dotnet build .\Assembly-CSharp.csproj
dotnet build .\Assembly-CSharp.Player.csproj
dotnet build .\Assembly-CSharp-Editor.csproj  # Editor 装配工具改动时
```

Play Mode 在 `MovementDemo` 场景验证：六态切换稳定、输入响应及时、平坦地面/斜面/带高度差平台不穿地不抖动、动画跨态无跳变。

## 对抗式审查（对架构/管理器变更单独输出）

- 是否把"有文件"误判为"可用能力"（本模块未实现，须标规划中）。
- 是否存在第二输入入口、第二状态入口、第二份业务规则。
- 是否能从场景启动到实际调用，而非仅编译通过。
- 失败是否可观察（非法状态转换、空动画片段、无效 Config、无地面掩码）。
- 是否引入未请求的抽象、全局单例、隐藏副作用或不可回收资源（如 PlayableGraph 泄漏）。
