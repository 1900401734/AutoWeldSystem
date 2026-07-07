### 阶段 2：中期优化（3-4周）🔧 中优先级

#### 优化 2.1：分离基础设施服务

**目标：** 创建 Infrastructure 项目，迁移非业务服务

**步骤：**

1. **创建 Infrastructure 项目**
```bash
dotnet new classlib -n AutoWeldSystem.Infrastructure
dotnet add AutoWeldSystem.Infrastructure reference AutoWeldSystem.Core
```

2. **迁移 PLC 服务**
```
Services/Plc/  →  Infrastructure/ExternalServices/Plc/
```

迁移的服务：
- PlcCommunicationService
- PlcProductionMonitorService
- PlcWeldCycleMonitorService
- PlcWorkIdMonitorService
- PlcBusinessSignalService

3. **迁移 MES 服务**
```
Services/Mes/  →  Infrastructure/ExternalServices/Mes/
```

4. **迁移日志服务**
```
Services/Log/  →  Infrastructure/Logging/
```

**统一日志接口：**
```csharp
// Infrastructure/Logging/ILoggerService.cs
public interface ILoggerService
{
    void LogInformation(string message, Dictionary<string, object>? context = null);
    void LogWarning(string message, Dictionary<string, object>? context = null);
    void LogError(Exception exception, string message, Dictionary<string, object>? context = null);
    void LogBusiness(string source, string message, string detail, Dictionary<string, object>? context = null);
}

// 统一实现，替代4个独立日志服务
public class LoggerService : ILoggerService
{
    // 内部路由到不同的日志文件
    // - 操作日志 → operation.log
    // - 异常日志 → exception.log
    // - 生产流程 → production.log
    // - MES交互 → mes.log
}
```

**工作量：** 7-10 天

**优点：**
- 职责清晰分离
- 日志服务统一
- 易于替换实现

**风险：** 中 - 需要更新依赖注入配置

---

#### 优化 2.2：简化 MonitorView（拆分视图）

**当前问题：**
- MonitorView.cs 有 5000+ 行代码
- 违反单一职责原则
- 难以维护和测试

**解决方案：** 采用 MVVM 模式 + Presenter 模式

**新结构：**
```
UI/
├── Views/
│   ├── MonitorView.cs                    # 主视图 (< 500 行)
│   ├── Controls/
│   │   ├── WorkOrderPanel.cs             # 工单信息面板
│   │   ├── ProductionMetricsPanel.cs     # 生产指标面板
│   │   ├── ProductHistoryPanel.cs        # 产品历史面板
│   │   └── RuntimeStatusPanel.cs         # 运行状态面板
│   └── Presenters/
│       ├── MonitorPresenter.cs           # 主视图逻辑
│       ├── WorkOrderPresenter.cs         # 工单逻辑
│       └── ProductionPresenter.cs        # 生产逻辑
└── ViewModels/
    ├── MonitorViewModel.cs
    ├── WorkOrderViewModel.cs
    └── ProductionViewModel.cs
```

**示例拆分：**
```csharp
// MonitorView.cs (简化后)
public partial class MonitorView : BaseView
{
    private readonly MonitorPresenter _presenter;
    
    public MonitorView(MonitorPresenter presenter)
    {
        _presenter = presenter;
        InitializeComponent();
        _presenter.Initialize(this);
    }
    
    // 仅保留UI交互代码
    private void StartButton_Click(object sender, EventArgs e)
    {
        _presenter.StartProduction();
    }
}

// MonitorPresenter.cs (业务逻辑)
public class MonitorPresenter
{
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductionFlowLogService _logService;
    private MonitorView _view;
    
    public void Initialize(MonitorView view)
    {
        _view = view;
        LoadCurrentTask();
    }
    
    public async Task StartProduction()
    {
        try
        {
            var task = await _weldTaskService.StartAsync(...);
            _view.UpdateTaskDisplay(task);
        }
        catch (Exception ex)
        {
            _view.ShowError(ex.Message);
        }
    }
}
```

**工作量：** 10-14 天

**优点：**
- 每个文件 < 500 行
- 职责清晰
- 易于测试 Presenter
- 易于维护

---

#### 优化 2.3：引入领域事件（可选）

**目标：** 解耦服务间的依赖

**当前问题：**
```csharp
// WeldTaskService 直接调用其他服务
public async Task FinishAsync(...)
{
    // 完工逻辑
    task.EndTime = DateTime.Now;
    _dbContext.Db.Updateable(task).ExecuteCommand();
    
    // 直接调用上传服务
    await _uploadTaskService.EnqueueFinishReportAsync(task);
    
    // 直接调用日志服务
    _logService.Write("FinishReport", ...);
}
```

**问题：**
- WeldTaskService 依赖太多服务
- 难以测试
- 违反单一职责

**解决方案：领域事件**
```csharp
// Domain/Events/TaskFinishedEvent.cs
public class TaskFinishedEvent : IDomainEvent
{
    public BizWeldTask Task { get; }
    public DateTime OccurredAt { get; }
    
    public TaskFinishedEvent(BizWeldTask task)
    {
        Task = task;
        OccurredAt = DateTime.Now;
    }
}

// WeldTaskService (简化)
public async Task FinishAsync(...)
{
    task.EndTime = DateTime.Now;
    await _weldTaskRepository.UpdateAsync(task);
    
    // 发布事件
    await _eventBus.PublishAsync(new TaskFinishedEvent(task));
}

// 事件处理器（独立）
public class TaskFinishedEventHandler : IEventHandler<TaskFinishedEvent>
{
    private readonly IUploadTaskService _uploadTaskService;
    private readonly ILogService _logService;
    
    public async Task HandleAsync(TaskFinishedEvent @event)
    {
        // 处理上传
        await _uploadTaskService.EnqueueFinishReportAsync(@event.Task);
        
        // 记录日志
        _logService.Write("FinishReport", ...);
    }
}
```

**工作量：** 5-7 天

**优点：**
- 解耦服务
- 易于扩展
- 易于测试

**风险：** 中 - 需要引入事件总线

---

### 阶段 3：长期重构（2-3个月）🏗️ 低优先级

#### 重构 3.1：完全迁移到 Clean Architecture

**步骤：**

1. **创建 Domain 层**
   - 从 Core 迁移 Entities
   - 创建聚合根（如 WeldTaskAggregate）
   - 定义领域服务

2. **创建 Application 层**
   - 从 Services 迁移业务服务
   - 从 Core 迁移 DTOs
   - 可选：引入 CQRS（Command/Query分离）

3. **重构 Infrastructure 层**
   - 完善 Repository 实现
   - 整合外部服务
   - 统一日志和配置

4. **简化 UI 层**
   - 移除对 Data 的依赖
   - 拆分大型视图
   - 引入 MVVM/MVP 模式

**完整依赖关系：**
```
┌────────────────────────────────────────────────────┐
│              Clean Architecture                     │
└────────────────────────────────────────────────────┘

    UI (WinForms)
         │
         │ 依赖
         ▼
    Application ─────────┐
         │               │
         │ 依赖          │ 使用
         ▼               ▼
      Domain ◄──── Infrastructure
    (核心领域)      (数据+外部服务)

依赖规则：
✓ UI → Application (允许)
✓ Application → Domain (允许)
✓ Infrastructure → Domain (允许，实现接口)
✗ Domain → Infrastructure (禁止)
✗ Domain → Application (禁止)
✗ UI → Infrastructure (禁止)
```

**工作量：** 40-60 天（团队协作）

---

#### 重构 3.2：引入 CQRS（可选）

**适用场景：**
- 读写分离
- 复杂查询优化
- 高并发场景

**示例：**
```
Application/
├── Commands/              # 写操作
│   ├── StartProductionCommand.cs
│   ├── StartProductionCommandHandler.cs
│   ├── FinishProductionCommand.cs
│   └── FinishProductionCommandHandler.cs
└── Queries/               # 读操作
    ├── GetWorkOrderQuery.cs
    ├── GetWorkOrderQueryHandler.cs
    ├── GetProductionHistoryQuery.cs
    └── GetProductionHistoryQueryHandler.cs
```

**优点：**
- 读写分离
- 性能优化
- 职责清晰

**缺点：**
- 复杂度增加
- 学习曲线陡峭

**建议：** 目前不需要，除非有明确的性能瓶颈

---

#### 重构 3.3：引入自动化测试

**测试金字塔：**
```
        /\        单元测试 (70%)
       /  \       - Domain 层
      /────\      - Application 层
     /      \
    /────────\    集成测试 (20%)
   /          \   - Repository
  /────────────\  - 外部服务 Mock
 /              \
/________________\ E2E 测试 (10%)
                   - 关键业务流程
```

**优先级：**

1. **Domain 层单元测试**（最高）
```csharp
[TestClass]
public class BizWeldTaskTests
{
    [TestMethod]
    public void BizWeldTask_Should_Be_Finished_When_EndTime_Set()
    {
        // Arrange
        var task = new BizWeldTask 
        { 
            StartTime = DateTime.Now,
            TaskStatus = "Running"
        };
        
        // Act
        task.EndTime = DateTime.Now;
        task.TaskStatus = "Completed";
        
        // Assert
        Assert.IsTrue(task.IsFinished);
    }
}
```

2. **Application 层单元测试**
```csharp
[TestClass]
public class WeldTaskServiceTests
{
    [TestMethod]
    public async Task StartAsync_Should_Create_Task_With_ExpStartId()
    {
        // Arrange
        var mockRepository = new Mock<IWeldTaskRepository>();
        var mockMesProvider = new Mock<IMesProvider>();
        mockMesProvider.Setup(x => x.StartWorkAsync(It.IsAny<ExperimentStartReq>()))
            .ReturnsAsync(new BasicRes<ExperimentStartRes> 
            { 
                Data = new ExperimentStartRes { ExpStartId = "MES123" }
            });
        
        var service = new WeldTaskService(mockRepository.Object, mockMesProvider.Object);
        
        // Act
        var task = await service.StartAsync("EMP001", 100, 1);
        
        // Assert
        Assert.AreEqual("MES123", task.ExpStartId);
        mockRepository.Verify(x => x.AddAsync(It.IsAny<BizWeldTask>()), Times.Once);
    }
}
```

3. **Repository 集成测试**
```csharp
[TestClass]
public class WeldTaskRepositoryIntegrationTests
{
    private SqlSugarDbContext _context;
    private WeldTaskRepository _repository;
    
    [TestInitialize]
    public void Setup()
    {
        // 使用内存数据库或测试数据库
        _context = new SqlSugarDbContext("test_connection_string");
        _repository = new WeldTaskRepository(_context);
    }
    
    [TestMethod]
    public async Task GetUnfinishedTaskAsync_Should_Return_Task_With_Null_EndTime()
    {
        // Arrange
        await _repository.AddAsync(new BizWeldTask 
        { 
            StationNo = 1, 
            EndTime = null, 
            TaskStatus = "Running" 
        });
        
        // Act
        var task = await _repository.GetUnfinishedTaskAsync(1);
        
        // Assert
        Assert.IsNotNull(task);
        Assert.AreEqual(1, task.StationNo);
    }
}
```

**工作量：** 20-30 天

**测试覆盖率目标：**
- Domain: > 80%
- Application: > 70%
- Infrastructure: > 50%

---

## 五、重构路线图（时间表）

### 📅 第1-2周：紧急修复
```
Week 1:
  Day 1-3: 解耦 UI → Data 依赖
  Day 4-5: 创建 Repository 接口

Week 2:
  Day 1-3: 实现 Repository 类
  Day 4-5: 修改 Services 使用 Repository
```

### 📅 第3-6周：中期优化
```
Week 3:
  Day 1-2: 创建 Infrastructure 项目
  Day 3-5: 迁移 PLC 服务

Week 4:
  Day 1-2: 迁移 MES 服务
  Day 3-5: 统一日志服务

Week 5-6:
  拆分 MonitorView
  - 提取 WorkOrderPanel
  - 提取 ProductionMetricsPanel
  - 提取 ProductHistoryPanel
  - 创建 Presenters
```

### 📅 第7-12周：长期重构（可选）
```
Week 7-8:
  创建 Domain 层
  迁移实体和领域逻辑

Week 9-10:
  创建 Application 层
  迁移应用服务

Week 11-12:
  完善 Infrastructure
  引入自动化测试
```

---

## 六、风险评估与缓解策略

### 风险矩阵

| 风险 | 可能性 | 影响 | 级别 | 缓解策略 |
|------|--------|------|------|----------|
| 重构引入新 Bug | 高 | 高 | 🔴 高 | 增量重构 + 回归测试 |
| 团队学习曲线 | 中 | 中 | 🟡 中 | 培训 + 代码审查 |
| 进度延期 | 中 | 中 | 🟡 中 | 分阶段实施，每阶段可独立交付 |
| 性能下降 | 低 | 中 | 🟢 低 | 性能基准测试 |
| 依赖冲突 | 低 | 低 | 🟢 低 | 使用 .NET 标准库 |

### 缓解措施

1. **增量重构**
   - 不要大爆炸式重构
   - 每次只改一个模块
   - 保持系统可运行

2. **回归测试**
   - 每次修改后运行完整测试
   - 建立自动化测试套件
   - 手工测试关键路径

3. **代码审查**
   - 所有改动必须 Code Review
   - 至少2人审查重要改动
   - 记录设计决策

4. **分支策略**
   ```
   main (生产)
     │
     ├─ develop (开发)
     │   │
     │   ├─ feature/repository-pattern
     │   ├─ feature/infrastructure-separation
     │   └─ feature/monitorview-refactor
     │
     └─ release/v2.0 (重构版本)
   ```

5. **回滚计划**
   - 每个阶段完成后打 Tag
   - 保留原有代码分支
   - 准备快速回滚脚本

---

## 七、成本效益分析

### 投入成本

| 阶段 | 时间 | 人力 | 成本（假设） |
|------|------|------|-------------|
| 阶段1 (紧急) | 2周 | 2人 | 80小时 |
| 阶段2 (中期) | 4周 | 2人 | 160小时 |
| 阶段3 (长期) | 8周 | 2-3人 | 400小时 |
| **总计** | **14周** | **2-3人** | **640小时** |

### 预期收益

| 收益类型 | 短期 (3个月) | 长期 (1年+) |
|---------|-------------|------------|
| **可维护性** | +30% | +60% |
| **开发效率** | +15% | +40% |
| **Bug 率** | -20% | -50% |
| **测试覆盖率** | +30% | +70% |
| **新功能交付** | 持平 | +35% |

### ROI 计算

**假设：**
- 当前每个 Bug 修复成本：4小时
- 当前每月 Bug 数量：20个
- 重构后 Bug 减少：50%

**年度节省：**
```
节省 = (20 bugs/月 × 50% 减少 × 4小时/bug × 12个月)
     = 480 小时/年
```

**投资回收期：**
```
回收期 = 640小时投入 / 480小时年度节省
       = 1.3 年
```

**结论：** 投资回报率为正，建议执行重构。

---

## 八、架构决策记录（ADR）

### ADR-001: 采用 Clean Architecture

**状态：** 提议

**上下文：**
- 当前架构存在 UI→Data 直接依赖
- Services 层职责混乱
- 缺少测试

**决策：**
采用 Clean Architecture，分离 Domain、Application、Infrastructure

**后果：**
- ✅ 依赖方向清晰
- ✅ 易于测试
- ✅ 易于维护
- ❌ 增加复杂度
- ❌ 需要团队学习

---

### ADR-002: 引入 Repository 模式

**状态：** 提议

**上下文：**
- 数据访问逻辑散落各处
- 紧耦合 SqlSugar
- 难以测试

**决策：**
引入 Repository 模式封装数据访问

**后果：**
- ✅ 数据访问集中
- ✅ 易于 Mock 测试
- ✅ 易于切换 ORM
- ❌ 增加抽象层

---

### ADR-003: 分离基础设施服务

**状态：** 提议

**上下文：**
- PLC、MES、Log 服务在 Services 层
- 职责不清晰

**决策：**
创建 Infrastructure 项目，迁移非业务服务

**后果：**
- ✅ 职责清晰
- ✅ 易于替换实现
- ❌ 项目数量增加

---

## 九、总结与建议

### 核心建议

1. **立即执行阶段1（紧急修复）**
   - 解耦 UI→Data 依赖
   - 引入 Repository 模式
   - 工作量：2周
   - 风险：低

2. **计划执行阶段2（中期优化）**
   - 分离基础设施服务
   - 拆分 MonitorView
   - 工作量：4周
   - 风险：中

3. **评估阶段3（长期重构）**
   - 完全迁移到 Clean Architecture
   - 根据团队能力和项目需求决定
   - 工作量：8周
   - 风险：中

### 优先级排序

**必须做（高优先级）：**
1. 解耦 UI→Data 依赖 ⚡
2. 创建 Repository 抽象 ⚡
3. 分离基础设施服务 🔧

**应该做（中优先级）：**
4. 拆分 MonitorView 🔧
5. 统一日志服务 🔧
6. 引入自动化测试 🏗️

**可以做（低优先级）：**
7. 完全迁移 Clean Architecture 🏗️
8. 引入 CQRS 🏗️
9. 引入领域事件 🏗️

### 最终评价

**当前架构评分：** ⭐⭐⭐☆☆ (3/5)

**重构后预期评分：** ⭐⭐⭐⭐☆ (4/5)

**建议：** 分阶段执行重构，从紧急修复开始，逐步优化到推荐架构。

---

## 附录

### 附录 A：参考资料

1. **Clean Architecture**
   - Clean Architecture: A Craftsman's Guide to Software Structure and Design (Robert C. Martin)
   - https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html

2. **Repository 模式**
   - Patterns of Enterprise Application Architecture (Martin Fowler)
   - https://martinfowler.com/eaaCatalog/repository.html

3. **.NET 架构最佳实践**
   - https://learn.microsoft.com/en-us/dotnet/architecture/

### 附录 B：代码审查清单

**架构层面：**
- [ ] 依赖方向是否正确（内向依赖）
- [ ] 是否有跨层依赖
- [ ] 接口是否在正确的层级
- [ ] 是否有循环依赖

**代码层面：**
- [ ] 单一职责原则
- [ ] 开闭原则
- [ ] 依赖倒置原则
- [ ] 接口隔离原则
- [ ] 里氏替换原则

**测试层面：**
- [ ] 是否可测试
- [ ] 是否有单元测试
- [ ] 测试覆盖率是否达标

---

**报告完成日期：** 2026年6月15日  
**评审人签名：** 资深 .NET 架构师  
**下次评审日期：** 重构完成后
