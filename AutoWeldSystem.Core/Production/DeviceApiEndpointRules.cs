using System.Net;
using System.Net.Sockets;

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
    /// 解析基地址中的主机和端口，供按路由探测本机上报 IP 使用。
    /// 这里不套用 NormalizeBaseUrl 的默认值，地址未配置时应直接放弃探测而不是误连本机。
    /// </summary>
    public static bool TryGetEndpoint(string? baseUrl, out string host, out int port)
    {
        host = string.Empty;
        port = 0;
        if (!Uri.TryCreate(NormalizeText(baseUrl), UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host)
            || uri.Port <= 0)
        {
            return false;
        }

        host = uri.Host;
        port = uri.Port;
        return true;
    }

    /// <summary>
    /// 取基地址中的非环回 IPv4 字面量。
    /// 现场把设备状态地址配成具体网卡 IP 时，该地址就是 MES 能访问到的本机地址，可直接作为上报 IP。
    /// </summary>
    public static bool TryGetIPv4AddressFromBaseUrl(string? baseUrl, out string address)
    {
        address = string.Empty;
        if (!Uri.TryCreate(NormalizeText(baseUrl), UriKind.Absolute, out var uri)
            || !IsUsableReportedIPv4Address(uri.Host))
        {
            return false;
        }

        address = uri.Host;
        return true;
    }

    /// <summary>
    /// 按可信度从高到低选择上报给 MES 的本机 IP：
    /// 到 MES 的路由源地址、设备状态地址中现场已配置的本机 IP、主机名解析结果。
    /// 全部不可用时返回空字符串，保持原有"宁可不报也不报错值"的行为。
    /// </summary>
    public static string SelectReportedIPv4Address(
        string? routedAddress,
        string? deviceBaseUrlAddress,
        string? hostEntryAddress)
    {
        foreach (var candidate in new[] { routedAddress, deviceBaseUrlAddress, hostEntryAddress })
        {
            var normalized = NormalizeText(candidate);
            if (IsUsableReportedIPv4Address(normalized))
            {
                return normalized;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 判断地址能否作为上报 IP：必须是非环回、非通配的 IPv4。
    /// </summary>
    public static bool IsUsableReportedIPv4Address(string? address)
    {
        return IPAddress.TryParse(NormalizeText(address), out var parsed)
            && parsed.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(parsed)
            && !IPAddress.Any.Equals(parsed);
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
