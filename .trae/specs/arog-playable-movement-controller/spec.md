# ARPG 可操控角色移动系统（Playable 动画 + FSM）Spec

## Why

现有 `CombatStateMachine` 仅覆盖 `Idle/Move/Attack/Dodge/CastBuff`，移动只有单一 `Move` 态（Walk/Run 不分）、无跳跃、无急停、无转身，且动画走 Animator Controller。ARPG 需要「行走/奔跑/跳跃/急停/转身」六态可控、动画用 Playable API 做融合、手感符合平台动作要求。本特性作为**独立移动演示原型**落地，与战斗管线**双套并存**，改动完全隔离。

## What Changes

- 新增独立模块 `Assets/C#/Movement/`，不改动 `CombatStateMachine`、`PlayerCombatBootstrap`、`基础移动` 等战斗管线（**隔离**，不破坏现有 2D URP 运行）。
- 实现六态移动 FSM：`Idle / Walk / Run / Jump / Skid(急停) / Turn(转身)`。
- 用 Unity **Playable API**（`AnimationPlayableGraph` + `AnimationMixerPlayable`）做状态间动画融合与速度平滑，不用 Animator Controller 状态机。
- 物理基于 `Rigidbody2D`：水平加速度/减速度、垂直重力、盒射线落地判定、Sprite flipX 朝向翻转。
- 新增可复用配置 `PlayerMovementConfig`（ScriptableObject）与编辑器一键装配工具。
- 新增 `MovementDemo` 演示场景，用于全场景测试六态切换。

## Impact

- Affected specs: 角色移动控制（新增）、Playable 动画桥接（新增）、地面检测与重力（新增）。
- Affected code（均为新增，不触碰现有文件）:
  - `Assets/C#/Movement/PlayerMovementController.cs`
  - `Assets/C#/Movement/PlayerMovementConfig.cs`
  - `Assets/C#/Movement/PlayerMovementStateMachine.cs`
  - `Assets/C#/Movement/IMovementState.cs`
  - `Assets/C#/Movement/PlayerMovementContext.cs`
  - `Assets/C#/Movement/States/{Idle,Walk,Run,Jump,Skid,Turn}State.cs`
  - `Assets/C#/Movement/PlayerAnimationGraph.cs`
  - `Assets/C#/Movement/PlayerMovementInputReader.cs`
  - `Assets/C#/Editor/PlayerMovementSetup.cs`
  - `Assets/Scenes/MovementDemo.unity` + 对应 `.meta`
- 资源现状（决定动画方案）：现有 Hero 片段 `Hero Idle.anim`、`Hero Run.anim`、`Hero Attack.anim`；**无 Walk/Jump/Skid/Turn 专用片段**。原型阶段 `Walk` 复用 `Hero Run`（降 timeScale），`Jump/Skid/Turn` 复用 `Hero Idle` 占位，Playable 图结构预留真实片段接口。

## ADDED Requirements

### Requirement: 六态移动 FSM
系统 SHALL 提供 `PlayerMovementStateMachine`，维护 `Idle/Walk/Run/Jump/Skid/Turn` 六态，实现 `Configure()` + `ChangeState()` + 非法转换可观察错误（`LogWarning`/`LogError`），并具备「未 Configure 时的自绑定回退」与单帧单次意图读取。

#### Scenario: 状态切换合法且可观察
- **WHEN** 任一状态请求切换到不允许的目标态
- **THEN** 状态机拒绝切换并输出 `[MovementFSM] 非法状态转换 X -> Y 被拒绝` 警告，日志带挂载对象名

#### Scenario: 装配回退自绑定
- **WHEN** 状态机未在 Awake 被显式 `Configure`（如仅手动挂组件）
- **THEN** `Start` 自动绑定 Rigidbody2D / Input / Config / Graph 并进入 `Idle`

### Requirement: Playable 动画融合与速度平滑
系统 SHALL 通过 `PlayerAnimationGraph` 封装 `AnimationPlayableGraph`，提供 `Play(clip, blendTime, speedMultiplier)` 在状态间做交叉淡化；所有移动态的速度变化（加速度/减速度/跳跃落地缓冲）SHALL 用 `Mathf.MoveTowards`/`Mathf.SmoothDamp` 平滑，避免瞬变。

#### Scenario: Walk→Run 交叉淡化
- **WHEN** 输入强度跨越行走/奔跑阈值
- **THEN** 图在 `blendTime` 内把权重从 Walk 片段转移到 Run 片段，且当前水平速度平滑逼近目标速度，无速度跳变

### Requirement: 输入强度决定行走/奔跑
系统 SHALL 从输入读取平滑后的移动强度；强度低于 `walkThreshold` 时进入 `Idle`，`[walkThreshold, runThreshold)` 行走，`>= runThreshold` 或按住 `sprintKey` 奔跑。

#### Scenario: 强度分级
- **WHEN** 输入强度 < `walkThreshold`
- **THEN** 状态进入 `Idle`，水平速度平滑归零
- **WHEN** 输入强度跨入 `[walkThreshold, runThreshold)`
- **THEN** 状态进入 `Walk`，以 `walkSpeed` 为目标
- **WHEN** 输入强度 >= `runThreshold`（或按住冲刺键）
- **THEN** 状态进入 `Run`，以 `runSpeed` 为目标

### Requirement: 跳跃仅地面可触发 + 落地回归
系统 SHALL 仅在处于 `Idle/Walk/Run`（地面态）且 `IsGrounded` 时响应跳跃；跳跃中施加垂直冲量并施加重力；当落地判定（向下盒射线命中地面层）成立时，从 `Jump` 回归到基于当前输入的 `Walk/Run/Idle`。

#### Scenario: 地面起跳
- **WHEN** 角色在地面且处于 `Idle/Walk/Run`，玩家按下跳跃
- **THEN** 进入 `Jump`，`rb` 获得 `jumpForce` 垂直速度，重力生效
- **WHEN** 跳跃中再次按下跳跃
- **THEN** 不触发二段跳（忽略）

#### Scenario: 落地回归
- **WHEN** 跳跃中向下盒射线命中地面层
- **THEN** 离开 `Jump`，依据当前输入进入 `Walk/Run/Idle`，且落地时为防硬着地在落地帧施加速度平滑

### Requirement: 急停（Skid）从高速平滑减速
系统 SHALL 在 `Run/Walk` 高速状态下收到刹车/反向指令时进入 `Skid`，在 `skidDuration` 内以强减速度平滑降至 0；减速完成回 `Idle`；若减速完成前输入恢复，可切回 `Walk/Run`。

#### Scenario: 急停减速
- **WHEN** 角色处于 `Run`（当前速率 >= `skidThresholdSpeed`）且触发急停指令
- **THEN** 进入 `Skid`，播放急停动画，水平速度在 `skidDuration` 内平滑归零
- **WHEN** 减速完成后无输入
- **THEN** 进入 `Idle`

### Requirement: 转身按夹角阈值自动触发
系统 SHALL 计算「期望朝向（输入/移动方向）」与「角色当前朝向（`flipX`/面朝方向）」的二维夹角；当夹角绝对值超过 `turnThresholdAngle` 且角色在地面、非转向中时，进入 `Turn`，在 `turnDuration` 内平滑完成朝向过渡（翻转 Sprite + 播放转身动画）后回归 `Walk/Run`。
> 说明：项目为 2D 侧视 URP，故「镜头朝向」按 2D 语义实现为「期望移动方向映射到角色朝向」。

#### Scenario: 反向输入触发转身
- **WHEN** 角色朝右移动中玩家输入反方向，夹角超过阈值
- **THEN** 进入 `Turn`，平滑翻转朝向并播放转身动画，随后按新朝向进入 `Walk/Run`

#### Scenario: 同向输入不转身
- **WHEN** 输入方向与当前朝向夹角小于阈值
- **THEN** 不进入 `Turn`，直接保持原朝向移动

### Requirement: 物理与地形兼容
移动控制器 SHALL 通过 `Rigidbody2D` 与碰撞体实现碰撞检测，重力（`gravityScale`）与最大下落速度受控，落地判定对平坦地面、斜面、带高度差平台均基于盒射线 + 层级掩码，保证跨地形不穿地、不抖动。

#### Scenario: 不同地形移动稳定
- **WHEN** 角色在平坦地面、斜面、可落脚平台上移动/跳跃
- **THEN** 不穿地、不下陷、落地不抖动，状态切换稳定

### Requirement: 与现有战斗管线隔离
本模块 SHALL 完全独立于 `CombatStateMachine`/`PlayerCombatBootstrap`/`基础移动`（纯移动演示原型），不改动其任何脚本、场景引用或装配顺序；默认不同时挂载于同一玩家对象，避免双写 `rb.linearVelocity`。

#### Scenario: 并存互不干扰
- **WHEN** 演示场景只挂本移动控制器
- **THEN** 现有 `SampleScene`/战斗管线行为不变，编译两套并存无回退
