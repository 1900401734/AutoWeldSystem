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
/// 每次交互（含失败与取消）都通过 <see cref="ICenterInteractionLogService"/> 记录一条本地日志。
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

    public CenterTelemetryClient(HttpClient httpClient, ICenterInteractionLogService interactionLogService)
    {
        _httpClient = httpClient;
        _interactionLogService = interactionLogService;
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
    /// 统一上传路径：序列化、发送、解析应答，并在 finally 中记录交互日志。
    /// 异常（含取消）只记录不吞，保持由调用方处理重试与状态发布的既有契约。
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

        try
        {
            // 地址构建放在 try 内：配置错误导致的失败同样要留下交互日志。
            var baseUrl = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
            var uri = new Uri(new Uri(baseUrl), relativePath);
            url = uri.ToString();
            requestBody = JsonSerializer.Serialize(request, WireJsonOptions);

            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(uri, content, cancellationToken);

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
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
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
}
