using UnityEngine;
using System.Collections.Generic;

public class CombatStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerAnimationPresenter presenter;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CombatDecisionComponent decision;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;

    private CombatContext context;
    private ICombatState currentState;

    public ICombatState CurrentState => currentState;

    private void Awake()
    {
        context = new CombatContext
        {
            Rigidbody = rb,
            CurrentSpeed = moveSpeed,
            StateMachine = this
        };
    }

    private void Start()
    {
        context.Presenter = presenter;
        context.InputReader = inputReader;
        context.Decision = decision;
        context.Rigidbody = rb;

        ChangeState(new IdleState());
    }

    private void Update()
    {
        if (currentState != null)
        {
            context.MoveInput = inputReader.GetIntent().MoveDirection;
            Vector2 aimPos = inputReader.GetIntent().AimPosition;
            if (aimPos != Vector2.zero)
                context.AimDirection = (aimPos - (Vector2)transform.position).normalized;
            currentState.Execute(context);
        }
    }

    public void ChangeState(ICombatState newState)
    {
        if (currentState != null && !currentState.CanTransitionTo(newState))
            return;

        currentState?.Exit(context);
        currentState = newState;
        currentState?.Enter(context);
    }

    public void RequestExitToIdle()
    {
        ChangeState(new IdleState());
    }
}
