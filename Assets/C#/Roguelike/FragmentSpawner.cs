using UnityEngine;
using Roguelike.Run;

namespace Roguelike.Reward
{
    /// <summary>
    /// 敌人死亡时在死亡位置生成灰烬碎片。可绑定到敌人列表或某敌人根（自动取子级 ChaState）。
    /// 与 RogueRoomFlowController 各自订阅 onDeath，职责分离（本组件只管掉落，不碰击杀计数）。
    /// </summary>
    [DisallowMultipleComponent]
    public class FragmentSpawner : MonoBehaviour
    {
        [Header("掉落实体")]
        [Tooltip("碎片预制体；留空则运行时仅创建无精灵占位对象。")]
        [SerializeField] private GameObject fragmentPrefab;
        [Tooltip("掉落基础数量（可按敌人 Growth 折算）。")]
        [SerializeField] private int baseAmount = 1;
        [Tooltip("是否按击杀者来源忽略（恒为 false，占位）。")]
        [SerializeField] private bool useGrowthScale = false;

        [Header("敌人来源")]
        [Tooltip("若为空则从 enemiesRoot 子级收集 ChaState。")]
        [SerializeField] private ChaState[] enemies;
        [SerializeField] private Transform enemiesRoot;

        [Header("局会话引用")]
        [SerializeField] private RunManager runManager;

        public void BindEnemies(ChaState[] targets)
        {
            if (targets == null) return;
            enemies = targets;
            SubscribeAll();
        }

        private void Awake()
        {
            if (runManager == null) runManager = FindObjectOfType<RunManager>();
            CollectAndSubscribe();
        }

        private void CollectAndSubscribe()
        {
            ChaState[] targets = enemies;
            if (targets == null || targets.Length == 0)
            {
                if (enemiesRoot == null)
                {
                    Debug.LogWarning("[Diagnostics] FragmentSpawner 未指定敌人（enemies 或 enemiesRoot 为空）。", this);
                    return;
                }
                targets = enemiesRoot.GetComponentsInChildren<ChaState>();
            }

            enemies = targets;
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            foreach (ChaState enemy in enemies)
            {
                if (enemy == null) continue;
                ChaState captured = enemy;
                enemy.onDeath += info => SpawnAt(captured.transform.position, captured);
            }
        }

        private void SpawnAt(Vector3 position, ChaState source)
        {
            int amount = baseAmount;
            if (useGrowthScale && source != null)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * source.GetStat("Growth")));
            }

            GameObject go;
            if (fragmentPrefab != null)
            {
                go = Instantiate(fragmentPrefab, position, Quaternion.identity);
            }
            else
            {
                go = CreatePlaceholder(position);
            }

            FragmentDrop drop = go.GetComponent<FragmentDrop>();
            if (drop == null)
            {
                drop = go.AddComponent<FragmentDrop>();
            }
            drop.Amount = amount;

            if (runManager == null)
            {
                runManager = FindObjectOfType<RunManager>();
            }
            drop.SetRunManager(runManager);
        }

        private static GameObject CreatePlaceholder(Vector3 position)
        {
            // 无预制体时的占位掉落物：无精灵但可拾取；装配工具可再赋值精灵。
            GameObject go = new GameObject("FragmentDrop");
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.8f, 0.7f, 0.5f, 1f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            go.AddComponent<FragmentDrop>();
            return go;
        }
    }
}
