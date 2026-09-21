using System.Runtime.CompilerServices;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 采集准入与完工排空。锁内只查数据库和登记凭据；网络、PLC、报表和事件都在锁外。
/// 只有数据库明确判定任务已结束才抛 <see cref="TaskRunRejectedException"/>；数据库故障原样抛出。
/// </summary>
public sealed class TaskCollectionLifecycleCoordinator : ITaskCollectionLifecycleCoordinator
{
    private static readonly ConditionalWeakTable<SqlSugarDbContext, TaskCollectionLifecycleCoordinator> Shared = new();
    private const string TaskStatusCompleted = "Completed";

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly TimeSpan _drainTimeout;
    private readonly Dictionary<int, CollectionLease> _activeByStation = new();
    private readonly Dictionary<int, ClosingLease> _closingByTask = new();

    public TaskCollectionLifecycleCoordinator(SqlSugarDbContext dbContext, IAppSettingsService settingsService,
        TimeSpan? drainTimeout = null)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 同一数据库上下文共用一个协调器，保证采集、完工在同一进程内看到同一份登记。
    /// </summary>
    public static TaskCollectionLifecycleCoordinator GetShared(SqlSugarDbContext dbContext, IAppSettingsService settingsService)
        => Shared.GetValue(dbContext, context => new TaskCollectionLifecycleCoordinator(context, settingsService));

    public ITaskCollectionLease Accept(BizWeldTask task, int stationNo)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            if (_closingByTask.ContainsKey(task.Id))
                throw Rejected(task.Id, "任务正在完工或结束，已关闭采集准入");
            // 同一工位同时只能有一个在途周期；这是调用方编排错误而非任务过期，不能按拒绝清空运行态。
            if (_activeByStation.ContainsKey(stationNo))
                throw new InvalidOperationException($"工位 {stationNo} 已有在途采集周期，本轮未受理。");
            var persisted = RequireUnfinished(task.Id, stationNo);
            var lease = new CollectionLease(this, persisted, stationNo);
            _activeByStation.Add(stationNo, lease);
            return lease;
        }
    }

    public ITaskClosingLease BeginClosing(BizWeldTask task)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            var persisted = RequireUnfinished(task.Id, task.StationNo);
            if (_closingByTask.ContainsKey(persisted.Id))
                throw Rejected(persisted.Id, "该任务已在完工或结束处理中，请等待本次操作结束");
            var closing = new ClosingLease(this, persisted.Id);
            _closingByTask.Add(persisted.Id, closing);
            SignalIfDrained(closing);
            return closing;
        }
    }

    public bool HasActivity(int taskId)
    {
        lock (_dbContext.TaskTransitionSync)
            return _activeByStation.Values.Any(lease => lease.TaskId == taskId) || _closingByTask.ContainsKey(taskId);
    }

    private BizWeldTask RequireUnfinished(int taskId, int stationNo)
    {
        _dbContext.InitDatabase();
        var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId);
        if (task is null || task.EndTime.HasValue || WeldTaskRuntimeRules.IsAbandoned(task)
            || string.Equals(task.TaskStatus, TaskStatusCompleted, StringComparison.OrdinalIgnoreCase))
            throw Rejected(taskId, "任务已完工、已作废或不存在");
        var settings = _settingsService.Get();
        var scope = RecipeStationScopeRules.ResolveSharedTaskStations(settings.EnableDualStation, settings.EnableDualWorkOrder, task.StationNo);
        if (!scope.Contains(stationNo))
            throw Rejected(taskId, $"工位 {stationNo} 不属于该任务");
        return task;
    }

    private void Validate(CollectionLease lease)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            if (!_activeByStation.TryGetValue(lease.StationNo, out var current) || !ReferenceEquals(current, lease))
                throw Rejected(lease.TaskId, "采集凭据已释放");
            RequireUnfinished(lease.TaskId, lease.StationNo);
        }
    }

    private void Validate(ClosingLease lease)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            if (!_closingByTask.TryGetValue(lease.TaskId, out var current) || !ReferenceEquals(current, lease))
                throw Rejected(lease.TaskId, "完工凭据已释放");
            var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(lease.TaskId);
            RequireUnfinished(lease.TaskId, task?.StationNo ?? 1);
        }
    }

    private void Release(CollectionLease lease)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            if (_activeByStation.TryGetValue(lease.StationNo, out var current) && ReferenceEquals(current, lease))
                _activeByStation.Remove(lease.StationNo);
            if (_closingByTask.TryGetValue(lease.TaskId, out var closing)) SignalIfDrained(closing);
        }
    }

    private void Release(ClosingLease lease)
    {
        lock (_dbContext.TaskTransitionSync)
        {
            if (_closingByTask.TryGetValue(lease.TaskId, out var current) && ReferenceEquals(current, lease))
                _closingByTask.Remove(lease.TaskId);
        }
    }

    private void SignalIfDrained(ClosingLease closing)
    {
        if (!_activeByStation.Values.Any(lease => lease.TaskId == closing.TaskId)) closing.Drained.TrySetResult();
    }

    private static TaskRunRejectedException Rejected(int taskId, string reason)
        => new($"任务 {taskId}：{reason}；未执行本轮 PLC 与数据库操作。");

    private sealed class CollectionLease(TaskCollectionLifecycleCoordinator owner, BizWeldTask snapshot, int stationNo)
        : ITaskCollectionLease
    {
        public int TaskId { get; } = snapshot.Id;
        public int StationNo { get; } = stationNo;
        public BizWeldTask TaskSnapshot { get; } = snapshot;
        public void EnsureValid() => owner.Validate(this);
        public void Dispose() => owner.Release(this);
    }

    private sealed class ClosingLease(TaskCollectionLifecycleCoordinator owner, int taskId) : ITaskClosingLease
    {
        public int TaskId { get; } = taskId;
        public TaskCompletionSource Drained { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitForDrainAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Drained.Task.WaitAsync(owner._drainTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException("当前产品的采集或 PLC 反馈尚未处理完，本次未完工；请等待其结束后重试。");
            }
            EnsureValid();
        }

        public void EnsureValid() => owner.Validate(this);
        public void Dispose() => owner.Release(this);
    }
}
