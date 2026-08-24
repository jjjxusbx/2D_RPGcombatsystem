# Checklist

> 验收对照 `docs/版本计划.md` 与 AGENTS.md 的构建/Play Mode 要求；所有检查在 `MovementDemo` 场景完成。

- [ ] 六态 FSM（Idle/Walk/Run/Jump/Skid/Turn）实现，非法转换有 `LogWarning/LogError` 且日志带对象名
- [ ] `Configure` 装配与未 Configure 的自绑定回退两条路径均能正常进入 Idle
- [ ] Playable 图谱（AnimationPlayableGraph + Mixer）Walk→Run 在 blendTime 内交叉淡化，水平速度无瞬变
- [ ] 输入强度分级：<walkThreshold→Idle、[walk, run)→Walk、>=runThreshold 或冲刺→Run，速度平滑逼近目标
- [ ] 跳跃仅地面态（Idle/Walk/Run）可触发；Jump 中再次跳跃不触发二段跳
- [ ] 跳跃下落命中地面层（盒射线 + groundLayers）后从 Jump 回归当前输入的 Walk/Run/Idle，落地帧速度平滑
- [ ] Run 高速态收到急停指令进入 Skid，skidDuration 内平滑减速归零，完成后回 Idle；恢复输入可回 Walk/Run
- [ ] 期望朝向与当前朝向夹角超过 turnThresholdAngle 且在地面时进入 Turn，平滑翻转朝向并播转身动画，随后回归 Walk/Run；同向输入不进入 Turn
- [ ] Rigidbody2D 碰撞 + 重力 + maxFallSpeed 生效；在平坦地面/斜面/带高度差平台移动不穿地、不抖动、落地稳定
- [ ] 与战斗管线隔离：未改动 CombatStateMachine/PlayerCombatBootstrap/基础移动；演示场景仅挂本移动控制器，无双写 rb.linearVelocity
- [ ] 编译验证：三个 csproj 均 0 警告 / 0 错误（`Assembly-CSharp`、`Assembly-CSharp.Player`、`Assembly-CSharp-Editor`）
- [ ] Editor 一键装配工具可生成 MovementDemo 场景配置并正确挂载控制器/Config/Input/Graph
