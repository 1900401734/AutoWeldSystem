namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 配方号持续调和监控服务。
/// 服务在开工任务运行期间检查 PLC 侧配方号，发现被切换后自动写回当前任务配方号。
/// </summary>
public interface IPlcRecipeReconcileMonitorService : IAsyncDisposable
{
    /// <summary>
    /// 启动后台监控循环。
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止后台监控循环。
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
