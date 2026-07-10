# Recipe Code Dropdown Sort Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the MonitorView recipe-code dropdown show recipe codes in ascending order, so values like `1`, `2`, `3`, `4`, `10` are not displayed in the current unstable source order.

**Architecture:** Keep the sort rule in `AutoWeldSystem.Core/Production/OfflineStartInputRules.cs` because both online and offline MonitorView dropdown bindings need the same normalized recipe-code ordering. `MonitorView` should only gather candidate recipe codes and bind the returned ordered list.

**Tech Stack:** .NET 8, C#, Windows Forms, AntdUI `Select`, existing console regression harness in `AutoWeldSystem.Tests/Program.cs`.

## Global Constraints

- Do not discard or reset the existing dirty working tree; current modified files already contain a separate offline program dropdown fix.
- Use `apply_patch` for manual edits.
- Keep the change limited to recipe-code option ordering in MonitorView and pure Core rules.
- Preserve existing filtering: blank recipe codes stay hidden, duplicate recipe-code text is shown once, and program-name dropdown ordering is not part of this change.
- Numeric recipe codes must sort by numeric value, so `10` comes after `4`, not between `1` and `2`.
- Non-numeric recipe codes, if any, sort after numeric codes by case-insensitive text.
- Add focused regression tests to `AutoWeldSystem.Tests/Program.cs` before changing implementation.
- Validate with the repo-native test harness and alternate build output path.

---

## File Structure

- Modify `AutoWeldSystem.Core/Production/OfflineStartInputRules.cs`
  - Add `BuildRecipeCodeOptions(IEnumerable<string?> recipeCodes)` as the reusable rule for dropdown recipe-code display.
  - Add a private numeric parse helper used only by the sort rule.
- Modify `AutoWeldSystem.UI/Views/MonitorView.cs`
  - Change `BindOnlineRecipeCodeOptions(...)` to call the Core rule after resolving local recipe codes from MES program items.
  - Change `BindOfflineRecipeCodeOptions(...)` to call the same Core rule for local program options.
- Modify `AutoWeldSystem.Tests/Program.cs`
  - Add a pure rule regression for numeric ascending order.
  - Add a source-level regression that both MonitorView recipe dropdown bindings use the shared rule.

---

### Task 1: Add Recipe-Code Sort Rule

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Modify: `AutoWeldSystem.Core/Production/OfflineStartInputRules.cs`

**Interfaces:**
- Consumes: `BizProgram.RecipeCode` values and online-resolved recipe-code strings.
- Produces: `OfflineStartInputRules.BuildRecipeCodeOptions(IEnumerable<string?> recipeCodes): IReadOnlyList<string>`.

- [ ] **Step 1: Register the failing regression test**

In `AutoWeldSystem.Tests/Program.cs`, add this item near the existing offline program dropdown tests:

```csharp
("Recipe code options sort numeric ascending", RecipeCodeOptionsSortNumericAscending),
```

Place it near these existing registrations:

```csharp
("Offline program dropdown displays program name", OfflineProgramDropdownDisplaysProgramName),
("Offline program dropdown includes empty-content program", OfflineProgramDropdownIncludesEmptyContentProgram),
("Recipe code options sort numeric ascending", RecipeCodeOptionsSortNumericAscending),
("Offline start request follows inline monitor input", OfflineStartRequestFollowsInlineMonitorInput),
```

- [ ] **Step 2: Add the failing pure rule test**

In `AutoWeldSystem.Tests/Program.cs`, add this method immediately after `OfflineProgramDropdownIncludesEmptyContentProgram()`:

```csharp
static void RecipeCodeOptionsSortNumericAscending()
{
    var options = OfflineStartInputRules.BuildRecipeCodeOptions(new[]
    {
        "3",
        "1",
        "10",
        "2",
        "4",
        " 2 ",
        string.Empty,
        null,
        "A2",
        "A1"
    });

    AssertSequenceEqual(
        new[] { "1", "2", "3", "4", "10", "A1", "A2" },
        options,
        "配方号候选列表应先按数字正序显示，非数字配方号排在数字后按文本正序显示。");
}
```

- [ ] **Step 3: Run the regression to verify it fails**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: FAIL with a compile error similar to:

```text
'OfflineStartInputRules' does not contain a definition for 'BuildRecipeCodeOptions'
```

- [ ] **Step 4: Implement the shared sort rule**

In `AutoWeldSystem.Core/Production/OfflineStartInputRules.cs`, add this public method inside `OfflineStartInputRules`, after `BuildProgramNameOptions(...)` and before `BuildRequest(...)`:

```csharp
/// <summary>
/// Creates normalized recipe-code options for MonitorView dropdowns.
/// Numeric recipe codes use numeric ascending order so 10 is listed after 4.
/// </summary>
/// <param name="recipeCodes">Candidate recipe-code values from local programs or MES-program mappings.</param>
/// <returns>Distinct non-empty recipe codes in operator-friendly ascending order.</returns>
public static IReadOnlyList<string> BuildRecipeCodeOptions(IEnumerable<string?> recipeCodes)
{
    ArgumentNullException.ThrowIfNull(recipeCodes);

    return recipeCodes
        .Select(Normalize)
        .Where(recipeCode => !string.IsNullOrWhiteSpace(recipeCode))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(recipeCode => TryParseRecipeCodeNumber(recipeCode, out _) ? 0 : 1)
        .ThenBy(recipeCode => TryParseRecipeCodeNumber(recipeCode, out var number) ? number : long.MaxValue)
        .ThenBy(recipeCode => recipeCode, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
```

In the same class, add this private helper near the existing private helpers:

```csharp
private static bool TryParseRecipeCodeNumber(string recipeCode, out long number)
{
    return long.TryParse(recipeCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
}
```

- [ ] **Step 5: Run the rule test to verify it passes**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: PASS, including `Recipe code options sort numeric ascending`.

- [ ] **Step 6: Commit Task 1 if working independently**

Only commit this task after verifying no unrelated hunks are staged:

```powershell
git diff -- AutoWeldSystem.Core\Production\OfflineStartInputRules.cs AutoWeldSystem.Tests\Program.cs
git add -p AutoWeldSystem.Core\Production\OfflineStartInputRules.cs AutoWeldSystem.Tests\Program.cs
git diff --cached
git commit -m "fix(monitor): sort recipe code dropdown options"
```

If the current branch already contains the earlier empty-content dropdown fix in the same files, keep commits separated by hunk with `git add -p`.

---

### Task 2: Use Rule In MonitorView Bindings

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Modify: `AutoWeldSystem.UI/Views/MonitorView.cs`

**Interfaces:**
- Consumes: `OfflineStartInputRules.BuildRecipeCodeOptions(IEnumerable<string?> recipeCodes): IReadOnlyList<string>` from Task 1.
- Produces: sorted `selectRecipeCode.Items` for both online and offline MonitorView start inputs.

- [ ] **Step 1: Register the failing source-level regression**

In `AutoWeldSystem.Tests/Program.cs`, add this item near the existing MonitorView program/recipe selection tests:

```csharp
("Monitor view recipe dropdown uses sorted recipe options", MonitorViewRecipeDropdownUsesSortedRecipeOptions),
```

Place it near this existing registration group:

```csharp
("Monitor view stabilizes online program selection", MonitorViewStabilizesOnlineProgramSelection),
("Monitor view links program and recipe selections for start input", MonitorViewLinksProgramAndRecipeSelectionsForStartInput),
("Monitor view recipe dropdown uses sorted recipe options", MonitorViewRecipeDropdownUsesSortedRecipeOptions),
("Monitor view uses PLC recipe only for offline idle inputs", MonitorViewUsesPlcRecipeOnlyForOfflineIdleInputs),
```

- [ ] **Step 2: Add the failing MonitorView regression**

In `AutoWeldSystem.Tests/Program.cs`, add this method immediately after `MonitorViewLinksProgramAndRecipeSelectionsForStartInput()`:

```csharp
static void MonitorViewRecipeDropdownUsesSortedRecipeOptions()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var onlineMethod = ExtractMethodText(
        viewCode,
        "private void BindOnlineRecipeCodeOptions",
        "    #endregion");
    var offlineMethod = ExtractMethodText(
        viewCode,
        "private void BindOfflineRecipeCodeOptions",
        "private void ApplyOfflineProgramNameOption");

    AssertTrue(
        onlineMethod.Contains("OfflineStartInputRules.BuildRecipeCodeOptions", StringComparison.Ordinal),
        "在线配方号下拉必须使用共享规则排序，避免 MES 程序列表顺序导致 3、1、2、4 乱序显示。");
    AssertTrue(
        offlineMethod.Contains("OfflineStartInputRules.BuildRecipeCodeOptions", StringComparison.Ordinal),
        "离线配方号下拉必须使用共享规则排序，避免本地程序库顺序导致配方号乱序显示。");
}
```

- [ ] **Step 3: Run the regression to verify it fails**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: FAIL with a message similar to:

```text
在线配方号下拉必须使用共享规则排序
```

- [ ] **Step 4: Update online recipe dropdown binding**

In `AutoWeldSystem.UI/Views/MonitorView.cs`, replace the local `recipeCodes` query inside `BindOnlineRecipeCodeOptions(...)`:

```csharp
var recipeCodes = programs
    .Select(ResolveRecipeCodeForPendingProgram)
    .Select(NormalizeRecipeCode)
    .Where(recipeCode => !string.IsNullOrWhiteSpace(recipeCode))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();
```

with:

```csharp
var recipeCodes = OfflineStartInputRules.BuildRecipeCodeOptions(
    programs.Select(ResolveRecipeCodeForPendingProgram));
```

Do not change the later `selectedIndex` or `ForceRecipeCodeSelection(...)` logic.

- [ ] **Step 5: Update offline recipe dropdown binding**

In `AutoWeldSystem.UI/Views/MonitorView.cs`, replace the local `recipeCodes` query inside `BindOfflineRecipeCodeOptions(...)`:

```csharp
var recipeCodes = options
    .Select(option => NormalizeRecipeCode(option.Program.RecipeCode))
    .Where(recipeCode => !string.IsNullOrWhiteSpace(recipeCode))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();
```

with:

```csharp
var recipeCodes = OfflineStartInputRules.BuildRecipeCodeOptions(
    options.Select(option => option.Program.RecipeCode));
```

Do not change program-name dropdown ordering or the offline selected program fallback.

- [ ] **Step 6: Run tests to verify MonitorView bindings pass**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: PASS, including `Monitor view recipe dropdown uses sorted recipe options`.

- [ ] **Step 7: Commit Task 2 if working independently**

Only commit this task after verifying no unrelated hunks are staged:

```powershell
git diff -- AutoWeldSystem.UI\Views\MonitorView.cs AutoWeldSystem.Tests\Program.cs
git add -p AutoWeldSystem.UI\Views\MonitorView.cs AutoWeldSystem.Tests\Program.cs
git diff --cached
git commit -m "fix(monitor): apply recipe code sort to start dropdowns"
```

If Task 1 and Task 2 are implemented in the same short session, one combined commit is acceptable because the rule and UI usage are tightly coupled.

---

### Task 3: Full Verification

**Files:**
- Verify: `AutoWeldSystem.Tests/Program.cs`
- Verify: `AutoWeldSystem.sln`

**Interfaces:**
- Consumes: completed changes from Task 1 and Task 2.
- Produces: evidence that the recipe-code dropdown ordering change compiles and passes existing regression coverage.

- [ ] **Step 1: Check formatting-sensitive diff output**

Run:

```powershell
git diff --check
```

Expected:

```text
```

No output means no whitespace errors.

- [ ] **Step 2: Run the console regression harness**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: PASS with no failed regression case.

- [ ] **Step 3: Build the solution with alternate output path**

Run:

```powershell
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

- [ ] **Step 4: Inspect final diff before handoff**

Run:

```powershell
git diff -- AutoWeldSystem.Core\Production\OfflineStartInputRules.cs AutoWeldSystem.UI\Views\MonitorView.cs AutoWeldSystem.Tests\Program.cs
```

Expected: diff contains only the shared recipe-code sort rule, MonitorView calls to that rule, and the two focused tests from this plan.

---

## Self-Review

- Spec coverage: The plan sorts the candidate recipe-code list in both online and offline MonitorView dropdowns, with numeric ascending order for the screenshot case `3`, `1`, `2`, `4`.
- Placeholder scan: No step uses placeholders such as TBD or generic "write tests" language; each code-changing step includes exact code.
- Type consistency: `BuildRecipeCodeOptions(IEnumerable<string?>): IReadOnlyList<string>` is defined in Task 1 and consumed by Task 2 with existing `FindIndex(...)` logic.
