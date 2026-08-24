using UnityEngine;
using Roguelike.Run;

namespace Roguelike.Reward
{
    /// <summary>
    /// 灰烬碎片掉落物：玩家触碰拾取，计入局内碎片（RunManager.AddFragments）。
    /// 掉落即结算（无背包）。amount 由 Spawner 在生成时按敌人 Growth/固定值设定。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FragmentDrop : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private int amount = 1;
        [SerializeField] private SpriteRenderer sr;

        public int Amount
        {
            get => amount;
            set => amount = Mathf.Max(1, value);
        }

        /// <summary>由 Spawner 注入局会话引用（运行时动态生成对象时调用）。</summary>
        public void SetRunManager(RunManager manager)
        {
            runManager = manager;
        }

        private void Awake()
        {
            if (runManager == null) runManager = FindObjectOfType<RunManager>();
            sr ??= GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (runManager != null)
            {
                runManager.AddFragments(amount);
                Debug.Log($"[Diagnostics] 拾取灰烬碎片 +{amount}。", this);
            }
            else
            {
                Debug.LogWarning("[Diagnostics] 拾取碎片的 RunManager 不可用，碎片未计入。", this);
            }

            Destroy(gameObject);
        }
    }
}
