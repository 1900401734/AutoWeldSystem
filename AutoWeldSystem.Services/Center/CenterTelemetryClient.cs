using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// HTTP client used by equipment software to upload snapshots to the center server.
/// 交互结果按中心服务器日志规则记录；连接类失败由共享门控聚合。
/// </summary>
public sealed class CenterTelemetryClient
{
    // 与 MesProvider 一致使用宽松转义：中文在交互日志中保持原样，便于阅读与关键字检索。
    private static readonly JsonSerializerOptions WireJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;
    private readonly ICenterInteractionLogService _interactionLogService;
    private readonly CenterServerAvailabilityLogGate _availabilityLogGate;

    public CenterTelemetryClient(
        HttpClient httpClient,
        ICenterInteractionLogService interactionLogService,
        CenterServerAvailabilityLogGate availabilityLogGate)
    {
        _httpClient = httpClient;
        _interactionLogService = interactionLogService;
        _availabilityLogGate = availabilityLogGate;
    }

    /// <summary>
    /// Uploads one telemetry snapshot using the center server URL stored in local settings.
    /// </summary>
    public Task<CenterTelemetryAck> UploadAsync(
        AppSettings settings,
        CenterTelemetrySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadCoreAsync(settings, AppConstants.CenterInteractionTypes.Telemetry, "api/center/telemetry", request, cancellationToken);
    }

    /// <summary>
    /// Uploads one lightweight keep-alive heartbeat (no station payload) to the center server.
    /// </summary>
    public Task<CenterTelemetryAck> UploadHeartbeatAsync(
        AppSettings settings,
        CenterTelemetrySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadCoreAsync(settings, AppConstants.CenterInteractionTypes.Heartbeat, "api/center/heartbeat", request, cancellationToken);
    }

    /// <summary>
    /// Uploads one completed product report to the center server.
    /// </summary>
    public Task<CenterTelemetryAck> UploadProductReportAsync(
        AppSettings settings,
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadCoreAsync(settings, AppConstants.CenterInteractionTypes.ProductReport, "api/center/product-report", request, cancellationToken);
    }

    /// <summary>
    /// 统一上传路径：中心交互失败由进程级门控聚合到服务器日志。
    /// 异常只记录不吞，保持由调用方处理重试与状态发布的既有契约。
    /// </summary>
    private async Task<CenterTelemetryAck> UploadCoreAsync<TRequest>(
        AppSettings settings,
        string interactionType,
        string relativePath,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var sendTime = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();
        var url = string.Empty;
        var requestBody = string.Empty;
        var responseBody = string.Empty;
        int? httpStatusCode = null;
        CenterTelemetryAck? ack = null;
        var errorMessage = string.Empty;
        var intentionalCancellation = false;
        var recovered = false;
        CenterServerAvailabilityLogGate.FailureLogDecision? failureDecision = null;

        try
        {
            // 地址构建放在 try 内：配置错误导致的失败同样要留下交互日志。
            var baseUrl = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
            var uri = new Uri(new Uri(baseUrl), relativePath);
            url = uri.ToString();
            requestBody = JsonSerializer.Serialize(request, WireJsonOptions);

            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(uri, content, cancellationToken);

            // 收到任意 HTTP 响应即证明连接已恢复；业务成功与否仍由应答内容判断。
            recovered = _availabilityLogGate.RegisterReachable();
            httpStatusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // 与原 ReadFromJsonAsync 同源解析：畸形响应体照旧抛出，由调用方按失败处理。
            ack = JsonSerializer.Deserialize<CenterTelemetryAck>(responseBody, WireJsonOptions);
            if (ack is not null)
            {
                return ack;
            }

            ack = new CenterTelemetryAck
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Accepted" : response.ReasonPhrase ?? "Center server error.",
                ServerTime = DateTime.Now
            };
            return ack;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            errorMessage = ex.Message;
            intentionalCancellation = true;
            throw;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            if (CenterServerAvailabilityLogGate.IsConnectivityFailure(ex, cancellationToken))
            {
                failureDecision = _availabilityLogGate.RegisterFailure(DateTime.Now);
            }

            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (!intentionalCancellation)
            {
                if (failureDecision is { ShouldWrite: true } decision)
                {
                    errorMessage = BuildConnectivityErrorMessage(errorMessage, decision);
                    WriteInteractionLog();
                }
                else if (failureDecision is null && ShouldWriteResponseLog(interactionType, ack?.Success == true, recovered))
                {
                    WriteInteractionLog();
                }
            }
        }

        void WriteInteractionLog()
        {
            _interactionLogService.Write(new CenterInteractionLogEntry
            {
                InteractionType = interactionType,
                Method = "POST",
                Url = url,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                HttpStatusCode = httpStatusCode,
                AckMessage = ack?.Message ?? string.Empty,
                ServerTime = ack?.ServerTime,
                IsSuccess = ack?.Success == true,
                ErrorMessage = errorMessage,
                SendTime = sendTime,
                ReceiveTime = DateTime.Now,
                DurationMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// 连续成功心跳不写日志；业务拒绝、非心跳交互以及断线后的首个恢复响应都保留。
    /// </summary>
    private static bool ShouldWriteResponseLog(string interactionType, bool isSuccess, bool recovered)
        => interactionType != AppConstants.CenterInteractionTypes.Heartbeat
            || !isSuccess
            || recovered;

    private static string BuildConnectivityErrorMessage(
        string errorMessage,
        CenterServerAvailabilityLogGate.FailureLogDecision decision)
    {
        if (decision.IsFirstFailure)
        {
            return errorMessage;
        }

        return $"中心服务器持续不可达 {decision.OutageDuration.TotalMinutes:0} 分钟，累计失败 {decision.FailureCount} 次。最近错误：{errorMessage}";
    }

}
