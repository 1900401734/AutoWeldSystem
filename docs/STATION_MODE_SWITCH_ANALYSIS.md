# 工位模式切换机制完整分析
## 单工位/双工位同工单/双工位双工单详解

> **核心配置：** AppSettings 中的两个关键字段  
> **切换逻辑：** MonitorView 根据配置动态调整界面和业务逻辑

---

## 一、三种工位模式详解

### 模式对比表

| 模式 | EnableDualStation | EnableDualWorkOrder | 工位数 | 工单数 | 典型场景 |
|------|------------------|-------------------|--------|--------|---------|
| **单工位模式** | false | false | 1 | 1 | 小型设备，单工位生产 |
| **双工位同工单** | true | false | 2 | 1（共享） | 大型设备，两个工位同时生产同一个工单 |
| **双工位双工单** | true | true | 2 | 2（独立） | 超大型设备，两个工位独立生产不同工单 |

---

### 1.1 单工位模式

```
配置：
EnableDualStation = false
EnableDualWorkOrder = false (无效，因为只有单工位)

特点：
- 界面只显示一个工位的数据
- CurrentStationNo 固定为 1
- 不显示工位切换器
- 任务的 StationNo = 1
```

**界面布局：**
```
┌────────────────────────────────────────────────┐
│  [工位切换器不显示]                            │
├────────────────────────────────────────────────┤
│  左侧：工位1预览                               │
│  右侧：工位1控制面板                           │
└────────────────────────────────────────────────┘
```

**数据库记录：**
```sql
-- 所有任务和焊点都是 StationNo = 1
BizWeldTask: { StationNo = 1, ... }
BizWeldPointRecord: { StationNo = 1, ... }
```

---

### 1.2 双工位同工单模式（最常用）

```
配置：
EnableDualStation = true
EnableDualWorkOrder = false  ← 关键：同工单

特点：
- 界面显示两个工位
- 两个工位共享一个工单
- 任务的 StationNo = 0（表示双工位共享）
- PLC 信号同时写入两个工位
```

**界面布局：**
```
┌────────────────────────────────────────────────┐
│  [工位1] [工位2] ← 可切换查看                 │
├────────────────────────────────────────────────┤
│  左侧：当前工位预览                            │
│  右侧：共享工单控制面板                        │
└────────────────────────────────────────────────┘
```

**核心逻辑：**
```csharp
// MonitorView.cs 第493行
private IReadOnlyList<int> ResolveWorkOrderSignalStations(int stationNo)
{
    var settings = _currentSettings;
    if (settings.EnableDualStation && !settings.EnableDualWorkOrder)
    {
        // ✅ 双工位同工单：返回 [1, 2]，信号写入两个工位
        return [1, 2];
    }
    
    // 双工位双工单：只返回当前工位
    return [NormalizeStatusStationNo(stationNo)];
}
```

**数据库记录：**
```sql
-- 任务是共享的，StationNo = 0
BizWeldTask: { StationNo = 0, ... }

-- 焊点记录分别标记工位
BizWeldPointRecord: { TaskId = 1, StationNo = 1, ... }  -- 工位1的焊点
BizWeldPointRecord: { TaskId = 1, StationNo = 2, ... }  -- 工位2的焊点
```

**业务流程示例：**
```
1. 工位1获取工单 "WO-2024001"
   ↓
2. 工位1开工上报
   → 创建任务：{ StationNo = 0, SN = "WO-2024001" }
   → 写入 PLC 信号到工位1和工位2（同时允许生产）
   ↓
3. 工位1和工位2同时焊接
   → 工位1采集焊点：{ TaskId = 1, StationNo = 1 }
   → 工位2采集焊点：{ TaskId = 1, StationNo = 2 }
   ↓
4. 任意工位完工上报
   → 更新任务：{ EndTime = now }
   → 写入 PLC 信号到工位1和工位2（同时禁止生产）
```

---

### 1.3 双工位双工单模式

```
配置：
EnableDualStation = true
EnableDualWorkOrder = true  ← 关键：独立工单

特点：
- 界面显示两个工位
- 两个工位各自独立工单
- 任务的 StationNo = 1 或 2（各自独立）
- PLC 信号只写入当前工位
```

**界面布局：**
```
┌────────────────────────────────────────────────┐
│  [工位1] [工位2] ← 可切换操作                 │
├────────────────────────────────────────────────┤
│  左侧：当前工位预览                            │
│  右侧：当前工位独立控制面板                    │
└────────────────────────────────────────────────┘
```

**数据库记录：**
```sql
-- 两个独立任务
BizWeldTask: { Id = 1, StationNo = 1, SN = "WO-2024001" }
BizWeldTask: { Id = 2, StationNo = 2, SN = "WO-2024002" }

-- 焊点记录各自关联
BizWeldPointRecord: { TaskId = 1, StationNo = 1, ... }
BizWeldPointRecord: { TaskId = 2, StationNo = 2, ... }
```

**业务流程示例：**
```
1. 工位1获取工单 "WO-2024001"
   工位2获取工单 "WO-2024002"
   ↓
2. 工位1开工上报
   → 创建任务：{ Id = 1, StationNo = 1, SN = "WO-2024001" }
   → 写入 PLC 信号仅到工位1
   ↓
3. 工位2开工上报
   → 创建任务：{ Id = 2, StationNo = 2, SN = "WO-2024002" }
   → 写入 PLC 信号仅到工位2
   ↓
4. 各自独立生产
   → 工位1采集：{ TaskId = 1, StationNo = 1 }
   → 工位2采集：{ TaskId = 2, StationNo = 2 }
   ↓
5. 工位1完工
   → 更新任务1：{ EndTime = now }
   → 写入 PLC 信号仅到工位1（工位2继续生产）
```

---

## 二、工位号的来源和流转

### 2.1 工位号的定义位置

#### 在 MonitorView 中

```csharp
// MonitorView.cs 第166行
private int _viewStationNo = ProductionConstants.Stations.DefaultStationNo;

// MonitorView.cs 第477行
private int CurrentStationNo => NormalizeStationNo(_viewStationNo);

// ProductionConstants.cs
public static class Stations
{
    public const int DefaultStationNo = 1;  // 默认工位号
    public const int SharedStationNo = 0;   // 双工位共享标识
}
```

**解释：**
- `_viewStationNo` 是私有字段，存储当前UI显示的工位号
- `CurrentStationNo` 是属性，规范化后的工位号（1或2）
- 默认值为 1

---

### 2.2 工位号的初始化流程

```
程序启动
  ↓
MonitorView.OnLoad()
  ↓
ConfigureDeviceMode()  ← 读取 AppSettings
  ↓
if (EnableDualStation)
  _viewStationNo = 1 (默认显示工位1)
  显示工位切换器
else
  _viewStationNo = 1 (固定工位1)
  隐藏工位切换器
  ↓
RestoreUnfinishedTask(CurrentStationNo)  ← 恢复未完成任务
```

**关键代码：**
```csharp
// MonitorView.cs 第341行
private void ConfigureDeviceMode()
{
    _dualStationEnabled = _currentSettings.EnableDualStation;
    
    // 控制工位相关UI的显示/隐藏
    tlpStation.Visible = _dualStationEnabled;          // 工位切换面板
    tabsPreview2.Visible = _dualStationEnabled;        // 工位2预览标签
    tabsMetrics2.Visible = _dualStationEnabled;        // 工位2指标标签
    tagStationResult2.Visible = _dualStationEnabled;   // 工位2状态标签
    
    // 如果禁用双工位，重置为默认工位
    if (!_dualStationEnabled && CurrentStationNo != ProductionConstants.Stations.DefaultStationNo)
    {
        _viewStationNo = ProductionConstants.Stations.DefaultStationNo;
    }
    
    // 同步 WeldTaskService 的工位
    if (!_dualStationEnabled
        && _weldTaskService.CurrentState.CurrentStationNo != ProductionConstants.Stations.DefaultStationNo)
    {
        _weldTaskService.SelectStation(ProductionConstants.Stations.DefaultStationNo);
    }
    
    BindStationSelection();  // 绑定工位选择器
    ApplyStationViewMode();  // 应用视图模式
}
```

---

### 2.3 工位号的切换流程

#### 用户操作触发

```
用户点击工位切换器（segmentedStationSwitch）
  ↓
Station_SelectedIndexChanged 事件触发
  ↓
SwitchStationFromUi(stationNo)
  ↓
更新 _viewStationNo
  ↓
恢复该工位未完成任务
  ↓
刷新界面显示
```

**完整代码流程：**
```csharp
// 1. 事件触发（MonitorView.cs 第1091行）
private void Station_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
{
    if (_syncingStationSelection || !_dualStationEnabled)
    {
        return;  // 防止递归触发
    }
    
    var stationNo = Math.Clamp(e.Value + 1, 1, 2);  // 索引0→工位1, 索引1→工位2
    SwitchStationFromUi(stationNo);
}

// 2. 切换工位（MonitorView.cs 第2300行）
private void SwitchStationFromUi(int stationNo)
{
    var normalizedStationNo = Math.Clamp(stationNo, 1, 2);
    if (normalizedStationNo != CurrentStationNo)
    {
        // ✅ 更新视图工位号
        _viewStationNo = normalizedStationNo;
        
        // ✅ 恢复该工位的未完成任务
        _weldTaskService.RestoreUnfinishedTask(normalizedStationNo);
    }
    
    // 刷新界面
    RefreshProductionRuntimeState();        // 刷新工单信息
    RestoreCurrentRuntimeTipState();        // 恢复提示状态
    ApplyAllStationStatuses();              // 应用工位状态
    QueueRefreshSchemePreview(force: true); // 刷新方案预览
    ApplyCurrentRealtimePreviewSnapshot();  // 应用实时预览
    SyncStationSelection();                 // 同步工位选择器
}

// 3. 选择操作工位（MonitorView.cs 第2321行）
private void SelectStationForOperation(int stationNo)
{
    // ✅ 通知 WeldTaskService 当前操作工位
    if (_weldTaskService.CurrentState.CurrentStationNo != stationNo)
    {
        _weldTaskService.SelectStation(stationNo);
    }
}
```

---

### 2.4 工位号的规范化

```csharp
// MonitorView.cs 第546行
private static int NormalizeStationNo(int stationNo) => stationNo == 2 ? 2 : 1;
```

**规则：**
- 输入 2 → 返回 2（工位2）
- 输入其他任何值（0, 1, 3, -1等） → 返回 1（工位1）

**目的：** 防止无效工位号，确保只有1和2两个值

---

### 2.5 工位号在数据流中的传递

```
UI（MonitorView）
  |
  | _viewStationNo (1 或 2)
  ↓
WeldTaskService.SelectStation(stationNo)
  |
  | ProductionRuntimeState.CurrentStationNo
  ↓
创建任务时
  |
  | BizWeldTask.StationNo
  |   - 单工位：1
  |   - 双工位同工单：0（共享）
  |   - 双工位双工单：1 或 2
  ↓
数据库保存
```

---

## 三、关键数据结构

### 3.1 ProductionRuntimeState

```csharp
// ProductionRuntimeState.cs
public class ProductionRuntimeState
{
    // 当前操作工位号
    public int CurrentStationNo { get; private set; } = 1;
    
    // 工位1的状态
    private ProductionStationRuntimeState? _station1;
    
    // 工位2的状态
    private ProductionStationRuntimeState? _station2;
    
    // 获取或创建工位状态
    public ProductionStationRuntimeState GetOrCreateStation(int stationNo)
    {
        if (stationNo == 2)
        {
            _station2 ??= new ProductionStationRuntimeState();
            return _station2;
        }
        
        _station1 ??= new ProductionStationRuntimeState();
        return _station1;
    }
    
    // 切换工位
    public void SelectStation(int stationNo)
    {
        CurrentStationNo = Math.Clamp(stationNo, 1, 2);
    }
}
```

### 3.2 ProductionStationRuntimeState

```csharp
// ProductionStationRuntimeState.cs
public class ProductionStationRuntimeState
{
    public BizWeldTask? ActiveTask { get; set; }          // 当前任务
    public WorkOrderRes? CurrentWorkOrder { get; set; }   // 当前工单
    public ExpItemData? SelectedProcess { get; set; }     // 选中工序
    public ProgramDataRes? SelectedProgram { get; set; }  // 选中程序
    public string MesOperatorNumber { get; set; }         // 操作员工号
    public DateTime UpdatedTime { get; set; }             // 更新时间
}
```

**每个工位独立维护：**
- 工位1有自己的 ActiveTask、CurrentWorkOrder 等
- 工位2有自己的 ActiveTask、CurrentWorkOrder 等
- 通过 `CurrentStationNo` 决定当前操作哪个工位

---

## 四、UI控件与工位的映射

### 4.1 工位切换器

```csharp
// MonitorView.Designer.cs
segmentedStationSwitch  // 工位切换器（Segmented控件）
  - Items[0] = "工位 1"
  - Items[1] = "工位 2"
  - SelectIndex: 0 = 工位1, 1 = 工位2
```

### 4.2 预览表格

```csharp
// 根据工位选择不同的表格
private DataGridView CurrentWeldPreviewGrid 
    => CurrentStationNo == 2 ? dgvPreview2 : dgvPreview1;

private AntdUI.Table CurrentProductHistoryTable 
    => CurrentStationNo == 2 ? tableHistory2 : tableHistory1;

private AntdUI.Table CurrentMetricTable 
    => CurrentStationNo == 2 ? tableMetric2 : tableMetric1;
```

### 4.3 状态标签

```csharp
// 根据工位选择不同的标签
private AntdUI.Label CurrentLivePreviewStatusLabel 
    => CurrentStationNo == 2 ? lblLiveHint2 : lblLiveHint1;

private AntdUI.Tag CurrentLiveResultTag 
    => CurrentStationNo == 2 ? tagLiveResult2 : tagLiveResult1;

// 工位状态标签
tagStationResult1  // 工位1状态
tagStationResult2  // 工位2状态
```

---

## 五、模式切换的影响范围

### 5.1 界面变化

| 元素 | 单工位 | 双工位同工单 | 双工位双工单 |
|------|--------|------------|------------|
| 工位切换器 | 隐藏 | 显示 | 显示 |
| 工位2标签页 | 隐藏 | 显示 | 显示 |
| 工位2指标表 | 隐藏 | 显示 | 显示 |
| 工位2状态标签 | 隐藏 | 显示 | 显示 |

### 5.2 业务逻辑变化

| 场景 | 单工位 | 双工位同工单 | 双工位双工单 |
|------|--------|------------|------------|
| **任务创建** | StationNo=1 | StationNo=0 | StationNo=当前工位 |
| **PLC信号写入** | 仅工位1 | 工位1+2 | 当前工位 |
| **任务查询** | WHERE StationNo=1 | WHERE StationNo=0 | WHERE StationNo=当前工位 |
| **完工范围** | 工位1完工 | 两个工位同时完工 | 当前工位完工 |

### 5.3 数据库影响

**示例对比：**

```sql
-- 单工位模式
INSERT INTO Biz_WeldTask (StationNo, SN, ...) VALUES (1, 'WO-001', ...);
INSERT INTO Biz_WeldPointRecord (TaskId, StationNo, ...) VALUES (1, 1, ...);

-- 双工位同工单模式
INSERT INTO Biz_WeldTask (StationNo, SN, ...) VALUES (0, 'WO-001', ...);  -- 共享标识
INSERT INTO Biz_WeldPointRecord (TaskId, StationNo, ...) VALUES (1, 1, ...);  -- 工位1焊点
INSERT INTO Biz_WeldPointRecord (TaskId, StationNo, ...) VALUES (1, 2, ...);  -- 工位2焊点

-- 双工位双工单模式
INSERT INTO Biz_WeldTask (StationNo, SN, ...) VALUES (1, 'WO-001', ...);  -- 工位1任务
INSERT INTO Biz_WeldTask (StationNo, SN, ...) VALUES (2, 'WO-002', ...);  -- 工位2任务
INSERT INTO Biz_WeldPointRecord (TaskId, StationNo, ...) VALUES (1, 1, ...);
INSERT INTO Biz_WeldPointRecord (TaskId, StationNo, ...) VALUES (2, 2, ...);
```

---

## 六、配置切换操作指南

### 6.1 如何切换工位模式？

**步骤：**
```
1. 打开系统设置界面
   ↓
2. 找到"生产配置"区域
   ↓
3. 修改配置：
   - EnableDualStation: 是否启用双工位
   - EnableDualWorkOrder: 是否启用双工单（仅双工位时有效）
   ↓
4. 保存配置
   ↓
5. 系统自动触发 SettingsService.SettingsChanged 事件
   ↓
6. MonitorView.ConfigureDeviceMode() 重新配置界面
```

### 6.2 配置组合建议

| 设备类型 | EnableDualStation | EnableDualWorkOrder | 说明 |
|---------|------------------|-------------------|------|
| 小型单臂机器人 | false | false | 单工位 |
| 中型双臂机器人 | true | false | 双工位同工单（推荐） |
| 大型流水线 | true | true | 双工位双工单 |

---

## 七、常见问题（FAQ）

### Q1: 如何判断当前是哪种模式？

**A:** 查看 AppSettings 配置或运行时检查：
```csharp
if (!_currentSettings.EnableDualStation)
{
    // 单工位模式
}
else if (!_currentSettings.EnableDualWorkOrder)
{
    // 双工位同工单模式
}
else
{
    // 双工位双工单模式
}
```

### Q2: 工位号为0是什么意思？

**A:** StationNo=0 是双工位同工单模式的特殊标识，表示该任务是两个工位共享的。查询时需要特殊处理：
```csharp
// 查询任务
WHERE StationNo = 0  // 双工位共享任务
OR StationNo = currentStationNo  // 当前工位独立任务
```

### Q3: 切换工位会丢失数据吗？

**A:** 不会。切换工位只是改变UI显示的数据源，不影响数据库。每个工位的任务和数据独立存储。

### Q4: 双工位同工单模式下，两个工位必须同时开工吗？

**A:** 是的。一个工位开工后，PLC信号同时写入两个工位，两个工位同时开始生产。

### Q5: 可以运行时动态切换模式吗？

**A:** 可以，但不建议在生产进行中切换。切换模式后，已有的未完成任务可能无法正确显示。建议在空闲时切换。

---

## 八、总结

### 核心要点

1. **工位号来源：** `_viewStationNo` 私有字段（默认1）
2. **工位号规范化：** 只允许1或2
3. **三种模式：** 由两个配置决定（EnableDualStation + EnableDualWorkOrder）
4. **切换触发：** 用户点击工位切换器 → 事件 → 更新字段 → 刷新界面
5. **数据隔离：** 每个工位独立的 ProductionStationRuntimeState
6. **共享标识：** StationNo=0 表示双工位共享任务

### 关键代码位置

| 功能 | 文件 | 行号 | 方法/字段 |
|------|------|------|----------|
| 工位号字段 | MonitorView.cs | 166 | `_viewStationNo` |
| 当前工位号 | MonitorView.cs | 477 | `CurrentStationNo` |
| 配置设备模式 | MonitorView.cs | 341 | `ConfigureDeviceMode()` |
| 切换工位 | MonitorView.cs | 2300 | `SwitchStationFromUi()` |
| 工位切换事件 | MonitorView.cs | 1091 | `Station_SelectedIndexChanged()` |
| 工位号规范化 | MonitorView.cs | 546 | `NormalizeStationNo()` |
| 工位信号范围 | MonitorView.cs | 493 | `ResolveWorkOrderSignalStations()` |
| 配置定义 | AppSettings.cs | 75,78 | `EnableDualStation`, `EnableDualWorkOrder` |
