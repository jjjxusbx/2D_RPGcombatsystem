using UnityEditor;
using UnityEngine;

namespace TurnBased.EditorTools
{
    /// <summary>
    /// 回合制域编辑工具：一键创建可运行的 autoDemo 演示对象（组合根 + 自检）。
    /// 幂等设计：已存在时复用根对象。
    /// </summary>
    public static class TurnBasedSetup
    {
        private const string MenuRoot = "Tools/回合制战术/";

        [MenuItem(MenuRoot + "Compose TurnBased Demo（组合根装配）")]
        public static void ComposeDemo()
        {
            GameObject root = GameObject.Find("TurnBasedDemo");
            if (root == null)
            {
                root = new GameObject("TurnBasedDemo");
            }

            var bootstrap = root.GetComponent<TurnBasedBootstrap>();
            if (bootstrap == null) bootstrap = root.AddComponent<TurnBasedBootstrap>();
            bootstrap.autoDemo = true;

            if (root.GetComponent<TurnBasedDiagnostics>() == null)
            {
                root.AddComponent<TurnBasedDiagnostics>();
            }

            Undo.RegisterCreatedObjectUndo(root, "Compose TurnBased Demo");
            Selection.activeGameObject = root;

            Debug.Log("[回合制装配] TurnBasedDemo 组合根已就绪（autoDemo 已勾选）。" +
                      "进入 Play Mode 后查看 [TurnBased.Diagnostics] 自检与 [Turn] 回合日志。", root);
        }
    }
}