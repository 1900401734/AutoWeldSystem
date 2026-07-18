# Product Process Copy-on-Add Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让地址维护页在选中已有产品工艺后点击“新增”时复制该行全部业务配置，只需再修改目标产品工号。

**Architecture:** 在 `AutoWeldSystem.Core` 增加一个纯工艺草稿工厂，显式复制业务字段并重置数据库身份字段；`AddressManageView` 只负责提供选中源行和现有默认值。控制台回归测试分别覆盖复制规则、无源默认值和页面接线。

**Tech Stack:** .NET 8、C#、WinForms、AntdUI、现有控制台回归测试框架。

## Global Constraints

- 有选中行时复制测试方案、工位、焊点、显示选项和全部 PLC 地址、长度、偏移表达式。
- 新记录暂时保留源产品工号，由现有重复校验阻止用户未修改产品工号就保存。
- 新记录的 `Id` 必须为 `0`，创建时间和更新时间必须是新草稿时间，不得覆盖源记录。
- 没有选中行时保持当前固定默认配置。
- 不修改数据库结构、服务接口、权限码或 WinForms Designer 布局。
- 保留工作区中已有的 `AddressManageView.Designer.cs` 和 `SystemSettingView.Designer.cs` 改动，不将其纳入本功能提交。

---

## File Map

- Create: `AutoWeldSystem.Core/Production/ProductProcessDraftRules.cs` — 创建复制草稿或默认草稿，不依赖 UI、数据库或 PLC。
- Modify: `AutoWeldSystem.Tests/Program.cs` — 注册并实现业务字段复制、身份重置、默认草稿和页面接线测试。
- Modify: `AutoWeldSystem.UI/Views/AddressManageView.cs` — 将“新增”事件改为调用草稿工厂，并继续选中新行。
- Add: `docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md` — 保存本功能的可复核实施步骤。

### Task 1: Product Process Draft Factory

**Files:**
- Create: `AutoWeldSystem.Core/Production/ProductProcessDraftRules.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Add: `docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md`

**Interfaces:**
- Consumes: `BizProductProcessConfig` 实体和由调用方提供的默认产品工号、默认方案 ID、草稿时间。
- Produces: `ProductProcessDraftRules.CreateDraft(BizProductProcessConfig? source, string? defaultProductNum, string? defaultSchemeId, DateTime draftTime)`。

- [x] **Step 1: 注册并编写失败测试**

在 `AutoWeldSystem.Tests/Program.cs` 的测试列表中增加：

```csharp
("Product process draft copies business fields and resets identity", ProductProcessDraftCopiesBusinessFieldsAndResetsIdentity),
("Product process draft keeps existing defaults without source", ProductProcessDraftKeepsExistingDefaultsWithoutSource),
```

在测试方法区增加：

```csharp
static void ProductProcessDraftCopiesBusinessFieldsAndResetsIdentity()
{
    var source = new BizProductProcessConfig
    {
        Id = 42,
        SchemeId = "S09",
        ProductNum = "P-001",
        StationNo = 2,
        TouchCount = 8,
        PointName = "相机",
        PointNoHeader = "相机序号",
        PointResultHeader = "相机结果",
        PointCountHeader = "相机数",
        ShowTestFlagInHistory = false,
        ProductBase = "DB20.0",
        ProductLen = 64,
        ProductNoExpr = "0:S-16",
        ProductResultExpr = "16:I-0",
        ActualTouchCountExpr = "18:I-0",
        PresetTouchCountExpr = "20:I-0",
        TouchBase = "DB20.64",
        TouchNoBase = "DB21.0",
        TouchResultBase = "DB22.0",
        TouchHeaderLen = 24,
        TouchNoExpr = "0:I-0",
        TouchResultExpr = "4:H-4",
        TestBase = "DB23.0",
        TestAreaLen = 96,
        Enabled = false,
        CreatedTime = new DateTime(2025, 1, 1),
        UpdatedTime = new DateTime(2025, 2, 1)
    };
    var draftTime = new DateTime(2026, 7, 18, 14, 30, 0);

    var draft = ProductProcessDraftRules.CreateDraft(source, "DEFAULT-P", "S01", draftTime);

    AssertEqual(0, draft.Id, "复制草稿必须保持新增身份。 ");
    AssertEqual(source.ProductNum, draft.ProductNum, "复制草稿应暂时保留源产品工号。 ");
    AssertEqual(source.SchemeId, draft.SchemeId, "测试方案应复制。 ");
    AssertEqual(source.StationNo, draft.StationNo, "工位应复制。 ");
    AssertEqual(source.TouchCount, draft.TouchCount, "焊点数量应复制。 ");
    AssertEqual(source.PointName, draft.PointName, "采集点名称应复制。 ");
    AssertEqual(source.PointNoHeader, draft.PointNoHeader, "编号表头应复制。 ");
    AssertEqual(source.PointResultHeader, draft.PointResultHeader, "结果表头应复制。 ");
    AssertEqual(source.PointCountHeader, draft.PointCountHeader, "数量表头应复制。 ");
    AssertEqual(source.ShowTestFlagInHistory, draft.ShowTestFlagInHistory, "历史显示选项应复制。 ");
    AssertEqual(source.ProductBase, draft.ProductBase, "产品头基地址应复制。 ");
    AssertEqual(source.ProductLen, draft.ProductLen, "产品头长度应复制。 ");
    AssertEqual(source.ProductNoExpr, draft.ProductNoExpr, "产品编号偏移应复制。 ");
    AssertEqual(source.ProductResultExpr, draft.ProductResultExpr, "产品结果偏移应复制。 ");
    AssertEqual(source.ActualTouchCountExpr, draft.ActualTouchCountExpr, "实际焊点数偏移应复制。 ");
    AssertEqual(source.PresetTouchCountExpr, draft.PresetTouchCountExpr, "预设焊点数偏移应复制。 ");
    AssertEqual(source.TouchBase, draft.TouchBase, "兼容焊点头基地址应复制。 ");
    AssertEqual(source.TouchNoBase, draft.TouchNoBase, "焊点编号基地址应复制。 ");
    AssertEqual(source.TouchResultBase, draft.TouchResultBase, "焊点结果基地址应复制。 ");
    AssertEqual(source.TouchHeaderLen, draft.TouchHeaderLen, "焊点头长度应复制。 ");
    AssertEqual(source.TouchNoExpr, draft.TouchNoExpr, "焊点编号偏移应复制。 ");
    AssertEqual(source.TouchResultExpr, draft.TouchResultExpr, "焊点结果偏移应复制。 ");
    AssertEqual(source.TestBase, draft.TestBase, "测试项基地址应复制。 ");
    AssertEqual(source.TestAreaLen, draft.TestAreaLen, "测试区长度应复制。 ");
    AssertEqual(source.Enabled, draft.Enabled, "启用状态应复制。 ");
    AssertEqual(draftTime, draft.CreatedTime, "复制草稿应使用新的创建时间。 ");
    AssertEqual(draftTime, draft.UpdatedTime, "复制草稿应使用新的更新时间。 ");

    draft.ProductBase = "DB99.0";
    AssertEqual("DB20.0", source.ProductBase, "修改草稿不得改变源配置。 ");
}

static void ProductProcessDraftKeepsExistingDefaultsWithoutSource()
{
    var draftTime = new DateTime(2026, 7, 18, 14, 35, 0);

    var draft = ProductProcessDraftRules.CreateDraft(null, "P-DEFAULT", "S-DEFAULT", draftTime);

    AssertEqual("P-DEFAULT", draft.ProductNum, "无源草稿应使用默认产品工号。 ");
    AssertEqual("S-DEFAULT", draft.SchemeId, "无源草稿应使用默认测试方案。 ");
    AssertEqual(ProductionConstants.Stations.SharedStationNo, draft.StationNo, "无源草稿应保持共享工位。 ");
    AssertEqual("DB8.0", draft.ProductBase, "无源草稿应保持原产品头基地址。 ");
    AssertEqual(32, draft.ProductLen, "无源草稿应保持原产品头长度。 ");
    AssertEqual("0:I-0", draft.ProductNoExpr, "无源草稿应保持原产品编号偏移。 ");
    AssertEqual("4:H-4", draft.ProductResultExpr, "无源草稿应保持原产品结果偏移。 ");
    AssertEqual("DB8.32", draft.TouchBase, "无源草稿应保持原焊点头基地址。 ");
    AssertEqual("DB8.100", draft.TestBase, "无源草稿应保持原测试项基地址。 ");
    AssertEqual(draftTime, draft.CreatedTime, "无源草稿应使用调用方时间。 ");
    AssertEqual(draftTime, draft.UpdatedTime, "无源草稿应使用调用方时间。 ");
}
```

- [x] **Step 2: 运行回归测试并确认红灯**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 编译失败，错误包含 `ProductProcessDraftRules` 不存在；失败原因必须是待实现规则缺失。

- [x] **Step 3: 实现最小草稿工厂**

创建 `AutoWeldSystem.Core/Production/ProductProcessDraftRules.cs`：

```csharp
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 创建产品工艺新增草稿，集中维护可复制的业务字段和不可复制的数据库身份字段。
/// </summary>
public static class ProductProcessDraftRules
{
    /// <summary>
    /// 有源配置时复制其业务内容；无源配置时保持地址维护页原有默认值。
    /// </summary>
    public static BizProductProcessConfig CreateDraft(
        BizProductProcessConfig? source,
        string? defaultProductNum,
        string? defaultSchemeId,
        DateTime draftTime)
    {
        if (source is null)
        {
            return CreateDefaultDraft(defaultProductNum, defaultSchemeId, draftTime);
        }

        return new BizProductProcessConfig
        {
            SchemeId = source.SchemeId,
            ProductNum = source.ProductNum,
            StationNo = source.StationNo,
            TouchCount = source.TouchCount,
            PointName = source.PointName,
            PointNoHeader = source.PointNoHeader,
            PointResultHeader = source.PointResultHeader,
            PointCountHeader = source.PointCountHeader,
            ShowTestFlagInHistory = source.ShowTestFlagInHistory,
            ProductBase = source.ProductBase,
            ProductLen = source.ProductLen,
            ProductNoExpr = source.ProductNoExpr,
            ProductResultExpr = source.ProductResultExpr,
            ActualTouchCountExpr = source.ActualTouchCountExpr,
            PresetTouchCountExpr = source.PresetTouchCountExpr,
            TouchBase = source.TouchBase,
            TouchNoBase = source.TouchNoBase,
            TouchResultBase = source.TouchResultBase,
            TouchHeaderLen = source.TouchHeaderLen,
            TouchNoExpr = source.TouchNoExpr,
            TouchResultExpr = source.TouchResultExpr,
            TestBase = source.TestBase,
            TestAreaLen = source.TestAreaLen,
            Enabled = source.Enabled,
            CreatedTime = draftTime,
            UpdatedTime = draftTime
        };
    }

    private static BizProductProcessConfig CreateDefaultDraft(
        string? defaultProductNum,
        string? defaultSchemeId,
        DateTime draftTime)
    {
        return new BizProductProcessConfig
        {
            ProductNum = defaultProductNum?.Trim() ?? string.Empty,
            SchemeId = string.IsNullOrWhiteSpace(defaultSchemeId) ? "S01" : defaultSchemeId.Trim(),
            StationNo = ProductionConstants.Stations.SharedStationNo,
            TouchCount = 1,
            PointName = "焊点",
            PointNoHeader = "焊点序号",
            PointResultHeader = "焊点结果",
            PointCountHeader = "焊点数",
            ShowTestFlagInHistory = true,
            ProductBase = "DB8.0",
            ProductLen = 32,
            ProductNoExpr = "0:I-0",
            ProductResultExpr = "4:H-4",
            TouchBase = "DB8.32",
            TouchNoBase = "DB8.32",
            TouchResultBase = "DB8.32",
            TouchHeaderLen = 16,
            TouchNoExpr = "0:I-0",
            TouchResultExpr = "4:H-4",
            TestBase = "DB8.100",
            TestAreaLen = 48,
            Enabled = true,
            CreatedTime = draftTime,
            UpdatedTime = draftTime
        };
    }
}
```

- [x] **Step 4: 运行回归测试并确认绿灯**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 进程退出码为 `0`，新增两项测试输出 `PASS`。

- [x] **Step 5: 检查并提交规则单元**

Run:

```powershell
git diff --check
git diff -- AutoWeldSystem.Core/Production/ProductProcessDraftRules.cs AutoWeldSystem.Tests/Program.cs docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md
git add -- AutoWeldSystem.Core/Production/ProductProcessDraftRules.cs AutoWeldSystem.Tests/Program.cs docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md
git diff --cached --check
git commit -m "feat(address): add product process draft rules"
```

Expected: 只提交规则、对应测试和实施计划，不包含两个 Designer 文件。

### Task 2: Wire Selected Row into Add Action

**Files:**
- Modify: `AutoWeldSystem.UI/Views/AddressManageView.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Modify: `docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md`

**Interfaces:**
- Consumes: `ProductProcessDraftRules.CreateDraft(BizProductProcessConfig? source, string? defaultProductNum, string? defaultSchemeId, DateTime draftTime)`。
- Produces: “新增”按钮使用 `_selectedProductProcessRow?.Source` 作为可选复制源，并保持现有刷新、选中和命令状态同步流程。

- [x] **Step 1: 注册并编写页面接线失败测试**

在测试列表中增加：

```csharp
("Address manage copies selected product process on add", AddressManageCopiesSelectedProductProcessOnAdd),
```

增加测试方法：

```csharp
static void AddressManageCopiesSelectedProductProcessOnAdd()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("ProductProcessDraftRules.CreateDraft(", StringComparison.Ordinal),
        "产品工艺新增入口必须复用核心草稿规则。 ");
    AssertTrue(
        viewCode.Contains("_selectedProductProcessRow?.Source", StringComparison.Ordinal),
        "产品工艺新增入口必须把当前选中行作为可选复制源。 ");
}
```

- [x] **Step 2: 运行回归测试并确认红灯**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 新增页面接线测试失败，消息为“产品工艺新增入口必须复用核心草稿规则”。

- [x] **Step 3: 将新增事件接入草稿工厂**

在 `AddressManageView.AddProductProcess_Click` 中保留默认产品工号和方案 ID 的计算，使用以下代码替换内联实体初始化：

```csharp
var config = ProductProcessDraftRules.CreateDraft(
    _selectedProductProcessRow?.Source,
    productNum,
    schemeId,
    DateTime.Now);
```

保留后续 `_productProcessConfigs.Add(config)`、筛选刷新、选中新行、摘要刷新和命令状态同步代码不变。

- [x] **Step 4: 运行完整测试和解决方案构建**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: 测试进程退出码为 `0`；构建退出码为 `0` 且 `0` 个错误。

- [x] **Step 5: 检查功能范围并提交 UI 接线**

Run:

```powershell
git diff --check
git diff -- AutoWeldSystem.UI/Views/AddressManageView.cs AutoWeldSystem.Tests/Program.cs docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md
git status --short
git add -- AutoWeldSystem.UI/Views/AddressManageView.cs AutoWeldSystem.Tests/Program.cs docs/superpowers/plans/2026-07-18-product-process-copy-on-add.md
git diff --cached --check
git diff --cached
git commit -m "feat(address): copy selected product process on add"
```

Expected: 提交只包含页面接线、接线回归测试和实施进度；两个既有 Designer 改动仍留在工作区。

### Task 3: Final Verification

**Files:**
- Verify only: all files changed by Tasks 1-2.

**Interfaces:**
- Consumes: Tasks 1-2 的两个功能提交。
- Produces: 可复核的测试、构建和 Git 范围证据。

- [ ] **Step 1: 重新运行完整验证**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: 两个命令退出码均为 `0`，构建为 `0` 个错误。

- [ ] **Step 2: 核对提交和剩余工作区**

Run:

```powershell
git log --oneline -4
git status --short --branch
git diff -- AutoWeldSystem.UI/Views/AddressManageView.Designer.cs AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs
```

Expected: 新增两个功能提交；剩余 Git 改动仅为用户原有的两个 Designer 文件，不执行推送或清理。
