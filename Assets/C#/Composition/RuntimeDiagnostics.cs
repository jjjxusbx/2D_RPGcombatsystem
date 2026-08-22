using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时装配自检：由 PlayerCombatBootstrap.Start 调用，输出可观察诊断报告。
/// 覆盖：伤害管线缺失、决策/执行/表现链路缺失、表现层引用缺失、
/// 双输入入口冲突、技能配置缺失。只读，不修改任何行为。
/// </summary>
[DisallowMultipleComponent]
public class RuntimeDiagnostics : MonoBehaviour
{
    private readonly List<string> _issues = new List<string>();
    private readonly List<string> _infos = new List<string>();

    public void Run(PlayerCombatBootstrap bootstrap)
    {
        _issues.Clear();
        _infos.Clear();

        CheckDamagePipeline();
        CheckPipeline();
        CheckLegacyConflict(bootstrap);
        CheckSkillConfig();

        Report();
    }

    private void CheckDamagePipeline()
    {
        if (GetComponent<ChaState>() == null)
        {
            _issues.Add("缺少 ChaState（统一伤害管线不可用）");
        }

        if (GetComponent<BuffController>() == null)
        {
            _issues.Add("缺少 BuffController（Buff 生命周期不可用）");
        }
    }

    private void CheckPipeline()
    {
        if (GetComponent<PlayerInputReader>() == null)
        {
            _issues.Add("缺少 PlayerInputReader（决策层缺失）");
        }

        if (GetComponent<CombatDecisionComponent>() == null)
        {
            _issues.Add("缺少 CombatDecisionComponent（体力/冷却决策缺失）");
        }

        if (GetComponent<CombatStateMachine>() == null)
        {
            _issues.Add("缺少 CombatStateMachine（执行层缺失）");
        }

        PlayerAnimationPresenter presenter = GetComponent<PlayerAnimationPresenter>();
        if (presenter == null)
        {
            _issues.Add("缺少 PlayerAnimationPresenter（表现层缺失）");
        }
        else
        {
            presenter.ValidateBindings(_issues);
        }
    }

    private void CheckLegacyConflict(PlayerCombatBootstrap bootstrap)
    {
        bool legacyMove = GetComponent<基础移动>() != null;
        bool legacyBow = GetComponent<跳跃射箭>() != null;
        bool fsmMode = bootstrap != null && bootstrap.combatMode == PlayerCombatBootstrap.CombatMode.Fsm;

        if (legacyMove && legacyBow)
        {
            _infos.Add("检测到 基础移动 与 跳跃射箭 并存（双写 rb.linearVelocity 风险）");
        }

        if (legacyMove && fsmMode)
        {
            _infos.Add("基础移动 已让出控制权（useCombatStateMachine=true），保留受击/死亡逻辑");
        }

        if (legacyBow && fsmMode)
        {
            _infos.Add("跳跃射箭 已被禁用：跳跃/射箭尚未纳入 FSM，将在后续版本迁移");
        }
    }

    private void CheckSkillConfig()
    {
        SkillExecutor executor = GetComponent<SkillExecutor>();
        if (executor == null)
        {
            _issues.Add("缺少 SkillExecutor（技能执行不可用）");
            return;
        }

        if (executor.mainSkill == null)
        {
            _issues.Add("SkillExecutor.mainSkill 未配置（普攻无伤害，仅播放动画）");
        }

        if (executor.selfBuffSkill == null)
        {
            _infos.Add("SkillExecutor.selfBuffSkill 未配置（Q 技能不可用）");
        }
    }

    private void Report()
    {
        if (_issues.Count > 0)
        {
            Debug.LogWarning($"[Diagnostics] {name} 装配检查发现 {_issues.Count} 个问题：\n- " + string.Join("\n- ", _issues), this);
        }

        if (_infos.Count > 0)
        {
            Debug.Log($"[Diagnostics] {name} 装配信息：\n- " + string.Join("\n- ", _infos), this);
        }

        if (_issues.Count == 0 && _infos.Count == 0)
        {
            Debug.Log($"[Diagnostics] {name} 装配检查通过。", this);
        }
    }
}