using System.Collections.Generic;
using UnityEngine;

namespace TurnBased
{
    /// <summary>回合域统一日志前缀，便于 Console 过滤。</summary>
    public static class TurnLog
    {
        public static void Log(string message) => Debug.Log($"[Turn] {message}");
        public static void Warn(string message) => Debug.LogWarning($"[Turn] {message}");
        public static void Error(string message) => Debug.LogError($"[Turn] {message}");
    }

    /// <summary>
    /// 行动接口：逻辑即时结算（Resolve），表现按 PresentationDuration 排队播放。
    /// </summary>
    public interface ITurnAction
    {
        string Describe();
        void Resolve();

        /// <summary>表现时长（秒）：0 = 无表现，逻辑结算后立即放行下一行动。</summary>
        float PresentationDuration { get; }

        /// <summary>启动表现播放；表现完成后必须回调 queue.NotifyPresentationDone()（动画锁）。</summary>
        void Present(TurnEventQueue queue);
    }

    /// <summary>
    /// 行动队列（回合驱动的心脏）：玩家/怪物/环境行动统一入队。
    /// 底层数据在 Resolve 中瞬间结算，表现层按序播放（动画锁）完毕后才继续出队，
    /// 保证"全部播完才推进下一行动/回合"。提供 IsBusy 轮询接口供调度器等待。
    /// </summary>
    public sealed class TurnEventQueue : MonoBehaviour
    {
        private readonly Queue<ITurnAction> _actions = new Queue<ITurnAction>();
        private bool _presenting;

        public int PendingCount => _actions.Count;
        public bool IsBusy => _actions.Count > 0 || _presenting;

        public void Enqueue(ITurnAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[TurnEventQueue] Enqueue 收到 null，忽略");
                return;
            }
            _actions.Enqueue(action);
        }

        public void Clear()
        {
            _actions.Clear();
            _presenting = false;
        }

        private void Update()
        {
            if (_presenting || _actions.Count == 0) return;

            var action = _actions.Dequeue();
            action.Resolve();
            TurnLog.Log($"[队列] {action.Describe()}");

            if (action.PresentationDuration > 0f) _presenting = true;
            action.Present(this);
        }

        /// <summary>表现播放完成回调（动画锁解除），由各行动的表现实现调用。</summary>
        public void NotifyPresentationDone()
        {
            _presenting = false;
        }
    }
}