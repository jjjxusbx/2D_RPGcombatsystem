# 任务清单

- [x] Task 1: 清理 `PlayerAttackTrigger.cs`
  - [x] 删除 `string branch` 及 3 处 `branch = "...";` 赋值（消除 CS0219）
  - [x] 删除 `// #region agent log H1/H2` 空注释
  - [x] 删除 `OnTriggerEnter2D` 内 4 条 `[CombatHitDebug]` Debug.Log 及仅用于日志的 `colliderEnabled`
- [x] Task 2: 清理 `CameraFollow2D.cs` 空注释残留
  - [x] 删除 `// #region agent log H3` / `// #endregion`
- [x] Task 3: 删除无引用空壳 `CombatHitDebugProbe.cs` + `.meta`（GUID 1471c7a2...）
- [x] Task 4: 删除无引用遗留脚本 `Test.cs`(GUID d837c2bd...) / `Test4.cs`(GUID 6b4a6f7e...) 及 `.meta`
- [x] Task 5: 编译验证三个 csproj（均 0 警告 0 错误；为让 dotnet build 反映删除，从两个运行时 csproj 移除对应 `<Compile>` 条目）
- [x] Task 6: 同步更新 `AGENTS.md`「当前已知问题与风险」（移除已解决项）
