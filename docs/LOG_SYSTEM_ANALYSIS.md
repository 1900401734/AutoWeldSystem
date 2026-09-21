# AutoWeldSystem 日志系统全面分析

## 一、日志系统架构概览

### 1.1 四大日志服务

| 服务名称 | 接口 | 实现类 | 用途 |
|---------|------|--------|------|
| **程序异常日志** | `IProgramExceptionLogService` | `ProgramExceptionLogService` | 捕获程序运行时异常和业务异常 |
| **生产流程日志** | `IProductionFlowLogService` | `ProductionFlowLogService` | 记录生产关键步骤（配方下发、数据采集等）|
| **MES 交互日志** | `IMesInteractionLogService` | `MesInteractionLogService` | 记录与MES系统的HTTP交互 |
| **操作日志** | `IOperationLogService` | `OperationLogService` | 记录用户操作（登录、配置变更等）|

### 1.2 项目结构

```
AutoWeldSystem.Core/
├── Interfaces/
│   ├── IProgramExceptionLogService.cs
│   ├── IProductionFlowLogService.cs
│   ├── IMesInteractionLogService.cs
│   └── IOperationLogService.cs
├── ViewModels/
│   ├── ProgramExceptionLogEntry.cs      # 异常日志条目
│   ├── ProductionFlowLogEntry.cs        # 生产流程日志条目
│   └── MesInteractionLogEntry.cs        # MES交互日志条目
└── DTOs/
    └── ProgramExceptionLogEntry.cs      # (别名)

AutoWeldSystem.Services/
└── Log/
    ├── ProgramExceptionLogService.cs
    ├── ProductionFlowLogService.cs
    ├── MesInteractionLogService.cs
    └── OperationLogService.cs

AutoWeldSystem.UI/
└── Views/
    └── LogManageView.cs                 # 日志展示界面
```

---

## 二、各日志服务详解

### 2.1 程序异常日志（ProgramExceptionLogService）

**接口方法：**
```csharp
// 记录异常对象
ProgramExceptionLogEntry Write(Exception exception, string source, string? context = null);

// 记录业务异常（预期的错误）
ProgramExceptionLogEntry WriteBusiness(
    string source,
    string message,
    string detail,
    string? context = null,
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = 0,
    [CallerMemberName] string sourceMemberName = "");

// 读取日志
IReadOnlyList<ProgramExceptionLogEntry> GetByDate(DateTime date, int take = 500);
```

**日志条目字段：**
- TraceId - 唯一标识
- OccurredTime - 发生时间
- Category - 类别（Business / Program）
- Severity - 严重级别
- Source - 异常来源
- Message - 异常消息
- ExceptionType - 异常类型
- StackTrace - 堆栈跟踪
- Context - 上下文信息
- SourceFilePath / SourceLineNumber - 源代码位置

**使用场景：**
- MonitorView 中捕获各种异常
- 配方校验失败
- PLC/MES通信异常
- 数据保存失败
- 意外的程序错误

**示例：**
```csharp
// 捕获异常
_exceptionLogService.Write(ex, "MonitorView.StartReport");

// 业务异常
_exceptionLogService.WriteBusiness(
    "PLC.RecipeCode", 
    "配方编号校验失败", 
    $"PC={pc}; PLC={plc}");
```

---

### 2.2 生产流程日志（ProductionFlowLogService）

**接口方法：**
```csharp
void Write(
    string step,              // 步骤名称（如 "RecipeCodeWriteSucceeded"）
    string summary,           // 摘要（如 "配方编号已下发"）
    string detail = "",       // 详细信息
    string level = "Info",    // 级别（Info/Warning/Error）
    int stationNo = 0,        // 工位号
    string workOrderId = "",  // 工单号
    string productNo = "",    // 产品编号
    string programId = "",    // 程序ID
    string plcSignal = "",    // PLC信号名称
    string plcAddress = "",   // PLC地址
    long? durationMilliseconds = null);  // 耗时
```

**日志条目字段：**
- TraceId - 追踪ID
- OccurredTime - 发生时间
- Level - 级别
- Step - 步骤标识
- Summary - 摘要
- **Detail - 详细信息（包含大量键值对）**
- StationNo - 工位号
- WorkOrder - 工单号
- ProductNo - 产品编号
- ProgramId - 程序ID
- PlcSignal - PLC信号
- PlcAddress - PLC地址
- DurationMilliseconds - 耗时

**使用场景（MonitorView）：**

通过 `WriteRecipeFlowLog` 方法记录配方相关流程：

1. **RecipeCodeResolveFailed** - 配方编号解析失败
2. **RecipeCodeWriteStarted** - 配方编号准备下发
3. **RecipeCodeWriteFailed** - 配方编号下发失败
4. **RecipeCodeWriteSucceeded** - 配方编号已下发
5. **RecipeCodeValidationFailed** - 配方编号校验失败
6. **RecipeCodeValidationSucceeded** - 配方编号校验通过

**Detail 字段内容示例：**
```
ProgramId; ProgramId=05898390c52e4dd0987c0ab431528ac2; LocalProgramMatched=True; LocalProgramId=15; RecipeCodePresent=True; ExpStartId=baa36b5dd6c641bd92a21723c06dac96; RecipeCode=1
```

**问题点：**
- Detail 字段内容过长，在日志界面中堆在一行
- 缺少换行，难以阅读
- 键值对格式不统一（有分号分隔，有等号赋值）

---

### 2.3 MES 交互日志（MesInteractionLogService）

**接口方法：**
```csharp
void Write(MesInteractionLogEntry entry);
IReadOnlyList<MesInteractionLogEntry> GetByDate(DateTime date, int take = 500);
```

**日志条目字段：**
- TraceId
- SendTime - 发送时间
- Purpose - 请求目的
- Method - HTTP方法
- Url - 请求URL
- RequestBody - 请求体（JSON）
- ResponseBody - 响应体（JSON）
- HttpStatusCode - HTTP状态码
- MesStatusCode - MES业务状态码
- MesMessage - MES返回消息
- IsSuccess - 是否成功
- DurationMilliseconds - 耗时

**使用场景：**
- MesProvider 中自动记录所有HTTP请求
- 开工上报、完工上报、工单查询等

---

### 2.4 操作日志（OperationLogService）

**接口方法：**
```csharp
void Write(string action, string detail, string level = "Info");
IReadOnlyList<SysOperationLog> GetRecent(int take = 200);
```

**存储：**
- 数据库表：`SysOperationLog`

**使用场景：**
- 用户登录/登出
- 系统配置变更
- 权限操作

---

## 三、日志展示界面（LogManageView）

### 3.1 界面结构

```
LogManageView (Tabs)
├── MES交互日志 Tab
│   ├── 数据网格 (dgvMesLogs)
│   └── 详情面板
│       ├── 基本信息
│       ├── 请求体 (JSON)
│       └── 响应体 (JSON)
├── 生产流程日志 Tab
│   ├── 数据网格 (dgvProductionLogs)
│   └── 详情面板
│       ├── 基本信息
│       └── 详细信息 (txtProductionDetail)  ⚠️ 问题区域
├── 程序异常日志 Tab
│   ├── 数据网格 (dgvExceptionLogs)
│   └── 详情面板
│       ├── 基本信息
│       ├── 堆栈跟踪
│       └── 上下文
└── 设备状态日志 Tab
    └── (略)
```

### 3.2 生产流程日志展示

**数据网格列：**
- 时间 (OccurredTime)
- 级别 (Level)
- 步骤 (Step)
- 摘要 (Summary)
- 工位 (StationNo)
- 工单 (WorkOrder)
- 产品 (ProductNo)
- 程序 (ProgramId)
- 信号 (PlcSignal)

**详情面板：**
```csharp
txtProductionBasicInfo.Text = BuildProductionBasicInfo(entry);
txtProductionDetail.Text = entry.Detail;  // ⚠️ 直接显示 Detail，无格式化
```

**当前问题：**
`txtProductionDetail` 直接显示原始 Detail 字符串，导致：
- 一行显示所有键值对
- 缺少换行，难以阅读
- 界面相对较小，横向滚动不便

---

## 四、配方下发日志详解

### 4.1 WriteRecipeFlowLog 方法

**位置：** `MonitorView.cs` (约第5431行)

```csharp
private void WriteRecipeFlowLog(
    string step,
    string summary,
    string detail,
    int stationNo,
    string level = "Info",
    string plcSignal = "",
    string plcAddress = "")
{
    var state = GetCurrentStationState();
    _productionLogService.Write(
        step,
        summary,
        detail,
        level,
        stationNo,
        state.ActiveTask?.WorkOrderId ?? inputSN.Text,
        productNo: string.Empty,
        programId: state.SelectedProgram?.Id ?? string.Empty,
        plcSignal: string.IsNullOrWhiteSpace(plcSignal) 
            ? AppConstants.PlcLogicalKeys.PcRecipeCode 
            : plcSignal,
        plcAddress: plcAddress);
}
```

### 4.2 调用位置汇总

| 调用位置 | Step | Summary | Detail 内容 |
|---------|------|---------|------------|
| 第5014行 | RecipeCodeResolveFailed | 配方编号解析失败 | `{resolution.Source}; {resolution.Detail}` |
| 第5031行 | RecipeCodeWriteStarted | 配方编号准备下发 | `{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}` |
| 第5046行 | RecipeCodeWriteFailed | 配方编号下发失败 | `{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}; Detail={writeResult.Message}` |
| 第5060行 | RecipeCodeWriteSucceeded | 配方编号已下发 | `{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}; ValidateRecipe=false` |
| 第5076行 | RecipeCodeValidationFailed | 配方编号校验失败 | `{resolution.Source}; {resolution.Detail}; PC={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}; Detail={syncResult.Message}` |
| 第5089行 | RecipeCodeValidationSucceeded | 配方编号校验通过 | `{resolution.Source}; {resolution.Detail}; RecipeCode={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}` |

### 4.3 Detail 内容来源

**resolution.Source 和 resolution.Detail 来自：**

```csharp
// 在 DispatchRecipeCodeAfterStartAsync 方法中
var resolution = ResolveRecipeCodeForStartedTask(task, selectedProgram);

// ResolveRecipeCodeForStartedTask 返回 RecipeCodeResolution 对象
public class RecipeCodeResolution
{
    public string RecipeCode { get; set; }
    public string Source { get; set; }      // 来源标识
    public string Detail { get; set; }      // 详细信息（键值对）
}
```

**典型的 Detail 内容示例：**
```
ProgramId; ProgramId=05898390c52e4dd0987c0ab431528ac2; LocalProgramMatched=True; LocalProgramId=15; RecipeCodePresent=True; ExpStartId=baa36b5dd6c641bd92a21723c06dac96; RecipeCode=1
```

**格式分析：**
- 使用分号 `;` 分隔
- 键值对使用 `Key=Value` 格式
- 第一个元素通常是来源标识（如 `ProgramId`）
- 包含大量调试信息（程序ID、匹配状态、实验ID等）

---

## 五、问题总结

### 5.1 日志服务混乱点

1. **四大日志服务职责有重叠**
   - `ProgramExceptionLogService` 既记录程序异常，又记录业务异常
   - `ProductionFlowLogService` 记录生产流程，但也包含错误日志
   - 缺少统一的日志级别定义

2. **日志接口不统一**
   - `Write` 方法参数差异大
   - 有的返回日志对象，有的返回 void
   - 事件命名不一致

3. **Detail 字段格式不规范**
   - 手动拼接字符串
   - 分号和等号混用
   - 缺少结构化

### 5.2 日志展示问题

1. **生产流程日志 Detail 字段展示**
   - **问题：** 一行显示，内容过长
   - **位置：** `LogManageView.ShowProductionLogDetails` (第771行)
   - **当前代码：** `txtProductionDetail.Text = entry.Detail;`
   - **影响：** 用户难以阅读，需要横向滚动

2. **界面空间限制**
   - 详情面板相对较小
   - 没有自动换行
   - 没有格式化

### 5.3 配方下发日志特殊问题

**Detail 字段内容示例：**
```
ProgramId; ProgramId=05898390c52e4dd0987c0ab431528ac2; LocalProgramMatched=True; LocalProgramId=15; RecipeCodePresent=True; ExpStartId=baa36b5dd6c641bd92a21723c06dac96; RecipeCode=1
```

**问题：**
1. 所有信息堆在一行
2. 键值对之间没有视觉分隔
3. 难以快速定位关键信息（如 RecipeCode）

---

## 六、改进建议

### 6.1 短期改进（立即可做）

**1. 格式化 Detail 显示**

在 `LogManageView.ShowProductionLogDetails` 方法中：

```csharp
private void ShowProductionLogDetails(ProductionFlowLogEntry? entry)
{
    if (entry is null)
    {
        txtProductionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoSelection);
        txtProductionDetail.Clear();
        return;
    }

    txtProductionBasicInfo.Text = BuildProductionBasicInfo(entry);
    
    // 格式化 Detail 字段，将分号替换为换行
    txtProductionDetail.Text = FormatProductionDetail(entry.Detail);
}

private string FormatProductionDetail(string detail)
{
    if (string.IsNullOrWhiteSpace(detail))
    {
        return string.Empty;
    }

    // 将分号分隔的键值对换行显示
    return detail.Replace("; ", Environment.NewLine);
}
```

**效果：**
```
// 修改前（一行）
ProgramId; ProgramId=05898390c52e4dd0987c0ab431528ac2; LocalProgramMatched=True; LocalProgramId=15; RecipeCodePresent=True; ExpStartId=baa36b5dd6c641bd92a21723c06dac96; RecipeCode=1

// 修改后（多行）
ProgramId
ProgramId=05898390c52e4dd0987c0ab431528ac2
LocalProgramMatched=True
LocalProgramId=15
RecipeCodePresent=True
ExpStartId=baa36b5dd6c641bd92a21723c06dac96
RecipeCode=1
```

**2. 设置 TextBox 自动换行**

在 `LogManageView.Designer.cs` 或初始化代码中：

```csharp
txtProductionDetail.Multiline = true;
txtProductionDetail.WordWrap = true;
txtProductionDetail.ScrollBars = ScrollBars.Vertical;
```

### 6.2 中期改进

**1. 结构化 Detail 字段**

创建专用的 Detail 构建器：

```csharp
public class ProductionDetailBuilder
{
    private readonly List<(string Key, string Value)> _items = new();

    public ProductionDetailBuilder Add(string key, object value)
    {
        _items.Add((key, value?.ToString() ?? string.Empty));
        return this;
    }

    public string Build()
    {
        return string.Join(Environment.NewLine, 
            _items.Select(i => $"{i.Key}={i.Value}"));
    }

    public string BuildCompact()
    {
        return string.Join("; ", 
            _items.Select(i => $"{i.Key}={i.Value}"));
    }
}
```

**使用示例：**
```csharp
var detail = new ProductionDetailBuilder()
    .Add("ProgramId", program.Id)
    .Add("LocalProgramMatched", true)
    .Add("LocalProgramId", 15)
    .Add("RecipeCode", recipeCode)
    .Build();
```

**2. JSON 格式存储**

将 Detail 字段改为 JSON 格式：

```json
{
  "ProgramId": "05898390c52e4dd0987c0ab431528ac2",
  "LocalProgramMatched": true,
  "LocalProgramId": 15,
  "RecipeCodePresent": true,
  "ExpStartId": "baa36b5dd6c641bd92a21723c06dac96",
  "RecipeCode": "1"
}
```

### 6.3 长期改进

1. **统一日志框架**
   - 采用结构化日志（如 Serilog）
   - 统一日志级别和格式
   - 支持日志查询和过滤

2. **日志可视化增强**
   - 添加语法高亮
   - 支持折叠/展开
   - 支持搜索和过滤

3. **性能优化**
   - 异步写入
   - 日志轮转
   - 索引优化

---

## 七、使用的类和服务汇总

### 7.1 日志服务类

| 类名 | 位置 | 职责 |
|------|------|------|
| `ProgramExceptionLogService` | AutoWeldSystem.Services/Log/ | 异常日志 |
| `ProductionFlowLogService` | AutoWeldSystem.Services/Log/ | 生产流程日志 |
| `MesInteractionLogService` | AutoWeldSystem.Services/Log/ | MES交互日志 |
| `OperationLogService` | AutoWeldSystem.Services/Log/ | 操作日志 |

### 7.2 日志条目类

| 类名 | 位置 | 用途 |
|------|------|------|
| `ProgramExceptionLogEntry` | AutoWeldSystem.Core/ViewModels/ | 异常日志条目 |
| `ProductionFlowLogEntry` | AutoWeldSystem.Core/ViewModels/ | 生产流程日志条目 |
| `MesInteractionLogEntry` | AutoWeldSystem.Core/ViewModels/ | MES交互日志条目 |
| `SysOperationLog` | AutoWeldSystem.Core/Entities/ | 操作日志实体 |

### 7.3 使用日志的界面

| 界面 | 使用的日志服务 |
|------|---------------|
| **MonitorView** | ProgramExceptionLogService, ProductionFlowLogService |
| **LogManageView** | 全部四个日志服务（展示） |
| **MainForm** | ProgramExceptionLogService |
| **AddressManageView** | ProgramExceptionLogService |

### 7.4 使用日志的服务

| 服务 | 使用的日志服务 |
|------|---------------|
| **MesProvider** | MesInteractionLogService |
| **WeldTaskService** | ProgramExceptionLogService |
| **PlcCommunicationService** | ProgramExceptionLogService |
| **ProductCycleCollectionService** | ProductionFlowLogService |

---

## 八、快速定位指南

### 8.1 配方下发日志的 Detail 来源

**调用链：**
```
MonitorView.StartReport_Click
  └─> DispatchRecipeCodeAfterStartAsync
       └─> ResolveRecipeCodeForStartedTask  // 生成 resolution
            └─> WriteRecipeFlowLog          // 写入日志
                 └─> ProductionFlowLogService.Write  // 保存到文件
```

**resolution 对象包含：**
- `RecipeCode` - 配方编号
- `Source` - 来源（如 "ProgramId", "LocalProgram"）
- `Detail` - 详细信息（键值对字符串）

**Detail 拼接位置：**
在 `ResolveRecipeCodeForStartedTask` 方法中手动拼接

### 8.2 日志展示界面位置

**文件：** `AutoWeldSystem.UI/Views/LogManageView.cs`

**关键方法：**
- `ShowProductionLogDetails` (第761行) - 显示生产流程日志详情
- `FormatProductionDetail` (需要新增) - 格式化 Detail 字段
- `ConfigureProductionGrid` (第138行) - 配置数据网格

**关键控件：**
- `txtProductionDetail` - 显示 Detail 字段的文本框
- `dgvProductionLogs` - 生产流程日志数据网格
