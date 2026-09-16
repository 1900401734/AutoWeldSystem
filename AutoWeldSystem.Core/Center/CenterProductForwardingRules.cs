using System.Security.Cryptography;
using System.Text;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;

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

    /// <summary>为旧队列补齐设备端已持久化的报表身份；重试中不能更换身份。</summary>
    public static void ApplyReportIdentity(
        CenterProductReportRequest request, BizWeldTask task, BizProductionReportFile report)
    {
        if (report.TaskId != task.Id || report.SequenceNo <= 0 || string.IsNullOrWhiteSpace(report.FileName))
        {
            throw new InvalidOperationException("设备端报表身份无效，不能转发中心报表。");
        }

        if (request.ReportSequenceNo.HasValue || request.ReportFileName is not null)
        {
            if (request.ReportSequenceNo != report.SequenceNo
                || !string.Equals(request.ReportFileName, report.FileName, StringComparison.Ordinal)
                || !string.Equals(request.DeviceId, report.DeviceId.Trim(), StringComparison.Ordinal)
                || !string.Equals(request.WorkOrder, report.SN, StringComparison.Ordinal)
                || !string.Equals(request.ProcessNo, report.ProcessNo, StringComparison.Ordinal)
                || request.StartTime != task.StartTime)
            {
                throw new InvalidOperationException("中心转发请求与已预留的报表身份不一致，禁止重新编号或覆盖。");
            }
            return;
        }

        request.DeviceId = report.DeviceId.Trim();
        request.WorkOrder = report.SN;
        request.ProcessNo = report.ProcessNo;
        request.StartTime = task.StartTime;
        request.ReportSequenceNo = report.SequenceNo;
        request.ReportFileName = report.FileName;
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
