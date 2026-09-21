using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

public static class TaskStatusDisplayRules
{
    public static string GetDisplayText(string? status)
    {
        return status?.Trim() switch
        {
            "Ready" => "待开工",
            ProductionConstants.ProductInstanceStatuses.Running => "生产中",
            "Paused" => "已暂停",
            ProductionConstants.ProductInstanceStatuses.Completed => "已完成",
            ProductionConstants.ProductInstanceStatuses.Abandoned => "已作废",
            ProductionConstants.UploadStatuses.Failed => "失败",
            _ => status ?? string.Empty
        };
    }
}
