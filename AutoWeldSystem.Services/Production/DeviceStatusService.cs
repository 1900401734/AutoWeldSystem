using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services.Log;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Device status service.
/// Status codes stored here use MES device-status values, not PLC raw running states.
/// </summary>
public class DeviceStatusService : IDeviceStatusService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly object _dbLock = new();
    private AppSettings _currentSettings;

    public DeviceStatusService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IMesProvider mesProvider)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _mesProvider = mesProvider;
    }

    public event EventHandler<BizDeviceStatusLog>? StatusChanged;

    public BizDeviceStatusLog GetCurrentStatus()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var latest = _dbContext.Db.Queryable<BizDeviceStatusLog>()
                .OrderByDescending(it => it.OccurredTime)
                .First();

            return latest ?? BuildDefaultStatus();
        }
    }

    public IReadOnlyList<BizDeviceStatusLog> GetLogs(DateTime? from = null, DateTime? to = null, int maxCount = 200)
    {
        var localLogs = DeviceStatusLocalLogStore.Read(CurrentSettings, from, to, maxCount);
        if (localLogs.Count > 0)
        {
            return localLogs;
        }

        // 旧版本只写数据库。没有本地 JSONL 文件时继续回退数据库，避免升级后旧日志不可见。
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var query = _dbContext.Db.Queryable<BizDeviceStatusLog>();

            if (from is not null)
            {
                query = query.Where(it => it.OccurredTime >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(it => it.OccurredTime <= to.Value);
            }

            return query
                .OrderByDescending(it => it.OccurredTime)
                .Take(Math.Clamp(maxCount, 1, 5000))
                .ToList();
        }
    }

    public string GetLogDirectory()
    {
        return DeviceStatusLocalLogStore.GetLogDirectory(CurrentSettings);
    }

    /// <summary>
    /// 永久删除设备状态日志，并取消关联的 MES 上传任务，避免已删除记录被后台补传。
    /// </summary>
    public int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs)
    {
        var selectedLogs = logs
            .Where(log => log.Id > 0)
            .GroupBy(log => log.Id)
            .Select(group => group.First())
            .ToList();
        if (selectedLogs.Count == 0)
        {
            return 0;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var logIds = selectedLogs.Select(log => log.Id).ToArray();
            var businessIds = logIds.Select(logId => $"device-status:{logId}").ToArray();
            var now = DateTime.Now;
            var transaction = _dbContext.Db.Ado.UseTran(() =>
            {
                var uploadTasks = _dbContext.Db.Queryable<BizUploadTask>()
                    .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
                        && businessIds.Contains(task.BusinessId!))
                    .ToList();
                foreach (var uploadTask in uploadTasks.Where(task => !task.IsDeleted))
                {
                    uploadTask.IsDeleted = true;
                    uploadTask.DeletedTime = now;
                    uploadTask.UpdatedTime = now;
                    uploadTask.Message = "Deleted with device status log.";
                }

                if (uploadTasks.Count > 0)
                {
                    _dbContext.Db.Updateable(uploadTasks).ExecuteCommand();
                }

                _dbContext.Db.Deleteable<BizDeviceStatusLog>()
                    .Where(log => logIds.Contains(log.Id))
                    .ExecuteCommand();

                if (!DeviceStatusLocalLogStore.TryRemove(selectedLogs, CurrentSettings))
                {
                    throw new InvalidOperationException("无法删除设备状态本地日志。");
                }
            });

            if (!transaction.IsSuccess)
            {
                throw transaction.ErrorException ?? new InvalidOperationException("删除设备状态日志失败。");
            }

            return selectedLogs.Count;
        }
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
        var normalizedStatus = NormalizeStatus(deviceStatus);
        var normalizedStationNo = NormalizeStationNo(stationNo);
        BizDeviceStatusLog log;

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var latest = _dbContext.Db.Queryable<BizDeviceStatusLog>()
                .Where(it => it.StationNo == normalizedStationNo)
                .OrderByDescending(it => it.OccurredTime)
                .First();

            if (!forceWrite)
            {
                var existingProgramBoundaryLog = FindExistingProgramBoundaryLog(normalizedStationNo, normalizedStatus, weldTaskId);
                if (existingProgramBoundaryLog is not null)
                {
                    return existingProgramBoundaryLog;
                }

                if (ShouldReuseLatestProgramBoundaryStatus(latest, normalizedStatus, weldTaskId))
                {
                    return latest!;
                }

                // 普通状态码没有变化时，不重复落库、上传或进入重试队列。
                if (DeviceStatusReportRules.ShouldSuppressDuplicateStatus(latest, normalizedStatus, weldTaskId, forceWrite))
                {
                    return latest!;
                }
            }

            log = CreateLog(normalizedStatus, remark, source, normalizedStationNo, weldTaskId, workOrderId, occurredTime);
            log = _dbContext.Db.Insertable(log).ExecuteReturnEntity();
        }

        if (CurrentSettings.EnableDeviceStatusReport == false)
        {
            log = MarkSkipped(log, "Device status report is disabled in system settings.");
            WriteLocalStatusLog(log);
            StatusChanged?.Invoke(this, log);
            return log;
        }

        if (reportToMes && reportInBackground)
        {
            // 关机路径不能等待网络请求；先保留本地证据，再后台做一次 MES 尝试。
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
    }

    private async Task<BizDeviceStatusLog> ReportStatusAsync(BizDeviceStatusLog log, CancellationToken cancellationToken)
    {
        BasicRes<object> response;
        try
        {
            response = await _mesProvider.ReportDeviceStatusAsync(new ReportDeviceStatusReq
            {
                DeviceId = DeviceStatusReportRules.ResolveReportDeviceId(_settingsService.Get().DeviceId, log.DeviceId),
                DevStatus = log.DeviceStatus,
                Ts = log.OccurredTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Remark = log.Remark ?? string.Empty
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            response = new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Error,
                Msg = ex.Message
            };
        }

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
    }

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
            TryEnqueueDeviceStatusUpload(log);
            WriteLocalStatusLog(log);
            StatusChanged?.Invoke(this, log);
        }
    }

    private void TryEnqueueDeviceStatusUpload(BizDeviceStatusLog log)
    {
        try
        {
            EnqueueDeviceStatusUpload(log);
        }
        catch
        {
            // 后台关机上报不能抛出异常；本地 failed 状态仍会写入，便于现场排查。
        }
    }

    private void EnqueueDeviceStatusUpload(BizDeviceStatusLog log)
    {
        var task = new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = $"device-status:{log.Id}",
            PayloadJson = JsonSerializer.Serialize(new
            {
                LogId = log.Id,
                log.DeviceId,
                log.StationNo,
                log.WeldTaskId,
                log.WorkOrderId,
                DevStatus = log.DeviceStatus,
                Ts = log.OccurredTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Remark = log.Remark ?? string.Empty
            }),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "Device status is queued for MES retry."
        };

        NormalizeUploadTask(task);
        var existing = FindExistingUploadTask(task);
        if (existing is null)
        {
            task.CreatedTime = DateTime.Now;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Insertable(task).ExecuteCommand();
            return;
        }

        if (existing.IsDeleted || existing.Status == ProductionConstants.UploadStatuses.Uploaded)
        {
            return;
        }

        existing.PayloadJson = task.PayloadJson;
        existing.Status = task.Status;
        existing.NextRetryTime = task.NextRetryTime;
        existing.Message = task.Message;
        existing.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(existing).ExecuteCommand();
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

    private BizDeviceStatusLog BuildDefaultStatus()
    {
        var settings = CurrentSettings;
        var defaultStatus = ProductionConstants.MesDeviceStatuses.PoweredOn;
        return new BizDeviceStatusLog
        {
            DeviceId = settings.DeviceId,
            StationNo = ProductionConstants.Stations.DefaultStationNo,
            DeviceStatus = defaultStatus,
            StatusName = DeviceStatusReportRules.GetStatusName(defaultStatus),
            Source = "Software",
            Remark = "No device status log yet.",
            OccurredTime = DateTime.Now,
            ReportStatus = ProductionConstants.UploadStatuses.Skipped
        };
    }

    private void WriteLocalStatusLog(BizDeviceStatusLog log)
    {
        // 本地文件只是现场排查证据，写入失败不能影响状态切换、MES 上报或界面实时刷新。
        _ = DeviceStatusLocalLogStore.TryAppend(log, CurrentSettings);
    }

    private static string NormalizeStatus(string deviceStatus)
    {
        return DeviceStatusReportRules.NormalizeMesDeviceStatusCode(deviceStatus);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private BizDeviceStatusLog MarkSkipped(BizDeviceStatusLog log, string message)
    {
        lock (_dbLock)
        {
            log.ReportStatus = ProductionConstants.UploadStatuses.Skipped;
            log.ReportTime = DateTime.Now;
            log.ReportMessage = message;
            _dbContext.Db.Updateable(log)
                .UpdateColumns(it => new { it.ReportStatus, it.ReportTime, it.ReportMessage })
                .Where(it => it.Id == log.Id)
                .ExecuteCommand();

            return log;
        }
    }

    private BizUploadTask? FindExistingUploadTask(BizUploadTask task)
    {
        return _dbContext.Db.Queryable<BizUploadTask>()
            .First(existing => existing.TaskType == task.TaskType
                && existing.Target == task.Target
                && existing.BusinessId == task.BusinessId);
    }

    private static void NormalizeUploadTask(BizUploadTask task)
    {
        task.Target = string.IsNullOrWhiteSpace(task.Target)
            ? ProductionConstants.UploadTargets.Mes
            : task.Target.Trim();
        task.BusinessId = string.IsNullOrWhiteSpace(task.BusinessId)
            ? throw new InvalidOperationException("Device status upload task business id cannot be empty.")
            : task.BusinessId.Trim();
    }

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

    private BizDeviceStatusLog? FindExistingProgramBoundaryLog(
        int stationNo,
        string normalizedStatus,
        int? weldTaskId)
    {
        if (weldTaskId is null)
        {
            return null;
        }

        var isProgramBoundaryStatus = normalizedStatus is ProductionConstants.MesDeviceStatuses.ProgramStarted
            or ProductionConstants.MesDeviceStatuses.ProgramEnded;
        if (!isProgramBoundaryStatus)
        {
            return null;
        }

        return _dbContext.Db.Queryable<BizDeviceStatusLog>()
            .Where(it => it.StationNo == stationNo
                && it.WeldTaskId == weldTaskId
                && it.DeviceStatus == normalizedStatus)
            .OrderByDescending(it => it.OccurredTime)
            .First();
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.SharedStationNo
            : stationNo;
    }
}
