using System;
using System.Collections;
using UnityEngine;

namespace TurnBased
{
    /// <summary>单位初始化配置：组合根从序列化字段/行为配置裁剪而来。</summary>
    public sealed class UnitConfig
    {
        public string entityId;
        public string displayName;
        public TeamId team;
        public int maxHealth;
        public int speed;
        public int attackDamage;
        public int attackRange;
        public int moveRangePerTurn = 1;
        public int initialEnergy = 80;
    }

    /// <summary>
    /// 回合制单位基类：网格实体 + 可受伤 + 可攻击 + 能量行动者 + 格子/Transform 同步。
    /// 由组合根创建并 Configure，随后注册进 GridMap 与 TurnManager。
    /// </summary>
    public abstract class TurnUnit : MonoBehaviour, IGridEntity, IDamageable, IAttacker, IUseable, ITurnActor
    {
        // ---- 数据 ----
        [SerializeField] private string entityId = "";
        [SerializeField] private string displayName = "Unit";
        [SerializeField] private TeamId team;
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int speed = 10;
        [SerializeField] private int attackDamage = 5;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private int moveRangePerTurn = 1;
        [SerializeField] private int initialEnergy = 80;

        // ---- 运行时状态 ----
        private GridMap _map;
        private GridPosition _gridPos;
        private int _health;
        private int _energy;
        private SpriteRenderer _sprite;

        // ---- 契约实现 ----
        public string EntityId => entityId;
        public string DisplayName => displayName;
        public TeamId Team => team;
        public GridPosition GridPosition => _gridPos;
        public bool IsAlive => !IsDead;
        public int MaxHealth => maxHealth;
        public int Health => _health;
        public bool IsDead => _health <= 0;
        public int AttackDamage => attackDamage;
        public int AttackRange => attackRange;
        public int Energy => _energy;
        public int MoveRangePerTurn => moveRangePerTurn;

        public event Action<IDamageable, int, IEntity> OnTakeDamage;
        public event Action<ITurnTaker> OnTurnEnd;

        // ---- 初始化（组合根调用） ----
        public virtual void Configure(UnitConfig config, GridMap map, GridPosition start, Color color)
        {
            entityId = config.entityId;
            displayName = config.displayName;
            team = config.team;
            maxHealth = config.maxHealth;
            speed = config.speed;
            attackDamage = config.attackDamage;
            attackRange = config.attackRange;
            moveRangePerTurn = config.moveRangePerTurn;
            initialEnergy = config.initialEnergy;

            _map = map;
            _gridPos = start;
            _health = maxHealth;
            _energy = initialEnergy;

            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite == null) _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = TurnUnitVisuals.WhiteSprite;
            _sprite.color = color;
            _sprite.sortingOrder = 10;

            transform.position = map.GridToWorld(start);
        }

        // ---- 移动执行 ----
        /// <summary>逻辑移动：更新 GridMap 占位与格子坐标（表现由 PresentMove 负责）。</summary>
        public bool TryCommitMove(GridPosition to)
        {
            if (_map == null)
            {
                TurnLog.Warn($"{DisplayName} 未绑定地图，无法移动");
                return false;
            }
            if (!_map.IsCellFree(to, this, to))
            {
                TurnLog.Warn($"{DisplayName} 移动被拒绝：{to} 不可行走或已被占用");
                return false;
            }
            if (!_map.TryMoveEntity(this, _gridPos, to))
            {
                TurnLog.Warn($"{DisplayName} 移动失败：{_gridPos} → {to}");
                return false;
            }
            _gridPos = to;
            return true;
        }

        /// <summary>表现移动：0 秒即时对齐格子；&gt;0 秒协程补间，完成后回调（动画锁）。</summary>
        public void PresentMove(GridPosition from, GridPosition to, float seconds, Action onDone)
        {
            if (seconds <= 0f)
            {
                transform.position = _map.GridToWorld(_gridPos);
                onDone?.Invoke();
                return;
            }
            StartCoroutine(LerpMove(from, to, seconds, onDone));
        }

        private IEnumerator LerpMove(GridPosition from, GridPosition to, float seconds, Action onDone)
        {
            Vector3 a = _map.GridToWorld(from);
            Vector3 b = _map.GridToWorld(to);
            float t = 0f;
            try
            {
                while (t < 1f)
                {
                    t += Time.deltaTime / Mathf.Max(0.001f, seconds);
                    transform.position = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                    yield return null;
                }
                transform.position = b;
            }
            finally
            {
                // 无论动画是否被打断，都解除动画锁，避免行动队列卡死
                onDone?.Invoke();
            }
        }

        // ---- 伤害 / 攻击 / 使用 ----
        public void TakeDamage(int amount, IEntity source)
        {
            if (IsDead)
            {
                TurnLog.Warn($"{DisplayName} 已死亡，忽略伤害");
                return;
            }
            if (amount < 0) amount = 0;
            _health -= amount;
            if (_health < 0) _health = 0;

            TurnLog.Log($"{DisplayName} 受到 {amount} 点伤害（来源 {source?.DisplayName}），生命 {_health}/{maxHealth}");
            OnTakeDamage?.Invoke(this, amount, source);

            if (IsDead) OnDeath();
        }

        protected virtual void OnDeath()
        {
            TurnLog.Log($"{DisplayName} 死亡，移出网格");
            _map?.TryRemoveEntity(this, _gridPos);
            OnTurnEnd?.Invoke(this);
            gameObject.SetActive(false);
        }

        public bool CanAttack(IDamageable target)
        {
            if (target == null || target.IsDead) return false;
            if (!(target is IGridEntity ge)) return false;
            return _gridPos.ManhattanDistance(ge.GridPosition) <= attackRange;
        }

        public void Use(IEntity user)
        {
            TurnLog.Warn($"{DisplayName} 尚未实现 Use（预留道具/技能通道）");
        }

        // ---- 能量行动者 ----
        public int GetSpeed() => speed;
        public void GainEnergy(int amount)
        {
            if (amount > 0) _energy += amount;
        }

        public bool TryConsumeEnergy(int cost)
        {
            if (_energy < cost)
            {
                TurnLog.Warn($"{DisplayName} 能量不足（{_energy}/{cost}）");
                return false;
            }
            _energy -= cost;
            return true;
        }

        public void OnTurnTaken()
        {
            TurnLog.Log($"{DisplayName} 取得行动权（剩余能量 {_energy}）");
        }

        /// <summary>行动结束（由调度器在 TakeTurn 后调用，触发 OnTurnEnd 观察者事件）。</summary>
        public void EndTurn()
        {
            OnTurnEnd?.Invoke(this);
        }

        // ---- 行动决策（子类实现） ----
        public abstract void TakeTurn(TurnExecutionContext context);
    }
}