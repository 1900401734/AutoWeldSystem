using System.Globalization;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 设备状态服务。
/// 状态码统一采用 PLC 原始状态：1=运行、2=暂停/空闲、3=停止、4=报警。
/// </summary>
public class DeviceStatusService : IDeviceStatusService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly object _dbLock = new();
    private AppSettings _currentSettings;

    public DeviceStatusService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        IUploadTaskService uploadTaskService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _mesProvider = mesProvider;
        _uploadTaskService = uploadTaskService;
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

    public async Task<BizDeviceStatusLog> ChangeStatusAsync(
        string deviceStatus,
        string? remark = null,
        string source = "Software",
        bool reportToMes = true,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        int? weldTaskId = null,
        string? workOrderId = null,
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

            // 状态码没有变化时，不重复落库、上传或进入重试队列。
            if (latest is not null
                && string.Equals(latest.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return latest;
            }

            log = CreateLog(normalizedStatus, remark, source, normalizedStationNo, weldTaskId, workOrderId);
            log = _dbContext.Db.Insertable(log).ExecuteReturnEntity();
        }

        if (reportToMes)
        {
            log = await ReportStatusAsync(log, cancellationToken);
        }
        else
        {
            EnqueueDeviceStatusUpload(log);
        }

        StatusChanged?.Invoke(this, log);
        return log;
    }

    private async Task<BizDeviceStatusLog> ReportStatusAsync(BizDeviceStatusLog log, CancellationToken cancellationToken)
    {
        var response = await _mesProvider.ReportDeviceStatusAsync(new ReportDeviceStatusReq
        {
            DeviceId = log.DeviceId,
            DevStatus = log.DeviceStatus,
            Ts = log.OccurredTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Remark = log.Remark ?? string.Empty
        }, cancellationToken);

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

            var storedLog = _dbContext.Db.Queryable<BizDeviceStatusLog>().InSingle(log.Id) ?? log;
            if (!response.IsSuccess)
            {
                EnqueueDeviceStatusUpload(storedLog);
            }

            return storedLog;
        }
    }

    private void EnqueueDeviceStatusUpload(BizDeviceStatusLog log)
    {
        _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
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
        });
    }

    private BizDeviceStatusLog CreateLog(
        string deviceStatus,
        string? remark,
        string source,
        int stationNo,
        int? weldTaskId,
        string? workOrderId)
    {
        var settings = CurrentSettings;
        return new BizDeviceStatusLog
        {
            DeviceId = settings.DeviceId,
            StationNo = stationNo,
            WeldTaskId = weldTaskId,
            WorkOrderId = NormalizeNullable(workOrderId),
            DeviceStatus = deviceStatus,
            StatusName = GetStatusName(deviceStatus),
            Source = string.IsNullOrWhiteSpace(source) ? "Software" : source.Trim(),
            Remark = NormalizeNullable(remark),
            OccurredTime = DateTime.Now,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
    }

    private BizDeviceStatusLog BuildDefaultStatus()
    {
        var settings = CurrentSettings;
        var defaultStatus = ProductionConstants.PlcDeviceStatuses.Paused.ToString(CultureInfo.InvariantCulture);
        return new BizDeviceStatusLog
        {
            DeviceId = settings.DeviceId,
            StationNo = ProductionConstants.Stations.DefaultStationNo,
            DeviceStatus = defaultStatus,
            StatusName = GetStatusName(defaultStatus),
            Source = "Software",
            Remark = "No device status log yet.",
            OccurredTime = DateTime.Now,
            ReportStatus = ProductionConstants.UploadStatuses.Skipped
        };
    }

    private static string NormalizeStatus(string deviceStatus)
    {
        var normalized = deviceStatus.Trim();
        return normalized switch
        {
            "1" or "2" or "3" or "4" => normalized,
            _ => throw new InvalidOperationException($"Unsupported PLC device status code: {deviceStatus}")
        };
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private static string GetStatusName(string deviceStatus)
    {
        return deviceStatus switch
        {
            "1" => "运行",
            "2" => "暂停/空闲",
            "3" => "停止",
            "4" => "报警",
            _ => "未知"
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }
}
