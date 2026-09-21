# Device Status Timestamp And Shutdown Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep software start/stop timestamps identical between device lifecycle logs and device status logs, preserve millisecond precision, and trigger one non-blocking shutdown status upload with retry deferred to the next startup.

**Architecture:** Treat software lifecycle device statuses as audit events with a shared `occurredTime`. Avoid reloading `BizDeviceStatusLog` from MySQL before writing local JSONL, because the database can truncate milliseconds. For shutdown, write the local status immediately, trigger a single background MES upload attempt, and rely on the existing upload task retry path on the next app start if that attempt fails.

**Tech Stack:** .NET 8 WinForms, C#, SqlSugar, local JSONL logs, existing console regression harness in `AutoWeldSystem.Tests/Program.cs`.

---

## File Structure

- Modify `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs`
  - Add optional `occurredTime`, `forceWrite`, and `reportInBackground` parameters to the device status write contract.
- Modify `AutoWeldSystem.Services/Production/DeviceStatusService.cs`
  - Preserve in-memory millisecond timestamps after MES upload.
  - Support single-attempt background reporting for shutdown without blocking callers.
  - Continue enqueueing failed device status uploads for next startup retry.
- Modify `AutoWeldSystem.Services/Log/DeviceLifecycleLogCoordinator.cs`
  - Use the same timestamp for `SoftwareStarted`/`0=开机`.
  - Use the same timestamp for `SoftwareStopped`/`1=停机`.
  - Trigger shutdown upload before software closes without waiting for MES.
- Modify `AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs`
  - De-duplicate multiple local JSONL records for the same database log id, keeping the latest report state.
- Modify `AutoWeldSystem.Services/Production/UploadTaskService.cs`
  - When next-start retry updates a device status log, append the updated log to local JSONL so the log page reflects retry results.
- Modify `AutoWeldSystem.Tests/Program.cs`
  - Add focused regression tests for millisecond preservation, timestamp synchronization, background shutdown upload, and local retry-state refresh.

---

### Task 1: Add Regression Tests For Timestamp Precision And Sync

**Files:**
- Modify: `AutoWeldSystem.Tests/Program.cs`

- [ ] **Step 1: Add tests to the test list**

Add these entries near the existing device status and lifecycle tests:

```csharp
("Device status report keeps millisecond timestamp after MES upload", DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload),
("Device lifecycle coordinator syncs software status timestamps", DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps),
("Device lifecycle stop triggers background status upload", DeviceLifecycleStopTriggersBackgroundStatusUpload),
("Device status local log store keeps latest state per log id", DeviceStatusLocalLogStoreKeepsLatestStatePerLogId),
```

- [ ] **Step 2: Add a fake MES provider result capture**

Extend `FakeMesProvider` so tests can inspect device status requests:

```csharp
public List<ReportDeviceStatusReq> DeviceStatusRequests { get; } = new();

public BasicRes<object> DeviceStatusResponse { get; set; } = new()
{
    Status = AppConstants.MesStatus.Success,
    Msg = "操作成功"
};

public Task<BasicRes<object>> ReportDeviceStatusAsync(ReportDeviceStatusReq requestData, CancellationToken cancellationToken = default)
{
    DeviceStatusRequests.Add(requestData);
    return Task.FromResult(DeviceStatusResponse);
}
```

Remove or replace the existing `throw new NotSupportedException()` body for `ReportDeviceStatusAsync`.

- [ ] **Step 3: Add a millisecond preservation test**

Add this test near `DeviceStatusLocalLogStoreWritesAndReadsJsonl`:

```csharp
static void DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload()
{
    var occurredTime = new DateTime(2026, 7, 7, 17, 11, 42, 724);
    var service = CreateDeviceStatusServiceForUnitTest(out var mesProvider, out var settings);

    var log = service.ChangeStatusAsync(
        ProductionConstants.MesDeviceStatuses.PoweredOn,
        "Software started successfully.",
        "Application",
        stationNo: ProductionConstants.Stations.SharedStationNo,
        occurredTime: occurredTime,
        forceWrite: true).GetAwaiter().GetResult();

    AssertEqual(occurredTime, log.OccurredTime, "MES 上传后返回的设备状态日志不能丢失毫秒。");
    AssertEqual(1, mesProvider.DeviceStatusRequests.Count, "开机状态应触发一次 MES 设备状态上传。");
    AssertEqual("2026-07-07 17:11:42", mesProvider.DeviceStatusRequests[0].Ts, "MES 接口时间格式仍按接口约定到秒。");

    var localLogs = DeviceStatusLocalLogStore.Read(
        settings.Current,
        occurredTime.Date,
        occurredTime.Date.AddDays(1).AddTicks(-1),
        maxCount: 10);

    AssertEqual(1, localLogs.Count, "本地设备状态日志应写入一条记录。");
    AssertEqual(occurredTime, localLogs[0].OccurredTime, "本地设备状态日志必须保留毫秒。");
}
```

- [ ] **Step 4: Add a lifecycle sync test**

Use the existing `DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps` test shape, but assert both start and stop:

```csharp
static void DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");
    coordinator.Stop();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped),
        "停机设备状态日志应在停止后写入。");

    var softwareStarted = lifecycleLogs.Entries.Single(entry => entry.EventType == AppConstants.DeviceLifecycleEventTypes.SoftwareStarted);
    var poweredOn = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn);
    var softwareStopped = lifecycleLogs.Entries.Single(entry => entry.EventType == AppConstants.DeviceLifecycleEventTypes.SoftwareStopped);
    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);

    AssertEqual(softwareStarted.OccurredTime, poweredOn.OccurredTime, "设备日志的软件开启时间必须和设备状态开机时间一致。");
    AssertEqual(softwareStopped.OccurredTime, stopped.OccurredTime, "设备日志的软件关闭时间必须和设备状态停机时间一致。");
}
```

- [ ] **Step 5: Add a shutdown background upload test**

Add this test near the lifecycle tests:

```csharp
static void DeviceLifecycleStopTriggersBackgroundStatusUpload()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");

    coordinator.Stop();

    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);
    AssertEqual(ProductionConstants.MesDeviceStatuses.Stopped, stopped.DeviceStatus, "停止协调器时必须写入停机状态。");
    AssertTrue(statusService.LastReportInBackground == true, "停机状态应触发后台上传，不能同步阻塞 UI。");
    AssertTrue(statusService.LastReportToMes == true, "停机状态应先尝试 MES 上传，而不是只进入待上传队列。");
}
```

- [ ] **Step 6: Add a local log de-duplication test**

Add this test near `DeviceStatusLocalLogStoreWritesAndReadsJsonl`:

```csharp
static void DeviceStatusLocalLogStoreKeepsLatestStatePerLogId()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusLogDedupeTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 7, 7, 17, 11, 42, 724);

    try
    {
        var pending = new BizDeviceStatusLog
        {
            Id = 100,
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
            Source = "Application",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending,
            ReportMessage = "Shutdown upload triggered."
        };
        var uploaded = new BizDeviceStatusLog
        {
            Id = 100,
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
            Source = "Application",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Uploaded,
            ReportMessage = "操作成功",
            ReportTime = occurredTime.AddSeconds(1)
        };

        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings), "待上传状态应写入本地日志。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(uploaded, settings), "重试成功状态应追加写入本地日志。");

        var logs = DeviceStatusLocalLogStore.Read(settings, occurredTime.Date, occurredTime.Date.AddDays(1).AddTicks(-1), 10);

        AssertEqual(1, logs.Count, "同一个设备状态日志 Id 只应显示最新状态。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, logs[0].ReportStatus, "本地日志读取应保留最新上传状态。");
        AssertEqual(occurredTime, logs[0].OccurredTime, "本地日志去重不能丢失原始毫秒。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
```

- [ ] **Step 7: Run tests and verify failure**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected before implementation: at least one new test fails because milliseconds are lost after DB/report flow or shutdown status is not marked as background MES upload.

---

### Task 2: Preserve Millisecond Precision In DeviceStatusService

**Files:**
- Modify: `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs`
- Modify: `AutoWeldSystem.Services/Production/DeviceStatusService.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`

- [ ] **Step 1: Update the interface signature**

In `IDeviceStatusService.ChangeStatusAsync`, use this full signature:

```csharp
Task<BizDeviceStatusLog> ChangeStatusAsync(
    string deviceStatus,
    string? remark = null,
    string source = "Software",
    bool reportToMes = true,
    int stationNo = ProductionConstants.Stations.DefaultStationNo,
    int? weldTaskId = null,
    string? workOrderId = null,
    DateTime? occurredTime = null,
    bool forceWrite = false,
    bool reportInBackground = false,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Update the service signature**

In `DeviceStatusService.ChangeStatusAsync`, match the same parameters:

```csharp
public async Task<BizDeviceStatusLog> ChangeStatusAsync(
    string deviceStatus,
    string? remark = null,
    string source = "Software",
    bool reportToMes = true,
    int stationNo = ProductionConstants.Stations.DefaultStationNo,
    int? weldTaskId = null,
    string? workOrderId = null,
    DateTime? occurredTime = null,
    bool forceWrite = false,
    bool reportInBackground = false,
    CancellationToken cancellationToken = default)
```

- [ ] **Step 3: Preserve the original timestamp when creating the log**

Keep `CreateLog(...)` accepting `DateTime? occurredTime` and assign:

```csharp
OccurredTime = occurredTime ?? DateTime.Now,
```

- [ ] **Step 4: Stop replacing the log with a DB re-read after upload**

Replace the end of `ReportStatusAsync` with this logic:

```csharp
lock (_dbLock)
{
    log.ReportStatus = response.IsSuccess
        ? ProductionConstants.UploadStatuses.Uploaded
        : ProductionConstants.UploadStatuses.Failed;
    log.ReportTime = DateTime.Now;
    log.ReportMessage = response.Msg;

    _dbContext.Db.Updateable(log)
        .UpdateColumns(it => new { it.ReportStatus, it.ReportTime, it.ReportMessage })
        .Where(it => it.Id == log.Id)
        .ExecuteCommand();

    if (!response.IsSuccess)
    {
        EnqueueDeviceStatusUpload(log);
    }

    return log;
}
```

This is the critical fix: do not call `InSingle(log.Id)` here, because MySQL may return `OccurredTime` with `.000`.

- [ ] **Step 5: Keep MES payload format unchanged**

Leave both `Ts` assignments as second-level strings:

```csharp
Ts = log.OccurredTime.ToString("yyyy-MM-dd HH:mm:ss"),
```

MES upload payload remains compatible while local UI keeps milliseconds.

- [ ] **Step 6: Update FakeDeviceStatusService signature**

In `AutoWeldSystem.Tests/Program.cs`, update the fake method signature:

```csharp
public bool? LastReportToMes { get; private set; }

public bool? LastReportInBackground { get; private set; }

public Task<BizDeviceStatusLog> ChangeStatusAsync(
    string deviceStatus,
    string? remark = null,
    string source = "Software",
    bool reportToMes = true,
    int stationNo = ProductionConstants.Stations.DefaultStationNo,
    int? weldTaskId = null,
    string? workOrderId = null,
    DateTime? occurredTime = null,
    bool forceWrite = false,
    bool reportInBackground = false,
    CancellationToken cancellationToken = default)
{
    LastReportToMes = reportToMes;
    LastReportInBackground = reportInBackground;
    var log = new BizDeviceStatusLog
    {
        DeviceStatus = deviceStatus,
        Remark = remark,
        Source = source,
        StationNo = stationNo,
        WeldTaskId = weldTaskId,
        WorkOrderId = workOrderId,
        OccurredTime = occurredTime ?? DateTime.Now
    };
    Logs.Add(log);
    StatusChanged?.Invoke(this, log);
    return Task.FromResult(log);
}
```

- [ ] **Step 7: Run the targeted tests**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected after this task: timestamp precision tests pass, but shutdown background upload may still fail until Task 3.

---

### Task 3: Trigger Shutdown Upload Without Blocking UI

**Files:**
- Modify: `AutoWeldSystem.Services/Production/DeviceStatusService.cs`
- Modify: `AutoWeldSystem.Services/Log/DeviceLifecycleLogCoordinator.cs`

- [ ] **Step 1: Add background reporting branch**

In `ChangeStatusAsync`, replace the current report section with:

```csharp
if (CurrentSettings.EnableDeviceStatusReport == false)
{
    log = MarkSkipped(log, "Device status report is disabled in system settings.");
    WriteLocalStatusLog(log);
    StatusChanged?.Invoke(this, log);
    return log;
}

if (reportToMes && reportInBackground)
{
    WriteLocalStatusLog(log);
    StatusChanged?.Invoke(this, log);
    _ = Task.Run(() => ReportStatusInBackgroundAsync(log));
    return log;
}

if (reportToMes)
{
    log = await ReportStatusAsync(log, cancellationToken);
}
else
{
    EnqueueDeviceStatusUpload(log);
}

WriteLocalStatusLog(log);
StatusChanged?.Invoke(this, log);
return log;
```

- [ ] **Step 2: Add the background helper**

Add this private method in `DeviceStatusService`:

```csharp
private async Task ReportStatusInBackgroundAsync(BizDeviceStatusLog log)
{
    try
    {
        var updatedLog = await ReportStatusAsync(log, CancellationToken.None);
        WriteLocalStatusLog(updatedLog);
        StatusChanged?.Invoke(this, updatedLog);
    }
    catch (Exception ex)
    {
        log.ReportStatus = ProductionConstants.UploadStatuses.Failed;
        log.ReportTime = DateTime.Now;
        log.ReportMessage = ex.Message;
        EnqueueDeviceStatusUpload(log);
        WriteLocalStatusLog(log);
        StatusChanged?.Invoke(this, log);
    }
}
```

This method makes exactly one MES attempt. If it fails, the existing upload task is the retry mechanism for the next startup.

- [ ] **Step 3: Update software start status call**

In `DeviceLifecycleLogCoordinator.Start()`, keep the shared timestamp:

```csharp
var occurredTime = DateTime.Now;
_logService.Write(DeviceLifecycleLogRules.CreateSoftwareStartedEntry(CurrentDeviceId, occurredTime));
RecordSoftwareStartedStatus(occurredTime);
RecordInitialConnectionSnapshots();
```

In `RecordSoftwareStartedStatus(DateTime occurredTime)`, call:

```csharp
await _deviceStatusService.ChangeStatusAsync(
    ProductionConstants.MesDeviceStatuses.PoweredOn,
    "Software started successfully.",
    SourceApplication,
    stationNo: ProductionConstants.Stations.SharedStationNo,
    occurredTime: occurredTime,
    forceWrite: true);
```

- [ ] **Step 4: Update software stop status call**

In `DeviceLifecycleLogCoordinator.Stop()`, use:

```csharp
var occurredTime = DateTime.Now;
_logService.Write(DeviceLifecycleLogRules.CreateSoftwareStoppedEntry(CurrentDeviceId, occurredTime));
RecordSoftwareStoppedStatus(occurredTime);
```

Replace `RecordSoftwareStoppedStatus` with:

```csharp
private void RecordSoftwareStoppedStatus(DateTime occurredTime)
{
    try
    {
        _ = _deviceStatusService.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.Stopped,
            "Software is closing.",
            SourceApplication,
            reportToMes: true,
            stationNo: ProductionConstants.Stations.SharedStationNo,
            occurredTime: occurredTime,
            forceWrite: true,
            reportInBackground: true);
    }
    catch
    {
        // Shutdown must continue even if the local status log cannot be written.
    }
}
```

Do not call `.GetAwaiter().GetResult()` in the shutdown path.

- [ ] **Step 5: Run the targeted tests**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected after this task: shutdown background upload test passes.

---

### Task 4: Keep Local Device Status Logs Current After Retry

**Files:**
- Modify: `AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs`
- Modify: `AutoWeldSystem.Services/Production/UploadTaskService.cs`

- [ ] **Step 1: De-duplicate local records by database id**

In `DeviceStatusLocalLogStore.Read(...)`, insert a helper after filtering:

```csharp
return DeduplicateByLogId(
        EnumerateCandidateDates(from, to)
            .SelectMany(date => ReadDate(settings, date, take))
            .Where(entry => IsInRange(entry, from, to)))
    .OrderByDescending(entry => entry.OccurredTime)
    .Take(take)
    .ToList();
```

Add this helper:

```csharp
private static IEnumerable<BizDeviceStatusLog> DeduplicateByLogId(IEnumerable<BizDeviceStatusLog> entries)
{
    var latestById = new Dictionary<int, BizDeviceStatusLog>();
    var noIdEntries = new List<BizDeviceStatusLog>();

    foreach (var entry in entries)
    {
        if (entry.Id <= 0)
        {
            noIdEntries.Add(entry);
            continue;
        }

        latestById[entry.Id] = entry;
    }

    return noIdEntries.Concat(latestById.Values);
}
```

Because records are read in file order, later appended upload results replace earlier pending records for the same id.

- [ ] **Step 2: Append retry results to local JSONL**

Inject `IAppSettingsService` into `UploadTaskService` if it is already present use that existing field. In `UpdateDeviceStatusLog`, after the DB update, append the updated row:

```csharp
_dbContext.Db.Updateable(log)
    .UpdateColumns(it => new { it.ReportStatus, it.ReportTime, it.ReportMessage })
    .Where(it => it.Id == log.Id)
    .ExecuteCommand();

DeviceStatusLocalLogStore.TryAppend(log, _settingsService.Get());
```

Add `using AutoWeldSystem.Services.Log;` only if the namespace is not already available.

- [ ] **Step 3: Run the tests**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: local log de-duplication and retry-state tests pass.

---

### Task 5: Full Verification

**Files:**
- Verify only; no edits expected.

- [ ] **Step 1: Run console regression harness**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: every test prints `PASS`; no unhandled exceptions.

- [ ] **Step 2: Build UI project with alternate output**

Run:

```powershell
dotnet build AutoWeldSystem.UI\AutoWeldSystem.UI.csproj --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

- [ ] **Step 3: Manual verification**

1. Start the WinForms client.
2. Open `日志管理 -> 设备日志`.
3. Locate `SoftwareStarted / 软件开启`.
4. Open `日志管理 -> 设备状态日志`.
5. Confirm `0 / 开机` has the same `HH:mm:ss.fff` as `SoftwareStarted`.
6. Close the client.
7. Reopen the client and check the same date.
8. Confirm `SoftwareStopped / 软件关闭` and `1 / 停机` have matching `HH:mm:ss.fff`.
9. If MES was offline during close, confirm the stop status is present locally and later retry changes upload status after the next startup retry.

- [ ] **Step 4: Review dirty worktree before commit**

Run:

```powershell
git status --short
git diff -- AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs AutoWeldSystem.Services/Production/DeviceStatusService.cs AutoWeldSystem.Services/Log/DeviceLifecycleLogCoordinator.cs AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs AutoWeldSystem.Services/Production/UploadTaskService.cs AutoWeldSystem.Tests/Program.cs
```

Expected: only the device status timestamp/shutdown upload changes appear in these files. Existing unrelated `AutoWeldSystem.UI/Views/MonitorView.Designer.cs` changes should remain unstaged unless the user explicitly asks to include them.

---

## Self-Review

- Spec coverage: The plan covers millisecond precision, lifecycle timestamp synchronization for start and stop, non-blocking shutdown upload, failure retry on next startup, and local log visibility after retry.
- Placeholder scan: No `TBD`, `TODO`, or vague "handle edge cases" steps remain.
- Type consistency: `occurredTime`, `forceWrite`, and `reportInBackground` are added consistently to interface, service, and fake implementations.
