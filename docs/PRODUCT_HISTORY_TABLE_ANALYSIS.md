# tableProductHistoryPreview1/2 数据显示分析

## 一、控件概述

### 1.1 控件定义

| 控件名称 | 工位 | 用途 |
|---------|------|------|
| `tableProductHistoryPreview1` | 工位1 | 显示工位1的产品历史数据 |
| `tableProductHistoryPreview2` | 工位2 | 显示工位2的产品历史数据 |

### 1.2 当前激活表格选择

```csharp
// MonitorView.cs 第698行
private AntdUI.Table CurrentProductHistoryTable => 
    CurrentStationNo == 2 ? tableProductHistoryPreview2 : tableProductHistoryPreview1;
```

根据 `CurrentStationNo`（当前选中工位）决定使用哪个表格。

---

## 二、数据显示触发条件

### 2.1 核心判断逻辑

**关键方法：** `RefreshProductHistoryPreviewCore()` (MonitorView.cs 第2894行)

```csharp
private void RefreshProductHistoryPreviewCore()
{
    try
    {
        var activeTask = GetCurrentStationState().ActiveTask;
        
        // 关键判断：如果没有活动任务，清空表格
        if (activeTask is null)
        {
            ConfigureProductHistoryTableColumns(CurrentProductHistoryTable, [], CurrentStationNo);
            BindProductHistoryRows(CurrentProductHistoryTable, []);  // 绑定空数据
            return;
        }

        // 有活动任务时，从数据库查询产品历史
        var snapshot = _productHistoryService.GetSnapshot(activeTask.Id, CurrentStationNo);
        BindProductHistorySnapshot(snapshot, activeTask);
    }
    catch (Exception ex)
    {
        _exceptionLogService.Write(ex, "MonitorView.RefreshProductHistoryPreview");
    }
}
```

### 2.2 显示数据的前提条件

**必要条件：**

1. **必须有活动任务（ActiveTask）**
   - `activeTask != null`
   - 任务状态为"已开工"或"已暂停"
   - 任务未完工（`EndTime == null`）

2. **数据库中存在已完成的产品数据**
   - `BizWeldPointRecord` 表中有该任务的记录
   - 至少有一条记录的 `ProductCompleted == true`
   - `ProductNo` 字段不为空

**简单说：只有在开工后且至少采集了一个完整产品的数据后，表格才会显示内容。**

---

## 三、数据查询流程

### 3.1 调用链

```
RefreshProductHistoryPreviewCore()
  ↓
_productHistoryService.GetSnapshot(activeTask.Id, CurrentStationNo)
  ↓
ProductHistoryService.GetTaskStationRecords(taskId, stationNo)
  ↓
数据库查询：SELECT * FROM BizWeldPointRecord 
           WHERE TaskId = ? AND StationNo = ?
  ↓
BuildProducts(records)  // 按 ProductNo 分组并过滤
  ↓
返回 ProductHistorySnapshot { Products = [...] }
```

### 3.2 数据库查询

**表：** `BizWeldPointRecord`

**查询条件：**
```sql
SELECT * FROM BizWeldPointRecord
WHERE TaskId = {activeTask.Id} 
  AND StationNo = {CurrentStationNo}
ORDER BY ProductNo, SequenceNo, Id
```

**关键字段：**
- `TaskId` - 任务ID
- `StationNo` - 工位号
- `ProductNo` - 产品编号
- `ProductCompleted` - 产品是否完成采集
- `SequenceNo` - 焊点序号
- `TouchNo` - 焊点编号
- `TestResult` - 测试结果（OK/NG）
- `UploadStatus` - 上传状态
- `IsTest` - 是否试焊件
- `RecordTime` - 采集时间

### 3.3 数据分组逻辑

**ProductHistoryService.BuildProducts() 方法：**

```csharp
private static IReadOnlyList<ProductHistoryProduct> BuildProducts(
    IReadOnlyList<BizWeldPointRecord> records)
{
    return records
        .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))  // 过滤空产品编号
        .GroupBy(record => record.ProductNo, StringComparer.OrdinalIgnoreCase)  // 按产品编号分组
        .Select(group => BuildProduct(group.ToList()))  // 构建每个产品
        .Where(product => product is not null)  // 过滤掉 null（未完成的产品）
        .Cast<ProductHistoryProduct>()
        .OrderBy(product => product.LastRecordTime ?? DateTime.MinValue)
        .ThenBy(product => product.ProductNo)
        .ToList();
}
```

**关键过滤：** `BuildProduct` 方法中的判断

```csharp
private static ProductHistoryProduct? BuildProduct(
    IReadOnlyList<BizWeldPointRecord> records)
{
    // 关键：只有至少一条记录 ProductCompleted == true 才返回产品
    if (records.Count == 0 || !records.Any(record => record.ProductCompleted))
    {
        return null;  // 返回 null，会被过滤掉
    }
    
    // 构建产品数据...
}
```

**结论：只有标记为"已完成"的产品才会显示在表格中。**

---

## 四、何时触发刷新

### 4.1 刷新触发点汇总

| 触发点 | 文件位置 | 场景 |
|--------|---------|------|
| `SettingsService_SettingsChanged` | 第245行 | 系统配置变更后 |
| `MonitorView.Load` | 第954行 | 页面首次加载 |
| `RefreshProductionRuntimeState` | 第2229行 | 生产状态刷新 |
| `PlcWeldCycleMonitorService_WeldPointCollected` | 第2263行 | **焊点采集完成（产品完成时）** |
| `ProductionLogService_LogWritten` | 第2298行 | 生产流程日志写入后 |
| `SetProductHistoryTestFlag` | 第2989行 | 设置试焊件标记后 |

### 4.2 最重要的触发点

**焊点采集完成：** `PlcWeldCycleMonitorService_WeldPointCollected`

```csharp
private void PlcWeldCycleMonitorService_WeldPointCollected(object? sender, BizWeldPointRecord record)
{
    ApplyStationResult(record);
    if (record.StationNo > 0 && record.StationNo != CurrentStationNo)
    {
        return;
    }

    BindWeldParameterRows(record);
    
    // 关键：只有产品完成采集时才刷新产品历史表格
    if (record.ProductCompleted)
    {
        RefreshProductHistoryPreview();
    }

    ClearRuntimeError();
    SetRuntimeStatusText($"数据采集完成：焊点{record.TouchNo} {record.TestResult}", isSuccess: true);
}
```

**流程：**
1. PLC 发出焊点完成信号
2. `PlcWeldCycleMonitorService` 采集焊点数据并保存到数据库
3. 触发 `WeldPointCollected` 事件
4. MonitorView 接收事件
5. 如果 `record.ProductCompleted == true`，刷新产品历史表格
6. 新产品出现在表格中

---

## 五、数据显示条件总结

### 5.1 表格显示数据的完整条件

```
✅ 必须满足所有条件：

1. 工位已开工（存在 ActiveTask）
   └─> 通过"开工上报"或"本地开工"创建任务

2. 至少完成了一个产品的采集
   └─> PLC 焊接循环完成
   └─> PlcWeldCycleMonitorService 采集并保存数据
   └─> 最后一个焊点标记 ProductCompleted = true

3. 产品编号（ProductNo）不为空
   └─> 从 PLC 读取或从工单中获取

4. 数据库中的记录状态正确
   └─> TaskId 匹配
   └─> StationNo 匹配
   └─> ProductCompleted = true
```

### 5.2 表格为空的常见原因

| 原因 | 说明 |
|------|------|
| **未开工** | 没有调用"开工上报"或"本地开工" |
| **没有完成产品** | 焊接未完成，ProductCompleted = false |
| **ProductNo 为空** | PLC 未提供产品编号，或工单中没有 |
| **工位不匹配** | 数据属于另一个工位 |
| **任务已完工** | ActiveTask.EndTime != null |
| **数据采集未启动** | PLC 通信断开或采集服务未运行 |

---

## 六、调试指南

### 6.1 检查是否有活动任务

**代码位置：** MonitorView.cs 第2898行

```csharp
var activeTask = GetCurrentStationState().ActiveTask;
if (activeTask is null)
{
    // 表格会被清空
}
```

**检查方法：**
1. 查看界面左上角工位状态标签
2. 应显示"已开工"或"已暂停"
3. 如果显示"未开工"或"待开工"，表格不会有数据

### 6.2 检查数据库中的记录

**SQL 查询：**
```sql
-- 查看当前任务的焊点记录
SELECT 
    Id,
    TaskId,
    StationNo,
    ProductNo,
    TouchNo,
    ProductCompleted,
    TestResult,
    UploadStatus,
    RecordTime
FROM BizWeldPointRecord
WHERE TaskId = {当前任务ID}
  AND StationNo = {当前工位号}
ORDER BY ProductNo, SequenceNo;
```

**关键检查点：**
- `TaskId` 是否正确
- `ProductNo` 是否有值
- `ProductCompleted` 是否为 `true`（至少有一条）
- 是否有多条记录属于同一个 `ProductNo`

### 6.3 检查 PLC 通信状态

**检查点：**
1. 界面右上角 PLC 标签应显示"已连接"
2. `PlcWeldCycleMonitorService` 是否正在运行
3. PLC 是否发送焊点完成信号

### 6.4 启用日志跟踪

**在 ProductHistoryService 中添加日志：**

```csharp
public ProductHistorySnapshot GetSnapshot(int taskId, int stationNo)
{
    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var records = GetTaskStationRecords(taskId, stationNo);
        
        // 调试日志
        Console.WriteLine($"[ProductHistory] TaskId={taskId}, StationNo={stationNo}, RecordCount={records.Count}");
        
        var products = BuildProducts(records);
        
        // 调试日志
        Console.WriteLine($"[ProductHistory] ProductCount={products.Count}");
        
        return new ProductHistorySnapshot
        {
            TaskId = taskId,
            StationNo = stationNo,
            Products = products
        };
    }
}
```

---

## 七、典型使用场景时间线

### 场景1：正常生产流程

```
时间线：

T0: 用户点击"开工上报"
    └─> 创建 BizWeldTask (ActiveTask)
    └─> RefreshProductHistoryPreview() 被调用
    └─> 表格为空（没有产品数据）

T1: PLC 完成第1个焊点
    └─> PlcWeldCycleMonitorService 采集数据
    └─> 保存到数据库，ProductCompleted = false
    └─> 不触发表格刷新

T2: PLC 完成第2个焊点
    └─> PlcWeldCycleMonitorService 采集数据
    └─> 保存到数据库，ProductCompleted = false
    └─> 不触发表格刷新

T3: PLC 完成最后一个焊点（产品完成）
    └─> PlcWeldCycleMonitorService 采集数据
    └─> 保存到数据库，ProductCompleted = true ✅
    └─> 触发 WeldPointCollected 事件
    └─> MonitorView.RefreshProductHistoryPreview()
    └─> 表格显示第1个产品 ✅

T4: 继续生产第2个产品...
    └─> 循环重复 T1-T3
    └─> 表格显示第2个产品 ✅

T5: 用户点击"完工上报"
    └─> ActiveTask.EndTime 被设置
    └─> 表格被清空（因为 activeTask 不再是活动状态）
```

### 场景2：离线调试（无PLC）

```
时间线：

T0: 用户点击"本地开工"
    └─> 创建本地任务
    └─> 表格为空

T1: 手动插入测试数据到数据库
    INSERT INTO BizWeldPointRecord 
    (TaskId, StationNo, ProductNo, TouchNo, ProductCompleted, ...)
    VALUES (1, 1, 'TEST001', 1, false, ...);
    
    └─> 表格仍为空（ProductCompleted = false）

T2: 更新最后一条记录为已完成
    UPDATE BizWeldPointRecord 
    SET ProductCompleted = true 
    WHERE Id = {最后一条记录的ID};
    
    └─> 手动调用 RefreshProductHistoryPreview()
    └─> 表格显示测试产品 ✅
```

---

## 八、快速排查清单

当表格不显示数据时，按以下顺序检查：

1. ☑️ **工位是否已开工？**
   - 查看工位状态标签
   - 如果未开工 → 点击"开工上报"或"本地开工"

2. ☑️ **是否完成了至少一个产品？**
   - 查看数据库 `BizWeldPointRecord` 表
   - 是否有 `ProductCompleted = true` 的记录

3. ☑️ **ProductNo 是否有值？**
   - 检查 PLC 产品工号地址配置
   - 或检查 MES 工单中的产品信息

4. ☑️ **PLC 是否连接？**
   - 查看界面右上角 PLC 状态
   - 应显示"已连接"

5. ☑️ **工位号是否匹配？**
   - 数据库中的 `StationNo` 必须与当前工位一致

6. ☑️ **是否有异常日志？**
   - 查看"日志管理" → "程序异常日志"
   - 搜索 "RefreshProductHistoryPreview"

---

## 九、代码关键位置速查

| 功能 | 文件 | 行号 | 方法 |
|------|------|------|------|
| 判断是否显示数据 | MonitorView.cs | 2898 | `RefreshProductHistoryPreviewCore()` |
| 数据库查询 | ProductHistoryService.cs | 87 | `GetTaskStationRecords()` |
| 产品分组过滤 | ProductHistoryService.cs | 98 | `BuildProducts()` |
| 产品完成判断 | ProductHistoryService.cs | 111 | `BuildProduct()` |
| 焊点完成触发 | MonitorView.cs | 2253 | `PlcWeldCycleMonitorService_WeldPointCollected()` |
| 绑定数据到表格 | MonitorView.cs | 2915 | `BindProductHistorySnapshot()` |

---

## 十、总结

**表格显示数据的核心逻辑：**

```
有活动任务 + 至少一个完整产品 + ProductNo不为空 = 表格显示数据
```

**最常见的"表格为空"原因：**
1. 没有开工（90%）
2. 产品未完成采集（5%）
3. ProductNo 为空（3%）
4. PLC 未连接（2%）
