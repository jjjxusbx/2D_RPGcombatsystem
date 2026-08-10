using UnityEngine;

public class HitBoxController : MonoBehaviour
{
    [SerializeField] private Collider2D hitBox;
    [SerializeField] private float activeDuration = 0.2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float knockbackForce;
    [SerializeField] private GameObject hitEffectPrefab;

    private Coroutine activeRoutine;

    public void Activate(float damageAmount, float knockback, LayerMask layers, GameObject effectPrefab)
    {
        damage = damageAmount;
        knockbackForce = knockback;
        targetLayers = layers;
        hitEffectPrefab = effectPrefab;

        if (hitBox != null)
            hitBox.enabled = true;

        DetectHits();

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DeactivateRoutine());
    }

    public void Deactivate()
    {
        if (hitBox != null)
            hitBox.enabled = false;
    }

    private void DetectHits()
    {
        if (hitBox == null) return;

        Collider2D[] hits = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D
        {
            layerMask = targetLayers,
            useLayerMask = true
        };

        int count = hitBox.Overlap(filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null)
                ApplyDamage(hits[i].gameObject);
        }
    }

    private void ApplyDamage(GameObject target)
    {
        target.SendMessage("ReceiveDamage", damage, SendMessageOptions.DontRequireReceiver);

        if (knockbackForce > 0)
        {
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (target.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);
    }

    private System.Collections.IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(activeDuration);
        Deactivate();
        activeRoutine = null;
    }
}
