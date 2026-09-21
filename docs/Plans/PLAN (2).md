# 双工位配方调和与未开工配方显示修复计划

## Summary
- 当前存在两类问题：
  - 开工状态下，后台调和按“单工位”写回 `PcRecipeCode`，只开启双工位且未启用双工单时，缺少“两个工位同配方”的共享写回策略。
  - 未开工时，系统只用 PLC 配方反查产品身份，没有把各工位 PLC 配方号作为实时快照更新到生产监控界面。
- 修复后：
  - 只开启双工位：运行任务配方调和时，工位 1/2 的 PC 配方同步保持一致。
  - 开启双工单：工位 1/2 各自按自己的任务配方独立调和。
  - 未开工：只读取各工位 PLC 配方并更新界面，不写 `PcRecipeCode`，不做调和。

## Key Changes
- 抽出配方工位范围规则，例如 `RecipeStationScopeRules`：
  - `EnableDualStation=true && EnableDualWorkOrder=false` 时，关联工位为 `[1, 2]`。
  - 单工位或双工单时，关联工位为当前工位。
  - `MonitorView.ResolveWorkOrderSignalStations` 和后台调和服务共用该规则，避免 UI 与后台行为不一致。

- 扩展 `IPlcRecipeReconcileMonitorService`：
  - 增加 `RecipeCodeChanged` 事件和 `GetCurrent(stationNo)` 快照读取。
  - 新增快照模型：`StationNo`、`RecipeCode`、`IsSuccess`、`Message`、`ReadAt`。
  - 快照只表示 PLC 回读配方，不代表任务期望配方。

- 修改 `RecipeCodeReconcileMonitorService`：
  - 轮询不再在“无运行任务”时直接返回；仍按当前工位模式扫描工位 1/2 或默认工位。
  - 工位有运行任务且 `ValidateRecipeAfterStart=true`：读取 `PlcRecipeCode`，若与任务 `RecipeCode` 不一致，则按关联工位范围写回 `PcRecipeCode` 并校验。
  - 工位有运行任务但 `ValidateRecipeAfterStart=false`：不调和，只清理 mismatch 状态。
  - 工位无运行任务：只读取 `PlcRecipeCode` 并发布快照；读取失败不写 PLC，不调和，不高频刷异常。
  - 同工单双工位调和成功后，两个工位的 PC 配方都应被写为同一个任务配方。

- 修改 `MonitorView`：
  - 注入并订阅 `IPlcRecipeReconcileMonitorService.RecipeCodeChanged`。
  - 当前工位无运行任务时，`selectRecipeCode` 优先显示该工位最新 PLC 配方快照；无快照时显示 `--` 或回退到已选程序配方。
  - 当前工位有运行任务时，继续显示任务配方，扫码或 PLC 空闲配方变化不覆盖任务显示。
  - 未开工配方快照变化后触发一次方案预览刷新，让 PLC 配方反查到的产品配置能同步更新。

## Test Plan
- 规则测试：
  - 双工位非双工单时，配方关联工位解析为 `[1, 2]`。
  - 双工位双工单时，工位 1 只解析为 `[1]`，工位 2 只解析为 `[2]`。
  - 未开工快照规则不产生写入动作，只更新快照。
- 后台调和测试：
  - 只开启双工位，任务配方为 `1`，工位 1 PLC 配方变为 `3`：后台写回工位 1/2 的 `PcRecipeCode=1`。
  - 开启双工单，工位 1 任务配方 `1`、工位 2 任务配方 `2`：两个工位独立调和，互不覆盖。
  - `ValidateRecipeAfterStart=false` 时，运行任务不执行调和。
- UI 验证：
  - 未开工时，工位 1/2 分别显示各自 PLC 配方号。
  - 未开工时 PLC 配方变化，生产监控配方号实时更新。
  - 开工后，界面显示任务配方，且双工位非双工单时两个 PC 配方保持一致。
- 构建验证：
  - `dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore`
  - `dotnet build AutoWeldSystem.sln --no-restore`

## Assumptions
- “开工状态必须调和”仍受现有系统设置 `ValidateRecipeAfterStart` 控制。
- “未开工更新 PC 配方”指更新上位机界面/内存显示，不写 PLC 的 `PcRecipeCode`。
- 不改数据库结构。
- 未开工读取 PLC 配方失败时保留最近一次成功快照；没有成功快照时界面显示 `--`。
