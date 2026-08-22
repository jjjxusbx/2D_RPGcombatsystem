using System;
using UnityEngine;

/// <summary>
/// 角色二维物理适配层：统一刚体模拟、速度/位置写入和碰撞事件入口。
/// 上层角色与能力不直接依赖 Rigidbody2D 的底层细节。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PhysicsSystem2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;

    public Rigidbody2D Body => body;
    public Collider2D BodyCollider => bodyCollider;
    public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

    public event Action<Collision2D> CollisionEntered;
    public event Action<Collision2D> CollisionStayed;
    public event Action<Collision2D> CollisionExited;
    public event Action<Collider2D> TriggerEntered;
    public event Action<Collider2D> TriggerExited;

    private void Awake()
    {
        BindReferences();
    }

    public void BindReferences()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (body != null)
        {
            body.linearVelocity = velocity;
        }
    }

    public void Stop()
    {
        SetVelocity(Vector2.zero);
    }

    public void Move(Vector2 velocity)
    {
        SetVelocity(velocity);
    }

    public void MovePosition(Vector2 position)
    {
        if (body != null)
        {
            body.MovePosition(position);
        }
        else
        {
            transform.position = position;
        }
    }

    public Collider2D[] OverlapCircle(Vector2 center, float radius, LayerMask layerMask)
    {
        return Physics2D.OverlapCircleAll(center, Mathf.Max(0f, radius), layerMask);
    }

    public bool Cast(Vector2 direction, float distance, LayerMask layerMask, out RaycastHit2D hit)
    {
        hit = default;
        if (bodyCollider == null)
        {
            return false;
        }

        hit = Physics2D.CircleCast(bodyCollider.bounds.center, bodyCollider.bounds.extents.x,
            direction.normalized, distance, layerMask);
        return hit.collider != null;
    }

    public void SetSimulationEnabled(bool enabled)
    {
        if (body != null)
        {
            body.simulated = enabled;
        }
    }

    public void SetCollisionEnabled(bool enabled)
    {
        if (bodyCollider != null)
        {
            bodyCollider.enabled = enabled;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisionEntered?.Invoke(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CollisionStayed?.Invoke(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        CollisionExited?.Invoke(collision);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TriggerEntered?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        TriggerExited?.Invoke(other);
    }

    

}
