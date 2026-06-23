using System.Text.Json;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// Queues completed products locally and forwards them to the center server on a background loop.
/// </summary>
public sealed class CenterProductForwardingService : ICenterProductForwardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly CenterTelemetryClient _client;
    private readonly object _dbLock = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private DateTime _lastFailureLogTime = DateTime.MinValue;

    public CenterProductForwardingService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IUploadTaskService uploadTaskService,
        IProductionFlowLogService productionLogService,
        IProgramExceptionLogService exceptionLogService,
        CenterTelemetryClient client)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _uploadTaskService = uploadTaskService;
        _productionLogService = productionLogService;
        _exceptionLogService = exceptionLogService;
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch
            {
                // Shutdown must not block the WinForms process from exiting.
            }
        }

        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void EnqueueCompletedProduct(
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync || records.Count == 0)
        {
            return;
        }

        var request = BuildRequest(settings, task, stationNo, records);
        var uploadTask = _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.CenterProductReport,
            Target = ProductionConstants.UploadTargets.CentralServer,
            BusinessId = BuildBusinessId(request),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "中心服务器产品数据转发已入队。"
        });

        _productionLogService.Write(
            "CenterProductForwardQueued",
            "中心服务器产品数据转发已入队",
            $"UploadTaskId={uploadTask.Id}, ProductNo={request.ProductNo}, PointCount={request.Points.Count}",
            stationNo: stationNo,
            workOrderId: task.SN,
            productNo: request.ProductNo,
            programId: task.ProgramId ?? string.Empty);
    }

    /// <summary>
    /// Processes pending center forwarding tasks without blocking product collection.
    /// </summary>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ExecutePendingAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                WriteFailureLog(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private async Task ExecutePendingAsync(CancellationToken cancellationToken)
    {
        var taskIds = GetPendingTaskIds();
        foreach (var taskId in taskIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = MarkUploading(taskId);
            if (task is null)
            {
                continue;
            }

            await ExecuteTaskAsync(task, cancellationToken);
        }
    }

    private IReadOnlyList<int> GetPendingTaskIds()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.CenterProductReport
                    && task.Target == ProductionConstants.UploadTargets.CentralServer
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded
                    && task.RetryCount < task.MaxRetryCount
                    && (task.NextRetryTime == null || task.NextRetryTime <= DateTime.Now))
                .OrderBy(task => task.CreatedTime)
                .Select(task => task.Id)
                .ToList();
        }
    }

    private BizUploadTask? MarkUploading(int taskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(taskId);
            if (task is null || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return null;
            }

            task.Status = ProductionConstants.UploadStatuses.Uploading;
            task.LastAttemptTime = DateTime.Now;
            task.RetryCount++;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            return task;
        }
    }

    private async Task ExecuteTaskAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync)
        {
            MarkRetry(task, "中心服务器同步未启用。");
            return;
        }

        var request = JsonSerializer.Deserialize<CenterProductReportRequest>(task.PayloadJson ?? string.Empty, JsonOptions);
        if (request is null)
        {
            MarkFailed(task, "中心服务器产品转发任务缺少请求内容。");
            return;
        }

        var response = await _client.UploadProductReportAsync(settings, request, cancellationToken);
        if (response.Success)
        {
            MarkUploaded(task, response.Message);
            _productionLogService.Write(
                "CenterProductForwardSucceeded",
                "中心服务器产品数据转发成功",
                $"UploadTaskId={task.Id}, ProductNo={request.ProductNo}, PointCount={request.Points.Count}",
                stationNo: request.StationNo,
                workOrderId: request.WorkOrder,
                productNo: request.ProductNo);
            return;
        }

        MarkRetry(task, response.Message);
        _productionLogService.Write(
            "CenterProductForwardFailed",
            "中心服务器产品数据转发失败，等待重试",
            $"UploadTaskId={task.Id}, ProductNo={request.ProductNo}, Error={response.Message}",
            "Warning",
            request.StationNo,
            request.WorkOrder,
            request.ProductNo);
    }

    private void MarkUploaded(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = ProductionConstants.UploadStatuses.Uploaded;
            task.CompletedTime = DateTime.Now;
            task.Message = string.IsNullOrWhiteSpace(message) ? "Center product report uploaded." : message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void MarkRetry(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = task.RetryCount >= task.MaxRetryCount
                ? ProductionConstants.UploadStatuses.Failed
                : ProductionConstants.UploadStatuses.Retrying;
            task.NextRetryTime = task.Status == ProductionConstants.UploadStatuses.Failed
                ? null
                : DateTime.Now.AddSeconds(Math.Min(120, 10 * Math.Max(1, task.RetryCount)));
            task.Message = message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void MarkFailed(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = ProductionConstants.UploadStatuses.Failed;
            task.Message = message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void WriteFailureLog(Exception ex)
    {
        if (DateTime.Now - _lastFailureLogTime < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastFailureLogTime = DateTime.Now;
        _exceptionLogService.Write(ex, "CenterProductForwardingService");
    }

    private static CenterProductReportRequest BuildRequest(
        AppSettings settings,
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var orderedRecords = records.OrderBy(record => record.SequenceNo).ThenBy(record => record.Id).ToList();
        var first = orderedRecords[0];
        return new CenterProductReportRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType),
            StationNo = stationNo,
            WorkOrder = task.SN ?? string.Empty,
            ProductJobNo = task.ProductNum ?? string.Empty,
            ProductNo = first.ProductNo,
            ProductModel = task.ProductModel ?? string.Empty,
            ProductResult = TestResultRules.ResolveProductResult(orderedRecords.Select(record => record.TestResult)),
            CompletedAt = orderedRecords.Max(record => record.Ts),
            Points = orderedRecords.Select(record => new CenterProductReportPointDto
            {
                SequenceNo = record.SequenceNo,
                TouchNo = record.TouchNo,
                TestResult = record.TestResult,
                CollectedAt = record.Ts,
                RawDataJson = record.RawDataJson ?? string.Empty
            }).ToList()
        };
    }

    private static string BuildBusinessId(CenterProductReportRequest request)
    {
        var raw = $"center:s{request.StationNo}:wo{request.WorkOrder}:p{request.ProductNo}";
        return raw.Length <= 100 ? raw : raw[..100];
    }
}
