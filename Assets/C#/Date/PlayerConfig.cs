using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Combat")]
    public float attackComboWindow = 0.3f;
    public float dodgeInvincibleTime = 0.2f;
    public float dodgeDistance = 3f;
    public float dodgeCooldown = 0.5f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 10f;
    public float dodgeStaminaCost = 25f;
    public float attackStaminaCost = 10f;
}
