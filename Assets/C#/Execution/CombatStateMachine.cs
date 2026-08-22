using UnityEngine;

[DefaultExecutionOrder(-900)]
public class CombatStateMachine : MonoBehaviour
{
    [SerializeField] private Character character;
    [SerializeField] private PlayerAnimationPresenter presenter;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CombatDecisionComponent decision;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;

    private CombatContext context;
    private ICombatState currentState;
    private bool configured;

    public ICombatState CurrentState => currentState;

    private void Awake()
    {
        EnsureContext();
        ApplyContextBindings();
    }

    private void EnsureContext()
    {
        if (context == null)
        {
            context = new CombatContext();
        }
    }

    /// <summary>
    /// 装配入口：由 PlayerCombatBootstrap 在 Awake 阶段调用，覆盖序列化引用。
    /// 未调用 Configure 时（手动挂载场景）Start 会退化为自绑定，保证两条路径行为一致。
    /// </summary>
    public void Configure(PlayerInputReader inputReader, CombatDecisionComponent decision,
        PlayerAnimationPresenter presenter, Rigidbody2D rb, SkillExecutor skillExecutor,
        float moveSpeed, Character character, PlayerConfig playerConfig)
    {
        EnsureContext();
        this.inputReader = inputReader;
        this.decision = decision;
        this.presenter = presenter;
        this.rb = rb;
        this.moveSpeed = moveSpeed;
        this.character = character;
        context.SkillExecutor = skillExecutor;
        context.AttackDuration = playerConfig != null ? playerConfig.attackDuration : 0.4f;
        context.InputBufferWindow = playerConfig != null ? playerConfig.attackComboWindow : 0.3f;
        context.MaxCombo = playerConfig != null ? Mathf.Max(1, playerConfig.maxCombo) : 3;
        context.DodgeDuration = playerConfig != null ? playerConfig.dodgeDuration : 0.25f;
        context.InvincibleDuration = playerConfig != null ? playerConfig.dodgeInvincibleTime : 0.2f;
        context.DodgeDistance = playerConfig != null ? playerConfig.dodgeDistance : 3f;
        configured = true;
        ApplyContextBindings();
    }

    private void Start()
    {
        if (!configured)
        {
            ApplyContextBindings();
        }

        ChangeState(new IdleState());
    }

    private void ApplyContextBindings()
    {
        EnsureContext();
        character = character != null ? character : GetComponent<Character>();
        presenter = presenter != null ? presenter : GetComponent<PlayerAnimationPresenter>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
        decision = decision != null ? decision : GetComponent<CombatDecisionComponent>();
        context.Character = character;
        context.Presenter = presenter;
        context.InputReader = inputReader;
        context.Decision = decision;
        context.Rigidbody = rb != null ? rb : GetComponent<Rigidbody2D>();
        context.CurrentSpeed = moveSpeed;
        context.StateMachine = this;
        context.SkillExecutor = context.SkillExecutor != null ? context.SkillExecutor : GetComponent<SkillExecutor>();
    }

    private void Update()
    {
        if (currentState == null || inputReader == null)
        {
            return;
        }

        // 单帧单次读取意图并缓存到上下文，避免状态内多次调用产生不一致输入
        PlayerIntent intent = character != null
            ? character.GetIntentForFrame()
            : inputReader.GetIntent();
        context.Intent = intent;
        context.MoveInput = intent.MoveDirection;
        if (intent.AimPosition != Vector2.zero)
        {
            context.AimDirection = ((Vector2)intent.AimPosition - (Vector2)transform.position).normalized;
        }

        currentState.Execute(context);
    }

    public void ChangeState(ICombatState newState)
    {
        if (newState == null)
        {
            Debug.LogError($"[FSM] {name} 拒绝切换到空状态。", this);
            return;
        }

        if (currentState != null && !currentState.CanTransitionTo(newState))
        {
            Debug.LogWarning($"[FSM] {name} 非法状态转换 {currentState.GetStateName()} -> {newState.GetStateName()} 被拒绝。", this);
            return;
        }

        currentState?.Exit(context);
        currentState = newState;
        currentState?.Enter(context);
    }

    public void RequestExitToIdle()
    {
        ChangeState(new IdleState());
    }
}