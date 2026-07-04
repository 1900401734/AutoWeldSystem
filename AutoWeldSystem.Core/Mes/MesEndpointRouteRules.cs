namespace AutoWeldSystem.Core.Mes;

/// <summary>
/// MES 接口路由和可选 Header 的统一规则。
/// 路由只保存相对路径，MES 基础地址仍由 MesBaseUrl 单独配置。
/// </summary>
public static class MesEndpointRouteRules
{
    public const string UserDefaultRoute = "api/User";
    public const string WorkOrderDefaultRoute = "api/ItemsOfBatchTech";
    public const string ServerTimeDefaultRoute = "api/ServerTime";
    public const string ProgramManageDefaultRoute = "api/ExpProgram";
    public const string StartWorkDefaultRoute = "api/ExpStartV2";
    public const string WorkStatusDefaultRoute = "api/ExpStatus";
    public const string EndWorkDefaultRoute = "api/ExpEnd";
    public const string ReportFileDefaultRoute = "api/ExpFile";
    public const string PostDataDefaultRoute = "api/PostData";
    public const string DeviceDefaultRoute = "api/Device";
    public const string DeviceStatusDefaultRoute = "api/DeviceStatusV2";

    /// <summary>
    /// 归一化相对路由；历史空值使用默认路由，前导斜杠会被移除。
    /// </summary>
    public static string NormalizeRoute(string? route, string defaultRoute)
    {
        var normalized = string.IsNullOrWhiteSpace(route)
            ? defaultRoute
            : route.Trim();

        normalized = normalized.Replace('\\', '/').TrimStart('/');
        return string.IsNullOrWhiteSpace(normalized)
            ? defaultRoute
            : normalized;
    }

    /// <summary>
    /// 保存设置页输入前校验路由，防止把完整 URL 或查询参数误填为路由。
    /// </summary>
    public static bool TryNormalizeRequiredRoute(
        string? route,
        string displayName,
        out string normalizedRoute,
        out string errorMessage)
    {
        normalizedRoute = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(route))
        {
            errorMessage = $"{displayName}不能为空。";
            return false;
        }

        normalizedRoute = route.Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalizedRoute))
        {
            errorMessage = $"{displayName}不能为空。";
            return false;
        }

        if (Uri.TryCreate(normalizedRoute, UriKind.Absolute, out _))
        {
            errorMessage = $"{displayName}请填写相对路由，例如 api/ExpProgram，不要填写完整 URL。";
            return false;
        }

        if (normalizedRoute.Contains('?', StringComparison.Ordinal)
            || normalizedRoute.Contains('#', StringComparison.Ordinal))
        {
            errorMessage = $"{displayName}不能包含查询参数或锚点。";
            return false;
        }

        return true;
    }

    public static string NormalizeHeaderKey(string? headerKey)
        => headerKey?.Trim() ?? string.Empty;

    public static string NormalizeHeaderValue(string? headerValue)
        => headerValue?.Trim() ?? string.Empty;

    /// <summary>
    /// HTTP Header 名称必须符合 token 格式，不能包含空格、冒号或中文字符。
    /// </summary>
    public static bool IsValidHeaderKey(string? headerKey)
    {
        var normalized = NormalizeHeaderKey(headerKey);
        if (normalized.Length == 0)
        {
            return false;
        }

        return normalized.All(IsHeaderTokenChar);
    }

    public static bool TryValidatePostDataHeader(
        bool enabled,
        string? headerKey,
        string? headerValue,
        out string normalizedHeaderKey,
        out string normalizedHeaderValue,
        out string errorMessage)
    {
        normalizedHeaderKey = NormalizeHeaderKey(headerKey);
        normalizedHeaderValue = NormalizeHeaderValue(headerValue);
        errorMessage = string.Empty;

        if (!enabled)
        {
            return true;
        }

        if (!IsValidHeaderKey(normalizedHeaderKey))
        {
            errorMessage = "PostData Header Key 不能为空，且不能包含空格、冒号或中文字符。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedHeaderValue))
        {
            errorMessage = "PostData Header Value 不能为空。";
            return false;
        }

        return true;
    }

    private static bool IsHeaderTokenChar(char value)
    {
        return value is >= 'A' and <= 'Z'
            || value is >= 'a' and <= 'z'
            || value is >= '0' and <= '9'
            || value is '!' or '#' or '$' or '%' or '&' or '\'' or '*'
                or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~';
    }
}
