using System.Net.Http.Json;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// HTTP client used by equipment software to upload snapshots to the center server.
/// </summary>
public sealed class CenterTelemetryClient
{
    private readonly HttpClient _httpClient;

    public CenterTelemetryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Uploads one telemetry snapshot using the center server URL stored in local settings.
    /// </summary>
    public async Task<CenterTelemetryAck> UploadAsync(
        AppSettings settings,
        CenterTelemetrySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(new Uri(baseUrl), "api/center/telemetry"),
            request,
            cancellationToken);

        var ack = await response.Content.ReadFromJsonAsync<CenterTelemetryAck>(cancellationToken: cancellationToken);
        if (ack is not null)
        {
            return ack;
        }

        return new CenterTelemetryAck
        {
            Success = response.IsSuccessStatusCode,
            Message = response.IsSuccessStatusCode ? "Accepted" : response.ReasonPhrase ?? "Center server error.",
            ServerTime = DateTime.Now
        };
    }

    /// <summary>
    /// Uploads one completed product report to the center server.
    /// </summary>
    public async Task<CenterTelemetryAck> UploadProductReportAsync(
        AppSettings settings,
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(new Uri(baseUrl), "api/center/product-report"),
            request,
            cancellationToken);

        var ack = await response.Content.ReadFromJsonAsync<CenterTelemetryAck>(cancellationToken: cancellationToken);
        if (ack is not null)
        {
            return ack;
        }

        return new CenterTelemetryAck
        {
            Success = response.IsSuccessStatusCode,
            Message = response.IsSuccessStatusCode ? "Accepted" : response.ReasonPhrase ?? "Center server error.",
            ServerTime = DateTime.Now
        };
    }
}
