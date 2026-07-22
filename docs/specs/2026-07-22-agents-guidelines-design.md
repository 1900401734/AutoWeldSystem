# 用户级与项目级 AGENTS.md 设计

## 背景

AutoWeldSystem 已存在项目根 `AGENTS.md`，并通过 `.gitignore` 中的 `/AGENTS.md` 保持仅本机使用。Codex 用户目录 `C:\Users\xyful\.codex\AGENTS.md` 也已存在，用于保存跨项目个人偏好。

本设计结合当前 .NET 8 WinForms 解决方案结构、近期提交历史和 [Conventional Commits 1.0.0 简体中文规范](https://www.conventionalcommits.org/zh-hans/v1.0.0/)，重新划分两级规则的职责，并明确中文提交和 README 更新要求。

## 目标

- 用户级规则只保存跨项目稳定偏好。
- 项目级规则只保存 AutoWeldSystem 的架构、开发、验证和交付约束。
- 提交信息符合 Conventional Commits，同时尽可能使用中文表达自然语言内容。
- 程序完善或修复影响使用说明时，同一任务同步更新 README。
- 避免两级规则重复、冲突和过度约束。

## 非目标

- 不修改程序源码或运行行为。
- 不引入提交检查工具、Git Hook 或新的依赖。
- 不把 README 变成逐提交变更流水账。
- 不暂存、提交或推送任何用户级或项目级 `AGENTS.md`。
- 不修改 `.gitignore` 对项目根 `AGENTS.md` 的忽略规则。

## 已确认的设计选择

### 规则分层

采用“分层且去重”方案。Codex 先加载用户级规则，再由更具体的项目级规则补充或覆盖；项目已有明确约定时，以项目规则为准。

### 用户级 AGENTS.md

目标路径：`C:\Users\xyful\.codex\AGENTS.md`。

只保存以下跨项目稳定偏好：

- 本地 Shell 默认使用 PowerShell 7。
- 默认使用中文沟通、解释代码和编写提交的自然语言内容。
- 遵循项目既有架构和行业主流方法，优先复用已有实现。
- 保持实现简单、职责清晰、高内聚、低耦合，避免投机性抽象和无关重构。
- 注释解释设计意图、复杂业务规则、边界条件和非显然逻辑，不追求逐行注释。
- 面向初学者清楚说明改动、验证结果、未执行检查和注意事项。
- 项目存在更具体规则时，以项目规则为准。

用户级文件不包含 AutoWeldSystem 的项目结构、构建命令、测试入口、WinForms 设计器规则或 README 专用要求。

### 项目级 AGENTS.md

目标路径：`E:\Desktop\AutoWeldSystem\AGENTS.md`。

保留并完善以下项目专用内容：

- `Core`、`Data`、`Services`、`UI`、`CenterServer`、`Tests` 的职责边界。
- WinForms 静态控件声明、布局和初始化与运行时业务逻辑的分工。
- 通用工程原则和意图型注释规则由用户级继承；项目级不重复短方法、简单控制流或抽象门槛等通用要求。
- C# 可空引用类型、命名和异步方法命名约定。
- PLC/MES 协议、双工位或配方的工位解析、CodeFirst 兼容等项目特定复杂点的注释补充。
- 控制台回归测试入口、解决方案构建命令，以及文件锁定时的备用输出目录。
- 真实运行配置、数据库密码、现场 PLC/MES 地址、令牌和本机路径的安全边界，以及示例模板的保留。
- 中文 Conventional Commits 规则和 README 按影响更新规则。
- 完成任务后报告 README 判断、控制台回归 harness、备用输出构建，以及未验证 PLC/MES/MySQL/UI 情况。

项目级文件继续被 Git 忽略，仅在本机生效。

## 提交规范

提交标题使用以下结构：

```text
<type>(<scope>): <中文简短描述>
```

规则如下：

- `type` 使用小写英文：`feat`、`fix`、`docs`、`refactor`、`perf`、`test`、`build`、`ci`、`chore`、`revert`。
- `scope` 使用简短英文业务名，优先复用 `plc`、`mes`、`production`、`program`、`monitor`、`settings`、`address`、`logs`、`ui`、`rbac`、`data`、`center`、`repo` 等现有范围，但不设置僵化白名单。
- 描述、正文和普通脚注优先使用中文，直接说明行为变化及原因。
- `feat` 对应语义化版本的次版本，`fix` 对应修订版本。
- 破坏性变更使用 `!` 或大写 `BREAKING CHANGE:`，对应主版本。
- 项目级仅补充提交前运行 `git diff --check` 检查空白错误；通用原子提交和暂存差异检查由用户级继承。

示例：

```text
feat(program): 支持双工位独立配置焊接程序
fix(plc): 修复关闭程序时 PLC 服务无法退出的问题
docs(readme): 补充现场部署和配置说明
refactor(logs): 复用设备状态日志查询逻辑
```

## README 更新规则

当程序变更影响以下任一内容时，同一任务必须同步更新 `README.md`：

- 用户可见功能、操作流程、默认行为、权限或错误处理方式。
- 配置项、环境要求、数据库初始化方式。
- 构建、运行、发布或现场部署步骤。
- 项目结构、外部接口、PLC/MES 协议或重要数据流。
- 用户操作方式或故障排查方法。
- 软件版本；README 中的当前版本应与 `Directory.Build.props` 一致。

以下变更通常无需修改 README：

- 不改变外部行为的内部重构。
- 仅增加或调整测试。
- 格式化、注释和纯代码风格调整。
- 不影响用户功能、配置、部署和排障方式的内部缺陷修复。

交付说明必须明确写出 README 是否更新及原因。禁止为了满足形式添加无意义内容，也不在 README 中维护重复的逐提交变更流水账。

## 实施范围

设计文档确认后，仅修改以下两个本机文件：

- `C:\Users\xyful\.codex\AGENTS.md`
- `E:\Desktop\AutoWeldSystem\AGENTS.md`

两个文件均不暂存、不提交、不推送。现有源码、README、`.gitignore` 和工作区中的其他用户改动保持不变。

## 验证方式

- 完整读取两个 `AGENTS.md`，检查规则分层、重复和冲突。
- 确认 PowerShell 7、中文提交、意图型注释和初学者说明存在于用户级规则。
- 确认项目架构、构建测试、提交规范、README 判断和安全边界存在于项目级规则。
- 确认项目根 `AGENTS.md` 仍被 Git 忽略。
- 对比修改前后的 `git status`，确认没有混入无关变更。
- 本任务不改变可执行代码，因此不运行 .NET 构建或测试，并在交付时如实说明。
