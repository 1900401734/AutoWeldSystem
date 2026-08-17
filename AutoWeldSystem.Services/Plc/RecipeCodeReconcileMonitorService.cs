using System.Globalization;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 配方号持续调和监控服务。
/// 开工任务运行期间，服务会持续比较当前任务配方号和 PLC 回读配方号，发现 PLC 被切换后自动写回任务配方号。
/// </summary>
public sealed class RecipeCodeReconcileMonitorService : IPlcRecipeReconcileMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReconcileTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RestoreAttemptInterval = TimeSpan.FromSeconds(10);

    private readonly IAppSettingsService _settingsService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcBusinessSignalService _plcBusinessSignalService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProgramManageService _programManageService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly object _stateSync = new();
    private readonly Dictionary<int, StationRecipeReconcileState> _stationStates = new();
    private readonly Dictionary<int, PlcRecipeCodeSnapshot> _recipeSnapshots = new();
    private readonly HashSet<int> _restoredTaskIds = new();
    private IReadOnlyList<BizProgram> _localProgramSnapshot = Array.Empty<BizProgram>();
    private AppSettings _currentSettings;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public RecipeCodeReconcileMonitorService(
        IAppSettingsService settingsService,
        IPlcCommunicationService plcCommunicationService,
        IPlcBusinessSignalService plcBusinessSignalService,
        IWeldTaskService weldTaskService,
        IProgramManageService programManageService,
        IProductionFlowLogService productionLogService,
        IProgramExceptionLogService exceptionLogService)
    {
        _settingsService = settingsService;
        _plcCommunicationService = plcCommunicationService;
        _plcBusinessSignalService = plcBusinessSignalService;
        _weldTaskService = weldTaskService;
        _programManageService = programManageService;
        _productionLogService = productionLogService;
        _exceptionLogService = exceptionLogService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    /// <summary>
    /// Raised when the latest PLC recipe snapshot for a station changes.
    /// </summary>
    public event EventHandler<PlcRecipeCodeSnapshot>? RecipeCodeChanged;

    /// <summary>
    /// Gets the latest PLC recipe snapshot for a station.
    /// </summary>
    /// <param name="stationNo">Station number.</param>
    /// <returns>Latest known snapshot or a failed placeholder when no read has completed yet.</returns>
    public PlcRecipeCodeSnapshot GetCurrent(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_stateSync)
        {
            return _recipeSnapshots.TryGetValue(normalizedStationNo, out var snapshot)
                ? snapshot
                : PlcRecipeCodeSnapshot.Failed(normalizedStationNo, "PLC recipe has not been read.");
        }
    }

    /// <summary>
    /// 启动后台监控循环。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止后台监控循环。
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_loopTask is null)
        {
            return;
        }

        try
        {
            await _loopTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch
        {
            // 后台监控停止失败不应阻塞程序退出。
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        _cts?.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// 后台循环按固定间隔扫描当前运行任务。
    /// </summary>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(cancellationToken);
                await Task.Delay(PollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                WriteBusinessFailureLog(ProductionConstants.Stations.DefaultStationNo, "PLC配方号持续调和监控失败", ex.Message);
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 扫描所有运行中的工位任务。
    /// </summary>
    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var settings = CurrentSettings;
        _localProgramSnapshot = (await _programManageService.GetProgramLookupsAsync(cancellationToken))
            .Select(lookup => lookup.ToEntityStub())
            .ToArray();
        var monitorStations = RecipeStationScopeRules.ResolveMonitorStations(settings.EnableDualStation);
        var activeTasks = GetRunningStationTasks(settings)
            .GroupBy(task => NormalizeStationNo(task.StationNo))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var stationNo in monitorStations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedStationNo = NormalizeStationNo(stationNo);

            if (!activeTasks.TryGetValue(normalizedStationNo, out var activeTask))
            {
                await PollIdleStationRecipeAsync(normalizedStationNo, cancellationToken);
                continue;
            }

            if (!settings.ValidateRecipeAfterStart)
            {
                ResetStationMismatch(normalizedStationNo);
                continue;
            }

            await PollStationAsync(settings, activeTask, cancellationToken);
        }
    }

    /// <summary>
    /// 检查单个工位的 PLC 配方号，必要时执行调和。
    /// </summary>
    private async Task PollStationAsync(AppSettings settings, ActiveRecipeTask activeTask, CancellationToken cancellationToken)
    {
        var stationNo = NormalizeStationNo(activeTask.StationNo);
        var task = activeTask.Task;
        var plcConnected = _plcCommunicationService.GetCurrent(stationNo).IsConnected;
        if (!plcConnected)
        {
            ResetStationMismatch(stationNo);
            return;
        }

        var expectedRecipe = ResolveExpectedRecipe(task, stationNo);
        if (string.IsNullOrWhiteSpace(expectedRecipe))
        {
            ResetStationMismatch(stationNo);
            return;
        }

        var workOrderStatus = await ReadWorkOrderStatusAsync(stationNo, cancellationToken);
        var readResult = await _plcBusinessSignalService.ReadTextAsync(
            AppConstants.PlcLogicalKeys.PlcRecipeCode,
            stationNo,
            cancellationToken);
        if (!readResult.IsSuccess)
        {
            PublishRecipeReadFailure(stationNo, readResult.Message);
            ResetStationMismatch(stationNo);
            WriteBusinessFailureLog(stationNo, "PLC recipe code read failed", readResult.Message);
            return;
        }

        PublishRecipeSnapshot(PlcRecipeCodeSnapshot.Success(stationNo, NormalizeRecipeCode(readResult.Value)));

        var decision = RecipeCodeReconcileRules.Decide(
            settings.ValidateRecipeAfterStart,
            plcConnected,
            hasRunningTask: true,
            expectedRecipe,
            readResult.Value,
            workOrderStatus);
        if (!decision.ShouldReconcile)
        {
            ResetStationMismatch(stationNo);
            return;
        }

        await ReconcileRecipeAsync(settings, stationNo, task, decision, readResult, cancellationToken);
    }

    /// <summary>
    /// Reads the PLC recipe code for an idle station and publishes it as a UI snapshot.
    /// Idle stations never write PC recipe codes because there is no running task to reconcile against.
    /// </summary>
    private async Task PollIdleStationRecipeAsync(int stationNo, CancellationToken cancellationToken)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        ResetStationMismatch(normalizedStationNo);

        if (!_plcCommunicationService.GetCurrent(normalizedStationNo).IsConnected)
        {
            PublishRecipeReadFailure(normalizedStationNo, "PLC is not connected.");
            return;
        }

        var readResult = await _plcBusinessSignalService.ReadTextAsync(
            AppConstants.PlcLogicalKeys.PlcRecipeCode,
            normalizedStationNo,
            cancellationToken);
        if (!readResult.IsSuccess)
        {
            PublishRecipeReadFailure(normalizedStationNo, readResult.Message);
            return;
        }

        PublishRecipeSnapshot(PlcRecipeCodeSnapshot.Success(
            normalizedStationNo,
            NormalizeRecipeCode(readResult.Value)));
    }

    /// <summary>
    /// 将任务配方号写回 PLC，并把检测到的切换和调和结果写入生产流程日志。
    /// </summary>
    private async Task ReconcileRecipeAsync(
        AppSettings settings,
        int stationNo,
        BizWeldTask task,
        RecipeCodeReconcileDecision decision,
        PlcBusinessSignalResult readResult,
        CancellationToken cancellationToken)
    {
        var state = GetStationState(stationNo);
        var mismatchKey = $"{task.Id}|{decision.ExpectedRecipeCode}|{decision.PlcRecipeCode}";
        var isNewMismatch = !string.Equals(state.LastMismatchKey, mismatchKey, StringComparison.Ordinal);
        if (!isNewMismatch && DateTime.Now < state.NextRetryTime)
        {
            // 上一次调和失败后等待冷却时间，避免同一错误持续刷生产流程日志和异常日志。
            return;
        }

        if (isNewMismatch)
        {
            state.LastMismatchKey = mismatchKey;
            state.NextRetryTime = default;
            WriteRecipeFlowLog(
                "RecipeCodeChangedDetected",
                ProductionFlowLogTexts.Summaries.FormatRecipeCodeChanged(decision.PlcRecipeCode),
                $"Station={stationNo}; TaskId={task.Id}; ExpectedRecipeCode={decision.ExpectedRecipeCode}; PlcRecipeCode={decision.PlcRecipeCode}",
                stationNo,
                task,
                level: "Info",
                plcSignal: AppConstants.PlcLogicalKeys.PlcRecipeCode,
                plcAddress: readResult.Address);
        }

        var targetStations = RecipeStationScopeRules.ResolveSharedRecipeStations(
            settings.EnableDualStation,
            settings.EnableDualWorkOrder,
            stationNo);
        var localProgram = ResolveLocalProgram(task);
        var recipeTargets = ProgramRecipeMappingRules.ResolveTargets(localProgram, targetStations);
        if (recipeTargets.Any(target => string.IsNullOrWhiteSpace(target.RecipeCode)))
        {
            var missingStations = string.Join(",", recipeTargets
                .Where(target => string.IsNullOrWhiteSpace(target.RecipeCode))
                .Select(target => target.StationNo));
            WriteBusinessFailureLog(
                stationNo,
                "PLC recipe reconcile skipped because local station recipe is missing.",
                $"TaskId={task.Id}; MissingStations={missingStations}");
            state.NextRetryTime = DateTime.Now + BusinessLogInterval;
            return;
        }

        foreach (var target in recipeTargets)
        {
            var targetStationNo = target.StationNo;
            var targetExpectedRecipe = target.RecipeCode;

            var syncResult = await _plcBusinessSignalService.SyncRecipeCodeAsync(
                targetStationNo,
                targetExpectedRecipe,
                ReconcileTimeout,
                cancellationToken);
            if (syncResult.IsSuccess)
            {
                WriteRecipeFlowLog(
                    "RecipeCodeReconcileSucceeded",
                    ProductionFlowLogTexts.Summaries.FormatRecipeCodeReconcileSucceeded(syncResult.PcRecipeCode),
                    $"Station={targetStationNo}; SourceStation={stationNo}; TaskId={task.Id}; ChangedPlcRecipeCode={decision.PlcRecipeCode}; ExpectedRecipeCode={targetExpectedRecipe}; SyncedPlcRecipeCode={syncResult.PlcRecipeCode}",
                    targetStationNo,
                    task,
                    level: "Info",
                    plcSignal: AppConstants.PlcLogicalKeys.PlcRecipeCode,
                    plcAddress: readResult.Address);
                continue;
            }

            var currentPlcRecipe = string.IsNullOrWhiteSpace(syncResult.PlcRecipeCode)
                ? decision.PlcRecipeCode
                : syncResult.PlcRecipeCode;
            WriteRecipeFlowLog(
                "RecipeCodeReconcileFailed",
                ProductionFlowLogTexts.Summaries.RecipeCodeReconcileFailed,
                $"Station={targetStationNo}; SourceStation={stationNo}; TaskId={task.Id}; ChangedPlcRecipeCode={decision.PlcRecipeCode}; ExpectedRecipeCode={targetExpectedRecipe}; PlcRecipeCode={currentPlcRecipe}; Detail={syncResult.Message}",
                targetStationNo,
                task,
                level: "Error",
                plcSignal: AppConstants.PlcLogicalKeys.PlcRecipeCode,
                plcAddress: readResult.Address);

            WriteBusinessFailureLog(
                targetStationNo,
                ProductionFlowLogTexts.Summaries.RecipeCodeReconcileFailed,
                $"Station={targetStationNo}; SourceStation={stationNo}; TaskId={task.Id}; Expected={targetExpectedRecipe}; PLC={currentPlcRecipe}; Detail={syncResult.Message}");
            state.NextRetryTime = DateTime.Now + BusinessLogInterval;
            return;
        }

        state.LastMismatchKey = string.Empty;
        state.LastFailureKey = string.Empty;
        state.NextRetryTime = default;
        return;

    }

    /// <summary>
    /// 读取 PLC 工单状态。读取失败时返回 null，由调和规则按“未读到完工状态”处理。
    /// </summary>
    private async Task<int?> ReadWorkOrderStatusAsync(int stationNo, CancellationToken cancellationToken)
    {
        var result = await _plcBusinessSignalService.ReadTextAsync(
            AppConstants.PlcLogicalKeys.WorkOrderStatus,
            stationNo,
            cancellationToken);
        return result.IsSuccess
            && int.TryParse(result.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var status)
                ? status
                : null;
    }

    /// <summary>
    /// 获取当前运行中的工位任务，只使用内存运行态，避免后台服务每秒查询数据库。
    /// </summary>
    private IReadOnlyList<ActiveRecipeTask> GetRunningStationTasks(AppSettings settings)
    {
        var runtimeTasks = GetRunningStationTasksFromRuntime();
        var monitorStations = ResolveMonitorStationNumbers(settings);
        var restoredTask = false;

        foreach (var stationNo in monitorStations)
        {
            if (runtimeTasks.Any(task => task.StationNo == stationNo))
            {
                continue;
            }

            restoredTask |= TryRestoreRunningTask(stationNo);
        }

        return restoredTask
            ? GetRunningStationTasksFromRuntime()
            : runtimeTasks;
    }

    /// <summary>
    /// Gets running station tasks from the in-memory runtime state only.
    /// </summary>
    private IReadOnlyList<ActiveRecipeTask> GetRunningStationTasksFromRuntime()
    {
        var runtimeState = _weldTaskService.CurrentState;
        var stationTasks = runtimeState.StationStates.Values
            .Where(station => IsRunningTask(station.ActiveTask))
            .Select(station => new ActiveRecipeTask(NormalizeStationNo(station.StationNo), station.ActiveTask!))
            .GroupBy(activeTask => activeTask.StationNo)
            .Select(group => group.First())
            .OrderBy(activeTask => activeTask.StationNo)
            .ToList();

        if (stationTasks.Count > 0)
        {
            return stationTasks;
        }

        return IsRunningTask(runtimeState.ActiveTask)
            ? [new ActiveRecipeTask(NormalizeStationNo(runtimeState.CurrentStationNo), runtimeState.ActiveTask!)]
            : [];
    }

    /// <summary>
    /// Restores an unfinished task for a station with a small cooldown to avoid polling the database every second.
    /// </summary>
    private bool TryRestoreRunningTask(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var state = GetStationState(normalizedStationNo);
        var now = DateTime.Now;
        if (now - state.LastRestoreAttemptTime < RestoreAttemptInterval)
        {
            return false;
        }

        state.LastRestoreAttemptTime = now;
        try
        {
            var restoredTask = _weldTaskService.RestoreUnfinishedTask(normalizedStationNo);
            if (!IsRunningTask(restoredTask))
            {
                return false;
            }

            lock (_stateSync)
            {
                _restoredTaskIds.Add(restoredTask!.Id);
            }
            return true;
        }
        catch (Exception ex)
        {
            WriteBusinessFailureLog(
                normalizedStationNo,
                "PLC recipe task restore failed.",
                $"Station={normalizedStationNo}; Detail={ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Resolves the stations that need recipe monitoring under the current station mode.
    /// </summary>
    private static int[] ResolveMonitorStationNumbers(AppSettings settings)
    {
        return RecipeStationScopeRules.ResolveMonitorStations(settings.EnableDualStation);
    }

    /// <summary>
    /// 判断任务是否处于运行中且未完工。
    /// </summary>
    private static bool IsRunningTask(BizWeldTask? task)
    {
        return task is not null
            && task.EndTime is null
            && string.Equals(task.TaskStatus, ProductionConstants.ProductInstanceStatuses.Running, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 从本地程序记录解析指定工位的期望配方号；找不到程序时保留任务字段作为旧数据回退。
    /// </summary>
    private string ResolveExpectedRecipe(BizWeldTask task, int stationNo)
    {
        var mappedRecipeCode = ProgramRecipeMappingRules.Resolve(ResolveLocalProgram(task), stationNo);
        if (!string.IsNullOrWhiteSpace(mappedRecipeCode))
        {
            return mappedRecipeCode;
        }

        lock (_stateSync)
        {
            return _restoredTaskIds.Contains(task.Id) && stationNo == task.StationNo
                ? NormalizeRecipeCode(task.RecipeCode)
                : string.Empty;
        }
    }

    private BizProgram? ResolveLocalProgram(BizWeldTask task)
    {
        var programs = _localProgramSnapshot;
        var programId = task.ProgramId?.Trim() ?? string.Empty;
        if (programId.StartsWith("local-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(programId["local-".Length..], out var localProgramId))
        {
            var byLocalId = programs.FirstOrDefault(program => program.Id == localProgramId);
            if (byLocalId is not null)
            {
                return byLocalId;
            }
        }

        var byProgramId = programs
            .Where(program => !string.IsNullOrWhiteSpace(programId) && SameText(program.ProgramId, programId))
            .OrderByDescending(program => SameText(program.DeviceId, task.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
        if (byProgramId is not null)
        {
            return byProgramId;
        }

        return programs
            .Where(program => SameText(program.ProgramName, task.ProgramName))
            .Where(program => string.IsNullOrWhiteSpace(task.ProductNum) || SameText(program.ProductNum, task.ProductNum))
            .OrderByDescending(program => SameText(program.DeviceId, task.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 写入配方相关生产流程日志，MonitorView 会订阅该日志并刷新提示区。
    /// </summary>
    private void WriteRecipeFlowLog(
        string step,
        string summary,
        string detail,
        int stationNo,
        BizWeldTask task,
        string level,
        string plcSignal,
        string plcAddress)
    {
        _productionLogService.Write(
            step,
            summary,
            detail,
            level,
            stationNo,
            task.SN,
            task.ProductNum ?? string.Empty,
            task.ProgramId ?? string.Empty,
            plcSignal,
            plcAddress);
    }

    /// <summary>
    /// 写入节流后的业务异常日志，避免 PLC 异常持续存在时刷屏。
    /// </summary>
    private void WriteBusinessFailureLog(int stationNo, string summary, string detail)
    {
        var state = GetStationState(stationNo);
        var failureKey = $"{stationNo}|{summary}|{detail}";
        if (!ShouldWriteFailureLog(state, failureKey))
        {
            return;
        }

        _exceptionLogService.WriteBusiness(
            "PLC.RecipeCodeReconcile",
            summary,
            detail,
            $"开工状态配方持续调和失败。Station={stationNo}");
    }

    /// <summary>
    /// 判断当前失败是否达到日志冷却时间。
    /// </summary>
    private static bool ShouldWriteFailureLog(StationRecipeReconcileState state, string failureKey)
    {
        var now = DateTime.Now;
        if (string.Equals(state.LastFailureKey, failureKey, StringComparison.Ordinal)
            && now - state.LastFailureLogTime < BusinessLogInterval)
        {
            return false;
        }

        state.LastFailureKey = failureKey;
        state.LastFailureLogTime = now;
        return true;
    }

    /// <summary>
    /// 获取或创建工位调和运行态。
    /// </summary>
    private StationRecipeReconcileState GetStationState(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_stateSync)
        {
            if (!_stationStates.TryGetValue(normalizedStationNo, out var state))
            {
                state = new StationRecipeReconcileState();
                _stationStates[normalizedStationNo] = state;
            }

            return state;
        }
    }

    /// <summary>
    /// 清理所有工位调和状态。
    /// </summary>
    private void ClearStationStates()
    {
        lock (_stateSync)
        {
            foreach (var state in _stationStates.Values)
            {
                state.LastMismatchKey = string.Empty;
                state.NextRetryTime = default;
            }
        }
    }

    /// <summary>
    /// 清理指定工位的当前不一致状态。
    /// </summary>
    private void ResetStationMismatch(int stationNo)
    {
        var state = GetStationState(stationNo);
        state.LastMismatchKey = string.Empty;
    }

    /// <summary>
    /// Publishes a changed PLC recipe snapshot to UI subscribers.
    /// </summary>
    private void PublishRecipeSnapshot(PlcRecipeCodeSnapshot snapshot)
    {
        lock (_stateSync)
        {
            if (_recipeSnapshots.TryGetValue(snapshot.StationNo, out var previous)
                && previous.IsSuccess == snapshot.IsSuccess
                && string.Equals(previous.RecipeCode, snapshot.RecipeCode, StringComparison.Ordinal)
                && string.Equals(previous.Message, snapshot.Message, StringComparison.Ordinal))
            {
                return;
            }

            _recipeSnapshots[snapshot.StationNo] = snapshot;
        }

        RecipeCodeChanged?.Invoke(this, snapshot);
    }

    /// <summary>
    /// Publishes a failed snapshot only when no previous successful recipe is available.
    /// </summary>
    private void PublishRecipeReadFailure(int stationNo, string message)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_stateSync)
        {
            if (_recipeSnapshots.TryGetValue(normalizedStationNo, out var previous)
                && previous.IsSuccess)
            {
                return;
            }
        }

        PublishRecipeSnapshot(PlcRecipeCodeSnapshot.Failed(normalizedStationNo, message));
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private static string NormalizeRecipeCode(string? value)
        => ProgramRecipeMappingRules.Normalize(value);

    private static string FirstNonEmpty(params string?[] values)
        => values
            .Select(NormalizeRecipeCode)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? string.Empty;

    private static bool SameText(string? left, string? right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;

    private sealed record ActiveRecipeTask(int StationNo, BizWeldTask Task);

    private sealed class StationRecipeReconcileState
    {
        public string LastMismatchKey { get; set; } = string.Empty;

        public string LastFailureKey { get; set; } = string.Empty;

        public DateTime LastFailureLogTime { get; set; }

        public DateTime NextRetryTime { get; set; }

        public DateTime LastRestoreAttemptTime { get; set; }
    }
}
