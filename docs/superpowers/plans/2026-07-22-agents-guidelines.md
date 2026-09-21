# 用户级与项目级 AGENTS.md 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按已确认设计生成分层去重的用户级和 AutoWeldSystem 项目级 `AGENTS.md`，并保持两个文件仅本机生效。

**Architecture:** 用户级文件保存跨项目稳定偏好，项目级文件保存 AutoWeldSystem 专用架构、验证、提交和 README 规则。Codex 先加载用户级规则，再由项目级规则补充或覆盖；两级文件均不进入 Git 提交。

**Tech Stack:** Markdown、Codex `AGENTS.md`、PowerShell 7、Git、Conventional Commits 1.0.0

## Global Constraints

- 本地 Shell 默认使用 PowerShell 7。
- 用户级文件路径固定为 `C:\Users\xyful\.codex\AGENTS.md`。
- 项目级文件路径固定为 `E:\Desktop\AutoWeldSystem\AGENTS.md`。
- 项目级 `AGENTS.md` 继续由 `.gitignore` 中的 `/AGENTS.md` 忽略。
- 两个 `AGENTS.md` 均不得暂存、提交或推送。
- 不修改源码、README、`.gitignore` 和工作区中的其他用户改动。
- `type`、`scope`、`BREAKING CHANGE` 保留英文，提交描述、正文和普通脚注优先中文。
- README 仅在程序变更影响用户功能、配置、部署、接口、版本或排障方式时更新。

---

### Task 1: 更新用户级 AGENTS.md

**Files:**
- Modify: `C:\Users\xyful\.codex\AGENTS.md`
- Reference: `docs/specs/2026-07-22-agents-guidelines-design.md`

**Interfaces:**
- Consumes: Codex 用户级规则发现机制和已确认的跨项目个人偏好。
- Produces: 所有项目默认继承的工作环境、沟通、工程、注释、验证和提交规则。

- [ ] **Step 1: 读取当前用户级文件，确认目标路径可访问**

Run:

```powershell
Get-Content -LiteralPath 'C:\Users\xyful\.codex\AGENTS.md' -Raw
```

Expected: 成功输出当前用户级规则，不包含读取错误。

- [ ] **Step 2: 用以下完整内容替换用户级文件**

```markdown
# 用户级开发协作规则

## 规则优先级

- 本文件只定义跨项目稳定偏好；项目目录中的 `AGENTS.md` 可以补充或覆盖这些规则。
- 优先遵循当前项目已有的架构、命名、测试、提交和交付约定，不将个人偏好强加给已有明确规范的项目。

## 工作环境

- 执行本地 Shell 命令时默认使用 PowerShell 7；仅在项目或平台明确要求其他 Shell 时例外。
- 使用适合当前平台的原生命令和工具，避免不必要的跨 Shell 调用。

## 沟通与说明

- 默认使用中文沟通、解释实现、报告验证结果和编写提交的自然语言内容；代码标识符、命令和标准协议名称保持原文。
- 面向初学者说明改动目的、关键实现、涉及文件、验证结果和注意事项，但避免无关的长篇说明。
- 对未运行、失败或受阻的检查必须如实说明，不把部分验证描述为全部通过。

## 通用工程原则

- 先检查现有结构、调用路径和可复用实现，再进行修改。
- 优先采用简单、主流且符合项目现状的方案，保持职责清晰、高内聚和低耦合。
- 优先复用已有类、方法、服务、控件和平台能力，避免重复实现。
- 只实现当前需求，避免无关重构、功能蔓延、投机性抽象和为未来预留的复杂结构。
- 修改应尽可能小且行为边界清晰，不用大段重写替代可控的局部修改。
- 不省略输入校验、错误提示、数据安全和其他必要边界处理。

## 注释规则

- 为关键类、公开方法、复杂业务规则、边界条件和非显然逻辑添加简洁中文注释。
- 注释应解释设计意图和原因，不逐行复述代码；简单属性、直观语句和显而易见的控制流无需强制注释。

## 验证与交付

- 根据改动风险运行最相关的测试、构建或静态检查，并在完成前核对实际输出。
- 交付说明应包含复用内容、修改文件、验证命令、验证结果、未执行检查和剩余风险。
- 保留用户已有修改，不暂存、覆盖、还原或提交与当前任务无关的文件。

## Git 提交默认规则

- 默认使用 Conventional Commits：`<type>(<scope>): <description>`。
- `type`、`scope` 和 `BREAKING CHANGE` 等机器识别字段使用英文；描述、正文和普通脚注优先使用中文。
- 项目存在明确提交规范时，以项目规范为准。
- 提交应保持原子性；提交前检查暂存差异，避免混入无关修改。
```

- [ ] **Step 3: 验证用户级关键规则**

Run:

```powershell
Select-String -LiteralPath 'C:\Users\xyful\.codex\AGENTS.md' -Pattern 'PowerShell 7|优先使用中文|不逐行复述|Conventional Commits|项目规范为准'
```

Expected: 五类关键规则均有匹配结果。

### Task 2: 更新 AutoWeldSystem 项目级 AGENTS.md

**Files:**
- Modify: `E:\Desktop\AutoWeldSystem\AGENTS.md`
- Reference: `E:\Desktop\AutoWeldSystem\AutoWeldSystem.sln`
- Reference: `E:\Desktop\AutoWeldSystem\Directory.Build.props`
- Reference: `E:\Desktop\AutoWeldSystem\docs\specs\2026-07-22-agents-guidelines-design.md`

**Interfaces:**
- Consumes: 用户级默认规则、解决方案结构、现有构建测试命令和项目安全边界。
- Produces: AutoWeldSystem 范围内覆盖或补充用户级默认的仓库专用规则。

- [ ] **Step 1: 读取当前项目级文件和忽略规则**

Run:

```powershell
Get-Content -LiteralPath 'E:\Desktop\AutoWeldSystem\AGENTS.md' -Raw
git check-ignore -v AGENTS.md
```

Expected: 项目级文件可读取，Git 输出 `.gitignore` 中的 `/AGENTS.md` 匹配规则。

- [ ] **Step 2: 用以下完整内容替换项目级文件**

```markdown
# AutoWeldSystem 仓库协作指南

## 项目概况与分层

`AutoWeldSystem.sln` 是 .NET 8 Windows Forms 解决方案，各项目职责如下：

- `AutoWeldSystem.Core`：领域规则、DTO、实体、常量、接口、权限定义和多语言资源。
- `AutoWeldSystem.Data`：SqlSugar 数据库上下文、CodeFirst 和数据库初始化。
- `AutoWeldSystem.Services`：MES、PLC、生产、程序管理、日志、权限和中心服务等业务集成与工作流。
- `AutoWeldSystem.UI`：WinForms 窗体、视图、控件、资源和应用启动基础设施。
- `AutoWeldSystem.CenterServer`：ASP.NET Core 数据接收和监控服务。
- `AutoWeldSystem.Tests`：控制台回归测试入口，测试列表位于 `Program.cs`。
- `AutoWeldSystem.Libs`：项目依赖的本地 DLL。
- `docs`：设计、实施计划、快速入门和其他项目文档。

业务规则、DTO、实体、常量和接口优先放在 `Core`；数据库上下文放在 `Data`；业务集成和工作流放在 `Services`；UI 不重复实现可复用业务决策。

## WinForms 约束

- 控件声明、静态属性、布局和静态初始化放在 `*.Designer.cs`；运行时行为、事件处理、数据加载和业务调用放在代码后置文件。
- 修改界面前检查控件层级、数据绑定、事件名称和调用路径，保留既有控件名称、事件绑定和行为契约。
- 优先复用现有控件、布局方法和页面模式，不为单一界面引入新的框架或抽象层。

## 构建、测试与运行

- 恢复依赖：`dotnet restore AutoWeldSystem.sln`
- 运行回归测试：`dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore`
- 构建解决方案：`dotnet build AutoWeldSystem.sln --no-restore`
- 默认输出被运行中的程序锁定时：`dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\`
- 启动 WinForms：`dotnet run --project AutoWeldSystem.UI\AutoWeldSystem.UI.csproj`
- 发布 WinForms：`dotnet publish AutoWeldSystem.UI\AutoWeldSystem.UI.csproj -c Release -r win-x64 --self-contained false`

行为变更优先运行控制台回归测试，再构建解决方案。只有文档或本机规则变更时可以不运行 .NET 测试和构建，但必须说明原因。

## C# 编码规范

- 保持可空引用类型启用，使用 4 空格缩进、短方法和简单控制流。
- 类、方法、属性和常量使用 PascalCase；局部变量和参数使用 camelCase；接口以 `I` 开头；异步方法以 `Async` 结尾。
- 优先复用现有类、方法、服务、控件和规则，不重复实现相同逻辑。
- 每个类和方法保持单一清晰职责，避免全局状态、静态业务依赖和跨层直接调用。
- 只有至少两个稳定场景确实需要时，才抽取公共接口、基类或通用组件。
- 不为未来扩展预先增加工厂、策略、配置项或多层包装。
- 优先在原有基础上做最小修改，不大段替换文件，不进行无关重构或顺手扩展功能。
- 不省略错误提示、输入校验和边界处理。

## 注释规范

- 为关键类、公开方法、复杂业务规则、边界条件和非显然逻辑添加简洁中文注释。
- 注释解释意图和原因，不逐行复述代码；直观属性、简单语句和显而易见的控制流无需强制注释。
- 面向初学者保持代码和说明易读，但不以注释数量代替清晰的命名和结构。

## 测试规范

- 在 `AutoWeldSystem.Tests/Program.cs` 的现有 `(Name, Run)` 列表中添加描述清晰的回归用例。
- 优先测试纯规则或服务逻辑，避免依赖真实 PLC、MES、MySQL 或 UI 自动化。
- 缺陷修复应覆盖根因和关键回归路径；新功能应覆盖主要行为和重要边界。
- 准确报告测试范围：构建成功不等于所有回归测试通过。

## Git 提交规范

提交标题使用：

```text
<type>(<scope>): <中文简短描述>
```

- `type` 使用小写英文：`feat`、`fix`、`docs`、`refactor`、`perf`、`test`、`build`、`ci`、`chore`、`revert`。
- `scope` 使用简短英文业务名，优先复用 `plc`、`mes`、`production`、`program`、`monitor`、`settings`、`address`、`logs`、`ui`、`rbac`、`data`、`center`、`repo` 等现有范围，但不设置僵化白名单。
- 描述、正文和普通脚注优先使用中文，直接说明行为变化和原因。
- `feat` 对应次版本，`fix` 对应修订版本；破坏性变更使用 `!` 或大写 `BREAKING CHANGE:`，对应主版本。
- 不相关变更拆分为原子提交；提交前检查暂存差异和 `git diff --check`，避免混入无关文件。

示例：

```text
feat(program): 支持双工位独立配置焊接程序
fix(plc): 修复关闭程序时 PLC 服务无法退出的问题
docs(readme): 补充现场部署和配置说明
refactor(logs): 复用设备状态日志查询逻辑
```

项目根 `AGENTS.md` 仅本机使用，不得暂存、提交或推送。

## README 更新规范

程序变更影响以下任一内容时，同一任务必须同步更新 `README.md`：

- 用户可见功能、操作流程、默认行为、权限或错误处理方式。
- 配置项、环境要求、数据库初始化方式。
- 构建、运行、发布或现场部署步骤。
- 项目结构、外部接口、PLC/MES 协议或重要数据流。
- 用户操作方式或故障排查方法。
- 软件版本；README 当前版本必须与 `Directory.Build.props` 一致。

以下变更通常无需修改 README：不改变外部行为的内部重构、仅调整测试、格式化或注释变更，以及不影响用户功能、配置、部署和排障方式的内部修复。

交付说明必须明确写出以下一种结论及原因：

```text
README：已更新，补充了……
```

```text
README：无需更新，因为本次修改不影响用户功能、配置、部署或排障方式。
```

禁止为了满足形式添加无意义内容，也不在 README 中维护重复的逐提交变更流水账。

## 安全与配置

- 不提交真实的 `AutoWeldSystem.UI/appsettings.json`、数据库密码、PLC 地址、MES 地址、令牌或本机绝对路径。
- 使用 `AutoWeldSystem.UI/appsettings.example.json` 作为配置模板。
- `bin`、`obj`、`.vs`、日志和发布产物仅作为本地输出。
- 修改实体或 CodeFirst 初始化前先考虑现有数据库兼容性，并优先在测试库验证。

## 完成任务时的报告

- 说明复用了哪些现有代码或模式。
- 列出实际修改的文件和行为变化。
- 说明 README 是否更新及判断原因。
- 列出实际运行的测试、构建或检查及其结果。
- 明确未运行的检查、已有阻塞和剩余风险。
- 保留工作区中的用户修改，不处理与当前任务无关的差异。
```

- [ ] **Step 3: 验证项目级关键规则和 Git 忽略状态**

Run:

```powershell
Select-String -LiteralPath 'E:\Desktop\AutoWeldSystem\AGENTS.md' -Pattern 'Designer.cs|verify-bin|中文简短描述|README：无需更新|不得暂存、提交或推送'
git check-ignore -v AGENTS.md
```

Expected: 五类关键项目规则均有匹配结果，并输出 `/AGENTS.md` 忽略规则。

### Task 3: 跨层一致性与工作区验证

**Files:**
- Verify: `C:\Users\xyful\.codex\AGENTS.md`
- Verify: `E:\Desktop\AutoWeldSystem\AGENTS.md`
- Verify unchanged: `E:\Desktop\AutoWeldSystem\.gitignore`

**Interfaces:**
- Consumes: Task 1 和 Task 2 生成的两级规则。
- Produces: 可确认的规则覆盖关系和无 Git 污染的本机配置结果。

- [ ] **Step 1: 完整读取两个文件并检查占位符**

Run:

```powershell
Get-Content -LiteralPath 'C:\Users\xyful\.codex\AGENTS.md' -Raw
Get-Content -LiteralPath 'E:\Desktop\AutoWeldSystem\AGENTS.md' -Raw
Select-String -Path 'C:\Users\xyful\.codex\AGENTS.md','E:\Desktop\AutoWeldSystem\AGENTS.md' -Pattern 'TBD|TODO|FIXME|待定'
```

Expected: 两个文件完整可读；占位符搜索无结果。

- [ ] **Step 2: 检查规则分层没有明显冲突**

确认以下事实：

- 用户级规则允许项目级规则覆盖。
- 用户级规则不包含 AutoWeldSystem 专用命令和模块结构。
- 项目级规则采用意图型注释，没有要求逐行注释。
- 两级规则都采用英文机器字段和中文自然语言提交内容。
- README 按影响更新要求只存在于项目级规则。

- [ ] **Step 3: 检查 Git 状态没有新增 AGENTS.md 差异**

Run:

```powershell
git status --short --branch
git check-ignore -v AGENTS.md
git diff -- .gitignore
```

Expected: `AGENTS.md` 不出现在 Git 状态中；`.gitignore` 没有本任务产生的差异；原有用户修改保持不变。

- [ ] **Step 4: 记录验证结论**

Expected completion report:

```text
README：无需更新，因为本次只调整本机代理规则，不影响程序功能、配置、部署或排障方式。
验证：已完整读取两个 AGENTS.md，关键规则匹配成功，项目级文件仍被 Git 忽略。
.NET：未运行测试或构建，因为本次未修改可执行代码。
Git：两个 AGENTS.md 均未暂存、提交或推送。
```
