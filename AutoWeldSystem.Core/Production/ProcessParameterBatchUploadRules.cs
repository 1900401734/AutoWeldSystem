using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using System.Security.Cryptography;
using System.Text;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Rules for creating quantity-mode process-parameter upload batches.
/// </summary>
public static class ProcessParameterBatchUploadRules
{
    /// <summary>
    /// Selects the next completed product numbers that are ready to form one upload batch.
    /// </summary>
    public static IReadOnlyList<string> TakeReadyProductNos(
        IEnumerable<BizWeldPointRecord> records,
        int taskId,
        int stationNo,
        int batchSize,
        IEnumerable<string>? excludedProductNos = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        var normalizedBatchSize = Math.Max(1, batchSize);
        var excluded = NormalizeProductNos(excludedProductNos).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return records
            .Where(record => record.TaskId == taskId)
            .Where(record => record.StationNo == stationNo)
            .Where(record => record.ProductCompleted)
            .Where(record => !string.Equals(record.UploadStatus, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase))
            .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => record.ProductNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                ProductNo = group.Key,
                FirstTime = group.Min(record => record.Ts),
                FirstSequence = group.Min(record => record.SequenceNo)
            })
            .Where(product => !excluded.Contains(product.ProductNo))
            .OrderBy(product => product.FirstTime)
            .ThenBy(product => product.FirstSequence)
            .ThenBy(product => product.ProductNo, StringComparer.OrdinalIgnoreCase)
            .Take(normalizedBatchSize)
            .Select(product => product.ProductNo)
            .ToList();
    }

    /// <summary>
    /// Returns true when a product list is large enough for one quantity-mode upload batch.
    /// </summary>
    public static bool IsReady(IReadOnlyCollection<string> productNos, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(productNos);
        return productNos.Count >= Math.Max(1, batchSize);
    }

    /// <summary>
    /// Builds a stable upload-task business id for one batch without exceeding the database field length.
    /// </summary>
    public static string BuildQuantityBusinessId(int taskId, int stationNo, IEnumerable<string> productNos)
    {
        var normalizedProductNos = NormalizeProductNos(productNos).ToList();
        if (normalizedProductNos.Count == 0)
        {
            throw new InvalidOperationException("Quantity upload batch must contain at least one product number.");
        }

        var source = $"{taskId}|{stationNo}|{string.Join('\u001F', normalizedProductNos)}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..16].ToLowerInvariant();
        return $"task-{taskId}:s{stationNo}:pp:q{normalizedProductNos.Count}:{hash}";
    }

    /// <summary>
    /// Normalizes product numbers for consistent payload and business-id generation.
    /// </summary>
    public static IReadOnlyList<string> NormalizeProductNos(IEnumerable<string?>? productNos)
    {
        return productNos is null
            ? Array.Empty<string>()
            : productNos
                .Select(productNo => productNo?.Trim())
                .Where(productNo => !string.IsNullOrWhiteSpace(productNo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(productNo => productNo!)
                .ToList();
    }
}
