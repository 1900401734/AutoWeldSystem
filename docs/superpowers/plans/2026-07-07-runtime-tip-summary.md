# Runtime Tip Summary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make MonitorView runtime/error tips key-based, localized, concise, and manually clearable.

**Architecture:** Keep the existing MonitorView persistence service and ProgramException log service. Add concise resource keys, route common MonitorView status/error summaries through keys, and show a clear button whenever the current error summary is non-empty.

**Tech Stack:** .NET 8, C# WinForms, AntdUI, RESX localization, existing console regression harness.

---

### Task 1: Add Source-Level Guard Tests

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs`

- [ ] Add a regression test that reads `MonitorView.Designer.cs`, `MonitorView.cs`, `TextKeys.cs`, `UiText.resx`, and `UiText.en.resx`.
- [ ] Assert that `btnClearErrorTips` exists, is wired to `ClearRuntimeError`, and clear button visibility is refreshed from current error text.
- [ ] Assert new concise runtime/error summary keys exist in `TextKeys` and both resource files.
- [ ] Run `dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore` and confirm the new test fails before implementation.

### Task 2: Add Localized Summary Keys

**Files:**
- Modify: `AutoWeldSystem.Core/Constants/TextKeys.cs`
- Modify: `AutoWeldSystem.Core/Localization/UiText.resx`
- Modify: `AutoWeldSystem.Core/Localization/UiText.en.resx`

- [ ] Add concise runtime status keys for program confirmation, work-order ready, process selection, station action success, recipe write/validation success, product collection result, and test flag update.
- [ ] Add concise runtime error keys for read-only operation, business signal write failure, recipe validation failure, station busy, station report failure, finish quantity read failure, and device alarm.
- [ ] Keep Chinese values short and operational; keep English values concise.

### Task 3: Add Error Clear Button

**Files:**
- Modify: `AutoWeldSystem.UI/Views/MonitorView.Designer.cs`
- Modify: `AutoWeldSystem.UI/Views/MonitorView.cs`

- [ ] Add `btnClearErrorTips` inside `grpErrorTips`.
- [ ] Dock `inputErrorTips` to fill and place the clear button on the right.
- [ ] Wire `btnClearErrorTips.Click` to `ClearRuntimeError()`.
- [ ] Update `RefreshRuntimeError`, `ApplyRuntimeErrorTone`, and `ClearRuntimeError` so the button is visible only when a current error summary exists.

### Task 4: Migrate Common Runtime Tips to Keys

**Files:**
- Modify: `AutoWeldSystem.UI/Views/MonitorView.cs`

- [ ] Replace common `SetRuntimeStatusText(...)` calls with key-based `SetRuntimeStatus(...)` calls.
- [ ] Replace common `SetRuntimeErrorText(...)` calls with key-based `SetRuntimeError(...)` calls.
- [ ] Leave fallback text overloads for old dynamic text and rare unknown details, but do not use them for restart-facing common summaries.
- [ ] Keep `ProgramException` detailed writes unchanged.

### Task 5: Verify

**Files:**
- Run only; no intended file edits.

- [ ] Run `dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore`.
- [ ] Run `dotnet build AutoWeldSystem.UI\AutoWeldSystem.UI.csproj --no-restore`.
- [ ] If UI build is blocked by locked `bin`, run `dotnet build AutoWeldSystem.UI\AutoWeldSystem.UI.csproj --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\`.
