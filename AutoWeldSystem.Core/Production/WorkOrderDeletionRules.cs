using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 历史工单删除的前置判定规则。
/// </summary>
public static class WorkOrderDeletionRules
{
    /// <summary>
    /// 待开工状态，与 TaskStatusDisplayRules 保持一致使用字面量。
    /// </summary>
    private const string ReadyStatus = "Ready";

    /// <summary>
    /// 已暂停状态，现场暂停后仍占用工位，视为运行中。
    /// </summary>
    private const string PausedStatus = "Paused";

    /// <summary>
    /// 生产中和已暂停的工单仍被 ProductionRuntimeState 引用，删除会留下悬空引用。
    /// </summary>
    public static bool IsRunning(string? taskStatus)
    {
        var status = taskStatus?.Trim();
        if (string.IsNullOrEmpty(status))
        {
            return false;
        }

        return string.Equals(status, ProductionConstants.ProductInstanceStatuses.Running, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, PausedStatus, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 只有非运行中的工单允许删除；待开工、已完成和已作废都可删。
    /// </summary>
    public static bool CanDelete(string? taskStatus)
    {
        return !IsRunning(taskStatus);
    }

    /// <summary>
    /// 校验报表文件是否位于报表根目录之下，避免数据库中的异常路径导致删除到目录外文件。
    /// </summary>
    public static bool IsDeletableReportPath(string? filePath, string? reportRootDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(reportRootDirectory))
        {
            return false;
        }

        string fullFilePath;
        string fullRootPath;
        try
        {
            fullFilePath = Path.GetFullPath(filePath.Trim());
            fullRootPath = Path.GetFullPath(reportRootDirectory.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        // 根目录统一补上分隔符，防止 Reports2 被误判为 Reports 的子目录
        var normalizedRoot = fullRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return fullFilePath.Length > normalizedRoot.Length
            && fullFilePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 拼接报表根目录，与 ProductionReportFileService 生成报表时的规则保持一致。
    /// </summary>
    public static string ResolveReportRootDirectory(string? dataDirectory)
    {
        var baseDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : dataDirectory.Trim();

        return Path.Combine(baseDirectory, "Reports");
    }
}
