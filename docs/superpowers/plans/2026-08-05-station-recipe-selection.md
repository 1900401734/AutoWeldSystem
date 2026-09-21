# 双工位 PLC 配方名称隐式关联实施计划

> **执行要求：** 按任务顺序实施；每个任务先更新/新增回归用例并确认 RED，再做最小实现并确认 GREEN。所有修改均在 `fix/station-recipe-selection` 分支完成。

**目标：** 用户在程序管理中只按工位选择 PLC 配方名称，系统隐藏保存并按目标工位下发数字配方号；业务界面不再展示或手工编辑配方号，MES 契约保持不变。

**架构：** 复用现有 `RecipeCode` 与 `Station2RecipeCode` 作为工位 1/2 的本地隐藏配方号。`PlcRecipeNameOption` 继续承载名称和槽位号；`ProgramRecipeMappingRules` 成为严格按工位解析的唯一规则；程序管理负责名称选择与保存，生产监控只选择程序并从本地记录解析配方。升级前恢复的运行中任务保留本站 `task.RecipeCode` 最小回退，新任务禁止其他回退。

**技术栈：** .NET 8、C#、Windows Forms、AntdUI、SqlSugar、现有 PLC/MES 服务、控制台回归 harness、PowerShell 7

## 全局约束

- 不新增数据库表或字段，不迁移现有数据。
- `RecipeCode` 只代表工位 1，`Station2RecipeCode` 只代表工位 2。
- 工位 2 为空时不得回退使用工位 1。
- 配方名称只用于显示；运行时不得按名称、产品工号或模糊文本重新匹配配方号。
- 程序管理下拉框只能从对应工位的 `PlcRecipeNameOption` 取隐藏配方号，不允许手工输入数字。
- 双工位允许一侧“不适用”，但两个工位不能同时“不适用”；双工位同工单生产时必须两个工位均配置。
- 配方名称读取失败时禁止新增程序；已有程序允许保存非配方字段并保留原关联。
- 普通业务界面不显示配方号；地址维护映射页和日志诊断可继续显示。
- MES 请求、响应、程序同步状态和 `ProgramDataWriteReq` 不修改。
- 静态控件声明、布局和删除控件放在 `*.Designer.cs`；运行时绑定与事件逻辑放在代码后置。
- 不修改或提交根目录 `AGENTS.md`；保留用户已有工作区内容。`artifacts/` 等本地构建产物不得暂存或提交。
- 自动测试与构建串行执行，使用隔离输出 `-m:1 -p:BaseOutputPath=..\artifacts\verify-bin\`。

## 任务 0：建立实施基线

- [ ] **步骤 1：确认分支与工作区范围**

```powershell
git status --short --branch
git log -2 --oneline --decorate
```

Expected：当前分支为 `fix/station-recipe-selection`；只有本地构建产物或实施计划文档等已知内容，不包含用户未知的源码修改。不得删除或暂存 `artifacts/`。

- [ ] **步骤 2：运行基线验证**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected：记录当前 harness 和 build 基线；如有既有失败，先判断是否与本功能相关，不在本计划中顺带修复无关问题。

## 任务 1：收紧核心配方规则

**文件：**

- 修改：`AutoWeldSystem.Core/Production/ProgramRecipeMappingRules.cs`
- 修改：`AutoWeldSystem.Core/Production/ProgramSaveRecipeRules.cs`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：更新保存和工位解析回归用例，确认 RED**

调整测试列表和现有方法：

- `ProgramSaveRecipeRulesRequirePositiveStationCodes`
  - 单工位：工位 1 必须为正整数；
  - 双工位：允许 `("2", null)` 和 `(null, "5")`；
  - 双工位：`(null, null)` 必须失败；
  - 任一非空的 `0`、负数或非数字必须失败。
- `ProgramRecipeMappingResolvesStationSpecificCodes`
  - 工位 1 只返回 `RecipeCode`；
  - 工位 2 只返回 `Station2RecipeCode`；
  - 工位 2 为空返回空串，不回退工位 1。
- `ProgramSharedRecipeTargetsResolveIndependently`
  - 两个目标按工位返回独立配方；
  - 缺少一侧时该目标的 `RecipeCode` 为空。

Run：

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected：上述新断言至少在工位 2 回退和双工位一侧为空的场景失败。

- [ ] **步骤 2：实现严格工位映射**

在 `ProgramRecipeMappingRules.Resolve` 中：

```csharp
var station1RecipeCode = Normalize(program.RecipeCode);
return stationNo == 2
    ? Normalize(program.Station2RecipeCode)
    : station1RecipeCode;
```

保留 `Normalize`、`Matches` 和 `ResolveTargets` 的公共签名，更新注释，删除“工位 2 安全回退”的描述。

- [ ] **步骤 3：实现双工位至少一侧有效校验**

在 `ProgramSaveRecipeRules.Validate` 中：

- 单工位调用 `ValidatePositive(station1RecipeCode, ...)`；
- 双工位先规范判断两个值是否为空；
- 两个都为空时抛出“至少选择一个适用工位配方”；
- 对每个非空值分别调用正整数校验。

不要加入重复名称、名称格式或动态映射校验。

- [ ] **步骤 4：运行定向回归确认 GREEN**

运行完整 harness，确认本任务新增/调整用例通过；其他因旧回退假设失败的用例记录到下一任务同步调整。

- [ ] **步骤 5：原子提交**

```powershell
git add -- AutoWeldSystem.Core/Production/ProgramRecipeMappingRules.cs AutoWeldSystem.Core/Production/ProgramSaveRecipeRules.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "fix(plc): 严格按工位解析程序配方"
```

## 任务 2：将程序管理收紧为名称选择和隐藏保存

**文件：**

- 修改：`AutoWeldSystem.UI/Views/ProgramManageView.cs`
- 修改：`AutoWeldSystem.UI/Views/ProgramManageView.Designer.cs`
- 修改：`AutoWeldSystem.Core/Constants/TextKeys.cs`
- 修改：`AutoWeldSystem.Core/Localization/UiText.resx`
- 修改：`AutoWeldSystem.Core/Localization/UiText.en.resx`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：增加程序管理源码契约测试，确认 RED**

新增或调整测试，断言：

- `ConfigureGrids` 不再添加 `BizProgram.RecipeCode` 列；
- `ApplyGridHeaders` 不再设置 `grid.program.recipe_code`；
- `ResolveSelectedRecipeCode` 不再解析 `select.Text` 中的数字；
- 读取失败时选择器保持 `List=true`、`ReadOnly=true`，不出现手工输入占位提示；
- 双工位下拉包含“不适用”语义；
- `TryBuildRequest` 双工位允许一侧为空、禁止两侧为空；
- 已有程序读取失败或历史关联失效时，未修改选择器会保留原字段。

Run harness，Expected：旧实现仍存在表格列、手工数字降级和双工位两侧必填，因此测试失败。

- [ ] **步骤 2：删除程序列表配方号列和配方排序**

在 `ProgramManageView.ConfigureGrids` 删除：

```csharp
dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.RecipeCode), 14));
```

在 `ApplyGridHeaders` 删除对应表头。在 `ApplyProgramFilter`：

- 保留程序名称、产品工号、零组件代码、备注和同步状态搜索；
- 删除配方号搜索；
- 删除 `GetRecipeSortBucket`、`GetRecipeSortNumber` 和按配方号排序；
- 使用产品工号、程序名称和更新时间等现有业务字段做稳定排序，不增加新可见列。

- [ ] **步骤 3：引入选择器状态而非文本解析**

在 `ProgramManageView` 内固定增加以下 UI 状态模型：

```csharp
private enum RecipeSelectionKind
{
    PlcOption,
    NotApplicable,
    MissingExisting
}

private sealed record RecipeSelectionItem(
    string DisplayText,
    string? RecipeCode,
    RecipeSelectionKind Kind);
```

增加按工位保存的平行列表：

```csharp
private readonly Dictionary<int, List<RecipeSelectionItem>> _recipeSelectionItems = new();
```

`Select.Items` 只加入 `DisplayText` 字符串，业务值始终通过 `SelectedIndex` 从 `_recipeSelectionItems[stationNo]` 获取。这样即使名称重复，也不会通过名称反查配方号。禁止把 `select.Text` 解析成数字作为后备。

- [ ] **步骤 4：实现单/双工位选项绑定**

- 成功读取时仅显示 `PlcRecipeNameOption.Name`；
- 不再使用 `DuplicateRecipeOption` 添加配方号后缀；重复名称暂不校验；
- 双工位两个列表各加入“不适用”；
- 单工位不加入“不适用”；
- 新增程序默认未选择有效配方；
- 选择器设置为列表模式并禁止自由文本输入。

移除或改造以下旧逻辑：

- `AddMissingRecipeOption` 以伪选项显示历史数字；
- `ResolveSelectedRecipeCode` 的显示文本匹配与 `int.TryParse` 回退；
- `PlaceholderRecipeManual` 和读取失败手工输入提示；
- 向用户展示 `MissingRecipeOption` 数字的行为。

- [ ] **步骤 5：实现读取失败和历史失效状态**

读取失败：

- 记录 `_recipeNameReadSucceeded[stationNo] = false`；
- 选择器禁用或只读且不接受输入；
- 新增模式保存前直接提示读取失败；
- 编辑模式保留实体原值，不把选择器文本写回。

读取成功但历史数字不在选项中：

- 显示“原关联配方已不可用”状态，不显示数字；
- 记录该工位未被用户重新选择；
- 非配方保存时沿用编辑实体原值；
- 一旦选择有效名称或“不适用”，按新状态覆盖。

- [ ] **步骤 6：实现保存规则**

`TryBuildRequest`：

- 单工位从工位 1 有效选项取得隐藏编号；
- 双工位分别取值，“不适用”写 `null`；
- 新增时任一需要读取的工位失败则阻止；
- 双工位两个都为空时阻止；
- 编辑已有程序时，未修改且不可编辑的工位保留原值；
- 产品工号不由配方名称赋值或校验。

- [ ] **步骤 7：调整 Designer 与本地化**

Designer 只调整静态文本/属性：

- 标签默认文字改为“工位1配方名称”“工位2配方名称”；
- 确保选择器不提供自由输入；
- 不改无关布局。

资源：

- 更新 `LabelStation1Recipe`、`LabelStation2Recipe`；
- 新增“不适用”“读取失败”“原关联不可用”“至少选择一个适用工位”等键；
- 移除业务代码对手工输入和数字缺失提示键的引用；未再使用的键可在确认无引用后删除。

- [ ] **步骤 8：运行 harness 和 UI 项目构建**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.UI\AutoWeldSystem.UI.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

- [ ] **步骤 9：原子提交**

```powershell
git add -- AutoWeldSystem.UI/Views/ProgramManageView.cs AutoWeldSystem.UI/Views/ProgramManageView.Designer.cs AutoWeldSystem.Core/Constants/TextKeys.cs AutoWeldSystem.Core/Localization/UiText.resx AutoWeldSystem.Core/Localization/UiText.en.resx AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "feat(program): 通过配方名称隐式关联工位配方"
```

## 任务 3：按工位过滤离线程序并移除离线配方号操作

**文件：**

- 修改：`AutoWeldSystem.Core/Production/OfflineStartInputRules.cs`
- 修改：`AutoWeldSystem.UI/Views/MonitorView.cs`
- 修改：`AutoWeldSystem.UI/Views/MonitorView.Designer.cs`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：增加离线程序可用性测试，确认 RED**

调整 `BuildProgramNameOptions` 接口，使其接收：

```csharp
IEnumerable<BizProgram> programs,
int stationNo,
bool requireBothStations
```

测试：

- 工位 1 只保留 `RecipeCode` 有效的程序；
- 工位 2 只保留 `Station2RecipeCode` 有效的程序；
- 同工单模式要求两个字段均有效；
- 删除配方号排序；
- 重名显示只包含程序名称和产品工号，不含“配方号=”；
- `BuildRequest` 仍从选中程序按工位解析实际配方号。

Run harness，Expected：旧接口只检查 `RecipeCode`，且重名文本包含数字配方号。

- [ ] **步骤 2：实现离线规则**

- `BuildProgramNameOptions` 使用 `ProgramRecipeMappingRules.Resolve(program, stationNo)` 过滤当前工位；
- `requireBothStations=true` 时同时检查工位 1 和工位 2；
- 排序只使用程序名称、产品工号和本地 ID；
- 重名文本改为：

```text
程序名称 | 产品工号=...
```

- 删除不再需要的 `BuildRecipeCodeOptions` 和数字排序帮助方法；
- `BuildRequest` 继续内部写入当前工位隐藏配方号。

- [ ] **步骤 3：从 MonitorView Designer 移除配方号控件**

在 `MonitorView.Designer.cs`：

- 从 `tlpProductNum` 删除 `selectRecipeCode`；
- 将布局从 3 列调整为产品工号占满剩余宽度；
- 删除 `selectRecipeCode` 初始化、字段声明和静态属性；
- 保持其他控件名称和事件不变。

- [ ] **步骤 4：删除离线配方号联动代码**

在 `MonitorView.cs` 删除：

- `_syncingRecipeCodeSelection`；
- `RecipeCodeSelection_SelectedIndexChanged` 绑定/解绑；
- `GetRecipeCodeSelectionText`；
- `FindRecipeCodeItemIndex`；
- `ForceRecipeCodeSelection`；
- `BindOfflineRecipeCodeOptions`；
- `ApplyOfflineRecipeCodeSelection`；
- `ResolveLocalProgramByRecipeCode` 的 UI 反向选择用途。

`ApplyOfflineProgramNameOption` 只联动产品工号、产品型号和程序显示，不写可见配方控件。离线请求始终从 `_offlineProgramNameOptions` 中的程序按当前工位解析配方号。

- [ ] **步骤 5：调整离线绑定调用**

调用 `BuildProgramNameOptions` 时传入：

- 当前工位；
- `EnableDualStation && !EnableDualWorkOrder` 作为同工单必须双侧有效的条件。

工位切换、程序刷新和模式切换后重新构建列表。未配置当前工位配方的程序自然不显示。

- [ ] **步骤 6：更新源码契约测试并确认 GREEN**

更新原测试：

- `MonitorViewLinksProgramAndRecipeSelectionsForStartInput` 改为断言无配方号选择事件；
- `MonitorViewRecipeDropdownUsesSortedRecipeOptions` 改为“生产界面不再绑定配方号下拉”；
- `MonitorViewUsesPlcRecipeOnlyForOfflineIdleInputs` 保留 PLC 空闲快照内部行为，但不要求写入可见控件；
- Designer 不再包含 `selectRecipeCode` 或“配方号：”。

运行 harness 与 UI 构建。

- [ ] **步骤 7：原子提交**

```powershell
git add -- AutoWeldSystem.Core/Production/OfflineStartInputRules.cs AutoWeldSystem.UI/Views/MonitorView.cs AutoWeldSystem.UI/Views/MonitorView.Designer.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "refactor(production): 隐藏离线开工配方号选择"
```

## 任务 4：收紧在线开工的本地配方关联

**文件：**

- 修改：`AutoWeldSystem.Services/Production/WeldTaskService.cs`
- 修改：`AutoWeldSystem.UI/Views/MonitorView.cs`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：增加在线开工回归用例，确认 RED**

增加规则/源码契约测试：

- 在线 MES 程序找不到本地程序时不得使用 `ProgramDataRes.RecipeCode` 回退；
- 本地程序当前工位配方为空时拒绝开工；
- 在线可选程序列表不包含缺少当前工位关联的程序；
- 同工单模式缺少任一侧关联时程序不可选；
- 选择程序预览不再写入配方号控件。

Run harness，Expected：`ResolveProgramRecipeCode` 和 `ResolveRecipeCodeForStartedTask` 仍存在 MES/任务回退。

- [ ] **步骤 2：收紧 WeldTaskService 新任务配方来源**

将 `ResolveProgramRecipeCode` 改为：

- 必须定位本机 `BizProgram`；
- 按目标工位调用 `ProgramRecipeMappingRules.Resolve`；
- 为空时抛出明确 `BusinessOperationException`；
- 删除 `ProgramRecipeMappingRules.Normalize(program.RecipeCode)` 的 MES 回退。

`StartAsync` 在插入 `BizWeldTask` 前完成此校验。`StartLocalAsync` 继续接收规则层生成的内部配方号，但服务入口仍拒绝空值。

- [ ] **步骤 3：按本地关联过滤在线 MES 程序**

在 `MonitorView` 在线程序绑定前：

- 通过 MES 程序 ID 优先匹配本地 `BizProgram`；
- 仅保留当前工位有配方关联的项目；
- 同工单模式要求本地程序两个工位都有效；
- 保持 MES 列表对象作为显示和下载来源，不修改 MES 服务；
- 本地关联缺失时在运行提示区说明需到程序管理配置 PLC 配方。

删除 `BindOnlineRecipeCodeOptions`、`ResolveRecipeCodeByProgramName` 和在线配方号反向选择路径。程序名称选择仍驱动下载和确认。

- [ ] **步骤 4：收紧新任务运行时解析**

`ResolveRecipeCodeForStartedTask`：

- 新在线/离线任务只接受匹配本地程序的当前工位配方；
- 不再 `FirstNonEmpty(mappedRecipeCode, task.RecipeCode, selectedProgram?.RecipeCode)`；
- 未匹配时返回失败结果，由调用方阻止下发。

升级前恢复任务的兼容在任务 5 单独实现，避免把通用回退留在此方法。

- [ ] **步骤 5：运行 harness 和服务/UI 构建**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

- [ ] **步骤 6：原子提交**

```powershell
git add -- AutoWeldSystem.Services/Production/WeldTaskService.cs AutoWeldSystem.UI/Views/MonitorView.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "fix(production): 开工时强制使用本站程序配方"
```

## 任务 5：收紧 PLC 调和并保留恢复任务最小兼容

**文件：**

- 修改：`AutoWeldSystem.Services/Plc/RecipeCodeReconcileMonitorService.cs`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：增加最小恢复任务标识**

在 `RecipeCodeReconcileMonitorService` 增加：

```csharp
private readonly HashSet<int> _restoredTaskIds = new();
```

`TryRestoreRunningTask` 调用 `RestoreUnfinishedTask` 并确认任务仍为 Running 后，在现有 `_stateSync` 锁内记录 `restoredTask.Id`。新版本 `StartAsync` / `StartLocalAsync` 创建的任务不会经过该入口，因此不会被标记。应用再次重启且任务仍未完成时，会再次通过该入口恢复并重新标记。

不修改 `BizWeldTask`，不新增运行时属性或数据库字段。

- [ ] **步骤 2：增加恢复与调和回归用例，确认 RED**

测试：

- 新任务本地工位关联为空时，`ResolveExpectedRecipe` 不回退 `task.RecipeCode`；
- 恢复任务本地本站关联为空时允许 `task.RecipeCode`；
- 恢复任务快照不得用于另一个目标工位；
- `ReconcileRecipeAsync` 遍历共享目标时，某目标为空必须阻止整体同步，不得使用源任务配方补齐；
- 两个目标都有效时分别调用本站配方号。

- [ ] **步骤 3：实现期望配方解析边界**

`RecipeCodeReconcileMonitorService.ResolveExpectedRecipe`：

- 普通新任务只返回 `ProgramRecipeMappingRules.Resolve(localProgram, stationNo)`；
- 仅当任务被识别为恢复任务且 `stationNo == task.StationNo` 时，允许回退 `task.RecipeCode`；
- 返回空时写清晰生产流程/异常日志，不调用 `SyncRecipeCodeAsync`。

`ReconcileRecipeAsync` 删除：

```csharp
FirstNonEmpty(target.RecipeCode, task.RecipeCode)
```

共享同工单目标中任一配方为空时，在任何 PLC 写入前整体退出并记录缺少的工位，避免部分下发。

- [ ] **步骤 4：保持任务快照与日志兼容**

- `BizWeldTask.RecipeCode` 继续作为任务开始时的本站快照和历史记录；
- 完工前 PLC 回读更新任务配方的现有行为保持；
- 日志继续记录实际数字配方号；
- 不把恢复任务快照写回程序的另一个工位字段。

- [ ] **步骤 5：运行完整 harness 和解决方案构建**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

- [ ] **步骤 6：原子提交**

仅暂存本任务的实际文件：

```powershell
git add -- AutoWeldSystem.Services/Plc/RecipeCodeReconcileMonitorService.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "fix(plc): 限制配方调和的历史任务回退"
```

## 任务 6：清理剩余业务配方号显示并同步文档

**文件：**

- 修改：`AutoWeldSystem.UI/Views/MonitorView.cs`
- 修改：`AutoWeldSystem.UI/Views/MonitorView.Designer.cs`
- 修改：`AutoWeldSystem.UI/Forms/LocalWorkOrderForm.cs`
- 修改：`AutoWeldSystem.UI/Forms/LocalWorkOrderForm.Designer.cs`
- 修改：`AutoWeldSystem.Core/Constants/TextKeys.cs`
- 修改：`AutoWeldSystem.Core/Localization/UiText.resx`
- 修改：`AutoWeldSystem.Core/Localization/UiText.en.resx`
- 修改：`README.md`
- 修改：`AutoWeldSystem.Tests/Program.cs`

- [ ] **步骤 1：全局扫描业务界面中的配方号可见性**

运行：

```powershell
Get-ChildItem AutoWeldSystem.UI -Recurse -Include *.cs,*.Designer.cs | Select-String -Pattern '配方号|RecipeCode|selectRecipeCode'
```

分类：

- 地址维护映射页：允许；
- 日志/诊断详情：允许；
- 程序管理、监控、在线/离线开工、普通操作提示：删除或改成“配方名称/PLC 配方”；
- 内部变量、服务调用和日志字段：保留。

- [ ] **步骤 2：清理业务可见文本**

- 删除普通界面 PrefixText、标签、下拉内容和提示中的数字配方号；
- `LocalWorkOrderForm` 删除 `lblRecipeCode`、`txtRecipeCode` 及其布局行；代码不再向文本框写配方号，提交请求时仍通过所选程序和 `_stationNo` 调用 `ProgramRecipeMappingRules.Resolve` 获取隐藏值；
- 生产运行状态可显示“配方已准备/未配置”，不显示数字；
- 保留地址维护列 `address.column.recipe_code` 和诊断日志文本。

- [ ] **步骤 3：清理失效资源键并验证本地化完整性**

- 删除业务侧不再使用的 `grid.program.recipe_code`、手工输入提示等引用；
- 只有确认代码和测试均无引用时才删除资源键；
- 中英文资源同步；
- 运行现有本地化完整性回归。

- [ ] **步骤 4：更新 README**

补充：

- 程序管理按工位选择 PLC 配方名称；
- 数字配方号由地址维护映射并隐藏保存；
- 双工位左右关联独立；
- “不适用”和生产可用性规则；
- MES 下载程序需配置本机 PLC 配方后才能生产；
- PLC 槽位变化后需重新选择并保存程序；
- 地址维护是查看数字配方号映射的入口。

不维护逐提交流水账。

- [ ] **步骤 5：更新源码契约测试**

断言：

- 普通业务 Designer 不再包含 `selectRecipeCode` 和“配方号：”；
- 程序列表不显示配方编号；
- 地址维护仍保留配方号列；
- 日志诊断资源可保留数字信息；
- README 包含新的用户操作说明。

- [ ] **步骤 6：运行完整验证**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
git diff --check
```

- [ ] **步骤 7：提交业务界面和资源清理**

```powershell
git add -- AutoWeldSystem.UI/Views/MonitorView.cs AutoWeldSystem.UI/Views/MonitorView.Designer.cs AutoWeldSystem.UI/Forms/LocalWorkOrderForm.cs AutoWeldSystem.UI/Forms/LocalWorkOrderForm.Designer.cs AutoWeldSystem.Core/Constants/TextKeys.cs AutoWeldSystem.Core/Localization/UiText.resx AutoWeldSystem.Core/Localization/UiText.en.resx AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git commit -m "refactor(ui): 移除业务界面配方号展示"
```

- [ ] **步骤 8：单独提交 README**

```powershell
git add -- README.md
git diff --cached --check
git commit -m "docs(readme): 说明工位配方名称关联流程"
```

## 任务 7：最终集成验证与交付准备

- [ ] **步骤 1：检查工作区和提交边界**

```powershell
git status --short --branch
git log --oneline --decorate develop..HEAD
git diff --check develop...HEAD
git diff --stat develop...HEAD
```

确认没有 `AGENTS.md`、真实配置、日志、`bin/obj`、发布产物或无关文件。

- [ ] **步骤 2：串行运行最终自动验证**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

分别报告 harness 和 build；既有警告需说明来源，不将其描述为本功能失败。

- [ ] **步骤 3：人工 WinForms 验证**

- 单工位新增：只显示工位 1 名称选择，数字不可见；
- 双工位新增：左右独立选择，不同槽位正确隐藏保存；
- 一侧“不适用”：只在另一工位可选；
- 同工单缺少一侧：不能开工；
- 读取失败：新增被阻止，已有程序可保存非配方字段；
- 历史关联失效：显示不可用，不显示数字；
- 在线/离线开工：无配方号控件，选择程序后正常开工。

- [ ] **步骤 4：现场依赖验证边界**

有现场条件时验证：

- PLC：工位 1/2 实际写入各自隐藏数字并完成回读校验；
- 双工位同工单：两个工位分别写入，不发生工位 1 配方复用；
- MES：创建、更新、下载载荷和同步状态无变化；
- MySQL：无新增表/列，现有程序字段保存正确。

没有现场条件时分别标记为未验证，不以本地构建代替。

- [ ] **步骤 5：交付说明**

必须明确：

- README 已更新的内容；
- harness、build 和人工 UI 结果；
- PLC、MES、MySQL 的实际验证范围；
- 分支 `fix/station-recipe-selection` 的提交列表；
- 未经用户授权不直接合并或推送到 `develop`。