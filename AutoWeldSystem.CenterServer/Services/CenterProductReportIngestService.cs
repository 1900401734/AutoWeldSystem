using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 接收设备端产品/完工请求，协调文件存储、看板数据库快照和实时通知。
/// XLSX 的路径、状态合并和原子保存由 <see cref="CenterProductReportFileStore"/> 单独负责。
/// </summary>
public sealed class CenterProductReportIngestService
{
    private readonly CenterServerSettingsService _settingsService;
    private readonly CenterProductReportFileStore _fileStore;
    private readonly ICenterProductReportIngestSideEffects _sideEffects;

    public CenterProductReportIngestService(
        CenterServerSettingsService settingsService,
        CenterProductReportFileStore fileStore,
        ICenterProductReportIngestSideEffects sideEffects)
    {
        _settingsService = settingsService;
        _fileStore = fileStore;
        _sideEffects = sideEffects;
    }

    /// <summary>
    /// 保存一个完成产品，或只推进同一工单的完工任务状态。
    /// </summary>
    public async Task<CenterTelemetryAck> IngestAsync(
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceId = request.DeviceId.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Fail("DeviceId is required.");
        }

        if (request.StationNo <= 0)
        {
            return Fail("StationNo is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkOrder))
        {
            return Fail("WorkOrder is required.");
        }

        if (request.IsTaskFinishUpdate && request.EndTime is null)
        {
            return Fail("EndTime is required for task finish updates.");
        }

        if (!request.IsTaskFinishUpdate && request.Points.Count == 0)
        {
            return Fail("Product report points are required.");
        }

        var settings = _settingsService.Get();
        var reportPath = _fileStore.Upsert(settings.DataDirectory, request);
        await _sideEffects.ApplyAsync(settings.DataDirectory, deviceId, request, cancellationToken);

        return new CenterTelemetryAck
        {
            Success = true,
            Message = $"Accepted, report={reportPath}",
            ServerTime = DateTime.Now
        };
    }

    private static CenterTelemetryAck Fail(string message)
    {
        return new CenterTelemetryAck
        {
            Success = false,
            Message = message,
            ServerTime = DateTime.Now
        };
    }

}
