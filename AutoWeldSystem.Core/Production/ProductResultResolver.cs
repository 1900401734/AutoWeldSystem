using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using System.Text.Json;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 从一组焊点记录解析产品级结果。
/// 优先读采集时固化的 <see cref="BizWeldPointRecord.ProductResult"/>；旧记录为空时回退 RawDataJson.product_result。
/// 不根据焊点 TestResult 重新推算：PLC 读取模式的产品结果由 PLC 给出，程序判定模式已在采集时写入该字段。
/// 产品历史、完工统计与中心转发共用，保证三处口径一致。
/// </summary>
public static class ProductResultResolver
{
    private const string LegacyProductResultKey = "product_result";

    public static string Resolve(IEnumerable<BizWeldPointRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var list = records as IReadOnlyList<BizWeldPointRecord> ?? records.ToList();
        var storedResult = list
            .Select(record => record.ProductResult)
            .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result));
        if (!string.IsNullOrWhiteSpace(storedResult))
        {
            return TestResultRules.Normalize(storedResult);
        }

        foreach (var record in list)
        {
            var legacyResult = ReadLegacyProductResult(record.RawDataJson);
            if (!string.IsNullOrWhiteSpace(legacyResult))
            {
                return TestResultRules.Normalize(legacyResult);
            }
        }

        return ProductionConstants.TestResults.Unknown;
    }

    private static string? ReadLegacyProductResult(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(LegacyProductResultKey, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => value.ToString()
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
