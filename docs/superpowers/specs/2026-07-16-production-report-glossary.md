# 生产报表领域术语表

| Term | Meaning |
| --- | --- |
| 流转卡号/工单 | `BizWeldTask.SN`; one task and one report identity. |
| 工位 | Physical station number on collected records. Only visible in reports when dual-station mode is enabled. |
| 工位显示名称 | User-configured label for station 1 or 2; defaults to “左” and “右”. |
| 产品编号 | PLC-collected `BizWeldPointRecord.ProductNo`, identifying a product under one work order. |
| 焊点编号/点编号 | Point identifier from `TouchNo` or the collection sequence fallback. |
| 点结果 | PLC-collected `BizWeldPointRecord.TestResult`; display title is configurable as “焊点结果” or “拍照结果”. |
| 产品结果 | PLC-collected task/product result persisted as `BizWeldPointRecord.ProductResult`; never derived from point results. |
| Enable | Collection gate. If disabled, downstream Save/Report/MES outputs do not take effect. |
| SaveEnable | Persist/forward channel for center-server product data and server-side XLSX generation. |
| ReportEnable | Equipment-side XLSX dynamic-column channel and MES report-file upload channel. |
| MesEnable | MES process-parameter channel and product-history preview channel. |
| 开工时间 | Persisted task `StartTime`, used verbatim in the report header. |
| 完工时间 | Persisted task `EndTime`, blank until finish and then used verbatim in the report header. |
| 生产数量 | Work-order planned quantity `StartAmount`, not an aggregate of station rows. |
| 合格数量 | Final task statistic `QualifiedQty`. |
| 操作人员 | Start operator identity `UserNumber`; finish uses the same identity. |
