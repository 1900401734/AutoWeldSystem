namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 生产流程日志摘要及其运行提示资源键。
/// 摘要固定使用中文落盘，资源键仅用于 MonitorView 的本地化运行提示。
/// </summary>
public static class ProductionFlowLogTexts
{
    public static class Summaries
    {
        public const string ProductDataReadyReset = "PLC已清空产品数据就绪信号";
        public const string ProductDataReady = "检测到产品数据就绪信号";
        public const string ProductDataReadyStaleHigh = "产品数据就绪仍为高电平，等待PLC复位";
        public const string ProductCollectionStart = "开始读取整件产品数据";
        public const string ProductCollectionFeedbackSucceeded = "已反馈PLC采集成功";
        public const string ProductCollectionFeedbackFailed = "已反馈PLC采集失败";
        public const string ProductCollectionFeedbackPending = "PLC采集反馈写入失败，等待重试";
        public const string ProductCollectionConfigurationFailed = "产品采集配置错误，已反馈PLC接收";
        public const string ProductDataReadStart = "开始采集产品周期数据";
        public const string ProductDataSaveFailed = "产品采集数据保存失败";
        public const string ProductDataSaved = "产品采集数据已保存";
        public const string ProcessParameterQuantityBatchCreated = "过程参数达到特定数量，已创建批次上传任务";
        public const string CenterProductForwardQueued = "中心服务器产品数据转发已入队";
        public const string CenterTaskFinishUpdateQueued = "中心服务器工单完工更新已入队";
        public const string CenterProductForwardSucceeded = "中心服务器产品数据转发成功";
        public const string CenterProductForwardFailed = "中心服务器产品数据转发失败，等待重试";
        public const string CenterProductForwardTaskTypeRepaired = "已修正历史中心服务器转发任务类型，将自动补传";
        public const string CenterProductForwardTasksResumed = "已恢复未完成的中心服务器转发任务，将重新补传";
        public const string CenterProductForwardFinishSweep = "工单完工补漏：已重新排队未成功的中心服务器产品数据";
        public const string UploadSucceeded = "上传成功";
        public const string UploadFailed = "上传失败";
        public const string ProcessParameterUploadSucceeded = "过程参数上传成功";
        public const string ProcessParameterUploadFailed = "过程参数上传失败";
        public const string ReportFileUploadSucceeded = "报告文件上传成功";
        public const string ReportFileUploadFailed = "报告文件上传失败";
        public const string RecipeCodeResolveFailed = "配方编号解析失败";
        public const string RecipeCodeWriteStarted = "配方编号准备下发";
        public const string RecipeCodeWriteFailed = "配方编号下发失败";
        public const string RecipeCodeWriteSucceeded = "配方编号已下发";
        public const string RecipeCodeValidationFailed = "配方编号校验失败";
        public const string RecipeCodeValidationSucceeded = "配方编号校验通过";
        public const string RecipeCodeReconcileFailed = "PLC配方号调和失败";
        public const string DeviceModeReconcileFailed = "设备模式调和失败";
        public const string WorkOrderStatusReconcileFailed = "工单状态调和失败";
        public const string WorkOrderStatusWriteFailed = "工单状态写入失败";

        public static string FormatRecipeCodeChanged(string recipeCode)
            => $"PLC侧配方号变更至：{recipeCode}";

        public static string FormatRecipeCodeReconcileSucceeded(string recipeCode)
            => $"配方号调和成功：{recipeCode}";

        public static string FormatSignalReadFailed(string plcSignal)
            => $"{plcSignal}读取失败，未执行调和写入";

        public static string FormatSignalReconcileSucceeded(string plcSignal)
            => $"{plcSignal}调和写入成功";
    }

    public static class ResourceKeys
    {
        public const string BusinessSignalWriteFailed = "monitor.production_hint.business_signal_write_failed";
        public const string BusinessSignalWriteSucceeded = "monitor.production_hint.business_signal_write_succeeded";
        public const string ProductCollectionFeedbackFailed = "monitor.production_hint.product_collection_feedback_failed";
        public const string ProductCollectionFeedbackSucceeded = "monitor.production_hint.product_collection_feedback_succeeded";
        public const string ProductCollectionStart = "monitor.production_hint.product_collection_start";
        public const string ProductDataReady = "monitor.production_hint.product_data_ready";
        public const string ProductDataReadStart = "monitor.production_hint.product_data_read_start";
        public const string ProductDataSaved = "monitor.production_hint.product_data_saved";
        public const string ProductDataSaveFailed = "monitor.production_hint.product_data_save_failed";
        public const string RecipeCodeValidationFailed = "monitor.production_hint.recipe_code_validation_failed";
        public const string RecipeCodeValidationSucceeded = "monitor.production_hint.recipe_code_validation_succeeded";
        public const string RecipeCodeWriteFailed = "monitor.production_hint.recipe_code_write_failed";
        public const string RecipeCodeWriteSucceeded = "monitor.production_hint.recipe_code_write_succeeded";
        public const string RecipeCodeChangedDetected = "monitor.production_hint.recipe_code_changed_detected";
        public const string RecipeCodeReconcileSucceeded = "monitor.production_hint.recipe_code_reconcile_succeeded";
        public const string RecipeCodeReconcileFailed = "monitor.production_hint.recipe_code_reconcile_failed";
    }

    /// <summary>
    /// 兼容旧 JSONL 中已经落盘的英文摘要，仅在读取显示时转换，不改写历史文件。
    /// </summary>
    public static string NormalizeLegacySummary(string? summary)
    {
        var value = summary?.Trim() ?? string.Empty;
        if (value.Equals("PLC recipe code reconcile failed", StringComparison.OrdinalIgnoreCase))
        {
            return Summaries.RecipeCodeReconcileFailed;
        }

        if (value.Equals("Device mode reconcile failed.", StringComparison.OrdinalIgnoreCase))
        {
            return Summaries.DeviceModeReconcileFailed;
        }

        if (value.Equals("Work order status reconcile failed.", StringComparison.OrdinalIgnoreCase))
        {
            return Summaries.WorkOrderStatusReconcileFailed;
        }

        return value.Equals("Work order status write failed.", StringComparison.OrdinalIgnoreCase)
            ? Summaries.WorkOrderStatusWriteFailed
            : value;
    }
}
