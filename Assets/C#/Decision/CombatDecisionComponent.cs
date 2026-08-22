using UnityEngine;

public class CombatDecisionComponent : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaRegen = 10f;
    [SerializeField] private float dodgeStaminaCost = 25f;
    [SerializeField] private float attackStaminaCost = 10f;
    [SerializeField] private float dodgeCooldown = 0.5f;

    private float lastDodgeTime;
    private float lastAttackTime;
    [SerializeField] private float attackComboWindow = 0.3f;

    public void Configure(PlayerConfig config)
    {
        if (config == null)
        {
            return;
        }

        maxStamina = config.maxStamina;
        staminaRegen = config.staminaRegen;
        dodgeStaminaCost = config.dodgeStaminaCost;
        attackStaminaCost = config.attackStaminaCost;
        dodgeCooldown = config.dodgeCooldown;
        attackComboWindow = config.attackComboWindow;
        currentStamina = maxStamina;
    }

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    public bool CanAttack()
    {
        return currentStamina >= attackStaminaCost;
    }

    public bool CanDodge()
    {
        return currentStamina >= dodgeStaminaCost && Time.time >= lastDodgeTime + dodgeCooldown;
    }

    public bool CanCombo()
    {
        return Time.time - lastAttackTime <= attackComboWindow;
    }

    public void ConsumeAttackStamina()
    {
        currentStamina -= attackStaminaCost;
        lastAttackTime = Time.time;
    }

    public void ConsumeDodgeStamina()
    {
        currentStamina -= dodgeStaminaCost;
        lastDodgeTime = Time.time;
    }
}
