# Tasks
- [x] Task 1: 复用并完善 MonsterPatrolPath 路径点管理组件
  - [x] SubTask 1.1: MonsterPatrolPath.cs（Transform 列表 + 循环/往返取点 + 最近点查询）
  - [x] SubTask 1.2: Scene 视图路径点可视化（Gizmos 点位 + 连线）
  - [x] SubTask 1.3: 补充 AddPoint 供 Editor 工具使用

- [x] Task 2: 完善 MonsterPatrolController 状态机控制器
  - [x] SubTask 2.1: Idle / Patrol / Chase 三态状态机
  - [x] SubTask 2.2: NavMeshAgent 2D 适配：XZ 平面导航 ↔ XY 平面表现（ToNavPosition 映射、Warp 对齐）
  - [x] SubTask 2.3: 玩家检测（检测范围 / 丢失范围 / 攻击范围）、恢复巡逻逻辑
  - [x] SubTask 2.4: Inspector 参数：巡逻速度、追击速度、检测半径、丢失距离、到点距离、等待时间
  - [x] SubTask 2.5: IsRun 参数防御（Slime 控制器无该参数）

- [x] Task 3: 改造 EnemyBase，移动控制移交 MonsterPatrolController
  - [x] SubTask 3.1: 未挂载巡逻控制器时保留原静止行为；挂载后不再清零速度
  - [x] SubTask 3.2: 受击时暂停导航（isStopped + ResetPath），保留击退，受击结束 Warp 对齐恢复
  - [x] SubTask 3.3: 保留攻击/受击入口，PlayerAttackTrigger → ReceiveDamage → TakeDamage 链路未破坏

- [x] Task 4: 提供演示场景搭建工具（Editor 一键配置）
  - [x] SubTask 4.1: Tools/怪物巡逻/Create Patrol Demo Setup（创建地面 + NavMeshSurface + 烘焙）
  - [x] SubTask 4.2: 创建巡逻路径容器 + 3 个路径点
  - [x] SubTask 4.3: 为选中怪物挂载 NavMeshAgent + MonsterPatrolController，关联路径与玩家

- [x] Task 5: 测试验证
  - [x] SubTask 5.1: `dotnet build Assembly-CSharp.csproj` 与 Player 项目编译通过（0 错误 0 警告）
  - [x] SubTask 5.2: Play Mode 手测清单已提供（巡逻往返、玩家进入追击、离开返回、受击暂停）
  - [x] SubTask 5.3: 边界检查：无路径点进入 Idle、目标不可达不卡死、NavMesh 未烘焙走 MoveDirect

- [x] Task 6: 交付与文档
  - [x] SubTask 6.1: 编写参数配置说明与烘焙步骤文档（docs/怪物巡逻系统使用与配置文档.md）
  - [x] SubTask 6.2: 维护优化建议（文档第七节）

# Task Dependencies
- [Task 3] depends on [Task 2]（EnemyBase 需配合 MonsterPatrolController 的暂停/恢复）
- [Task 4] depends on [Task 1, Task 2, Task 3]
- [Task 5] depends on [Task 4]
- [Task 6] depends on [Task 5]
