using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 设备状态服务实现。
/// 所有设备状态变化都先落本地日志，再尝试上报 MES，保证现场可追溯。
/// </summary>
public class DeviceStatusService : IDeviceStatusService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly object _dbLock = new();

    public DeviceStatusService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IMesProvider mesProvider)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
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
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatus(deviceStatus);
        var log = CreateLog(normalizedStatus, remark, source);

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            log = _dbContext.Db.Insertable(log).ExecuteReturnEntity();
        }

        if (reportToMes)
        {
            log = await ReportStatusAsync(log, cancellationToken);
        }

        StatusChanged?.Invoke(this, log);
        return log;
    }

    private async Task<BizDeviceStatusLog> ReportStatusAsync(BizDeviceStatusLog log, CancellationToken cancellationToken)
    {
        var response = await _mesProvider.ReportDeviceStatusAsync(new ReportDeviceStatusRequest
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

            return _dbContext.Db.Queryable<BizDeviceStatusLog>().InSingle(log.Id) ?? log;
        }
    }

    private BizDeviceStatusLog CreateLog(string deviceStatus, string? remark, string source)
    {
        var settings = _settingsService.Get();
        return new BizDeviceStatusLog
        {
            DeviceId = settings.DeviceId,
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
        var settings = _settingsService.Get();
        return new BizDeviceStatusLog
        {
            DeviceId = settings.DeviceId,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
            StatusName = GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
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
            ProductionConstants.MesDeviceStatuses.Stopped
                or ProductionConstants.MesDeviceStatuses.PoweredOn
                or ProductionConstants.MesDeviceStatuses.Exception
                or ProductionConstants.MesDeviceStatuses.Recovered
                or ProductionConstants.MesDeviceStatuses.ProgramStarted
                or ProductionConstants.MesDeviceStatuses.ProgramEnded => normalized,
            _ => throw new InvalidOperationException($"不支持的设备状态编码：{deviceStatus}")
        };
    }

    private static string GetStatusName(string deviceStatus)
    {
        return deviceStatus switch
        {
            ProductionConstants.MesDeviceStatuses.Stopped => "停机",
            ProductionConstants.MesDeviceStatuses.PoweredOn => "开机",
            ProductionConstants.MesDeviceStatuses.Exception => "异常",
            ProductionConstants.MesDeviceStatuses.Recovered => "异常恢复",
            ProductionConstants.MesDeviceStatuses.ProgramStarted => "程序运行开始",
            ProductionConstants.MesDeviceStatuses.ProgramEnded => "程序运行结束",
            _ => "未知"
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
