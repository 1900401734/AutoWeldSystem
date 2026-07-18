# System Setting Responsive I18n Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `SystemSettingView` 改为适配不同 DPI 和窗口宽度的语义三列布局，并完整支持页面中英文切换，同时保持现有设置业务行为不变。

**Architecture:** 在 `AutoWeldSystem.Core.Runtime` 增加不依赖 WinForms 的纯布局规则，负责把设备像素宽度换算为 96 DPI 逻辑宽度并选择一列、两列或三列模式。Designer 静态声明滚动视口、响应式表格和左中右三个语义列；代码后置文件只根据纯规则调整表格行列。所有显示文字通过 `TextKeys.SystemSetting` 和成对 `.resx` 资源读取，MES 校验规则返回稳定错误码，由 UI 本地化错误消息。

**Tech Stack:** .NET 8、C#、Windows Forms、AntdUI、`.resx`/`ResourceManager`、现有控制台回归测试 `AutoWeldSystem.Tests`。

## Global Constraints

- 静态控件声明、初始化、分组归属和基础布局必须位于 `AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs`。
- 运行时响应式重排、语言刷新和事件处理必须位于 `AutoWeldSystem.UI/Views/SystemSettingView.cs`。
- 逻辑宽度断点固定为：`>= 1200` 三列、`760..1199` 两列、`< 760` 单列；逻辑宽度以 96 DPI 为基准。
- 三列顺序固定为：左列 PLC/设备，中列生产/应用/中心服务器，右列 MES；单列阅读顺序为 PLC、设备、生产、应用、中心服务器、MES。
- MES 内容区域独立纵向滚动，页面不得产生横向滚动。
- 不改变设置字段、配置键、默认值、保存格式、读取/校验/保存/连接测试行为。
- 语言切换不得清空当前输入、选择状态或滚动位置；长文本增加可用高度，不缩小字体。
- `tlpProductConfig` 第三行必须保持 `SizeType.AutoSize`。
- 不修改、暂存或提交 `AutoWeldSystem.UI/Views/AddressManageView.Designer.cs` 的现有工作区改动。
- 不提交 `appsettings.json`、`.vs`、`bin`、`obj`、`artifacts` 或其他机器本地内容。

## File Map

- Create: `AutoWeldSystem.Core/Runtime/SystemSettingLayoutRules.cs` — DPI 逻辑宽度换算和布局模式选择。
- Modify: `AutoWeldSystem.Core/Mes/MesEndpointRouteRules.cs` — 将面向用户的中文校验字符串改为稳定错误码。
- Modify: `AutoWeldSystem.Core/Constants/TextKeys.cs` — 声明系统设置页所有本地化键。
- Modify: `AutoWeldSystem.Core/Localization/UiText.resx` — 中文资源。
- Modify: `AutoWeldSystem.Core/Localization/UiText.en.resx` — 英文资源。
- Modify: `AutoWeldSystem.Services/LocalizationService.cs` — 缺失资源键回退并写入 Trace 警告。
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs` — 静态三列容器、自动尺寸和 MES 内部滚动。
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.cs` — 响应式重排、本地化绑定、错误码映射。
- Modify: `AutoWeldSystem.Tests/Program.cs` — 纯规则、Designer 结构、资源完整性和校验错误码回归测试。

---

### Task 1: DPI-aware responsive layout rules

**Files:**
- Create: `AutoWeldSystem.Core/Runtime/SystemSettingLayoutRules.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs:1-260`
- Test: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: 设备像素下的可用宽度 `int clientWidth` 和当前 DPI `int deviceDpi`。
- Produces: `SystemSettingLayoutMode ResolveMode(int clientWidth, int deviceDpi)`、`int ToLogicalWidth(int clientWidth, int deviceDpi)`，供 Task 2 的 UI 重排调用。

- [ ] **Step 1: Register and write the failing rule test**

在测试列表靠前位置注册测试，确保它在当前已知 Designer 回归之前运行：

```csharp
("System setting layout rules honor DPI breakpoints", SystemSettingLayoutRulesHonorDpiBreakpoints),
```

增加测试方法：

```csharp
static void SystemSettingLayoutRulesHonorDpiBreakpoints()
{
    AssertEqual(SystemSettingLayoutMode.SingleColumn, SystemSettingLayoutRules.ResolveMode(759, 96), "96 DPI 下 759 应为单列。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(760, 96), "96 DPI 下 760 应进入两列。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(1199, 96), "96 DPI 下 1199 应保持两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1200, 96), "96 DPI 下 1200 应进入三列。");

    AssertEqual(760, SystemSettingLayoutRules.ToLogicalWidth(950, 120), "125% DPI 应换算为 96 DPI 逻辑宽度。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(950, 120), "125% DPI 下 950 设备像素应为两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1500, 120), "125% DPI 下 1500 设备像素应为三列。");

    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(1140, 144), "150% DPI 下 1140 设备像素应为两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1800, 144), "150% DPI 下 1800 设备像素应为三列。");
    AssertEqual(SystemSettingLayoutMode.SingleColumn, SystemSettingLayoutRules.ResolveMode(-1, 0), "无效宽度和 DPI 必须安全回退。");
}
```

- [ ] **Step 2: Run the harness and verify the new test fails first**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 编译失败，提示 `SystemSettingLayoutRules` 或 `SystemSettingLayoutMode` 不存在。不要把后续已知 `SizeType.AutoSize` 回归误报为本任务失败。

- [ ] **Step 3: Add the minimal pure layout rule**

创建 `AutoWeldSystem.Core/Runtime/SystemSettingLayoutRules.cs`：

```csharp
namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// 系统设置页支持的响应式列模式。
/// </summary>
public enum SystemSettingLayoutMode
{
    SingleColumn,
    TwoColumns,
    ThreeColumns
}

/// <summary>
/// 将设备像素换算为逻辑宽度，并集中决定系统设置页的响应式模式。
/// </summary>
public static class SystemSettingLayoutRules
{
    public const int BaseDpi = 96;
    public const int TwoColumnMinimumLogicalWidth = 760;
    public const int ThreeColumnMinimumLogicalWidth = 1200;

    public static int ToLogicalWidth(int clientWidth, int deviceDpi)
    {
        var normalizedWidth = Math.Max(0, clientWidth);
        var normalizedDpi = deviceDpi > 0 ? deviceDpi : BaseDpi;
        return (int)Math.Floor(normalizedWidth * (double)BaseDpi / normalizedDpi);
    }

    public static SystemSettingLayoutMode ResolveMode(int clientWidth, int deviceDpi)
    {
        var logicalWidth = ToLogicalWidth(clientWidth, deviceDpi);
        if (logicalWidth >= ThreeColumnMinimumLogicalWidth)
        {
            return SystemSettingLayoutMode.ThreeColumns;
        }

        return logicalWidth >= TwoColumnMinimumLogicalWidth
            ? SystemSettingLayoutMode.TwoColumns
            : SystemSettingLayoutMode.SingleColumn;
    }
}
```

- [ ] **Step 4: Run the harness and verify the rule test passes**

Run the same command. Expected: output contains `PASS System setting layout rules honor DPI breakpoints`; until Task 2 restores the current Designer regression, the process may later stop at `Station display names load legacy defaults and collapse hidden row`.

- [ ] **Step 5: Commit the pure rule**

```powershell
git add -- AutoWeldSystem.Core/Runtime/SystemSettingLayoutRules.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "feat(settings): add responsive layout rules"
```

### Task 2: Static semantic columns and runtime relayout

**Files:**
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs:290-330, 2180-2400`
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.cs:90-180, 430-630`
- Modify: `AutoWeldSystem.Tests/Program.cs:7195-7235`
- Test: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: `SystemSettingLayoutRules.ResolveMode(int, int)` from Task 1.
- Produces: Designer controls `basicSettingsViewport`, `basicSettingsLayout`, `leftSettingsColumn`, `middleSettingsColumn`, `rightSettingsColumn`; code-behind method `ApplyBasicSettingsLayout(bool force = false)`.

- [ ] **Step 1: Replace the fixed-coordinate regression assertions with a failing semantic-layout test**

保留 `StationDisplayNamesLoadLegacyDefaultsAndCollapseHiddenRow` 的 `SizeType.AutoSize` 断言，删除对三个分组绝对 `Point/Size` 的解析及不再使用的 `ParseDesignerPointY`、`ParseDesignerSizeHeight`。注册并增加：

```csharp
("System setting view uses responsive semantic columns", SystemSettingViewUsesResponsiveSemanticColumns),
```

```csharp
static void SystemSettingViewUsesResponsiveSemanticColumns()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("private Panel basicSettingsViewport;", StringComparison.Ordinal), "Designer 必须声明基础设置滚动视口。");
    AssertTrue(designerCode.Contains("private TableLayoutPanel basicSettingsLayout;", StringComparison.Ordinal), "Designer 必须声明响应式主表格。");
    AssertTrue(designerCode.Contains("leftSettingsColumn.Controls.Add(grpPlcConfig, 0, 0);", StringComparison.Ordinal), "左列第一组必须是 PLC。");
    AssertTrue(designerCode.Contains("leftSettingsColumn.Controls.Add(grpDeviceConfig, 0, 1);", StringComparison.Ordinal), "左列第二组必须是设备。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpProductionConfig, 0, 0);", StringComparison.Ordinal), "中列第一组必须是生产。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpAppConfig, 0, 1);", StringComparison.Ordinal), "中列第二组必须是应用。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpCenterServerConfig, 0, 2);", StringComparison.Ordinal), "中列第三组必须是中心服务器。");
    AssertTrue(designerCode.Contains("rightSettingsColumn.Controls.Add(grpMesConfig, 0, 0);", StringComparison.Ordinal), "右列必须是 MES。");
    AssertTrue(designerCode.Contains("tableLayoutPanelMesConfig.AutoScroll = true;", StringComparison.Ordinal), "MES 内容必须独立滚动。");
    AssertFalse(designerCode.Contains("tabBasicSettings.Controls.Add(grpPlcConfig);", StringComparison.Ordinal), "分组不应继续直接使用页签绝对坐标。");
    AssertTrue(viewCode.Contains("SystemSettingLayoutRules.ResolveMode(basicSettingsViewport.ClientSize.Width, DeviceDpi)", StringComparison.Ordinal), "运行时必须按 DPI 逻辑宽度选择布局。");
    AssertTrue(viewCode.Contains("private void ApplyBasicSettingsLayout(bool force = false)", StringComparison.Ordinal), "代码后置文件必须提供统一重排入口。");
}
```

- [ ] **Step 2: Run the harness and confirm the semantic-layout test fails**

Run the console harness. Expected: `System setting view uses responsive semantic columns` fails because新容器尚不存在。

- [ ] **Step 3: Build the static three-column hierarchy in Designer**

在 Designer 中声明并初始化 `Panel` 和四个 `TableLayoutPanel`。关键静态结构必须等价于：

```csharp
tabBasicSettings.Controls.Add(basicSettingsViewport);

basicSettingsViewport.AutoScroll = true;
basicSettingsViewport.Dock = DockStyle.Fill;
basicSettingsViewport.Padding = new Padding(8);
basicSettingsViewport.Controls.Add(basicSettingsLayout);

basicSettingsLayout.AutoSize = true;
basicSettingsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
basicSettingsLayout.ColumnCount = 3;
basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
basicSettingsLayout.Controls.Add(leftSettingsColumn, 0, 0);
basicSettingsLayout.Controls.Add(middleSettingsColumn, 1, 0);
basicSettingsLayout.Controls.Add(rightSettingsColumn, 2, 0);
basicSettingsLayout.Dock = DockStyle.Top;
basicSettingsLayout.RowCount = 1;
basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

leftSettingsColumn.Controls.Add(grpPlcConfig, 0, 0);
leftSettingsColumn.Controls.Add(grpDeviceConfig, 0, 1);
middleSettingsColumn.Controls.Add(grpProductionConfig, 0, 0);
middleSettingsColumn.Controls.Add(grpAppConfig, 0, 1);
middleSettingsColumn.Controls.Add(grpCenterServerConfig, 0, 2);
rightSettingsColumn.Controls.Add(grpMesConfig, 0, 0);
```

三个列面板均使用单个 `100%` 列、`AutoSize = true`、`AutoSizeMode = GrowAndShrink`、`Dock = Top`，各行使用 `SizeType.AutoSize`。六个分组使用 `Dock = Top`、左右 `Margin = 6`，保留各自内容所需高度；`grpMesConfig` 使用明确的最小可见高度，`tableLayoutPanelMesConfig.AutoScroll = true`。同时恢复：

```csharp
tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.AutoSize));
```

不要使用 WinForms Designer 重新保存 `AddressManageView.Designer.cs`。

- [ ] **Step 4: Add idempotent runtime relayout in code-behind**

增加状态字段和入口：

```csharp
private SystemSettingLayoutMode? _lastLayoutMode;
private Size _lastLayoutViewportSize = Size.Empty;
private int _lastLayoutDpi;

protected override void OnSizeChanged(EventArgs e)
{
    base.OnSizeChanged(e);
    ApplyBasicSettingsLayout();
}

protected override void OnDpiChangedAfterParent(EventArgs e)
{
    base.OnDpiChangedAfterParent(e);
    ApplyBasicSettingsLayout(force: true);
}

private void ApplyBasicSettingsLayout(bool force = false)
{
    if (basicSettingsViewport.IsDisposed)
    {
        return;
    }

    var viewportSize = basicSettingsViewport.ClientSize;
    var mode = SystemSettingLayoutRules.ResolveMode(viewportSize.Width, DeviceDpi);
    if (!force && mode == _lastLayoutMode && viewportSize == _lastLayoutViewportSize && DeviceDpi == _lastLayoutDpi)
    {
        return;
    }

    basicSettingsLayout.SuspendLayout();
    try
    {
        ConfigureBasicSettingsGrid(mode);
        _lastLayoutMode = mode;
        _lastLayoutViewportSize = viewportSize;
        _lastLayoutDpi = DeviceDpi;
    }
    catch (Exception ex)
    {
        Trace.TraceWarning("SystemSettingView responsive layout failed: {0}", ex);
        ConfigureBasicSettingsGrid(SystemSettingLayoutMode.SingleColumn);
    }
    finally
    {
        basicSettingsLayout.ResumeLayout(true);
    }
}
```

`ConfigureBasicSettingsGrid` 只移动三个列面板，不重新创建输入控件：

```csharp
private void ConfigureBasicSettingsGrid(SystemSettingLayoutMode mode)
{
    basicSettingsLayout.ColumnStyles.Clear();
    basicSettingsLayout.RowStyles.Clear();
    basicSettingsLayout.SetColumnSpan(leftSettingsColumn, 1);
    basicSettingsLayout.SetColumnSpan(middleSettingsColumn, 1);
    basicSettingsLayout.SetColumnSpan(rightSettingsColumn, 1);

    switch (mode)
    {
        case SystemSettingLayoutMode.ThreeColumns:
            basicSettingsLayout.ColumnCount = 3;
            basicSettingsLayout.RowCount = 1;
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
            basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(1, 0));
            basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(2, 0));
            break;

        case SystemSettingLayoutMode.TwoColumns:
            basicSettingsLayout.ColumnCount = 2;
            basicSettingsLayout.RowCount = 2;
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
            basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(1, 0));
            basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(0, 1));
            basicSettingsLayout.SetColumnSpan(rightSettingsColumn, 2);
            break;

        default:
            basicSettingsLayout.ColumnCount = 1;
            basicSettingsLayout.RowCount = 3;
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
            basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(0, 1));
            basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(0, 2));
            break;
    }
}
```

在 `OnLoad` 完成基础加载后调用 `ApplyBasicSettingsLayout(force: true)`；`OnVisibleChanged` 在页面重新显示时调用一次普通重排。`OnLanguageChanged` 的滚动位置保留逻辑在 Task 3 完成。把成对复选框所在的固定高度行改为 `AutoSize` 且保留 45 逻辑像素的最小高度，使英文较长时通过增加行高完整显示，不缩小字体。

- [ ] **Step 5: Run regression tests and solution build**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: 新布局规则和 Designer 结构测试通过；原 `SizeType.AutoSize` 测试恢复通过；构建 0 errors。若仍有既有 `CS0169`，准确记录但不要把警告描述为错误。

- [ ] **Step 6: Commit only the responsive layout slice**

```powershell
git add -- AutoWeldSystem.Core/Runtime/SystemSettingLayoutRules.cs AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs AutoWeldSystem.UI/Views/SystemSettingView.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "feat(settings): add responsive semantic layout"
```

确认暂存差异中没有 `AddressManageView.Designer.cs`。

### Task 3: Complete page localization and localized validation errors

**Files:**
- Modify: `AutoWeldSystem.Core/Constants/TextKeys.cs:1089-1150`
- Modify: `AutoWeldSystem.Core/Localization/UiText.resx`
- Modify: `AutoWeldSystem.Core/Localization/UiText.en.resx`
- Modify: `AutoWeldSystem.Core/Mes/MesEndpointRouteRules.cs`
- Modify: `AutoWeldSystem.Services/LocalizationService.cs:25-40`
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.cs:20-75, 551-720, 1015-1060, 1060-1210`
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Test: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: 现有 `ILocalizationService.GetString(string, params object[])`。
- Produces: `MesEndpointValidationError`、完整 `TextKeys.SystemSetting` 资源键、仅保存稳定值的本地化选项绑定。

- [ ] **Step 1: Add failing localization and MES validation-code tests**

注册：

```csharp
("System setting localization resources are complete", SystemSettingLocalizationResourcesAreComplete),
("MES endpoint validation returns stable error codes", MesEndpointValidationReturnsStableErrorCodes),
("Localization service reports missing resource keys", LocalizationServiceReportsMissingResourceKeys),
```

增加以下完整测试方法。资源测试通过反射覆盖 `TextKeys.SystemSetting` 的全部现有和新增常量，因此不会只检查下表的子集：

```csharp
static void SystemSettingLocalizationResourcesAreComplete()
{
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var keys = typeof(TextKeys.SystemSetting)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    foreach (var key in keys)
    {
        AssertTrue(zhResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"中文资源必须包含 {key}。");
        AssertTrue(enResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"英文资源必须包含 {key}。");
    }

    var chineseLiteral = System.Text.RegularExpressions.Regex.Match(
        viewCode,
        "\"[^\"\\r\\n]*[\\u4e00-\\u9fff][^\"\\r\\n]*\"");
    AssertFalse(chineseLiteral.Success, $"SystemSettingView.cs 不应保留中文字符串字面量：{chineseLiteral.Value}");
    AssertTrue(viewCode.Contains("private sealed record LocalizedOption<T>(T Value, string TextKey);", StringComparison.Ordinal), "本地化选项必须统一保存稳定值和资源键。");
    AssertFalse(viewCode.Contains("record UploadModeOption", StringComparison.Ordinal), "不应继续为各下拉框维护重复的 DisplayName record。");
}

static void MesEndpointValidationReturnsStableErrorCodes()
{
    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute(" ", out _, out var required), "空路由必须失败。");
    AssertEqual(MesEndpointValidationError.Required, required, "空路由应返回 Required。");

    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute("https://mes/api/Test", out _, out var absolute), "完整 URL 必须失败。");
    AssertEqual(MesEndpointValidationError.AbsoluteUrlNotAllowed, absolute, "完整 URL 应返回 AbsoluteUrlNotAllowed。");

    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute("api/Test?id=1", out _, out var query), "带查询参数的路由必须失败。");
    AssertEqual(MesEndpointValidationError.QueryOrFragmentNotAllowed, query, "查询参数应返回 QueryOrFragmentNotAllowed。");

    AssertTrue(MesEndpointRouteRules.TryNormalizeRequiredRoute("/api/Test", out var route, out var routeError), "合法相对路由应通过。");
    AssertEqual("api/Test", route, "合法路由应去掉前导斜杠。");
    AssertEqual(MesEndpointValidationError.None, routeError, "合法路由应返回 None。");

    AssertFalse(MesEndpointRouteRules.TryValidatePostDataHeader(true, "Bad Key", "value", out _, out _, out var keyError), "非法 Header Key 必须失败。");
    AssertEqual(MesEndpointValidationError.InvalidHeaderKey, keyError, "非法 Header Key 应返回 InvalidHeaderKey。");

    AssertFalse(MesEndpointRouteRules.TryValidatePostDataHeader(true, "X-Test", " ", out _, out _, out var valueError), "空 Header Value 必须失败。");
    AssertEqual(MesEndpointValidationError.HeaderValueRequired, valueError, "空 Header Value 应返回 HeaderValueRequired。");

    AssertTrue(MesEndpointRouteRules.TryValidatePostDataHeader(false, "", "", out _, out _, out var disabledError), "未启用自定义 Header 时空值应通过。");
    AssertEqual(MesEndpointValidationError.None, disabledError, "未启用时应返回 None。");
}

static void LocalizationServiceReportsMissingResourceKeys()
{
    var settings = new FakeAppSettingsService();
    var localizer = new AutoWeldSystem.Services.LocalizationService(settings);
    using var writer = new StringWriter();
    using var listener = new System.Diagnostics.TextWriterTraceListener(writer);
    System.Diagnostics.Trace.Listeners.Add(listener);
    try
    {
        const string missingKey = "system.test.missing_key";
        AssertEqual(missingKey, localizer.GetString(missingKey), "缺失资源必须回退为原键。");
        listener.Flush();
        AssertTrue(writer.ToString().Contains(missingKey, StringComparison.Ordinal), "缺失资源必须写入 Trace 警告。");
    }
    finally
    {
        System.Diagnostics.Trace.Listeners.Remove(listener);
    }
}
```

- [ ] **Step 2: Run the harness and verify the new tests fail**

Run the console harness. Expected: 缺少资源键、错误枚举和 Trace 警告导致新增测试失败。

- [ ] **Step 3: Add stable MES validation errors without changing validation behavior**

在 `MesEndpointRouteRules.cs` 声明：

```csharp
public enum MesEndpointValidationError
{
    None,
    Required,
    AbsoluteUrlNotAllowed,
    QueryOrFragmentNotAllowed,
    InvalidHeaderKey,
    HeaderValueRequired
}
```

将两个 `Try...` 方法的最后一个 `out string errorMessage` 改为 `out MesEndpointValidationError error`。路由方法不再接收 `displayName`；空值返回 `Required`，绝对 URL 返回 `AbsoluteUrlNotAllowed`，查询参数/锚点返回 `QueryOrFragmentNotAllowed`。Header 方法保持规范化输出和启用开关语义，只把中文错误字符串替换为 `InvalidHeaderKey` 或 `HeaderValueRequired`。

- [ ] **Step 4: Add all resource keys and translations**

在 `TextKeys.SystemSetting` 增加与下表名称对应的常量，值统一使用小写点分隔形式：

| Constant | Resource key | 中文 | English |
|---|---|---|---|
| `GroupCenterServer` | `system.group.center_server` | 中心服务器 | Center Server |
| `LabelPlcFormatMode` | `system.label.plc_format_mode` | 处理方式 | Processing mode |
| `LabelMesTimeout` | `system.label.mes_timeout` | MES 超时（秒） | MES timeout (s) |
| `LabelProgramPath` | `system.label.program_path` | 程序目录 | Program directory |
| `LabelCenterServerUrl` | `system.label.center_server_url` | 中心服务器地址 | Center server URL |
| `LabelCenterServerSystemType` | `system.label.center_server_system_type` | 系统类型 | System type |
| `LabelCenterServerHeartbeat` | `system.label.center_server_heartbeat` | 心跳间隔（秒） | Heartbeat interval (s) |
| `LabelProcessParameterDeviceType` | `system.label.process_parameter_device_type` | 过程参数设备类型 | Process parameter device type |
| `LabelPostDataHeaderKey` | `system.label.postdata_header_key` | Header Key | Header Key |
| `LabelPostDataHeaderValue` | `system.label.postdata_header_value` | Header Value | Header Value |
| `ChkEnablePlcStringFormatting` | `system.checkbox.enable_plc_string_formatting` | 启用 PLC 字符串数值处理 | Enable PLC string numeric processing |
| `ChkEnablePlcAlarmReading` | `system.checkbox.enable_plc_alarm_reading` | 启用 PLC 报警读取 | Enable PLC alarm reading |
| `ChkEnableCenterServerSync` | `system.checkbox.enable_center_server_sync` | 启用中心服务器同步 | Enable center server sync |
| `ChkEnablePostDataHeader` | `system.checkbox.enable_postdata_header` | 启用 PostData 自定义 Header | Enable custom PostData header |
| `ChkShowTestFlagInHistory` | `system.checkbox.show_test_flag_history` | 产品历史显示试焊件 | Show test pieces in product history |
| `ChkEnableDeviceStatusReport` | `system.checkbox.enable_device_status_report` | 启用设备状态上报 | Enable device status reporting |
| `ChkEnableWorkOrderStatusReport` | `system.checkbox.enable_work_order_status_report` | 启用工单状态上报 | Enable work order status reporting |
| `OptionPlcFormatTruncate` | `system.option.plc_format.truncate` | 固定长度裁切 | Fixed-length truncation |
| `OptionPlcFormatRound` | `system.option.plc_format.round` | 四舍五入 | Round |
| `OptionUploadRealtime` | `system.option.upload.realtime` | 单件实时上传 | Real-time per item |
| `OptionUploadQuantity` | `system.option.upload.quantity` | 按特定数量上传 | Upload by quantity |
| `OptionUploadBatch` | `system.option.upload.batch` | 完工批量上传 | Batch on completion |
| `OptionDeviceElectromagnetic` | `system.option.device.electromagnetic` | 电磁系统 | Electromagnetic system |
| `OptionDeviceWholePieceCheck` | `system.option.device.whole_piece_check` | 整件系统-检测设备 | Whole-piece inspection equipment |
| `OptionDeviceWholePieceWeld` | `system.option.device.whole_piece_weld` | 整件系统-点焊设备 | Whole-piece spot-welding equipment |
| `OptionCenterWholePiece` | `system.option.center.whole_piece` | 整件系统 | Whole-piece system |
| `OptionCenterOther` | `system.option.center.other` | 其它 | Other |
| `RouteUser` | `system.route.user` | 员工信息路由 | Employee route |
| `RouteWorkOrder` | `system.route.work_order` | 工单信息路由 | Work order route |
| `RouteServerTime` | `system.route.server_time` | 服务器时间路由 | Server time route |
| `RouteProgram` | `system.route.program` | 程序管理路由 | Program route |
| `RouteStartWork` | `system.route.start_work` | 开工上报路由 | Start report route |
| `RouteWorkStatus` | `system.route.work_status` | 工单状态路由 | Work status route |
| `RouteEndWork` | `system.route.end_work` | 完工上报路由 | Completion report route |
| `RouteReportFile` | `system.route.report_file` | 报告文件路由 | Report file route |
| `RoutePostData` | `system.route.post_data` | PostData 路由 | PostData route |
| `RouteDevice` | `system.route.device` | 设备编号路由 | Device route |
| `RouteDeviceStatus` | `system.route.device_status` | 设备状态路由 | Device status route |
| `MessageRuntimeModeLocked` | `system.message.runtime_mode_locked` | 存在未完工任务，不能切换双工位/双工单模式，请先完工后再调整。 | An unfinished task exists. Complete it before changing dual-station or dual-work-order mode. |
| `MessageDeviceManagementLocked` | `system.message.device_management_locked` | 存在未完工任务，请先完工后再修改设备管理信息。 | An unfinished task exists. Complete it before changing device management settings. |
| `MessagePositiveIntegerRequired` | `system.message.positive_integer_required` | {0} 必须是大于 0 的整数。 | {0} must be an integer greater than 0. |
| `MessageRouteRequired` | `system.message.route_required` | {0}不能为空。 | {0} is required. |
| `MessageRelativeRouteRequired` | `system.message.relative_route_required` | {0}请填写相对路由，例如 api/ExpProgram，不要填写完整 URL。 | Enter a relative route for {0}, such as api/ExpProgram, not a full URL. |
| `MessageRouteQueryNotAllowed` | `system.message.route_query_not_allowed` | {0}不能包含查询参数或锚点。 | {0} cannot contain query parameters or fragments. |
| `MessageHeaderKeyInvalid` | `system.message.header_key_invalid` | PostData Header Key 不能为空，且不能包含空格、冒号或中文字符。 | PostData Header Key is required and cannot contain spaces, colons, or non-ASCII characters. |
| `MessageHeaderValueRequired` | `system.message.header_value_required` | PostData Header Value 不能为空。 | PostData Header Value is required. |
| `MessageStartupIntegrationFailed` | `system.message.startup_integration_failed` | 开机自启设置失败：{0} | Failed to update startup integration: {0} |

`OptionDeviceElectromagnetic` 同时用于过程参数和中心服务器系统类型，避免重复资源。

- [ ] **Step 5: Convert option and route definitions to text keys**

把五种重复 option record 合并为：

```csharp
private sealed record LocalizedOption<T>(T Value, string TextKey);
```

所有静态 option 数组只保存稳定值和 `TextKeys.SystemSetting.*`。所有 `Bind...Options()` 使用 `_localizer.GetString(option.TextKey)` 生成显示项，并继续用 `option.Value` 恢复选择，禁止用显示文本入库。

把 `MesRouteInputDefinition.DisplayName` 改为 `TextKey`，增加 `GetMesRouteLabel(string key)` 与现有 input switch 一一对应。`ApplyLocalizedTexts()` 遍历定义，设置 route label；保存校验时用 `_localizer.GetString(definition.TextKey)` 作为错误消息参数。

- [ ] **Step 6: Localize every visible control and message**

`ApplyLocalizedTexts()` 中所有赋值都改为 `_localizer.GetString(TextKeys.SystemSetting.*)`，包括中心服务器、PLC 格式、MES 超时、程序目录、PostData、试焊件和两个状态上报复选框。两个未完工提示改用 `ShowWarning(key)`，正整数提示改用 `ShowWarning(MessagePositiveIntegerRequired, fieldName)`；开机自启失败改用 `ShowWarning(MessageStartupIntegrationFailed, startupResult.Message)`，把系统返回的动态错误作为本地化提示的参数。

增加错误码映射：

```csharp
private string GetMesValidationMessage(MesEndpointValidationError error, string fieldName = "")
{
    var key = error switch
    {
        MesEndpointValidationError.Required => TextKeys.SystemSetting.MessageRouteRequired,
        MesEndpointValidationError.AbsoluteUrlNotAllowed => TextKeys.SystemSetting.MessageRelativeRouteRequired,
        MesEndpointValidationError.QueryOrFragmentNotAllowed => TextKeys.SystemSetting.MessageRouteQueryNotAllowed,
        MesEndpointValidationError.InvalidHeaderKey => TextKeys.SystemSetting.MessageHeaderKeyInvalid,
        MesEndpointValidationError.HeaderValueRequired => TextKeys.SystemSetting.MessageHeaderValueRequired,
        _ => string.Empty
    };

    return string.IsNullOrEmpty(key)
        ? string.Empty
        : _localizer.GetString(key, fieldName);
}
```

语言切换时先保存：

```csharp
var scrollOffset = new Point(
    -basicSettingsViewport.AutoScrollPosition.X,
    -basicSettingsViewport.AutoScrollPosition.Y);
```

刷新全部文本和选项后调用 `ApplyBasicSettingsLayout(force: true)`，再用 `basicSettingsViewport.AutoScrollPosition = scrollOffset` 恢复滚动位置。不得重新创建输入控件。

- [ ] **Step 7: Trace missing localization keys**

在 `LocalizationService.GetString(string key)` 中读取一次资源；为 `null` 时执行：

```csharp
Trace.TraceWarning("Missing localization resource '{0}' for culture '{1}'.", key, CurrentLanguage);
return key;
```

正常资源直接返回，不写警告。加入 `using System.Diagnostics;`。

- [ ] **Step 8: Run tests and build**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: 全部控制台测试退出码 0；构建 0 errors。记录但不隐藏既有警告。

- [ ] **Step 9: Commit the localization slice**

```powershell
git add -- AutoWeldSystem.Core/Constants/TextKeys.cs AutoWeldSystem.Core/Localization/UiText.resx AutoWeldSystem.Core/Localization/UiText.en.resx AutoWeldSystem.Core/Mes/MesEndpointRouteRules.cs AutoWeldSystem.Services/LocalizationService.cs AutoWeldSystem.UI/Views/SystemSettingView.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "feat(settings): localize all system settings text"
```

### Task 4: Visual matrix, final verification, and delivery readiness

**Files:**
- Modify only if verification finds an in-scope defect: files listed in Tasks 1-3.
- Do not modify: `AutoWeldSystem.UI/Views/AddressManageView.Designer.cs`.

**Interfaces:**
- Consumes: 完整响应式布局和资源化页面。
- Produces: 可复核的测试、构建和视觉验收证据；干净的本功能提交边界。

- [ ] **Step 1: Inspect commit and worktree boundaries**

```powershell
git log --oneline -5
git status --short
git diff -- AutoWeldSystem.UI/Views/AddressManageView.Designer.cs
git diff --check
```

Expected: 本功能提交只包含计划列出的文件；`AddressManageView.Designer.cs` 仍是未暂存的用户改动，没有被覆盖。

- [ ] **Step 2: Run the complete automated verification**

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: 测试退出码 0；构建 0 errors。任何失败都必须先按 `systematic-debugging` 查明原因，不得仅凭构建成功宣称全部验证通过。

- [ ] **Step 3: Perform the visual acceptance matrix**

启动 UI：

```powershell
dotnet run --project AutoWeldSystem.UI\AutoWeldSystem.UI.csproj --no-restore
```

分别在 Windows 显示缩放 100%、125%、150% 下检查中文和英文；每种组合调整到三列、两列、单列宽度。逐项确认：

- 没有文字截断、控件重叠或页面级横向滚动。
- MES 只在自身内容区纵向滚动。
- 列顺序符合全局约束。
- 切换语言、调整窗口宽度后，未保存输入和选择保持不变。
- 单工位时工位名称行折叠，不留下空白高度。

- [ ] **Step 4: Fix only verified in-scope visual defects and re-run verification**

若发现缺陷，先增加能覆盖该缺陷的规则或源码回归测试，再做最小修复。重新运行 Task 4 Step 2 和受影响的视觉组合。修复提交使用：

```powershell
git add -p -- AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs AutoWeldSystem.UI/Views/SystemSettingView.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "fix(settings): stabilize responsive settings layout"
```

没有缺陷时不创建空提交。

- [ ] **Step 5: Prepare push without creating a PR**

```powershell
git fetch origin codex/production-workflow-stabilization
git status --short --branch
git log --oneline origin/codex/production-workflow-stabilization..HEAD
```

确认远端未前进后，执行普通 push；若远端前进，先把本功能提交变基到最新远端并重新运行 Task 4 Step 2，禁止强制推送。最终推送命令：

```powershell
git push origin codex/production-workflow-stabilization
```

推送后核对本地 `HEAD` 与 `git rev-parse origin/codex/production-workflow-stabilization` 一致，不创建 PR。
