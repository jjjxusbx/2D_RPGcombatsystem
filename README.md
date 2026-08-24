# Unity 2D RPG Combat System

> **灰烬矿脉 / Ashes Mine Vein** — Unity 6 横版 2D 动作游戏原型，采用 FSM 战斗、属性系统、怪物巡逻（NavMesh）与 Roguelike 跑局循环。

**版本**: v0.1.5 (2026-08-24) | **引擎**: Unity 6000.0.47f1 | **状态**: 功能开发中

---

## 目录

- [快速开始](#快速开始)
- [项目架构](#项目架构)
- [核心系统](#核心系统)
- [开发工作流](#开发工作流)
- [版本路线图](#版本路线图)
- [文档导航](#文档导航)
- [贡献指南](#贡献指南)

---

## 快速开始

### 环境要求

- Unity 6000.0.47f1 或更高版本
- Windows 10/11 (当前开发环境)
- Visual Studio 2022 / Rider (C# IDE)

### 克隆与打开

```bash
git clone https://github.com/your-org/2D_RPGcombatsystem.git
cd 2D_RPGcombatsystem
```

双击 `Assembly-CSharp.sln` 或直接打开 Unity Hub 加载项目。

### 编译验证

```powershell
# 运行时主程序集
dotnet build .\Assembly-CSharp.csproj --no-restore

# 运行时 Player 程序集
dotnet build .\Assembly-CSharp.Player.csproj --no-restore

# 编辑器程序集
dotnet build .\Assembly-CSharp-Editor.csproj --no-restore
```

**验收标准**: 0 错误 / 0 警告 + DLL 生成。

### Unity 编辑器内装配

1. 打开 `Assets/Scenes/SampleScene.unity`
2. 选中玩家 Hero 对象
3. 菜单栏 → `Tools → 战斗系统 → Compose Player Combat（核心框架装配）`
4. 进入 Play Mode，检查 Console 的 `[Diagnostics]` 日志（应零问题项）

### 运行 Roguelike Demo

```powershell
# 在 Unity 编辑器内
Tools → 局会话 → 装配 Rogue Demo（房间流/掉落/祭坛/结算）
```

进入 Play Mode 后体验完整跑局循环：战斗房 → 祭坛强化 → BOSS → 结算。

---

## 项目架构

### 四层架构 + 组合根

本项目采用严格的**四层架构**，通过单一组合根（Composition Root）装配，避免双入口问题。

```
┌─────────────────────────────────────────────────────┐
│                   Composition Root                    │
│         PlayerCombatBootstrap / RunManager            │
└─────────────────────────────────────────────────────┘
          ↓              ↓               ↓
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  数据层       │ │  表现层       │ │  决策层       │
│ ChaState     │ │ PlayerAnim   │ │ PlayerInput  │
│ Attribute    │ │ Presenter    │ │ Reader       │
│ SkillData    │ │              │ │ CombatDec    │
│ BuffData     │ │              │ │ sion         │
└──────────────┘ └──────────────┘ └──────────────┘
          ↓
┌──────────────┐
│  执行层       │
│ CombatState  │
│ Machine     │
│ SkillExec    │
│ MonsterPat   │
│ rolController│
└──────────────┘
```

**设计原则**:
- **单一装配入口**: 所有组件在 `PlayerCombatBootstrap.Awake()` 内顺序装配
- **旧脚本仲裁**: 不删除遗留脚本，而是显式禁用/让权，避免双写 `rb.linearVelocity`
- **单帧单次读取**: `CombatStateMachine.Update()` 每帧只调用一次 `GetIntent()`
- **失败可观察**: `RuntimeDiagnostics` 输出装配问题清单，运行时错误在编辑器可见

### 目录结构

```
Assets/
├── C#/                          # 所有 C# 脚本
│   ├── CombatSystem/           # 战斗上下文 + FSM 状态机
│   │   └── State/              # Idle/Move/Attack/Dodge/CastBuff
│   ├── CombatSystem/ECS/       # Buff ECS 组件（独立 asmdef）
│   ├── Execution/              # 执行层
│   │   ├── AI/                 # 怪物巡逻（NavMesh）
│   │   └── HitDetection/       # 命中盒检测
│   ├── Decision/               # 输入 → 意图抽象
│   ├── Presentation/           # 动画/朝向表现层
│   ├── Composition/            # 装配根 + 运行时自检
│   ├── TurnBased/              # 独立回合制原型（网格战斗）
│   ├── Roguelike/              # v0.1.5 跑局循环模块
│   │   ├── Run/                # RunManager + RunCurrencyStore
│   │   ├── Flow/               # RogueRoomFlowController
│   │   ├── Reward/             # FragmentDrop/Spawner
│   │   └── UI/                 # AltarChoiceUI + SettlementUI
│   ├── Editor/                 # 编辑器工具
│   │   ├── CombatSystemSetup.cs
│   │   ├── RogueSetup.cs       # v0.1.5 一键装配
│   │   ├── MonsterPatrolSetup.cs
│   │   └── TurnBasedSetup.cs
│   ├── RunManager.cs           # 局会话生命周期
│   ├── RunCurrencyStore.cs     # 局外货币持久化（版本化 + 原子写）
│   ├── ChaState.cs             # 统一属性容器（8 属性 + 修饰器管线）
│   ├── Attribute.cs            # 属性值 + 五段式修饰器
│   ├── CombatData.cs           # SkillData/MagicEffect/BuffData
│   ├── BuffSystem.cs           # BuffController + AttributeModifier
│   ├── EnemyBase.cs            # 敌人基类（HP/死亡/受击）
│   ├── 基础移动.cs              # 旧脚本（FSM 模式下让权）
│   └── 跳跃射箭.cs              # 旧脚本（v0.2 迁移前禁用）
├── Scenes/
│   ├── SampleScene.unity       # 主场景（战斗 + 跑局 Demo）
│   ├── UiShowcase.unity        # UI 展示场景
│   └── Test.unity              # 测试场景
├── 动画/                       # 动画控制器/片段
├── Arts/                       # Tilemap/音频/UI 素材
├── docs/                       # 设计文档 + 版本计划 + 测试报告
├── .trae/
│   ├── specs/                  # Spec 驱动开发目录
│   │   ├── p0-code-cleanup/
│   │   ├── create-monster-patrol/
│   │   └── arog-playable-movement-controller/
│   └── rules/                  # 场景触发协作规则
└── Packages/                   # Unity 包清单
```

---

## 核心系统

### 1. 战斗状态机（Combat FSM）

**5 状态**（`CombatStateMachine` 驱动）:

| 状态 | 职责 | 触发条件 |
|------|------|----------|
| IdleState | 待机，监听输入 | 默认入口 |
| MoveState | 移动（朝向/速度） | 摇杆/方向键 |
| AttackState | 近战攻击（剑/弓切换） | 左键/攻击键 |
| DodgeState | 闪避（体力消耗） | 右键/闪避键 |
| CastBuffState | 施放增益技能 | Q 键/技能键 |

**配置**:
- `SkillData`（ScriptableObject）驱动技能配置
- `PlayerCombatBootstrap.Awake()` 顺序装配 FSM
- 支持 `基础移动` 让权 + `跳跃射箭` 禁用仲裁

### 2. 属性系统（ChaState + Attribute）

**统一伤害管线**:

```
TakeDamage(damage, owner)
  → 防御减免（defense.GetValue()）
  → 扣血（currentHealth -= actual）
  → onDamaged 事件（击退/受击动画/UI）
  → 死亡判定 → onDeath 事件（销毁/掉落）
```

**8 项基础属性**（全部可动态修改）:

| 属性 | 说明 | 默认值 |
|------|------|--------|
| MaxHealth | 生命上限 | 100 |
| HpRegen | 每秒回血 | 0 |
| Atk | 攻击力 | 10 |
| Defense | 防御（减伤） | 0 |
| MoveSpeed | 移动速度 | 5 |
| AttackRate | 攻速倍率 | 1 |
| PickupRange | 拾取范围 | 2 |
| Growth | 经验获取倍率 | 1 |

**修饰器管线（五段式）**:

```
flatBonus（固定加） → percentBonus（百分比加） → overrideValue（覆盖） → finalBonus（最终加） → clamp（边界）
```

### 3. Roguelike 跑局循环（v0.1.5）

**房间流**（`RogueRoomFlowController`）:

```
开局（StartRun）
  → 战斗房 × 3（清怪 → 掉碎片 → 拾取）
  → 祭坛房（三选一强化：Atk/MaxHealth/AttackRate/MoveSpeed/Growth）
  → 战斗房 × 3
  → 祭坛房
  → 战斗房 × 3
  → BOSS 房
  → 结算（碎片 × 系数 → 局外晶核，RunCurrencyStore 版本化持久化）
  → 死亡 → 失败结算（属性还原：RemoveModifiersFromSource）
```

**持久化**（`RunCurrencyStore`）:
- 版本号：v0.1.5
- 原子写入：`.tmp` 临时文件 → `File.Replace`
- 版本不符/损坏 → 回退 0 并告警

### 4. 怪物巡逻（NavMesh 2D）

**状态机**（`MonsterPatrolController`）:

```
Idle（无路径点） → Patrol（往返巡逻）
                  ↓ 检测到玩家（detectionRadius）
                Chase（追击）
                  ↓ 进入攻击范围（attackRange）
                Attack（触发攻击动画）
                  ↓ 丢失目标（loseTargetDistance）
                ResumePatrol（返回最近路径点）
```

**2D ↔ NavMesh XZ 坐标映射**: `PhysicsSystem2D` 自动转换。

---

## 开发工作流

### 版本驱动开发

当前里程碑: **v0.1.5**（极简 Rogue 验证）

```bash
# 查看版本计划
cat docs/版本计划.md

# 查看当前验收清单
cat docs/测试报告.md
```

### Spec 驱动开发（可选）

```bash
# 创建新功能 spec
mkdir -p .trae/specs/<feature-name>
# 编写 spec.md + tasks.md + checklist.md
```

### 编辑器工具

| 菜单 | 功能 | 状态 |
|------|------|------|
| `Tools → 战斗系统 → Compose Player Combat` | 一键装配玩家战斗管线 | ✅ 生产就绪 |
| `Tools → 怪物巡逻 → Create Patrol Demo Setup` | 生成怪物巡逻演示场景 | ✅ 生产就绪 |
| `Tools → TurnBased → Setup TurnBased Demo` | 生成回合制网格演示 | ✅ 生产就绪 |
| `Tools → UI Showcase → Setup UiShowcase` | 生成 UI 展示场景 | ✅ 生产就绪 |
| `Tools → 局会话 → 装配 Rogue Demo` | 装配 Roguelike 跑局循环 | ✅ v0.1.5 新增 |

### 调试技巧

- **FSM 不触发**: 检查 `CombatStateMachine.Configure()` 是否被调用（`[Diagnostics]` 应有装配日志）
- **Animator 参数缺失**: 使用 `SetBoolIfExists` / `SetTriggerIfExists` 守卫
- **怪物不巡逻**: 确认 `MonsterPatrolPath` 有路径点 + `NavMeshSurface` 已烘焙
- **状态卡死**: 检查 `CombatStateMachine.ChangeState` 日志（非法转换输出 `LogWarning`）

---

## 版本路线图

| 版本 | 主题 | 状态 | 预计周期 |
|------|------|------|----------|
| v0.1 | 核心框架（装配根/诊断/FSM 加固） | ✅ 已交付 | - |
| v0.1.5 | 极简 Rogue 验证（砍怪-爆装-变强） | ⏳ 开发完成待验证 | 2 周 |
| v0.2 | 输入系统迁移 + 跳跃纳入 FSM | 📋 计划中 | 2 周 |
| v0.3 | 垂直切片关卡（1-1~2-3 + 3 BOSS） | 📋 计划中 | 3 周 |
| v0.4 | 上线打磨（音效/特效/对象池/性能） | 📋 计划中 | 2 周 |

**当前分支**: `feature-bossfight`

---

## 文档导航

| 文档 | 用途 | 阅读对象 |
|------|------|----------|
| [docs/核心框架设计.md](docs/核心框架设计.md) | v0.1 架构决策 + 装配链 + 诊断契约 | 开发人员 |
| [docs/版本计划.md](docs/版本计划.md) | 里程碑规划 + 验收清单 | 项目经理 + 开发 |
| [docs/测试报告.md](docs/测试报告.md) | 编译/功能/兼容/性能/安全验证 | QA + 开发 |
| [docs/怪物巡逻系统使用与配置文档.md](docs/怪物巡逻系统使用与配置文档.md) | NavMesh 巡逻参数 + 场景配置 | 关卡策划 + 开发 |
| [docs/策划流程与双档方案.md](docs/策划流程与双档方案.md) | 策划方法论 + 双档方案（极简/标准版 Rogue） | 策划 + 主策 |
| [docs/UI作品集设计说明.md](docs/UI作品集设计说明.md) | 6 个界面设计决策 + 视觉体系 | UI/UX 设计师 |
| [docs/配置步骤.md](docs/配置步骤.md) | 场景布线清单（Rogue 房间 + 流程） | 关卡策划 |
| [AGENTS.md](AGENTS.md) | 项目能力盘点 + 可复用边界 + 已知问题 | 所有协作者 |
| [docs/战斗属性ECS框架.md](docs/战斗属性ECS框架.md) | Buff ECS 组件设计 | 系统程序员 |

---

## 贡献指南

### 分支策略

- `master` — 稳定分支，发布版本
- `feature-bossfight` — BOSS 战斗功能开发（当前分支）
- 功能分支从 `master` 切出，PR 合并回 `master`

### 提交规范

```bash
# 格式：<type>(<scope>): <subject>

feat(战斗系统): 新增跳跃纳入 FSM
fix(ui): 修复祭坛面板按钮引用丢失
docs: 更新版本计划 v0.2 验收清单
chore(依赖): 升级 Unity Input System 至 1.7.0
```

**Type**: `feat` / `fix` / `docs` / `chore` / `refactor` / `test` / `perf`

### 代码规范

- C# 命名：`PascalCase`（类/方法） / `camelCase`（字段/局部变量） / `_camelCase`（私有字段）
- 每个新脚本必须有 `.meta` 文件（Unity 要求）
- 修改旧代码前先读 `AGENTS.md` 确认可复用边界
- 任何架构/管理器变更前输出对抗式审查（四段式：事实/风险/结论/验证）

### 编译门禁

```powershell
# 提交前必须通过
dotnet build .\Assembly-CSharp.csproj --no-restore    # 0 错误 0 警告
dotnet build .\Assembly-CSharp.Player.csproj --no-restore  # 0 错误 0 警告
```

- 编辑器脚本变更时额外构建 `Assembly-CSharp-Editor.csproj`
- 退出码 1 可能是 sandbox 日志限制，以「0 警告 0 错误」判定

---

## 已知问题

| 问题 | 影响 | 计划修复版本 |
|------|------|-------------|
| `跳跃射箭` 在 FSM 模式下被禁用（双写 rb.linearVelocity 风险） | 跳跃/射箭不可用 | v0.2 |
| `CameraFollow2D` 被 Cinemachine 替代，残留调试日志 | 控制台刷屏 | v0.4 |
| NavMesh 需手动烘焙（无自动烘焙流程） | 新场景怪物不巡逻 | 待排期 |
| `EnemyBase.Update()` 每帧 `GetComponent<MonsterPatrolController>()` | 轻微 GC 压力 | 待优化 |

详见 [AGENTS.md](AGENTS.md) 已知问题章节。

---

## 许可

本项目为原型学习项目，未开源。

---

## 联系方式

- 项目负责人：bigbigworld
- 问题反馈：提交 Issue 或查看 [docs/版本计划.md](docs/版本计划.md)
