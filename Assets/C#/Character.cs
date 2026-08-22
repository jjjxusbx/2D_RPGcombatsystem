using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有可控制角色的核心入口。
/// 负责输入帧缓存、角色控制、动画状态机引用和能力生命周期，不承载具体技能规则。
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsSystem2D))]
public class Character : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private PhysicsSystem2D physicsSystem;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CombatStateMachine stateMachine;
    [SerializeField] private AbilityBase[] startingAbilities;

    private readonly List<AbilityBase> abilities = new List<AbilityBase>();
    private PlayerIntent currentIntent;
    private int intentFrame = -1;
    private bool initialized;

    public PhysicsSystem2D Physics => physicsSystem;
    public Animator CharacterAnimator => characterAnimator;
    public PlayerInputReader InputReader => inputReader;
    public CombatStateMachine StateMachine => stateMachine;
    public PlayerIntent CurrentIntent => currentIntent;
    public IReadOnlyList<AbilityBase> Abilities => abilities;

    protected virtual void Awake()
    {
        InitializeCharacter();
    }

    protected virtual void OnCharacterUpdate() { }
    protected virtual void OnCharacterFixedUpdate() { }

    private void Update()
    {
        if (!initialized)
        {
            InitializeCharacter();
        }

        CacheInputForCurrentFrame();
        TickAbilities(Time.deltaTime);
        OnCharacterUpdate();
    }

    private void FixedUpdate()
    {
        OnCharacterFixedUpdate();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null && !abilities[i].IsInitialized)
            {
                abilities[i].Initialize(this);
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i]?.Deactivate();
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i]?.Dispose();
        }
    }

    public void InitializeCharacter()
    {
        if (initialized)
        {
            return;
        }

        physicsSystem = physicsSystem != null ? physicsSystem : GetComponent<PhysicsSystem2D>();
        if (physicsSystem == null)
        {
            physicsSystem = gameObject.AddComponent<PhysicsSystem2D>();
        }

        characterAnimator = characterAnimator != null ? characterAnimator : GetComponent<Animator>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
        stateMachine = stateMachine != null ? stateMachine : GetComponent<CombatStateMachine>();

        AbilityBase[] discovered = GetComponentsInChildren<AbilityBase>(true);
        for (int i = 0; i < discovered.Length; i++)
        {
            RegisterAbility(discovered[i]);
        }

        if (startingAbilities != null)
        {
            for (int i = 0; i < startingAbilities.Length; i++)
            {
                RegisterAbility(startingAbilities[i]);
            }
        }

        initialized = true;
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i].Initialize(this);
        }
    }

    public void BindInputReader(PlayerInputReader reader)
    {
        inputReader = reader;
    }

    public void BindStateMachine(CombatStateMachine machine)
    {
        stateMachine = machine;
    }

    public void RegisterAbility(AbilityBase ability)
    {
        if (ability == null || abilities.Contains(ability))
        {
            return;
        }

        abilities.Add(ability);
        if (initialized)
        {
            ability.Initialize(this);
        }
    }

    public bool UnregisterAbility(AbilityBase ability, bool dispose = true)
    {
        if (ability == null || !abilities.Remove(ability))
        {
            return false;
        }

        if (dispose)
        {
            ability.Dispose();
        }

        return true;
    }

    public T GetAbility<T>() where T : AbilityBase
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] is T ability)
            {
                return ability;
            }
        }

        return null;
    }

    public bool TryActivateAbility<T>() where T : AbilityBase
    {
        T ability = GetAbility<T>();
        return ability != null && ability.Activate();
    }

    public bool TryExecuteAbility<T>() where T : AbilityBase
    {
        T ability = GetAbility<T>();
        if (ability == null || !ability.Activate())
        {
            return false;
        }

        ability.Deactivate();
        return true;
    }

    public PlayerIntent GetIntentForFrame()
    {
        CacheInputForCurrentFrame();
        return currentIntent;
    }

    public void Move(Vector2 velocity)
    {
        physicsSystem?.Move(velocity);
    }

    public void StopMoving()
    {
        physicsSystem?.Stop();
    }

    public void SetAnimationBool(string parameterName, bool value)
    {
        PlayerAnimationPresenter.SetBoolIfExists(characterAnimator, parameterName, value);
    }

    public void TriggerAnimation(string parameterName)
    {
        PlayerAnimationPresenter.SetTriggerIfExists(characterAnimator, parameterName);
    }

    private void CacheInputForCurrentFrame()
    {
        if (intentFrame == Time.frameCount)
        {
            return;
        }

        intentFrame = Time.frameCount;
        currentIntent = inputReader != null ? inputReader.GetIntent() : default;
    }

    private void TickAbilities(float deltaTime)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i]?.Tick(deltaTime);
        }
    }
}
