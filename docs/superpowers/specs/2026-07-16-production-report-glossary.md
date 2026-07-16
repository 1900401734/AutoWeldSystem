# 生产报表领域术语表

| 术语 | 含义 |
| --- | --- |
| 流转卡号/工单 | `BizWeldTask.SN`；一个任务对应一份报表。 |
| 工位 | 采集记录中的物理工位编号；只有启用双工位时才在报表中显示。 |
| 工位显示名称 | 用户为工位 1/2 配置的名称；默认值为“左”和“右”。 |
| 产品编号 | PLC 采集的 `BizWeldPointRecord.ProductNo`，用于标识工单下的产品。 |
| 焊点编号/点编号 | 来自 `TouchNo` 的点编号；为空时回退使用采集序号。 |
| 点结果 | PLC 采集的 `BizWeldPointRecord.TestResult`；显示标题可配置为“焊点结果”或“拍照结果”。 |
| 产品结果 | PLC 采集并保存到 `BizWeldPointRecord.ProductResult` 的结果，禁止根据点结果推算。 |
| Enable | 采集总开关；关闭后 Save/Report/MES 输出均不生效。 |
| SaveEnable | 中心服务器产品数据转发和服务器 XLSX 生成通道。 |
| ReportEnable | 设备端 XLSX 动态列和 MES 报表文件上传通道。 |
| MesEnable | MES 过程参数上传和产品历史预览通道。 |
| 开工时间 | 持久化任务的 `StartTime`，原样写入报表表头。 |
| 完工时间 | 持久化任务的 `EndTime`；完工前留空，完工后原样写入报表表头。 |
| 生产数量 | 工单计划数量 `StartAmount`，不按工位记录累加。 |
| 合格数量 | 任务最终统计值 `QualifiedQty`。 |
| 操作人员 | 开工操作员身份 `UserNumber`；完工沿用同一身份。 |
