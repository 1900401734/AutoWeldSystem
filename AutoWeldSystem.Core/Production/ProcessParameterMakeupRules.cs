using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 完工时过程参数补传范围规则。
/// 按数量上传模式下，完工前已成批提交过的产品不能再次进入补传范围，
/// 否则同一批过程参数会被重复提交给 MES。
/// </summary>
public static class ProcessParameterMakeupRules
{
    /// <summary>
    /// 计算完工时仍需补传过程参数的产品号。
    /// 排除两类产品：已上传成功的，以及仍被在途或待重试上传任务认领的。
    /// </summary>
    /// <param name="records">该焊接任务下的焊点采集记录。</param>
    /// <param name="weldTaskId">本地焊接任务 ID。</param>
    /// <param name="claimedProductNos">已被其他未完成上传任务覆盖的产品号。</param>
    public static IReadOnlyList<string> TakeMakeupProductNos(
        IEnumerable<BizWeldPointRecord> records,
        int weldTaskId,
        IEnumerable<string>? claimedProductNos = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        var claimed = ProcessParameterBatchUploadRules
            .NormalizeProductNos(claimedProductNos)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ProcessParameterBatchUploadRules.NormalizeProductNos(
            records
                .Where(record => record.TaskId == weldTaskId)
                .Where(record => record.ProductCompleted)
                .Where(record => !string.Equals(
                    record.UploadStatus,
                    ProductionConstants.UploadStatuses.Uploaded,
                    StringComparison.OrdinalIgnoreCase))
                .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
                .Where(record => !claimed.Contains(record.ProductNo.Trim()))
                .OrderBy(record => record.Ts)
                .ThenBy(record => record.SequenceNo)
                .Select(record => record.ProductNo));
    }
}
