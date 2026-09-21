# PLC 状态悬浮面板拟亚克力优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将监控页 PLC 状态悬浮提示改为高 DPI 友好的拟亚克力卡片，并以中英文显示精简后的状态历史。

**Architecture:** 保留 `MonitorView` 的现有悬停、刷新和释放流程，仅将运行时容器替换为项目已引用的 `AntdUI.Panel`。文本仍由一个运行时标签承载，新的格式化和定位辅助方法留在 `MonitorView`，以避免引入 UI 抽象层或新窗体。

**Tech Stack:** .NET 8、WinForms、AntdUI 2.3.9、现有控制台源码回归 harness。

## Global Constraints

- 不新增 NuGet 包、DWM/PInvoke、顶层窗体或真实背景模糊。
- 不修改 `MonitorView.Designer.cs`、PLC 通讯接口、PLC 状态发布或生产流程。
- 历史仅显示和保留当前工位最近五条；当前详情保留完整原始诊断消息。
- UI 标签、字段名、状态历史标题和是/否文本必须中英文切换；原始 PLC/HSL 消息不得翻译。
- 本次为小范围 UI 优化，版本从 `1.0.8` 调整为 `1.0.9`，程序集和文件版本为 `1.0.9.0`。
- 同步 `docs/QUICK_START.md` 当前版本说明。
- 本次不创建 Git 提交或推送；发布由后续明确请求决定。

---

### Task 1: Add a source-level regression test

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs` near the existing MonitorView source-shape tests.
- Test: `AutoWeldSystem.Tests/Program.cs` (`PlcStatusTooltipUsesCompactLocalizedAcrylicPanel`)

**Interfaces:**
- Consumes: existing `GetRepoFilePath`, `ExtractMethodText`, `AssertTrue` and `AssertFalse` helpers.
- Produces: a failing harness case that locks the approved visual, history and localization boundaries before production edits.

- [x] **Step 1: Register the regression case and add its assertions**

```csharp
("PLC status tooltip uses compact localized acrylic panel", PlcStatusTooltipUsesCompactLocalizedAcrylicPanel),

static void PlcStatusTooltipUsesCompactLocalizedAcrylicPanel()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var textKeysCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("AntdUI.Panel? _plcStatusToolTipPanel", StringComparison.Ordinal),
        "PLC 状态悬浮提示必须使用现有 AntdUI.Panel。" );
    AssertTrue(viewCode.Contains("private const int PlcStatusHistoryLimit = 5;", StringComparison.Ordinal),
        "PLC 状态历史必须限制为最近五条。" );
    AssertTrue(viewCode.Contains("FormatCompactPlcStatusHistoryEntry", StringComparison.Ordinal),
        "PLC 状态历史必须使用紧凑格式。" );
    AssertFalse(viewCode.Contains("当前读取时间", StringComparison.Ordinal),
        "悬浮提示不应每次刷新都显示当前读取时间。" );
    AssertTrue(viewCode.Contains("Screen.FromControl(tagPLC).WorkingArea", StringComparison.Ordinal),
        "悬浮提示定位必须受当前屏幕工作区约束。" );

    foreach (var key in new[]
    {
        "monitor.plc_tooltip.title", "monitor.plc_tooltip.connected",
        "monitor.plc_tooltip.recent_history", "monitor.plc_tooltip.yes",
        "monitor.plc_tooltip.no"
    })
    {
        AssertTrue(textKeysCode.Contains(key, StringComparison.Ordinal), $"TextKeys 必须声明 {key}。" );
        AssertTrue(zhResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"中文资源必须包含 {key}。" );
        AssertTrue(enResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"英文资源必须包含 {key}。" );
    }
}
```

- [x] **Step 2: Run the harness and verify RED**

Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

Expected: the new tooltip case fails because the current implementation still uses `Panel`, ten historical items and hardcoded Chinese text. Existing unrelated failures must be reported separately if encountered.

### Task 2: Implement the localized compact card

**Files:**
- Modify: `AutoWeldSystem.UI/Views/MonitorView.cs` in the PLC tooltip constants, popup lifecycle, content formatting and placement methods.
- Modify: `AutoWeldSystem.Core/Constants/TextKeys.cs` under `TextKeys.Monitor`.
- Modify: `AutoWeldSystem.Core/Localization/UiText.resx`.
- Modify: `AutoWeldSystem.Core/Localization/UiText.en.resx`.

**Interfaces:**
- Consumes: existing `IPlcCommunicationService.GetCurrent`, `PlcConnectionSnapshot`, `GetLocalizedPlcStateText`, `UiColors.Table`, `OnLanguageChanged`, and `AntdUI.Panel`.
- Produces: a lazy-created noninteractive card that remains a child of `MonitorView` and accepts no public API changes.

- [x] **Step 1: Introduce compact card metrics and replace only the popup container**

```csharp
private const int PlcStatusToolTipMaxWidth = 480;
private const int PlcStatusHistoryLimit = 5;
private const int PlcStatusToolTipPadding = 10;
private const int PlcStatusToolTipRadius = 8;
private const int PlcStatusToolTipShadow = 6;

private AntdUI.Panel? _plcStatusToolTipPanel;
private Font? _plcStatusToolTipFont;
```

Create the panel lazily with `Back = UiColors.Table.HeaderBackColor`, `BorderColor = UiColors.Table.GridLineColor`, `BorderWidth = 1F`, scaled `Radius` and `Shadow`, and a nonanimated shadow. Keep the existing child `Label`, but set its background to the same neutral color, its font to a cached `Microsoft YaHei UI` 9pt font, and its location/size with DPI-scaled inner padding. Dispose the cached font with the popup.

- [x] **Step 2: Localize headings and format current details plus five compact history entries**

Add `TextKeys.Monitor.PlcToolTip` constants and matching Chinese/English resource values for title, station, current status, connection status, endpoint, connected/heartbeat times, current message, recent history, empty history, yes/no, and a history entry template.

Replace hardcoded tooltip text with `_localizer.GetString(...)` calls. Keep `FormatToolTipValue(snapshot.Message)` for the current detail. Format each history item through a helper that preserves the raw message but normalizes whitespace and truncates it with the existing `NormalizeRuntimeSummary` helper:

```csharp
private string FormatCompactPlcStatusHistoryEntry(PlcStatusHistoryEntry entry)
{
    var message = NormalizeRuntimeSummary(entry.Message);
    return _localizer.GetString(
        TextKeys.Monitor.PlcToolTip.HistoryEntry,
        entry.ChangedTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
        GetLocalizedPlcStateText(entry.State),
        string.IsNullOrWhiteSpace(message) ? "--" : message);
}
```

Remove the current-read-time line and the repeated enum/connection fields from history. Change the shared history retention constant to five so the in-memory list cannot grow beyond what the tooltip uses.

- [x] **Step 3: Clamp card size and position at the active DPI**

Add a small `ScalePlcStatusToolTipMetric(int logicalValue)` helper using `DeviceDpi / 96F`. During text updates, calculate a DPI-scaled maximum label width from the smaller of the configured width and available client width. During display, start below `tagPLC`, use `Screen.FromControl(tagPLC).WorkingArea` to flip left/up if needed, then clamp the converted client point to `ClientSize`.

```csharp
var workingArea = Screen.FromControl(tagPLC).WorkingArea;
var anchor = tagPLC.PointToScreen(new Point(0, tagPLC.Height + gap));
var x = Math.Clamp(anchor.X, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - popupSize.Width));
var y = anchor.Y + popupSize.Height <= workingArea.Bottom
    ? anchor.Y
    : Math.Max(workingArea.Top, tagPLC.PointToScreen(Point.Empty).Y - popupSize.Height - gap);
```

- [x] **Step 4: Run the harness and verify GREEN for the new case**

Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

Expected: `PASS PLC status tooltip uses compact localized acrylic panel`; any separately reported existing assertion must remain outside this feature's source diff.

### Task 3: Synchronize version metadata and verify the deliverable

**Files:**
- Modify: `Directory.Build.props`.
- Modify: `docs/QUICK_START.md`.
- Modify: `docs/superpowers/specs/2026-07-21-plc-status-tooltip-design.md`.
- Verify: files from Tasks 1 and 2.

**Interfaces:**
- Consumes: semantic-version policy and `Directory.Build.props` as the version source of truth.
- Produces: build metadata and documentation that consistently report `1.0.9`.

- [x] **Step 1: Update all authoritative version text**

```xml
<Version>1.0.9</Version>
<AssemblyVersion>1.0.9.0</AssemblyVersion>
<FileVersion>1.0.9.0</FileVersion>
<InformationalVersion>1.0.9</InformationalVersion>
```

Change the `docs/QUICK_START.md` current-version sentence from `1.0.8` to `1.0.9`.

- [x] **Step 2: Run source, whitespace, harness and build verification**

```powershell
git diff --check
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -m:1 -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: no whitespace errors; the new regression case passes; build exits with zero errors. The known permitted `CS0169` warning may remain.

- [x] **Step 3: Review scope without publishing**

Run: `git diff --stat; git status --short`

Expected: only the planned source, resource, version and documentation files are changed, and no commit, push or PR is created.
