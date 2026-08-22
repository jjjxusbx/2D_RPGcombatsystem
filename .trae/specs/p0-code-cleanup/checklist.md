# 验收清单

- [x] `dotnet build .\Assembly-CSharp.csproj`：0 错误 0 警告
- [x] `dotnet build .\Assembly-CSharp.Player.csproj`：0 错误 0 警告
- [x] `dotnet build .\Assembly-CSharp-Editor.csproj`：0 错误 0 警告（未受影响）
- [x] 全库 `agent log` / `[CombatHitDebug]` / `hypothesisId` / `D:\.cursor` / `CombatHitDebugProbe` / `Test4` 引用：命中为 0
- [x] `Test`/`Test4`/`CombatHitDebugProbe` GUID 无场景/预制体引用（仅各自 .meta）
- [x] 删除文件时连带删除 `.meta`，无 dangling GUID
- [x] 未改动任何 `Assets/C#/Editor/` 与 `Assets/Plugins/` 脚本；三程序集编译通过

> 附注：为保证 `dotnet build` 反映删除，从 `Assembly-CSharp.csproj`、`Assembly-CSharp.Player.csproj` 各移除 3 条对应 `<Compile>` 条目（Unity 下次导入会重新生成一致 csproj）。
