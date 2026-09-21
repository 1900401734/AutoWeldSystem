# MES 设备状态 0/1 语义调整 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 MES 设备生命周期状态稳定遵循 `0=停机`、`1=开机`，且不影响 PLC 原始状态编码。

**Architecture:** MES 生命周期状态由 `ProductionConstants.MesDeviceStatuses` 集中定义；所有启动、退出和默认状态路径只引用语义常量。只交换 `PoweredOn` 与 `Stopped` 的字符串值，`DeviceStatusReportRules` 的现有名称映射即可自动产生正确文本。

**Tech Stack:** .NET 8、C#、控制台回归测试程序集。

## Global Constraints

- 仅修改 MES 生命周期状态 `0`、`1` 的语义；PLC 原始状态 `1/2/3/4` 保持不变。
- 不迁移数据库、本地 JSONL 日志或历史 MES 上报数据。
- 保持 `PoweredOn`、`Stopped` 常量名称及调用接口不变，避免扩大调用方改动。
- 所有生产代码改动必须先由失败的控制台回归测试定义。

## File Structure

- `AutoWeldSystem.Core/Constants/ProductionConstants.cs`：MES 状态码的唯一业务定义。
- `AutoWeldSystem.Tests/Program.cs`：验证状态码、状态名称和状态标识文本。
- `docs/superpowers/specs/2026-07-15-mes-device-status-code-semantics-design.md`：已确认的设计范围，不再修改。

---

### Task 1: 交换 MES 开机与停机状态码

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs:894-903`
- Modify: `AutoWeldSystem.Tests/Program.cs:935-940`
- Modify: `AutoWeldSystem.Core/Constants/ProductionConstants.cs:161-180`

**Interfaces:**
- Consumes: `ProductionConstants.MesDeviceStatuses.PoweredOn` 和 `ProductionConstants.MesDeviceStatuses.Stopped`。
- Produces: `PoweredOn == "1"`、`Stopped == "0"`；`DeviceStatusReportRules.GetStatusName("0") == "停机"`、`DeviceStatusReportRules.GetStatusName("1") == "开机"`。

- [ ] **Step 1: 将现有 MES 状态规则测试改为目标语义**

  在 `MesDeviceStatusRulesUseConfiguredMesCodes` 中替换前两项断言，并补充状态名称断言：

  ```csharp
  AssertEqual("1", ProductionConstants.MesDeviceStatuses.PoweredOn, "MES 设备状态 1 必须表示软件开机。");
  AssertEqual("0", ProductionConstants.MesDeviceStatuses.Stopped, "MES 设备状态 0 必须表示软件停机。");
  AssertEqual("停机", DeviceStatusReportRules.GetStatusName("0"), "状态名称需要按 MES 语义显示。");
  AssertEqual("开机", DeviceStatusReportRules.GetStatusName("1"), "状态名称需要按 MES 语义显示。");
  ```

  在 `MesDeviceStatusRulesFormatStatusIdentity` 中替换前两项断言：

  ```csharp
  AssertEqual("0-停机", DeviceStatusReportRules.FormatStatusIdentity("0"), "停机状态标识应包含状态码和描述。");
  AssertEqual("1-开机", DeviceStatusReportRules.FormatStatusIdentity("1"), "开机状态标识应包含状态码和描述。");
  ```

- [ ] **Step 2: 运行控制台回归测试，确认它因旧编码失败**

  Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

  Expected: `MES device status rules use configured MES codes` 失败，原因是当前 `PoweredOn` 仍为 `"0"`，而测试要求 `"1"`。

- [ ] **Step 3: 以最小改动交换 MES 常量值**

  在 `ProductionConstants.MesDeviceStatuses` 中替换两个常量定义：

  ```csharp
  public const string PoweredOn = "1";        // Software started
  public const string Stopped = "0";          // Software stopped
  ```

  不修改 `Exception`、`Recovered`、`ProgramStarted`、`ProgramEnded`，也不修改 `PlcDeviceStatuses`。

- [ ] **Step 4: 运行控制台回归测试，确认新语义通过且无回归**

  Run: `dotnet run --project AutoWeldSystem.Tests\\AutoWeldSystem.Tests.csproj --no-restore`

  Expected: 退出码为 `0`，并包含通过的 `MES device status rules use configured MES codes` 与 `MES device status rules format status identity`。

- [ ] **Step 5: 使用独立输出目录编译整个解决方案**

  Run: `dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\\artifacts\\verify-bin\\`

  Expected: 退出码为 `0`，无编译错误；独立输出目录避免运行中的 WinForms 程序锁定默认 `bin` 文件。

- [ ] **Step 6: 在用户要求提交时创建范围明确的提交**

  ```powershell
  git add -- AutoWeldSystem.Core/Constants/ProductionConstants.cs AutoWeldSystem.Tests/Program.cs docs/superpowers/specs/2026-07-15-mes-device-status-code-semantics-design.md docs/superpowers/plans/2026-07-15-mes-device-status-code-semantics.md
  git commit -m "fix(status): align mes power state codes"
  ```
