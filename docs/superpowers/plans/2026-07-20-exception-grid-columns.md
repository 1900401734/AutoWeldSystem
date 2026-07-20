# 异常日志表格列精简实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 从程序异常日志表格移除 `Source` 和 `SourceLocation` 两列，同时保留详情面板中的来源信息并将版本更新到 `1.0.3`。

**Architecture:** 只修改 WinForms 异常日志视图的列声明/绑定和共享版本属性。日志实体、落盘格式、详情构建和其他日志页保持不变；回归测试继续使用当前控制台测试 harness 的源码断言模式。

**Tech Stack:** .NET 8、WinForms、C#、现有 `AutoWeldSystem.Tests` 控制台测试。

## Global Constraints

- 本次属于缺陷修复/小范围优化，版本从 `1.0.2` 调整为 `1.0.3`，`AssemblyVersion` 和 `FileVersion` 为 `1.0.3.0`。
- `ProgramExceptionLogEntry.Source`、`SourceFilePath`、`SourceMemberName` 必须保留并继续出现在 `BuildExceptionBasicInfo`。
- 不修改日志 JSONL 格式、筛选行为、选中行行为或其他日志页。
- `LogManageView.Designer.cs` 当前存在用户的设备生命周期布局差异，只删除异常表格两列相关代码，不覆盖其他差异。
- 不提交 `AGENTS.md`、`AddressManageView.Designer.cs`、`SystemSettingView.Designer.cs`、`docs/QUICK_START.md`。

---

### Task 1: Add the regression test

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs` near the existing log view source-shape tests.
- Test: `AutoWeldSystem.Tests/Program.cs` (`ExceptionGridOmitsSourceColumns`)

**Interfaces:**
- Consumes: `GetRepoFilePath`, `ExtractMethodText`, `AssertTrue`, `AssertFalse` from the existing test harness.
- Produces: A failing regression test that requires the two grid columns to be absent while requiring source details to remain.

- [ ] **Step 1: Add the test registration and assertion method before production edits**

```csharp
    ("Exception grid omits source columns but keeps detail source", ExceptionGridOmitsSourceColumns),

static void ExceptionGridOmitsSourceColumns()
{
    var designerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"),
        Encoding.UTF8);
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var basicInfoMethod = ExtractMethodText(
        viewCode,
        "private static string BuildExceptionBasicInfo",
        "private static string BuildExceptionContext");

    AssertFalse(designerCode.Contains("colExceptionSource", StringComparison.Ordinal),
        "异常日志表格不得声明或注册 Source 列。");
    AssertFalse(designerCode.Contains("colExceptionSourceLocation", StringComparison.Ordinal),
        "异常日志表格不得声明或注册 SourceLocation 列。");
    AssertTrue(basicInfoMethod.Contains("Source: {entry.Source}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 Source。");
    AssertTrue(basicInfoMethod.Contains("SourceFile: {GetSourceLocation(entry)}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 SourceFile。");
    AssertTrue(basicInfoMethod.Contains("SourceMember: {entry.SourceMemberName}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 SourceMember。");
}
```

- [ ] **Step 2: Run the focused harness and verify the expected RED failure**

Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

Expected: the new test fails with `异常日志表格不得声明或注册 Source 列。` because the current Designer still contains the two columns. The existing unrelated Designer assertion may stop later in the same harness; the new failure must appear before that stop.

### Task 2: Remove the two grid columns

**Files:**
- Modify: `AutoWeldSystem.UI/Views/LogManageView.Designer.cs` at the exception column declarations, initialization, `Columns.AddRange`, and two column property blocks.
- Modify: `AutoWeldSystem.UI/Views/LogManageView.cs` in `ApplyExceptionGridHeaders` and the nested `ExceptionLogRow`.

**Interfaces:**
- Consumes: Existing `dgvExceptionLogs` binding to `ExceptionLogRow`.
- Produces: A grid with only time, category, severity, exception type, and message columns; detail panel remains unchanged.

- [ ] **Step 1: Delete only the Designer references for the two columns**

Remove `colExceptionSource` and `colExceptionSourceLocation` from their field declarations, constructor initialization, `dgvExceptionLogs.Columns.AddRange(...)`, and their individual configuration blocks. Leave all unrelated Designer changes untouched.

- [ ] **Step 2: Remove only the runtime header assignments and row properties**

Delete these two assignments from `ApplyExceptionGridHeaders`:

```csharp
colExceptionSource.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSource);
colExceptionSourceLocation.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSourceLine);
```

Delete `ExceptionLogRow.Source` and `ExceptionLogRow.SourceLocation`; keep `ExceptionLogRow.Entry`, time, category, severity, exception type, message, and its type-name helper.

- [ ] **Step 3: Run the focused harness and verify GREEN for the new behavior**

Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

Expected: `PASS Exception grid omits source columns but keeps detail source`; the harness may later stop at the pre-existing `SystemSettingView.Designer.cs` `AutoSize` assertion.

### Task 3: Bump the semantic version

**Files:**
- Modify: `Directory.Build.props` lines 5-8.

**Interfaces:**
- Consumes: Current authoritative version `1.0.2`.
- Produces: Build metadata `1.0.3`, assembly/file metadata `1.0.3.0`.

- [ ] **Step 1: Update all four version properties together**

```xml
<Version>1.0.3</Version>
<AssemblyVersion>1.0.3.0</AssemblyVersion>
<FileVersion>1.0.3.0</FileVersion>
<InformationalVersion>1.0.3</InformationalVersion>
```

- [ ] **Step 2: Verify the versioned solution build**

Run: `dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\\artifacts\\verify-bin\\`

Expected: exit code 0 and 0 errors; the existing Designer warning is allowed.

### Task 4: Final verification and atomic commit

**Files:**
- Test: `AutoWeldSystem.Tests/Program.cs`
- Verify: `AutoWeldSystem.UI/Views/LogManageView.cs`
- Verify: `AutoWeldSystem.UI/Views/LogManageView.Designer.cs`
- Verify: `Directory.Build.props`

- [ ] **Step 1: Check the exact diff boundary**

Run: `git diff --check` for the four target files and `git status --short`.

Expected: no whitespace errors in target files; pre-existing user files remain unstaged.

- [ ] **Step 2: Stage only this feature**

```powershell
git add Directory.Build.props AutoWeldSystem.Tests/Program.cs AutoWeldSystem.UI/Views/LogManageView.cs AutoWeldSystem.UI/Views/LogManageView.Designer.cs
git diff --cached --check
git diff --cached --stat
```

- [ ] **Step 3: Commit the atomic change**

```powershell
git commit -m "fix(logs): remove redundant exception source columns"
```

- [ ] **Step 4: Confirm the commit and worktree boundary**

Run: `git show --stat --oneline HEAD; git status --short`

Expected: the commit contains exactly the four target files; unrelated user changes remain outside the commit.
