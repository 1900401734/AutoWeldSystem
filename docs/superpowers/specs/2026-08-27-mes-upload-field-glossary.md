# MES 上报字段术语表

本表覆盖设备向 MES 上报时的请求字段语义。字段名称与 `AutoWeldSystem.Core/DTOs/Mes/Request` 下的 DTO 属性一一对应。

## 完工上报（ExperimentEndReq）

| 字段 | 含义 |
| --- | --- |
| ExpStartId | 开工上报接口返回的 MES 任务 ID；离线任务在开工补传成功后才有值，补传完工必须等它就位。 |
| DeviceId | 设备编号，取任务落库时的 `BizWeldTask.DeviceId`。 |
| SN | 流转卡号/工单号，对应 `BizWeldTask.SN`。 |
| ProcessNo | 工序号，开工时校验非空。 |
| EndTs | 完工时间，格式 `yyyy-MM-dd HH:mm:ss`；与本地任务 `EndTime` 共享同一时间戳，避免报表与 MES 出现毫秒级漂移。 |
| EndExperID | 完工人员员工号；沿用开工时录入的员工号，不接受登录账号兜底。 |
| ExpStatus | 工单状态：`-1` 异常、`0` 开工、`1` 完工、`2` 暂停。完工上报固定为 `1`。 |
| WorkHour | 实际工作时长，单位为小时。由 `MesWorkHourRules` 按开工到完工区间计算，保留两位小数并定标（整小时也输出 `1.00`），第三位按四舍五入进位（`AwayFromZero`）。不足 0.01 小时（约 18 秒）的任务上报 `0.00`，不抬升为 `0.01`；结束时间早于开工时间回退为 `0.00`，不出现负工时。 |
| ExpQty | 实际生产数量，取任务最终统计 `ActualQty`。 |
| QualifyNumber | 合格数量，取任务最终统计 `QualifiedQty`。 |
| FailureNumber | 失效数量，取任务最终统计 `FailedQty`。 |

## 相关约定

- 完工上报有三条构造路径：在线完工即时请求、离线完工写入补传 payload、补传前对缺失工时的重算。三条路径共用同一套字段规则。
- 离线完工的请求会被序列化进 `BizUploadTask.PayloadJson` 持久化，因此字段规则变更不会自动作用于已入库的历史 payload。
