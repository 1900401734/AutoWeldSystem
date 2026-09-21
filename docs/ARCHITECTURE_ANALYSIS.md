# AutoWeldSystem 架构分析与标准化建议

## 当前架构

### 项目结构

```
AutoWeldSystem/
├── AutoWeldSystem.Core          # 核心层（无依赖）
├── AutoWeldSystem.Data          # 数据访问层（依赖 Core）
├── AutoWeldSystem.Services      # 服务/业务逻辑层（依赖 Core, Data）
├── AutoWeldSystem.UI            # 表示层/UI层（依赖 Core, Data, Services）
└── AutoWeldSystem.Libs          # 第三方库
```

### 依赖关系图

```
┌─────────────────┐
│  AutoWeldSystem.UI  │
│   (WinForms)    │
└────────┬────────┘
         │ 依赖
         ▼
┌─────────────────┐
│  Services       │
│  (业务逻辑)     │
└────────┬────────┘
         │ 依赖
         ▼
┌─────────────────┐
│  Data           │
│  (数据访问)     │
└────────┬────────┘
         │ 依赖
         ▼
┌─────────────────┐
│  Core           │
│  (领域核心)     │
└─────────────────┘
```

### 各层职责分析

#### 1. **AutoWeldSystem.Core** ✅ 设计良好
- **当前内容**：
  - Constants（常量）
  - DTOs（数据传输对象）
  - Entities（实体类）
  - Enums（枚举）
  - Exceptions（自定义异常）
  - Interfaces（服务接口）
  - Models（领域模型）
  - ViewModels（视图模型）
  - GlobalContext（全局上下文）

- **符合标准**：作为核心领域层，无任何外部依赖，包含业务模型和契约
- **问题**：无明显问题

#### 2. **AutoWeldSystem.Data** ✅ 职责清晰
- **当前内容**：
  - SqlSugarDbContext（数据库上下文）

- **符合标准**：纯数据访问层，封装 ORM 操作
- **问题**：过于简单，所有 Repository 逻辑可能都在 Services 层

#### 3. **AutoWeldSystem.Services** ⚠️ 职责混杂
- **当前内容**：
  - 业务服务（AppSettingsService, ProgramManageService, RbacService...）
  - PLC 通信服务（Plc/）
  - MES 集成服务（Mes/）
  - 日志服务（Log/）
  - 生产流程服务（Production/）

- **问题**：
  1. **基础设施服务混在业务层**：PLC 通信、MES 集成应该在独立的基础设施层
  2. **服务直接访问 Data 层**：缺少 Repository 抽象
  3. **日志服务应该在基础设施层**

#### 4. **AutoWeldSystem.UI** ⚠️ 关注点分离不足
- **当前内容**：
  - Forms/Views（窗体）
  - Components/Controls（自定义控件）
  - Infrastructure（UI基础设施？）
  - Program.cs（启动和依赖注入）

- **问题**：
  1. **直接依赖 Data 层**：UI 不应该直接访问数据层
  2. **Infrastructure 目录命名混淆**：与基础设施层概念冲突
  3. **业务逻辑可能泄漏到 UI**：需要检查 View 中的逻辑复杂度

#### 5. **AutoWeldSystem.Libs**
- **当前内容**：HslCommunication.dll（第三方 PLC 通信库）
- **问题**：应该通过 NuGet 管理，或者有明确的原因说明为何本地引用

---

## 标准化架构建议

### 推荐的分层架构（Clean Architecture / Onion Architecture）

```
┌──────────────────────────────────────────┐
│           Presentation Layer             │
│  AutoWeldSystem.UI (WinForms)            │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│         Application Layer                │
│  AutoWeldSystem.Application              │
│  - Services (Application Services)       │
│  - DTOs, Commands, Queries               │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│           Domain Layer                   │
│  AutoWeldSystem.Domain                   │
│  - Entities, Aggregates                  │
│  - Domain Services                       │
│  - Domain Events                         │
│  - Repository Interfaces                 │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│       Infrastructure Layer               │
│  AutoWeldSystem.Infrastructure           │
│  - Data (SqlSugar, Repositories)         │
│  - ExternalServices (MES, PLC)           │
│  - Logging                               │
└──────────────────────────────────────────┘
```

### 重构建议

#### 方案一：渐进式重构（推荐，风险低）

**保持现有结构，局部调整：**

1. **创建 Repository 层**
   - 在 `AutoWeldSystem.Data` 中创建 `Repositories/` 目录
   - 在 `AutoWeldSystem.Core` 中定义 `IRepository<T>` 接口
   - Services 通过 Repository 访问数据，而不是直接使用 `SqlSugarDbContext`

2. **分离基础设施服务**
   - 创建 `AutoWeldSystem.Infrastructure` 项目
   - 移动 `Services/Plc/`, `Services/Mes/`, `Services/Log/` 到 Infrastructure
   - 保留纯业务逻辑在 Services 层

3. **解耦 UI 对 Data 的依赖**
   - 移除 `AutoWeldSystem.UI` 对 `AutoWeldSystem.Data` 的直接引用
   - UI 只依赖 `Services` 和 `Core`

4. **重命名 UI.Infrastructure**
   - 改名为 `UI/Helpers` 或 `UI/Common`，避免与基础设施层混淆

**调整后的依赖关系：**
```
UI → Services → Data → Core
      ↓
Infrastructure → Core
```

#### 方案二：完全重构（理想，但工作量大）

**完全按照 Clean Architecture 重新组织：**

```
AutoWeldSystem/
├── AutoWeldSystem.Domain           # 领域层（核心业务逻辑，无依赖）
│   ├── Entities/                  # 实体（从 Core.Entities 迁移）
│   ├── ValueObjects/              # 值对象
│   ├── Aggregates/                # 聚合根
│   ├── DomainServices/            # 领域服务
│   ├── Events/                    # 领域事件
│   └── Interfaces/                # Repository 接口
│
├── AutoWeldSystem.Application      # 应用层（用例编排）
│   ├── Services/                  # 应用服务（从 Services 迁移）
│   ├── DTOs/                      # 数据传输对象（从 Core.DTOs 迁移）
│   ├── Commands/                  # 命令（CQRS）
│   ├── Queries/                   # 查询（CQRS）
│   └── Interfaces/                # 应用服务接口
│
├── AutoWeldSystem.Infrastructure   # 基础设施层（外部依赖）
│   ├── Data/                      # 数据访问（SqlSugar，从 Data 迁移）
│   │   ├── Repositories/          # Repository 实现
│   │   └── SqlSugarDbContext.cs
│   ├── ExternalServices/          # 外部服务
│   │   ├── Plc/                   # PLC 通信（从 Services.Plc 迁移）
│   │   └── Mes/                   # MES 集成（从 Services.Mes 迁移）
│   ├── Logging/                   # 日志（从 Services.Log 迁移）
│   └── Configuration/             # 配置管理
│
└── AutoWeldSystem.UI               # 表示层
    ├── Forms/
    ├── Views/
    ├── Components/
    └── Program.cs
```

**依赖方向：**
```
UI → Application → Domain
         ↓
    Infrastructure → Domain
```

---

## 对比：当前 vs 推荐

| 层级 | 当前项目 | 标准推荐 | 问题 |
|------|---------|---------|------|
| **领域核心** | Core | Domain | Core 包含了太多非领域内容（DTOs, ViewModels） |
| **应用逻辑** | Services | Application | Services 混杂了基础设施服务 |
| **数据访问** | Data | Infrastructure.Data | 缺少 Repository 抽象 |
| **基础设施** | 散落在 Services | Infrastructure | PLC/MES/Log 应该独立 |
| **表示层** | UI | UI | UI 直接依赖 Data，耦合过紧 |

---

## 具体重构步骤（渐进式方案）

### 第一阶段：创建 Repository 层

1. 在 `Core/Interfaces/` 创建：
   ```csharp
   public interface IRepository<T> where T : class
   {
       Task<T?> GetByIdAsync(int id);
       Task<IEnumerable<T>> GetAllAsync();
       Task AddAsync(T entity);
       Task UpdateAsync(T entity);
       Task DeleteAsync(int id);
   }
   ```

2. 在 `Data/Repositories/` 实现：
   ```csharp
   public class Repository<T> : IRepository<T> where T : class
   {
       private readonly SqlSugarDbContext _context;
       // 实现...
   }
   ```

3. 逐步修改 Services 使用 Repository 而不是直接访问 DbContext

### 第二阶段：分离基础设施

1. 创建 `AutoWeldSystem.Infrastructure` 项目
2. 移动：
   - `Services/Plc/` → `Infrastructure/ExternalServices/Plc/`
   - `Services/Mes/` → `Infrastructure/ExternalServices/Mes/`
   - `Services/Log/` → `Infrastructure/Logging/`
3. 更新依赖注入配置

### 第三阶段：解耦 UI

1. 移除 `UI` 对 `Data` 的项目引用
2. 如果 UI 中有直接使用 `SqlSugarDbContext` 的代码，通过 Service 封装

### 第四阶段：清理 Core

1. 将 DTOs 移动到 Application 层（如果创建）或保留在 Core 但明确其用途
2. 将 ViewModels 移动到 UI 层
3. Core 只保留纯领域对象（Entities, Enums, Domain Services）

---

## 优先级建议

**高优先级（立即执行）：**
1. ✅ 解耦 UI 对 Data 的依赖
2. ✅ 创建 Repository 抽象

**中优先级（下个迭代）：**
3. 分离 PLC/MES/Log 到 Infrastructure
4. 重命名 UI.Infrastructure 避免混淆

**低优先级（技术债务，长期优化）：**
5. 完全重构为 Clean Architecture
6. 引入 CQRS 模式（如果业务复杂度增加）

---

## 总结

**当前架构评价：**
- ✅ 基础分层清晰（Core, Data, Services, UI）
- ✅ 依赖方向正确（从外向内）
- ⚠️ 职责混杂（基础设施服务在 Services 层）
- ⚠️ UI 直接依赖 Data（耦合过紧）
- ⚠️ 缺少 Repository 抽象

**建议：**
- 采用**渐进式重构**，保持系统稳定
- 优先解决高优先级问题
- 逐步向标准化架构演进
