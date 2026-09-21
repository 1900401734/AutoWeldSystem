# 产品重焊/删除 + 程序计数模式 设计文档

日期：2026-09-09　版本：2.26.0　状态：已实施

## 背景

点焊设备现场需要在产品历史中对未上传产品做「重焊」（后续采集覆盖）与「删除」（不上传，可撤销）。这两个动作让本地产量与 PLC 计数器脱钩，因此同批引入「产量统计来源」开关，程序模式下产品编号、完工三项数量与监控页产量都由程序统计。中心看板在用，需同步作废语义。

## 已核实的关键事实

- 现有重测只服务整件检测，靠 PLC 编号与紧邻上轮相同被动识别（`ProductRetestRules`）；点焊设备命中自然键即静默跳过。
- 自然键 `(TaskId, StationNo, ProductNo, TouchNo)` 此前无数据库唯一索引；`_dbLock` 为单例实例锁。
- 现场 `ProductNoExpr = 0:H-0`（Int16 原值），实际值裸十进制。
- `IsTest` 产品级语义、焊点级冗余存储，读侧 `Any` 聚合。
- 既存缺陷：`ApplyRetestValues` 把 `IsTest` 覆盖回采集值，采集从不写该字段。
- MES 完工 `ExpEnd` 三项全部来自 PLC 快照；完工先 `EndWorkAsync` 后排队补传。
- `UploadMode` 已热生效；Batch 模式生产期间全部 Pending。
- 报表表头只有工单数量与合格数；中心侧无删除端点，`MergeRows` 整产品覆盖；`TodayTotalCount` 每次 ingest 全量重算但只看当天。
- 设置项中仅数据库连接串需重启。

## 决策

| # | 决策 |
|---|---|
| 门禁 | 统一按「产品未上传」（Pending/Failed/Retrying），范围随 `UploadMode` 变化 |
| 重焊 | 单槽位预约、覆盖后自动清除、可取消；标记存焊点行 `IsReweldPending` |
| 删除 | 软删 `IsDeleted`，行保留可撤销；编号不回收 |
| 编号 | 每任务每工位 `Max+1` 裸数字，含已删除行 |
| 开关 | 单一 `ProductionCountSource = Plc/Program`，默认 Plc，有任务时禁止切换，放「生产配置」分组 |
| 数量 | 总数按工位+编号去重、排除已删除；合格=OK；不良=总数−合格；试焊件计入 |
| PLC 计数 | 程序模式不读 PLC 计数、不阻塞；写 `PLC.FinishQuantity.Compare` 对账日志 |
| 指标 | 监控页产量与完工同源 |
| 整件检测 | PLC 模式保留被动重测；程序模式仍读 PLC 编号作信号，预约优先 |
| 覆盖字段 | 结果类覆盖、上传状态复位、主键/序号/编号保留、`IsTest` 保留、`IsReweldPending/IsDeleted` 清零 |
| 上传任务 | 删除时过程参数任务置 `Skipped`，撤销时重新入队 |
| 中心 | `CenterProductReportRequest.IsDeleted`；隐藏页列末尾追加；`LoadProducts` 与可见页过滤 |
| 权限 | 不新增重焊/删除权限点；删除二次确认 + 操作日志；采集数据页签新增权限码仅开发者默认可见 |
| 唯一索引 | 无条件补 `UX_WeldPointRecord_NaturalKey`，升级前查重 |

## 实现要点

- Core：`LocalProductNoRules`、`FinishQuantityRules`、`ProductHistoryActionRules`、`ProductResultResolver`、`WeldPointRecordScopeRules`、`IProductionCountService`。
- Services：`ProductCycleCollectionService.ResolveLocalProductNo` 在锁内取号并决定覆盖目标；`ProductHistoryService` 四个动作共用 `ApplyProductAction` 骨架；`UploadTaskService.SkipProcessParameterTasks` 解析载荷按产品号跳过。
- UI：`MonitorView` 右键菜单按 `item.ID` 分发；`TryResolveFinishQuantities` 与 `BindProductionMetrics` 按模式分流；`SystemSettingView` 新增下拉与保存锁定；`DataManageView.ApplyTabPermissions` 运行时挂载采集数据页签。
- CenterServer：存储行、隐藏页列、当日计数、可见页四处加 `IsDeleted`。

## 既存限制

- 中心看板跨天删除不修正历史日计数。
- 多台上位机共库时进程内锁失效，依赖唯一索引兜底（撞号抛错而非静默丢件）。
