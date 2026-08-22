using UnityEngine;

public class SlimeATKTri : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    public Transform owner;

    private void Start()
    {
        Destroy(gameObject, 0.3f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        基础移动 player = collision.GetComponent<基础移动>() ?? collision.GetComponentInParent<基础移动>();
        if (player == null)
        {
            Debug.LogWarning($"[Combat] Slime attack hit Player tag but no 基础移动 component: {collision.name}", collision);
            return;
        }

        player.TakeDamage(damage, owner != null ? owner : transform);
    }

    public void SetDamage(int Num, Transform Owner)
    {
        damage = Num;
        owner = Owner;
    }
}
