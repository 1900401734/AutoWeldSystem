using AutoWeldSystem.Core.Interfaces.PLC;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 报警通知已读状态的进程级实现。
/// 主屏与扩展屏的监控视图共用同一实例；只在签名真正变化时触发事件，避免两个视图互相回写形成回环。
/// </summary>
public sealed class PlcAlarmAcknowledgementService : IPlcAlarmAcknowledgementService
{
    private readonly object _sync = new();
    private string? _dismissedNotificationSignature;
    private string? _dismissedSummarySignature;
    private int _attachedViewCount;

    public event EventHandler? Changed;

    public string? DismissedNotificationSignature
    {
        get
        {
            lock (_sync)
            {
                return _dismissedNotificationSignature;
            }
        }
    }

    public string? DismissedSummarySignature
    {
        get
        {
            lock (_sync)
            {
                return _dismissedSummarySignature;
            }
        }
    }

    public void Attach()
    {
        lock (_sync)
        {
            _attachedViewCount++;
        }
    }

    public void Detach()
    {
        bool shouldReset;
        lock (_sync)
        {
            _attachedViewCount = Math.Max(0, _attachedViewCount - 1);
            shouldReset = _attachedViewCount == 0;
        }

        // 最后一个监控视图销毁（例如注销）后清空已读状态，重新登录时仍在持续的报警会重新弹出，与改造前行为一致。
        if (shouldReset)
        {
            Reset();
        }
    }

    public void DismissNotification(string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);
        Update(signature, summarySignature: null, updateSummary: false);
    }

    public void DismissSummary(string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);
        Update(signature, signature, updateSummary: true);
    }

    public void Reset() => Update(null, null, updateSummary: true);

    private void Update(string? notificationSignature, string? summarySignature, bool updateSummary)
    {
        bool changed;
        lock (_sync)
        {
            changed = !string.Equals(_dismissedNotificationSignature, notificationSignature, StringComparison.Ordinal);
            _dismissedNotificationSignature = notificationSignature;
            if (updateSummary)
            {
                changed |= !string.Equals(_dismissedSummarySignature, summarySignature, StringComparison.Ordinal);
                _dismissedSummarySignature = summarySignature;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
