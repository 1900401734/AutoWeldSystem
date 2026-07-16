using System.Security.Cryptography;
using System.Text;
using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.Core.Center;

/// <summary>
/// 中心产品转发任务的稳定身份规则。
/// </summary>
public static class CenterProductForwardingRules
{
    private const int MaxBusinessIdLength = 100;
    private const int Sha256HexLength = 64;

    /// <summary>
    /// 使用可读前缀和完整请求身份的 SHA256，避免长工单或产品号截断碰撞。
    /// </summary>
    public static string BuildBusinessId(CenterProductReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var readableIdentity = request.IsTaskFinishUpdate
            ? $"center:finish:wo{request.WorkOrder}"
            : $"center:s{request.StationNo}:wo{request.WorkOrder}:p{request.ProductNo}";
        var fullIdentity = BuildFullIdentity(request);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullIdentity)))
            .ToLowerInvariant();
        var readableLength = MaxBusinessIdLength - Sha256HexLength - 1;
        var readablePrefix = readableIdentity.Length <= readableLength
            ? readableIdentity
            : readableIdentity[..readableLength];

        return $"{readablePrefix}:{hash}";
    }

    /// <summary>
    /// 长度前缀让字段边界稳定，字段内容即使包含分隔符也不会产生身份歧义。
    /// </summary>
    private static string BuildFullIdentity(CenterProductReportRequest request)
    {
        var builder = new StringBuilder();
        AppendPart(builder, request.IsTaskFinishUpdate ? "finish" : "product");
        AppendPart(builder, (request.DeviceId ?? string.Empty).Trim());
        AppendPart(builder, request.StationNo.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendPart(builder, request.WorkOrder ?? string.Empty);
        AppendPart(builder, request.IsTaskFinishUpdate ? string.Empty : request.ProductNo ?? string.Empty);
        return builder.ToString();
    }

    private static void AppendPart(StringBuilder builder, string value)
    {
        builder.Append(value.Length)
            .Append(':')
            .Append(value);
    }
}
