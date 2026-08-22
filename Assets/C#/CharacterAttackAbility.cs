using UnityEngine;

/// <summary>
/// 角色普攻能力：将动画表现和技能命中执行组合为一个可注册、可独立停用的能力。
/// </summary>
public sealed class CharacterAttackAbility : AbilityBase
{
    [SerializeField] private PlayerAnimationPresenter presenter;
    [SerializeField] private SkillExecutor skillExecutor;
    [SerializeField] private int comboIndex;

    protected override void OnInitialize()
    {
        presenter = presenter != null ? presenter : GetComponent<PlayerAnimationPresenter>();
        skillExecutor = skillExecutor != null ? skillExecutor : GetComponent<SkillExecutor>();
    }

    protected override void OnActivate()
    {
        if (presenter != null)
        {
            presenter.PlayAttack(comboIndex);
        }

        skillExecutor?.ExecuteAttack();
    }

    public void SetComboIndex(int index)
    {
        comboIndex = Mathf.Max(0, index);
    }
}
