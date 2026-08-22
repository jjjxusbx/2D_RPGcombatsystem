namespace TurnBased
{
    /// <summary>移动行动：逻辑即时更新 GridMap 占位，表现由单位协程补间。</summary>
    public sealed class MoveAction : ITurnAction
    {
        private readonly TurnUnit _unit;
        private readonly GridPosition _to;
        private readonly float _presentSeconds;
        private GridPosition _from;

        public MoveAction(TurnUnit unit, GridPosition to, float presentSeconds)
        {
            _unit = unit;
            _to = to;
            _presentSeconds = presentSeconds;
        }

        public string Describe() => $"{_unit?.DisplayName} 移动 {_from} → {_to}";
        public float PresentationDuration => _presentSeconds;

        public void Resolve()
        {
            _from = _unit.GridPosition;
            if (!_unit.TryCommitMove(_to))
            {
                TurnLog.Warn($"[行动] {_unit.DisplayName} 移动被拒绝（目标 {_to} 不可行走或已被占用）");
            }
        }

        public void Present(TurnEventQueue queue)
        {
            _unit.PresentMove(_from, _to, _presentSeconds, queue.NotifyPresentationDone);
        }
    }

    /// <summary>攻击行动：射程判定 + 伤害即时结算，表现留接口（框架阶段为 0 时长）。</summary>
    public sealed class AttackAction : ITurnAction
    {
        private readonly TurnUnit _attacker;
        private readonly IDamageable _target;
        private readonly GridPosition _targetPos;

        public AttackAction(TurnUnit attacker, IDamageable target, GridPosition targetPos)
        {
            _attacker = attacker;
            _target = target;
            _targetPos = targetPos;
        }

        public string Describe() => $"{_attacker?.DisplayName} 攻击 {(_target as IEntity)?.DisplayName ?? "?"}";
        public float PresentationDuration => 0f;

        public void Resolve()
        {
            if (_target == null || _target.IsDead)
            {
                TurnLog.Warn($"[行动] {_attacker.DisplayName} 攻击目标已死亡/为空，跳过");
                return;
            }
            int dist = _attacker.GridPosition.ManhattanDistance(_targetPos);
            if (dist > _attacker.AttackRange)
            {
                TurnLog.Warn($"[行动] {_attacker.DisplayName} 攻击超出射程（距离 {dist} > {_attacker.AttackRange}），跳过");
                return;
            }
            _target.TakeDamage(_attacker.AttackDamage, _attacker);
        }

        public void Present(TurnEventQueue queue) => queue.NotifyPresentationDone();
    }

    /// <summary>等待行动：单位选择待机时入队，保持日志可观察。</summary>
    public sealed class WaitAction : ITurnAction
    {
        private readonly TurnUnit _unit;

        public WaitAction(TurnUnit unit) => _unit = unit;
        public string Describe() => $"{_unit?.DisplayName} 待机";
        public float PresentationDuration => 0f;
        public void Resolve() { }
        public void Present(TurnEventQueue queue) => queue.NotifyPresentationDone();
    }
}