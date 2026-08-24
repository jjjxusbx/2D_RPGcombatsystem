# Tasks

> 本任务为「纯移动演示原型」，模块隔离在 `Assets/C#/Movement/`，不触碰战斗管线。建议按 数据→FSM→Playable→物理→场景 顺序推进；无依赖任务可并行。

- [ ] Task 1: 建立移动数据模型 `PlayerMovementConfig` 与上下文
  - [ ] 1.1 新建 `Movement/PlayerMovementConfig.cs`（ScriptableObject）：walkSpeed、runSpeed、acceleration、deceleration、skidDuration、skidThresholdSpeed、jumpForce、gravityScale、maxFallSpeed、groundCheckDistance、groundLayers、walkThreshold、runThreshold、turnThresholdAngle、turnDuration、blendTime。
  - [ ] 1.2 新建 `Movement/IMovementState.cs`：`Enter/Execute/Exit/CanTransitionTo/GetStateName`。
  - [ ] 1.3 新建 `Movement/PlayerMovementContext.cs`：持有 Rigidbody2D、Config、Input、AnimationGraph、StateMachine、意图帧缓存、当前朝向、地面/空中、当前水平速率。
  - [ ] 1.4 为新增脚本补 `.meta`。

- [ ] Task 2: 实现六态移动 FSM（PlayerMovementStateMachine）
  - [ ] 2.1 新建 `Movement/States/{Idle,Walk,Run,Jump,Skid,Turn}State.cs`，实现各态 Enter/Execute/Exit，读写上下文并请求动画与朝向。
  - [ ] 2.2 定义合法转换表并在 `CanTransitionTo` 严格校验（Jump 仅地面态可入；Jump 仅落地可出；Skid 仅高速 Run/Walk 可入；Turn 依据夹角阈值）。
  - [ ] 2.3 新建 `Movement/PlayerMovementStateMachine.cs`：`Configure()` + `Start` 自绑定回退 + `ChangeState()` + 非法转换 `LogWarning`/`LogError` + 单帧单次意图读取。
  - [ ] 2.4 各进入态调用 `context.Graph.Play(clip, blendTime, speedMultiplier)`，退出态修正朝向/速度，规避瞬变。

- [ ] Task 3: 封装 Playable 动画图（PlayerAnimationGraph）
  - [ ] 3.1 新建 `Movement/PlayerAnimationGraph.cs`：持有 `AnimationPlayableGraph`、`AnimationMixerPlayable`，映射各态到 AnimationClip，支持 `Play(clip, blendTime, speedMultiplier)` 与权重交叉淡化。
  - [ ] 3.2 处理图生命周期：`Awake` 创建/`OnDestroy` 销毁，未配置 0 片段时不抛异常（可观察告警）。
  - [ ] 3.3 片段占位策略：Walk 复用 `Hero Run`（降 timeScale）、Jump/Skid/Turn 复用 `Hero Idle`，真实片段接口预留。

- [ ] Task 4: 角色控制器与物理（PlayerMovementController）
  - [ ] 4.1 新建 `Movement/PlayerMovementController.cs`：持有 Rigidbody2D、Config、Input、Graph、FSM；`Update` 读意图驱动 FSM，`FixedUpdate` 应用水平加减速、重力与 `maxFallSpeed`。
  - [ ] 4.2 落地判定：向下盒射线基于 `groundLayers`，返回 `IsGrounded`；跳跃仅地面可触发，落地帧做速度平滑。
  - [ ] 4.3 朝向：`flipX` 依据期望朝向；`Turn` 状态在 `turnDuration` 内平滑过渡并复用 `PlayerAnimationPresenter.ApplyFacing` 翻转逻辑（或等价格式）。
  - [ ] 4.4 新建 `Movement/PlayerMovementInputReader.cs`：读取输入并输出平滑移动强度（含模拟量/冲刺键）、跳跃、急停、转向相关布尔。

- [ ] Task 5: 编辑器一键装配 + MovementDemo 演示场景
  - [ ] 5.1 新建 `Editor/PlayerMovementSetup.cs`：菜单一键把移动控制器/Config/Input/Graph 挂载到选中角色并烘焙落地掩码，自动生成 `MovementDemo` 场景配置。
  - [ ] 5.2 新建 `Assets/Scenes/MovementDemo.unity`（含相机、含高差/斜面平台、参照角色、六态可测布局），补 `.meta`。
  - [ ] 5.3 场景挂载 UI 显示当前状态名，便于观察切换。

- [ ] Task 6: 验证
  - [ ] 6.1 `dotnet build .\Assembly-CSharp.csproj`（运行层）
  - [ ] 6.2 `dotnet build .\Assembly-CSharp.Player.csproj`
  - [ ] 6.3 `dotnet build .\Assembly-CSharp-Editor.csproj`（编辑器装配工具改动时必跑）
  - [ ] 6.4 Unity Play Mode 在 `MovementDemo` 验证六态切换、输入响应、地形兼容与动画融合。

# Task Dependencies
- Task 2 依赖 Task 1（Context/State 接口）。
- Task 3 依赖 Task 1（Context 引用 Graph）。
- Task 4 依赖 Task 1/2/3。
- Task 5 依赖 Task 4（控制器完成后才能装配场景）。
- Task 6 依赖全部前置任务。
- Task 1.1/1.2/1.3 可并行；Task 2 内各 State 文件可并行。
