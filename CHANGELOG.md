# CHANGELOG

All notable changes to the Unity 2D RPG Combat System project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added (新增文档)

- **README.md** - 项目根目录主入口文档
  - 快速开始指南（环境要求/编译验证/Unity 编辑器装配）
  - 四层架构图 + 设计原则说明
  - 目录结构树
  - 核心系统概览（战斗 FSM/属性系统/Rogue 跑局/怪物巡逻）
  - 开发工作流 + Spec 驱动开发说明
  - 版本路线图 + 文档导航
  - 贡献指南（分支策略/提交规范/代码规范/编译门禁）

- **docs/README.md** - 文档索引中心
  - 核心设计文档清单
  - 游戏设计文档清单
  - 系统配置文档清单
  - PRD 产品需求文档清单
  - Spec 驱动开发目录索引
  - 外部资源链接

- **docs/v0.2-PRD-输入系统迁移.md** - v0.2 版本产品需求文档
  - 输入系统迁移方案（Input System 接入 + InputActionAsset 配置）
  - FSM 扩展：JumpState + 武器切换体系
  - 用户故事与验收标准（5 个 US）
  - 技术实现细节（Action Map 配置表/状态转换图/代码示例）
  - 测试验证清单（编译门禁 + Play Mode 行为清单）
  - 迁移策略与风险缓解
  - 文件变更清单 + 参考资料

### Changed (文档更新)

- **docs/版本计划.md**
  - v0.1.5 状态：⏳ 下一目标 → ✅ 开发完成，待验收验证
  - v0.1.5 章节扩展：交付物清单 + 待验证项
  - v0.2 状态：待排 → 📋 PRD 完成，待排期开发

---

## [v0.1.5] - 2026-08-24 (待发布)

### Added (v0.1.5 交付)

#### Roguelike 跑局循环

- **RogueRoomFlowController** - 局会话房间流元循环
  - 5 种房间类型：Combat / Altar / Boss / Settlement
  - 自动房间推进：战斗 → 祭坛 → 战斗 × 3 → BOSS → 结算
  - 击杀订阅：监听敌人 `ChaState.onDeath` 触发 `RunManager.NotifyKill()`
  - 房间清空判定：击杀计数触发 `NotifyRoomCleared()` → 推进下一房间
  - `[Diagnostics]` 房间转换日志

- **FragmentDrop + FragmentSpawner** - 碎片掉落系统
  - `FragmentSpawner` 订阅敌人死亡事件，在死亡位置生成碎片实例
  - `FragmentDrop` 碰撞触发器：玩家进入 `PickupRange` 自动拾取
  - 碎片数量基于 `ChaState.Growth` 属性计算
  - 拾取后调用 `RunManager.AddFragments(amount)`

- **AltarChoiceUI** - 祭坛三选一面板
  - 5 项基础强化池：Atk / MaxHealth / AttackRate / MoveSpeed / Growth
  - 随机抽取 3 项展示（UI 按钮 + 文本标签）
  - 选中后调用 `RunManager.ApplyRoomBuff()` 即时生效
  - MaxHealth 强化顺带补血（玩家感知"生效了"）
  - 支持隐藏/显示切换

- **SettlementUI** - 结算面板
  - 展示本局碎片 / 击杀数 / 累计晶核
  - 胜负文案区分（"通关成功" / "冒险失败"）
  - 两个按钮：「再开一局」调用 `flow.StartRun()` / 「回营地」重载场景
  - 晶核数据从 `RunCurrencyStore` 读取

- **RunManager 增强** - 局会话生命周期管理
  - `StartRun()` - 清掉上一局强化（`RemoveModifiersFromSource`）
  - `EndRun(bool isWin)` - 碎片结算晶核 + 强化清场 + `RunCurrencyStore` 持久化
  - `ApplyRoomBuff(AttributeModifier)` - 祭坛强化入口 + MaxHealth 自动补血
  - `AddFragments(int)` / `NotifyKill()` / `NotifyRoomCleared()` - 数据采集点

- **RunCurrencyStore** - 局外货币持久化
  - 版本号 v0.1.5 + 版本校验
  - 原子写入：`.tmp` 临时文件 → `File.Replace` 原子替换
  - 损坏/版本不符 → 回退 0 并告警，不抛出异常
  - 无状态工具类：零静态全局状态

#### 编辑器工具

- **RogueSetup** - Roguelike Demo 一键装配工具
  - 菜单路径：`Tools → 局会话 → 装配 Rogue Demo（房间流/掉落/祭坛/结算）`
  - 幂等设计：重复执行不叠加组件
  - 自动查找/创建 `RogueSystem` 根对象 + `Canvas` + `EventSystem`
  - 动态创建祭坛/结算 UGUI 面板（避免手写场景 YAML）
  - 自动绑定 `RunManager` / `RogueRoomFlowController` / `FragmentSpawner` 引用
  - `[Diagnostics]` 装配完成日志

### Changed

- **版本计划.md**
  - v0.1.5 从"下一目标"升级为"开发完成，待验证"
  - 新增交付物清单（6 个核心模块 + 编辑器工具）

- **README.md** - 本文档的根目录版本（新增）

### Fixed

- **CombatContext.Intent** - 新增意图字段（v0.1 引入，单帧单次读取）

---

## [v0.1] - 2026-08-10

### Added

- **PlayerCombatBootstrap** - 组合根（Composition Root）
  - 单一装配入口：`Awake()` 顺序装配数据/决策/表现/执行层
  - 旧脚本仲裁：`基础移动` 让权 / `跳跃射箭` 禁用
  - `Start()` 触发 `RuntimeDiagnostics.Run()` 输出装配报告

- **RuntimeDiagnostics** - 运行时自检工具
  - 输出 `[Diagnostics]` 日志（问题项 `LogWarning` + 信息项 `Log`）
  - 装配缺失/引用丢失/技能未配置即时可见

- **CompositionSetup** - 编辑器装配工具
  - 菜单路径：`Tools → 战斗系统 → Compose Player Combat（核心框架装配）`
  - 幂等：重复执行不重复挂载

- **CombatStateMachine 加固**
  - `Configure()` 装配接口
  - `Start()` 自绑定回退
  - Intent 缓存（单帧单次读取）
  - `ChangeState` 非法转换可观察错误

- **DodgeState 修复** - 消除闪避后状态卡死
  - `CanTransitionTo` 新增 `IdleState`
  - `RequestExitToIdle()` 可用

- **PlayerInputReader 配置化** - 键位 `SerializeField` 化
  - `sprintKey` / `dodgeKey` / `skillKey` / `attackMouseButton` 可 Inspector 配置

- **PlayerAnimationPresenter** - 新增参数存在性守卫
  - `SetBoolIfExists` / `SetTriggerIfExists` 避免 Animator 参数缺失报错

### Changed

- **AttackState** - 删除旧 `Input` 死代码 `Attack()` 方法
- **IdleState / MoveState** - 改用 `ctx.Intent`（单帧单次读取）
- **版本计划.md** - 建立里程碑规划文档
- **测试报告.md** - 建立编译/功能/兼容/性能/安全验证框架

---

## [0.1.0] - 2026-08-05 (基线)

### Added

- 怪物巡逻系统（`MonsterPatrolController` + `MonsterPatrolPath` + NavMesh）
- 交易系统（`InventoryManager` + `ShopPanel`）
- 基础战斗系统（`ChaState` / `Attribute` / `CombatStateMachine`）
- TurnBased 回合制原型（`TurnManager` + 网格地图）
- UI 展示场景（`UiShowcase` + 6 个界面设计）

### Fixed

- `MonsterPatrolSetup` 类型错误修复（`PatrolPathComponent` → `MonsterPatrolPath`）
- `NavMeshSurface` 命名空间补充（`Unity.AI.Navigation`）

---

**维护规则**:
- 每个版本交付必须填写"上线复盘模板"（版本计划.md 第 4 节）
- PRD 文档（如 v0.2-PRD）在版本完成归档至 `docs/prd/` 目录
- 重大功能变更必须更新本文件
