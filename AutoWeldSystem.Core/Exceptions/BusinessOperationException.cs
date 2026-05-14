namespace AutoWeldSystem.Core.Exceptions;

/// <summary>
/// 可预见的业务异常。
/// 用于区分 MES/PLC/业务规则失败和代码运行时异常，界面只显示简要信息，详细信息写入日志。
/// </summary>
public sealed class BusinessOperationException : InvalidOperationException
{
    public BusinessOperationException(string source, string message, string? detail = null)
        : base(message)
    {
        SourceName = source;
        Detail = string.IsNullOrWhiteSpace(detail) ? message : detail;
    }

    /// <summary>
    /// 业务异常来源，例如 MES.StartReport 或 PLC.WorkIdMonitor。
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// 详细错误内容，写入日志管理用于排查。
    /// </summary>
    public string Detail { get; }
}
