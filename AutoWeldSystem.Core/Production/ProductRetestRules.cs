using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品重测规则。
/// 现场约束：检测设备的 PLC 触摸屏点击“重测”后不会更新产品编号，
/// 因此上位机只能依据“紧邻上一轮的产品编号相同”来识别重测，PLC 不额外提供重测信号。
/// </summary>
public static class ProductRetestRules
{
    /// <summary>
    /// 判断当前设备类型是否支持产品重测。
    /// 重测只为整件检测设备设计；点焊设备（电磁、整件焊接）不需要该功能，
    /// 必须保持既有的“命中自然键即跳过”行为，避免影响双工位共享产品就绪信号的防重逻辑。
    /// </summary>
    public static bool IsSupportedDeviceType(string? processParameterDeviceType)
        => string.Equals(
            processParameterDeviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断本轮采集是否为同一产品的重测。
    /// 仅当已存在同一自然键记录，且该产品编号等于本任务本工位最近一次采集的产品编号时成立。
    /// 限定“紧邻上一轮”而非“任务内任意重复”，避免产品编号回绕时把新件误判为重测。
    /// </summary>
    /// <param name="processParameterDeviceType">过程参数设备类型。</param>
    /// <param name="latestProductNo">本任务本工位最近一次采集的产品编号。</param>
    /// <param name="incomingProductNo">本轮采集到的产品编号。</param>
    public static bool IsRetest(
        string? processParameterDeviceType,
        string? latestProductNo,
        string? incomingProductNo)
    {
        if (!IsSupportedDeviceType(processParameterDeviceType))
        {
            return false;
        }

        var normalizedLatest = latestProductNo?.Trim();
        var normalizedIncoming = incomingProductNo?.Trim();
        return !string.IsNullOrEmpty(normalizedLatest)
            && !string.IsNullOrEmpty(normalizedIncoming)
            && string.Equals(normalizedLatest, normalizedIncoming, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 将重测采集到的最新值覆盖到已存在的记录上。
    /// 只覆盖测试结果类字段，保留主键与顺序号，使报表、产品历史和上传任务沿用既有产品级自然键。
    /// 同时把上传状态打回待上传：过程参数待上传集合会排除已上传记录，
    /// 不重置状态则重测数据不会重新进入上报流程。
    /// </summary>
    public static void ApplyRetestValues(BizWeldPointRecord existing, BizWeldPointRecord incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);

        existing.TestResult = incoming.TestResult;
        existing.ProductResult = incoming.ProductResult;
        existing.RawDataJson = incoming.RawDataJson;
        existing.Ts = incoming.Ts;
        existing.IsTest = incoming.IsTest;
        existing.OperatorNo = incoming.OperatorNo;
        existing.ProductCompleted = incoming.ProductCompleted;
        existing.UploadStatus = ProductionConstants.UploadStatuses.Pending;
        existing.UploadTime = null;
        existing.UploadMessage = null;
        existing.RetryCount = 0;
    }

    /// <summary>
    /// 选出上一轮多余、本轮未覆盖的残留焊点或面记录。
    /// 现场约束：PLC 要等视觉数据全部测试完成才触发采集，实际面数恒定，
    /// 因此该集合正常为空；保留该判定用于防呆，避免出现两轮混合的产品数据
    /// 被四面转 A/B 聚合成既不属于上一轮也不属于本轮的产品结果。
    /// </summary>
    public static IReadOnlyList<BizWeldPointRecord> SelectStaleRecords(
        IEnumerable<BizWeldPointRecord> existingRecords,
        IEnumerable<BizWeldPointRecord> incomingRecords)
    {
        ArgumentNullException.ThrowIfNull(existingRecords);
        ArgumentNullException.ThrowIfNull(incomingRecords);

        var incomingTouchNos = incomingRecords
            .Select(record => record.TouchNo?.Trim() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return existingRecords
            .Where(record => !incomingTouchNos.Contains(record.TouchNo?.Trim() ?? string.Empty))
            .ToList();
    }
}
