# 生产报表模板与多通道测试项输出设计

## 摘要

将设备端和中心服务器的生产报表统一为客户提供的“生产报表”模板：公共任务字段放在表头，焊点/拍照明细和动态测试项放在明细区。报表固定输出中文，并按实际动态列数扩展表头合并范围。

## 领域规则

### 任务与报表范围

- 一个流转卡号/工单对应一个 `BizWeldTask`，生成一份报表。
- 双工位设备可以同时生产同一工单；同一任务的两个工位记录合并到同一报表。
- 不同流转卡号/工单分别生成独立报表。
- 开始时间只使用任务 `StartTime`；结束时间只使用任务 `EndTime`。
- 任务未完工时结束时间留空，禁止使用当前时间或焊点采集时间替代。
- 报表生成前按 `TaskId` 读取最新任务，确保时间和最终统计与工单持久化数据一致。

### 表头字段

| 模板字段 | 数据来源 | 规则 |
| --- | --- | --- |
| 产品工号 | `BizWeldTask.ProductNum` | 任务级字段 |
| 图号 | `BizWeldTask.DrawingNo` | 任务级字段 |
| 批次 | `BizWeldTask.Batch` | 任务级字段 |
| 流转卡号 | `BizWeldTask.SN` | 报表身份标识 |
| 部件规格 | `BizWeldTask.Spec` | 任务级字段 |
| 型号 | `BizWeldTask.ProductModel` | 任务级字段 |
| 工序 | `BizWeldTask.ProcessNo` | 按客户要求使用工序号 |
| 生产数量 | `BizWeldTask.StartAmount` | 同一工单使用同一个值 |
| 合格数量 | `BizWeldTask.QualifiedQty` | 任务最终统计值 |
| 开始时间 | `BizWeldTask.StartTime` | `yyyy-MM-dd HH:mm:ss` |
| 结束时间 | `BizWeldTask.EndTime` | `yyyy-MM-dd HH:mm:ss`，完工前留空 |
| 操作人员 | `BizWeldTask.UserNumber` | 完工沿用开工操作员身份 |
| 备注 | 无 | 当前始终留空 |

### 明细字段

- Single-station reports omit the station column completely.
- Dual-station reports add a station column and resolve station number through the configurable mapping (`1` and `2` by default “左” and “右”).
- Product number and point number are preserved as collected values.
- Point result is the PLC-collected `BizWeldPointRecord.TestResult`; the display title comes from `BizProductProcessConfig.PointResultHeader`, allowing “焊点结果” or “拍照结果”.
- Product result is the PLC-collected value, not an aggregation of point results.
- Dynamic test-item columns are generated from the scheme details enabled for the relevant output channel.

## 四类开关语义

每个方案明细角色均遵循以下生效规则：

```text
Enable == false
    => do not collect; Save/Report/MES outputs are ineffective
Enable == true and SaveEnable
    => persist for center-server forwarding and server-side report generation
Enable == true and ReportEnable
    => include in equipment XLSX and upload the report file to MES
Enable == true and MesEnable
    => upload process parameters to MES and show in product-history preview
```

同一个角色可以同时启用多个输出通道；各通道都必须先通过公共采集门槛，再独立执行自己的筛选。

## 报表生成与上传

- Product completion refreshes the local report so that collected data is available during production.
- A task finish persists `EndTime` and final quantity statistics before the final report is regenerated and queued for MES upload.
- A report is still generated when no dynamic `ReportEnable` role exists; the fixed task and point fields remain available.
- Device and center-server XLSX files use the same visible template and Chinese headers.
- The center-server report uses `SaveEnable` dynamic columns, while the device report uses `ReportEnable` dynamic columns.

## 中心服务器协议

设备端与中心服务器同步升级。请求新增任务级表头数据、解析后的 `StationName`、任务 `StartTime`/`EndTime` 和最终统计值。

- Product-completion requests carry point rows and the current task snapshot; `EndTime` is empty before finish.
- A task-finish update carries no duplicate point rows and updates the existing work-order report header with the persisted `EndTime` and final statistics.
- Center reports are keyed by device and work-order identity; different work orders remain separate files.
- A work order without any `SaveEnable` role still forwards its public fields and point results to the center server.

## 工位设置与国际化

- Add two editable station-name settings with defaults “左” and “右”.
- Hide both inputs unless dual-station mode is enabled.
- When enabled, both names are required and must be distinct after trimming.
- Labels, hints, validation messages, and buttons are localized through the existing UI resource system.
- Station-name values remain user data and stay as entered; default values remain Chinese even when the UI is English.
- Excel output remains Chinese regardless of UI language.

## 历史数据

为 `BizWeldPointRecord` 增加独立的 `ProductResult` 字段，在采集时直接写入 PLC 产品结果。历史旧记录优先读取 `RawDataJson["product_result"]`；如果不存在，则显示为空/未知，绝不根据明细结果推算。

## 验证要求

- Unit/rule tests cover the four-channel effective-output matrix and the PLC-result source rule.
- Report tests verify exact header field sources, conditional station column, dynamic-column filtering, merge boundaries, and `yyyy-MM-dd HH:mm:ss` timestamps.
- Integration tests verify the center-server finish update changes only the work-order header and does not duplicate point rows.
- Rendered XLSX inspection compares sheet name, merged ranges, column order, fixed Chinese labels, and representative dual/single-station cases against the supplied template.
