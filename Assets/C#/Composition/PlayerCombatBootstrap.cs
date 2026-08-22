using UnityEngine;

/// <summary>
/// 玩家战斗管线装配根（组合根，核心框架的单一装配入口）。
/// Awake 按 数据 → 决策 → 表现 → 执行 的固定顺序装配组件并仲裁旧输入脚本，
/// 消除"编辑器手点 + 运行时隐式添加"双路径；Start 输出 RuntimeDiagnostics 自检报告。
///
/// 仲裁规则（Fsm 模式）：
/// - 基础移动.useCombatStateMachine = true，让出移动/攻击控制权给 FSM，保留受击/死亡逻辑。
/// - 跳跃射箭 被禁用：跳跃/鼠标射箭尚未纳入 FSM，迁移到 FSM 前不并行运行（避免双写 rb.linearVelocity）。
/// </summary>
[DisallowMultipleComponent]
public class PlayerCombatBootstrap : MonoBehaviour
{
    public enum CombatMode
    {
        Fsm,
        Legacy
    }

    [Header("装配模式")]
    [Tooltip("Fsm：禁用旧移动/射击脚本，由 FSM 单一接管决策与执行；Legacy：保留旧脚本路径，仅输出诊断。")]
    public CombatMode combatMode = CombatMode.Fsm;

    [Header("数据")]
    [Tooltip("可选：为 FSM 提供移动速度等初始数值，未配置时使用默认值。")]
    public PlayerConfig playerConfig;

    [Header("执行")]
    [Tooltip("可选：自绑定失败时回退到 GetComponent。")]
    public SkillExecutor skillExecutor;

    [Header("表现")]
    [Tooltip("可选：自绑定失败时回退到 GetComponent。")]
    public PlayerAnimationPresenter presenter;

    private CombatStateMachine _stateMachine;
    private PlayerInputReader _inputReader;
    private CombatDecisionComponent _decision;
    private Character _character;
    private CharacterAttackAbility _attackAbility;
    private PlayerAttackTrigger[] _legacyAttacks;
    private 基础移动 _legacyMove;
    private 跳跃射箭 _legacyBow;

    private void Awake()
    {
        _character = GetComponent<Character>();
        _legacyMove = GetComponent<基础移动>();
        _legacyBow = GetComponent<跳跃射箭>();
        _legacyAttacks = GetComponentsInChildren<PlayerAttackTrigger>(true);

        // 数据层：统一伤害管线与 Buff 生命周期
        if (GetComponent<ChaState>() == null)
        {
            gameObject.AddComponent<ChaState>();
        }

        if (GetComponent<BuffController>() == null)
        {
            gameObject.AddComponent<BuffController>();
        }

        // 表现层
        presenter = presenter != null ? presenter : GetComponent<PlayerAnimationPresenter>();
        if (presenter == null)
        {
            presenter = gameObject.AddComponent<PlayerAnimationPresenter>();
        }

        // 决策层
        _inputReader = GetComponent<PlayerInputReader>();
        if (_inputReader == null)
        {
            _inputReader = gameObject.AddComponent<PlayerInputReader>();
        }

        _decision = GetComponent<CombatDecisionComponent>();
        if (_decision == null)
        {
            _decision = gameObject.AddComponent<CombatDecisionComponent>();
        }
        _character?.BindInputReader(_inputReader);

        // 执行层
        skillExecutor = skillExecutor != null ? skillExecutor : GetComponent<SkillExecutor>();
        if (skillExecutor == null)
        {
            skillExecutor = gameObject.AddComponent<SkillExecutor>();
        }

        _stateMachine = GetComponent<CombatStateMachine>();
        if (_stateMachine == null)
        {
            _stateMachine = gameObject.AddComponent<CombatStateMachine>();
        }
        _character?.BindStateMachine(_stateMachine);

        _attackAbility = GetComponent<CharacterAttackAbility>();
        if (_attackAbility == null)
        {
            _attackAbility = gameObject.AddComponent<CharacterAttackAbility>();
        }
        _character?.RegisterAbility(_attackAbility);

        _decision.Configure(playerConfig);
        _stateMachine.Configure(_inputReader, _decision, presenter, GetComponent<Rigidbody2D>(),
            skillExecutor, playerConfig != null ? playerConfig.moveSpeed : 5f, _character, playerConfig);

        if (combatMode == CombatMode.Fsm)
        {
            if (_legacyMove != null)
            {
                _legacyMove.useCombatStateMachine = true;
            }

            if (_legacyBow != null)
            {
                _legacyBow.enabled = false;
            }

            if (_legacyAttacks != null)
            {
                for (int i = 0; i < _legacyAttacks.Length; i++)
                {
                    if (_legacyAttacks[i] != null)
                    {
                        _legacyAttacks[i].enabled = false;
                    }
                }
            }
        }
        else
        {
            _stateMachine.enabled = false;
            _inputReader.enabled = false;
            _attackAbility.enabled = false;
        }
    }

    private void Start()
    {
        // 所有组件的 Awake 已完成（含自绑定），此时输出装配自检最可靠
        RuntimeDiagnostics diagnostics = GetComponent<RuntimeDiagnostics>();
        if (diagnostics == null)
        {
            diagnostics = gameObject.AddComponent<RuntimeDiagnostics>();
        }

        diagnostics.Run(this);
    }
}