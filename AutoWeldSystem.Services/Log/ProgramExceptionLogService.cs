using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// 程序异常日志文件服务。
/// </summary>
public sealed class ProgramExceptionLogService : IProgramExceptionLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IAppSettingsService _settingsService;
    private readonly object _writeLock = new();
    private AppSettings _currentSettings;

    public ProgramExceptionLogService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    public event EventHandler<ProgramExceptionLogEntry>? LogWritten;

    public ProgramExceptionLogEntry Write(Exception exception, string source, string? context = null)
    {
        var entry = CreateEntry(exception, source, context);
        Write(entry);
        return entry;
    }

    public ProgramExceptionLogEntry WriteBusiness(
        string source,
        string message,
        string detail,
        string? context = null,
        string sourceFilePath = "",
        int sourceLineNumber = 0,
        string sourceMemberName = "")
    {
        var entry = CreateBusinessEntry(source, message, detail, context, sourceFilePath, sourceLineNumber, sourceMemberName);
        Write(entry);
        return entry;
    }

    public void Write(ProgramExceptionLogEntry entry)
    {
        try
        {
            var filePath = GetLogFilePath(entry.OccurredTime);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = JsonSerializer.Serialize(entry, JsonOptions);

            lock (_writeLock)
            {
                File.AppendAllText(filePath, json + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }

            LogWritten?.Invoke(this, entry);
        }
        catch
        {
            // 异常日志写入失败时不能再次抛出，避免日志系统把原始异常覆盖掉。
        }
    }

    public IReadOnlyList<ProgramExceptionLogEntry> GetByDate(DateTime date, int take = 500)
    {
        try
        {
            var filePath = GetLogFilePath(date);
            if (!File.Exists(filePath))
            {
                return Array.Empty<ProgramExceptionLogEntry>();
            }

            return ReadLatestRecords(filePath, Math.Max(1, take))
                .Reverse()
                .Select(TryDeserialize)
                .Where(entry => entry is not null)
                .Cast<ProgramExceptionLogEntry>()
                .ToList();
        }
        catch
        {
            return Array.Empty<ProgramExceptionLogEntry>();
        }
    }

    public string GetLogDirectory()
    {
        var root = CurrentSettings.LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.ProgramException);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private static ProgramExceptionLogEntry CreateEntry(Exception exception, string source, string? context)
    {
        var frameInfo = GetBestSourceFrame(exception);
        var entryAssembly = Assembly.GetEntryAssembly();

        return new ProgramExceptionLogEntry
        {
            TraceId = Guid.NewGuid().ToString("N"),
            OccurredTime = DateTime.Now,
            Category = AppConstants.ExceptionLogCategories.Program,
            Source = source,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            SourceFilePath = frameInfo.FilePath,
            SourceLineNumber = frameInfo.LineNumber,
            SourceMemberName = frameInfo.MemberName,
            TargetSite = exception.TargetSite?.ToString() ?? string.Empty,
            ThreadId = Environment.CurrentManagedThreadId,
            ThreadName = Thread.CurrentThread.Name ?? string.Empty,
            UserName = Environment.UserName,
            MachineName = Environment.MachineName,
            ApplicationVersion = entryAssembly?.GetName().Version?.ToString() ?? string.Empty,
            Context = context ?? string.Empty,
            StackTrace = exception.ToString(),
            InnerException = exception.InnerException?.ToString() ?? string.Empty
        };
    }

    private static ProgramExceptionLogEntry CreateBusinessEntry(
        string source,
        string message,
        string detail,
        string? context,
        string sourceFilePath,
        int sourceLineNumber,
        string sourceMemberName)
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        return new ProgramExceptionLogEntry
        {
            TraceId = Guid.NewGuid().ToString("N"),
            OccurredTime = DateTime.Now,
            Severity = "Warning",
            Category = AppConstants.ExceptionLogCategories.Business,
            Source = source,
            ExceptionType = AppConstants.ExceptionLogCategories.Business,
            Message = message,
            SourceFilePath = sourceFilePath,
            SourceLineNumber = sourceLineNumber,
            SourceMemberName = sourceMemberName,
            ThreadId = Environment.CurrentManagedThreadId,
            ThreadName = Thread.CurrentThread.Name ?? string.Empty,
            UserName = Environment.UserName,
            MachineName = Environment.MachineName,
            ApplicationVersion = entryAssembly?.GetName().Version?.ToString() ?? string.Empty,
            Context = BuildBusinessContext(detail, context)
        };
    }

    private static string BuildBusinessContext(string detail, string? context)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(detail))
        {
            builder.AppendLine("Detail:");
            builder.AppendLine(detail.Trim());
        }

        if (!string.IsNullOrWhiteSpace(context))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine("Context:");
            builder.AppendLine(context.Trim());
        }

        return builder.ToString();
    }

    private string GetLogFilePath(DateTime date)
    {
        return Path.Combine(GetLogDirectory(), $"{date:yyyy-MM-dd}.jsonl");
    }

    private static SourceFrameInfo GetBestSourceFrame(Exception exception)
    {
        var trace = new StackTrace(exception, true);
        var frames = trace.GetFrames() ?? Array.Empty<StackFrame>();
        var frame = frames.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.GetFileName()))
            ?? frames.FirstOrDefault();

        if (frame is null)
        {
            return new SourceFrameInfo(string.Empty, 0, string.Empty);
        }

        var method = frame.GetMethod();
        var memberName = method is null
            ? string.Empty
            : $"{method.DeclaringType?.FullName}.{method.Name}".Trim('.');

        return new SourceFrameInfo(
            frame.GetFileName() ?? string.Empty,
            frame.GetFileLineNumber(),
            memberName);
    }

    private static IEnumerable<string> ReadLatestRecords(string filePath, int take)
    {
        var records = new Queue<string>(take);

        foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (records.Count >= take)
            {
                records.Dequeue();
            }

            records.Enqueue(line);
        }

        return records;
    }

    private static ProgramExceptionLogEntry? TryDeserialize(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ProgramExceptionLogEntry>(line, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed record SourceFrameInfo(string FilePath, int LineNumber, string MemberName);
}
