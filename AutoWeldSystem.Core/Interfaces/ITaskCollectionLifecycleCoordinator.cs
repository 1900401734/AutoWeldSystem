using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 采集准入与完工排空协调器。
/// 每个 PLC 副作用前按数据库复核任务仍未完工；完工与异常结束先关闭准入、等待在途周期结束再提交。
/// 只在本进程内协调；跨进程由单实例互斥体保证。
/// </summary>
public interface ITaskCollectionLifecycleCoordinator
{
    /// <summary>
    /// 受理一次采集周期。任务已完工、已作废、不存在或工位不属于该任务时抛出
    /// <see cref="Exceptions.TaskRunRejectedException"/>；数据库故障原样抛出，按普通采集失败处理。
    /// </summary>
    ITaskCollectionLease Accept(BizWeldTask task, int stationNo);

    /// <summary>
    /// 关闭该任务的采集准入，用于完工与异常结束；调用方必须等待排空后再提交终态。
    /// </summary>
    ITaskClosingLease BeginClosing(BizWeldTask task);

    /// <summary>
    /// 该任务是否仍有在途采集周期或正在关闭。
    /// </summary>
    bool HasActivity(int taskId);
}

/// <summary>
/// 一次已受理的采集周期凭据，持有期间完工必须等待；释放后才允许终态提交。
/// </summary>
public interface ITaskCollectionLease : IDisposable
{
    int TaskId { get; }

    int StationNo { get; }

    /// <summary>
    /// 受理时从数据库读取的任务快照，采集与保存只使用该快照。
    /// </summary>
    BizWeldTask TaskSnapshot { get; }

    /// <summary>
    /// 再次按数据库复核任务未完工且凭据仍有效；失败抛出 <see cref="Exceptions.TaskRunRejectedException"/>。
    /// </summary>
    void EnsureValid();
}

/// <summary>
/// 任务关闭凭据：持有期间新采集被拒绝；等待排空成功后方可提交终态。
/// </summary>
public interface ITaskClosingLease : IDisposable
{
    int TaskId { get; }

    /// <summary>
    /// 等待该任务全部在途采集周期释放；超时抛出 <see cref="TimeoutException"/>。
    /// </summary>
    Task WaitForDrainAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 复核任务仍未完工且本凭据仍有效；失败抛出 <see cref="Exceptions.TaskRunRejectedException"/>。
    /// </summary>
    void EnsureValid();
}
