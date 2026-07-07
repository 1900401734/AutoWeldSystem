# MonitorView 业务逻辑完整总结
## 快速理解指南（7417行代码浓缩版）

> **文件：** AutoWeldSystem.UI/Views/MonitorView.cs  
> **代码量：** 7417 行  
> **复杂度：** ⭐⭐⭐⭐⭐ 极高  
> **核心职责：** 生产监控主界面 - 系统最复杂的页面

---

## 一、页面概览

### 1.1 这个页面是什么？

**MonitorView = 生产线实时监控中心**

这是整个系统的**核心界面**，操作员在这里：
- 📋 获取和查看工单信息
- ▶️ 开工上报（启动生产任务）
- ⏸️ 暂停/恢复生产
- ⏹️ 完工上报（结束生产任务）
- 📊 实时查看焊接数据和生产状态
- 🔍 监控 PLC 和 MES 通信状态
- 📈 查看生产历史记录

**类比：** 如果把焊接系统比作飞机，MonitorView 就是驾驶舱的主控制面板。

---

### 1.2 界面布局

```
┌─────────────────────────────────────────────────────────────────┐
│  [Logo]  系统标题              [PLC] [MES] [设备] [任务]        │ ← 顶部状态栏
├─────────────────────────────────────────────────────────────────┤
│                         │                                         │
│   左侧：实时预览区       │   右侧：工单和控制区                  │
│                         │                                         │
│  ┌─────────────────┐   │   ┌───────────────────────────────┐   │
│  │ 工位1/工位2选择 │   │   │ 工单信息                       │   │
│  ├─────────────────┤   │   │ - 工单号                       │   │
│  │ 实时焊点数据表  │   │   │ - 产品型号                     │   │
│  │ (横向滚动)      │   │   │ - 批次                         │   │
│  │                 │   │   │ - 工序                         │   │
│  │ TouchNo  Result │   │   │ - 操作员                       │   │
│  │   1       OK    │   │   └───────────────────────────────┘   │
│  │   2       NG    │   │                                         │
│  │   3       OK    │   │   ┌───────────────────────────────┐   │
│  │  ...            │   │   │ 控制按钮                       │   │
│  └─────────────────┘   │   │ [获取工单] [开工上报]          │   │
│                         │   │ [本地开工] [完工上报]          │   │
│  ┌─────────────────┐   │   └───────────────────────────────┘   │
│  │ 产品历史        │   │                                         │
│  │ Product1  OK    │   │   ┌───────────────────────────────┐   │
│  │ Product2  NG    │   │   │ 生产指标表                     │   │
│  │ Product3  OK    │   │   │ 电流 电压 位移 时间 ...        │   │
│  └─────────────────┘   │   └───────────────────────────────┘   │
│                         │                                         │
│                         │   ┌───────────────────────────────┐   │
│                         │   │ 运行状态/错误提示              │   │
│                         │   └───────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 二、核心业务流程（5大流程）

### 流程1：获取工单 → 准备程序 ⏱️ 30秒

**用户操作：** 点击"获取工单"按钮

**系统流程：**
```
1. 用户输入/扫描工单号
   ↓
2. 调用 MES 接口查询工单信息
   ↓
3. 显示工单详情（产品型号、批次、工序等）
   ↓
4. 用户选择工序
   ↓
5. 根据产品工号匹配本地程序
   ↓
6. 显示程序内容预览（焊点列表）
   ↓
7. 等待用户确认开工
```

**关键方法：**
- `PrepareWorkOrderAsync()` - 准备工单
- `PrepareProgramForCurrentWorkOrderAsync()` - 准备程序

**涉及的服务：**
- `IWeldTaskService` - 工单查询
- `IProgramManageService` - 程序匹配

---

### 流程2：在线开工上报 ⏱️ 5秒

**用户操作：** 点击"开工上报"按钮

**系统流程：**
```
1. 验证操作员身份（输入工号）
   ↓
2. 验证工单状态（不能重复开工）
   ↓
3. 调用 MES 开工接口（ExpStartRequest）
   ↓
4. MES 返回 ExpStartId（任务唯一标识）
   ↓
5. 创建本地任务记录（BizWeldTask）
   ↓
6. 下发配方编号到 PLC
   ↓
7. 校验 PLC 配方是否匹配
   ↓
8. 写入工单状态信号（允许生产）
   ↓
9. 界面切换到"生产中"状态
   ↓
10. 开始实时数据采集
```

**关键方法：**
- `WeldTaskService.StartAsync()` - 创建任务
- `DispatchRecipeCodeAfterStartAsync()` - 下发配方
- `WriteStartBusinessSignalsAsync()` - 写入PLC信号

**关键字段：**
- `ExpStartId` - MES任务ID（最重要）
- `LocalExpStartId` - 本地任务ID
- `RecipeCode` - 配方编号（必须匹配）

---

### 流程3：离线/本地开工 ⏱️ 3秒

**用户操作：** 点击"本地开工"按钮（MES断网时）

**系统流程：**
```
1. 用户手动填写工单信息
   ↓
2. 选择本地程序
   ↓
3. 创建离线任务（ExpStartId = NULL）
   ↓
4. 标记 IsOfflineCreated = true
   ↓
5. 将开工上报加入上传队列
   ↓
6. 下发配方到 PLC
   ↓
7. 开始生产
   ↓
8. 等待 MES 恢复后自动上传
```

**关键方法：**
- `WeldTaskService.StartLocalAsync()` - 创建离线任务

**关键区别：**
| 对比项 | 在线开工 | 离线开工 |
|--------|---------|---------|
| ExpStartId | MES分配（有值） | NULL（待同步） |
| UploadStatus | Uploaded | Pending |
| IsOfflineCreated | false | true |

---

### 流程4：实时数据采集 ⏱️ 持续运行

**触发方式：** PLC 发送完成信号

**系统流程：**
```
PLC 焊接一个点完成
   ↓
触发 PlcWeldCycleMonitorService.WeldPointCollected 事件
   ↓
MonitorView 接收事件
   ↓
保存焊点记录到数据库（BizWeldPointRecord）
   ↓
更新实时预览表格（左侧滚动表格）
   ↓
生成 Excel 报告
   ↓
如果是产品最后一个点：
   └─> 更新产品历史表格
   └─> 可选：实时上传到 MES
```

**关键方法：**
- `PlcWeldCycleMonitorService_WeldPointCollected()` - 事件处理
- `BindWeldParameterRows()` - 更新UI

**数据流：**
```
PLC → PlcWeldCycleMonitorService 
    → ProductCycleCollectionService
    → BizWeldPointRecord (数据库)
    → MonitorView (UI更新)
```

---

### 流程5：完工上报 ⏱️ 3秒

**用户操作：** 点击"完工上报"按钮

**系统流程：**
```
1. 读取 PLC 产量数据
   ↓
2. 用户确认数量（实际/合格/不合格）
   ↓
3. 调用 MES 完工接口（ExpEndRequest）
   ↓
4. 更新任务状态（EndTime, TaskStatus=Completed）
   ↓
5. 写入 PLC 工单状态（禁止生产）
   ↓
6. 上传工艺参数到 MES
   ↓
7. 上传 Excel 报告
   ↓
8. 清空界面，准备下一个工单
```

**关键方法：**
- `WeldTaskService.FinishAsync()` - 完工处理
- `WriteFinishBusinessSignalsAsync()` - 写入PLC信号

---

## 三、关键业务规则（10条必知）

### 规则1：配方编号校验（最严格）

**规则：** 程序配方 = PLC配方，否则不允许生产

```csharp
// 校验逻辑
if (programRecipeCode != plcRecipeCode)
{
    throw new BusinessException("配方不匹配");
}
```

**目的：** 防止用错程序导致产品报废

---

### 规则2：一个工位同时只能有一个任务

**规则：** 开工前检查是否有未完成的任务

```csharp
var activeTask = GetUnfinishedTask(stationNo);
if (activeTask != null)
{
    throw new BusinessException("已有任务在进行中");
}
```

---

### 规则3：双工位模式支持

**规则：** 根据配置，两个工位可以：
- 共用一个工单（同步）
- 各自独立工单（独立）

```csharp
if (IsSameWorkOrderMode())
{
    // 两个工位共享同一个任务
    task.StationNo = 0;  // 0 = 双工位共享
}
```

---

### 规则4：离线数据自动上传

**规则：** MES 恢复后自动上传离线期间的数据

```
系统启动时 → 检查 Pending 上传任务 → 自动重试上传
```

---

### 规则5：工单状态同步到 PLC

**规则：** 开工/完工后必须写入 PLC 状态信号

```csharp
// 开工：允许生产
await WritePLC("WorkOrderStatus", value: 1);

// 完工：禁止生产
await WritePLC("WorkOrderStatus", value: 2);
```

**目的：** PLC 根据状态控制设备是否可以焊接

---

### 规则6：ExpStartId 是数据关联核心

**规则：** 所有数据必须关联 ExpStartId

```
BizWeldTask.ExpStartId
    ↓
BizWeldPointRecord.ExpStartId (继承自任务)
    ↓
上传到 MES 时用 ExpStartId 关联
```

---

### 规则7：产品完成判断

**规则：** 最后一个焊点采集时标记产品完成

```csharp
if (currentPointNo == totalPointCount)
{
    record.ProductCompleted = true;
    RefreshProductHistoryPreview();
}
```

---

### 规则8：试焊件标记

**规则：** 可以右键产品历史记录标记为试焊件

```csharp
// 试焊件不计入合格率统计
if (product.IsTest)
{
    // 跳过统计
}
```

---

### 规则9：实时上传 vs 批量上传

**规则：** 根据配置选择上传模式

```csharp
if (_settings.UploadMode == "Realtime")
{
    // 每个焊点完成后立即上传
    await UploadImmediately(weldPoint);
}
else
{
    // 完工后批量上传
    await QueueForLaterUpload(weldPoint);
}
```

---

### 规则10：状态恢复（断电重启）

**规则：** 系统重启后恢复未完成的任务

```csharp
// OnLoad 时
var unfinishedTask = RestoreUnfinishedTask(currentStation);
if (unfinishedTask != null)
{
    // 恢复界面状态
    RestoreWorkOrderDisplay(unfinishedTask);
    ResendRecipeCodeToPLC(unfinishedTask.RecipeCode);
}
```

---

## 四、依赖的服务（19个）

### 4.1 核心业务服务

| 服务 | 职责 | 关键方法 |
|------|------|---------|
| **IWeldTaskService** | 任务管理 | StartAsync, FinishAsync |
| **IProgramManageService** | 程序管理 | GetPrograms, DownloadProgram |
| **IProductProcessConfigService** | 产品工序配置 | GetConfig |

### 4.2 PLC 通信服务（7个）

| 服务 | 职责 |
|------|------|
| **IPlcCommunicationService** | PLC 基础通信 |
| **IPlcProductionMonitorService** | 生产状态监控 |
| **IPlcWorkIdMonitorService** | 工号监控 |
| **IPlcWeldCycleMonitorService** | 焊接周期监控（最重要）|
| **IPlcAddressService** | 地址映射 |
| **IPlcBusinessSignalService** | 业务信号读写 |
| **IPlcExpressionReadService** | 表达式读取 |

### 4.3 MES 服务

| 服务 | 职责 |
|------|------|
| **IMesConnectionMonitor** | MES 连接状态监控 |

### 4.4 数据和日志服务

| 服务 | 职责 |
|------|------|
| **IProductRealtimePreviewService** | 实时预览数据 |
| **IProductHistoryService** | 产品历史数据 |
| **IProgramExceptionLogService** | 异常日志 |
| **IProductionFlowLogService** | 生产流程日志 |
| **IRuntimeTipStateService** | 运行提示状态 |

### 4.5 配置和国际化

| 服务 | 职责 |
|------|------|
| **IAppSettingsService** | 系统配置 |
| **ILocalizationService** | 多语言 |
| **ITestSchemeConfigService** | 测试方案配置 |

---

## 五、状态管理（7个状态）

### 5.1 任务状态

```csharp
// ProductionRuntimeState.ActiveTask
- null: 未开工
- 有值 + EndTime=null: 进行中
- 有值 + EndTime!=null: 已完成
```

### 5.2 PLC 连接状态

```csharp
tagPLC.BackColor = 
    - Green: 已连接
    - Red: 断开
    - Gray: 未知
```

### 5.3 MES 连接状态

```csharp
tagMes.BackColor =
    - Green: 已连接
    - Red: 断开
```

### 5.4 设备状态

```csharp
tagDeviceStatus.Text =
    - "待机": 空闲
    - "运行中": 生产中
    - "故障": 设备异常
```

### 5.5 工单状态

```csharp
- 未获取: 界面显示"请获取工单"
- 已获取: 显示工单信息
- 已开工: 显示实时数据
- 已完工: 清空界面
```

### 5.6 错误提示状态

```csharp
inputErrorTips.Text = 错误消息
inputErrorTips.ForeColor = Red
```

### 5.7 运行状态提示

```csharp
inputRunningStatus.Text = 成功消息
inputRunningStatus.ForeColor = Green
```

---

## 六、事件监听（6个关键事件）

### 事件1：焊点采集完成

```csharp
_plcWeldCycleMonitorService.WeldPointCollected += 
    PlcWeldCycleMonitorService_WeldPointCollected;

// 处理：更新UI、保存数据库、生成报告
```

### 事件2：生产状态变化

```csharp
_plcProductionMonitorService.StatusChanged += 
    PlcProductionMonitorService_StatusChanged;

// 处理：更新设备状态标签
```

### 事件3：工号变化

```csharp
_plcWorkIdMonitorService.WorkIdChanged += 
    PlcWorkIdMonitorService_WorkIdChanged;

// 处理：显示当前产品编号
```

### 事件4：实时预览数据更新

```csharp
_productRealtimePreviewService.SnapshotChanged += 
    ProductRealtimePreviewService_SnapshotChanged;

// 处理：刷新左侧预览表格
```

### 事件5：生产流程日志写入

```csharp
_productionLogService.LogWritten += 
    ProductionLogService_LogWritten;

// 处理：显示提示信息
```

### 事件6：配置变更

```csharp
_settingsService.SettingsChanged += 
    SettingsService_SettingsChanged;

// 处理：重新加载配置
```

---

## 七、关键数据结构

### 7.1 ProductionRuntimeState（运行时状态）

```csharp
public class ProductionRuntimeState
{
    public BizWeldTask? ActiveTask { get; set; }      // 当前任务
    public WorkOrderRes? WorkOrder { get; set; }      // 工单信息
    public ExpItemData? SelectedProcess { get; set; } // 选中工序
    public ProgramDataRes? SelectedProgram { get; set; } // 选中程序
    public string MesOperatorNumber { get; set; }     // 操作员工号
    public DateTime UpdatedTime { get; set; }         // 更新时间
}
```

### 7.2 BizWeldTask（任务实体）

```csharp
public class BizWeldTask
{
    public int Id { get; set; }
    public string ExpStartId { get; set; }        // MES任务ID ⭐
    public string LocalExpStartId { get; set; }   // 本地任务ID
    public string SN { get; set; }                // 工单号
    public string RecipeCode { get; set; }        // 配方编号 ⭐
    public DateTime StartTime { get; set; }       // 开始时间
    public DateTime? EndTime { get; set; }        // 结束时间
    public bool IsOfflineCreated { get; set; }    // 是否离线创建
}
```

### 7.3 BizWeldPointRecord（焊点记录）

```csharp
public class BizWeldPointRecord
{
    public int Id { get; set; }
    public int TaskId { get; set; }               // 关联任务ID
    public string ExpStartId { get; set; }        // 继承自任务 ⭐
    public int TouchNo { get; set; }              // 焊点编号
    public string TestResult { get; set; }        // OK/NG
    public double? MaxElectric { get; set; }      // 最大电流
    public double? MaxVoltage { get; set; }       // 最大电压
    public bool ProductCompleted { get; set; }    // 产品是否完成
}
```

---

## 八、常见问题解答（FAQ）

### Q1: 为什么代码这么长（7417行）？

**A:** MonitorView 承担了太多职责，违反单一职责原则。应该拆分为：
- WorkOrderPanel（工单信息）
- ProductionControlPanel（控制按钮）
- RealtimePreviewPanel（实时预览）
- ProductHistoryPanel（产品历史）
- MonitorPresenter（业务逻辑）

### Q2: ExpStartId 为什么这么重要？

**A:** ExpStartId 是 MES 分配的任务唯一标识，用于：
- 关联所有焊点数据
- 上传数据到 MES
- 追溯生产历史

### Q3: 离线模式如何工作？

**A:** 
1. 离线时创建任务，ExpStartId=NULL
2. 数据保存到本地数据库
3. MES 恢复后，自动上传开工上报
4. MES 返回 ExpStartId，更新所有相关数据
5. 继续上传完工和焊点数据

### Q4: 配方校验为什么这么严格？

**A:** 防止用错程序导致产品报废（可能损失数万元）

### Q5: 双工位模式如何切换？

**A:** 通过 `segmentedStationSwitch` 控件切换工位，每个工位维护独立的状态

---

## 九、重构建议（优先级）

### 🔴 P0 - 立即重构

1. **拆分 MonitorView**
   - 目标：每个文件 < 500 行
   - 方法：提取 Panel 和 Presenter

2. **提取业务逻辑到 Service**
   - 配方校验逻辑 → RecipeValidationService
   - 统计计算逻辑 → ProductionStatisticsService

### 🟡 P1 - 短期重构

3. **引入 MVVM/MVP 模式**
   - ViewModel 管理状态
   - Presenter 处理业务逻辑
   - View 只负责UI交互

4. **统一事件处理**
   - 使用领域事件替代直接方法调用

### 🟢 P2 - 长期优化

5. **性能优化**
   - 实时预览表格使用虚拟滚动
   - 减少不必要的UI刷新

---

## 十、快速上手指南

### 新人如何快速理解这个页面？

**第1步：** 理解5大流程（本文档第二章）  
**第2步：** 阅读关键方法的注释  
**第3步：** 调试一次完整的开工→生产→完工流程  
**第4步：** 查看事件监听（第六章）  
**第5步：** 理解数据结构（第七章）

### 如何调试？

1. 设置断点在 `StartAsync()` - 开工流程
2. 设置断点在 `WeldPointCollected` - 数据采集
3. 设置断点在 `FinishAsync()` - 完工流程
4. 观察 `ProductionRuntimeState` 的变化

---

## 十一、总结

### MonitorView 的核心价值

**一句话：** MonitorView 是整个焊接生产系统的**指挥中心**，协调 PLC、MES、数据库、UI 的所有交互。

### 关键数字

- **7417 行代码**
- **5 大核心流程**
- **19 个依赖服务**
- **6 个关键事件**
- **10 条业务规则**

### 最重要的3个概念

1. **ExpStartId** - 数据关联的核心
2. **配方校验** - 质量保证的关键
3. **离线模式** - 韧性设计的体现

---

**阅读完本文档，你应该能回答：**
- ✅ MonitorView 的主要功能是什么？
- ✅ 开工流程有哪些步骤？
- ✅ ExpStartId 为什么重要？
- ✅ 离线模式如何工作？
- ✅ 如何调试这个页面？
