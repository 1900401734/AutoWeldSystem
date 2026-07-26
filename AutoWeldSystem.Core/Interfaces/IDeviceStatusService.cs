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

    bool ShouldPreserveUploadingTask(BizUploadTask task);

    string GetLogDirectory();

    int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs);

    Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default);

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
        string? alarmAddress = null,
        string? alarmContent = null,
        CancellationToken cancellationToken = default);
}
