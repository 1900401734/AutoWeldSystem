using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// 程序异常日志。
/// 保存排查异常时最需要的信息，界面层可以直接用这些字段定位源码和查看堆栈。
/// </summary>
public sealed class ProgramExceptionLogEntry
{
    /// <summary>
    /// 本次异常的唯一追踪编号。
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 异常发生时间。
    /// </summary>
    public DateTime OccurredTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 异常等级，当前默认使用 Error，后续可扩展 Warning、Fatal。
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// 异常分类：Business 表示可预见业务异常，Program 表示程序运行异常。
    /// </summary>
    public string Category { get; set; } = AppConstants.ExceptionLogCategories.Program;

    /// <summary>
    /// 记录异常的位置，例如全局 UI 异常、后台任务异常或启动异常。
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 异常类型全名。
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 最接近业务代码的源码文件路径。
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 源码行号。没有调试符号时可能为 0。
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// 源码所在成员，例如 Namespace.Type.Method。
    /// </summary>
    public string SourceMemberName { get; set; } = string.Empty;

    /// <summary>
    /// 抛出异常的目标方法。
    /// </summary>
    public string TargetSite { get; set; } = string.Empty;

    /// <summary>
    /// 当前线程编号。
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 当前线程名称。
    /// </summary>
    public string ThreadName { get; set; } = string.Empty;

    /// <summary>
    /// Windows 当前登录用户。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 当前机器名。
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// 程序版本。
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;

    /// <summary>
    /// 调用方补充的业务上下文。
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// 完整异常堆栈。
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>
    /// 内部异常的完整文本。
    /// </summary>
    public string InnerException { get; set; } = string.Empty;
}
