namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 把上传任务里持久化的英文处理消息映射为操作员可读的中文。
/// 服务层写入的消息同时充当运维排障线索、并会落到上传任务与 JSONL，
/// 因此不改写存储值，只在界面渲染时翻译一次，历史库里的旧英文记录也能一并显示为中文。
/// 未收录的消息原样返回：MES 返回的 Msg 和异常文本必须透传，不能吞掉排障信息。
/// </summary>
public static class UploadMessageDisplayRules
{
    private static readonly Dictionary<string, string> DisplayTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Device status is queued for MES retry."] = "设备状态已排队，等待 MES 补传。",
        ["Device status is already uploaded."] = "设备状态已上传。",
        ["Device status report is disabled in system settings."] = "系统设置已关闭设备状态上报。",
        ["Device status JSONL initial write failed."] = "设备状态本地记录写入失败。",
        ["Device status JSONL source was deleted."] = "设备状态本地记录已删除。",
        ["Device status JSONL source is missing or no longer pending."] = "设备状态本地记录缺失或已不需补传。",
        ["Start report is queued for MES retry."] = "开工上报已排队，等待 MES 补传。",
        ["Start report uploaded."] = "开工上报已上传。",
        ["Start report uploaded to MES."] = "开工上报已上传到 MES。",
        ["Finish report uploaded."] = "完工上报已上传。",
        ["Finish report is queued for MES retry."] = "完工上报已排队，等待 MES 补传。",
        ["Work-order status is queued for MES retry."] = "工单状态已排队，等待 MES 补传。",
        ["Report file restored from generated XLSX record."] = "报告文件已按已生成的 XLSX 记录恢复。",
        ["Final report file reopened after premature upload."] = "报告文件在提前上传后已重新打开。",
        ["Local task created offline. Start report is queued for MES retry."] = "离线创建本地任务，开工上报已排队，等待 MES 补传。",
        ["Local finish completed offline. Finish data is queued for MES retry."] = "离线完成本地完工，完工数据已排队，等待 MES 补传。",
        ["Upload task has no weld task."] = "上传任务没有关联的生产任务。",
        ["Upload task is waiting for start report upload."] = "上传任务正在等待开工上报完成。",
        ["Upload task has no process-parameter task id."] = "上传任务缺少过程参数任务 ID。",
        ["Manual retry requested."] = "已手动触发重试。",
        ["Center product report uploaded."] = "中心服务器产品数据已上传。"
    };

    /// <summary>
    /// 取处理消息的显示文本；未收录的消息按原文返回。
    /// </summary>
    public static string GetDisplayText(string? message)
    {
        var normalized = message?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        if (DisplayTexts.TryGetValue(normalized, out var displayText))
        {
            return displayText;
        }

        return TranslateInterpolated(normalized);
    }

    /// <summary>
    /// 翻译带运行时数据的消息。这些消息由 $"..." 拼出，无法整句匹配，
    /// 因此按前缀识别后保留其中的产品编号、数量等现场排障需要的原始数据。
    /// </summary>
    private static string TranslateInterpolated(string message)
    {
        const string quantityBatchPrefix = "Quantity upload batch is ready. ProductCount=";
        if (message.StartsWith(quantityBatchPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return $"数量批次已满足上传条件，产品数 {message[quantityBatchPrefix.Length..]}。";
        }

        const string productCompletedPrefix = "Product ";
        const string realtimeReadySuffix = " completed. Realtime process-parameter upload is ready.";
        const string quantityWaitingSuffix = " completed. Waiting for quantity upload threshold.";
        if (message.StartsWith(productCompletedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (message.EndsWith(realtimeReadySuffix, StringComparison.OrdinalIgnoreCase))
            {
                var productNo = message[productCompletedPrefix.Length..^realtimeReadySuffix.Length];
                return $"产品 {productNo} 已完成，实时过程参数上传就绪。";
            }

            if (message.EndsWith(quantityWaitingSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var productNo = message[productCompletedPrefix.Length..^quantityWaitingSuffix.Length];
                return $"产品 {productNo} 已完成，等待达到数量上传阈值。";
            }
        }

        return message;
    }
}
