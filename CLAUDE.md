# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**Unity 2D RPG Combat System** (灰烬矿脉 / Ashes Mine Vein) — Unity 6 prototype with FSM-based combat, attribute system, monster patrol (NavMesh), and a roguelike run session layer (v0.1.5 in progress).

**Tech stack:** Unity `6000.0.47f1`, C#, Unity 2D/Tilemap/UGUI/Cinemachine/AI Navigation/Input System packages (see `Packages/manifest.json` for exact versions).

**Current milestone:** v0.1.5 (roguelike run loop: enter room → clear enemies → altar upgrade → BOSS →结算). v0.1 core framework is delivered and builds cleanly.

---

## Architecture (4 Layers + Composition Root)

This project uses a strict **4-layer architecture** with a single composition root. All new code must fit into one of these layers; bypassing the composition root creates dual-entry bugs (two input paths, two state machines, or duplicated business rules).

### Data Layer
- `ChaState` / `Attribute` — Unified attribute container (8 stats: MaxHealth, HpRegen, Atk, Defense, MoveSpeed, AttackRate, PickupRange, Growth) with 5-stage modifier pipeline (flat add → % add → multiply → clamp).
- `SkillData`, `MagicEffectData`, `BuffData` — ScriptableObject configs; skills use main config + Projectile/Range sub-data.
- `PlayerConfig` — Player initial values (attack duration, combo window, dodge settings).

### Decision Layer
- `PlayerInputReader` — Input → intent abstraction. Keybinds are `SerializeField`-exposed (not hardcoded).
- `CombatDecisionComponent` — Stamina/cooldown decisions.
- `MonsterBrain` / `MonsterBrainStateMachine` — AI decision (turn-based prototype; not used by real-time patrol).

### Execution Layer
- `CombatStateMachine` — FSM with 5 states: `IdleState`, `MoveState`, `AttackState`, `DodgeState`, `CastBuffState`. Configured by `PlayerCombatBootstrap` in `Awake`.
- `SkillExecutor` — Executes `SkillData` configs (range detection → `magicEffect.Apply`).
- `MonsterPatrolController` — NavMesh-based patrol (Idle → Patrol → Chase). Uses `MonsterPatrolPath` for waypoints; 2D↔NavMesh XZ coordinate mapping in `PhysicsSystem2D`.
- `HitBoxController` — Hit detection for attacks.

### Presentation Layer
- `PlayerAnimationPresenter` — Animator parameter routing with `SetBoolIfExists` / `SetTriggerIfExists` existence guards (avoids errors when parameters are missing from a controller).

### Composition Root (唯一装配入口)
- **`PlayerCombatBootstrap`** — Assembles the entire player combat pipeline in `Awake()`: Data → Decision → Presentation → Execution. Also arbitrates legacy scripts (`基础移动`让权, `跳跃射箭` disabled in FSM mode to avoid dual `rb.linearVelocity` writes).
- **`RuntimeDiagnostics`** — Outputs `[Diagnostics]` logs at startup (missing components, unbound references, unconfigured skills).
- **`RunManager`** — Roguelike run session root (fragment tracking, kill count, room index, `_sessionSource` for modifier cleanup).
- **`RunCurrencyStore`** — Stateless, atomic-write, versioned JSON persistence for off-run currency.

---

## Key Entry Points & Lifecycle

1. **`PlayerCombatBootstrap.Awake()`** → Adds missing components if absent, configures FSM, disables legacy scripts (FSM mode only).
2. **Component `Awake()`** → Self-bind (reader binds camera, ChaState registers stat map, presenter binds animator).
3. **`CombatStateMachine.Start()`** → Self-bind fallback if `Configure()` wasn't called; enters `IdleState`.
4. **`PlayerCombatBootstrap.Start()`** → `RuntimeDiagnostics.Run()` prints `[Diagnostics]` report.
5. **`CombatStateMachine.Update()`** → Single-frame single-read intent cache (`context.Intent`), dispatches to `currentState.Execute(ctx)`.

**Anti-patterns enforced by the framework:**
- No dual `rb.linearVelocity` writers (FSM mode disables `基础移动` movement; `跳跃射箭` is fully disabled until migrated to FSM in v0.2).
- No cross-scene singletons or static state.
- All damage goes through `ChaState.TakeDamage` → defense reduction → HP deduction → `onDamaged`/`onDeath` events. Presentation subscribes to events for hit-reaction/animation/destruction; ChaState does not destroy objects.

---

## Directory Structure (What to Know Before Searching)

```
Assets/
├── C#/                    # All runtime and editor C# scripts
│   ├── Character.cs       # Base class for all controllable characters
│   ├── 基础移动.cs         # Player movement (legacy; yields to FSM)
│   ├── 跳跃射箭.cs         # Jump + bow (disabled in FSM; to be migrated v0.2)
│   ├── EnemyBase.cs       # Enemy base: HP, death, hit-reaction via ChaState
│   ├── Attribute.cs       # Attribute value + modifier pipeline
│   ├── CombatData.cs      # SkillData/MagicEffectData/BuffData containers
│   ├── BuffSystem.cs      # BuffController + AttributeModifier lifecycle
│   ├── CombatSystem/      # CombatContext, ICombatState, FSM states
│   │   └── State/         # IdleState, MoveState, AttackState, DodgeState, CastBuffState
│   ├── Execution/         # CombatStateMachine, SkillExecutor, HitDetection, AI
│   │   ├── AI/            # MonsterPatrolController, MonsterPatrolPath, MonsterMoveToTarget
│   │   └── HitDetection/  # HitBoxController
│   ├── Decision/          # PlayerInputReader, CombatDecisionComponent, IPlayerIntent
│   ├── Presentation/      # PlayerAnimationPresenter
│   ├── Composition/       # PlayerCombatBootstrap, RuntimeDiagnostics
│   ├── TurnBased/         # Independent grid-based turn combat prototype
│   │   ├── Contracts/     # IEntity, ITurnTaker, IDamageable, IGridEntity, IUseable
│   │   ├── Data/          # GridMap, GridPosition, LevelData, SimpleLevelGenerator
│   │   ├── Decision/      # MonsterBrain, MonsterBrainStateMachine
│   │   ├── Entities/      # TurnUnit, TurnPlayerUnit, TurnMonsterUnit
│   │   ├── Execution/     # TurnManager, TurnActions, TurnEventQueue, PathfinderAStar, FieldOfView
│   │   └── Presentation/  # TurnUnitVisuals
│   ├── Editor/            # CompositionSetup, CombatSystemSetup, TurnBasedSetup, UiShowcaseSetup, MonsterPatrolSetup
│   ├── RunManager.cs      # Roguelike run session
│   └── RunCurrencyStore.cs # Off-run currency persistence
├── Scenes/                # SampleScene.unity (main), UiShowcase.unity, Test.unity
├── 动画/                   # Animation controllers/clips (Hero, Sword, Slash, Slime_sheet)
├── Arts/                  # Tilemap sprites, audio, UI assets
├── Plugins/               # Third-party plugins (ES installer)
├── docs/                  # Design docs, version plan, testing reports
└── .trae/
    ├── rules/             # Scene-triggered collaboration rules
    └── specs/             # Spec-driven task directories (spec.md + tasks.md + checklist.md)
```

---

## Common Commands

### Build (compile only — does not import Unity assets)

```powershell
dotnet build .\Assembly-CSharp.csproj
dotnet build .\Assembly-CSharp.Player.csproj
dotnet build .\Assembly-CSharp-Editor.csproj
```

- `--no-restore` may fail if Unity/Defender locks `obj` or `Temp`; rerun once without the flag before reporting failure.
- Three csproj target different assembly definitions; all must pass with 0 errors and 0 warnings before delivery.
- The `.sln` file has duplicate `Assembly-CSharp` project names; use the individual csproj commands instead.
- Sandbox environments may return exit code 1 for log-write restrictions; judge by "0 warnings / 0 errors" + DLL output, not the exit code.

### Unity Editor (open project)

```powershell
& "D:\Program Files\Unity 6000.0.47f1\Editor\Unity.exe" -projectPath "D:\project_unity\2D_RPGcombatsystem"
```

### Editor Setup Tools (run inside Unity, not CLI)

- `Tools → 战斗系统 → Compose Player Combat（核心框架装配）` — One-click player bootstrap (idempotent; safe to re-run).
- `Tools → 怪物巡逻 → Create Patrol Demo Setup` — Generates a patrol demo scene.
- `Tools → TurnBased → Setup TurnBased Demo` — Generates a turn-based grid demo.
- `Tools → UI Showcase → Setup UiShowcase` — Generates UI showcase scene.

### Testing & Validation

- **Compile validation**: Run the three `dotnet build` commands above. 0 errors / 0 warnings is the pass bar.
- **Play Mode validation** (required for behavior changes):
  - Movement triggers `IsRun` on the Hero Animator.
  - Attack triggers `ATK_1` (Sword) and `ATK1` (Slash) — do NOT hard-code state transitions; use Trigger parameters.
  - Monster patrol: patrol ↔ chase ↔ return-to-path loop; attack trigger fires in range; hit pauses navigation → resumes.
  - After bootstrap: `[Diagnostics]` should show **zero problem items** (warnings); info items are acceptable.
  - `跳跃射箭` must be disabled when FSM mode is active (no dual `rb.linearVelocity` writes).

---

## Development Workflow

### Before Changing Code

1. **Read the entry point first.** Start with this file, then `docs/版本计划.md` to confirm the current milestone scope.
2. **Check capability inventory.** AGENTS.md's "能力盘点 + 可复用边界" table is authoritative. Having a file ≠ available capability. Prototype/exists/conditional modules are not stable frameworks.
3. **Find the minimal verification path.** Trace from scene startup to actual invocation; confirm lifecycle (create → enable → update → disable → destroy); run the Play Mode checklist, not just compile.

### Version-Driven Workflow

The project follows the **版本计划.md** milestone plan. Each version has an independent acceptance checklist (two csproj builds + Play Mode validation). Version transitions require a retrospective review before the next version starts.

**Current in-progress: v0.1.5** (roguelike loop).
- Prerequisite: SaveManager extension for versioned off-run currency (only currency needs persistence this version).
- Delivery bar: 3+ room loop playable, `[Diagnostics]` zero issues, two csproj pass, post-run currency persists after restart.

### Spec-Driven Development (optional)

For new features, consider creating a spec directory under `.trae/specs/`:
```
.trae/specs/<feature-name>/
├── spec.md       # Requirements and design
├── tasks.md      # Task breakdown
└── checklist.md  # Acceptance checklist
```

### Adversarial Review Protocol (architecture/manager changes only)

Before any architecture or manager change, output the four-section review:
- **事实 (Facts):** Code, scene, package, or build evidence.
- **风险 (Risks):** Dual triggers, data loss, state lock, lifecycle leaks.
- **结论 (Conclusion):** Reusable / fixable / reference-only / does not exist.
- **验证 (Verification):** Compile, Unity startup, Play Mode, or behavior test results.

---

## Animator Conventions

Use **Trigger parameters** for state transitions; do not hard-code transitions in code when the controller uses Triggers.

| Character | Key Parameters |
|---|---|
| Hero | `IsRun` (bool), `Attack` (trigger) |
| Slime | `IsRun` (bool), `IsGetHit` (trigger), `Attack` (trigger) |
| Sword  | `ATK_1` (trigger) |
| Slash  | `ATK1` (trigger) |

Use `PlayerAnimationPresenter.SetBoolIfExists` / `SetTriggerIfExists` for parameter existence guards. Do NOT rotate `SwordPivot` during attack clips if the animation clip already owns that rotation.

---

## Key Script Paths (Chinese characters included — use exact paths)

Several script and asset directory names contain Chinese characters. In PowerShell use `-LiteralPath`; in Bash the paths work as-is. Do not transliterate or rename without explicit request.

---

## .gitignore and Generated Files

Never edit `Library/`, `Temp/`, `obj/`, `Logs/`, `.vs/`, `UserSettings/`. These are generated by Unity or the build system. Add `.meta` files for every new Unity asset or script (Unity requires them).

---

## Security

This is a local Unity prototype — no backend, no web, no auth, no key handling. Do not add network calls, credential storage, analytics, or external service integrations unless explicitly requested. Persistence is restricted to stateless, versioned, atomic writes (see `RunCurrencyStore`).

---

## Debugging Tips

- **FSM not firing:** Check `CombatStateMachine.Configure()` was called (look for `[Diagnostics]` problems at startup). Check `基础移动.useCombatStateMachine` is `true` in FSM mode.
- **Animator parameters not found:** Use `SetBoolIfExists` / `SetTriggerIfExists`. `Slime` controller is missing `IsRun`; guard it.
- **Monster not patrolling:** Confirm `MonsterPatrolPath` has waypoints and `NavMeshSurface` is baked. `MonsterPatrolController` requires `Rigidbody2D`, `Collider2D`, and `NavMeshAgent` (enforced by `RequireComponent`).
- **State machine stuck:** Check `CombatStateMachine.ChangeState` logs — illegal transitions are logged as `LogWarning`/`LogError`, not silently swallowed.
- **Skill not executing:** `SkillExecutor.mainSkill` must be assigned in the Inspector; `SkillData.ranges` must have at least one `RangeProperty` with a valid `radius`.

---

## Resources

- `docs/核心框架设计.md` — v0.1 architecture decisions, assembly chain, diagnostic contract.
- `docs/版本计划.md` — Milestone roadmap with acceptance checklists per version.
- `docs/策划流程与双档方案.md` — Design workflow and dual-track planning.
- `docs/测试报告.md` — Compile/functional/compatibility/performance/security test results.
- `docs/怪物巡逻系统使用与配置文档.md` — NavMesh patrol setup and parameter reference.
- `docs/配置步骤.md` — Scene wiring checklist for roguelike rooms and flow.
- `AGENTS.md` — Authoritative project context (capability inventory, reusable boundaries, known issues, adversarial review rules). Read this before the docs if you need to calibrate what is and isn't available in this repo.
