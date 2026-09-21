namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Provides the dispatcher to base controls that cannot use constructor injection.
/// </summary>
public static class UiThreadDispatcherProvider
{
    private static readonly IUiThreadDispatcher FallbackDispatcher = new WinFormsUiThreadDispatcher();
    private static IUiThreadDispatcher? _dispatcher;

    public static IUiThreadDispatcher Current => Volatile.Read(ref _dispatcher) ?? FallbackDispatcher;

    public static void Configure(IUiThreadDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        Volatile.Write(ref _dispatcher, dispatcher);
    }
}
