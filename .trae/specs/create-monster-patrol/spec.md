# 怪物巡逻与追击系统 Spec

## Why
当前项目已有 `EnemyBase` 战斗行为和 2D Tilemap 场景，但缺少可复用的怪物移动决策层。需要将巡逻、追击、闲置状态统一为状态机，并通过 Unity AI Navigation 提供可配置、可恢复的路径寻路能力。

## What Changes
- 新增可复用的怪物 AI 状态机，支持 `Idle`、`Patrol`、`Chase` 三种状态。
- 新增巡逻路径点管理组件，支持 Inspector 配置多个路径点并在 Scene 视图中可视化连线/标记。
- 使用 Unity AI Navigation 的 `NavMeshSurface`/`NavMeshAgent` 完成 2D 场景导航；Agent 运动与现有 2D Rigidbody/Animator 表现层保持边界清晰。
- 新增怪物控制器，配置巡逻速度、追击速度、检测半径、攻击范围、丢失目标距离、到点距离和闲置时间。
- 玩家进入检测范围后从巡逻/闲置切换到追击；玩家离开视野范围后返回最近的巡逻路径点并继续往返巡逻。
- 为路径丢失、NavMesh 不可用、目标引用为空等情况提供可恢复的保守行为，避免状态卡死。
- 更新 `SampleScene` 或新增演示场景，包含可烘焙导航区域、巡逻点、怪物控制器和玩家对象配置。
- 提供 Unity Inspector 参数配置与 NavMesh 烘焙说明。

## Impact
- Affected specs: 怪物 AI、导航寻路、状态机、2D 场景配置、战斗表现衔接。
- Affected code: `Assets/C#/EnemyBase.cs`、新增怪物 AI/路径点/状态脚本、`Assets/Scenes/SampleScene.unity` 或演示场景及其 `.meta` 文件、必要的 Animator 参数配置。
- Package dependency: 使用项目已安装的 `com.unity.ai.navigation` 2.0.14。

## ADDED Requirements
### Requirement: 可复用的怪物 AI 状态机
系统 SHALL 以状态机管理怪物的闲置、巡逻和追击状态，状态切换由感知条件和寻路结果驱动，而不是由多个脚本分散修改。

#### Scenario: 怪物没有有效目标
- **WHEN** 怪物启动且没有检测到玩家
- **THEN** 怪物进入 `Patrol`；没有有效路径点时进入 `Idle`，并保持可恢复检查。

#### Scenario: 玩家进入检测范围
- **WHEN** 带有 `Player` Tag 的玩家进入怪物检测半径，且目标可被导航系统使用
- **THEN** 怪物停止当前巡逻目标，切换到 `Chase` 并以追击速度向玩家移动。

#### Scenario: 玩家离开视野范围
- **WHEN** 追击中的玩家离开丢失目标距离或目标无效
- **THEN** 怪物停止追击，选择最近的有效巡逻点作为恢复点，并继续原方向的往返巡逻。

### Requirement: NavMesh 导航
系统 SHALL 使用 `NavMeshAgent` 在烘焙后的 NavMesh 上移动，并处理目标点不可达、Agent 未启用和 NavMesh 未烘焙等情况。

#### Scenario: 巡逻点可达
- **WHEN** 当前巡逻点位于有效 NavMesh 上且路径状态为可达
- **THEN** 怪物平滑移动到该点，到达后等待配置的闲置时间并切换到下一个点。

#### Scenario: 路径不可用
- **WHEN** 当前巡逻目标不可达或导航路径丢失
- **THEN** 怪物不进入无限等待，尝试跳过该点或回到最近有效点；所有点均无效时进入 `Idle`。

### Requirement: Inspector 配置与路径可视化
系统 SHALL 在 Inspector 暴露路径点容器、巡逻速度、追击速度、检测范围、攻击范围、丢失范围、到点阈值和等待时间，并在编辑器 Scene 视图显示路径点和路径连线。

#### Scenario: 设计师调整路径
- **WHEN** 设计师在 Inspector 增删或移动路径点
- **THEN** Scene 视图即时显示点位与路径顺序，运行时控制器使用最新路径配置。

### Requirement: 现有战斗系统兼容
系统 SHALL 保留 `EnemyBase` 的受伤和攻击表现能力；巡逻/追击移动不得破坏现有 Animator、攻击触发器和 Rigidbody2D 绑定。

#### Scenario: 怪物进入攻击范围
- **WHEN** 追击状态下玩家进入攻击范围
- **THEN** AI 停止或减速到攻击距离，向现有攻击表现层发出攻击请求；攻击实现不重复复制 `EnemyBase` 的伤害触发逻辑。

### Requirement: 演示场景与验证
系统 SHALL 提供一个可直接运行的演示场景配置，包含导航烘焙环境、至少两个巡逻点、怪物和玩家，并能通过 Play Mode 验证完整状态循环。

#### Scenario: 演示场景运行
- **WHEN** 在 Unity Play Mode 启动演示场景
- **THEN** 怪物可在多个点之间往返巡逻，玩家靠近时追击，玩家远离后回到路线继续巡逻。
