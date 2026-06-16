namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Centralizes safe marshaling from background service events back to the WinForms UI thread.
/// </summary>
public interface IUiThreadDispatcher
{
    bool TryRun(Control owner, Action action, string source, bool requireHandle = true);

    Task<bool> TryRunAsync(Control owner, Func<Task> action, string source, bool requireHandle = true);
}
