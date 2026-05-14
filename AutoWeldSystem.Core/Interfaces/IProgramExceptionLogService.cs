using System.Runtime.CompilerServices;
using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 程序异常日志服务。
/// 统一负责异常落盘、历史读取和实时通知，避免界面层直接处理文件 IO。
/// </summary>
public interface IProgramExceptionLogService
{
    /// <summary>
    /// 新异常日志写入后触发，供日志界面实时刷新。
    /// </summary>
    event EventHandler<ProgramExceptionLogEntry>? LogWritten;

    /// <summary>
    /// 根据异常对象生成日志并写入本地文件。
    /// </summary>
    ProgramExceptionLogEntry Write(Exception exception, string source, string? context = null);

    /// <summary>
    /// 写入可预见业务异常，例如 PLC 读取失败、MES 返回失败等。
    /// </summary>
    ProgramExceptionLogEntry WriteBusiness(
        string source,
        string message,
        string detail,
        string? context = null,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0,
        [CallerMemberName] string sourceMemberName = "");

    /// <summary>
    /// 写入已构造好的异常日志。
    /// </summary>
    void Write(ProgramExceptionLogEntry entry);

    /// <summary>
    /// 读取指定日期最近的异常日志。
    /// </summary>
    IReadOnlyList<ProgramExceptionLogEntry> GetByDate(DateTime date, int take = 500);

    /// <summary>
    /// 获取异常日志目录。
    /// </summary>
    string GetLogDirectory();
}
