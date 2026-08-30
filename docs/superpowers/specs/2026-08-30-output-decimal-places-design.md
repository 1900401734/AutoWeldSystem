# 报表与过程参数的输出小数位设计

## 背景

小数位此前只有一个来源：测试项偏移量表达式里的 `_小数位`（如 `14:F-0_2`）。`ExpressionReadService.FormatNumericValue` 在**采集时**就按它格式化，存进 `RawDataJson` 的已经是成品字符串；报表和 MES 过程参数都直接取这个存量值。三处口径被绑死，现场无法让报表和过程参数各用各的位数。

本次给报表和过程参数各加一个全局小数位设置，在输出端再格式化一次。截断还是四舍五入沿用系统设置里已有的「PLC 数值格式处理方式」，不新增模式选项。

## 目标

- 报表和过程参数可分别指定输出小数位，互不影响。
- 留空表示沿用采集位数，升级后不配置则行为与之前完全一致。
- 数值处理模式复用现有全局设置，不引入第二套模式配置。

## 非目标

- 不改采集与存储，不动 `ExpressionReadService` 和偏移量表达式语义。
- 不改界面显示位数（实时预览、历史数据仍按采集位数显示）。
- 不做中心端报表（`AutoWeldSystem.CenterServer`）。它从设备端转发的逐面原始数据自行生成，与设备端 XLSX 是两条链路，纳入会牵扯转发协议和中心端列定义。

## 两条使用限制

1. **输出位数不能超过采集精度。** 采集按 `_2` 把 15.8834 存成 `15.88`，报表配 3 位只能得到 `15.880`，不是 `15.883`。适用场景是减位或统一对齐位数；要更高精度须改测试项表达式的小数位。
2. **存在二次舍入。** 采集舍一次、输出再舍一次，与直接从原始值一次舍入可能差一个末位（15.845 → `15.85` → `15.9`，而一次舍到 1 位是 `15.8`）。这是「只在输出端调整」路线的固有代价，已与用户确认接受。

## 设计

### 配置

`AppSettings` 新增 `ReportDecimalPlaces` 和 `ProcessParameterDecimalPlaces`（均为 `int?`）。`null` = 不调整。取值 0–`PlcOffsetExpression.MaxDecimalPlaces`（10），负数按 `null` 处理，超上限收敛到上限。

`PlcOffsetExpression.MaxDecimalPlaces` 由 `private` 改为 `public`，让设置校验与表达式解析共用同一个上限，不再各写一个字面量。

### OutputNumericFormat

新增 `AutoWeldSystem.Core/Plc/OutputNumericFormat.cs`，一个 `readonly record struct`，承载「小数位 + 模式」并提供 `Apply(value)`。它不实现格式化算法，内部转调既有的 `PlcStringNumericFormatter.Format`。

存在的理由是消除重复的派生逻辑：从 `AppSettings` 推出格式时要处理「`EnablePlcStringNumericFormatting` 关闭时按 `Round`」这条与采集侧 `ExpressionReadService.FormatNumericValue` 对齐的约定。若让报表和 MES 各写一遍，两边迟早不一致。`ForReport` / `ForProcessParameter` 两个工厂把这条约定固定在一处，`NormalizeDecimalPlaces` 也放在这里，与使用处同源且可被回归测试直接覆盖。

非数值文本（`OK`/`NG`、报表里表示不适用的 `\`、空值）由 `PlcStringNumericFormatter` 原样返回，因此结果列和不适用单元格天然安全，不需要额外分支。

### 两个插入点

都选在链路汇聚处，各改一处覆盖两条路径：

- **MES 过程参数** —— `UploadTaskService.FormatMesRoleValue`。A/B 聚合路径和普通路径都汇聚到它，实际值、上限、下限一并处理。顺序是**先按小数位格式化再拼单位**：拼了单位就不再是纯数值文本，格式化器会原样返回。小数位从 `UploadProcessParameterGroupAsync` 沿调用链透传，因为该方法链是 `static` 的。
- **XLSX 报表** —— `ProductionReportFileService.WriteDataRows` 中写 `DynamicValues` 的那一步。报表有两条取值路径（A/B 走 `BuildAbReportValues`，普通走 `AddSchemeDynamicValues`），但都在这里汇合。固定列（工位、产品编号、面号、面结果、产品结果）在字典初始化时已写好，不经过这个循环，天然不受影响。

设置来源用服务已有的 `_currentSettings` 快照而非 `_settingsService.Get()`：现有报表回归测试用 `GetUninitializedObject` 构造服务，`_settingsService` 在那条路径上为 null。

### 界面

系统设置页 PLC 一组新增两个输入框，用 `AntdUI.Input` 而非 `InputNumber`——后者无法表达「不配置」。留空是合法输入，0–10 之外给出提示而不是静默截断。

## 同批附带的界面重构

同一分支上还做了一次系统设置页的信息架构调整（由用户在设计器中完成）：

- 「启用PLC报警读取」与「报警触发模式」合并到一行，「启用PLC字符串数值处理」与「处理方式」合并到一行。复选框本身充当说明文字，两个独立标签控件连同其 `TextKeys` 常量和本地化赋值一并移除。
- 过程参数相关设置从 MES 配置组移到生产配置组，`tlpProcessParameterType` 的父容器由 `tableLayoutPanelMesConfig` 改为 `tlpProductConfig`；「检测结果来源」「A/B配对聚合方式」从该容器内部提升为生产配置组的独立行。

**随之修复的布局退化**：设计器把 `tlpProductConfig` 承载可隐藏行的三行（检测结果来源、A/B配对聚合方式、整件检测开关容器）的 `AutoSize` 实测高度固化成了 `Absolute, 40F`。后果是这些行按设备类型隐藏后不再折叠，留下等高空洞，且整件检测下多行内容会被裁到 40px。已改回 `AutoSize`。这类退化不会导致编译失败，只能靠回归断言和目视确认发现。

原断言检查的是 `tableLayoutPanelMesConfig`，因过程参数已移出 MES 组而失去意义，改为检查 `tlpProductConfig` 的 AutoSize 行数，并新增 `CountDesignerAutoSizeRows` 辅助方法（兼容 `new RowStyle()` 与 `new RowStyle(SizeType.AutoSize)` 两种等价写法）。

## 测试与验收

回归用例 `Output decimal places apply to report and process parameter` 覆盖：

- 留空时与改动前完全一致——升级不改变现场行为的保证。
- 减位：`15.88` 配 1 位，截断得 `15.8`、四舍五入得 `15.9`。
- 关闭全局数值处理时按四舍五入，与采集口径一致。
- 增位补零：`15.88` 配 3 位得 `15.880`。
- 非数值原样返回：`OK`、`\`、空值。
- 报表与过程参数两个设置互相独立。
- 归一化边界：负数按未配置、超上限收敛、0 位合法。
- 两个插入点的位置断言，以及「先格式化再拼单位」的顺序断言。

`Process parameter numeric roles append test item units` 补充了配置小数位后仍先格式化再拼单位、结果字段不被改写两条。

`System setting view uses responsive semantic columns` 的折叠断言按新信息架构更新。

命令：

```bash
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=../artifacts/verify-bin/
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

## 实施结果

- 构建成功，0 警告 0 错误（独立输出目录，避免默认输出被运行中的程序锁定）。
- 控制台回归 harness：434 项全部 PASS。
- `git diff --check` 无空白错误。
- 两份 `UiText*.resx` 的大段改动经核对为 VS 的格式重排（单行 `<data>` 展开为多行），条目本身只删除了随控件移除而失去引用的 `system.label.plc_format_mode` 和 `system.label.plc_alarm_trigger_mode`。

未执行的验证：导出真实 XLSX 报表、触发真实 MES 过程参数上传核对位数，以及系统设置页重构后的目视确认，均依赖现场设备或人工操作，本地无法替代。

## 范围

- `AutoWeldSystem.Core/Plc/OutputNumericFormat.cs`（新增）、`PlcOffsetExpression.cs`
- `AutoWeldSystem.Core/Entities/AppSettings.cs`、`Constants/TextKeys.cs`、`Localization/UiText*.resx`
- `AutoWeldSystem.Services/AppSettingsService.cs`、`Production/UploadTaskService.cs`、`Production/ProductionReportFileService.cs`
- `AutoWeldSystem.UI/Views/SystemSettingView.cs` 及其 Designer
- `AutoWeldSystem.Tests/Program.cs`、`Directory.Build.props`、`README.md`
