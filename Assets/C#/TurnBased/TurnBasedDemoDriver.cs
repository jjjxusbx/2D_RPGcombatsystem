using System.Collections;
using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 无 UI 自动演示驱动器：每轮由 TurnManager 得出行动者，
    /// 依次 TakeTurn 将行动入队，等待行动队列（含表现播放）清空后才推进下一轮。
    /// 全程打印日志，可在 Console 直接验证回合推进、能量频率与 AI 决策。
    /// </summary>
    public sealed class TurnBasedDemoDriver : MonoBehaviour
    {
        private TurnBasedBootstrap _bootstrap;
        private int _maxRounds;
        private float _interval;

        public void Begin(TurnBasedBootstrap bootstrap, int maxRounds, float intervalSeconds)
        {
            _bootstrap = bootstrap;
            _maxRounds = maxRounds;
            _interval = intervalSeconds;
            StartCoroutine(RunDemo());
        }

        private IEnumerator RunDemo()
        {
            yield return new WaitForSeconds(0.2f); // 先让诊断日志输出
            if (_bootstrap == null) yield break;

            var manager = _bootstrap.TurnManager;
            var queue = _bootstrap.TurnQueue;

            for (int round = 1; round <= _maxRounds; round++)
            {
                if (_bootstrap.AllMonstersDead)
                {
                    TurnLog.Log($"第 {round} 轮：所有怪物死亡，演示提前结束。");
                    yield break;
                }
                if (_bootstrap.Player == null || _bootstrap.Player.IsDead)
                {
                    TurnLog.Log($"第 {round} 轮：玩家死亡，演示提前结束。");
                    yield break;
                }

                var actors = manager.AdvanceRound();
                string names = actors.Count == 0
                    ? "（无，能量累积中）"
                    : string.Join("、", actors.ConvertAll(a => a.DisplayName));
                TurnLog.Log($"===== 第 {round} 轮：行动者 {names} =====");

                for (int i = 0; i < actors.Count; i++)
                {
                    if (actors[i] is ITurnActor actor)
                    {
                        actor.TakeTurn(_bootstrap.Context);
                        if (actors[i] is TurnUnit unit) unit.EndTurn();
                    }
                    else
                    {
                        TurnLog.Warn($"单位 {actors[i].DisplayName} 未实现 ITurnActor，跳过行动");
                    }
                }

                // 等待行动队列清空：逻辑已即时结算，表现播放完毕才进入下一轮（动画锁）
                int guard = 0;
                while (queue.IsBusy)
                {
                    yield return null;
                    if (++guard > 5000)
                    {
                        TurnLog.Error("行动队列表现超时，强制清空推进（动画锁可能失效）");
                        queue.Clear();
                        break;
                    }
                }

                if (_interval > 0f) yield return new WaitForSeconds(_interval);
            }

            TurnLog.Log($"===== 自动演示结束（共 {_maxRounds} 轮）=====");
        }
    }
}