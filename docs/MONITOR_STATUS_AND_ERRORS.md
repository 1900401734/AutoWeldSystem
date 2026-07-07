# MonitorView 异常提示和运行状态汇总

## 一、运行状态提示（RuntimeStatus）

### 1. 系统状态

| 状态Key | 文本内容 | 触发场景 |
|---------|---------|---------|
| `monitor.runtime.idle` | 空闲 | 初始状态、操作完成后 |
| `monitor.runtime.loading_work_order` | 正在加载工单 | 扫描工单后查询MES |
| `monitor.runtime.loading_programs` | 正在加载程序列表 | 从MES获取程序列表 |
| `monitor.runtime.downloading_program` | 正在下载程序 | 从MES下载程序详情 |
| `monitor.runtime.validating_operator` | 正在验证操作工 | 验证MES操作工信息 |
| `monitor.runtime.submitting_start` | 正在提交开工 | 开工上报到MES |
| `monitor.runtime.submitting_finish` | 正在提交完工 | 完工上报到MES |

### 2. 成功状态（动态文本）

| 场景 | 文本示例 |
|------|---------|
| 程序确认 | "加工程序已确认，本次开工将使用当前程序内容。" |
| 本地开工成功 | "工位{stationNo}本地开工已成功。" |
| 本地完工成功 | "工位{stationNo}本地完工已成功。" |
| 开工上报成功 | "工位{stationNo}开工上报已成功。" |
| 完工上报成功 | "工位{stationNo}完工上报已成功。" |
| 工单信息获取 | "工单信息已获取，请确认工序后点击开工上报。" |
| 工序选择 | "已选择工序：{process.ItemName}" |
| 数据采集完成 | "数据采集完成：焊点{record.TouchNo} {record.TestResult}" |
| 配方编号校验 | "配方编号校验通过：{recipeCode}" |
| 测试标志设置 | result.Message（来自服务层）|

### 3. 生产流程提示（ProductionHint）

| 提示Key | 文本内容 | 触发场景 |
|---------|---------|---------|
| `monitor.production_hint.product_data_ready` | 产品数据准备就绪 | PLC信号ProductDataReady |
| `monitor.production_hint.product_collection_start` | 产品数据采集开始 | 开始采集产品数据 |
| `monitor.production_hint.product_data_read_start` | 产品数据读取开始 | 从PLC读取产品数据 |
| `monitor.production_hint.product_data_saved` | 产品数据已保存 | 数据成功保存到数据库 |
| `monitor.production_hint.product_data_save_failed` | 产品数据保存失败 | 数据保存失败 |
| `monitor.production_hint.product_collection_feedback_succeeded` | 产品采集反馈成功 | 反馈信号写入PLC成功 |
| `monitor.production_hint.product_collection_feedback_failed` | 产品采集反馈失败 | 反馈信号写入PLC失败 |
| `monitor.production_hint.recipe_code_write_succeeded` | 配方编号写入成功 | 配方编号下发到PLC成功 |
| `monitor.production_hint.recipe_code_write_failed` | 配方编号写入失败 | 配方编号下发到PLC失败 |
| `monitor.production_hint.recipe_code_validation_succeeded` | 配方编号校验成功 | PLC配方编号与PC匹配 |
| `monitor.production_hint.recipe_code_validation_failed` | 配方编号校验失败 | PLC配方编号与PC不匹配 |
| `monitor.production_hint.business_signal_write_succeeded` | 业务信号写入成功 | 业务信号写入PLC成功 |
| `monitor.production_hint.business_signal_write_failed` | 业务信号写入失败 | 业务信号写入PLC失败 |

---

## 二、异常/错误提示（RuntimeError & Message）

### 1. 运行时错误（RuntimeError）

| 错误Key | 文本内容 | 触发场景 |
|---------|---------|---------|
| `monitor.error.work_id_read_failed` | 工单号读取失败 | 从PLC读取WorkId失败 |
| `monitor.error.production_collect_failed` | 生产数据采集失败 | 产品数据采集异常 |
| `monitor.error.operation_failed` | 操作失败 | 通用操作异常（catch Exception） |

### 2. 业务警告（Message - Warning）

| 警告Key | 文本内容 | 触发场景 |
|---------|---------|---------|
| `monitor.message.work_id_required` | 请先扫描工单号 | 未输入工单号 |
| `monitor.message.work_order_load_failed` | 工单加载失败 | MES工单查询失败 |
| `monitor.message.process_required` | 请先选择工序 | 未选择工序 |
| `monitor.message.program_list_empty` | 程序列表为空 | MES返回空程序列表 |
| `monitor.message.program_download_failed` | 程序下载失败 | MES程序下载失败 |
| `monitor.message.start_prerequisite_missing` | 开工前置条件不满足 | 缺少工单、工序或程序 |
| `monitor.message.start_blocked_by_unfinished_task` | 当前有未完工任务，请先完工 | 存在未完工任务 |
| `monitor.message.quantity_invalid` | 数量无效 | 输入的数量不合法 |
| `monitor.message.operator_validation_failed` | 操作工验证失败 | MES操作工验证失败 |
| `monitor.message.finish_prerequisite_missing` | 完工前置条件不满足 | 缺少开工记录 |

### 3. MessageBox 弹窗错误（动态文本）

| 场景 | 文本示例 | 触发位置 |
|------|---------|---------|
| 业务操作异常 | ex.Message（BusinessOperationException） | RunUiOperationAsync |
| 工位操作禁用 | "工位{stationNo}{actionName}已禁用，当前窗口为只读看板。" | CheckViewOnlyBlock |
| 工位操作冲突 | "工位{stationNo}{actionName}正在执行中，请稍后再试。" | RunStationOperationAsync |
| 工位操作失败 | "工位{stationNo}{actionName}失败：{detail}" | RunStationOperationAsync catch |

### 4. 业务操作异常（BusinessOperationException）

| 异常源 | 消息内容 | 触发场景 |
|--------|---------|---------|
| `PLC.RecipeCode` | "配方编号解析失败" | 配方编号格式错误 |
| `PLC.RecipeCode` | "配方编号下发失败" | 写入PLC失败 |
| `PLC.RecipeCodeCheck` | "配方编号校验失败" | PLC配方编号不匹配 |
| `PLC.RecipeCode` | "配方编号下发失败：当前无生产任务" | 任务状态不正确 |
| 其他（动态） | 由调用方指定 | WriteBusinessSignalAsync等 |

---

## 三、异常处理流程

### 1. 异常捕获层级

```
┌─────────────────────────────────────┐
│  RunUiOperationAsync                │  UI操作入口
│  - 捕获 BusinessOperationException   │
│  - 捕获 Exception                    │
│  - 记录日志 + 显示错误              │
└─────────────────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│  RunStationOperationAsync           │  工位操作入口
│  - 检查工位冲突                     │
│  - 捕获 BusinessOperationException   │
│  - 捕获 Exception                    │
│  - 记录日志 + 显示工位错误          │
└─────────────────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│  业务方法                            │  具体业务逻辑
│  - throw BusinessOperationException │
│  - throw Exception                  │
└─────────────────────────────────────┘
```

### 2. 异常日志记录

| 异常类型 | 日志方法 | 记录内容 |
|---------|---------|---------|
| `BusinessOperationException` | `_exceptionLogService.WriteBusiness()` | SourceName, Message, Detail |
| `Exception` | `_exceptionLogService.Write()` | Exception对象, 方法名 |
| 通用异常 | `_exceptionLogService.Write()` | 各个catch块中 |

### 3. 异常显示机制

| 显示方式 | 方法 | 用途 |
|---------|-----|------|
| 运行状态区（绿色） | `SetRuntimeStatusText(message, isSuccess: true)` | 成功提示 |
| 异常提示区（红色） | `SetRuntimeErrorText(message)` | 错误提示 |
| MessageBox弹窗 | `ShowError(message)` | 阻塞式错误 |
| MessageBox弹窗 | `ShowWarning(messageKey)` | 阻塞式警告 |

---

## 四、错误提示区域布局

### 1. UI控件

```
┌─────────────────────────────────────┐
│  grpErrorTips (异常提示组)           │
│  ┌──────────────────────────────┐  │
│  │ inputErrorTips               │  │
│  │ (显示错误文本)               │  │
│  └──────────────────────────────┘  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  grpRunningStatus (运行状态组)       │
│  ┌──────────────────────────────┐  │
│  │ inputRunningStatus           │  │
│  │ (显示状态文本)               │  │
│  └──────────────────────────────┘  │
└─────────────────────────────────────┘
```

### 2. 颜色编码

| 状态类型 | 控件颜色 | 用途 |
|---------|---------|------|
| 错误状态 | `UiColors.Status.Danger` (红色) | 异常、错误提示 |
| 正常状态 | `UiColors.Status.Muted` (灰色) | 空闲、无错误 |
| 成功状态 | `UiColors.Status.Success` (绿色) | 操作成功 |
| 进行中状态 | `UiColors.Status.Primary` (蓝色) | 正在执行操作 |

---

## 五、关键方法汇总

### 1. 状态设置方法

```csharp
// 设置运行状态（本地化Key）
SetRuntimeStatus(string messageKey, params object[] args)

// 设置运行状态（直接文本）
SetRuntimeStatusText(string message, bool isSuccess = false)

// 设置错误状态（本地化Key）
SetRuntimeError(string messageKey, params object[] args)

// 设置错误状态（直接文本）
SetRuntimeErrorText(string message)

// 清除错误状态
ClearRuntimeError()
```

### 2. 错误显示方法

```csharp
// 显示警告弹窗（本地化Key）
ShowWarning(string messageKey, params object[] args)

// 显示错误弹窗（直接文本）
ShowError(string message)

// 显示错误弹窗（本地化Key）
ShowError(string messageKey, params object[] args)

// 显示错误弹窗（业务异常）
ShowBusinessError(string source, string message, string detail, string messageKey, Dictionary<string, object>? context = null)
```

### 3. 工位操作错误

```csharp
// 设置工位操作失败
SetStationReportFailure(int stationNo, string actionName, string detail)

// 构建工位成功文本
BuildStationReportSuccessText(int stationNo, string actionName)

// 构建工位失败文本
BuildStationReportFailureText(int stationNo, string actionName, string detail)
```

---

## 六、典型错误场景示例

### 场景1：PLC断开连接

```
异常提示区显示：
TextKeys.Monitor.RuntimeError.WorkIdReadFailed
→ "工单号读取失败"

或

TextKeys.Monitor.RuntimeError.ProductionCollectFailed
→ "生产数据采集失败"
```

### 场景2：开工前置条件不满足

```
MessageBox警告：
TextKeys.Monitor.Message.StartPrerequisiteMissing
→ "开工前置条件不满足"

异常提示区显示：
"工位1开工上报失败：开工前置条件不满足"
```

### 场景3：操作工验证失败

```
运行状态：
TextKeys.Monitor.RuntimeStatus.ValidatingOperator
→ "正在验证操作工"

失败后MessageBox：
TextKeys.Monitor.Message.OperatorValidationFailed
→ "操作工验证失败"
```

### 场景4：配方编号下发失败

```
异常提示区显示：
throw new BusinessOperationException(
    "PLC.RecipeCode",
    "配方编号下发失败",
    writeResult.Message)
```

### 场景5：通用操作异常

```
异常提示区显示：
TextKeys.Monitor.RuntimeError.OperationFailed
→ "操作失败"

同时弹出MessageBox显示详细错误信息
```

---

## 七、状态持久化

运行状态和错误提示会被持久化到 `BizRuntimeTipState` 表，包括：

| 字段 | 说明 |
|------|------|
| `RuntimeStatusKey` | 当前运行状态Key |
| `RuntimeStatusText` | 当前运行状态文本 |
| `RuntimeErrorText` | 当前错误提示文本 |

在 `MonitorView` 加载时会恢复上次的状态。
