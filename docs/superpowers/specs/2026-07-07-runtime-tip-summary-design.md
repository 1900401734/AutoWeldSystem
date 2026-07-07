# Runtime Tip Summary Design

## Goal

Unify MonitorView runtime/error tips so `inputErrorTips` shows short localized business summaries, while `ProgramException` keeps the detailed local log record. Restart recovery must resolve text from resource keys instead of reusing newly written hardcoded Chinese text.

## Scope

This design covers `MonitorView`, `BizRuntimeTipState`, monitor text keys/resources, and focused console regression tests. It does not change MES, PLC, upload, database service contracts, or existing `ProgramException` log file format.

## Current State

`BizRuntimeTipState` already stores `RuntimeStatusKey`, `RuntimeStatusArgsJson`, `RuntimeErrorKey`, and `RuntimeErrorArgsJson`. It also stores fallback dynamic text fields. `MonitorView` currently uses both key-based calls and direct text calls such as `SetRuntimeErrorText(...)` and `SetRuntimeStatusText(...)`, which can persist hardcoded summaries and makes restart localization inconsistent.

## Approach

Use key-first runtime tips. Add organized summary keys under `TextKeys.Monitor.RuntimeStatus` and `TextKeys.Monitor.RuntimeError` for common dynamic states and business errors. Chinese summary values should stay concise, ideally within 20 characters. English values should be short and operational.

Keep detailed context in `ProgramException` by continuing to call `_exceptionLogService.WriteBusiness(...)` for business exceptions and `_exceptionLogService.Write(...)` for unexpected exceptions. The UI summary and log detail stay linked by the same source/call site and summary key.

## UI Behavior

Add a clear button inside `grpErrorTips`. The button is hidden when `inputErrorTips` is empty and visible whenever any current error summary exists. Clicking it calls the existing clear path, removes the current error summary from the UI, and persists the cleared state. PLC/device alarm auto-clear behavior remains intact.

## Persistence

New runtime status and error updates should save resource keys plus args. Existing dynamic text fields remain only as backward-compatible fallbacks for old cached rows. Restore logic should prefer key-based values and localize them at display time.

## Testing

Add focused source-level regressions in `AutoWeldSystem.Tests/Program.cs` to verify:

- the clear button exists, is wired, and visibility follows current error content;
- key-based runtime/error methods are used for common summaries;
- new summary keys exist in both `UiText.resx` and `UiText.en.resx`;
- restart-facing runtime/error state does not introduce new hardcoded Chinese summary persistence.

Validation should run `dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore`, then build the UI project. If normal `bin` output is locked, use `-p:BaseOutputPath=..\artifacts\verify-bin\`.
