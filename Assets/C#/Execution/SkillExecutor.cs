using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置化技能执行器：按 SkillData 配置命中与施法。
/// 挂在玩家根对象上，由 CombatStateMachine 在 Start 时注入 CombatContext.SkillExecutor。
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    [Header("技能配置")]
    [Tooltip("主攻击技能(普攻),ExecuteAttack 读取其 ranges 与 magicEffect")]
    public SkillData mainSkill;

    [Tooltip("自身增益技能(如狂暴),CastSelfBuff 对施法者自身施放")]
    public SkillData selfBuffSkill;

    [Tooltip("命中层遮罩，装配时建议设为排除 Player 层。默认 ~0 全部层")]
    public LayerMask targetLayers = ~0;

    /// <summary>普攻：以自身为圆心按 mainSkill 配置的半径 OverlapCircleAll 命中，逐个施加 magicEffect。</summary>
    public void ExecuteAttack()
    {
        if (mainSkill == null)
        {
            Debug.LogWarning("[Skill] ExecuteAttack 失败:mainSkill 未配置。", this);
            return;
        }

        // 取第一个有效 RangeProperty，无则半径默认 1.5f
        float radius = 1.5f;
        if (mainSkill.ranges != null)
        {
            for (int i = 0; i < mainSkill.ranges.Count; i++)
            {
                if (mainSkill.ranges[i] != null)
                {
                    radius = mainSkill.ranges[i].radius;
                    break;
                }
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
        int count = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            // 跳过自身及子物体
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            // 跳过 Player 标签的对象（Player 为 Unity 内置标签，恒存在，CompareTag 安全）。
            // 注意：场景中玩家与敌人目前同在 Default 层（0），不能用 layer 判断排除玩家。
            if (hit.gameObject.CompareTag("Player")) continue;

            if (mainSkill.magicEffect != null)
            {
                mainSkill.magicEffect.Apply(gameObject, hit.gameObject);
                count++;
            }
        }

        Debug.Log($"[Skill] ExecuteAttack hits={count} radius={radius}", this);
    }

    /// <summary>对施法者自身施放增益技能（如狂暴）。</summary>
    public void CastSelfBuff()
    {
        if (selfBuffSkill == null)
        {
            Debug.LogWarning("[Skill] CastSelfBuff 失败:selfBuffSkill 未配置。", this);
            return;
        }

        if (selfBuffSkill.magicEffect == null)
        {
            Debug.LogWarning("[Skill] CastSelfBuff 失败:selfBuffSkill.magicEffect 为空。", this);
            return;
        }

        selfBuffSkill.magicEffect.Apply(gameObject, gameObject);
        Debug.Log($"[Skill] CastSelfBuff applied: {selfBuffSkill.name}", this);
    }
}