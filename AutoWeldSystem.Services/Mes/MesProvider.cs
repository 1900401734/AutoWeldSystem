using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Core.Runtime;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoWeldSystem.Services.Mes;

public class MesProvider : IMesProvider, IDisposable
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
    private AppSettings _currentSettings;

    public MesProvider(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILocalizationService localizer,
        IMesInteractionLogService mesLogService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _localizer = localizer;
        _mesLogService = mesLogService;
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    /// <summary>
    /// 获取员工信息。
    /// </summary>
    public Task<BasicRes<UserInfoRes>> GetUserInfoAsync(string numberOrName, CancellationToken cancellationToken = default)
        => GetAsync<UserInfoRes>(AppConstants.MesLogPurposes.GetUserInfo, "api/User", new Dictionary<string, string?> { ["NumberOrName"] = numberOrName }, cancellationToken);

    /// <summary>
    /// 获取工单信息。
    /// </summary>
    public Task<BasicRes<WorkOrderRes>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default)
        => GetAsync<WorkOrderRes>(AppConstants.MesLogPurposes.GetWorkOrderInfo, "api/ItemsOfBatchTech", new Dictionary<string, string?> { ["WorkId"] = workId }, cancellationToken);

    /// <summary>
    /// 获取服务器时间。
    /// </summary>
    public Task<BasicRes<ServerTimeRes>> GetServerTimeAsync(CancellationToken cancellationToken = default)
        => GetAsync<ServerTimeRes>(AppConstants.MesLogPurposes.GetServerTime, "api/ServerTime", null, cancellationToken);

    /// <summary>
    /// 后台连接监控专用探测。
    /// 探测请求只服务于在线状态判断，不写入交互日志，避免日志界面被高频心跳刷新。
    /// </summary>
    public Task<BasicRes<ServerTimeRes>> ProbeServerTimeAsync(CancellationToken cancellationToken = default)
        => GetAsync<ServerTimeRes>(AppConstants.MesLogPurposes.GetServerTime, "api/ServerTime", null, cancellationToken, writeLog: false);

    /// <summary>
    /// 使用设置页里临时输入的地址测试 MES 连通性。
    /// 不直接写库，这样用户可以先测通再保存。
    /// </summary>
    public Task<BasicRes<ServerTimeRes>> TestConnectionAsync(string baseUrl, int timeoutSeconds, bool isWriteLog, CancellationToken cancellationToken = default)
        => GetAsync<ServerTimeRes>(AppConstants.MesLogPurposes.TestConnection, "api/ServerTime", null, cancellationToken, baseUrl, timeoutSeconds, isWriteLog);

    /// <summary>
    /// 获取程序列表。
    /// </summary>
    public Task<BasicRes<List<MesProgramListItemData>>> GetProgramListAsync(string deviceId, string? productNum, CancellationToken cancellationToken = default)
        => GetAsync<List<MesProgramListItemData>>(AppConstants.MesLogPurposes.GetProgramList, "api/ExpProgram", new Dictionary<string, string?> { ["deviceId"] = deviceId, ["productNum"] = productNum }, cancellationToken);

    /// <summary>
    /// 下载程序。
    /// </summary>
    public Task<BasicRes<ProgramDataRes>> DownloadProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
        => GetAsync<ProgramDataRes>(AppConstants.MesLogPurposes.DownloadProgram, "api/ExpProgram", new Dictionary<string, string?> { ["deviceId"] = deviceId, ["id"] = programId }, cancellationToken);

    /// <summary>
    /// 新增程序。
    /// </summary>
    public Task<BasicRes<ProgramDataRes>> AddExpProgramAsync(ProgramDataRes requestData, CancellationToken cancellationToken = default)
        => SendWithPayloadAsync<ProgramDataRes, ProgramDataRes>(
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
    public Task<BasicRes<ProgramDataRes>> UpdateExpProgramAsync(ProgramDataRes requestData, CancellationToken cancellationToken = default)
        => SendWithPayloadAsync<ProgramDataRes, ProgramDataRes>(
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
    public async Task<BasicRes<object>> DeleteExpProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?> { ["deviceId"] = deviceId, ["id"] = programId };
        var requestBody = FormatGetRequestBody(query);
        if (!TryBuildUri("api/ExpProgram", query, null, out var uri, out var errorMessage))
        {
            return CreateRequestBuildFailure<object>(
                AppConstants.MesLogPurposes.DeleteProgram,
                HttpMethod.Delete.Method,
                "api/ExpProgram",
                requestBody,
                errorMessage,
                true);
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        return await SendAsync<object>(
            AppConstants.MesLogPurposes.DeleteProgram,
            request,
            requestBody,
            cancellationToken,
            null,
            true);
    }

    /// <summary>
    /// 开工上报。
    /// </summary>
    public Task<BasicRes<ExperimentStartRes>> StartWorkAsync(ExperimentStartReq requestData, CancellationToken cancellationToken = default)
        => PostAsync<ExperimentStartReq, ExperimentStartRes>(AppConstants.MesLogPurposes.StartWork, "api/ExpStartV2", ApiCode.common_002, "ExpStartV2", requestData, cancellationToken);

    /// <summary>
    /// 变更工单状态。
    /// </summary>
    public Task<BasicRes<object>> ChangeWorkStatusAsync(ReportExperimentStatusReq requestData, CancellationToken cancellationToken = default)
        => PostAsync<ReportExperimentStatusReq, object>(AppConstants.MesLogPurposes.ChangeWorkStatus, "api/ExpStatus", ApiCode.common_005, "ExpStatus", requestData, cancellationToken);

    /// <summary>
    /// 完工上报。
    /// </summary>
    public Task<BasicRes<object>> EndWorkAsync(ExperimentEndReq requestData, CancellationToken cancellationToken = default)
        => PostAsync<ExperimentEndReq, object>(AppConstants.MesLogPurposes.EndWork, "api/ExpEnd", ApiCode.common_003, "ExpEnd", requestData, cancellationToken);

    /// <summary>
    /// 报告文件上报。
    /// MES 该接口使用 multipart/form-data，而不是统一的 ApiCode JSON 包装。
    /// </summary>
    public async Task<BasicRes<object>> UploadReportFileAsync(UploadReportFileReq requestData, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(requestData.FilePath))
        {
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Error,
                Msg = $"Report file does not exist: {requestData.FilePath}"
            };
        }

        var fileInfo = new FileInfo(requestData.FilePath);
        var requestLogBody = JsonSerializer.Serialize(new
        {
            requestData.ExpStartId,
            requestData.DeviceId,
            requestData.SN,
            requestData.ProcessNo,
            requestData.FileType,
            FileName = fileInfo.Name,
            FileLength = fileInfo.Length
        }, JsonOptions);

        if (!TryBuildUri("api/ExpFile", null, null, out var uri, out var errorMessage))
        {
            return CreateRequestBuildFailure<object>(
                AppConstants.MesLogPurposes.UploadReportFile,
                HttpMethod.Post.Method,
                "api/ExpFile",
                requestLogBody,
                errorMessage,
                true);
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

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = form
        };

        return await SendAsync<object>(
            AppConstants.MesLogPurposes.UploadReportFile,
            request,
            requestLogBody,
            cancellationToken,
            null,
            true);
    }

    /// <summary>
    /// 采集参数上传。
    /// MES 文档要求 Data 为焊点参数数组，路径固定为 /api/PostData。
    /// </summary>
    public Task<BasicRes<object>> UploadProcessParametersAsync(IReadOnlyList<ProcessParameterUploadItem> requestData, CancellationToken cancellationToken = default)
    {
        var settings = CurrentSettings;
        var apiName = string.IsNullOrWhiteSpace(settings.ProcessParameterApiName)
            ? "EMWeldDetail"
            : settings.ProcessParameterApiName.Trim();

        return PostAsync<IReadOnlyList<ProcessParameterUploadItem>, object>(
            AppConstants.MesLogPurposes.UploadProcessParameters,
            "api/PostData",
            settings.ProcessParameterApiCode,
            apiName,
            requestData,
            cancellationToken);
    }

    private async Task<BasicRes<T>> GetAsync<T>(string purpose, string apiCode, IDictionary<string, string?>? query,
        CancellationToken cancellationToken, string? baseUrlOverride = null, int? timeoutSecondsOverride = null, bool writeLog = true)
    {
        var requestBody = FormatGetRequestBody(query);
        if (!TryBuildUri(apiCode, query, baseUrlOverride, out var uri, out var errorMessage))
        {
            return CreateRequestBuildFailure<T>(purpose, HttpMethod.Get.Method, apiCode, requestBody, errorMessage, writeLog);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendAsync<T>(purpose, request, requestBody, cancellationToken, timeoutSecondsOverride, writeLog);
    }

    private async Task<BasicRes<TResponse>> PostAsync<TRequest, TResponse>(
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

    private async Task<BasicRes<TResponse>> SendWithPayloadAsync<TRequest, TResponse>(
        string purpose,
        HttpMethod method,
        string path,
        ApiCode apiCode,
        string apiName,
        TRequest requestData,
        CancellationToken cancellationToken)
    {
        var payload = new PostReq<TRequest>
        {
            ApiCode = apiCode,
            ApiName = apiName,
            Data = requestData
        };

        var requestBody = JsonSerializer.Serialize(payload, JsonOptions);
        if (!TryBuildUri(path, null, null, out var uri, out var errorMessage))
        {
            return CreateRequestBuildFailure<TResponse>(
                purpose,
                method.Method,
                path,
                requestBody,
                errorMessage,
                true);
        }

        using var request = new HttpRequestMessage(method, uri)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        return await SendAsync<TResponse>(purpose, request, requestBody, cancellationToken, null, true);
    }

    private async Task<BasicRes<T>> SendAsync<T>(
        string purpose,
        HttpRequestMessage request,
        string requestBody,
        CancellationToken cancellationToken,
        int? timeoutSecondsOverride,
        bool writeLog)
    {
        var sendTime = DateTime.Now;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeoutSeconds = Math.Max(3, timeoutSecondsOverride ?? CurrentSettings.MesTimeoutSeconds);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var method = request.Method.Method;
        var responseBody = string.Empty;
        int? httpStatusCode = null;
        var errorMessage = string.Empty;
        BasicRes<T>? result = null;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            httpStatusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                result = new BasicRes<T>
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
            result = new BasicRes<T>
            {
                Status = "E",
                Msg = errorMessage
            };

            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            result = new BasicRes<T>
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

    private static BasicRes<T> DeserializeResponse<T>(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var result = new BasicRes<T>
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

    /// <summary>
    /// 统一构建 MES 地址。
    /// 地址为空或格式不合法时只返回失败信息，不抛异常，避免启动阶段直接退出。
    /// </summary>
    private bool TryBuildUri(string path, IDictionary<string, string?>? query, string? baseUrlOverride, out Uri? uri, out string errorMessage)
    {
        var settings = CurrentSettings;
        var baseUrl = string.IsNullOrWhiteSpace(baseUrlOverride)
            ? settings.MesBaseUrl
            : baseUrlOverride.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            uri = null;
            errorMessage = "MES地址未配置，请在系统设置中填写MES服务器地址。";
            return false;
        }

        baseUrl = NormalizeBaseUrl(baseUrl);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || !IsHttpUri(baseUri))
        {
            uri = null;
            errorMessage = $"MES地址格式不正确：{baseUrl}，请填写以 http:// 或 https:// 开头的完整地址。";
            return false;
        }

        var relativePath = path.TrimStart('/');
        if (!Uri.TryCreate(baseUri, relativePath, out var requestUri))
        {
            uri = null;
            errorMessage = $"MES接口路径格式不正确：{path}";
            return false;
        }

        var builder = new UriBuilder(requestUri);
        if (query is not null && query.Count > 0)
        {
            builder.Query = string.Join("&", query
                .Where(it => !string.IsNullOrWhiteSpace(it.Value))
                .Select(it => $"{Uri.EscapeDataString(it.Key)}={Uri.EscapeDataString(it.Value!)}"));
        }

        uri = builder.Uri;
        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// 工控机现场经常只填写 IP:端口，这里默认按 HTTP 处理，降低配置门槛。
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl)
    {
        var normalized = baseUrl.Trim();
        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            normalized = $"http://{normalized}";
        }

        if (!normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return normalized;
    }

    private static bool IsHttpUri(Uri uri)
    {
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private BasicRes<T> CreateRequestBuildFailure<T>(
        string purpose,
        string method,
        string path,
        string requestBody,
        string errorMessage,
        bool writeLog)
    {
        var message = _localizer.GetString(TextKeys.Mes.RequestException, errorMessage);
        var result = new BasicRes<T>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = message
        };

        if (writeLog)
        {
            var now = DateTime.Now;
            _mesLogService.Write(new MesInteractionLogEntry
            {
                Purpose = purpose,
                Method = method,
                Url = path,
                RequestBody = requestBody,
                ResponseBody = string.Empty,
                HttpStatusCode = null,
                MesStatus = result.Status,
                MesMessage = result.Msg,
                IsSuccess = false,
                ErrorMessage = message,
                SendTime = now,
                ReceiveTime = now,
                DurationMilliseconds = 0
            });
        }

        return result;
    }

    /// <summary>
    /// 设置设备编号。
    /// </summary>
    public Task<BasicRes<object>> SetDeviceIdAsync(AddDeviceReq addDeviceRequest, CancellationToken cancellationToken = default)
        => PostAsync<AddDeviceReq, object>(
            AppConstants.MesLogPurposes.SetDeviceId,
            "api/Device",
            ApiCode.common_004,
            "AddDevice",
            addDeviceRequest,
            cancellationToken);

    /// <summary>
    /// 上报设备状态。
    /// </summary>
    public Task<BasicRes<object>> ReportDeviceStatusAsync(ReportDeviceStatusReq requestData, CancellationToken cancellationToken = default)
        => PostAsync<ReportDeviceStatusReq, object>(
            AppConstants.MesLogPurposes.ReportDeviceStatus,
            "api/DeviceStatusV2",
            ApiCode.common_001,
            "DeviceStatusV2",
            requestData,
            cancellationToken);

    public void Dispose()
    {
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }
}
