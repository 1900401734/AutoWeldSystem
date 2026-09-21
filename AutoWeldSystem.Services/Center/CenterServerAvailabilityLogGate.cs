namespace AutoWeldSystem.Services.Center;

/// <summary>
/// Aggregates repeated center-server connectivity failures across all client instances.
/// </summary>
public sealed class CenterServerAvailabilityLogGate
{
    internal static readonly TimeSpan SummaryInterval = TimeSpan.FromMinutes(10);

    private readonly object _sync = new();
    private bool _isUnavailable;
    private DateTime _outageStartedAt;
    private DateTime _lastLogAt;
    private long _failureCount;

    public FailureLogDecision RegisterFailure(DateTime occurredAt)
    {
        lock (_sync)
        {
            if (!_isUnavailable)
            {
                _isUnavailable = true;
                _outageStartedAt = occurredAt;
                _lastLogAt = occurredAt;
                _failureCount = 1;
                return new FailureLogDecision(true, true, _failureCount, TimeSpan.Zero);
            }

            _failureCount++;
            var outageDuration = occurredAt - _outageStartedAt;
            if (occurredAt - _lastLogAt < SummaryInterval)
            {
                return new FailureLogDecision(false, false, _failureCount, outageDuration);
            }

            _lastLogAt = occurredAt;
            return new FailureLogDecision(true, false, _failureCount, outageDuration);
        }
    }

    public bool RegisterReachable()
    {
        lock (_sync)
        {
            if (!_isUnavailable)
            {
                return false;
            }

            _isUnavailable = false;
            _outageStartedAt = default;
            _lastLogAt = default;
            _failureCount = 0;
            return true;
        }
    }

    internal static bool IsConnectivityFailure(Exception exception, CancellationToken callerToken)
    {
        if (exception is OperationCanceledException)
        {
            return !callerToken.IsCancellationRequested;
        }

        return exception is HttpRequestException
            || exception.InnerException is not null
                && IsConnectivityFailure(exception.InnerException, callerToken);
    }

    public readonly record struct FailureLogDecision(
        bool ShouldWrite,
        bool IsFirstFailure,
        long FailureCount,
        TimeSpan OutageDuration);
}
