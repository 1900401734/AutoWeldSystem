using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 设备状态服务。
/// 统一维护当前设备状态、本地设备状态日志和 MES 设备状态上报。
/// </summary>
public interface IDeviceStatusService
{
    /// <summary>
    /// 设备状态变化事件，供 MonitorView 或后续轻量服务器状态缓存使用。
    /// </summary>
    event EventHandler<BizDeviceStatusLog>? StatusChanged;

    /// <summary>
    /// Raised after device-status log records are deleted or otherwise invalidated.
    /// </summary>
    event EventHandler? LogsChanged;

    /// <summary>
    /// Notifies consumers that persisted device-status log content was refreshed.
    /// </summary>
    void NotifyLogsChanged();

    /// <summary>
    /// 获取当前设备状态。没有历史记录时返回一个未保存的默认状态。
    /// </summary>
    BizDeviceStatusLog GetCurrentStatus();

    /// <summary>
    /// 查询设备状态日志。
    /// </summary>
    IReadOnlyList<BizDeviceStatusLog> GetLogs(DateTime? from = null, DateTime? to = null, int maxCount = 200);

    /// <summary>
    /// Ensures a pending or failed device-status log has a matching upload task.
    /// </summary>
    BizUploadTask EnsurePendingUploadTask(BizDeviceStatusLog log);

    /// <summary>
    /// 获取设备状态日志本地 JSONL 文件目录。
    /// </summary>
    string GetLogDirectory();

    /// <summary>
    /// Permanently removes selected device-status logs and their local copies.
    /// </summary>
    int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs);

    /// <summary>
    /// 切换设备状态，写入本地日志，并按需上报 MES。
    /// </summary>
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
