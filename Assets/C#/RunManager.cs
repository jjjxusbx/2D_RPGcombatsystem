using UnityEngine;

namespace Roguelike.Run
{
    /// <summary>
    /// 局会话容器：一局"进房→清怪→祭坛→BOSS→结算"的状态与强化源管理。
    /// 为什么强化源用实例对象而不是字符串：ChaState.Attribute.RemoveAllFromSource 用
    /// ReferenceEquals 定向移除，复用同一实例引用才能保证 EndRun 精确回收本局强化。
    /// </summary>
    [DisallowMultipleComponent]
    public class RunManager : MonoBehaviour
    {
        /// <summary>一局会话运行期快照，不落盘。</summary>
        [System.Serializable]
        public struct RunSession
        {
            public int fragments;
            public int kills;
            public int roomIndex;
        }

        [Header("局外货币（永久晶核）结算")]
        [Tooltip("通关：碎片 x 系数 = 晶核")]
        [SerializeField] private float winConversion = 1f;
        [Tooltip("失败：碎片 x 系数 = 晶核")]
        [SerializeField] private float loseConversion = 0.5f;

        private RunSession _session;
        private ChaState _playerState;

        /// <summary>本局强化源标识（同一实例引用，供定向移除）。</summary>
        private readonly object _sessionSource = new object();

        public RunSession Session => _session;

        private void Awake()
        {
            RunDiag.SelfCheck(this, "RunManager");
            _playerState = FindPlayerState();
        }

        /// <summary>启动新一局：清掉上一局残留强化，重置会话计数。</summary>
        public void StartRun()
        {
            ChaState player = GetPlayerState();
            player?.RemoveModifiersFromSource(_sessionSource);

            _session = new RunSession { fragments = 0, kills = 0, roomIndex = 0 };
            RunDiag.Log("[Diagnostics] 局会话已启动，上一局强化已清理。");
        }

        /// <summary>结束一局：碎片结算为局外货币，并定向移除本局全部强化。</summary>
        public void EndRun(bool isWin)
        {
            float rate = isWin ? winConversion : loseConversion;
            int reward = Mathf.RoundToInt(_session.fragments * rate);

            int current = 0;
            RunCurrencyStore.LoadCurrency(out current);
            RunCurrencyStore.SaveCurrency(current + reward);

            ChaState player = GetPlayerState();
            player?.RemoveModifiersFromSource(_sessionSource);

            RunDiag.Log($"[Diagnostics] 局会话结束:isWin={isWin} 结算晶核 +{reward}（累计 {current + reward}），强化已清理。");
        }

        /// <summary>碎片收入统一点。</summary>
        public void AddFragments(int amount)
        {
            if (amount <= 0) return;
            _session.fragments += amount;
            RunDiag.Log($"[Diagnostics] 碎片 +{amount}，当前 {_session.fragments}");
        }

        /// <summary>击杀计数（RoomFlowController 每击杀一个敌人调用一次）。</summary>
        public void NotifyKill()
        {
            _session.kills++;
        }

        /// <summary>房间推进计数（每次房间清理完毕调用一次）。</summary>
        public void NotifyRoomCleared()
        {
            _session.roomIndex++;
        }

        /// <summary>
        /// 祭坛强化：施加到玩家 ChaState 并登记来源。
        /// source 被 [NonSerialized] 忽略，必须在此运行时赋值。
        /// +MaxHealth 只加上限玩家会感觉"没生效"，顺带补同等血量（仍直连 ChaState）。
        /// </summary>
        public void ApplyRoomBuff(AttributeModifier mod)
        {
            ChaState player = GetPlayerState();
            if (player == null || mod == null)
            {
                RunDiag.Warn("[Diagnostics] 祭坛强化失败：玩家 ChaState 或修饰器不可用。");
                return;
            }

            mod.source = _sessionSource;
            player.ApplyModifier(mod);

            if (mod.statId == "MaxHealth" && mod.type == ModifierType.AddValue && mod.value > 0f)
            {
                player.Heal(mod.value);
            }

            RunDiag.Log($"[Diagnostics] 已施加强化 {mod.statId} (+{mod.value})");
        }

        private ChaState GetPlayerState()
        {
            if (_playerState == null)
            {
                _playerState = FindPlayerState();
            }
            return _playerState;
        }

        private static ChaState FindPlayerState()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            ChaState state = player != null ? player.GetComponentInChildren<ChaState>() : null;
            if (state == null)
            {
                RunDiag.Warn("[Diagnostics] 未找到玩家 ChaState(检查 Player 标签与组件）。");
            }
            return state;
        }
    }

    /// <summary>
    /// 运行时自检输出。当前 RuntimeDiagnostics 只有 Run(PlayerCombatBootstrap) 入口、
    /// 没有 RecordComponent 方法（已核对源码），此处用等价 [Diagnostics] 日志满足可观察约束。
    /// </summary>
    internal static class RunDiag
    {
        public static void SelfCheck(MonoBehaviour owner, string label)
        {
            Debug.Log($"[Diagnostics] {label} 已就绪（挂载于 {owner.name}）。", owner);
        }

        public static void Log(string message) => Debug.Log(message);

        public static void Warn(string message) => Debug.LogWarning(message);
    }
}