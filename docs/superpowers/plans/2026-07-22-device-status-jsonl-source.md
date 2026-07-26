# 设备状态 JSONL 单一数据源实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `DeviceStatus/*.jsonl` 设为设备状态唯一事实来源，使日志显示、MES 首传、失败补传、删除和中心遥测都服从同一份文件数据。

**Architecture:** 保留 `BizDeviceStatusLog` 类型名以缩小调用方改动，但移除 SqlSugar 映射并为新记录增加 GUID `RecordId`；旧 JSONL 继续通过 `legacy:{Id}` 关联。`Biz_UploadTask` 只保存记录键并作为可重建的派生索引，每次显示或执行前都重新读取 JSONL；物理 `Biz_DeviceStatusLog` 表保留但运行时代码不再访问。

**Tech Stack:** .NET 8、C#、Windows Forms、SqlSugar（仅保留通用上传任务）、System.Text.Json、JSONL、PowerShell 7、现有控制台回归 harness

## Global Constraints

- 设备状态 JSONL 是唯一事实来源；不得再从 `Biz_DeviceStatusLog` 查询、插入、更新或删除数据。
- 状态码固定为 `0=停机`、`1=开机`、`4=异常`、`5=异常恢复`、`6=程序执行开始`、`7=程序执行结束`。
- 只有状态变化或现有调用方显式传入 `forceWrite: true` 时才新增状态事件。
- 新记录必须先成功写入 JSONL，之后才能通知 UI、调用 MES 或创建 `Biz_UploadTask`。
- 首次写入失败只写程序异常日志，不通知设备状态 UI、不调用 MES、不创建补传任务。
- JSONL 最新版本为 `Pending` 或 `Failed` 时才允许出现在待上传设备状态页签并参与上传。
- 同一 `RecordId` 的 `Pending -> Failed -> Uploaded` 采用追加写，读取时最后版本生效。
- 新记录使用 `Guid.NewGuid().ToString("N")`；旧记录使用 `legacy:{Id}`；无有效身份的记录跳过并写程序异常日志。
- 新设备状态任务 payload 只保存 `RecordKey`；旧任务的 `LogId` 和 `device-status:{Id}` 继续兼容。
- 单条和批量执行都必须在调用 MES 前重新校验 JSONL；来源缺失时只软删除未成功任务，已上传任务保持不变。
- 不增加 `FileSystemWatcher`、轮询器、新数据库迁移或第三方依赖；外部删除在刷新、重新进入页面或执行上传时生效。
- 不删除、迁移或 `DROP` 现场已有的 `Biz_DeviceStatusLog` 物理表；只从 CodeFirst 注册和运行时代码中移除。
- 保留工作区中的 `.idea/` 和根目录 `AGENTS.md`，不得暂存、修改或提交。
- 每个任务先运行 RED，再做最小实现、运行 GREEN，并按任务创建 Conventional Commit 原子提交。

## 文件职责与改动范围

| 文件 | 职责 |
| --- | --- |
| `AutoWeldSystem.Core/Entities/BizDeviceStatusLog.cs` | 保留旧字段兼容的普通 JSON 模型，新增 `RecordId`，移除 SqlSugar 特性。 |
| `AutoWeldSystem.Core/Production/DeviceStatusRecordIdentityRules.cs` | 唯一负责 GUID、旧整数 ID、任务 `BusinessId` 和 payload 的记录键解析。 |
| `AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs` | 串行追加、按记录键去重、按键查询、结果版本追加和按键删除。 |
| `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs` | 向消费者提供可空当前状态、工位最新状态、按键查询、待上传来源和按键重试。 |
| `AutoWeldSystem.Services/Production/DeviceStatusService.cs` | 执行 JSONL 写入优先、MES 上报、结果追加和派生任务同步。 |
| `AutoWeldSystem.Services/Production/UploadTaskService.cs` | 从 JSONL 重建设备状态任务、清理失效投影并在每次执行前重新校验来源。 |
| `AutoWeldSystem.Core/DTOs/Upload/UploadTaskSummary.cs` | 用 `DeviceStatusRecordKey` 替代数据库日志整数 ID。 |
| `AutoWeldSystem.UI/Views/LogManageView.cs` | 仅通过 `LogsChanged` 重载 JSONL，移除会重复插入的实时行事件。 |
| `AutoWeldSystem.UI/Views/StateManageView.cs` | 按记录键索引和删除 JSONL 来源。 |
| `AutoWeldSystem.Services/Production/DeviceApiEndpointService.cs` | 无有效 JSONL 时返回“暂无设备状态记录”。 |
| `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs` | PLC 无有效值时通过设备状态服务读取 JSONL，不再查询旧表。 |
| `AutoWeldSystem.Data/SqlSugarDbContext.cs` | 停止为新数据库 CodeFirst 创建设备状态表。 |
| `AutoWeldSystem.Tests/Program.cs` | 增加身份、文件、写入顺序、补传门禁、消费者和 README 回归用例。 |
| `README.md` | 说明唯一来源、删除效果、已上传边界和落盘失败排障。 |
| `docs/QUICK_START.md` | 移除设备状态数据库/JSONL 双来源的过时说明。 |

---

### Task 1: 建立 JSONL 记录身份与按键文件操作

**Files:**
- Create: `AutoWeldSystem.Core/Production/DeviceStatusRecordIdentityRules.cs`
- Modify: `AutoWeldSystem.Core/Entities/BizDeviceStatusLog.cs`
- Modify: `AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs`
- Test: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: 旧 JSONL 的整数 `Id`、现有多行 JSONL 格式和 `LocalJsonLogFormatter`。
- Produces: `DeviceStatusRecordIdentityRules.GetRecordKey(BizDeviceStatusLog?) -> string?`、`ReadTaskRecordKey(string?, string?) -> string?`、`DeviceStatusLocalLogStore.ReadByRecordKey(...) -> BizDeviceStatusLog?`、`ReadPending(...) -> IReadOnlyList<BizDeviceStatusLog>`、`TryAppendVersion(...) -> bool`。

- [ ] **Step 1: 在测试列表加入身份与文件版本回归用例**

在 `AutoWeldSystem.Tests/Program.cs` 现有设备状态用例附近加入：

```csharp
("Device status record identity supports guid and legacy keys", DeviceStatusRecordIdentitySupportsGuidAndLegacyKeys),
("Device status local log store uses record keys", DeviceStatusLocalLogStoreUsesRecordKeys),
("Device status local log store skips invalid identities", DeviceStatusLocalLogStoreSkipsInvalidIdentities),
```

并加入以下完整测试方法：

```csharp
static void DeviceStatusRecordIdentitySupportsGuidAndLegacyKeys()
{
    var guid = Guid.Parse("A7A2A606-7840-4A3D-9CE4-8B8C7BE8357B");
    var current = new BizDeviceStatusLog { RecordId = guid.ToString("D"), Id = 42 };
    var legacy = new BizDeviceStatusLog { Id = 42 };

    AssertEqual(guid.ToString("N"), DeviceStatusRecordIdentityRules.GetRecordKey(current), "新记录必须把 GUID 规范化为 N 格式。");
    AssertEqual("legacy:42", DeviceStatusRecordIdentityRules.GetRecordKey(legacy), "旧记录必须使用 legacy:{Id}。");
    AssertEqual(null, DeviceStatusRecordIdentityRules.GetRecordKey(new BizDeviceStatusLog()), "无 GUID 且无旧 Id 的记录没有可靠身份。");
    AssertEqual(
        "legacy:42",
        DeviceStatusRecordIdentityRules.ReadTaskRecordKey("device-status:42", "{\"LogId\":42}"),
        "旧任务的整数 BusinessId 和 LogId 必须继续可解析。");
    AssertEqual(
        guid.ToString("N"),
        DeviceStatusRecordIdentityRules.ReadTaskRecordKey(
            $"device-status:{guid:N}",
            $"{{\"RecordKey\":\"{guid:D}\"}}"),
        "新任务必须从只含 RecordKey 的 payload 定位 JSONL。");
    AssertSequenceEqual(
        new[] { "device-status:legacy:42", "device-status:42" },
        DeviceStatusRecordIdentityRules.GetCompatibleBusinessIds("legacy:42").ToArray(),
        "旧记录查重时必须同时识别规范和历史 BusinessId。");
}

static void DeviceStatusLocalLogStoreUsesRecordKeys()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusRecordKeyTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 7, 22, 8, 30, 0, 123);
    var recordId = Guid.NewGuid().ToString("N");

    try
    {
        var pending = new BizDeviceStatusLog
        {
            RecordId = recordId,
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
        var failed = new BizDeviceStatusLog
        {
            RecordId = recordId,
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Failed,
            ReportTime = occurredTime.AddSeconds(1),
            ReportMessage = "MES offline"
        };
        var retained = new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 2,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Recovered),
            OccurredTime = occurredTime.AddMinutes(1),
            ReportStatus = ProductionConstants.UploadStatuses.Uploaded
        };

        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings), "Pending 首版本必须成功落盘。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppendVersion(failed, settings), "同一记录键的 Failed 版本必须追加到既有来源。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(retained, settings), "另一条记录必须成功落盘。");

        var latest = DeviceStatusLocalLogStore.ReadByRecordKey(settings, recordId);
        AssertTrue(latest is not null, "必须能按 GUID 记录键读取来源。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, latest!.ReportStatus, "同一键最后追加的版本必须生效。");
        AssertEqual(1, DeviceStatusLocalLogStore.ReadPending(settings).Count, "只有 Pending/Failed 最新版本进入待上传来源。");
        AssertEqual(recordId, DeviceStatusRecordIdentityRules.GetRecordKey(DeviceStatusLocalLogStore.ReadLatestForStation(settings, 1)), "工位最新状态必须来自 JSONL。");

        AssertTrue(DeviceStatusLocalLogStore.TryRemove(new[] { failed }, settings), "按记录键删除必须成功。");
        AssertEqual(null, DeviceStatusLocalLogStore.ReadByRecordKey(settings, recordId), "删除后同一键的全部追加版本都必须消失。");
        AssertEqual(1, DeviceStatusLocalLogStore.Read(settings, maxCount: 10).Count, "删除不能影响其他记录键。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusLocalLogStoreSkipsInvalidIdentities()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusInvalidIdentityTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var directory = DeviceStatusLocalLogStore.GetLogDirectory(settings);
    var filePath = Path.Combine(directory, "2026-07-22.jsonl");
    var errors = new List<string>();

    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            filePath,
            "{\"DeviceId\":\"D-001\",\"DeviceStatus\":\"1\",\"OccurredTime\":\"2026-07-22T09:00:00\"}" + Environment.NewLine,
            Encoding.UTF8);

        var logs = DeviceStatusLocalLogStore.Read(
            settings,
            maxCount: 10,
            onError: (_, context) => errors.Add(context));

        AssertEqual(0, logs.Count, "无 RecordId 和旧 Id 的记录必须跳过。");
        AssertEqual(1, errors.Count, "跳过无效身份时必须向业务服务暴露一次诊断。");
        AssertTrue(errors[0].Contains("2026-07-22.jsonl", StringComparison.OrdinalIgnoreCase), "诊断必须包含损坏来源文件。");
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

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 编译失败，至少报告 `BizDeviceStatusLog.RecordId`、`DeviceStatusRecordIdentityRules`、`TryAppendVersion`、`ReadPending`、`ReadByRecordKey` 或 `ReadLatestForStation` 尚不存在。

- [ ] **Step 3: 将设备状态模型改为普通 JSON 模型**

用以下完整内容替换 `AutoWeldSystem.Core/Entities/BizDeviceStatusLog.cs`：

```csharp
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 设备状态 JSONL 记录；Id 只用于兼容旧文件，新记录使用 RecordId。
/// </summary>
public class BizDeviceStatusLog
{
    public string? RecordId { get; set; }

    public int Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    public int? WeldTaskId { get; set; }

    public string? WorkOrderId { get; set; }

    public string DeviceStatus { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public string Source { get; set; } = "Software";

    public string? Remark { get; set; }

    public DateTime OccurredTime { get; set; } = DateTime.Now;

    public string ReportStatus { get; set; } = ProductionConstants.UploadStatuses.Pending;

    public DateTime? ReportTime { get; set; }

    public string? ReportMessage { get; set; }
}
```

- [ ] **Step 4: 新增唯一的记录键兼容规则**

创建 `AutoWeldSystem.Core/Production/DeviceStatusRecordIdentityRules.cs`：

```csharp
using System.Text.Json;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 统一解析新 GUID、旧日志整数 Id 和上传任务中的设备状态记录键。
/// </summary>
public static class DeviceStatusRecordIdentityRules
{
    private const string LegacyPrefix = "legacy:";
    private const string BusinessPrefix = "device-status:";

    public static string? GetRecordKey(BizDeviceStatusLog? log)
    {
        if (log is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(log.RecordId))
        {
            return NormalizeRecordKey(log.RecordId);
        }

        return log.Id > 0 ? $"{LegacyPrefix}{log.Id}" : null;
    }

    public static string? NormalizeRecordKey(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(normalized[LegacyPrefix.Length..], out var legacyId) && legacyId > 0
                ? $"{LegacyPrefix}{legacyId}"
                : null;
        }

        return Guid.TryParse(normalized, out var recordId)
            ? recordId.ToString("N")
            : null;
    }

    public static string BuildBusinessId(string recordKey)
    {
        var normalized = NormalizeRecordKey(recordKey)
            ?? throw new ArgumentException("设备状态记录键无效。", nameof(recordKey));
        return $"{BusinessPrefix}{normalized}";
    }

    public static IReadOnlyList<string> GetCompatibleBusinessIds(string recordKey)
    {
        var normalized = NormalizeRecordKey(recordKey)
            ?? throw new ArgumentException("设备状态记录键无效。", nameof(recordKey));
        var values = new List<string> { $"{BusinessPrefix}{normalized}" };
        if (TryGetLegacyId(normalized, out var legacyId))
        {
            values.Add($"{BusinessPrefix}{legacyId}");
        }

        return values;
    }

    public static string? ReadTaskRecordKey(string? businessId, string? payloadJson)
    {
        var payloadKey = ReadPayloadRecordKey(payloadJson);
        if (payloadKey is not null)
        {
            return payloadKey;
        }

        var normalizedBusinessId = businessId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBusinessId)
            || !normalizedBusinessId.StartsWith(BusinessPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = normalizedBusinessId[BusinessPrefix.Length..];
        if (int.TryParse(suffix, out var legacyId) && legacyId > 0)
        {
            return $"{LegacyPrefix}{legacyId}";
        }

        return NormalizeRecordKey(suffix);
    }

    private static string? ReadPayloadRecordKey(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("RecordKey", out var recordKeyElement)
                && recordKeyElement.ValueKind == JsonValueKind.String)
            {
                var recordKey = NormalizeRecordKey(recordKeyElement.GetString());
                if (recordKey is not null)
                {
                    return recordKey;
                }
            }

            return root.TryGetProperty("LogId", out var logIdElement)
                && logIdElement.TryGetInt32(out var logId)
                && logId > 0
                    ? $"{LegacyPrefix}{logId}"
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetLegacyId(string recordKey, out int legacyId)
    {
        legacyId = 0;
        return recordKey.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(recordKey[LegacyPrefix.Length..], out legacyId)
            && legacyId > 0;
    }
}
```

- [ ] **Step 5: 将本地存储改为按记录键串行读写**

在 `DeviceStatusLocalLogStore` 中增加 `using AutoWeldSystem.Core.Production;`，保留现有目录、临时文件和备份策略，并完成以下精确改动：

```csharp
private static readonly object SyncRoot = new();

public static bool TryAppend(BizDeviceStatusLog entry, AppSettings settings)
{
    if (DeviceStatusRecordIdentityRules.GetRecordKey(entry) is null)
    {
        return false;
    }

    lock (SyncRoot)
    {
        return TryAppendCore(entry, settings);
    }
}

public static bool TryAppendVersion(BizDeviceStatusLog entry, AppSettings settings)
{
    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
    if (recordKey is null)
    {
        return false;
    }

    lock (SyncRoot)
    {
        var filePath = GetLogFilePath(settings, entry.OccurredTime);
        if (!File.Exists(filePath))
        {
            return false;
        }

        var sourceExists = ReadFile(filePath, onError: null)
            .Any(log => string.Equals(
                DeviceStatusRecordIdentityRules.GetRecordKey(log),
                recordKey,
                StringComparison.OrdinalIgnoreCase));
        return sourceExists && TryAppendCore(entry, settings);
    }
}

public static IReadOnlyList<BizDeviceStatusLog> Read(
    AppSettings settings,
    DateTime? from = null,
    DateTime? to = null,
    int maxCount = 200,
    Action<Exception, string>? onError = null)
{
    var take = Math.Clamp(maxCount, 1, 5000);
    lock (SyncRoot)
    {
        return ReadLatestCore(settings, from, to, onError)
            .Where(entry => IsInRange(entry, from, to))
            .OrderByDescending(entry => entry.OccurredTime)
            .Take(take)
            .ToList();
    }
}

public static IReadOnlyList<BizDeviceStatusLog> ReadPending(
    AppSettings settings,
    Action<Exception, string>? onError = null)
{
    lock (SyncRoot)
    {
        return ReadLatestCore(settings, from: null, to: null, onError: onError)
            .Where(entry => DeviceStatusUploadVisibilityRules.ShouldInclude(entry.ReportStatus))
            .OrderByDescending(entry => entry.OccurredTime)
            .ToList();
    }
}

public static BizDeviceStatusLog? ReadByRecordKey(
    AppSettings settings,
    string recordKey,
    Action<Exception, string>? onError = null)
{
    var normalized = DeviceStatusRecordIdentityRules.NormalizeRecordKey(recordKey);
    if (normalized is null)
    {
        return null;
    }

    lock (SyncRoot)
    {
        return ReadLatestCore(settings, from: null, to: null, onError: onError)
            .FirstOrDefault(entry => string.Equals(
                DeviceStatusRecordIdentityRules.GetRecordKey(entry),
                normalized,
                StringComparison.OrdinalIgnoreCase));
    }
}

public static BizDeviceStatusLog? ReadLatestForStation(
    AppSettings settings,
    int stationNo,
    Action<Exception, string>? onError = null)
{
    lock (SyncRoot)
    {
        return ReadLatestCore(settings, from: null, to: null, onError: onError)
            .Where(entry => entry.StationNo == stationNo)
            .OrderByDescending(entry => entry.OccurredTime)
            .FirstOrDefault();
    }
}

private static bool TryAppendCore(BizDeviceStatusLog entry, AppSettings settings)
{
    try
    {
        entry.OccurredTime = entry.OccurredTime == default ? DateTime.Now : entry.OccurredTime;
        var filePath = GetLogFilePath(settings, entry.OccurredTime);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var json = LocalJsonLogFormatter.Serialize(entry);
        File.AppendAllText(filePath, json + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
        return true;
    }
    catch
    {
        return false;
    }
}

private static IReadOnlyList<BizDeviceStatusLog> ReadLatestCore(
    AppSettings settings,
    DateTime? from,
    DateTime? to,
    Action<Exception, string>? onError)
{
    var latestByKey = new Dictionary<string, BizDeviceStatusLog>(StringComparer.OrdinalIgnoreCase);
    IEnumerable<string> filePaths;
    try
    {
        filePaths = EnumerateCandidateFiles(settings, from, to).ToList();
    }
    catch (Exception ex)
    {
        onError?.Invoke(ex, $"Directory={GetLogDirectory(settings)}");
        return Array.Empty<BizDeviceStatusLog>();
    }

    foreach (var filePath in filePaths)
    {
        foreach (var entry in ReadFile(filePath, onError))
        {
            var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
            if (recordKey is null)
            {
                onError?.Invoke(
                    new InvalidDataException("设备状态记录缺少有效 RecordId 或旧 Id。"),
                    $"File={filePath}");
                continue;
            }

            latestByKey[recordKey] = entry;
        }
    }

    return latestByKey.Values.ToList();
}

private static IReadOnlyList<BizDeviceStatusLog> ReadFile(
    string filePath,
    Action<Exception, string>? onError)
{
    if (!File.Exists(filePath))
    {
        return Array.Empty<BizDeviceStatusLog>();
    }

    var entries = new List<BizDeviceStatusLog>();
    try
    {
        foreach (var record in LocalJsonLogFormatter.ReadAllRecords(filePath))
        {
            try
            {
                var entry = LocalJsonLogFormatter.Deserialize<BizDeviceStatusLog>(record);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex, $"File={filePath}");
            }
        }
    }
    catch (Exception ex)
    {
        onError?.Invoke(ex, $"File={filePath}");
    }

    return entries;
}

private static IEnumerable<string> EnumerateCandidateFiles(
    AppSettings settings,
    DateTime? from,
    DateTime? to)
{
    if (from is not null || to is not null)
    {
        foreach (var date in EnumerateCandidateDates(from, to))
        {
            var filePath = GetLogFilePath(settings, date);
            if (File.Exists(filePath))
            {
                yield return filePath;
            }
        }

        yield break;
    }

    var directory = GetLogDirectory(settings);
    if (!Directory.Exists(directory))
    {
        yield break;
    }

    foreach (var filePath in Directory
        .EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly)
        .Where(filePath => DateTime.TryParseExact(
            Path.GetFileNameWithoutExtension(filePath),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _))
        .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase))
    {
        yield return filePath;
    }
}
```

同时把现有 `TryRemove` 的整数 ID 分组替换为记录键分组，并让整个重写过程位于同一个 `lock (SyncRoot)` 中：

```csharp
public static bool TryRemove(IReadOnlyCollection<BizDeviceStatusLog> entries, AppSettings settings)
{
    var recordKeysByDate = entries
        .Select(entry => new
        {
            Entry = entry,
            RecordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry)
        })
        .Where(item => item.RecordKey is not null && item.Entry.OccurredTime != default)
        .GroupBy(item => item.Entry.OccurredTime.Date)
        .ToDictionary(
            group => group.Key,
            group => group.Select(item => item.RecordKey!).ToHashSet(StringComparer.OrdinalIgnoreCase));
    if (recordKeysByDate.Count == 0)
    {
        return true;
    }

    lock (SyncRoot)
    {
        return TryRewriteWithoutKeys(recordKeysByDate, settings);
    }
}

private static bool TryRewriteWithoutKeys(
    IReadOnlyDictionary<DateTime, HashSet<string>> recordKeysByDate,
    AppSettings settings)
{
    var rewrites = new List<LocalFileRewrite>();
    try
    {
        foreach (var (date, recordKeys) in recordKeysByDate)
        {
            var filePath = GetLogFilePath(settings, date);
            if (!File.Exists(filePath))
            {
                continue;
            }

            var retainedRecords = LocalJsonLogFormatter.ReadAllRecords(filePath)
                .Where(record => !ShouldRemove(record, recordKeys))
                .ToList();
            rewrites.Add(new LocalFileRewrite(filePath, FormatRecords(retainedRecords)));
        }

        foreach (var rewrite in rewrites)
        {
            rewrite.TempPath = $"{rewrite.FilePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(rewrite.TempPath, rewrite.Content, Encoding.UTF8);
        }

        foreach (var rewrite in rewrites)
        {
            rewrite.BackupPath = $"{rewrite.FilePath}.{Guid.NewGuid():N}.bak";
            File.Copy(rewrite.FilePath, rewrite.BackupPath, overwrite: true);
            if (string.IsNullOrEmpty(rewrite.Content))
            {
                File.Delete(rewrite.FilePath);
            }
            else
            {
                File.Move(rewrite.TempPath!, rewrite.FilePath, overwrite: true);
            }

            rewrite.Applied = true;
        }

        return true;
    }
    catch
    {
        foreach (var rewrite in rewrites.Where(rewrite => rewrite.Applied).Reverse())
        {
            if (!string.IsNullOrWhiteSpace(rewrite.BackupPath) && File.Exists(rewrite.BackupPath))
            {
                File.Copy(rewrite.BackupPath, rewrite.FilePath, overwrite: true);
            }
        }

        return false;
    }
    finally
    {
        foreach (var rewrite in rewrites)
        {
            TryDeleteFile(rewrite.TempPath);
            TryDeleteFile(rewrite.BackupPath);
        }
    }
}

private static bool ShouldRemove(string record, ISet<string> recordKeys)
{
    try
    {
        var entry = LocalJsonLogFormatter.Deserialize<BizDeviceStatusLog>(record);
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
        return recordKey is not null && recordKeys.Contains(recordKey);
    }
    catch
    {
        return false;
    }
}
```

删除旧的 `DeduplicateByLogId`、`ReadDate`、`ReadAllDateRecords` 和整数版 `ShouldRemove`。保留现有 `GetLogDirectory`、`GetLogFilePath`、`EnumerateCandidateDates`、`IsInRange`、`FormatRecords`、`TryDeleteFile` 与 `LocalFileRewrite`；在 `SyncRoot` 上方添加注释：

```csharp
// ponytail: 设备状态写入量很低，先使用进程内全局锁；出现实测争用后再按日期文件拆锁。
```

- [ ] **Step 6: 运行设备状态回归并确认 GREEN**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 新增的三个身份/文件用例通过；旧整数 ID 的写入、去重和删除用例仍通过。若 harness 先停在仓库既有无关失败，记录首个失败并至少使用测试名称过滤后的临时本地入口确认这三个新用例通过，不得把部分结果描述为全量通过。

- [ ] **Step 7: 提交记录身份与本地存储**

```powershell
git add AutoWeldSystem.Core/Entities/BizDeviceStatusLog.cs AutoWeldSystem.Core/Production/DeviceStatusRecordIdentityRules.cs AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "feat(logs): 建立设备状态 JSONL 记录标识"
```

---

### Task 2: 将设备状态服务改为 JSONL 写入优先

**Files:**
- Modify: `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs`
- Modify: `AutoWeldSystem.Services/Production/DeviceStatusService.cs`
- Modify: `AutoWeldSystem.Data/SqlSugarDbContext.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: Task 1 的记录键规则和按键 JSONL 操作、现有 `IMesProvider`、`IProgramExceptionLogService`、`Biz_UploadTask`。
- Produces: `GetCurrentStatus() -> BizDeviceStatusLog?`、`GetLatestStatus(int) -> BizDeviceStatusLog?`、`GetPendingLogs() -> IReadOnlyList<BizDeviceStatusLog>`、`GetLog(string) -> BizDeviceStatusLog?`、`RetryUploadAsync(string, CancellationToken) -> Task<BasicRes<object>?>`。

- [ ] **Step 1: 加入写入顺序、失败短路和数据库解耦测试**

在测试列表加入：

```csharp
("Device status service writes jsonl before MES", DeviceStatusServiceWritesJsonlBeforeMes),
("Device status service stops when first jsonl write fails", DeviceStatusServiceStopsWhenFirstJsonlWriteFails),
("Device status runtime no longer persists database log rows", DeviceStatusRuntimeNoLongerPersistsDatabaseLogRows),
```

加入以下测试方法：

```csharp
static void DeviceStatusServiceWritesJsonlBeforeMes()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusWriteFirstTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);
    var pendingSeenBeforeMes = false;
    var notifications = 0;
    service.LogsChanged += (_, _) => notifications++;
    mes.DeviceStatusRequestObserved = _ =>
    {
        var persisted = service.GetLogs(maxCount: 10);
        pendingSeenBeforeMes = persisted.Count == 1
            && persisted[0].ReportStatus == ProductionConstants.UploadStatuses.Pending;
    };

    try
    {
        var result = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,
                "PLC alarm",
                "PLC-S1",
                stationNo: 1)
            .GetAwaiter()
            .GetResult();
        var persistedResult = service.GetLog(result.RecordId!);

        AssertTrue(pendingSeenBeforeMes, "调用 MES 时 JSONL 中必须已经存在 Pending 首版本。");
        AssertEqual(1, mes.DeviceStatusRequests.Count, "首版本落盘成功后才允许调用一次 MES。");
        AssertTrue(Guid.TryParseExact(result.RecordId, "N", out _), "新记录必须使用 N 格式 GUID RecordId。");
        AssertTrue(persistedResult is not null, "MES 结果必须继续保存在同一个 JSONL 记录键下。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, persistedResult!.ReportStatus, "成功响应必须追加 Uploaded 版本。");
        AssertEqual(result.OccurredTime, persistedResult.OccurredTime, "追加结果不能丢失原始毫秒时间。");
        AssertTrue(notifications >= 2, "Pending 首版本和 Uploaded 结果版本都必须通知 UI 重载。");
        AssertEqual(0, exceptionLogs.Entries.Count, "正常落盘和上报不应写程序异常日志。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceStopsWhenFirstJsonlWriteFails()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusWriteFailureTests", Guid.NewGuid().ToString("N"));
    var blockedLogRoot = Path.Combine(root, "blocked-root");
    Directory.CreateDirectory(root);
    File.WriteAllText(blockedLogRoot, "this path is a file", Encoding.UTF8);
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = blockedLogRoot,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);
    var notifications = 0;
    service.LogsChanged += (_, _) => notifications++;

    try
    {
        var result = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.PoweredOn,
                "开机",
                "Application")
            .GetAwaiter()
            .GetResult();

        AssertEqual(0, mes.DeviceStatusRequests.Count, "首版本落盘失败时禁止调用 MES。");
        AssertEqual(0, notifications, "首版本落盘失败时禁止通知设备状态 UI。");
        AssertEqual(1, exceptionLogs.Entries.Count, "首版本落盘失败必须写程序异常日志。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, result.ReportStatus, "返回对象必须明确标记本地落盘失败。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusRuntimeNoLongerPersistsDatabaseLogRows()
{
    var entityCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Entities", "BizDeviceStatusLog.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"), Encoding.UTF8);
    var dbCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Data", "SqlSugarDbContext.cs"), Encoding.UTF8);

    AssertFalse(entityCode.Contains("SugarTable", StringComparison.Ordinal), "设备状态 JSON 模型不能继续映射 SqlSugar 表。");
    AssertFalse(entityCode.Contains("SugarColumn", StringComparison.Ordinal), "设备状态 JSON 模型不能保留数据库列特性。");
    AssertFalse(serviceCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态服务不能再查询旧表。");
    AssertFalse(serviceCode.Contains("Insertable(log)", StringComparison.Ordinal), "设备状态服务不能再插入旧表。");
    AssertFalse(serviceCode.Contains("Updateable(log)", StringComparison.Ordinal), "设备状态服务不能再更新旧表。");
    AssertFalse(serviceCode.Contains("Deleteable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态服务不能再删除旧表行。");
    AssertFalse(dbCode.Contains("typeof(BizDeviceStatusLog)", StringComparison.Ordinal), "CodeFirst 不能再为新数据库创建设备状态表。");
    AssertTrue(serviceCode.Contains("IProgramExceptionLogService", StringComparison.Ordinal), "JSONL 写入失败必须接入程序异常日志。");
}
```

在 `FakeMesProvider` 增加观察回调，并在 `ReportDeviceStatusAsync` 中先触发回调再记录请求：

```csharp
public Action<ReportDeviceStatusReq>? DeviceStatusRequestObserved { get; set; }

public Task<BasicRes<object>> ReportDeviceStatusAsync(
    ReportDeviceStatusReq requestData,
    CancellationToken cancellationToken = default)
{
    DeviceStatusRequestObserved?.Invoke(requestData);
    DeviceStatusRequests.Add(requestData);
    return Task.FromResult(DeviceStatusResponse);
}
```

在测试替身区域加入：

```csharp
sealed class FakeProgramExceptionLogService : IProgramExceptionLogService
{
    public event EventHandler<ProgramExceptionLogEntry>? LogWritten;

    public List<ProgramExceptionLogEntry> Entries { get; } = new();

    public ProgramExceptionLogEntry Write(Exception exception, string source, string? context = null)
    {
        var entry = new ProgramExceptionLogEntry
        {
            Source = source,
            Message = exception.Message,
            Context = context ?? string.Empty,
            StackTrace = exception.ToString(),
            OccurredTime = DateTime.Now
        };
        Write(entry);
        return entry;
    }

    public ProgramExceptionLogEntry WriteBusiness(
        string source,
        string message,
        string detail,
        string? context = null,
        string sourceFilePath = "",
        int sourceLineNumber = 0,
        string sourceMemberName = "")
    {
        var entry = new ProgramExceptionLogEntry
        {
            Source = source,
            Message = message,
            StackTrace = detail,
            Context = context ?? string.Empty,
            OccurredTime = DateTime.Now
        };
        Write(entry);
        return entry;
    }

    public void Write(ProgramExceptionLogEntry entry)
    {
        Entries.Add(entry);
        LogWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<ProgramExceptionLogEntry> GetByDate(DateTime date, int take = 500)
        => Entries.Where(entry => entry.OccurredTime.Date == date.Date).Take(take).ToList();

    public string GetLogDirectory() => string.Empty;
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 编译失败，因为 `DeviceStatusService` 尚未接受 `IProgramExceptionLogService`，且尚未公开 `GetLog`；数据库解耦断言在实现前也应失败。

- [ ] **Step 3: 扩展设备状态服务契约，暂留旧事件以保持中间提交可构建**

用以下内容替换 `IDeviceStatusService`。`StatusChanged` 和 `NotifyLogsChanged` 只为 Task 3/4 之前的调用方保持编译，在 Task 4 删除；新业务代码不得再调用或发布 `StatusChanged`。

```csharp
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

public interface IDeviceStatusService
{
    event EventHandler<BizDeviceStatusLog>? StatusChanged;

    event EventHandler? LogsChanged;

    void NotifyLogsChanged();

    BizDeviceStatusLog? GetCurrentStatus();

    BizDeviceStatusLog? GetLatestStatus(int stationNo);

    IReadOnlyList<BizDeviceStatusLog> GetLogs(
        DateTime? from = null,
        DateTime? to = null,
        int maxCount = 200);

    IReadOnlyList<BizDeviceStatusLog> GetPendingLogs();

    BizDeviceStatusLog? GetLog(string recordKey);

    BizUploadTask? EnsurePendingUploadTask(BizDeviceStatusLog log);

    string GetLogDirectory();

    int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs);

    Task<BasicRes<object>?> RetryUploadAsync(
        string recordKey,
        CancellationToken cancellationToken = default);

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
}
```

- [ ] **Step 4: 删除 CodeFirst 注册但保留物理表**

从 `SqlSugarDbContext.InitDatabase()` 的 `Db.CodeFirst.InitTables(...)` 参数中只删除这一行：

```csharp
typeof(BizDeviceStatusLog),
```

不要增加 `DropTable`、迁移 SQL 或旧数据复制代码。

- [ ] **Step 5: 将 DeviceStatusService 改成文件先行工作流**

保留类名和现有构造位置，用以下字段、构造函数及兼容事件替换对应成员：

```csharp
private readonly SqlSugarDbContext _dbContext;
private readonly IAppSettingsService _settingsService;
private readonly IMesProvider _mesProvider;
private readonly IProgramExceptionLogService _exceptionLogService;
private readonly object _dbLock = new();
private AppSettings _currentSettings;

public DeviceStatusService(
    SqlSugarDbContext dbContext,
    IAppSettingsService settingsService,
    IMesProvider mesProvider,
    IProgramExceptionLogService exceptionLogService)
{
    _dbContext = dbContext;
    _settingsService = settingsService;
    _mesProvider = mesProvider;
    _exceptionLogService = exceptionLogService;
    _currentSettings = settingsService.Get();
    _settingsService.SettingsChanged += SettingsService_SettingsChanged;
}

public event EventHandler<BizDeviceStatusLog>? StatusChanged;

public event EventHandler? LogsChanged;

public void NotifyLogsChanged() => RaiseLogsChanged();
```

添加 `using AutoWeldSystem.Core.Interfaces.Log;`，并用以下查询成员完全替换数据库查询与默认开机对象：

```csharp
public BizDeviceStatusLog? GetCurrentStatus()
    => GetLogs(from: null, to: null, maxCount: 1).FirstOrDefault();

public BizDeviceStatusLog? GetLatestStatus(int stationNo)
    => DeviceStatusLocalLogStore.ReadLatestForStation(
        CurrentSettings,
        NormalizeStationNo(stationNo),
        WriteLocalReadError);

public IReadOnlyList<BizDeviceStatusLog> GetLogs(
    DateTime? from = null,
    DateTime? to = null,
    int maxCount = 200)
    => DeviceStatusLocalLogStore.Read(CurrentSettings, from, to, maxCount, WriteLocalReadError);

public IReadOnlyList<BizDeviceStatusLog> GetPendingLogs()
    => DeviceStatusLocalLogStore.ReadPending(CurrentSettings, WriteLocalReadError);

public BizDeviceStatusLog? GetLog(string recordKey)
    => DeviceStatusLocalLogStore.ReadByRecordKey(CurrentSettings, recordKey, WriteLocalReadError);

public string GetLogDirectory()
    => DeviceStatusLocalLogStore.GetLogDirectory(CurrentSettings);
```

用以下实现替换 `EnsurePendingUploadTask`、任务构造和查重方法；payload 只能包含 `RecordKey`：

```csharp
public BizUploadTask? EnsurePendingUploadTask(BizDeviceStatusLog log)
{
    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log)
        ?? throw new ArgumentException("设备状态日志缺少有效记录键。", nameof(log));
    var source = GetLog(recordKey);
    if (source is null || !DeviceStatusUploadVisibilityRules.ShouldInclude(source.ReportStatus))
    {
        return null;
    }

    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var task = BuildDeviceStatusUploadTask(source, recordKey);
        var existing = FindExistingUploadTask(recordKey);
        if (existing is null)
        {
            task.CreatedTime = DateTime.Now;
            task.UpdatedTime = DateTime.Now;
            return _dbContext.Db.Insertable(task).ExecuteReturnEntity();
        }

        existing.IsDeleted = false;
        existing.DeletedTime = null;
        existing.BusinessId = task.BusinessId;
        existing.PayloadJson = task.PayloadJson;
        existing.Status = task.Status;
        existing.NextRetryTime = task.NextRetryTime;
        existing.Message = task.Message;
        existing.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(existing).ExecuteCommand();
        return _dbContext.Db.Queryable<BizUploadTask>().InSingle(existing.Id) ?? existing;
    }
}

private static BizUploadTask BuildDeviceStatusUploadTask(BizDeviceStatusLog log, string recordKey)
{
    var status = string.Equals(
        log.ReportStatus,
        ProductionConstants.UploadStatuses.Failed,
        StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.UploadStatuses.Failed
            : ProductionConstants.UploadStatuses.Pending;
    return new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
        Target = ProductionConstants.UploadTargets.Mes,
        BusinessId = DeviceStatusRecordIdentityRules.BuildBusinessId(recordKey),
        PayloadJson = JsonSerializer.Serialize(new { RecordKey = recordKey }),
        Status = status,
        NextRetryTime = DateTime.Now,
        Message = string.IsNullOrWhiteSpace(log.ReportMessage)
            ? "Device status is queued for MES retry."
            : log.ReportMessage
    };
}

private BizUploadTask? FindExistingUploadTask(string recordKey)
{
    var businessIds = DeviceStatusRecordIdentityRules.GetCompatibleBusinessIds(recordKey).ToArray();
    return _dbContext.Db.Queryable<BizUploadTask>()
        .First(task => task.TaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
            && task.Target == ProductionConstants.UploadTargets.Mes
            && businessIds.Contains(task.BusinessId!));
}
```

用以下按键删除实现替换事务中删除数据库日志行的旧实现：

```csharp
public int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs)
{
    var selectedLogs = logs
        .Select(log => new
        {
            Log = log,
            RecordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log)
        })
        .Where(item => item.RecordKey is not null)
        .GroupBy(item => item.RecordKey!, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .ToList();
    if (selectedLogs.Count == 0)
    {
        return 0;
    }

    if (!DeviceStatusLocalLogStore.TryRemove(selectedLogs.Select(item => item.Log).ToList(), CurrentSettings))
    {
        throw new InvalidOperationException("无法删除设备状态 JSONL 日志。");
    }

    SoftDeleteUnfinishedUploadTasks(selectedLogs.Select(item => item.RecordKey!).ToHashSet(StringComparer.OrdinalIgnoreCase));
    RaiseLogsChanged();
    return selectedLogs.Count;
}

private void SoftDeleteUnfinishedUploadTasks(ISet<string> recordKeys)
{
    try
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var now = DateTime.Now;
            var tasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
                    && !task.IsDeleted
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Where(task =>
                {
                    var recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson);
                    return recordKey is not null && recordKeys.Contains(recordKey);
                })
                .ToList();
            foreach (var task in tasks)
            {
                task.IsDeleted = true;
                task.DeletedTime = now;
                task.UpdatedTime = now;
                task.Message = "Device status JSONL source was deleted.";
            }

            if (tasks.Count > 0)
            {
                _dbContext.Db.Updateable(tasks).ExecuteCommand();
            }
        }
    }
    catch (Exception ex)
    {
        _exceptionLogService.Write(ex, "DeviceStatusService.DeleteUploadProjection");
    }
}
```

用以下完整方法替换 `ChangeStatusAsync` 和旧 `ReportStatusAsync`/后台上报/数据库状态更新方法：

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
{
    var normalizedStatus = DeviceStatusReportRules.NormalizeMesDeviceStatusCode(deviceStatus);
    var normalizedStationNo = NormalizeStationNo(stationNo);
    var latest = GetLatestStatus(normalizedStationNo);
    if (!forceWrite)
    {
        var existingBoundary = FindExistingProgramBoundaryLog(normalizedStationNo, normalizedStatus, weldTaskId);
        if (existingBoundary is not null)
        {
            return existingBoundary;
        }

        if (ShouldReuseLatestProgramBoundaryStatus(latest, normalizedStatus, weldTaskId)
            || DeviceStatusReportRules.ShouldSuppressDuplicateStatus(
                latest,
                normalizedStatus,
                weldTaskId,
                forceWrite))
        {
            return latest!;
        }
    }

    var log = CreateLog(
        normalizedStatus,
        remark,
        source,
        normalizedStationNo,
        weldTaskId,
        workOrderId,
        occurredTime);
    if (CurrentSettings.EnableDeviceStatusReport == false)
    {
        log.ReportStatus = ProductionConstants.UploadStatuses.Skipped;
        log.ReportTime = DateTime.Now;
        log.ReportMessage = "Device status report is disabled in system settings.";
    }

    if (!DeviceStatusLocalLogStore.TryAppend(log, CurrentSettings))
    {
        log.ReportStatus = ProductionConstants.UploadStatuses.Failed;
        log.ReportMessage = "Device status JSONL initial write failed.";
        WriteAppendFailure(log, log.ReportMessage);
        return log;
    }

    if (log.ReportStatus == ProductionConstants.UploadStatuses.Skipped)
    {
        RaiseLogsChanged();
        return log;
    }

    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log)!;
    if (!reportToMes)
    {
        TryEnsurePendingUploadTask(log);
        RaiseLogsChanged();
        return log;
    }

    RaiseLogsChanged();
    if (reportInBackground)
    {
        _ = Task.Run(() => RetryInBackgroundAsync(recordKey));
        return log;
    }

    await RetryUploadAsync(recordKey, cancellationToken);
    return GetLog(recordKey) ?? log;
}

public async Task<BasicRes<object>?> RetryUploadAsync(
    string recordKey,
    CancellationToken cancellationToken = default)
{
    var normalizedRecordKey = DeviceStatusRecordIdentityRules.NormalizeRecordKey(recordKey);
    if (normalizedRecordKey is null)
    {
        return null;
    }

    var log = GetLog(normalizedRecordKey);
    if (log is null || !DeviceStatusUploadVisibilityRules.ShouldInclude(log.ReportStatus))
    {
        return null;
    }

    BasicRes<object> response;
    if (CurrentSettings.EnableDeviceStatusReport == false)
    {
        response = new BasicRes<object>
        {
            Status = ProductionConstants.UploadStatuses.Skipped,
            Msg = "Device status report is disabled in system settings.",
            Data = new object()
        };
    }
    else
    {
        response = await SendToMesAsync(log, cancellationToken);
    }

    return PersistReportResult(log, normalizedRecordKey, response);
}

private async Task<BasicRes<object>> SendToMesAsync(
    BizDeviceStatusLog log,
    CancellationToken cancellationToken)
{
    try
    {
        return await _mesProvider.ReportDeviceStatusAsync(new ReportDeviceStatusReq
        {
            DeviceId = DeviceStatusReportRules.ResolveReportDeviceId(CurrentSettings.DeviceId, log.DeviceId),
            DevStatus = log.DeviceStatus,
            Ts = log.OccurredTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Remark = log.Remark ?? string.Empty
        }, cancellationToken);
    }
    catch (Exception ex)
    {
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = ex.Message
        };
    }
}

private BasicRes<object>? PersistReportResult(
    BizDeviceStatusLog log,
    string recordKey,
    BasicRes<object> response)
{
    var previousStatus = log.ReportStatus;
    var previousTime = log.ReportTime;
    var previousMessage = log.ReportMessage;
    log.ReportStatus = IsSkippedResponse(response)
        ? ProductionConstants.UploadStatuses.Skipped
        : response.IsSuccess
            ? ProductionConstants.UploadStatuses.Uploaded
            : ProductionConstants.UploadStatuses.Failed;
    log.ReportTime = DateTime.Now;
    log.ReportMessage = response.Msg;

    if (!DeviceStatusLocalLogStore.TryAppendVersion(log, CurrentSettings))
    {
        log.ReportStatus = previousStatus;
        log.ReportTime = previousTime;
        log.ReportMessage = previousMessage;
        if (GetLog(recordKey) is null)
        {
            return null;
        }

        WriteAppendFailure(log, "MES result could not be appended to device status JSONL.");
        TryEnsurePendingUploadTask(log);
        RaiseLogsChanged();
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = "MES 响应已返回，但设备状态结果未能写入 JSONL，任务保持待重试。"
        };
    }

    if (!response.IsSuccess && !IsSkippedResponse(response))
    {
        TryEnsurePendingUploadTask(log);
    }

    RaiseLogsChanged();
    return response;
}

private async Task RetryInBackgroundAsync(string recordKey)
{
    try
    {
        await RetryUploadAsync(recordKey, CancellationToken.None);
    }
    catch (Exception ex)
    {
        _exceptionLogService.Write(ex, "DeviceStatusService.BackgroundUpload", $"RecordKey={recordKey}");
    }
}
```

用以下辅助方法替换旧的 `CreateLog`、默认状态、`WriteLocalStatusLog`、`PublishStatusChanged`、`MarkSkipped` 和数据库版程序边界查询；保留已有的 `ShouldReuseLatestProgramBoundaryStatus`、`NormalizeNullable`、`NormalizeStationNo`、`CurrentSettings` 与设置变更处理：

```csharp
private BizDeviceStatusLog CreateLog(
    string deviceStatus,
    string? remark,
    string source,
    int stationNo,
    int? weldTaskId,
    string? workOrderId,
    DateTime? occurredTime)
{
    var settings = CurrentSettings;
    return new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = settings.DeviceId,
        StationNo = stationNo,
        WeldTaskId = weldTaskId,
        WorkOrderId = NormalizeNullable(workOrderId),
        DeviceStatus = deviceStatus,
        StatusName = DeviceStatusReportRules.GetStatusName(deviceStatus),
        Source = string.IsNullOrWhiteSpace(source) ? "Software" : source.Trim(),
        Remark = NormalizeNullable(remark),
        OccurredTime = occurredTime ?? DateTime.Now,
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
}

private BizDeviceStatusLog? FindExistingProgramBoundaryLog(
    int stationNo,
    string normalizedStatus,
    int? weldTaskId)
{
    if (weldTaskId is null
        || normalizedStatus is not (ProductionConstants.MesDeviceStatuses.ProgramStarted
            or ProductionConstants.MesDeviceStatuses.ProgramEnded))
    {
        return null;
    }

    return GetLogs(from: null, to: null, maxCount: 5000)
        .Where(log => log.StationNo == stationNo
            && log.WeldTaskId == weldTaskId
            && string.Equals(log.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(log => log.OccurredTime)
        .FirstOrDefault();
}

private void TryEnsurePendingUploadTask(BizDeviceStatusLog log)
{
    try
    {
        _ = EnsurePendingUploadTask(log);
    }
    catch (Exception ex)
    {
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log) ?? "invalid";
        _exceptionLogService.Write(ex, "DeviceStatusService.UploadProjection", $"RecordKey={recordKey}");
    }
}

private void WriteAppendFailure(BizDeviceStatusLog log, string message)
{
    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log) ?? "invalid";
    _exceptionLogService.WriteBusiness(
        "DeviceStatusLocalLogStore",
        "设备状态 JSONL 写入失败",
        $"{message} RecordKey={recordKey}; Status={log.DeviceStatus}; Station={log.StationNo}",
        $"Directory={GetLogDirectory()}");
}

private void WriteLocalReadError(Exception exception, string context)
    => _exceptionLogService.Write(exception, "DeviceStatusLocalLogStore.Read", context);

private void RaiseLogsChanged()
    => LogsChanged?.Invoke(this, EventArgs.Empty);

private static bool IsSkippedResponse(BasicRes<object> response)
    => string.Equals(
        response.Status,
        ProductionConstants.UploadStatuses.Skipped,
        StringComparison.OrdinalIgnoreCase);
```

确认文件中不再存在 `BuildDefaultStatus`、`ReportStatusAsync`、`WriteLocalStatusLog`、`PublishStatusChanged`、`MarkSkipped`、`Queryable<BizDeviceStatusLog>`、`Insertable(log)`、`Updateable(log)` 或 `Deleteable<BizDeviceStatusLog>`。

- [ ] **Step 6: 更新测试替身以实现新增接口**

在 `FakeDeviceStatusService` 保留 Task 4 前仍需的兼容事件和 `NotifyLogsChanged`，并将查询/重试成员替换为：

```csharp
public BizDeviceStatusLog? CurrentStatus { get; set; } = new();

public BasicRes<object>? RetryResponse { get; set; } = new()
{
    Status = AppConstants.MesStatus.Success,
    Msg = "OK"
};

public List<string> RetriedRecordKeys { get; } = new();

public BizDeviceStatusLog? GetCurrentStatus()
{
    GetCurrentStatusCallCount++;
    return CurrentStatus;
}

public BizDeviceStatusLog? GetLatestStatus(int stationNo)
    => Logs.Where(log => log.StationNo == stationNo).OrderByDescending(log => log.OccurredTime).FirstOrDefault();

public IReadOnlyList<BizDeviceStatusLog> GetLogs(
    DateTime? from = null,
    DateTime? to = null,
    int maxCount = 200)
    => Logs
        .Where(log => from is null || log.OccurredTime >= from.Value)
        .Where(log => to is null || log.OccurredTime <= to.Value)
        .OrderByDescending(log => log.OccurredTime)
        .Take(maxCount)
        .ToList();

public IReadOnlyList<BizDeviceStatusLog> GetPendingLogs()
    => Logs
        .Where(log => DeviceStatusUploadVisibilityRules.ShouldInclude(log.ReportStatus))
        .OrderByDescending(log => log.OccurredTime)
        .ToList();

public BizDeviceStatusLog? GetLog(string recordKey)
    => Logs.LastOrDefault(log => string.Equals(
        DeviceStatusRecordIdentityRules.GetRecordKey(log),
        recordKey,
        StringComparison.OrdinalIgnoreCase));

public BizUploadTask? EnsurePendingUploadTask(BizDeviceStatusLog log)
{
    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
    return recordKey is null
        ? null
        : new BizUploadTask
        {
            Id = log.Id,
            TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
            BusinessId = DeviceStatusRecordIdentityRules.BuildBusinessId(recordKey),
            PayloadJson = JsonSerializer.Serialize(new { RecordKey = recordKey }),
            Status = log.ReportStatus
        };
}

public Task<BasicRes<object>?> RetryUploadAsync(
    string recordKey,
    CancellationToken cancellationToken = default)
{
    RetriedRecordKeys.Add(recordKey);
    return Task.FromResult(RetryResponse);
}
```

在该替身的 `ChangeStatusAsync` 新对象初始化中增加：

```csharp
RecordId = Guid.NewGuid().ToString("N"),
ReportStatus = ProductionConstants.UploadStatuses.Pending,
```

并把 `DeleteLogs` 的整数集合替换为记录键集合：

```csharp
var recordKeys = logs
    .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
    .Where(recordKey => recordKey is not null)
    .Cast<string>()
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var deletedCount = Logs.RemoveAll(log =>
{
    var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
    return recordKey is not null && recordKeys.Contains(recordKey);
});
LogsChanged?.Invoke(this, EventArgs.Empty);
return deletedCount;
```

- [ ] **Step 7: 调整三个旧源码契约测试到 JSONL 语义**

将 `DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload` 方法替换为：

```csharp
static void DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var sendMethod = ExtractMethodText(
        serviceCode,
        "private async Task<BasicRes<object>> SendToMesAsync",
        "private BasicRes<object>? PersistReportResult");
    var persistMethod = ExtractMethodText(
        serviceCode,
        "private BasicRes<object>? PersistReportResult",
        "private async Task RetryInBackgroundAsync");

    AssertTrue(
        sendMethod.Contains("Ts = log.OccurredTime.ToString(\"yyyy-MM-dd HH:mm:ss\")", StringComparison.Ordinal),
        "MES 设备状态接口时间格式仍应按接口约定保持到秒。");
    AssertTrue(
        persistMethod.Contains("DeviceStatusLocalLogStore.TryAppendVersion(log", StringComparison.Ordinal),
        "MES 结果必须追加到同一个 JSONL 记录，不能回写数据库。");
    AssertFalse(
        persistMethod.Contains("InSingle", StringComparison.Ordinal),
        "结果追加不能用数据库回读对象覆盖原始毫秒时间。");
}
```

在 `DeviceStatusLogDeletionRefreshIsWiredAcrossViews` 中删除旧的 `UseTran` 断言，并替换为：

```csharp
AssertTrue(serviceCode.Contains("DeviceStatusLocalLogStore.TryRemove", StringComparison.Ordinal), "设备状态删除必须先重写 JSONL。");
AssertTrue(serviceCode.Contains("SoftDeleteUnfinishedUploadTasks", StringComparison.Ordinal), "删除来源后必须软删除未成功派生任务。");
AssertFalse(serviceCode.Contains("Deleteable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态删除不能再操作旧表。");
```

在 `DeviceStatusPendingSourceAndTaskReconciliationAreWired` 中把旧查重断言替换为：

```csharp
AssertTrue(
    serviceCode.Contains("var existing = FindExistingUploadTask(recordKey);", StringComparison.Ordinal),
    "设备状态任务补建必须按 JSONL 记录键兼容查找现有任务。");
```

- [ ] **Step 8: 运行回归测试并确认 GREEN**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 三个新增服务/数据库用例通过；六个状态码、去重、毫秒时间和生命周期调用方旧用例继续通过。构造函数由 DI 自动解析 `IProgramExceptionLogService`，无需修改 `AutoWeldSystem.UI/Program.cs`。

- [ ] **Step 9: 提交 JSONL 写入优先服务**

```powershell
git add AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs AutoWeldSystem.Services/Production/DeviceStatusService.cs AutoWeldSystem.Data/SqlSugarDbContext.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "refactor(logs): 改为设备状态 JSONL 写入优先"
```

---

### Task 3: 让设备状态补传任务完全受 JSONL 来源约束

**Files:**
- Modify: `AutoWeldSystem.Core/DTOs/Upload/UploadTaskSummary.cs`
- Modify: `AutoWeldSystem.Services/Production/UploadTaskService.cs`
- Modify: `AutoWeldSystem.UI/Views/StateManageView.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: `IDeviceStatusService.GetPendingLogs`、`GetLog`、`EnsurePendingUploadTask`、`RetryUploadAsync` 和 Task 1 的任务记录键兼容规则。
- Produces: `UploadTaskSummary.DeviceStatusRecordKey: string`；所有设备状态列表与执行路径在 MES 调用前校验 JSONL。

- [ ] **Step 1: 加入任务 payload、投影清理和执行门禁测试**

在测试列表加入：

```csharp
("Device status upload task payload contains only record key", DeviceStatusUploadTaskPayloadContainsOnlyRecordKey),
("Device status upload execution revalidates jsonl source", DeviceStatusUploadExecutionRevalidatesJsonlSource),
("Device status pending projection preserves uploaded history", DeviceStatusPendingProjectionPreservesUploadedHistory),
```

加入以下测试方法：

```csharp
static void DeviceStatusUploadTaskPayloadContainsOnlyRecordKey()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var buildTaskMethod = ExtractMethodText(
        serviceCode,
        "private static BizUploadTask BuildDeviceStatusUploadTask",
        "private BizUploadTask? FindExistingUploadTask");

    AssertTrue(buildTaskMethod.Contains("new { RecordKey = recordKey }", StringComparison.Ordinal), "新任务 payload 必须只保存记录键。");
    AssertFalse(buildTaskMethod.Contains("LogId =", StringComparison.Ordinal), "新任务不能继续保存数据库日志 Id。");
    AssertFalse(buildTaskMethod.Contains("DeviceId =", StringComparison.Ordinal), "任务 payload 不能复制设备编号作为权威来源。");
    AssertFalse(buildTaskMethod.Contains("DevStatus =", StringComparison.Ordinal), "任务 payload 不能复制设备状态正文。");
    AssertFalse(buildTaskMethod.Contains("Remark =", StringComparison.Ordinal), "任务 payload 不能复制备注正文。");
}

static void DeviceStatusUploadExecutionRevalidatesJsonlSource()
{
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var executeMethod = ExtractMethodText(
        uploadCode,
        "public async Task<UploadTaskSummary?> ExecuteAsync",
        "public async Task<int> ExecuteAllPendingAsync");
    var executeAllMethod = ExtractMethodText(
        uploadCode,
        "public async Task<int> ExecuteAllPendingAsync",
        "public void RequestRetry");
    var uploadMethod = ExtractMethodText(
        uploadCode,
        "private Task<BasicRes<object>?> UploadDeviceStatusAsync",
        "private async Task<BasicRes<object>> UploadProcessParametersAsync");

    AssertSourceOrder(
        executeMethod,
        "_deviceStatusService.GetLog(recordKey)",
        "MarkUploading(id)",
        "单条执行必须先重新读取 JSONL，再把任务改为 Uploading。");
    AssertTrue(executeMethod.Contains("SoftDeleteDeviceStatusTask", StringComparison.Ordinal), "来源缺失时单条执行必须软删除未成功投影。");
    AssertTrue(executeAllMethod.Contains("SyncDeviceStatusTasksFromLogs", StringComparison.Ordinal), "批量执行查询任务前必须先按 JSONL 对账。");
    AssertTrue(executeAllMethod.Contains("await ExecuteAsync(taskId", StringComparison.Ordinal), "批量执行的每一条仍必须复用单条门禁。");
    AssertTrue(uploadMethod.Contains("_deviceStatusService.RetryUploadAsync", StringComparison.Ordinal), "实际 MES 请求必须由设备状态服务从 JSONL 构造。");
    AssertFalse(uploadCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "上传任务服务不能再查询设备状态旧表。");
    AssertFalse(uploadCode.Contains("Updateable(updatedLog)", StringComparison.Ordinal), "上传任务服务不能再更新设备状态旧表。");
    AssertFalse(uploadCode.Contains("ReadDeviceStatusRequest", StringComparison.Ordinal), "上传任务不能再从复制 payload 还原设备状态正文。");
}

static void DeviceStatusPendingProjectionPreservesUploadedHistory()
{
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var reconcileMethod = ExtractMethodText(
        uploadCode,
        "private HashSet<string> SyncDeviceStatusTasksFromLogs",
        "public BizUploadTask EnqueueOrUpdate");

    AssertTrue(reconcileMethod.Contains("GetPendingLogs()", StringComparison.Ordinal), "待上传设备状态必须直接来自全部 JSONL 最新版本。");
    AssertTrue(reconcileMethod.Contains("task.Status != ProductionConstants.UploadStatuses.Uploaded", StringComparison.Ordinal), "来源缺失清理必须排除已经上传的任务。");
    AssertTrue(reconcileMethod.Contains("task.IsDeleted = true", StringComparison.Ordinal), "来源缺失的未成功任务必须软删除。");
    AssertFalse(reconcileMethod.Contains("Deleteable<BizUploadTask>", StringComparison.Ordinal), "派生任务清理不能物理删除诊断记录。");
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 新增源码契约失败；当前实现仍从任务 payload 构造 `ReportDeviceStatusReq`、直接查询/更新 `BizDeviceStatusLog`，且批量执行未先对账 JSONL。

- [ ] **Step 3: 将上传摘要的设备状态关联改为字符串记录键**

在 `UploadTaskSummary` 中删除 `DeviceStatusLogId`，加入：

```csharp
/// <summary>
/// JSONL record key when this row represents a device-status upload.
/// </summary>
public string DeviceStatusRecordKey { get; set; } = string.Empty;
```

- [ ] **Step 4: 查询设备状态任务前重建有效投影并软删除失效投影**

用以下实现替换 `UploadTaskService.GetTasks` 中的整数集合逻辑：

```csharp
public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false)
{
    var normalizedTaskType = NormalizeTaskType(taskType);
    var deviceStatusRecordKeys = normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
        ? SyncDeviceStatusTasksFromLogs()
        : null;

    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var query = _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.TaskType == normalizedTaskType && !task.IsDeleted);
        if (!includeCompleted)
        {
            query = query.Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded);
        }

        var rows = query.ToList()
            .OrderByDescending(task => IsActionRequired(task.Status))
            .ThenByDescending(task => task.UpdatedTime)
            .Select(ToSummary)
            .ToList();
        if (deviceStatusRecordKeys is not null)
        {
            rows = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.DeviceStatusRecordKey)
                    && deviceStatusRecordKeys.Contains(row.DeviceStatusRecordKey))
                .ToList();
        }

        return rows;
    }
}
```

用以下完整方法替换 `SyncDeviceStatusTasksFromLogs`：

```csharp
private HashSet<string> SyncDeviceStatusTasksFromLogs()
{
    var logs = _deviceStatusService.GetPendingLogs().ToList();
    var activeRecordKeys = logs
        .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
        .Where(recordKey => recordKey is not null)
        .Cast<string>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var log in logs)
    {
        _ = _deviceStatusService.EnsurePendingUploadTask(log);
    }

    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var now = DateTime.Now;
        var staleTasks = _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
                && !task.IsDeleted
                && task.Status != ProductionConstants.UploadStatuses.Uploaded)
            .ToList()
            .Where(task =>
            {
                var recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson);
                return recordKey is null || !activeRecordKeys.Contains(recordKey);
            })
            .ToList();
        foreach (var task in staleTasks)
        {
            task.IsDeleted = true;
            task.DeletedTime = now;
            task.UpdatedTime = now;
            task.Message = "Device status JSONL source is missing or no longer pending.";
        }

        if (staleTasks.Count > 0)
        {
            _dbContext.Db.Updateable(staleTasks).ExecuteCommand();
        }
    }

    return activeRecordKeys;
}
```

- [ ] **Step 5: 在单条和批量执行入口增加 JSONL 二次校验**

用以下实现替换 `ExecuteAsync`：

```csharp
public async Task<UploadTaskSummary?> ExecuteAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    var candidate = GetRetryableTask(id);
    if (candidate is null)
    {
        return null;
    }

    string? recordKey = null;
    if (string.Equals(
        candidate.TaskType,
        ProductionConstants.UploadTaskTypes.DeviceStatus,
        StringComparison.OrdinalIgnoreCase))
    {
        recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(
            candidate.BusinessId,
            candidate.PayloadJson);
        var source = recordKey is null ? null : _deviceStatusService.GetLog(recordKey);
        if (source is null || !DeviceStatusUploadVisibilityRules.ShouldInclude(source.ReportStatus))
        {
            SoftDeleteDeviceStatusTask(candidate.Id, "Device status JSONL source is missing or no longer pending.");
            return null;
        }
    }

    var task = MarkUploading(id);
    if (task is null)
    {
        return null;
    }

    BasicRes<object>? response = recordKey is null
        ? await ExecuteByTypeAsync(task, cancellationToken)
        : await UploadDeviceStatusAsync(recordKey, cancellationToken);
    if (response is null)
    {
        SoftDeleteDeviceStatusTask(task.Id, "Device status JSONL source was removed before MES upload.");
        return null;
    }

    return FinishExecution(task.Id, response);
}

private BizUploadTask? GetRetryableTask(int id)
{
    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
        return task is not null && UploadTaskVisibilityRules.ShouldRetry(task)
            ? task
            : null;
    }
}

private void SoftDeleteDeviceStatusTask(int id, string message)
{
    UploadTaskStatusChangedEventArgs? changed = null;
    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
        if (task is null
            || task.IsDeleted
            || task.Status == ProductionConstants.UploadStatuses.Uploaded)
        {
            return;
        }

        task.IsDeleted = true;
        task.DeletedTime = DateTime.Now;
        task.UpdatedTime = DateTime.Now;
        task.Message = message;
        _dbContext.Db.Updateable(task).ExecuteCommand();
        changed = ToStatusChangedEvent(task, "Deleted");
    }

    PublishTaskStatusChanged(changed);
}
```

用以下实现替换 `ExecuteAllPendingAsync`，确保列表对账发生在数据库任务枚举之前：

```csharp
public async Task<int> ExecuteAllPendingAsync(
    string taskType,
    CancellationToken cancellationToken = default)
{
    var normalizedTaskType = NormalizeTaskType(taskType);
    if (normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus)
    {
        _ = SyncDeviceStatusTasksFromLogs();
    }

    List<int> taskIds;
    lock (_dbLock)
    {
        _dbContext.InitDatabase();
        taskIds = _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.TaskType == normalizedTaskType && !task.IsDeleted)
            .ToList()
            .Where(UploadTaskVisibilityRules.ShouldRetry)
            .Select(task => task.Id)
            .ToList();
    }

    var executedCount = 0;
    foreach (var taskId in taskIds)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await ExecuteAsync(taskId, cancellationToken) is not null)
        {
            executedCount++;
        }
    }

    return executedCount;
}
```

在 `RequestRetryAll` 获取数据库锁之前加入同样的对账，避免旧按钮路径重新激活失效设备状态任务：

```csharp
var normalizedTaskType = NormalizeTaskType(taskType);
if (normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus)
{
    _ = SyncDeviceStatusTasksFromLogs();
}
```

并删除该方法锁内重复声明的 `normalizedTaskType`。

- [ ] **Step 6: 让实际上报只通过设备状态服务读取 JSONL**

从 `ExecuteByTypeAsync` 的 switch 中删除设备状态分支：

```csharp
ProductionConstants.UploadTaskTypes.DeviceStatus => await UploadDeviceStatusAsync(task, cancellationToken),
```

用以下方法替换旧 `UploadDeviceStatusAsync`：

```csharp
private Task<BasicRes<object>?> UploadDeviceStatusAsync(
    string recordKey,
    CancellationToken cancellationToken)
    => _deviceStatusService.RetryUploadAsync(recordKey, cancellationToken);
```

完整删除以下旧成员，因为它们会让任务 payload 或数据库成为第二来源：

```text
UpdateDeviceStatusLog
TryPreserveLocalOccurredTime
ReadDeviceStatusRequest
ReadDeviceStatusCode
ReadDeviceStatusLogId
DeviceStatusUploadRequest
IsDeviceStatusReportEnabled
```

同时删除 `using AutoWeldSystem.Services.Log;`；`UploadTaskService` 不再直接访问 `DeviceStatusLocalLogStore`。

- [ ] **Step 7: 摘要显示从 JSONL 来源取状态码和工位**

用以下实现替换 `ToSummary` 和设备状态身份分支：

```csharp
private UploadTaskSummary ToSummary(BizUploadTask task)
{
    var payload = ReadUploadPayload(task.PayloadJson);
    var productNos = ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson);
    var productText = productNos.Count > 0
        ? string.Join(", ", productNos)
        : payload.ProductNo;
    var message = task.Message ?? string.Empty;
    var recordKey = string.Equals(
        task.TaskType,
        ProductionConstants.UploadTaskTypes.DeviceStatus,
        StringComparison.OrdinalIgnoreCase)
            ? DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson)
            : null;
    var deviceStatusLog = recordKey is null ? null : _deviceStatusService.GetLog(recordKey);

    return new UploadTaskSummary
    {
        Id = task.Id,
        TaskType = task.TaskType,
        Target = task.Target,
        BusinessId = task.BusinessId ?? string.Empty,
        DeviceStatusRecordKey = recordKey ?? string.Empty,
        TaskIdentity = ResolveTaskSummaryIdentity(task, deviceStatusLog),
        StationNo = deviceStatusLog?.StationNo ?? payload.StationNo,
        ProductNo = productText,
        Status = task.Status,
        IsVirtual = false,
        CanRetry = task.Status != ProductionConstants.UploadStatuses.Uploaded,
        CanDelete = true,
        RetryCount = task.RetryCount,
        MaxRetryCount = task.MaxRetryCount,
        NextRetryTime = task.NextRetryTime,
        LastAttemptTime = task.LastAttemptTime,
        CompletedTime = task.CompletedTime,
        FilePath = ResolveDisplayFilePath(task),
        Message = message,
        DisplayMessage = message,
        CreatedTime = task.CreatedTime,
        UpdatedTime = task.UpdatedTime
    };
}

private string ResolveTaskSummaryIdentity(
    BizUploadTask task,
    BizDeviceStatusLog? deviceStatusLog)
{
    if (deviceStatusLog is not null)
    {
        return DeviceStatusReportRules.FormatStatusIdentity(deviceStatusLog.DeviceStatus);
    }

    return ResolveTaskIdentity(task);
}
```

- [ ] **Step 8: StateManageView 按字符串记录键索引与删除**

把字段替换为：

```csharp
private readonly Dictionary<string, BizDeviceStatusLog> _deviceStatusLogsByRecordKey =
    new(StringComparer.OrdinalIgnoreCase);
```

用以下实现替换 `RefreshDeviceStatusLogIndex`：

```csharp
private void RefreshDeviceStatusLogIndex()
{
    _deviceStatusLogsByRecordKey.Clear();
    foreach (var log in _deviceStatusService.GetPendingLogs())
    {
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
        if (recordKey is not null)
        {
            _deviceStatusLogsByRecordKey[recordKey] = log;
        }
    }
}
```

在 `DeleteSelectedDeviceStatusTasks` 中用以下片段替换 `DeviceStatusLogId` 映射和兜底任务删除：

```csharp
var selectedLogs = selectedTasks
    .Where(task => !string.IsNullOrWhiteSpace(task.DeviceStatusRecordKey))
    .Select(task => _deviceStatusLogsByRecordKey.TryGetValue(task.DeviceStatusRecordKey, out var log) ? log : null)
    .Where(log => log is not null)
    .Cast<BizDeviceStatusLog>()
    .ToList();
var selectedRecordKeys = selectedLogs
    .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
    .Where(recordKey => recordKey is not null)
    .Cast<string>()
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
int deletedCount;
try
{
    deletedCount = _deviceStatusService.DeleteLogs(selectedLogs);
}
catch (Exception ex)
{
    ShowErrorMessage(ex.Message);
    return;
}

var orphanTasks = selectedTasks
    .Where(task => string.IsNullOrWhiteSpace(task.DeviceStatusRecordKey)
        || !selectedRecordKeys.Contains(task.DeviceStatusRecordKey))
    .ToList();
foreach (var task in orphanTasks)
{
    _uploadTaskService.DeleteTask(task.Id);
}

ReloadActiveTasks();
dgvPending.ClearSelection();
ShowInfo($"已删除选中的 {deletedCount + orphanTasks.Count} 条设备状态上传记录。");
```

- [ ] **Step 9: 更新旧投影与删除源码契约测试**

将 `DeviceStatusPendingSourceAndTaskReconciliationAreWired` 中关于 `DeviceStatusLogId`、`GetLogs` 的断言替换为：

```csharp
AssertTrue(interfaceCode.Contains("GetPendingLogs", StringComparison.Ordinal), "接口必须直接暴露 JSONL 待上传来源。");
AssertTrue(interfaceCode.Contains("RetryUploadAsync", StringComparison.Ordinal), "接口必须提供按记录键重新读取并上报的方法。");
AssertTrue(uploadTaskCode.Contains("GetPendingLogs()", StringComparison.Ordinal), "任务查询必须以全部 JSONL 最新版本为来源。");
AssertTrue(uploadTaskCode.Contains("SyncDeviceStatusTasksFromLogs", StringComparison.Ordinal), "任务查询和批量执行必须先对账来源。");
AssertTrue(summaryCode.Contains("DeviceStatusRecordKey", StringComparison.Ordinal), "上传摘要必须携带 JSONL 记录键。");
AssertFalse(summaryCode.Contains("DeviceStatusLogId", StringComparison.Ordinal), "上传摘要不能继续依赖数据库日志 Id。");
```

将 `DeviceStatusLogDeletionRefreshIsWiredAcrossViews` 最后一条断言替换为：

```csharp
AssertTrue(uploadTaskCode.Contains("_deviceStatusService.RetryUploadAsync", StringComparison.Ordinal), "设备状态补传结果和刷新必须统一由设备状态服务处理。");
AssertFalse(uploadTaskCode.Contains("NotifyLogsChanged", StringComparison.Ordinal), "上传任务服务不能绕过设备状态服务公开触发日志刷新。");
```

- [ ] **Step 10: 运行回归测试并确认 GREEN**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 三个新增补传门禁用例通过；设备状态页签仍显示 `Pending/Failed`，`Uploaded/Skipped` 不显示；旧 `LogId` 任务仍能解析，但找不到对应 JSONL 时被软删除且不调用 MES。

- [ ] **Step 11: 提交补传来源门禁**

```powershell
git add AutoWeldSystem.Core/DTOs/Upload/UploadTaskSummary.cs AutoWeldSystem.Services/Production/UploadTaskService.cs AutoWeldSystem.UI/Views/StateManageView.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "fix(logs): 按 JSONL 来源约束设备状态补传"
```

---

### Task 4: 统一日志 UI、设备 API 与中心遥测消费者

**Files:**
- Modify: `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs`
- Modify: `AutoWeldSystem.UI/Views/LogManageView.cs`
- Modify: `AutoWeldSystem.Services/Production/DeviceApiEndpointService.cs`
- Modify: `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs`
- Modify: `AutoWeldSystem.Tests/Program.cs`

**Interfaces:**
- Consumes: Task 2/3 的可空当前状态、工位最新状态和 `LogsChanged`。
- Produces: 所有设备状态消费者只读 JSONL；最终接口删除 `StatusChanged` 和公开 `NotifyLogsChanged`。

- [ ] **Step 1: 加入无来源 API、消费者解耦和 UI 重载测试**

在测试列表加入：

```csharp
("Device status API rejects missing jsonl record", DeviceStatusApiRejectsMissingJsonlRecord),
("Device status consumers do not query legacy table", DeviceStatusConsumersDoNotQueryLegacyTable),
("Log manage reloads device status jsonl on reentry", LogManageReloadsDeviceStatusJsonlOnReentry),
```

加入以下测试方法：

```csharp
static void DeviceStatusApiRejectsMissingJsonlRecord()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001" }
    };
    var statusService = new FakeDeviceStatusService { CurrentStatus = null };
    var service = CreateDeviceApiEndpointService(settings, statusService);

    var response = service.GetDeviceStatus("D-001");

    AssertFalse(response.IsSuccess, "JSONL 没有有效记录时设备状态 API 必须返回失败。");
    AssertEqual("暂无设备状态记录", response.Msg, "无来源失败消息必须稳定，不能伪造默认开机状态。");
    AssertEqual(null, response.Data, "无来源时不能返回设备状态 Data。");
    AssertEqual(1, statusService.GetCurrentStatusCallCount, "设备编号校验通过后应读取一次 JSONL 当前状态。");
}

static void DeviceStatusConsumersDoNotQueryLegacyTable()
{
    var apiCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceApiEndpointService.cs"),
        Encoding.UTF8);
    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetrySyncService.cs"),
        Encoding.UTF8);
    var interfaceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Interfaces", "IDeviceStatusService.cs"),
        Encoding.UTF8);

    AssertTrue(apiCode.Contains("var currentStatus = _deviceStatusService.GetCurrentStatus();", StringComparison.Ordinal), "设备 API 必须通过设备状态服务读取 JSONL。");
    AssertTrue(apiCode.Contains("暂无设备状态记录", StringComparison.Ordinal), "设备 API 必须显式处理空 JSONL。");
    AssertTrue(centerCode.Contains("_deviceStatusService.GetLatestStatus(stationNo)", StringComparison.Ordinal), "中心遥测必须通过设备状态服务读取工位最新 JSONL。");
    AssertFalse(centerCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "中心遥测不能再查询设备状态旧表。");
    AssertFalse(interfaceCode.Contains("StatusChanged", StringComparison.Ordinal), "最终接口只保留来源重载事件，不能保留重复实时插入事件。");
    AssertFalse(interfaceCode.Contains("NotifyLogsChanged", StringComparison.Ordinal), "最终接口不能允许外部伪造来源变更通知。");
}

static void LogManageReloadsDeviceStatusJsonlOnReentry()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var visibleMethod = ExtractMethodText(
        viewCode,
        "protected override void OnVisibleChanged",
        "protected override void OnLanguageChanged");
    var wireMethod = ExtractMethodText(
        viewCode,
        "private void WireEvents()",
        "private void ShowLogDate_CheckedChanged");

    AssertTrue(visibleMethod.Contains("LoadDeviceStatusLogs();", StringComparison.Ordinal), "重新进入日志管理页必须重读当前日期 JSONL。");
    AssertTrue(wireMethod.Contains("_deviceStatusService.LogsChanged +=", StringComparison.Ordinal), "日志页必须监听持久化来源变化。");
    AssertFalse(wireMethod.Contains("_deviceStatusService.StatusChanged +=", StringComparison.Ordinal), "日志页不能同时监听实时行事件造成重复插入。");
    AssertFalse(viewCode.Contains("AddLiveDeviceStatusLog", StringComparison.Ordinal), "设备状态行只能从 JSONL 重载，不能直接附加内存对象。");
    AssertFalse(viewCode.Contains("FileSystemWatcher", StringComparison.Ordinal), "外部删除不增加文件监听器。");
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 新增测试失败；当前日志页仍订阅 `StatusChanged`，中心遥测仍查询 `BizDeviceStatusLog`，设备 API 在空来源时仍可能解引用或返回旧默认状态。

- [ ] **Step 3: 收紧最终 IDeviceStatusService 接口**

用以下完整内容替换 `IDeviceStatusService.cs`，删除 Task 2 暂留的 `StatusChanged` 和 `NotifyLogsChanged`：

```csharp
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 设备状态 JSONL 查询、写入和 MES 上报服务。
/// </summary>
public interface IDeviceStatusService
{
    event EventHandler? LogsChanged;

    BizDeviceStatusLog? GetCurrentStatus();

    BizDeviceStatusLog? GetLatestStatus(int stationNo);

    IReadOnlyList<BizDeviceStatusLog> GetLogs(
        DateTime? from = null,
        DateTime? to = null,
        int maxCount = 200);

    IReadOnlyList<BizDeviceStatusLog> GetPendingLogs();

    BizDeviceStatusLog? GetLog(string recordKey);

    BizUploadTask? EnsurePendingUploadTask(BizDeviceStatusLog log);

    string GetLogDirectory();

    int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs);

    Task<BasicRes<object>?> RetryUploadAsync(
        string recordKey,
        CancellationToken cancellationToken = default);

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
}
```

从 `DeviceStatusService` 删除兼容事件和公开通知方法：

```csharp
public event EventHandler<BizDeviceStatusLog>? StatusChanged;

public void NotifyLogsChanged() => RaiseLogsChanged();
```

- [ ] **Step 4: 日志管理只保留 JSONL 重载事件**

把 `LogManageView.OnVisibleChanged` 替换为：

```csharp
protected override void OnVisibleChanged(EventArgs e)
{
    base.OnVisibleChanged(e);
    Volatile.Write(ref _viewVisible, Visible ? 1 : 0);
    if (Visible && _initialized)
    {
        QueueExceptionLogFlush();
        LoadDeviceStatusLogs();
    }
}
```

从 `WireEvents()` 删除：

```csharp
_deviceStatusService.StatusChanged += DeviceStatusService_StatusChanged;
Disposed += (_, _) => _deviceStatusService.StatusChanged -= DeviceStatusService_StatusChanged;
```

保留以下来源重载订阅：

```csharp
_deviceStatusService.LogsChanged += DeviceStatusService_LogsChanged;
Disposed += (_, _) => _deviceStatusService.LogsChanged -= DeviceStatusService_LogsChanged;
```

完整删除 `DeviceStatusService_StatusChanged` 和 `AddLiveDeviceStatusLog`。`DeviceStatusService_LogsChanged` 保持通过 `LoadDeviceStatusLogs()` 重读文件，不直接接收日志对象。

- [ ] **Step 5: 设备 API 对空 JSONL 返回稳定失败**

用以下完整方法替换 `DeviceApiEndpointService.GetDeviceStatus`：

```csharp
public BasicRes<DeviceStatusQueryRes> GetDeviceStatus(string? deviceId)
{
    var settings = _settingsService.Get();
    var currentDeviceId = DeviceApiEndpointRules.NormalizeText(settings.DeviceId);
    BasicRes<DeviceStatusQueryRes> response = Failure<DeviceStatusQueryRes>("设备状态查询未完成");

    try
    {
        if (string.IsNullOrWhiteSpace(currentDeviceId))
        {
            response = Failure<DeviceStatusQueryRes>("本地设备编号未配置");
            return response;
        }

        if (!DeviceApiEndpointRules.IsRequestedDeviceIdAllowed(deviceId, currentDeviceId))
        {
            response = Failure<DeviceStatusQueryRes>("设备编号不匹配");
            return response;
        }

        var currentStatus = _deviceStatusService.GetCurrentStatus();
        if (currentStatus is null)
        {
            response = Failure<DeviceStatusQueryRes>("暂无设备状态记录");
            return response;
        }

        var statusCode = DeviceStatusReportRules.NormalizeMesDeviceStatusCode(currentStatus.DeviceStatus);
        response = Success(new DeviceStatusQueryRes
        {
            DeviceId = currentDeviceId,
            DeviceStatus = statusCode
        });
        return response;
    }
    catch (Exception ex)
    {
        response = Failure<DeviceStatusQueryRes>($"设备状态查询失败：{ex.Message}");
        return response;
    }
    finally
    {
        WriteDeviceStatusQueryLifecycleLog(deviceId, currentDeviceId, response);
    }
}
```

- [ ] **Step 6: 中心遥测通过 IDeviceStatusService 获取 JSONL 回退**

在 `CenterTelemetrySyncService` 字段中加入：

```csharp
private readonly IDeviceStatusService _deviceStatusService;
```

把构造函数签名和赋值改为：

```csharp
public CenterTelemetrySyncService(
    SqlSugarDbContext dbContext,
    IAppSettingsService settingsService,
    IDeviceStatusService deviceStatusService,
    IPlcCommunicationService plcCommunicationService,
    IPlcProductionMonitorService productionMonitorService,
    IProgramExceptionLogService exceptionLogService,
    CenterTelemetryClient client)
{
    _dbContext = dbContext;
    _settingsService = settingsService;
    _deviceStatusService = deviceStatusService;
    _plcCommunicationService = plcCommunicationService;
    _productionMonitorService = productionMonitorService;
    _exceptionLogService = exceptionLogService;
    _client = client;
    Current = new CenterTelemetryConnectionSnapshot(false, default, "Center telemetry has not been pushed yet.");
}
```

在 `BuildStationSnapshot` 中替换最新状态读取行：

```csharp
var latestStatus = _deviceStatusService.GetLatestStatus(stationNo);
```

完整删除原 `GetLatestDeviceStatus` 数据库查询方法。保留 `ResolvePlcStatusCode` 的现有优先级：有效实时 PLC 状态优先，其次使用 JSONL 状态，二者都没有时返回空字符串；`CenterTelemetryRules.ResolvePlcStatusName` 会把空值显示为“未知”。

- [ ] **Step 7: 更新 FakeDeviceStatusService 和旧中心遥测源码测试**

从 `FakeDeviceStatusService` 删除 `StatusChanged` 事件与 `NotifyLogsChanged`，并把 `ChangeStatusAsync` 末尾替换为：

```csharp
Logs.Add(log);
LogsChanged?.Invoke(this, EventArgs.Empty);
return Task.FromResult(log);
```

在 `PlcSoftwareAlarmsStayLocalToMonitorView` 中把中心遥测方法提取的结束标记替换为：

```csharp
var buildStationSnapshot = ExtractMethodText(
    centerCode,
    "private CenterTelemetryStationSnapshot BuildStationSnapshot",
    "private TodayProductionSummary GetTodayProductionSummary");
```

并追加断言：

```csharp
AssertTrue(
    buildStationSnapshot.Contains("_deviceStatusService.GetLatestStatus(stationNo)", StringComparison.Ordinal),
    "PLC 无有效值时中心遥测必须从设备状态 JSONL 获取回退状态。");
```

- [ ] **Step 8: 运行回归测试并确认 GREEN**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 三个新增消费者用例通过；已有设备 API 成功、编号不匹配、生命周期审计以及中心遥测报警内容用例继续通过。

- [ ] **Step 9: 提交消费者统一改造**

```powershell
git add AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs AutoWeldSystem.UI/Views/LogManageView.cs AutoWeldSystem.Services/Production/DeviceApiEndpointService.cs AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "refactor(logs): 统一设备状态 JSONL 消费入口"
```

---

### Task 5: 更新现场说明并执行完整验证

**Files:**
- Modify: `README.md`
- Modify: `docs/QUICK_START.md`
- Modify: `AutoWeldSystem.Tests/Program.cs`
- Verify: `AutoWeldSystem.Core/Entities/BizDeviceStatusLog.cs`
- Verify: `AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs`
- Verify: `AutoWeldSystem.Data/SqlSugarDbContext.cs`
- Verify: `AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs`
- Verify: `AutoWeldSystem.Services/Production/DeviceStatusService.cs`
- Verify: `AutoWeldSystem.Services/Production/UploadTaskService.cs`
- Verify: `AutoWeldSystem.Services/Production/DeviceApiEndpointService.cs`
- Verify: `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs`
- Verify: `AutoWeldSystem.UI/Views/LogManageView.cs`
- Verify: `AutoWeldSystem.UI/Views/StateManageView.cs`

**Interfaces:**
- Consumes: Task 1-4 的最终行为和项目 README/验证规则。
- Produces: 现场可执行的唯一来源与故障排查说明，以及自动测试、构建、源码审计和手工联调清单。

- [ ] **Step 1: 加入文档契约回归用例**

在测试列表加入：

```csharp
("Device status jsonl source behavior is documented", DeviceStatusJsonlSourceBehaviorIsDocumented),
```

加入测试方法：

```csharp
static void DeviceStatusJsonlSourceBehaviorIsDocumented()
{
    var readme = File.ReadAllText(GetRepoFilePath("README.md"), Encoding.UTF8);
    var quickStart = File.ReadAllText(GetRepoFilePath("docs", "QUICK_START.md"), Encoding.UTF8);

    AssertTrue(readme.Contains("设备状态 JSONL 是唯一事实来源", StringComparison.Ordinal), "README 必须说明设备状态唯一来源。");
    AssertTrue(readme.Contains("未成功上传", StringComparison.Ordinal), "README 必须说明删除来源会取消未成功记录的补传资格。");
    AssertTrue(readme.Contains("已成功上传", StringComparison.Ordinal), "README 必须说明已上传结果不因本地删除而撤销。");
    AssertTrue(readme.Contains("程序异常日志", StringComparison.Ordinal), "README 必须给出落盘失败排障入口。");
    AssertTrue(quickStart.Contains("不再读写 `Biz_DeviceStatusLog`", StringComparison.Ordinal), "快速入门不能继续描述数据库与 JSONL 双来源。");
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 仅新增文档契约失败，因为 README 和快速入门尚未说明最终行为。

- [ ] **Step 3: 在 README 增加唯一来源与排障说明**

在 `README.md` 的“运行”与“版本管理”之间加入：

```markdown
## 设备状态日志与补传

设备状态固定使用 `0=停机`、`1=开机`、`4=异常`、`5=异常恢复`、`6=程序执行开始`、`7=程序执行结束`。设备状态 JSONL 是唯一事实来源，文件位于配置日志根目录下的 `DeviceStatus/*.jsonl`；日志管理、当前状态查询和待上传数据中的设备状态都从这里读取。

- 状态变化会先写入 JSONL，落盘成功后才会刷新界面、上传 MES 或建立补传任务。
- 删除某个日期文件或整个 `DeviceStatus` 目录后，刷新或重新进入日志管理/待上传页面即可移除对应记录；其中未成功上传的状态不再参与单条或批量补传。
- 已成功上传到 MES 的结果不会因为删除本地 JSONL 而撤销，已有上传任务历史也不会回退为待上传。
- JSONL 首次落盘失败时不会上传该状态，也不会生成补传任务。请到“日志管理 -> 程序异常日志”检查磁盘空间、日志目录和写入权限。
- 升级不会删除旧的 `Biz_DeviceStatusLog` 物理表，但程序不再创建、读取或写入该表。
```

保持 README 版本为 `v1.0.9`，与 `Directory.Build.props` 一致；本任务不修改软件版本。

- [ ] **Step 4: 修正 QUICK_START 中已过时的双来源描述**

把 `docs/QUICK_START.md` 日志段落中设备状态说明替换为：

```markdown
- `MesInteractionLogService`、`ProductionFlowLogService`、`ProgramExceptionLogService`、`DeviceLifecycleLogService` 和 `DeviceStatusLocalLogStore` 把 JSONL 日志写到配置的日志目录。
- `LogManageView` 是查看入口；设备状态只以 `DeviceStatus/*.jsonl` 为事实来源，不再读写 `Biz_DeviceStatusLog`。`BizUploadTask` 仅保存可由 JSONL 重建的设备状态补传索引。
```

把数据库表用途表中上传行替换为：

```markdown
| 上传 | `BizUploadTask`、`BizProductionReportFile` | 保存通用待传、重试、失败和本地报告状态；设备状态正文只保存在 JSONL |
```

- [ ] **Step 5: 运行完整回归 harness**

Run:

```powershell
dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore
```

Expected: 全部回归用例通过，包括以下新增覆盖：

```text
Device status record identity supports guid and legacy keys
Device status local log store uses record keys
Device status local log store skips invalid identities
Device status service writes jsonl before MES
Device status service stops when first jsonl write fails
Device status runtime no longer persists database log rows
Device status upload task payload contains only record key
Device status upload execution revalidates jsonl source
Device status pending projection preserves uploaded history
Device status API rejects missing jsonl record
Device status consumers do not query legacy table
Log manage reloads device status jsonl on reentry
Device status jsonl source behavior is documented
```

若 harness 在既有无关用例先失败，准确记录测试名称和错误，不得声称全量通过；修复本任务引入的编译或回归后重新运行。

- [ ] **Step 6: 使用备用输出目录构建解决方案**

Run:

```powershell
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\
```

Expected: `Build succeeded.`，0 errors。警告必须逐条确认是否为本任务新增；不得用构建成功替代回归 harness 结论。

- [ ] **Step 7: 审计旧表、旧事件、旧整数 DTO 与文件监听器残留**

Run:

```powershell
rg -n "Queryable<BizDeviceStatusLog>|Insertable\(.*BizDeviceStatusLog|Updateable\(.*BizDeviceStatusLog|Deleteable<BizDeviceStatusLog>|typeof\(BizDeviceStatusLog\)" AutoWeldSystem.Core AutoWeldSystem.Data AutoWeldSystem.Services AutoWeldSystem.UI
rg -n "_deviceStatusService\.StatusChanged|NotifyLogsChanged|DeviceStatusLogId|ReadDeviceStatusRequest|UpdateDeviceStatusLog" AutoWeldSystem.Core/Interfaces/IDeviceStatusService.cs AutoWeldSystem.Core/DTOs/Upload/UploadTaskSummary.cs AutoWeldSystem.Services/Production/DeviceStatusService.cs AutoWeldSystem.Services/Production/UploadTaskService.cs AutoWeldSystem.UI/Views/LogManageView.cs AutoWeldSystem.UI/Views/StateManageView.cs
rg -n "FileSystemWatcher" AutoWeldSystem.Services/Log/DeviceStatusLocalLogStore.cs AutoWeldSystem.Services/Production/DeviceStatusService.cs AutoWeldSystem.UI/Views/LogManageView.cs AutoWeldSystem.UI/Views/StateManageView.cs
```

Expected: 三条命令均无输出。`BizDeviceStatusLog` 类型仍可出现在 JSON 模型、规则签名和测试中，但不能出现在 SqlSugar 查询、写入或 CodeFirst 注册中。

- [ ] **Step 8: 检查状态码、任务 payload 和写入顺序**

Run:

```powershell
rg -n "PoweredOn|Stopped|Exception|Recovered|ProgramStarted|ProgramEnded" AutoWeldSystem.Core/Constants/ProductionConstants.cs AutoWeldSystem.Core/Production/DeviceStatusReportRules.cs
rg -n "RecordKey|LogId|DevStatus|DeviceId|Remark" AutoWeldSystem.Services/Production/DeviceStatusService.cs AutoWeldSystem.Services/Production/UploadTaskService.cs
git diff --check
```

Expected:

- 状态常量仍为 `0/1/4/5/6/7`，PLC 原始 `1/2/3/4` 常量未被改写。
- `BuildDeviceStatusUploadTask` 的序列化对象只含 `RecordKey`；`LogId` 只出现在兼容解析规则或测试中。
- `ReportDeviceStatusReq` 只在读取到有效 JSONL 记录后构造。
- `git diff --check` 无空白错误。

- [ ] **Step 9: 执行 WinForms 手工验证**

在可运行现场配置或测试环境中逐项确认并记录结果：

```text
1. 打开日志管理的设备状态页签，触发一个实际状态变化，确认新记录先出现在 DeviceStatus 当日 JSONL，再出现在界面。
2. 触发相同普通状态，确认不重复落盘；触发现有 forceWrite 生命周期入口，确认仍会新增记录。
3. MES 离线时触发状态变化，刷新待上传数据的设备状态页签，确认出现 Pending/Failed 记录。
4. 在应用外删除对应日期 JSONL，回到日志页重新进入或点击刷新，确认日志记录消失。
5. 刷新待上传设备状态页签，确认对应未成功任务消失；点击单条/全部上传均不能向 MES 发出该记录。
6. 不删除文件时执行成功补传，确认 JSONL 追加 Uploaded，同一记录只显示最新版本且退出待上传页。
```

Expected: 不使用文件监听器，删除效果在刷新/重新进入/上传动作时生效；表格控件名称、事件绑定和布局不发生无关变化。

- [ ] **Step 10: 分别记录 MES、PLC 和 MySQL 联调边界**

MES 验证：

```text
- 在线状态变化：JSONL Pending -> MES 请求 -> JSONL Uploaded。
- MES 返回失败：JSONL Pending -> Failed，并进入设备状态补传页。
- 补传成功：从 JSONL 构造请求，追加 Uploaded 后任务完成。
- 删除未成功来源：单条和批量均不发 MES 请求。
- 已上传任务：删除 JSONL 后不撤销 MES 结果，不软删除 Uploaded 任务历史。
```

PLC 验证：

```text
- PLC 从非报警进入 4 时写 MES 状态 4=异常。
- PLC 从 4 恢复到非报警时写 MES 状态 5=异常恢复。
- PLC 运行/暂停/停止之间的普通变化不误报为新的 MES 生命周期状态。
- 软件开关机和程序开始/结束继续写 1/0/6/7。
```

MySQL 验证：

```text
- 升级已有数据库后 Biz_DeviceStatusLog 物理表仍存在，旧行不丢失。
- 触发新设备状态后该表行数和内容不再变化。
- Biz_UploadTask 仍为通用派生索引；删除 JSONL 只软删除未成功设备状态任务，Uploaded 行保持不变。
- 新数据库 CodeFirst 不再创建 Biz_DeviceStatusLog。
```

无法访问真实 PLC、MES 或 MySQL 时，在交付报告中分别写“未执行”及原因，不能用本地回归或构建代替现场结论。

- [ ] **Step 11: 提交文档与最终测试，并复核工作区**

```powershell
git add README.md docs/QUICK_START.md AutoWeldSystem.Tests/Program.cs
git diff --cached --check
git diff --cached
git commit -m "docs(readme): 说明设备状态 JSONL 单一来源"
git status --short --branch
git log -5 --oneline
```

Expected:

- 最后一个提交只包含 README、快速入门和文档契约测试。
- 前四个提交分别对应记录身份、写入优先、补传门禁和消费者统一。
- `.idea/` 仍保持原有未跟踪状态，不进入任何提交。
- README：已更新，补充设备状态 JSONL 唯一来源、删除对未成功补传的影响、已上传边界和落盘失败排障。

## 实施完成检查表

- [ ] `DeviceStatus/*.jsonl` 是日志 UI、当前状态、待上传列表和上传请求的唯一设备状态正文来源。
- [ ] 新状态首次 JSONL 落盘发生在 UI 通知、MES 请求和任务创建之前。
- [ ] 首次落盘失败只写程序异常日志，不产生 UI、MES 或任务副作用。
- [ ] 新 GUID 与旧 `legacy:{Id}` 都能追加、查找、去重和删除。
- [ ] 同一记录最后追加版本生效，`Pending/Failed` 可补传，`Uploaded/Skipped` 不可补传。
- [ ] 外部删除后刷新/重进/重试均清理未成功投影且不调用 MES，Uploaded 任务不变。
- [ ] 设备 API 无来源返回“暂无设备状态记录”；中心遥测按 PLC -> JSONL -> 未知回退。
- [ ] `Biz_DeviceStatusLog` 不再 CodeFirst 创建或被运行时代码读写，物理旧表无删除迁移。
- [ ] 未引入 `FileSystemWatcher`、轮询器、第三方依赖或无关 UI/数据库重构。
- [ ] README 与快速入门已同步，版本仍和 `Directory.Build.props` 一致。
- [ ] 回归 harness、备用输出构建、源码审计和 `git diff --check` 的实际结果已准确记录。
