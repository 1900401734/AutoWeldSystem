using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services.Log;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 设备状态 JSONL 写入、查询和 MES 上报服务。
/// </summary>
public class DeviceStatusService : IDeviceStatusService
{
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

        SoftDeleteUnfinishedUploadTasks(selectedLogs
            .Select(item => item.RecordKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase));
        RaiseLogsChanged();
        return selectedLogs.Count;
    }

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

    private static bool ShouldReuseLatestProgramBoundaryStatus(
        BizDeviceStatusLog? latest,
        string normalizedStatus,
        int? weldTaskId)
    {
        if (latest is null || weldTaskId is null)
        {
            return false;
        }

        var isProgramBoundaryStatus = normalizedStatus is ProductionConstants.MesDeviceStatuses.ProgramStarted
            or ProductionConstants.MesDeviceStatuses.ProgramEnded;
        return isProgramBoundaryStatus
            && latest.WeldTaskId == weldTaskId
            && string.Equals(latest.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.SharedStationNo
            : stationNo;

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
        => Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
}
