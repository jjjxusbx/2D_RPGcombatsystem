#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 核心框架装配工具：以 PlayerCombatBootstrap 为组合根完成玩家战斗管线装配。
/// 幂等设计：重复执行不会产生重复组件或资产（示例技能资产为 CreateOrReplace 语义，
/// 若已有其他对象引用旧资产实例，请重新回填引用）。
/// </summary>
public static class CompositionSetup
{
    private const string MenuRoot = "Tools/战斗系统/";
    private const string ConfigDir = "Assets/战斗配置";

    [MenuItem(MenuRoot + "Compose Player Combat（核心框架装配）")]
    public static void ComposePlayerCombat()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length != 1)
        {
            Debug.LogWarning("[装配] 请恰好选中一个玩家根对象（Hero 根）后执行。");
            return;
        }

        GameObject playerRoot = Selection.activeGameObject;
        if (playerRoot == null)
        {
            return;
        }

        CombatSystemSetup.CreateExampleConfig();

        PlayerCombatBootstrap bootstrap = playerRoot.GetComponent<PlayerCombatBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = playerRoot.AddComponent<PlayerCombatBootstrap>();
        }

        PlayerConfig config = LoadOrCreateAsset<PlayerConfig>(ConfigDir + "/PlayerConfig.asset", "PlayerConfig");
        SerializedObject soBootstrap = new SerializedObject(bootstrap);
        soBootstrap.FindProperty("playerConfig").objectReferenceValue = config;

        SkillExecutor executor = playerRoot.GetComponent<SkillExecutor>();
        if (executor == null)
        {
            executor = playerRoot.AddComponent<SkillExecutor>();
        }

        SkillData mainSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ConfigDir + "/Skill_普攻.asset");
        SkillData selfBuffSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ConfigDir + "/Skill_狂暴.asset");
        SerializedObject soExecutor = new SerializedObject(executor);
        soExecutor.FindProperty("mainSkill").objectReferenceValue = mainSkill;
        soExecutor.FindProperty("selfBuffSkill").objectReferenceValue = selfBuffSkill;
        soExecutor.ApplyModifiedPropertiesWithoutUndo();

        soBootstrap.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(playerRoot);

        Debug.Log($"[装配] {playerRoot.name} 核心框架装配完成：bootstrap + PlayerConfig + 技能引用。" +
                  "进入 Play Mode 后查看 [Diagnostics] 自检日志，确认无问题项。", playerRoot);
    }

    private static T LoadOrCreateAsset<T>(string path, string assetName) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        if (!AssetDatabase.IsValidFolder(ConfigDir))
        {
            AssetDatabase.CreateFolder("Assets", "战斗配置");
        }

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = assetName;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
#endif