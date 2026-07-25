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
    // ponytail: 状态变化频率低，单锁即可保证判重与首版本落盘原子；出现实测争用后再按工位拆锁。
    private readonly object _statusChangeLock = new();
    // 设备状态频率低，单一门禁即可避免并发响应倒序，无需维护按记录锁表。
    private readonly SemaphoreSlim _uploadGate = new(1, 1);
    private readonly object _uploadTaskLock = new();
    private readonly Dictionary<string, Task<BasicRes<object>?>> _activeUploads = new(StringComparer.OrdinalIgnoreCase);
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

    public event EventHandler? LogsChanged;

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

    public bool ShouldPreserveUploadingTask(BizUploadTask task)
    {
        if (!string.Equals(
                task.Status,
                ProductionConstants.UploadStatuses.Uploading,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson);
        return recordKey is not null && (IsUploadActive(recordKey) || IsRecentUploadAttempt(task));
    }

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

            var existingStatus = existing.Status;
            var existingLastAttemptTime = existing.LastAttemptTime;
            if (existingStatus == ProductionConstants.UploadStatuses.Uploaded
                || (existingStatus == ProductionConstants.UploadStatuses.Uploading
                    && (IsUploadActive(recordKey) || IsRecentUploadAttempt(existing))))
            {
                return existing;
            }

            existing.IsDeleted = false;
            existing.DeletedTime = null;
            existing.BusinessId = task.BusinessId;
            existing.PayloadJson = task.PayloadJson;
            existing.Status = task.Status;
            existing.NextRetryTime = task.NextRetryTime;
            existing.Message = task.Message;
            existing.UpdatedTime = DateTime.Now;
            var updateable = _dbContext.Db.Updateable(existing)
                .UpdateColumns(taskRow => new
                {
                    taskRow.IsDeleted,
                    taskRow.DeletedTime,
                    taskRow.BusinessId,
                    taskRow.PayloadJson,
                    taskRow.Status,
                    taskRow.NextRetryTime,
                    taskRow.Message,
                    taskRow.UpdatedTime
                })
                .Where(taskRow => taskRow.Id == existing.Id
                    && taskRow.Status == existingStatus
                    && taskRow.LastAttemptTime == existingLastAttemptTime);
            if (existingStatus != ProductionConstants.UploadStatuses.Uploading)
            {
                updateable = updateable.Where(taskRow =>
                    taskRow.Status != ProductionConstants.UploadStatuses.Uploading
                    && taskRow.Status != ProductionConstants.UploadStatuses.Uploaded);
            }

            _ = updateable.ExecuteCommand();
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
        string? alarmAddress = null,
        string? alarmContent = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = DeviceStatusReportRules.NormalizeMesDeviceStatusCode(deviceStatus);
        var normalizedStationNo = NormalizeStationNo(stationNo);
        BizDeviceStatusLog log;
        lock (_statusChangeLock)
        {
            if (!forceWrite)
            {
                // 关闭期间取消的普通状态不能越过最终停机；生命周期 0/1 使用 forceWrite 仍保证落盘。
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!forceWrite)
            {
                var latest = GetLatestStatus(normalizedStationNo);
                var existingBoundary = FindExistingProgramBoundaryLog(normalizedStationNo, normalizedStatus, weldTaskId);
                if (existingBoundary is not null)
                {
                    return existingBoundary;
                }

                var existingAlarm = FindExistingAlarmStatusLog(normalizedStationNo, normalizedStatus, alarmAddress);
                if (existingAlarm is not null)
                {
                    return existingAlarm;
                }

                if (ShouldReuseLatestProgramBoundaryStatus(latest, normalizedStatus, weldTaskId)
                    || DeviceStatusReportRules.ShouldSuppressDuplicateStatus(
                        latest,
                        normalizedStatus,
                        weldTaskId,
                        forceWrite,
                        alarmAddress))
                {
                    return latest!;
                }
            }

            log = CreateLog(
                normalizedStatus,
                remark,
                source,
                normalizedStationNo,
                weldTaskId,
                workOrderId,
                occurredTime,
                alarmAddress,
                alarmContent);
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
        await RetryPendingUploadsAsync(cancellationToken).ConfigureAwait(false);
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

        TaskCompletionSource<BasicRes<object>?>? starter = null;
        Task<BasicRes<object>?> activeUpload;
        lock (_uploadTaskLock)
        {
            if (_activeUploads.TryGetValue(normalizedRecordKey, out var existingUpload))
            {
                activeUpload = existingUpload;
            }
            else
            {
                starter = new TaskCompletionSource<BasicRes<object>?>(TaskCreationOptions.RunContinuationsAsynchronously);
                activeUpload = starter.Task;
                _activeUploads.Add(normalizedRecordKey, activeUpload);
            }
        }

        if (starter is not null)
        {
            _ = CompleteSharedUploadAsync(normalizedRecordKey, starter, cancellationToken);
        }

        return await activeUpload.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 按发生时间从旧到新补传全部 JSONL 待上传状态。
    /// 整批复用现有上传门禁，避免后续实时状态插入旧状态之间造成 MES 最终状态倒退。
    /// </summary>
    public async Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default)
    {
        await _uploadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 先取得顺序门禁，再把 JSONL 扫描和 MES 请求切到默认调度器，避免捕获已停止泵消息的 UI 上下文。
            _ = await Task.Run(
                    () => RetryPendingUploadsCoreAsync(cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _uploadGate.Release();
        }
    }

    private async Task CompleteSharedUploadAsync(
        string normalizedRecordKey,
        TaskCompletionSource<BasicRes<object>?> completion,
        CancellationToken cancellationToken)
    {
        try
        {
            completion.TrySetResult(
                await RetryUploadSerializedAsync(normalizedRecordKey, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completion.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
        finally
        {
            lock (_uploadTaskLock)
            {
                if (_activeUploads.TryGetValue(normalizedRecordKey, out var activeUpload)
                    && ReferenceEquals(activeUpload, completion.Task))
                {
                    _activeUploads.Remove(normalizedRecordKey);
                }
            }
        }
    }

    private async Task<BasicRes<object>?> RetryUploadSerializedAsync(
        string normalizedRecordKey,
        CancellationToken cancellationToken)
    {
        await _uploadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await RetryPendingUploadsCoreAsync(cancellationToken, normalizedRecordKey)
                .ConfigureAwait(false);
            return response ?? ResolveRetryResult(normalizedRecordKey);
        }
        finally
        {
            _uploadGate.Release();
        }
    }

    private async Task<BasicRes<object>?> RetryPendingUploadsCoreAsync(
        CancellationToken cancellationToken,
        string? requestedRecordKey = null)
    {
        BasicRes<object>? requestedResponse = null;
        var pendingLogs = GetPendingLogs()
            .OrderBy(log => log.OccurredTime)
            .ToList();
        foreach (var log in pendingLogs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
            if (recordKey is not null)
            {
                var response = await RetryUploadCoreAsync(recordKey, cancellationToken).ConfigureAwait(false);
                if (string.Equals(recordKey, requestedRecordKey, StringComparison.OrdinalIgnoreCase))
                {
                    requestedResponse = response;
                }

                if (response is not null && !response.IsSuccess && !IsSkippedResponse(response))
                {
                    break;
                }
            }
        }

        return requestedResponse;
    }

    private BasicRes<object>? ResolveRetryResult(string recordKey)
    {
        var log = GetLog(recordKey);
        if (log is null)
        {
            return null;
        }

        if (string.Equals(log.ReportStatus, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = log.ReportMessage ?? string.Empty,
                Data = new object()
            };
        }

        if (string.Equals(log.ReportStatus, ProductionConstants.UploadStatuses.Skipped, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicRes<object>
            {
                Status = ProductionConstants.UploadStatuses.Skipped,
                Msg = log.ReportMessage ?? string.Empty,
                Data = new object()
            };
        }

        return new BasicRes<object>
        {
            Status = string.Equals(
                log.ReportStatus,
                ProductionConstants.UploadStatuses.Pending,
                StringComparison.OrdinalIgnoreCase)
                    ? ProductionConstants.UploadStatuses.Pending
                    : AppConstants.MesStatus.Error,
            Msg = string.IsNullOrWhiteSpace(log.ReportMessage)
                ? "设备状态正在等待更早记录先完成上传。"
                : log.ReportMessage
        };
    }

    private async Task<BasicRes<object>?> RetryUploadCoreAsync(
        string normalizedRecordKey,
        CancellationToken cancellationToken)
    {
        var log = GetLog(normalizedRecordKey);
        if (log is null)
        {
            return null;
        }

        if (string.Equals(
                log.ReportStatus,
                ProductionConstants.UploadStatuses.Uploaded,
                StringComparison.OrdinalIgnoreCase))
        {
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = string.IsNullOrWhiteSpace(log.ReportMessage)
                    ? "Device status is already uploaded."
                    : log.ReportMessage,
                Data = new object()
            };
        }

        if (!DeviceStatusUploadVisibilityRules.ShouldInclude(log.ReportStatus))
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
            response = await SendToMesAsync(log, cancellationToken).ConfigureAwait(false);
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
                Remark = ResolveMesRemark(log)
            }, cancellationToken).ConfigureAwait(false);
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

    private static string ResolveMesRemark(BizDeviceStatusLog log)
    {
        if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Recovered)
        {
            // 旧恢复记录没有逐地址明细，补传时必须保留原 Remark 兼容历史数据。
            if (string.IsNullOrWhiteSpace(log.AlarmAddress))
            {
                return log.Remark ?? string.Empty;
            }

            return DeviceStatusReportRules.FormatRecoveryRemark(
                log.AlarmContent,
                log.StationNo,
                IsSharedAlarm(log));
        }

        if (log.DeviceStatus != ProductionConstants.MesDeviceStatuses.Exception)
        {
            return log.Remark ?? string.Empty;
        }

        // 新记录用 StationNo=0 表示共享；旧 JSONL 没有该约定时只兼容原有“双工位”标记。
        return DeviceStatusReportRules.FormatExceptionRemark(
            log.AlarmContent,
            log.StationNo,
            IsSharedAlarm(log));
    }

    private static bool IsSharedAlarm(BizDeviceStatusLog log)
        => log.StationNo <= ProductionConstants.Stations.SharedStationNo
            || log.Remark?.Contains("工位：双工位", StringComparison.Ordinal) == true;

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
            if (GetLog(recordKey) is null)
            {
                if (response.IsSuccess)
                {
                    TryCompleteUploadTaskProjection(log, recordKey);
                    WriteAppendFailure(log, "MES upload succeeded after the device status JSONL source was removed.");
                    return response;
                }

                return null;
            }

            log.ReportStatus = previousStatus;
            log.ReportTime = previousTime;
            log.ReportMessage = previousMessage;
            WriteAppendFailure(log, "MES result could not be appended to device status JSONL.");
            TryEnsurePendingUploadTask(log);
            RaiseLogsChanged();
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Error,
                Msg = "MES 响应已返回，但设备状态结果未能写入 JSONL，任务保持待重试。"
            };
        }

        if (response.IsSuccess || IsSkippedResponse(response))
        {
            TryCompleteUploadTaskProjection(log, recordKey);
        }
        else
        {
            TryEnsurePendingUploadTask(log);
        }

        RaiseLogsChanged();
        return response;
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
                        return recordKey is not null
                            && recordKeys.Contains(recordKey)
                            && !ShouldPreserveUploadingTask(task);
                    })
                    .ToList();
                foreach (var task in tasks)
                {
                    var existingStatus = task.Status;
                    var existingLastAttemptTime = task.LastAttemptTime;
                    task.IsDeleted = true;
                    task.DeletedTime = now;
                    task.UpdatedTime = now;
                    task.Message = "Device status JSONL source was deleted.";
                    _ = _dbContext.Db.Updateable(task)
                        .UpdateColumns(taskRow => new
                        {
                            taskRow.IsDeleted,
                            taskRow.DeletedTime,
                            taskRow.UpdatedTime,
                            taskRow.Message
                        })
                        .Where(taskRow => taskRow.Id == task.Id
                            && taskRow.Status == existingStatus
                            && taskRow.LastAttemptTime == existingLastAttemptTime)
                        .ExecuteCommand();
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
        DateTime? occurredTime,
        string? alarmAddress,
        string? alarmContent)
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
            AlarmAddress = NormalizeNullable(alarmAddress),
            AlarmContent = NormalizeNullable(alarmContent),
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

    private BizDeviceStatusLog? FindExistingAlarmStatusLog(
        int stationNo,
        string normalizedStatus,
        string? alarmAddress)
    {
        if (normalizedStatus is not (ProductionConstants.MesDeviceStatuses.Exception
                or ProductionConstants.MesDeviceStatuses.Recovered)
            || string.IsNullOrWhiteSpace(alarmAddress))
        {
            return null;
        }

        var normalizedAddress = AlarmAddressImportRules.NormalizeAddress(alarmAddress);
        foreach (var log in GetLogs(from: null, to: null, maxCount: 5000)
                     .Where(log => log.StationNo == stationNo)
                     .OrderByDescending(log => log.OccurredTime))
        {
            if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Recovered)
            {
                if (string.IsNullOrWhiteSpace(log.AlarmAddress)
                    && normalizedStatus == ProductionConstants.MesDeviceStatuses.Exception)
                {
                    return null;
                }
            }

            if (log.DeviceStatus is not (ProductionConstants.MesDeviceStatuses.Exception
                    or ProductionConstants.MesDeviceStatuses.Recovered)
                || !string.Equals(
                    AlarmAddressImportRules.NormalizeAddress(log.AlarmAddress),
                    normalizedAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return log.DeviceStatus == normalizedStatus ? log : null;
        }

        return null;
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

    private void TryCompleteUploadTaskProjection(BizDeviceStatusLog log, string recordKey)
    {
        try
        {
            lock (_dbLock)
            {
                _dbContext.InitDatabase();
                var task = FindExistingUploadTask(recordKey);
                if (task is null || task.IsDeleted)
                {
                    return;
                }

                var existingStatus = task.Status;
                var existingLastAttemptTime = task.LastAttemptTime;
                task.Status = log.ReportStatus;
                task.CompletedTime = log.ReportTime ?? DateTime.Now;
                task.NextRetryTime = null;
                task.Message = log.ReportMessage;
                task.UpdatedTime = DateTime.Now;
                _ = _dbContext.Db.Updateable(task)
                    .UpdateColumns(taskRow => new
                    {
                        taskRow.Status,
                        taskRow.CompletedTime,
                        taskRow.NextRetryTime,
                        taskRow.Message,
                        taskRow.UpdatedTime
                    })
                    .Where(taskRow => taskRow.Id == task.Id
                        && !taskRow.IsDeleted
                        && taskRow.Status == existingStatus
                        && taskRow.LastAttemptTime == existingLastAttemptTime)
                    .ExecuteCommand();
            }
        }
        catch
        {
            // JSONL 已保存 MES 终态；派生任务投影失败时由下次待上传页面对账修复。
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

    private bool IsUploadActive(string recordKey)
    {
        lock (_uploadTaskLock)
        {
            return _activeUploads.ContainsKey(recordKey);
        }
    }

    private bool IsRecentUploadAttempt(BizUploadTask task)
    {
        if (task.LastAttemptTime is null)
        {
            return false;
        }

        // 覆盖 MarkUploading 到进入上传门禁的交接窗口，超时后允许恢复上次进程遗留任务。
        var timeoutSeconds = Math.Max(3, CurrentSettings.MesTimeoutSeconds) + 5;
        return DateTime.Now - task.LastAttemptTime.Value <= TimeSpan.FromSeconds(timeoutSeconds);
    }

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
