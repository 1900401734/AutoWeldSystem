using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品重测触发的上传任务重开规则。
/// 重测就地覆盖同一产品的记录，因此按产品级自然键复用的上传任务必须重新上报；
/// 报表文件另有独立的重开依赖规则，不在此处处理。
/// </summary>
public static class UploadTaskRetestReopenRules
{
    /// <summary>
    /// 判断任务类型是否允许因重测重开。
    /// 过程参数和中心看板转发都按 (任务, 工位, 产品编号) 复用同一条任务，重测后必须携带新数据重传；
    /// 开工、完工和设备状态与单个产品无关，不参与重开。
    /// </summary>
    public static bool IsReopenableTaskType(string? taskType)
    {
        var normalized = taskType?.Trim();
        return string.Equals(normalized, ProductionConstants.UploadTaskTypes.ProcessParameter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, ProductionConstants.UploadTaskTypes.CenterProductReport, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断已上传任务是否应因重测重开。
    /// 仅整件检测设备支持重测；只有已上传任务收到待上传的新数据时才重开，
    /// 避免正常重试或状态同步把已完成任务反复打回。
    /// </summary>
    public static bool ShouldReopen(
        string? existingStatus,
        string? incomingStatus,
        string? processParameterDeviceType)
    {
        if (!ProductRetestRules.IsSupportedDeviceType(processParameterDeviceType))
        {
            return false;
        }

        return string.Equals(existingStatus?.Trim(), ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase)
            && string.Equals(incomingStatus?.Trim(), ProductionConstants.UploadStatuses.Pending, StringComparison.OrdinalIgnoreCase);
    }
}
