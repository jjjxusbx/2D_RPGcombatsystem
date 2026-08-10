using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 怪物巡逻系统一键搭建工具。
/// 菜单路径：Tools/怪物巡逻/Create Patrol Demo Setup
/// 说明：
/// 1. 创建导航地面（XZ 平面薄板 + NavMeshSurface）并烘焙
/// 2. 创建巡逻路径容器（含 3 个路径点子对象）
/// 3. 为选中对象挂载 NavMeshAgent + MonsterPatrolController，并关联路径与玩家
/// </summary>
public static class MonsterPatrolSetup
{
    private const string MenuRoot = "Tools/怪物巡逻/";

    [MenuItem(MenuRoot + "Create Patrol Demo Setup", priority = 1)]
    public static void CreateDemoSetup()
    {
        GameObject navRoot = CreateNavMeshGround();
        PatrolPathComponent path = CreatePatrolPath();
        SetupSelectedMonsters(path);
        BakeNavMesh(navRoot);
        Selection.activeGameObject = navRoot;
        Debug.Log("[MonsterPatrol] 演示环境搭建完成：导航地面 + 巡逻路径 + 选中怪物已配置。");
    }

    [MenuItem(MenuRoot + "Bake NavMesh Only", priority = 2)]
    public static void BakeOnly()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError("[MonsterPatrol] 场景中未找到 NavMeshSurface，请先执行 Create Patrol Demo Setup。");
            return;
        }

        surface.BuildNavMesh();
        Debug.Log("[MonsterPatrol] NavMesh 已重新烘焙。");
    }

    private static GameObject CreateNavMeshGround()
    {
        GameObject existing = GameObject.Find("MonsterNav_Ground");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "MonsterNav_Ground";
        // 薄板：在 XZ 平面铺开，Y 仅作厚度
        ground.transform.position = new Vector3(0f, -0.25f, 0f);
        ground.transform.localScale = new Vector3(20f, 0.5f, 12f);

        Collider col = ground.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        ground.AddComponent<NavMeshSurface>();
        return ground;
    }

    private static MonsterPatrolPath CreatePatrolPath()
    {
        GameObject existing = GameObject.Find("MonsterPatrol_Path");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject root = new GameObject("MonsterPatrol_Path");
        MonsterPatrolPath path = root.AddComponent<MonsterPatrolPath>();

        for (int i = 0; i < 3; i++)
        {
            GameObject wp = new GameObject("Waypoint_" + i);
            wp.transform.SetParent(root.transform, false);
            wp.transform.position = new Vector3(i * 4f - 4f, 1f, 0f);
            path.AddPoint(wp.transform);
        }

        return path;
    }

    private static void SetupSelectedMonsters(MonsterPatrolPath path)
    {
        Transform player = null;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null)
            {
                continue;
            }

            NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = go.AddComponent<NavMeshAgent>();
            }

            agent.radius = 0.2f;
            agent.height = 0.2f;
            agent.baseOffset = 0f;
            agent.speed = 2f;

            MonsterPatrolController controller = go.GetComponent<MonsterPatrolController>();
            if (controller == null)
            {
                controller = go.AddComponent<MonsterPatrolController>();
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("patrolPath").objectReferenceValue = path;
            if (player != null)
            {
                so.FindProperty("target").objectReferenceValue = player;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[MonsterPatrol] 未选中任何怪物，请在 Hierarchy 中选中怪物后重新执行。");
        }
    }

    private static void BakeNavMesh(GameObject navRoot)
    {
        NavMeshSurface surface = navRoot.GetComponent<NavMeshSurface>();
        if (surface != null)
        {
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }
    }
}
