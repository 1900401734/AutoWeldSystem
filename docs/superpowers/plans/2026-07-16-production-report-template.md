# 生产报表模板与多通道输出实施计划

> **给代理开发者：** 实施本计划时，必须使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans`，并按任务逐项执行。每一步都使用复选框跟踪。

**目标：** 将设备端和中心服务器生产报表统一为客户 Excel 模板，并让 Enable/SaveEnable/ReportEnable/MesEnable 四类开关按已确认语义分别驱动采集、历史、MES 和中心服务器输出。

**架构：** 以方案明细角色规则作为唯一输出门槛；采集阶段保存 PLC 原始点结果和产品结果，报表、历史、MES 和中心服务器只按通道读取已采集值。设备端与中心服务器共同使用任务级表头、动态明细列和同一中文模板；中心服务器通过工单级完工更新补齐结束时间。

**技术栈：** .NET 8、C#、WinForms、SqlSugar Code First、ClosedXML、现有 `AutoWeldSystem.Tests` 控制台回归测试。

## 全局约束

- `Enable == false` 时不采集，Save/Report/MES 后续输出全部不生效。
- `ReportEnable` 只决定设备端报表动态列和 MES 报表文件上传。
- `MesEnable` 只决定 MES 过程参数上传和产品历史预览显示。
- `SaveEnable` 只决定中心服务器转发和服务器端报表动态列。
- 报表固定输出中文；软件界面新增文字必须进入 `UiText.resx` 与 `UiText.en.resx`。
- 开始时间只取 `BizWeldTask.StartTime`，结束时间只取 `BizWeldTask.EndTime`；完工前结束时间为空。
- 同一流转卡号/工单对应一个任务和一份报表；双工位同工单记录合并，不同工单分开。
- 不覆盖工作区已有的 `ProgramManageView.Designer.cs` 与 `SystemSettingView.Designer.cs` 未提交布局修改；新增 Designer 改动必须基于当前工作区合并。

---

### 任务 1：扩展采集记录并固化四通道规则

**文件：**
- 修改：`AutoWeldSystem.Core/Entities/BizWeldPointRecord.cs`
- 修改：`AutoWeldSystem.Core/Production/SchemeDetailRoleRules.cs`
- 修改：`AutoWeldSystem.Services/Production/ProductCycleCollectionService.cs`
- 修改：`AutoWeldSystem.Services/Production/ProductHistoryService.cs`
- 修改：`AutoWeldSystem.Services/Production/DataHistoryQueryService.cs`
- 测试：`AutoWeldSystem.Tests/Program.cs`

**接口：**
- 产生 `BizWeldPointRecord.ProductResult`，保存 PLC 产品结果。
- 保持 `BizWeldPointRecord.TestResult` 表示 PLC 焊点/拍照结果。
- `SchemeDetailRoleRules.ShouldShowHistoryRole` 改为 `Enable && MesEnable`。

- [ ] **步骤 1：先写失败测试**
  - 验证 `ProductResult` 存在并由采集快照写入。
  - 验证 `Enable=false` 时 Save/Report/MES 均不输出。
  - 验证 `MesEnable=true` 且 `SaveEnable=false` 时产品历史仍显示该角色。
  - 验证旧记录优先读取 `RawDataJson["product_result"]`，缺失时返回空/未知，不调用 `TestResultRules.ResolveProductResult`。
- [ ] **步骤 2：运行测试确认失败**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

预期：新增规则测试失败，现有历史规则仍按 `SaveEnable` 判断。

- [ ] **步骤 3：实现最小改动**
  - 在 `BizWeldPointRecord` 增加可空或默认空字符串的 `ProductResult` 字段，交由 SqlSugar Code First 同步列。
  - 在 `ProductCycleCollectionService` 将 PLC `ProductResultExpr` 的标准化值同时写入实体字段和 `RawDataJson`。
  - 在 `ProductHistoryService`、`DataHistoryQueryService` 使用实体字段；旧数据缺字段值时读取 JSON 回退。
  - 保留 `SchemeDetailRoleRules.ShouldPersistRole` 的联合持久化逻辑，但将历史显示规则改为 `MesEnable` 通道。
- [ ] **步骤 4：运行测试确认通过**
- [ ] **步骤 5：提交**

```powershell
git add AutoWeldSystem.Core\Entities\BizWeldPointRecord.cs AutoWeldSystem.Core\Production\SchemeDetailRoleRules.cs AutoWeldSystem.Services\Production\ProductCycleCollectionService.cs AutoWeldSystem.Services\Production\ProductHistoryService.cs AutoWeldSystem.Services\Production\DataHistoryQueryService.cs AutoWeldSystem.Tests\Program.cs
git commit -m "fix(report): persist PLC product results and align output gates"
```

### 任务 2：增加双工位显示名称配置和国际化

**文件：**
- 修改：`AutoWeldSystem.Core/Entities/AppSettings.cs`
- 修改：`AutoWeldSystem.Services/AppSettingsService.cs`
- 修改：`AutoWeldSystem.Core/Constants/TextKeys.cs`
- 修改：`AutoWeldSystem.Core/Localization/UiText.resx`
- 修改：`AutoWeldSystem.Core/Localization/UiText.en.resx`
- 修改：`AutoWeldSystem.UI/Views/SystemSettingView.cs`
- 修改：`AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs`
- 测试：`AutoWeldSystem.Tests/Program.cs`

**接口：**
- `AppSettings.Station1DisplayName` 默认“左”。
- `AppSettings.Station2DisplayName` 默认“右”。
- 新增 `StationDisplayNameRules.NormalizeAndValidate(bool dualStationEnabled, string station1, string station2)`，返回规范化名称或抛出明确校验错误。

- [ ] **步骤 1：先写失败测试**
  - 验证默认值为“左/右”。
  - 验证未启用双工位时界面不显示两个输入框。
  - 验证启用双工位后空名称、重复名称、首尾空格均按规则处理。
  - 验证中英文资源均存在新增标签、提示和校验消息。
- [ ] **步骤 2：运行测试确认失败**
- [ ] **步骤 3：实现最小改动**
  - 将两个配置字段加入 `AppSettingsService` 的规范化/加载保存流程。
  - 在 Designer 中增加两个工位名称输入行；只使用 Designer 文件承载控件初始化和布局。
  - 在 `SystemSettingView.cs` 中根据 `EnableDualStation` 设置输入区可见性，保存前执行非空且不重复校验。
  - 使用现有本地化模式设置标签、提示、按钮和验证消息；输入值始终保留用户输入，不随 UI 语言翻译。
- [ ] **步骤 4：运行测试确认通过**
- [ ] **步骤 5：提交**

```powershell
git add AutoWeldSystem.Core\Entities\AppSettings.cs AutoWeldSystem.Services\AppSettingsService.cs AutoWeldSystem.Core\Constants\TextKeys.cs AutoWeldSystem.Core\Localization\UiText.resx AutoWeldSystem.Core\Localization\UiText.en.resx AutoWeldSystem.UI\Views\SystemSettingView.cs AutoWeldSystem.UI\Views\SystemSettingView.Designer.cs AutoWeldSystem.Tests\Program.cs
git commit -m "feat(settings): add localized dual-station display mapping"
```

### 任务 3：重构设备端生产报表为模板格式

**文件：**
- 修改：`AutoWeldSystem.Services/Production/ProductionReportFileService.cs`
- 修改：`AutoWeldSystem.Services/Production/ProductCycleCollectionService.cs`
- 修改：`AutoWeldSystem.Services/Production/WeldTaskService.cs`
- 修改：`AutoWeldSystem.Services/Production/UploadTaskService.cs`
- 修改：`AutoWeldSystem.Core/Center/CenterProductReportFormat.cs`
- 测试：`AutoWeldSystem.Tests/Program.cs`

**接口：**
- 报表生成内部入口继续使用 `GenerateXlsxReport(BizWeldTask task)`，但方法内必须按 `TaskId` 读取最新任务。
- 新增内部模板写入方法，分别负责表头、明细、动态列和合并范围，避免把任务字段重复写入每个明细行。

- [ ] **步骤 1：先写失败测试**
  - 读取生成的 XLSX，验证工作表名为“生产报表”。
  - 验证表头字段来源、时间格式、备注为空、生产数量来自 `StartAmount`、合格数量来自 `QualifiedQty`、操作人员来自 `UserNumber`。
  - 验证单工位不生成工位列，双工位生成工位列并使用配置名称。
  - 验证 `ReportEnable` 动态列存在，`SaveEnable`/`MesEnable` 独占字段不进入设备报表。
  - 验证产品结果读取 `ProductResult`，点结果读取 `TestResult`，不调用聚合计算。
  - 验证开始/结束时间与任务持久化值完全一致，未完工时结束时间为空。
  - 验证产品/工位公共明细字段的合并范围。
- [ ] **步骤 2：运行测试确认失败**
- [ ] **步骤 3：实现最小改动**
  - 将表头写入模板式多行区域，按实际列数动态扩展合并区。
  - 动态明细列只调用 `SchemeDetailRoleRules.ShouldWriteReportRole`，并保持配置标题；`PointResultHeader` 作为焊点/拍照结果标题。
  - `MergeRepeatedProductFields` 改为按“工位（启用时）+产品编号”分组；产品结果直接取 PLC 字段。
  - 保持产品完成后的增量刷新；完工流程先持久化任务 `EndTime` 和统计，再重新生成最终报表并进入现有 MES 报表上传任务。
- [ ] **步骤 4：运行测试确认通过并用模板文件做视觉比对**
- [ ] **步骤 5：提交**

```powershell
git add AutoWeldSystem.Services\Production\ProductionReportFileService.cs AutoWeldSystem.Services\Production\ProductCycleCollectionService.cs AutoWeldSystem.Services\Production\WeldTaskService.cs AutoWeldSystem.Services\Production\UploadTaskService.cs AutoWeldSystem.Core\Center\CenterProductReportFormat.cs AutoWeldSystem.Tests\Program.cs
git commit -m "feat(report): generate customer production template"
```

### 任务 4：按 SaveEnable 重构中心服务器报表与完工更新

**文件：**
- 修改：`AutoWeldSystem.Core/DTOs/CenterServer/CenterProductReportRequest.cs`
- 修改：`AutoWeldSystem.Services/Center/CenterProductForwardingService.cs`
- 修改：`AutoWeldSystem.Services/Center/CenterTelemetryClient.cs`
- 修改：`AutoWeldSystem.CenterServer/Services/CenterProductReportIngestService.cs`
- 修改：`AutoWeldSystem.CenterServer/Program.cs`
- 测试：`AutoWeldSystem.Tests/Program.cs`

**接口：**
- `CenterProductReportRequest` 增加：`StationName`、`ProductResult`、`StartTime`、`EndTime`、`QualifiedQty`、`IsTaskFinishUpdate`。
- 产品完成请求携带点明细；工单完工更新只携带任务级字段和 `IsTaskFinishUpdate=true`，不重复携带点明细。
- 中心服务器按“设备编号 + 流转卡号”定位报表文件，并在完工更新时只更新表头统计。

- [ ] **步骤 1：先写失败测试**
  - 验证 `SaveEnable` 动态列进入中心请求，`ReportEnable`/`MesEnable` 独占字段不进入。
  - 验证单工位请求不要求 `StationName`，双工位请求携带解析后的名称。
  - 验证产品请求先生成空结束时间报表，完工更新后写入任务 `EndTime` 和最终 `QualifiedQty`。
  - 验证完工更新不重复添加产品点行。
  - 验证没有 `SaveEnable` 动态项时仍转发公共字段和点结果。
- [ ] **步骤 2：运行测试确认失败**
- [ ] **步骤 3：实现最小改动**
  - 扩展 DTO 和 JSON 请求构造，复用当前上传任务队列保证断网重试。
  - `BuildDynamicReportColumns` 改用 `ShouldPersistRole && IsSaveEnabled` 的明确 SaveEnable 过滤。
  - 中心服务器将可见表头和内部数据页分离，保留幂等产品替换逻辑；完工更新只刷新任务级表头。
  - 在工单完工流程加入中心服务器完工更新任务，确保设备端已保存 `EndTime` 后再入队。
- [ ] **步骤 4：运行测试确认通过**
- [ ] **步骤 5：提交**

```powershell
git add AutoWeldSystem.Core\DTOs\CenterServer\CenterProductReportRequest.cs AutoWeldSystem.Services\Center\CenterProductForwardingService.cs AutoWeldSystem.Services\Center\CenterTelemetryClient.cs AutoWeldSystem.CenterServer\Services\CenterProductReportIngestService.cs AutoWeldSystem.CenterServer\Program.cs AutoWeldSystem.Tests\Program.cs
git commit -m "feat(center): generate synchronized work-order reports"
```

### 任务 5：端到端验证与模板视觉回归

**文件：**
- 修改：`AutoWeldSystem.Tests/Program.cs`
- 检查：`E:/Desktop/YC Projects/03 CASC/01 Docs/过程文档/报表格式.xlsx`

- [ ] **步骤 1：增加代表性测试数据**
  - 单工位点焊设备：结果标题“焊点结果”，仅 ReportEnable 动态列。
  - 双工位检测设备：结果标题“拍照结果”，同一任务包含工位 1/2，产品结果来自 PLC 字段。
  - 不同工单：验证文件路径和报表互不覆盖。
  - 未完工任务：结束时间为空；完工后严格等于任务 `EndTime`。
- [ ] **步骤 2：运行完整回归测试**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

预期：全部测试通过。

- [ ] **步骤 3：运行隔离输出目录构建**

```powershell
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

预期：`0 Warning(s)`、`0 Error(s)`。

- [ ] **步骤 4：打开生成的 XLSX 做视觉检查**
  - 检查工作表名称、中文表头、合并单元格、列顺序、边框和列宽。
  - 对照模板检查单工位和双工位两种布局。
  - 检查中心服务器生成的文件与设备端可见格式一致。

- [ ] **步骤 5：提交验证结果文档**

```powershell
git add AutoWeldSystem.Tests\Program.cs docs\superpowers\specs\2026-07-16-production-report-template-design.md docs\superpowers\specs\2026-07-16-production-report-glossary.md
git commit -m "test(report): verify production report template"
```
