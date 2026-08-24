#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗系统一键搭建工具（配置化技能 + 统一属性 + Buff 生效的最低可用演示）。
/// 菜单：Tools/战斗系统/Create Example Combat Config、Tools/战斗系统/Setup Player Combat
/// </summary>
public static class CombatSystemSetup
{
    private const string MenuRoot = "Tools/战斗系统/";
    private const string ConfigDir = "Assets/战斗配置";

    [MenuItem(MenuRoot + "Create Example Combat Config")]
    public static void CreateExampleConfig()
    {
        // 建目录（存在则跳过）
        if (!AssetDatabase.IsValidFolder(ConfigDir))
        {
            AssetDatabase.CreateFolder("Assets", "战斗配置");
        }

        // 中毒 Buff：duration 5s，DOT 4/秒
        BuffData buffPoison = CreateOrReplaceAsset<BuffData>(ConfigDir + "/Buff_中毒.asset", "Buff_中毒");
        buffPoison.buffId = "poison_01";
        buffPoison.duration = 5f;
        buffPoison.components.Add(new BuffDotComponent { damagePerSecond = 4f });

        // 狂暴 Buff：duration 10s，Atk +50%
        BuffData buffRage = CreateOrReplaceAsset<BuffData>(ConfigDir + "/Buff_狂暴.asset", "Buff_狂暴");
        buffRage.buffId = "rage_01";
        buffRage.duration = 10f;
        buffRage.components.Add(new BuffStatComponent
        {
            modifiers = new List<AttributeModifier> { new AttributeModifier("Atk", ModifierType.PercentAdd, 0.5f) }
        });

        // 普攻 MagicEffect：伤害 × 1 + 施加中毒
        MagicEffectData meleeEffect = CreateOrReplaceAsset<MagicEffectData>(ConfigDir + "/MagicEffect_普攻.asset", "MagicEffect_普攻");
        meleeEffect.effectId = "melee_01";
        meleeEffect.properties.Add(new DamageMagicProperty { damageMultiplier = 1f });
        meleeEffect.properties.Add(new ApplyBuffMagicProperty { buff = buffPoison });

        // 狂暴 MagicEffect：施加狂暴 Buff
        MagicEffectData rageEffect = CreateOrReplaceAsset<MagicEffectData>(ConfigDir + "/MagicEffect_狂暴.asset", "MagicEffect_狂暴");
        rageEffect.effectId = "rage_01";
        rageEffect.properties.Add(new ApplyBuffMagicProperty { buff = buffRage });

        // 主攻击技能：普攻，半径 1.5
        SkillData mainSkill = CreateOrReplaceAsset<SkillData>(ConfigDir + "/Skill_普攻.asset", "Skill_普攻");
        mainSkill.skillId = "melee_01";
        mainSkill.effectKind = SkillEffectKind.MagicEffect;
        mainSkill.ranges.Add(new RangeProperty { radius = 1.5f, duration = 0.2f });
        mainSkill.magicEffect = meleeEffect;

        // 自身增益技能：狂暴
        SkillData selfBuffSkill = CreateOrReplaceAsset<SkillData>(ConfigDir + "/Skill_狂暴.asset", "Skill_狂暴");
        selfBuffSkill.skillId = "rage_01";
        selfBuffSkill.effectKind = SkillEffectKind.MagicEffect;
        selfBuffSkill.magicEffect = rageEffect;

        AssetDatabase.SaveAssets();
        Debug.Log("[战斗系统] 示例配置已生成到 Assets/战斗配置：Buff_中毒、Buff_狂暴、MagicEffect_普攻、MagicEffect_狂暴、Skill_普攻、Skill_狂暴。");
    }

    [MenuItem(MenuRoot + "Setup Player Combat")]
    public static void SetupPlayerCombat()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length != 1)
        {
            Debug.LogWarning("[战斗系统] 请恰好选中一个玩家根对象（Hero 根）后执行。");
            return;
        }

        GameObject playerRoot = Selection.activeGameObject;
        if (playerRoot == null)
        {
            Debug.LogWarning("[战斗系统] 选中对象为空。");
            return;
        }

        // 添加战斗组件（ChaState 由基础移动 Awake 运行时添加，这里不处理）
        PlayerInputReader inputReader = AddComponentIfMissing<PlayerInputReader>(playerRoot);
        CombatDecisionComponent decision = AddComponentIfMissing<CombatDecisionComponent>(playerRoot);
        PlayerAnimationPresenter presenter = AddComponentIfMissing<PlayerAnimationPresenter>(playerRoot);
        CombatStateMachine stateMachine = AddComponentIfMissing<CombatStateMachine>(playerRoot);
        SkillExecutor skillExecutor = AddComponentIfMissing<SkillExecutor>(playerRoot);
        AddComponentIfMissing<BuffController>(playerRoot);

        // 回填 CombatStateMachine 的序列化字段
        SerializedObject soMachine = new SerializedObject(stateMachine);
        soMachine.FindProperty("presenter").objectReferenceValue = presenter;
        soMachine.FindProperty("inputReader").objectReferenceValue = inputReader;
        soMachine.FindProperty("decision").objectReferenceValue = decision;
        soMachine.FindProperty("rb").objectReferenceValue = playerRoot.GetComponent<Rigidbody2D>();
        soMachine.ApplyModifiedPropertiesWithoutUndo();

        // 回填 SkillExecutor 的序列化字段（从配置目录读取）
        SkillData mainSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ConfigDir + "/Skill_普攻.asset");
        SkillData selfBuffSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ConfigDir + "/Skill_狂暴.asset");
        SerializedObject soExecutor = new SerializedObject(skillExecutor);
        soExecutor.FindProperty("mainSkill").objectReferenceValue = mainSkill;
        soExecutor.FindProperty("selfBuffSkill").objectReferenceValue = selfBuffSkill;
        soExecutor.ApplyModifiedPropertiesWithoutUndo();

        // 关闭旧式攻击触发器，避免与 FSM 攻击重复触发
        PlayerAttackTrigger trigger = playerRoot.GetComponent<PlayerAttackTrigger>();
        if (trigger != null)
        {
            trigger.enabled = false;
        }

        // 基础移动让出移动/攻击控制权给 FSM
        基础移动 baseMove = playerRoot.GetComponent<基础移动>();
        if (baseMove != null)
        {
            SerializedObject soMove = new SerializedObject(baseMove);
            soMove.FindProperty("useCombatStateMachine").boolValue = true;
            soMove.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(playerRoot);
        Debug.Log($"[战斗系统] 玩家 {playerRoot.name} 战斗组件装配完成。请确认 CombatStateMachine/SkillExecutor 引用与技能配置，然后进入 Play Mode 验证。");
    }

    private static T AddComponentIfMissing<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
        }

        return comp;
    }

    /// <summary>已存在同路径资产则先删除再创建，避免重复与残留引用。</summary>
    private static T CreateOrReplaceAsset<T>(string path, string assetName) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        T asset = ScriptableObject.CreateInstance<T>();
        asset.name = assetName;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
#endif
