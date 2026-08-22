using UnityEngine;

public enum AbilityLifecycleState
{
    Uninitialized,
    Inactive,
    Active,
    Disposed
}

/// <summary>
/// 可组合能力的生命周期基类。
/// 子类只实现生命周期钩子，不自行管理注册和销毁时序。
/// </summary>
public abstract class AbilityBase : MonoBehaviour
{
    public Character Owner { get; private set; }
    public AbilityLifecycleState LifecycleState { get; private set; } = AbilityLifecycleState.Uninitialized;
    public bool IsInitialized => LifecycleState != AbilityLifecycleState.Uninitialized;
    public bool IsActive => LifecycleState == AbilityLifecycleState.Active;
    public bool IsDisposed => LifecycleState == AbilityLifecycleState.Disposed;

    public void Initialize(Character owner)
    {
        if (IsDisposed || IsInitialized)
        {
            return;
        }

        Owner = owner;
        LifecycleState = AbilityLifecycleState.Inactive;
        OnInitialize();
    }

    public bool Activate()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (!IsInitialized)
        {
            Initialize(GetComponentInParent<Character>());
        }

        if (IsActive)
        {
            return false;
        }

        LifecycleState = AbilityLifecycleState.Active;
        OnActivate();
        return true;
    }

    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        OnDeactivate();
        LifecycleState = AbilityLifecycleState.Inactive;
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (IsActive)
        {
            OnTick(deltaTime);
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (IsActive)
        {
            Deactivate();
        }

        OnDispose();
        LifecycleState = AbilityLifecycleState.Disposed;
        Owner = null;
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnActivate() { }
    protected virtual void OnDeactivate() { }
    protected virtual void OnTick(float deltaTime) { }
    protected virtual void OnDispose() { }

    private void OnDisable()
    {
        if (IsActive)
        {
            Deactivate();
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
