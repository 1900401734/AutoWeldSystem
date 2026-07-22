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
