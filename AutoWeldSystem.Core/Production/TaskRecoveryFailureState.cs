using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 仅抑制自动恢复的重复尝试与日志；生产放行校验不使用此缓存。
/// </summary>
public sealed class TaskRecoveryFailureState
{
    private int _taskId;
    private string? _content;
    private string _mode = string.Empty;
    private DateTime _lastAttempt;
    public string? Error { get; private set; }

    public bool ShouldAttempt(BizWeldTask task, string mode, DateTime now)
    {
        if (Error is not null && _taskId == task.Id && _content == task.ProgramContentSnapshot
            && _mode == mode && now - _lastAttempt < TimeSpan.FromSeconds(10)) return false;
        if (_taskId != task.Id || _content != task.ProgramContentSnapshot || _mode != mode) Error = null;
        _taskId = task.Id;
        _content = task.ProgramContentSnapshot;
        _mode = mode;
        _lastAttempt = now;
        return true;
    }

    public bool RecordFailure(string error)
    {
        var changed = !string.Equals(Error, error, StringComparison.Ordinal);
        Error = error;
        return changed;
    }
}
