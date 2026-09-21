# MonitorView 状态提示改进计划

## 问题分析

### 当前问题

**场景1：开工成功后配方校验失败**
```
当前行为：
1. 开工上报成功（数据已保存）
2. 配方编号校验失败（抛出异常）
3. 异常提示：工位1开工上报失败：配方编号校验失败  ❌ 误导

实际情况：
- 开工上报已成功
- 只是后续的配方编号校验失败

期望行为：
1. 运行状态提示：工位1开工上报成功  ✅
2. 异常提示：配方编号校验失败  ✅
```

**场景2：其他魔法字符串散落各处**
- 大量硬编码的中文字符串
- 无法国际化
- 难以统一管理

## 解决方案

### 第一步：分离开工上报和配方校验的错误提示

**修改点1：** `StartReport_Click` 方法
- 将配方校验从 `RunReportOperationAsync` 块中分离
- 开工上报成功后，独立处理配方校验异常

**代码结构：**
```csharp
await RunReportOperationAsync(stationNo, "开工上报", async () =>
{
    await _weldTaskService.StartAsync(...);
    SetRuntimeStatusText("开工上报成功", isSuccess: true);
});

// 独立处理配方校验，不影响上面的成功提示
try
{
    await WriteStartBusinessSignalsAsync(...);
}
catch (BusinessOperationException ex)
{
    SetRuntimeErrorText("配方编号校验失败");
    _exceptionLogService.WriteBusiness(...);
}
```

### 第二步：提取所有魔法字符串到资源文件

**需要提取的字符串类型：**

1. **工位操作相关**
   - "开工上报"
   - "完工上报"
   - "本地开工"
   - "本地完工"
   - "开工上报成功"
   - "完工上报成功"
   - "工位{0}{1}成功"
   - "工位{0}{1}失败：{2}"
   - "工位{0}{1}已禁用，当前窗口为只读看板"
   - "工位{0}{1}正在执行中，请稍后再试"

2. **配方编号相关**
   - "配方编号解析失败"
   - "配方编号下发失败"
   - "配方编号校验失败"
   - "配方编号校验通过：{0}"
   - "配方编号已下发：{0}"

3. **其他运行状态**
   - "加工程序已确认，本次开工将使用当前程序内容"
   - "工单信息已获取，请确认工序后点击开工上报"
   - "已选择工序：{0}"
   - "数据采集完成：焊点{0} {1}"

4. **流程日志**
   - "RecipeCodeResolveFailed" / "配方编号解析失败"
   - "RecipeCodeWriteFailed" / "配方编号写入失败"
   - "RecipeCodeValidationFailed" / "配方编号校验失败"
   - "RecipeCodeValidationSucceeded" / "配方编号校验通过"

### 第三步：创建 UiText.resx 资源文件

**文件结构：**
```
AutoWeldSystem.UI/
├── Localization/
│   ├── UiText.resx              # 默认（中文）
│   ├── UiText.en.resx           # 英文
│   └── UiText.Designer.cs       # 自动生成
```

**资源键命名规范：**
```
Monitor_Report_Start                → "开工上报"
Monitor_Report_Finish              → "完工上报"
Monitor_Report_StartSuccess        → "工位{0}开工上报成功"
Monitor_Report_StartFailed         → "工位{0}开工上报失败：{1}"
Monitor_Recipe_ValidationFailed    → "配方编号校验失败"
Monitor_Recipe_ValidationSuccess   → "配方编号校验通过：{0}"
Monitor_Recipe_WriteSuccess        → "配方编号已下发：{0}"
Monitor_Status_ProgramConfirmed    → "加工程序已确认，本次开工将使用当前程序内容"
Monitor_Status_WorkOrderReady      → "工单信息已获取，请确认工序后点击开工上报"
Monitor_Status_ProcessSelected     → "已选择工序：{0}"
Monitor_Status_DataCollected       → "数据采集完成：焊点{0} {1}"
Monitor_Error_StationDisabled      → "工位{0}{1}已禁用，当前窗口为只读看板"
Monitor_Error_StationBusy          → "工位{0}{1}正在执行中，请稍后再试"
```

## 实施步骤

### 阶段1：修复配方校验错误提示（优先）

1. 修改 `StartReport_Click` 和 `LocalStart_Click`
2. 分离配方校验异常处理
3. 确保提示准确

### 阶段2：创建资源文件

1. 创建 `Localization/UiText.resx`
2. 添加所有魔法字符串

### 阶段3：替换代码中的硬编码字符串

1. 逐个替换 MonitorView.cs 中的魔法字符串
2. 使用 `UiText.ResourceManager.GetString("key")` 或生成的属性

### 阶段4：验证和测试

1. 测试所有提示场景
2. 验证国际化切换
3. 确保无遗漏

## 关键代码修改预览

### 修改前（有问题）
```csharp
await RunReportOperationAsync(stationNo, "开工上报", async () =>
{
    await _weldTaskService.StartAsync(...);
    await WriteStartBusinessSignalsAsync(...); // 配方校验在这里，失败会显示"开工上报失败"
    SetRuntimeStatusText(BuildStationReportSuccessText(stationNo, "开工上报"), isSuccess: true);
});
```

### 修改后（正确）
```csharp
await RunReportOperationAsync(stationNo, UiText.Monitor_Report_Start, async () =>
{
    await _weldTaskService.StartAsync(...);
    SetRuntimeStatusText(
        string.Format(UiText.Monitor_Report_StartSuccess, stationNo), 
        isSuccess: true);
});

// 配方校验独立处理
try
{
    await WriteStartBusinessSignalsAsync(state.SelectedProgram, stationNo);
}
catch (BusinessOperationException ex) when (ex.SourceName == "PLC.RecipeCodeCheck")
{
    SetRuntimeErrorText(UiText.Monitor_Recipe_ValidationFailed);
    _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
}
```

## 预期效果

### 修复后的用户体验

**场景1：开工成功，配方校验失败**
```
运行状态区（绿色）：工位1开工上报成功
异常提示区（红色）：配方编号校验失败
```

**场景2：开工本身失败**
```
运行状态区（灰色）：空闲
异常提示区（红色）：工位1开工上报失败：[具体原因]
```

**场景3：切换到英文**
```
Running Status: Station 1 start report succeeded
Error Tips: Recipe code validation failed
```
