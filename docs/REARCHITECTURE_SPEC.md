# 3D战斗系统四层架构重构 - 技术规格说明

## 一、架构概述

本重构按照AGENTS.md的四层架构原则，将现有混合在一起的代码分离为：

- **数据层 (Data)**：配置数据、SO定义
- **决策层 (Decision)**：输入解析、行为判断
- **执行层 (Execution)**：FSM状态机、移动控制、碰撞检测
- **表现层 (Presentation)**：动画、特效、音效、翻转

核心原则：表现层只负责"播放"，执行层只负责"逻辑"，数据层只负责"读取"。

---

## 二、现有问题

| 文件 | 当前职责 | 问题 |
|------|----------|------|
| PlayerCombatAnimation.cs | 表现+执行+决策混合 | 动画/瞄准/输入/触发全在一起 |
| 基础移动.cs | 决策+执行混合 | 输入解析和移动控制耦合 |

---

## 三、目标架构

`
Assets/C#/
├── Data/
│   └── PlayerConfig.cs              # 玩家配置SO
├── Decision/
│   ├── IPlayerIntent.cs             # 意图定义
│   ├── PlayerInputReader.cs         # 输入解析
│   └── CombatDecisionComponent.cs   # 战斗判断（体力/冷却）
├── Execution/
│   ├── CombatStateMachine.cs        # FSM调度器
│   ├── HitDetection/
│   │   └── HitBoxController.cs      # 碰撞检测
│   └── PlayerCombatContext.cs
├── Presentation/
│   └── PlayerAnimationPresenter.cs  # 纯表现：动画/特效/翻转
└── CombatSystem/
    ├── ICombatState.cs              # 状态接口
    ├── CombatContext.cs             # 上下文容器
    └── State/
        ├── IdleState.cs
        ├── MoveState.cs
        ├── AttackState.cs
        └── DodgeState.cs
`

---

## 四、接口规格

### 4.1 IPlayerIntent

`csharp
public enum PlayerIntentType { None, Move, Attack, Dodge, Aim }

public struct PlayerIntent
{
    public PlayerIntentType Type;
    public Vector2 MoveDirection;
    public Vector2 AimPosition;
    public bool IsSprint;
    public bool AttackPressed;
    public bool DodgePressed;
}
`

### 4.2 ICombatState

`csharp
public interface ICombatState
{
    void Enter(CombatContext context);
    void Execute(CombatContext context);
    void Exit(CombatContext context);
    bool CanTransitionTo(ICombatState newState);
    string GetStateName();
}
`

### 4.3 CombatContext

`csharp
public class CombatContext
{
    public Rigidbody2D Rigidbody;
    public Vector2 MoveInput;
    public Vector2 AimDirection;
    public bool IsAttacking;
    public bool IsInvincible;
    public float CurrentSpeed;
    public int ComboIndex;
    public float StateTimer;
    
    public PlayerAnimationPresenter Presenter;
    public PlayerInputReader InputReader;
    public CombatDecisionComponent Decision;
    public CombatStateMachine StateMachine;
}
`

---

## 五、各层职责

### 数据层 (PlayerConfig)
- 定义ScriptableObject
- 存储静态配置：速度/体力/冷却时间

### 决策层
- **PlayerInputReader**：将Unity Input转换为PlayerIntent
- **CombatDecisionComponent**：判断CanAttack/CanDodge/CanCombo，管理体力

### 执行层
- **CombatStateMachine**：状态切换调度，维护CurrentState
- **HitBoxController**：碰撞盒激活、伤害结算、击退
- **各State类**：Enter/Execute/Exit三阶段生命周期

### 表现层 (PlayerAnimationPresenter)
- SetMove(bool)：设置IsRun
- PlayAttack(int)：触发攻击动画
- PlayDodge()：触发闪避动画
- UpdateAimDirection(Vector2)：翻转渲染器

---

## 六、状态转换规则

| 当前状态 | 意图 | 目标状态 | 条件 |
|----------|------|----------|------|
| Idle | Move | Move | 输入非空 |
| Idle | Attack | Attack | 体力足够 |
| Idle | Dodge | Dodge | 体力+冷却 |
| Move | 输入空 | Idle | 输入为空 |
| Move | Attack | Attack | 体力足够 |
| Move | Dodge | Dodge | 体力+冷却 |
| Attack | Time>Duration | Idle | 动画结束 |
| Attack | Dodge | Dodge | 可取消 |
| Dodge | Time>Duration | Idle | 闪避结束 |
| Dodge | Attack | Attack | 可取消 |

---

## 七、测试计划

### 单元测试
1. PlayerInputReader.GetIntent() - 验证各输入映射
2. CombatDecisionComponent - 体力消耗/恢复逻辑
3. 各State.CanTransitionTo() - 状态转换合法性

### 集成测试
1. 完整输入流程：MoveInput → Intent → StateMachine → Presenter
2. 攻击连击：ComboIndex递增
3. 闪避无敌帧：IsInvincible状态正确

---

## 八、待实现清单

- [x] PlayerConfig (Data层)
- [x] IPlayerIntent / PlayerInputReader (决策层)
- [x] CombatDecisionComponent (决策层)
- [x] ICombatState / CombatContext
- [x] CombatStateMachine (执行层)
- [x] HitBoxController (执行层)
- [x] IdleState / MoveState / AttackState / DodgeState
- [x] PlayerAnimationPresenter (表现层)
- [ ] 测试用例编写
- [ ] 场景集成验证
