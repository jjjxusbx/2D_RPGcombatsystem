using System;
using System.Collections.Generic;
using UnityEngine;

public enum AINodeType
{
    Selector,
    Sequence,
    Function
}

[Serializable]
public class AINodeDefinition
{
    public string id;
    public AINodeType type;
    public string function;
    public List<AINodeDefinition> children = new List<AINodeDefinition>();
}

[Serializable]
public class AIBehaviorTreeDefinition
{
    public AINodeDefinition root;
}

[CreateAssetMenu(menuName = "战斗/AI行为树JSON")]
public class AIBehaviorTreeAsset : ScriptableObject
{
    public TextAsset json;

    public AIBehaviorTreeDefinition Load()
    {
        return json == null ? null : JsonUtility.FromJson<AIBehaviorTreeDefinition>(json.text);
    }
}

public static class AIFunctionLibrary
{
    private static readonly Dictionary<string, Func<GameObject, bool>> functions =
        new Dictionary<string, Func<GameObject, bool>>();

    public static void Register(string functionName, Func<GameObject, bool> function)
    {
        if (!string.IsNullOrEmpty(functionName) && function != null)
        {
            functions[functionName] = function;
        }
    }

    public static bool Execute(string functionName, GameObject owner)
    {
        Func<GameObject, bool> function;
        return functions.TryGetValue(functionName, out function) && function(owner);
    }
}

public class AIBehaviorTreeRunner : MonoBehaviour
{
    [SerializeField] private AIBehaviorTreeAsset treeAsset;
    private AIBehaviorTreeDefinition tree;

    private void Awake()
    {
        if (treeAsset != null)
        {
            tree = treeAsset.Load();
        }
    }

    public bool Tick()
    {
        return tree != null && tree.root != null && Evaluate(tree.root);
    }

    private bool Evaluate(AINodeDefinition node)
    {
        if (node.type == AINodeType.Function)
        {
            return AIFunctionLibrary.Execute(node.function, gameObject);
        }

        if (node.type == AINodeType.Sequence)
        {
            for (int i = 0; i < node.children.Count; i++)
            {
                if (!Evaluate(node.children[i]))
                {
                    return false;
                }
            }

            return true;
        }

        for (int i = 0; i < node.children.Count; i++)
        {
            if (Evaluate(node.children[i]))
            {
                return true;
            }
        }

        return false;
    }
}
