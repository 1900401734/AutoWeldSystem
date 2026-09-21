# BizWeldTask 业务实体分析报告

## 一、实体概述

### 1.1 基本定义

**表名：** `Biz_WeldTask` (焊接任务表)

**核心作用：** BizWeldTask 是焊接生产系统的**核心业务实体**，代表一次完整的生产任务周期，从开工到完工的全过程记录。

### 1.2 实体特性

- **主键：** `Id` (自增)
- **字段数量：** 30+ 个字段
- **生命周期：** 从开工创建 → 运行中 → 完工结束
- **作用域：** 工位级别（支持多工位独立生产）

---

## 二、核心字段分组

### 2.1 标识字段

| 字段 | 类型 | 说明 | 业务意义 |
|------|------|------|---------|
| `Id` | int | 主键 | 本地数据库唯一标识 |
| `LocalExpStartId` | string(32) | 本地任务ID | Guid格式，本地生成，始终存在 |
| `ExpStartId` | string(50)? | MES任务ID | MES返回的任务标识，离线时为null |
| `DeviceId` | string(50) | 设备编号 | 标识哪台设备 |
| `StationNo` | int | 工位号 | 支持一台设备多个工位 |

**关键设计：**
- `LocalExpStartId` 本地生成，保证离线时任务可追踪
- `ExpStartId` 在线时由MES分配，离线时为null

### 2.2 工单信息（9个字段）

| 字段 | 说明 | 来源 |
|------|------|------|
| `SN` | 工单号/流转卡号 | MES或手动输入 |
| `ProductNum` | 产品工号 | MES工单 |
| `ProductModel` | 产品型号 | MES工单 |
| `Spec` | 规格 | MES工单 |
| `Batch` | 批次 | MES工单 |
| `ProductName` | 部件名称 | MES工单 |
| `DrawingNo` | 零件图号 | MES工单 |
| `ProcessNo` | 工序号 | MES工序 |
| `ProcessName` | 工序名称 | MES工序 |
| `StartAmount` | 生产数量/工单数量 | MES工序 |
| `ProjectFrom` | 项目来源（TDM/MES） | 系统配置 |

### 2.3 时间和状态字段

| 字段 | 类型 | 说明 | 状态流转 |
|------|------|------|---------|
| `StartTime` | DateTime | 开工时间 | 创建任务时设置 |
| `EndTime` | DateTime? | 完工时间 | 完工时设置，未完工为null |
| `TaskStatus` | string(20) | 任务状态 | Ready → Running → Paused → Completed |
| `UploadStatus` | string(20) | 上传状态 | Pending → Uploading → Uploaded/Failed |

**状态机：**
```
TaskStatus:
  Ready (准备) → Running (运行中) → Paused (暂停) → Completed (已完成)
  
UploadStatus:
  Pending (待上传) → Uploading (上传中) → Uploaded (已上传)
                                      ↓
                                   Failed (失败) → Retrying (重试中)
```

### 2.4 统计字段

| 字段 | 说明 | 来源 | 用途 |
|------|------|------|------|
| `ActualQty` | 实际数量/加工总数 | PLC或手动输入 | 完工上报 |
| `QualifiedQty` | 合格数量 | PLC或手动输入 | 完工上报 |
| `FailedQty` | 不良数量 | PLC或手动输入 | 完工上报 |

**计算关系：** `ActualQty = QualifiedQty + FailedQty`

### 2.5 程序信息

| 字段 | 说明 | 来源 | 关联性 |
|------|------|------|--------|
| `ProgramId` | 程序ID | MES或本地程序库 | 关联 BizProgram 表 |
| `ProgramName` | 程序名称 | MES或本地 | 18位标准格式 |
| `RecipeCode` | 配方编号 | 程序或手动设置 | **关键：下发到PLC** |
| `ProgramContentSnapshot` | 程序内容快照 | 开工时快照 | text类型，保存完整程序 |

**RecipeCode 特殊性：**
- 关联 PLC 配方，标识唯一加工程序
- 开工后下发到 PLC
- 用于配方校验

### 2.6 员工信息

| 字段 | 说明 | 来源 | 时机 |
|------|------|------|------|
| `UserNumber` | 员工编号 | MES验证 | 开工时 |
| `UserName` | 员工姓名 | MES验证 | 开工时 |
| `DeptName` | 部门名称 | MES验证 | 开工时 |
| `TeamName` | 班组名称 | MES验证 | 开工时 |
| `EndOperatorNumber` | 结束人员 | 完工时输入 | 完工时 |

### 2.7 上传和追踪字段

| 字段 | 说明 | 用途 |
|------|------|------|
| `UploadStatus` | 上传状态 | 追踪MES上传进度 |
| `UploadMessage` | 上传消息 | 记录上传结果或错误 |
| `IsOfflineCreated` | 是否离线创建 | 区分在线/离线任务 |

---

## 三、核心业务流程

### 3.1 开工流程（在线）

**方法：** `WeldTaskService.StartAsync()`

```
用户操作：点击"开工上报"
  ↓
1. 验证操作工（MES.ValidateUser）
  ↓
2. 构建开工请求（ExperimentStartReq）
  ↓
3. 调用MES开工接口（MES.StartWork）
  ↓
4. MES返回 ExpStartId
  ↓
5. 创建 BizWeldTask 记录
   - LocalExpStartId = Guid.NewGuid()
   - ExpStartId = MES返回的ID
   - StartTime = DateTime.Now
   - TaskStatus = "Running"
   - UploadStatus = "Uploaded"
   - IsOfflineCreated = false
  ↓
6. 插入数据库
  ↓
7. 更新运行时状态（CurrentState.ActiveTask）
  ↓
8. 下发配方编号到PLC
  ↓
9. 触发 StateChanged 事件
```

**关键代码位置：** `WeldTaskService.cs` 第301行

### 3.2 开工流程（离线/本地）

**方法：** `WeldTaskService.StartLocalAsync()`

```
用户操作：点击"本地开工"
  ↓
1. 用户填写本地工单表单
  ↓
2. 创建本地工单、工序、程序
  ↓
3. 创建 BizWeldTask 记录
   - LocalExpStartId = Guid.NewGuid()
   - ExpStartId = null  ← 关键：离线时为空
   - StartTime = DateTime.Now
   - TaskStatus = "Running"
   - UploadStatus = "Pending"  ← 待上传
   - UploadMessage = "Local task created offline..."
   - IsOfflineCreated = true  ← 标记为离线
  ↓
4. 插入数据库
  ↓
5. 更新运行时状态
  ↓
6. 将开工上报加入上传队列（BizUploadTask）
  ↓
7. 下发配方编号到PLC
  ↓
8. 等待MES恢复后自动重试上传
```

**关键代码位置：** `WeldTaskService.cs` 第378行

### 3.3 完工流程（在线）

**方法：** `WeldTaskService.FinishAsync()`

```
用户操作：点击"完工上报"
  ↓
1. 读取PLC产量数据（可选）
  ↓
2. 用户确认/输入数量
  ↓
3. 构建完工请求（ExperimentEndReq）
  ↓
4. 调用MES完工接口（MES.EndWork）
  ↓
5. 更新 BizWeldTask 记录
   - ActualQty = 实际数量
   - QualifiedQty = 合格数量
   - FailedQty = 不良数量
   - EndTime = DateTime.Now
   - TaskStatus = "Completed"
   - EndOperatorNumber = 操作工
  ↓
6. 更新数据库
  ↓
7. 清空运行时状态（ActiveTask = null）
  ↓
8. 触发 StateChanged 事件
```

**关键代码位置：** `WeldTaskService.cs` 第498行

### 3.4 完工流程（离线/本地）

**方法：** `WeldTaskService.FinishLocalAsync()`

```
用户操作：点击"本地完工"
  ↓
1. 用户输入数量
  ↓
2. 更新 BizWeldTask 记录
   - EndTime = DateTime.Now
   - TaskStatus = "Completed"
   - UploadStatus = "Pending"  ← 待上传
  ↓
3. 将完工上报加入上传队列
  ↓
4. 等待MES恢复后重试
```

---

## 四、任务生命周期状态机

### 4.1 TaskStatus 状态流转

```
[创建] → Ready
         ↓
      Running ⇄ Paused
         ↓
      Completed (终态)
```

**状态说明：**
- **Ready：** 初始状态（较少使用）
- **Running：** 正在生产
- **Paused：** 暂停（通过状态切换）
- **Completed：** 已完工（终态，不再变化）

### 4.2 UploadStatus 状态流转

```
[创建] → Pending
         ↓
      Uploading
         ↓
    Uploaded / Failed
         ↓
      Retrying → Uploaded
```

**状态说明：**
- **Pending：** 待上传（离线创建或MES失败）
- **Uploading：** 上传中
- **Uploaded：** 已上传成功
- **Failed：** 上传失败
- **Retrying：** 重试中
- **Skipped：** 已跳过（不再上传）

### 4.3 在线 vs 离线任务对比

| 特性 | 在线任务 | 离线任务 |
|------|---------|---------|
| `ExpStartId` | MES分配（有值） | null |
| `IsOfflineCreated` | false | true |
| `UploadStatus` | Uploaded（开工时） | Pending（待上传） |
| `UploadMessage` | MES响应 | "Local task created offline..." |
| 上传队列 | 不需要 | 需要（BizUploadTask） |
| MES同步 | 实时 | 延迟（网络恢复后） |

---

## 五、与其他实体的关系

### 5.1 BizWeldPointRecord（焊点记录）

**关系：** 一对多（一个任务包含多个焊点）

```
BizWeldTask (1) ─┬─→ BizWeldPointRecord (N)
                 │
                 ├─→ 焊点1
                 ├─→ 焊点2
                 └─→ 焊点N
```

**关联字段：**
- `BizWeldPointRecord.TaskId` → `BizWeldTask.Id`
- `BizWeldPointRecord.StationNo` → `BizWeldTask.StationNo`

**业务逻辑：**
- 焊点采集时创建 `BizWeldPointRecord`
- 关联到当前活动的 `BizWeldTask`
- 完工时，所有焊点记录一起上传

### 5.2 BizProgram（程序管理）

**关系：** 多对一（多个任务可使用同一程序）

```
BizWeldTask (N) ─→ BizProgram (1)
```

**关联字段：**
- `BizWeldTask.ProgramId` → `BizProgram.Id`
- `BizWeldTask.RecipeCode` → `BizProgram.RecipeCode`

**业务逻辑：**
- 开工前选择程序
- 将程序内容快照到 `ProgramContentSnapshot`
- `RecipeCode` 用于PLC配方匹配

### 5.3 BizUploadTask（上传任务）

**关系：** 一对多（一个任务可能有多个上传任务）

```
BizWeldTask (1) ─┬─→ BizUploadTask (开工上报)
                 ├─→ BizUploadTask (完工上报)
                 ├─→ BizUploadTask (工艺参数)
                 └─→ BizUploadTask (报告文件)
```

**关联字段：**
- `BizUploadTask.TaskId` → `BizWeldTask.Id`

**上传类型：**
1. **StartReport** - 开工上报（离线时）
2. **FinishReport** - 完工上报（离线或失败时）
3. **ProcessParameter** - 焊点工艺参数
4. **ReportFile** - 生产报告文件

### 5.4 ProductionRuntimeState（运行时状态）

**关系：** 引用关系（内存中的当前任务）

```
ProductionRuntimeState.ActiveTask → BizWeldTask (当前活动任务)
```

**业务逻辑：**
- 开工时设置 `ActiveTask`
- 完工时清空 `ActiveTask`
- 切换工位时切换 `ActiveTask`

---

## 六、关键业务场景

### 6.1 场景1：正常在线生产

```
时间线：

T0: 用户扫描工单
    └─> MES返回工单信息

T1: 用户点击"开工上报"
    └─> 创建 BizWeldTask
    └─> ExpStartId = MES返回的ID
    └─> TaskStatus = Running
    └─> UploadStatus = Uploaded

T2: PLC焊接，采集焊点数据
    └─> 创建多条 BizWeldPointRecord
    └─> TaskId = BizWeldTask.Id

T3: 用户点击"完工上报"
    └─> 更新 BizWeldTask
    └─> EndTime = DateTime.Now
    └─> TaskStatus = Completed
    └─> 上传焊点数据到MES

T4: 任务完成
    └─> ActiveTask = null
```

### 6.2 场景2：离线生产（网络断开）

```
时间线：

T0: MES网络断开

T1: 用户点击"本地开工"
    └─> 创建 BizWeldTask
    └─> ExpStartId = null  ← 无MES ID
    └─> IsOfflineCreated = true
    └─> UploadStatus = Pending
    └─> 创建 BizUploadTask (StartReport)

T2: PLC焊接，采集焊点数据
    └─> 创建多条 BizWeldPointRecord
    └─> 数据暂存本地

T3: 用户点击"本地完工"
    └─> 更新 BizWeldTask
    └─> EndTime = DateTime.Now
    └─> TaskStatus = Completed
    └─> 创建 BizUploadTask (FinishReport)

T4: MES网络恢复
    └─> 自动重试上传队列
    └─> 开工上报成功，获得 ExpStartId
    └─> 更新 BizWeldTask.ExpStartId
    └─> 完工上报成功
    └─> 焊点数据上传成功
    └─> UploadStatus = Uploaded
```

### 6.3 场景3：双工位并行生产

```
工位1：
  └─> BizWeldTask (StationNo=1, TaskStatus=Running)
      └─> 生产产品A

工位2：
  └─> BizWeldTask (StationNo=2, TaskStatus=Running)
      └─> 生产产品B

特点：
- 两个任务独立
- 各自维护 ActiveTask
- 数据库中同时存在两条 EndTime=null 的记录
```

### 6.4 场景4：配方编号下发

```
开工后：
  ↓
1. 从 BizWeldTask.RecipeCode 读取配方编号
  ↓
2. 写入 PLC 的 PC 配方地址
  ↓
3. 可选：校验 PLC 配方地址
  ↓
4. 记录到生产流程日志
```

---

## 七、数据库操作模式

### 7.1 创建（INSERT）

**位置：**
- `WeldTaskService.StartAsync()` - 在线开工
- `WeldTaskService.StartLocalAsync()` - 离线开工

**代码模式：**
```csharp
var task = new BizWeldTask { ... };
task = _dbContext.Db.Insertable(task).ExecuteReturnEntity();
```

### 7.2 查询（SELECT）

**常见查询模式：**

1. **查询未完工任务：**
```csharp
_dbContext.Db.Queryable<BizWeldTask>()
    .Where(task => task.TaskStatus != "Completed" && task.EndTime == null)
    .Where(task => task.StationNo == stationNo)
    .First();
```

2. **按ID查询：**
```csharp
_dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId);
```

3. **查询待上传任务：**
```csharp
_dbContext.Db.Queryable<BizWeldTask>()
    .Where(task => task.UploadStatus == "Pending")
    .ToList();
```

### 7.3 更新（UPDATE）

**常见更新场景：**

1. **完工更新：**
```csharp
task.ActualQty = actualQty;
task.QualifiedQty = qualifiedQty;
task.FailedQty = failedQty;
task.EndTime = DateTime.Now;
task.TaskStatus = "Completed";
task.UploadStatus = "Uploaded";
_dbContext.Db.Updateable(task).ExecuteCommand();
```

2. **上传状态更新：**
```csharp
task.UploadStatus = "Uploaded";
task.UploadMessage = "Success";
_dbContext.Db.Updateable(task).ExecuteCommand();
```

3. **ExpStartId 补充（离线任务上传成功后）：**
```csharp
task.ExpStartId = mesResponse.ExpStartId;
_dbContext.Db.Updateable(task).ExecuteCommand();
```

### 7.4 删除（DELETE）

**通常不删除：**
- 任务记录作为历史数据保留
- 只更新状态，不物理删除

---

## 八、使用该实体的服务和组件

### 8.1 核心服务

| 服务 | 文件 | 主要操作 |
|------|------|---------|
| **WeldTaskService** | WeldTaskService.cs | 创建、更新、查询任务 |
| **UploadTaskService** | UploadTaskService.cs | 上传离线任务到MES |
| **ProductCycleCollectionService** | ProductCycleCollectionService.cs | 焊点采集时关联任务 |
| **ProductHistoryService** | ProductHistoryService.cs | 查询任务的产品历史 |
| **UploadStatusSummaryService** | UploadStatusSummaryService.cs | 汇总任务上传状态 |
| **ProductionReportFileService** | ProductionReportFileService.cs | 生成任务报告文件 |

### 8.2 UI组件

| 组件 | 文件 | 使用方式 |
|------|------|---------|
| **MonitorView** | MonitorView.cs | 显示当前任务、触发开工/完工 |
| **DataManageView** | (未列出) | 查询历史任务 |

### 8.3 运行时状态

| 状态类 | 用途 |
|--------|------|
| **ProductionRuntimeState** | 维护当前所有工位的 ActiveTask |
| **ProductionStationRuntimeState** | 单工位的运行时状态 |

---

## 九、设计亮点

### 9.1 离线支持

**LocalExpStartId + ExpStartId 双ID设计：**
- `LocalExpStartId` 本地生成，保证离线可追踪
- `ExpStartId` MES分配，在线时存在
- 离线任务先用本地ID，上传成功后补充MES ID

**优点：**
- 网络断开不影响生产
- 数据可追溯
- 自动同步

### 9.2 工位隔离

**StationNo 工位号设计：**
- 支持一台设备多个工位
- 每个工位独立任务
- 数据隔离清晰

### 9.3 程序快照

**ProgramContentSnapshot：**
- 开工时快照程序内容
- 防止程序被修改影响历史数据
- 可追溯生产时使用的具体程序

### 9.4 状态机设计

**TaskStatus + UploadStatus 双状态机：**
- `TaskStatus` 管理生产流程
- `UploadStatus` 管理数据同步
- 互不干扰，清晰明确

---

## 十、代码位置速查

| 功能 | 文件 | 行号/方法 |
|------|------|----------|
| 实体定义 | BizWeldTask.cs | 全文 |
| 在线开工 | WeldTaskService.cs | 301行 StartAsync() |
| 离线开工 | WeldTaskService.cs | 378行 StartLocalAsync() |
| 在线完工 | WeldTaskService.cs | 498行 FinishAsync() |
| 离线完工 | WeldTaskService.cs | 约600行 FinishLocalAsync() |
| 查询未完工任务 | WeldTaskService.cs | 75行 GetUnfinishedTask() |
| 恢复未完工任务 | WeldTaskService.cs | 约90行 RestoreUnfinishedTask() |
| 重试上传 | WeldTaskService.cs | 约650行 RetryPendingUploadsAsync() |

---

## 十一、总结

### BizWeldTask 的核心价值

1. **生产任务的中心枢纽**
   - 串联工单、程序、员工、焊点数据
   - 管理任务完整生命周期
   - 支持在线/离线双模式

2. **数据追溯的锚点**
   - 所有焊点记录关联到任务
   - 程序内容快照保留
   - 上传状态可查

3. **MES集成的桥梁**
   - 开工/完工上报
   - 离线数据同步
   - 状态双向同步

4. **多工位协调的基础**
   - 工位隔离
   - 独立任务管理
   - 状态独立维护

**一句话概括：**
> BizWeldTask 是焊接生产系统的**核心业务实体**，代表从开工到完工的一次完整生产任务，负责管理工单信息、程序关联、数据采集、MES同步的全流程，支持在线/离线双模式和多工位并行生产。
