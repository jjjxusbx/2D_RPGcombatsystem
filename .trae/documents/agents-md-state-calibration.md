# 计划：仅校准 AGENTS.md（单一文件，文档修订，不运行构建）

> 目标：只修改 `AGENTS.md` 一个文件，把它校准到与当前代码 / 包 / 模块的真实状态一致，消除其中过时或与现状不符的表述。不改其他文档、不改代码、不运行构建。完成后再回传「改了哪里」。

---

## 一、当前状态分析（事实 / 风险 / 结论）

### 事实
- `AGENTS.md`（[AGENTS.md](file:///d:/project_unity/2D_RPGcombatsystem/AGENTS.md)）当前内容已确认。其中存在相对真实状态**过时或不完整**的地方：
  1. **Tech Stack（L21-30）未列出 `com.unity.entities`**，但 [manifest.json](file:///d:/project_unity/2D_RPGcombatsystem/Packages/manifest.json) 已含 `com.unity.entities: 1.4.8`。
  2. **Directory Structure 未提及 `CombatSystem/ECS/`**（`Game.ECS.Buff` 程序集，`autoReferenced:false`，仅被该程序集自引用，不参与默认三 csproj）。
  3. **Directory Structure 未提及新增的 `Roguelike/` 模块**（`RogueRoomFlowController`、`FragmentDrop`、`FragmentSpawner`、`AltarChoiceUI`、`SettlementUI`）与 **`Editor/RogueSetup.cs`**；L47 的 Editor 工具列表也未含 `RogueSetup`。
  4. **能力盘点「局会话（Rogue）」行（L89）**只写 `RunManager/RunCurrencyStore`，未反映房间流/掉落/祭坛/结算代码已落地；仍写「结算/房间流待场景联调」。
  5. **能力盘点缺「ECS Buff」「移动演示（Playable）」行**：`CombatSystem/ECS/` 与 `.trae/specs/arog-playable-movement-controller/`（规格三件套）均未在盘点表体现。`.trae/rules/movement-fsm-playable.md` 明确要求在模块落地前不得当作可用能力，应与盘点表一致地标注「规划中/未实现」。
  6. **已知问题与风险（L108-113）**未提及 ECS 程序集隔离与新增 Roguelike 模块未接场景/未验证。

### 风险
- 这些缺口会让「有文件 ≠ 可用能力」的盘点失真（尤其是 ECS/移动模块的存在与真实可用性）。
- 只改 AGENTS.md 无法同步修正 `docs/版本计划.md`、`docs/核心框架设计.md`、`docs/战斗属性ECS框架.md` 等伴生文档的对应不一致（如 v0.1.5 前置条件 SaveManager、ECS 文档「未安装 Entities」表述）。本轮按用户要求**仅限 AGENTS.md**，伴生文档的不一致留待后续单独处理。

### 结论
- 界定为「事实性校准」：把客观已存在、但不被 AGENTS.md 记录/误记的内容如实更正，**不未来评审、不做路线决策**，因此引入新冲突的风险最低。
- 范围约束：仅 `AGENTS.md`。不触碰代码、场景、预制体、其他 `.md`。不运行构建。

### 验证
- 本轮为文档修订，**不运行构建**。完成后的验证 = 确认仅 `AGENTS.md` 被修改（`git status`/`git diff --stat` 只出现 AGENTS.md），且改动后的 AGENTS.md 与上述实际情况一致（可人工核对）。

---

## 二、建议改动（全部落在 AGENTS.md，按小节列出：改什么 / 为什么）

### 1. Tech Stack（L21-30 区域，新增一条）
- **改什么**：新增一行 `Unity Entities 1.4.8（DOTS，已安装；当前仅被 Game.ECS.Buff 程序集引用，未接入主线战斗）`。
- **为什么**：manifest 已装 Entities，技术栈清单遗漏，属 P3 不一致的 AGENTS.md 侧。

### 2. Directory Structure（L34-56，增补条目）
- **改什么**：
  - 在 `CombatSystem/` 相关条目下新增：`CombatSystem/ECS/：Game.ECS.Buff 程序集（BuffECSComponents/Queries/Config/Systems/Authoring/Runtime；autoReferenced:false，不纳入默认三 csproj，未接入主线）。`
  - 新增：`Roguelike/（Roguelike.Flow / .Reward / .UI）：v0.1.5 局会话元循环与表现——RogueRoomFlowController、FragmentDrop、FragmentSpawner、AltarChoiceUI、SettlementUI。`
  - 在 L47 Editor 工具列表追加 `RogueSetup`。
- **为什么**：目录结构应反映真实存放位置；否则读者按图索骥找不到 ECS 与 Roguelike 模块。

### 3. 当前能力盘点（L77-99，更新 + 新增行）
- **改什么**：
  - 「局会话（Rogue）」行改为：`原型 | 条件可用 | RunManager/RunCurrencyStore + Roguelike/{房间流,碎片掉落,祭坛,结算}；代码已落地，未接场景、未 Play Mode 验证。`
  - 新增行：`ECS Buff（DOTS）| 原型/试验 | 否 | CombatSystem/ECS/ Game.ECS.Buff（autoReferenced:false，不参与默认三 csproj）；Entities 1.4.8 已装；未接入主线战斗。`
  - 新增行：`移动演示（Playable）| 规划中/未实现 | 否 | .trae/specs/arog-playable-movement-controller/ 规格三件套存在；无运行代码/场景；按 .trae/rules/movement-fsm-playable.md 不作为可用能力。`
- **为什么**：盘点表是「有文件 ≠ 可用能力」的关键校准表；补全实际存在但未反映的模块，并把未实现项明确标为规划中，避免被误用。

### 4. 可复用边界（L101-106，补一句）
- **改什么**：追加一句 `ECS Buff（Game.ECS.Buff）程序集暂不复用：autoReferenced:false、未接入主线，待明确验收标准。`
- **为什么**：与盘点新增行呼应，避免把「有文件」当稳定框架复用。

### 5. 当前已知问题与风险（L108-113，追加两条）
- **改什么**：
  - 新增：`新增 Roguelike/ 模块（房间流/掉落/祭坛/结算）代码未接场景、未 Play Mode 验证；需在 Unity 内执行 Editor 装配工具后联调。`
  - 新增：`CombatSystem/ECS/（Game.ECS.Buff）依赖 Unity Entities 1.4.8，属未接入主线的独立程序集；后续若评估接入主线需先补齐编译与验收。`
- **为什么**：把已知缺口显式列出，作为后续最小验证与风险评估的依据。

---

## 三、假设与决策

- **唯一目标文件**：`AGENTS.md`。不修改其他 `.md`、代码、场景、预制体、`.meta`。
- **性质**：事实性校准（如实记录现状），不展开对版本计划/核心框架设计/ECS 文档等的跨文件修订（留待后续、单独授权）。
- **不运行构建**：纯 Markdown 修订，无编译影响。
- **风险提醒**：若后续要彻底消除与伴生文档的冲突，需另行批准修改 `版本计划.md`、`核心框架设计.md`、`战斗属性ECS框架.md`（非本文件，本轮不动）。

---

## 四、验证步骤

1. `git status --short` / `git diff --stat AGENTS.md` → 确认**只有 AGENTS.md** 被改动。
2. 人工核对：Tech Stack 含 Entities；Directory Structure 含 `CombatSystem/ECS/`、`Roguelike/`、`RogueSetup`；能力盘点含 ECS、移动演示（规划中）并更新局会话行；已知风险含 ECS 与 Roguelike 两条。
3. 不执行任何 `dotnet build` / Unity 命令。

> 交付说明格式（完成后按此回传）：改动文件 + 改动的小节与要点；未做的（其他文档/代码）与原因。
