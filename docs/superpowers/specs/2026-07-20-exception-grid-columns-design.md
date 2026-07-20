# 异常日志表格列精简设计

## 背景

程序异常日志表格 `dgvExceptionLogs` 当前显示 `Source` 和 `SourceLocation` 两列。
这两项信息已在选中日志后的 `txtExceptionBasicInfo` 中完整展示，表格重复显示会挤占异常消息列的可视空间。

## 目标

- 从 `dgvExceptionLogs` 完全移除 `Source` 和 `SourceLocation` 两列。
- 保留日志实体字段和详情面板中的 `Source`、`SourceFile`、`SourceMember` 信息。
- 不改变日志落盘格式、筛选逻辑、选中行行为或其他日志页。
- 本次小范围优化将版本从 `1.0.2` 调整为 `1.0.3`，同步程序集和文件版本为 `1.0.3.0`。

## 方案

直接删除两列的 Designer 字段、实例化、配置和 `Columns.Add` 注册；同时删除异常行视图模型中仅为这两列提供的绑定属性，以及对应的表头赋值。
保留 `ProgramExceptionLogEntry` 的来源字段和 `BuildExceptionBasicInfo` 的详情输出，因此不会丢失诊断信息。

不采用“仅设置 `Visible = false`”：该方式仍保留无用列和绑定维护成本，也可能被 Designer 重新显示。

## 验证

- 回归测试检查异常表格不再声明或注册这两列。
- 回归测试检查基本信息构建仍输出 `Source`、`SourceFile` 和 `SourceMember`。
- 执行 `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`。
- 执行 `dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\\artifacts\\verify-bin\\`。

## 范围

本次只涉及 `LogManageView` 的异常日志表格、对应测试、版本属性和本设计说明；不处理当前工作区已有的其他 Designer 或文档改动。
