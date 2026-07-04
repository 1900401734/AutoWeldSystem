namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 设备端远程接口的纯规则。
/// URL 拼接和设备编号匹配放在这里，便于 UI、MES 同步和测试复用。
/// </summary>
public static class DeviceApiEndpointRules
{
    public const string DefaultBaseUrl = "http://127.0.0.1:7098/";

    private const string StatusPath = "api/DeviceStatus";

    /// <summary>
    /// 统一设备端 API 基地址格式，确保末尾带斜杠。
    /// </summary>
    public static string NormalizeBaseUrl(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultBaseUrl;
        }

        return normalized.EndsWith("/", StringComparison.Ordinal)
            ? normalized
            : $"{normalized}/";
    }

    /// <summary>
    /// 构建设备状态查询完整地址，用于同步给 MES 或返回给平台。
    /// </summary>
    public static string BuildDeviceStatusUrl(string? baseUrl, string? deviceId)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var normalizedDeviceId = NormalizeText(deviceId);
        return $"{normalizedBaseUrl}{StatusPath}?DeviceId={Uri.EscapeDataString(normalizedDeviceId)}";
    }

    /// <summary>
    /// 从平台下发的设备状态完整地址中反解设备端 API 基地址。
    /// </summary>
    public static bool TryExtractBaseUrlFromStatusUrl(string? devStatusUrl, out string baseUrl)
    {
        baseUrl = string.Empty;
        var normalized = devStatusUrl?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || !Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var marker = $"/{StatusPath}";
        var markerIndex = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var basePath = uri.AbsolutePath[..markerIndex].Trim('/');
        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port)
        {
            Path = basePath,
            Query = string.Empty,
            Fragment = string.Empty
        };

        baseUrl = NormalizeBaseUrl(builder.Uri.AbsoluteUri);
        return true;
    }

    /// <summary>
    /// 判断远程请求中的设备编号是否允许访问当前设备。
    /// 空查询参数表示查询当前设备。
    /// </summary>
    public static bool IsRequestedDeviceIdAllowed(string? requestedDeviceId, string? currentDeviceId)
    {
        var requested = NormalizeText(requestedDeviceId);
        if (string.IsNullOrWhiteSpace(requested))
        {
            return true;
        }

        return SameDeviceId(requested, currentDeviceId);
    }

    /// <summary>
    /// 判断 OldDeviceId 是否命中当前设备编号或最近一次已同步设备编号。
    /// </summary>
    public static bool IsKnownOldDeviceId(
        string? oldDeviceId,
        string? currentDeviceId,
        string? syncedDeviceId)
    {
        var oldId = NormalizeText(oldDeviceId);
        return string.IsNullOrWhiteSpace(oldId)
            || SameDeviceId(oldId, currentDeviceId)
            || SameDeviceId(oldId, syncedDeviceId);
    }

    /// <summary>
    /// 统一空值和首尾空格处理。
    /// </summary>
    public static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static bool SameDeviceId(string? left, string? right)
    {
        return string.Equals(
            NormalizeText(left),
            NormalizeText(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
