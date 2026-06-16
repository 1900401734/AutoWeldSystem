using AutoWeldSystem.Core.Interfaces.Log;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// WinForms implementation that hides InvokeRequired/BeginInvoke checks and shutdown races.
/// </summary>
public sealed class WinFormsUiThreadDispatcher : IUiThreadDispatcher
{
    private readonly IProgramExceptionLogService? _exceptionLogService;

    public WinFormsUiThreadDispatcher(IProgramExceptionLogService? exceptionLogService = null)
    {
        _exceptionLogService = exceptionLogService;
    }

    public bool TryRun(Control owner, Action action, string source, bool requireHandle = true)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(action);

        if (!CanUseOwner(owner, requireHandle))
        {
            return false;
        }

        try
        {
            if (owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ExecuteSafely(owner, action, source, requireHandle)));
                return true;
            }

            return ExecuteSafely(owner, action, source, requireHandle);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task<bool> TryRunAsync(Control owner, Func<Task> action, string source, bool requireHandle = true)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(action);

        if (!CanUseOwner(owner, requireHandle))
        {
            return Task.FromResult(false);
        }

        try
        {
            if (owner.InvokeRequired)
            {
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                owner.BeginInvoke(new Action(async () =>
                {
                    var result = await ExecuteSafelyAsync(owner, action, source, requireHandle);
                    completion.TrySetResult(result);
                }));

                return completion.Task;
            }

            return ExecuteSafelyAsync(owner, action, source, requireHandle);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(false);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(false);
        }
    }

    private static bool CanUseOwner(Control owner, bool requireHandle)
    {
        return !owner.IsDisposed
            && (!requireHandle || owner.IsHandleCreated);
    }

    private bool ExecuteSafely(Control owner, Action action, string source, bool requireHandle)
    {
        if (!CanUseOwner(owner, requireHandle))
        {
            return false;
        }

        try
        {
            action();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Exception ex)
        {
            WriteException(ex, source);
            return false;
        }
    }

    private async Task<bool> ExecuteSafelyAsync(Control owner, Func<Task> action, string source, bool requireHandle)
    {
        if (!CanUseOwner(owner, requireHandle))
        {
            return false;
        }

        try
        {
            await action();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Exception ex)
        {
            WriteException(ex, source);
            return false;
        }
    }

    private void WriteException(Exception exception, string source)
    {
        _exceptionLogService?.Write(
            exception,
            string.IsNullOrWhiteSpace(source) ? "UI.ThreadDispatcher" : source);
    }
}
