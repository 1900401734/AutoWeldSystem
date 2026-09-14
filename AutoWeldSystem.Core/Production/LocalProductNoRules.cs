using System.Globalization;
using System.Text.Json;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序自算产品编号规则。
/// 现场 PLC 编号为 Int16 原值（裸十进制 1,2,3…），程序自算沿用同一形状，报表编号列外观不变。
/// 编号只增不回收：已删除产品的行仍留在库中参与取最大值，避免新品复用旧号后撞上自然键去重被静默丢件。
/// </summary>
public static class LocalProductNoRules
{
    // 沿用历史采集记录的原始编号键，供程序统计模式识别 PLC 被动重测。
    public const string PlcProductNoKey = "plc_product_no";

    /// <summary>
    /// 根据同一任务、同一工位的记录只读解析待采集编号，不占号，也不修改已有记录。
    /// </summary>
    public static (string ProductNo, bool IsOverwrite) ResolvePendingProductNo(
        IReadOnlyList<BizWeldPointRecord> existingRecords,
        string? plcProductNo,
        string? processParameterDeviceType)
    {
        ArgumentNullException.ThrowIfNull(existingRecords);

        // 软件预约优先于 PLC 被动重测，必须覆盖操作员指定的产品。
        var reweldTarget = existingRecords
            .Where(record => record.IsReweldPending && !string.IsNullOrWhiteSpace(record.ProductNo))
            .OrderByDescending(record => record.SequenceNo)
            .FirstOrDefault();
        if (reweldTarget is not null)
        {
            return (reweldTarget.ProductNo.Trim(), true);
        }

        if (!string.IsNullOrWhiteSpace(plcProductNo)
            && ProductRetestRules.IsSupportedDeviceType(processParameterDeviceType))
        {
            var latest = existingRecords
                .OrderByDescending(record => record.SequenceNo)
                .ThenByDescending(record => record.Id)
                .FirstOrDefault();
            var latestPlcProductNo = ReadPlcProductNo(latest?.RawDataJson);
            if (latest is not null
                && !latest.IsDeleted
                && string.Equals(latestPlcProductNo, plcProductNo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return (latest.ProductNo.Trim(), true);
            }
        }

        return (NextProductNo(existingRecords.Select(record => record.ProductNo)), false);
    }

    /// <summary>
    /// 依据该任务该工位已有的全部产品编号（含已删除）计算下一个编号。
    /// 非数字编号（PLC 模式遗留）忽略；无可解析编号时从 1 开始。
    /// </summary>
    public static string NextProductNo(IEnumerable<string?> existingProductNos)
    {
        ArgumentNullException.ThrowIfNull(existingProductNos);

        long max = 0;
        foreach (var productNo in existingProductNos)
        {
            if (TryParse(productNo, out var value) && value > max)
            {
                max = value;
            }
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string? ReadPlcProductNo(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(PlcProductNoKey, out var value)
                && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryParse(string? productNo, out long value)
        => long.TryParse(productNo?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
