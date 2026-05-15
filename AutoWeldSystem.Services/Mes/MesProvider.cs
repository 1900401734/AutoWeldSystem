using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;

namespace AutoWeldSystem.Services.Mes;

public class MesProvider : IMesProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly IAppSettingsService _settingsService;
    private readonly ILocalizationService _localizer;
    private readonly IMesInteractionLogService _mesLogService;

    public MesProvider(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILocalizationService localizer,
        IMesInteractionLogService mesLogService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _localizer = localizer;
        _mesLogService = mesLogService;
    }

    /// <summary>
    /// 获取员工信息。
    /// </summary>
    public Task<MesBaseResponse<MesUserInfoResponse>> GetUserInfoAsync(string numberOrName, CancellationToken cancellationToken = default)
        => GetAsync<MesUserInfoResponse>(AppConstants.MesLogPurposes.GetUserInfo, "api/User", new Dictionary<string, string?> { ["NumberOrName"] = numberOrName }, cancellationToken);

    /// <summary>
    /// 获取工单信息。
    /// </summary>
    public Task<MesBaseResponse<MesWorkOrderResponse>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default)
        => GetAsync<MesWorkOrderResponse>(AppConstants.MesLogPurposes.GetWorkOrderInfo, "api/ItemsOfBatchTech", new Dictionary<string, string?> { ["WorkId"] = workId }, cancellationToken);

    /// <summary>
    /// 获取服务器时间。
    /// </summary>
    public Task<MesBaseResponse<MesServerTimeResponse>> GetServerTimeAsync(CancellationToken cancellationToken = default)
        => GetAsync<MesServerTimeResponse>(AppConstants.MesLogPurposes.GetServerTime, "api/ServerTime", null, cancellationToken);

    /// <summary>
    /// 后台连接监控专用探测。
    /// 探测请求只服务于在线状态判断，不写入交互日志，避免日志界面被高频心跳刷新。
    /// </summary>
    public Task<MesBaseResponse<MesServerTimeResponse>> ProbeServerTimeAsync(CancellationToken cancellationToken = default)
        => GetAsync<MesServerTimeResponse>(AppConstants.MesLogPurposes.GetServerTime, "api/ServerTime", null, cancellationToken, writeLog: false);

    /// <summary>
    /// 使用设置页里临时输入的地址测试 MES 连通性。
    /// 不直接写库，这样用户可以先测通再保存。
    /// </summary>
    public Task<MesBaseResponse<MesServerTimeResponse>> TestConnectionAsync(string baseUrl, int timeoutSeconds, CancellationToken cancellationToken = default)
        => GetAsync<MesServerTimeResponse>(AppConstants.MesLogPurposes.TestConnection, "api/ServerTime", null, cancellationToken, baseUrl, timeoutSeconds);

    /// <summary>
    /// 获取程序列表。
    /// </summary>
    public Task<MesBaseResponse<List<MesProgramListItemData>>> GetProgramListAsync(string deviceId, string? productNum, CancellationToken cancellationToken = default)
        => GetAsync<List<MesProgramListItemData>>(AppConstants.MesLogPurposes.GetProgramList, "api/ExpProgram", new Dictionary<string, string?> { ["deviceId"] = deviceId, ["productNum"] = productNum }, cancellationToken);

    /// <summary>
    /// 下载程序。
    /// </summary>
    public Task<MesBaseResponse<MesProgramData>> DownloadProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
        => GetAsync<MesProgramData>(AppConstants.MesLogPurposes.DownloadProgram, "api/ExpProgram", new Dictionary<string, string?> { ["deviceId"] = deviceId, ["id"] = programId }, cancellationToken);

    /// <summary>
    /// 新增程序。
    /// </summary>
    public Task<MesBaseResponse<MesProgramData>> AddExpProgramAsync(MesProgramData requestData, CancellationToken cancellationToken = default)
        => SendWithPayloadAsync<MesProgramData, MesProgramData>(
            AppConstants.MesLogPurposes.AddProgram,
            HttpMethod.Post,
            "api/ExpProgram",
            ApiCode.common_006,
            "AddExpProgram",
            requestData,
            cancellationToken);

    /// <summary>
    /// 更新程序。
    /// </summary>
    public Task<MesBaseResponse<MesProgramData>> UpdateExpProgramAsync(MesProgramData requestData, CancellationToken cancellationToken = default)
        => SendWithPayloadAsync<MesProgramData, MesProgramData>(
            AppConstants.MesLogPurposes.UpdateProgram,
            HttpMethod.Put,
            "api/ExpProgram",
            ApiCode.common_007,
            "UpdateExpProgram",
            requestData,
            cancellationToken);

    /// <summary>
    /// 删除程序。
    /// </summary>
    public async Task<MesBaseResponse<object>> DeleteExpProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            BuildUri("api/ExpProgram", new Dictionary<string, string?> { ["deviceId"] = deviceId, ["id"] = programId }, null));

        return await SendAsync<object>(
            AppConstants.MesLogPurposes.DeleteProgram,
            request,
            FormatGetRequestBody(new Dictionary<string, string?> { ["deviceId"] = deviceId, ["id"] = programId }),
            cancellationToken,
            null,
            true);
    }

    /// <summary>
    /// 开工上报。
    /// </summary>
    public Task<MesBaseResponse<ExpStartResponse>> StartWorkAsync(ExpStartRequest requestData, CancellationToken cancellationToken = default)
        => PostAsync<ExpStartRequest, ExpStartResponse>(AppConstants.MesLogPurposes.StartWork, "api/ExpStartV2", ApiCode.common_002, "ExpStartV2", requestData, cancellationToken);

    /// <summary>
    /// 变更工单状态。
    /// </summary>
    public Task<MesBaseResponse<object>> ChangeWorkStatusAsync(ExpStatusRequest requestData, CancellationToken cancellationToken = default)
        => PostAsync<ExpStatusRequest, object>(AppConstants.MesLogPurposes.ChangeWorkStatus, "api/ExpStatus", ApiCode.common_005, "ExpStatus", requestData, cancellationToken);

    /// <summary>
    /// 完工上报。
    /// </summary>
    public Task<MesBaseResponse<object>> EndWorkAsync(ExpEndRequest requestData, CancellationToken cancellationToken = default)
        => PostAsync<ExpEndRequest, object>(AppConstants.MesLogPurposes.EndWork, "api/ExpEnd", ApiCode.common_003, "ExpEnd", requestData, cancellationToken);

    /// <summary>
    /// 报告文件上报。
    /// MES 该接口使用 multipart/form-data，而不是统一的 ApiCode JSON 包装。
    /// </summary>
    public async Task<MesBaseResponse<object>> UploadReportFileAsync(ReportFileUploadRequest requestData, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(requestData.FilePath))
        {
            return new MesBaseResponse<object>
            {
                Status = AppConstants.MesStatus.Error,
                Msg = $"Report file does not exist: {requestData.FilePath}"
            };
        }

        await using var fileStream = File.OpenRead(requestData.FilePath);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(requestData.ExpStartId), nameof(requestData.ExpStartId) },
            { new StringContent(requestData.DeviceId), nameof(requestData.DeviceId) },
            { new StringContent(requestData.SN), nameof(requestData.SN) },
            { new StringContent(requestData.ProcessNo), nameof(requestData.ProcessNo) },
            { new StringContent(requestData.FileType.ToString()), nameof(requestData.FileType) }
        };

        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(requestData.FilePath));

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("api/ExpFile", null, null))
        {
            Content = form
        };

        var requestLogBody = JsonSerializer.Serialize(new
        {
            requestData.ExpStartId,
            requestData.DeviceId,
            requestData.SN,
            requestData.ProcessNo,
            requestData.FileType,
            FileName = Path.GetFileName(requestData.FilePath),
            FileLength = fileStream.Length
        }, JsonOptions);

        return await SendAsync<object>(
            AppConstants.MesLogPurposes.UploadReportFile,
            request,
            requestLogBody,
            cancellationToken,
            null,
            true);
    }

    private async Task<MesBaseResponse<T>> GetAsync<T>(
        string purpose,
        string path,
        IDictionary<string, string?>? query,
        CancellationToken cancellationToken,
        string? baseUrlOverride = null,
        int? timeoutSecondsOverride = null,
        bool writeLog = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, query, baseUrlOverride));
        return await SendAsync<T>(purpose, request, FormatGetRequestBody(query), cancellationToken, timeoutSecondsOverride, writeLog);
    }

    private async Task<MesBaseResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string purpose,
        string path,
        ApiCode apiCode,
        string apiName,
        TRequest requestData,
        CancellationToken cancellationToken)
    {
        return await SendWithPayloadAsync<TRequest, TResponse>(
            purpose,
            HttpMethod.Post,
            path,
            apiCode,
            apiName,
            requestData,
            cancellationToken);
    }

    private async Task<MesBaseResponse<TResponse>> SendWithPayloadAsync<TRequest, TResponse>(
        string purpose,
        HttpMethod method,
        string path,
        ApiCode apiCode,
        string apiName,
        TRequest requestData,
        CancellationToken cancellationToken)
    {
        var payload = new MesPostRequest<TRequest>
        {
            ApiCode = apiCode,
            ApiName = apiName,
            Data = requestData
        };

        var requestBody = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(method, BuildUri(path, null, null))
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        return await SendAsync<TResponse>(purpose, request, requestBody, cancellationToken, null, true);
    }

    private async Task<MesBaseResponse<T>> SendAsync<T>(
        string purpose,
        HttpRequestMessage request,
        string requestBody,
        CancellationToken cancellationToken,
        int? timeoutSecondsOverride,
        bool writeLog)
    {
        var sendTime = DateTime.Now;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeoutSeconds = Math.Max(3, timeoutSecondsOverride ?? _settingsService.Get().MesTimeoutSeconds);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var method = request.Method.Method;
        var responseBody = string.Empty;
        int? httpStatusCode = null;
        var errorMessage = string.Empty;
        MesBaseResponse<T>? result = null;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            httpStatusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                result = new MesBaseResponse<T>
                {
                    Status = "E",
                    Msg = _localizer.GetString(TextKeys.Mes.HttpError, (int)response.StatusCode, responseBody)
                };

                return result;
            }

            result = DeserializeResponse<T>(responseBody);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            errorMessage = _localizer.GetString(TextKeys.Mes.Timeout, timeoutSeconds);
            result = new MesBaseResponse<T>
            {
                Status = "E",
                Msg = errorMessage
            };

            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            result = new MesBaseResponse<T>
            {
                Status = "E",
                Msg = _localizer.GetString(TextKeys.Mes.RequestException, ex.Message)
            };

            return result;
        }
        finally
        {
            stopwatch.Stop();

            if (writeLog)
            {
                _mesLogService.Write(new MesInteractionLogEntry
                {
                    Purpose = purpose,
                    Method = method,
                    Url = url,
                    RequestBody = requestBody,
                    ResponseBody = responseBody,
                    HttpStatusCode = httpStatusCode,
                    MesStatus = result?.Status ?? string.Empty,
                    MesMessage = result?.Msg ?? string.Empty,
                    IsSuccess = result?.IsSuccess == true,
                    ErrorMessage = errorMessage,
                    SendTime = sendTime,
                    ReceiveTime = DateTime.Now,
                    DurationMilliseconds = stopwatch.ElapsedMilliseconds
                });
            }
        }
    }

    private static string FormatGetRequestBody(IDictionary<string, string?>? query)
    {
        if (query is null || query.Count == 0)
        {
            return string.Empty;
        }

        var values = query
            .Where(it => !string.IsNullOrWhiteSpace(it.Value))
            .ToDictionary(it => it.Key, it => it.Value);

        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static MesBaseResponse<T> DeserializeResponse<T>(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var result = new MesBaseResponse<T>
        {
            Status = root.TryGetProperty("Status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
            Msg = root.TryGetProperty("Msg", out var msg) ? msg.GetString() ?? string.Empty : string.Empty
        };

        if (!root.TryGetProperty("Data", out var dataElement))
        {
            return result;
        }

        if (typeof(T) == typeof(object))
        {
            result.Data = (T)(object)new object();
            return result;
        }

        if (dataElement.ValueKind == JsonValueKind.Array && !IsListType(typeof(T)))
        {
            var first = dataElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
            {
                result.Data = JsonSerializer.Deserialize<T>(first.GetRawText(), JsonOptions);
            }

            return result;
        }

        result.Data = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), JsonOptions);
        return result;
    }

    private static bool IsListType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private Uri BuildUri(string path, IDictionary<string, string?>? query, string? baseUrlOverride)
    {
        var settings = _settingsService.Get();
        var baseUrl = string.IsNullOrWhiteSpace(baseUrlOverride)
            ? settings.MesBaseUrl
            : baseUrlOverride.Trim();
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }

        var builder = new UriBuilder(new Uri(new Uri(baseUrl), path));
        if (query is null || query.Count == 0)
        {
            return builder.Uri;
        }

        builder.Query = string.Join("&", query
            .Where(it => !string.IsNullOrWhiteSpace(it.Value))
            .Select(it => $"{Uri.EscapeDataString(it.Key)}={Uri.EscapeDataString(it.Value!)}"));

        return builder.Uri;
    }

    /// <summary>
    /// 设置设备编号。
    /// </summary>
    public Task<MesBaseResponse<object>> SetDeviceIdAsync(AddDeviceRequest addDeviceRequest, CancellationToken cancellationToken = default)
        => PostAsync<AddDeviceRequest, object>(
            AppConstants.MesLogPurposes.SetDeviceId,
            "api/Device",
            ApiCode.common_004,
            "AddDevice",
            addDeviceRequest,
            cancellationToken);

    /// <summary>
    /// 上报设备状态。
    /// </summary>
    public Task<MesBaseResponse<object>> ReportDeviceStatusAsync(ReportDeviceStatusRequest requestData, CancellationToken cancellationToken = default)
        => PostAsync<ReportDeviceStatusRequest, object>(
            AppConstants.MesLogPurposes.ReportDeviceStatus,
            "api/DeviceStatusV2",
            ApiCode.common_001,
            "DeviceStatusV2",
            requestData,
            cancellationToken);
}
