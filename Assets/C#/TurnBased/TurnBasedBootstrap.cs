using System.Collections.Generic;
using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 回合制网格战术组合根：显式装配 GridMap / TurnManager / TurnEventQueue /
    /// 玩家与怪物实体 / AI 上下文，参照 PlayerCombatBootstrap 模式。
    /// 完全独立于现有实时战斗管线（CombatSystem/CombatStateMachine 等不受影响）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurnBasedBootstrap : MonoBehaviour
    {
        [Header("演示")]
        [Tooltip("勾选后由 autoDemo 驱动玩家/怪物自动行动并打印日志，无需手操输入。")]
        public bool autoDemo = true;

        [Tooltip("自动演示最大轮数。")]
        public int demoRounds = 60;

        [Tooltip("每轮推进间隔（秒）。")]
        public float roundIntervalSeconds = 0.15f;

        [Tooltip("移动表现时长（秒）：0 = 逻辑即时结算；>0 = 演示动画锁（队列等待表现播完）。")]
        public float movePresentationSeconds = 0f;

        [Header("玩家")]
        public int playerSpeed = 25;
        public int playerMaxHealth = 50;
        public int playerAttackDamage = 10;
        public int playerAttackRange = 1;
        public int initialEnergy = 80;
        public Vector2Int playerStart = new Vector2Int(4, 4);

        [Header("怪物生成")]
        [Tooltip("留空则使用默认两条：近战莽夫(11,7) + 远程风筝(10,2)。")]
        public MonsterSpawnEntry[] monsterSpawns;

        // ---- 装配产物（供 DemoDriver / Diagnostics 读取） ----
        public GridMap Map { get; private set; }
        public TurnManager TurnManager { get; private set; }
        public TurnEventQueue TurnQueue { get; private set; }
        public TurnExecutionContext Context { get; private set; }
        public TurnPlayerUnit Player { get; private set; }
        public List<TurnMonsterUnit> Monsters { get; } = new List<TurnMonsterUnit>();

        public bool AllMonstersDead => Monsters.Count == 0 || Monsters.TrueForAll(m => m == null || m.IsDead);

        private void Awake()
        {
            LevelData level = SimpleLevelGenerator.GenerateDemoLevel();
            Map = new GridMap(level.Width, level.Height, level.Walkable);
            TurnManager = new TurnManager();

            TurnQueue = GetComponent<TurnEventQueue>();
            if (TurnQueue == null) TurnQueue = gameObject.AddComponent<TurnEventQueue>();

            SpawnPlayer();
            SpawnMonsters();

            Context = new TurnExecutionContext
            {
                Map = Map,
                Queue = TurnQueue,
                DistanceField = new DistanceField(),
                Pathfinder = new PathfinderAStar(),
                Fov = new FieldOfView(),
                Player = Player,
                Units = new List<TurnUnit>(),
                MovePresentationSeconds = movePresentationSeconds,
            };
            Context.Units.Add(Player);
            for (int i = 0; i < Monsters.Count; i++) Context.Units.Add(Monsters[i]);
        }

        private void Start()
        {
            TurnBasedDiagnostics diagnostics = GetComponent<TurnBasedDiagnostics>();
            if (diagnostics == null) diagnostics = gameObject.AddComponent<TurnBasedDiagnostics>();
            diagnostics.Run(this);

            if (autoDemo)
            {
                var driver = GetComponent<TurnBasedDemoDriver>();
                if (driver == null) driver = gameObject.AddComponent<TurnBasedDemoDriver>();
                driver.Begin(this, demoRounds, roundIntervalSeconds);
            }
        }

        private void SpawnPlayer()
        {
            var go = new GameObject("TurnPlayer");
            go.transform.SetParent(transform, false);

            var unit = go.AddComponent<TurnPlayerUnit>();
            var config = new UnitConfig
            {
                entityId = "player",
                displayName = "玩家",
                team = TeamId.Player,
                maxHealth = playerMaxHealth,
                speed = playerSpeed,
                attackDamage = playerAttackDamage,
                attackRange = playerAttackRange,
                initialEnergy = initialEnergy,
            };
            unit.Configure(config, Map, new GridPosition(playerStart.x, playerStart.y), Color.cyan);

            if (!Map.TryPlaceEntity(unit, unit.GridPosition))
            {
                TurnLog.Error("玩家出生点非法，未注册进网格");
            }
            TurnManager.AddUnit(unit);
            Player = unit;
        }

        private void SpawnMonsters()
        {
            if (monsterSpawns == null || monsterSpawns.Length == 0)
            {
                monsterSpawns = new[]
                {
                    new MonsterSpawnEntry { archetype = MonsterArchetype.MeleeBerserker, gridPos = new Vector2Int(11, 7) },
                    new MonsterSpawnEntry { archetype = MonsterArchetype.RangedKiter, gridPos = new Vector2Int(10, 2) },
                };
            }

            for (int i = 0; i < monsterSpawns.Length; i++)
            {
                var entry = monsterSpawns[i];
                var behavior = entry.archetype == MonsterArchetype.RangedKiter
                    ? MonsterBehaviorConfig.RangedKiter()
                    : MonsterBehaviorConfig.MeleeBerserker();

                var go = new GameObject($"TurnMonster_{i + 1}_{behavior.displayName}");
                go.transform.SetParent(transform, false);

                var unit = go.AddComponent<TurnMonsterUnit>();
                var config = new UnitConfig
                {
                    entityId = $"monster_{i + 1}",
                    displayName = behavior.displayName,
                    team = TeamId.Monster,
                    maxHealth = behavior.maxHealth,
                    speed = behavior.speed,
                    attackDamage = behavior.attackDamage,
                    attackRange = behavior.attackRange,
                    moveRangePerTurn = behavior.moveRangePerTurn,
                    initialEnergy = initialEnergy,
                };
                unit.ConfigureMonster(config, behavior, Map, new GridPosition(entry.gridPos.x, entry.gridPos.y),
                    entry.archetype == MonsterArchetype.RangedKiter ? new Color(1f, 0.7f, 0f) : new Color(1f, 0.3f, 0.3f));

                if (!Map.TryPlaceEntity(unit, unit.GridPosition))
                {
                    TurnLog.Error($"{behavior.displayName} 出生点非法，未注册进网格");
                }
                TurnManager.AddUnit(unit);
                Monsters.Add(unit);
            }
        }
    }
}