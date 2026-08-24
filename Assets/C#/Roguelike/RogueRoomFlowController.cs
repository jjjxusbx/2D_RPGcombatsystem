using System.Collections.Generic;
using UnityEngine;
using Roguelike.Run;

namespace Roguelike.Flow
{
    /// <summary>局会话内的房间类型（元循环，不参与战斗 FSM）。</summary>
    public enum RoomType
    {
        Combat,    // 战斗房：清怪拿碎片
        Altar,     // 祭坛房：三选一强化
        Boss,      // BOSS 房
        Settlement // 结算
    }

    /// <summary>
    /// v0.1.5 局会话房间流：战斗房 → 祭坛房 → … → BOSS 房 → 结算。
    /// 直接驱动 RunManager（StartRun / NotifyKill / NotifyRoomCleared / EndRun / ApplyRoomBuff）。
    /// 不改动 CombatStateMachine / PlayerCombatBootstrap / 基础移动，作为独立元循环模块。
    /// 通过订阅敌人 ChaState.onDeath 追踪击杀与清房；订阅玩家 onDeath 触发失败结算。
    /// </summary>
    [DisallowMultipleComponent]
    public class RogueRoomFlowController : MonoBehaviour
    {
        [Header("局会话引用（可留空，运行时自动查找）")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private ChaState playerState;

        [Tooltip("战斗房敌人根：进入战斗房时自动收集子级 ChaState 绑定（演示用）。")]
        [SerializeField] private Transform enemiesRoot;

        [Header("房间节奏")]
        [Tooltip("清完 N 个战斗房后进入 BOSS 房")]
        [SerializeField] private int combatRoomsBeforeBoss = 3;

        [Tooltip("进入场景立即自动开局（演示场景用）。")]
        [SerializeField] private bool autoStartOnEnable = true;

        private readonly Dictionary<ChaState, System.Action<DamageInfo>> _bindings =
            new Dictionary<ChaState, System.Action<DamageInfo>>();

        private RoomType _room = RoomType.Settlement;
        private int _clearedCombatRooms;
        private bool _runActive;

        public RoomType CurrentRoom => _room;
        public int ClearedCombatRooms => _clearedCombatRooms;

        /// <summary>一局结束（通关/失败）时触发，参数为是否通关。结算 UI 订阅此事件显示面板。</summary>
        public event System.Action<bool> onSettled;

        private void Awake()
        {
            if (runManager == null) runManager = FindObjectOfType<RunManager>();
            if (playerState == null) playerState = FindPlayerState();
            if (playerState != null)
            {
                playerState.onDeath += OnPlayerDeath;
            }
            RogueDiag.Log("[Diagnostics] RogueRoomFlowController 已就绪（挂载于 " + name + "）。", this);
        }

        private void Start()
        {
            if (autoStartOnEnable && !_runActive)
            {
                StartRun();
            }
        }

        private void OnDestroy()
        {
            if (playerState != null) playerState.onDeath -= OnPlayerDeath;
            UnbindRoomEnemies();
        }

        /// <summary>开局：清掉上一局强化，进入第一个战斗房。</summary>
        public void StartRun()
        {
            if (runManager == null)
            {
                RogueDiag.Warn("[Diagnostics] RogueRoomFlowController 缺少 RunManager，无法开局。");
                return;
            }

            runManager.StartRun();
            _clearedCombatRooms = 0;
            _runActive = true;
            EnterRoom(RoomType.Combat);
        }

        /// <summary>绑定当前战斗房的敌人列表，用于击杀计数与清房判定。</summary>
        public void BindCombatRoom(ChaState[] enemies)
        {
            UnbindRoomEnemies();
            if (enemies == null) return;

            foreach (ChaState enemy in enemies)
            {
                if (enemy == null || _bindings.ContainsKey(enemy)) continue;
                ChaState captured = enemy;
                System.Action<DamageInfo> handler = info => OnEnemyDeath(captured, info);
                _bindings[enemy] = handler;
                enemy.onDeath += handler;
            }

            RogueDiag.Log("[Diagnostics] 战斗房已绑定 " + _bindings.Count + " 个敌人。");
        }

        /// <summary>祭坛房选择完成：施加强化并推进到下一战斗房。</summary>
        public void OnAltarChosen(AttributeModifier mod)
        {
            if (runManager == null)
            {
                RogueDiag.Warn("[Diagnostics] 缺少 RunManager，祭坛强化未生效。");
                return;
            }

            runManager.ApplyRoomBuff(mod);
            runManager.NotifyRoomCleared();

            _clearedCombatRooms++;
            if (_clearedCombatRooms >= combatRoomsBeforeBoss)
            {
                EnterRoom(RoomType.Boss);
            }
            else
            {
                EnterRoom(RoomType.Combat);
            }
        }

        /// <summary>BOSS 被击败：通关结算。</summary>
        public void OnBossCleared()
        {
            if (runManager == null) return;
            runManager.NotifyRoomCleared();
            EndRun(true);
        }

        /// <summary>玩家手动/流程终止：结束一局（由外部或玩家死亡触发）。</summary>
        public void EndRun(bool isWin)
        {
            if (!_runActive) return;
            _runActive = false;
            UnbindRoomEnemies();
            runManager?.EndRun(isWin);
            EnterRoom(RoomType.Settlement);
            onSettled?.Invoke(isWin);
        }

        private void OnPlayerDeath(DamageInfo info)
        {
            EndRun(false);
        }

        private void OnEnemyDeath(ChaState enemy, DamageInfo info)
        {
            if (enemy != null)
            {
                if (_bindings.TryGetValue(enemy, out System.Action<DamageInfo> handler))
                {
                    enemy.onDeath -= handler;
                    _bindings.Remove(enemy);
                }
            }

            if (runManager == null) return;
            runManager.NotifyKill();

            RogueDiag.Log("[Diagnostics] 击杀 +1，本房剩余 " + _bindings.Count + " 个敌人。");
            if (_bindings.Count == 0 && _room == RoomType.Combat)
            {
                // 战斗房清空：判定是否已达到 BOSS 房前置条件
                if (_clearedCombatRooms + 1 >= combatRoomsBeforeBoss)
                {
                    EnterRoom(RoomType.Boss);
                }
                else
                {
                    runManager.NotifyRoomCleared();
                    _clearedCombatRooms++;
                    EnterRoom(RoomType.Altar);
                }
            }
        }

        private void UnbindRoomEnemies()
        {
            foreach (KeyValuePair<ChaState, System.Action<DamageInfo>> kv in _bindings)
            {
                if (kv.Key != null) kv.Key.onDeath -= kv.Value;
            }
            _bindings.Clear();
        }

        private void EnterRoom(RoomType room)
        {
            _room = room;
            RogueDiag.Log("[Diagnostics] 进入房间：" + room);

            if (room == RoomType.Combat && enemiesRoot != null && _bindings.Count == 0)
            {
                BindCombatRoom(enemiesRoot.GetComponentsInChildren<ChaState>());
            }
        }

        private static ChaState FindPlayerState()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            ChaState state = player != null ? player.GetComponentInChildren<ChaState>() : null;
            if (state == null)
            {
                RogueDiag.Warn("[Diagnostics] 未找到玩家 ChaState（检查 Player 标签与组件）。");
            }
            return state;
        }
    }

    /// <summary>局会话自检/提示日志，统一带 [Diagnostics] 前缀。</summary>
    internal static class RogueDiag
    {
        public static void Log(string message) => Debug.Log(message);

        public static void Log(string message, Object context) => Debug.Log(message, context);

        public static void Warn(string message) => Debug.LogWarning(message);
    }
}
