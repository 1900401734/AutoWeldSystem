using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;
using System.Globalization;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 产品焊点实时预览服务。
/// 该服务按固定周期读取 PLC；产品编号则由产品就绪上升沿门控，避免采集完成后提前跳到下一号。
/// </summary>
public sealed class ProductRealtimePreviewService : IProductRealtimePreviewService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProgramManageService _programManageService;
    private readonly IAppSettingsService _settingsService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IProductCycleCollectionService _productCycleCollectionService;
    private readonly IPlcWeldCycleMonitorService? _plcWeldCycleMonitorService;
    private readonly object _snapshotSync = new();
    private readonly Dictionary<int, ProductRealtimePreviewSnapshot> _snapshots = new();
    private readonly Dictionary<int, ProductNumberPreviewGate> _productNumberGates = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public ProductRealtimePreviewService(
        IWeldTaskService weldTaskService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProgramManageService programManageService,
        IAppSettingsService settingsService,
        IPlcCommunicationService plcCommunicationService,
        IPlcExpressionReadService plcExpressionReadService,
        IProgramExceptionLogService exceptionLogService,
        IProductCycleCollectionService productCycleCollectionService,
        IPlcWeldCycleMonitorService? plcWeldCycleMonitorService = null)
    {
        _weldTaskService = weldTaskService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _programManageService = programManageService;
        _settingsService = settingsService;
        _plcCommunicationService = plcCommunicationService;
        _plcExpressionReadService = plcExpressionReadService;
        _exceptionLogService = exceptionLogService;
        _productCycleCollectionService = productCycleCollectionService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        if (_plcWeldCycleMonitorService is not null)
        {
            _plcWeldCycleMonitorService.ProductReady += PlcWeldCycleMonitorService_ProductReady;
            _plcWeldCycleMonitorService.WeldPointCollected += PlcWeldCycleMonitorService_WeldPointCollected;
        }
    }

    public event EventHandler<ProductRealtimePreviewSnapshot>? SnapshotChanged;

    public ProductRealtimePreviewSnapshot? GetCurrent(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_snapshotSync)
        {
            return _snapshots.TryGetValue(normalizedStationNo, out var snapshot)
                ? snapshot
                : null;
        }
    }

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
            // 实时预览是辅助显示，停止失败不应阻塞程序退出。
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

        _cts?.Dispose();
        if (_plcWeldCycleMonitorService is not null)
        {
            _plcWeldCycleMonitorService.ProductReady -= PlcWeldCycleMonitorService_ProductReady;
            _plcWeldCycleMonitorService.WeldPointCollected -= PlcWeldCycleMonitorService_WeldPointCollected;
        }

        _disposed = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _exceptionLogService.Write(ex, "ProductRealtimePreviewService.RunAsync");
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        if (!_plcCommunicationService.Current.IsConnected)
        {
            return;
        }

        var localPrograms = await _programManageService.GetProgramLookupsAsync(cancellationToken);
        foreach (var station in ResolvePreviewStations())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stationNo = NormalizeStationNo(station.StationNo);
            if (station.ActiveTask is null)
            {
                ResetProductNumberGate(stationNo);
                PublishStatusSnapshot(stationNo, string.Empty);
                continue;
            }

            EnsureProductNumberGateTask(stationNo, station.ActiveTask.Id);
            try
            {
                _ = TaskProductProcessConfigResolver.ValidateProgram(_productProcessConfigService, _testSchemeConfigService,
                    station.ActiveTask, new[] { stationNo }, _settingsService.Get().ProcessParameterDeviceType);
            }
            catch (InvalidOperationException ex)
            {
                PublishStatusSnapshot(stationNo, $"不可判定：{ex.Message}");
                continue;
            }
            // 配方改为按程序名称下发，不再从 PLC 读回配方反查产品身份：
            // 反查链末端是程序里填的工号，通用程序下不代表本批产品。
            var identity = ResolveProductIdentity(station, localPrograms);
            if (identity is null || string.IsNullOrWhiteSpace(identity.ProductNum))
            {
                PublishStatusSnapshot(stationNo, "未选择加工程序，无法确定产品工艺。");
                continue;
            }

            if (!identity.TouchCount.HasValue)
            {
                PublishStatusSnapshot(
                    identity.StationNo,
                    "当前程序缺少有效的焊点数量，请先在程序管理中填写大于 0 的整数。",
                    identity);
                continue;
            }

            var config = ResolveProcessConfig(identity.ProductNum, station);
            if (config is null)
            {
                PublishStatusSnapshot(identity.StationNo, $"未找到产品工号 {identity.ProductNum} 的产品工艺配置。", identity);
                continue;
            }

            try
            {
                var snapshot = await BuildSnapshotAsync(identity, config, station.ActiveTask, cancellationToken);
                Publish(ApplyProductNumberGate(snapshot, station.ActiveTask.Id));
            }
            catch (InvalidOperationException ex)
            {
                PublishStatusSnapshot(identity.StationNo, $"不可判定：{ex.Message}", identity);
            }
        }
    }

    private IReadOnlyList<ProductionStationRuntimeState> ResolvePreviewStations()
    {
        var state = _weldTaskService.CurrentState;
        var stations = state.StationStates.Values
            .Where(station => station.HasWorkOrder || station.HasProgram || station.ActiveTask is not null)
            .OrderBy(station => station.StationNo)
            .ToList();

        if (stations.Count > 0)
        {
            return stations;
        }

        return new[]
        {
            new ProductionStationRuntimeState
            {
                StationNo = NormalizeStationNo(state.CurrentStationNo),
                CurrentWorkOrder = state.CurrentWorkOrder,
                SelectedProcess = state.SelectedProcess,
                SelectedProgram = state.SelectedProgram,
                ActiveTask = state.ActiveTask
            }
        };
    }

    /// <summary>
    /// 解析预览用产品身份，只采信所选（或任务绑定）本地程序里填写的产品工号。
    /// 现场存在一款本地程序供多个产品工号通用的设备，程序里填的工号（可能是“通用产品”）
    /// 与工单工号不对应，而产品工艺和测试项都配置在程序工号下，
    /// 因此不再回退工单或任务的工号，避免用查不到工艺的工号解析出空预览。
    /// </summary>
    private ProductPreviewIdentity? ResolveProductIdentity(ProductionStationRuntimeState station, IReadOnlyList<ProgramLookup> localPrograms)
    {
        var localProgram = station.SelectedProgram is not null
            ? ResolveLocalProgram(station.SelectedProgram, localPrograms)
            : ResolveLocalProgramById(station.ActiveTask?.ProgramId, station.ActiveTask?.DeviceId, localPrograms);
        if (string.IsNullOrWhiteSpace(localProgram?.ProductNum))
        {
            return null;
        }

        return new ProductPreviewIdentity(
            NormalizeStationNo(station.StationNo),
            localProgram.ProductNum.Trim(),
            localProgram.ProductModel?.Trim() ?? string.Empty,
            ResolveRuntimeTouchCount(station, localProgram.TouchCount));
    }

    private BizProductProcessConfig? ResolveProcessConfig(string productNum, ProductionStationRuntimeState station)
    {
        if (station.ActiveTask is not null)
        {
            return _productProcessConfigService.FindActiveForTask(station.ActiveTask, station.StationNo);
        }

        return _productProcessConfigService.FindActive(productNum, station.StationNo);
    }

    private async Task<ProductRealtimePreviewSnapshot> BuildSnapshotAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        BizWeldTask? activeTask,
        CancellationToken cancellationToken)
    {
        var touchCount = identity.TouchCount!.Value;
        var refreshTime = DateTime.Now;
        var settings = _settingsService.Get();
        if (activeTask is not null)
            _ = ProgramContentJsonRules.NormalizeForProduction(activeTask.ProgramContentSnapshot, settings.ProcessParameterDeviceType);
        var useProgramResult = WholePieceProgramResultRules.IsApplicable(settings.ProcessParameterDeviceType);
        var useProgramPointNumber = string.Equals(
            ProductionConstants.RealtimePointNumberSources.Normalize(settings.RealtimePointNumberSource),
            ProductionConstants.RealtimePointNumberSources.Program,
            StringComparison.OrdinalIgnoreCase);
        var productNo = await ResolveProductNoAsync(identity.StationNo, config, activeTask, settings, cancellationToken);
        var plcProductResult = FormatResult(await ReadExpressionTextAsync(config.ProductBase, 0, config.ProductResultExpr, cancellationToken));
        // 程序判断模式下面数完全由检测结果推算，不读 PLC 的实际数与预设数。
        var actualTouchCount = useProgramPointNumber
            ? string.Empty
            : await ReadExpressionTextAsync(config.ProductBase, 0, config.ActualTouchCountExpr, cancellationToken);
        var presetTouchCount = useProgramPointNumber
            ? string.Empty
            : await ReadExpressionTextAsync(config.ProductBase, 0, config.PresetTouchCountExpr, cancellationToken);
        var rowResult = await BuildRowsAsync(
            identity,
            config,
            touchCount,
            FormatValue(productNo),
            activeTask?.ProgramContentSnapshot,
            useProgramResult,
            useProgramPointNumber,
            refreshTime,
            cancellationToken);
        var mergedDefinitions = ResolveMergedDefinitions(
            config,
            touchCount,
            settings,
            detail => SchemeDetailRoleRules.ShouldEvaluateProgramRole(detail, SchemeDetailValueRole.Actual));
        var mergedDisplayDefinitions = WholePieceAbAggregationRules.IsApplicable(settings.ProcessParameterDeviceType, touchCount)
            ? WholePieceMergedDisplayRules.ResolveDefinitions(
                settings.ProcessParameterDeviceType,
                touchCount,
                _testSchemeConfigService.GetDetails(config.SchemeId),
                _testSchemeConfigService.GetItems())
            : Array.Empty<WholePieceAbValueDefinition>();
        // 合并列只显示上报实际值项，与开工时的静态表头同源，不依赖已采集的行数。
        var mergedColumns = mergedDisplayDefinitions.Count > 0
            ? WholePieceMergedDisplayRules.BuildColumns(mergedDisplayDefinitions)
            : Array.Empty<WholePieceMergedColumn>();
        var mergedAggregation = mergedDefinitions.Count > 0 && rowResult.IsComplete
            ? BuildMergedAggregation(rowResult, settings, mergedDefinitions)
            : null;
        var mergedSucceeded = mergedAggregation is { IsSuccess: true };
        // 聚合值按测试项表达式的小数位格式化，合并视图还要再按「判定与上报小数位」处理一次，
        // 与报告文件、过程参数和产品判定同源——操作员看到的数值就是判定所用、对外上报的值。
        var uploadFormat = OutputNumericFormat.ForUpload(settings);
        var mergedValues = mergedSucceeded
            ? WholePieceMergedDisplayRules.BuildValues(mergedColumns, mergedAggregation!.Rows)
                .ToDictionary(
                    pair => pair.Key,
                    pair => uploadFormat.Apply(pair.Value),
                    StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // PLC 读取模式没有程序判定依据，失败列保持为空，界面不标红。
        IReadOnlyList<string> mergedFailedColumns = Array.Empty<string>();
        string? judgementError = null;
        string productResult;
        if (!rowResult.IsComplete)
        {
            productResult = ProductionConstants.TestResults.NotAvailable;
        }
        else if (useProgramResult)
        {
            productResult = TestResultRules.ToDisplayText(ResolveRealtimeProgramProductResult(
                rowResult,
                activeTask?.ProgramContentSnapshot,
                mergedAggregation,
                mergedDefinitions,
                settings,
                out mergedFailedColumns,
                out judgementError));
            mergedFailedColumns = mergedFailedColumns
                .Where(columnName => mergedColumns.Any(column =>
                    string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        else
        {
            productResult = plcProductResult;
        }
        var message = judgementError ?? (rowResult.Errors.Count > 0
            ? string.Join("；", rowResult.Errors)
            : rowResult.Rows.Count == 0
                ? "测试方案没有可显示的测试项，请检查方案明细和测试项字典。"
                : string.Empty);

        return new ProductRealtimePreviewSnapshot(
            identity.StationNo,
            FormatValue(productNo),
            identity.ProductNum,
            identity.ProductModel,
            config.SchemeId,
            BuildTouchCountText(touchCount, actualTouchCount, presetTouchCount, useProgramPointNumber, rowResult.PlcFaceResults),
            ResolvePointName(config),
            productResult,
            refreshTime,
            rowResult.Rows,
            message)
        {
            MergedColumns = mergedColumns,
            MergedValues = mergedValues,
            MergedDefinitions = mergedDisplayDefinitions,
            MergedFailedColumns = mergedFailedColumns
        };
    }

    private async Task<string> ResolveProductNoAsync(
        int stationNo,
        BizProductProcessConfig config,
        BizWeldTask? activeTask,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ProductionConstants.ProductionCountSources.IsProgram(settings.ProductionCountSource))
        {
            return await ReadExpressionTextAsync(config.ProductBase, 0, config.ProductNoExpr, cancellationToken);
        }

        if (activeTask is null || activeTask.Id <= 0)
        {
            return "--";
        }

        string? plcProductNo = null;
        if (ProductRetestRules.IsSupportedDeviceType(settings.ProcessParameterDeviceType)
            && !string.IsNullOrWhiteSpace(config.ProductNoExpr))
        {
            try
            {
                var result = await _plcExpressionReadService.ReadExpressionTextAsync(
                    config.ProductBase,
                    0,
                    config.ProductNoExpr,
                    cancellationToken: cancellationToken);
                if (result.IsSuccess)
                {
                    plcProductNo = result.Value?.Trim();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // PLC 编号只辅助识别重测，读取失败不能阻止程序编号显示。
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return _productCycleCollectionService.ResolvePendingProductNo(
            activeTask.Id,
            stationNo,
            plcProductNo,
            settings.ProcessParameterDeviceType).ProductNo;
    }

    /// <summary>
    /// 解析四面整件检测的 A/B 聚合字段。非四面整件检测返回空集合。
    /// </summary>
    private IReadOnlyList<WholePieceAbValueDefinition> ResolveMergedDefinitions(
        BizProductProcessConfig config,
        int touchCount,
        AppSettings settings,
        Func<BizSchemeDetail, bool> shouldInclude)
    {
        if (!WholePieceAbAggregationRules.IsApplicable(settings.ProcessParameterDeviceType, touchCount))
        {
            return Array.Empty<WholePieceAbValueDefinition>();
        }

        return ResolveSchemeItems(config.SchemeId)
            .Where(item => shouldInclude(item.Detail))
            .Select(item => new WholePieceAbValueDefinition(
                item.Item.ItemId,
                item.Item.ItemName,
                item.Item.ItemName,
                item.Item.ActualExpression))
            .ToList();
    }

    /// <summary>
    /// 四面整件检测的合并显示与产品判定共用同一次 A/B 聚合，保证界面、上传和报表口径一致。
    /// </summary>
    private static WholePieceAbAggregationResult BuildMergedAggregation(
        PreviewRowsResult rowResult,
        AppSettings settings,
        IReadOnlyList<WholePieceAbValueDefinition> definitions)
    {
        // 只收参与聚合的测试项：未读取实际值的行只有空串，按测试项名分组时会盖掉真实值。
        var aggregatedItemIds = definitions.Select(definition => definition.ItemId).ToHashSet();
        var sideItemValues = rowResult.Rows
            .Where(row => aggregatedItemIds.Contains(row.ItemId))
            .GroupBy(row => row.TouchIndex.ToString(CultureInfo.InvariantCulture))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, string>)group
                    .GroupBy(row => row.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(item => item.Key, item => item.First().ActualValue, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        var sideResults = rowResult.Rows
            .GroupBy(row => row.TouchIndex.ToString(CultureInfo.InvariantCulture))
            .ToDictionary(group => group.Key, group => group.First().TouchResult, StringComparer.OrdinalIgnoreCase);

        return WholePieceAbAggregationRules.AggregatePreview(
            sideItemValues,
            sideResults,
            definitions,
            settings.EnablePlcStringNumericFormatting ?? true,
            settings.PlcStringNumericFormatMode);
    }

    private static string ResolveRealtimeProgramProductResult(
        PreviewRowsResult rowResult,
        string? programContentSnapshot,
        WholePieceAbAggregationResult? mergedAggregation,
        IReadOnlyList<WholePieceAbValueDefinition> definitions,
        AppSettings settings,
        out IReadOnlyList<string> failedColumns,
        out string? errorMessage)
    {
        failedColumns = Array.Empty<string>();
        errorMessage = null;
        if (!ProgramContentJsonRules.TryReadLimits(programContentSnapshot, out _, out var configurationError,
                ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck))
        {
            errorMessage = $"不可判定：{configurationError}";
            return ProductionConstants.TestResults.Unknown;
        }
        if (!rowResult.IsComplete) return ProductionConstants.TestResults.NotAvailable;
        if (mergedAggregation is null || !mergedAggregation.IsSuccess)
        {
            errorMessage = $"不可判定：{mergedAggregation?.ErrorMessage ?? "缺少有效的 A/B 聚合数据。"}";
            return ProductionConstants.TestResults.Unknown;
        }

        var merged = WholePieceProgramResultRules.EvaluateAggregated(
            programContentSnapshot,
            mergedAggregation.Rows,
            definitions,
            settings.EffectiveJudgementDecimalPlaces,
            settings.PlcStringNumericFormatMode);
        if (!merged.IsSuccess)
        {
            errorMessage = $"不可判定：{merged.ErrorMessage}";
            return ProductionConstants.TestResults.Unknown;
        }

        // 失败项已经是界面列名，直接给合并视图标红用。
        failedColumns = merged.FailedItems;
        return merged.Result;
    }

    private async Task<PreviewRowsResult> BuildRowsAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        int touchCount,
        string productNo,
        string? programContentSnapshot,
        bool useProgramResult,
        bool useProgramPointNumber,
        DateTime refreshTime,
        CancellationToken cancellationToken)
    {
        var schemeItems = ResolveSchemeItems(config.SchemeId);
        // 参与程序判定的测试项集合，与采集落库口径一致；预览显示范围不影响判定范围。
        var programItemIds = schemeItems
            .Where(item => SchemeDetailRoleRules.ShouldEvaluateProgramRole(item.Detail, SchemeDetailValueRole.Actual))
            .Select(item => item.Item.ItemId)
            .ToHashSet();
        var rows = new List<ProductRealtimePreviewRow>();
        var faceResults = new List<string?>();
        var plcFaceResults = new List<string?>();
        var errors = new List<string>();
        // 程序判断模式按检测进度逐面显示：遇到第一个未完成面后就不再建行，但仍要读完每面结果，
        // 否则 IsComplete 判定拿不到完整面结果，会连带打断合并显示、产品判定和 PLC 回写。
        var completedPrefix = true;

        for (var touchNo = 1; touchNo <= touchCount; touchNo++)
        {
            var touchContextOffset = (touchNo - 1) * config.TouchHeaderLen;
            var testContextOffset = (touchNo - 1) * config.TestAreaLen;
            var plcTouchResult = FormatResult(await ReadExpressionTextAsync(
                ResolveTouchResultBase(config),
                touchContextOffset,
                config.TouchResultExpr,
                cancellationToken));
            var shouldReadTestValues = ProductRealtimePreviewRules.ShouldReadTestValues(plcTouchResult);
            if (!shouldReadTestValues)
            {
                completedPrefix = false;
            }

            var shouldBuildRows = !useProgramPointNumber || completedPrefix;
            if (!shouldBuildRows)
            {
                faceResults.Add(plcTouchResult);
                plcFaceResults.Add(plcTouchResult);
                continue;
            }

            var realtimeTouchNo = useProgramPointNumber
                ? touchNo.ToString(CultureInfo.InvariantCulture)
                : NormalizeRealtimePointNumber(await ReadExpressionTextAsync(
                    ResolveTouchNoBase(config),
                    touchContextOffset,
                    config.TouchNoExpr,
                    cancellationToken));
            var faceRows = new List<ProductRealtimePreviewRow>();
            foreach (var schemeItem in schemeItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                faceRows.Add(await BuildRowAsync(
                    identity,
                    config,
                    productNo,
                    touchNo,
                    realtimeTouchNo,
                    testContextOffset,
                    plcTouchResult,
                    shouldReadTestValues,
                    schemeItem,
                    refreshTime,
                    cancellationToken));
            }

            // 逐面判定已取消：面结果沿用 PLC 的检测完成信号，产品结果只由 A/B 合并值判定。
            var displayedTouchResult = plcTouchResult;

            foreach (var row in faceRows)
            {
                row.TouchResult = displayedTouchResult;
            }

            rows.AddRange(faceRows);
            faceResults.Add(displayedTouchResult);
            plcFaceResults.Add(plcTouchResult);
        }

        return new PreviewRowsResult(
            rows,
            faceResults,
            plcFaceResults,
            plcFaceResults.Count == touchCount
                && plcFaceResults.All(IsTerminalPointResult),
            errors);
    }

    private async Task<ProductRealtimePreviewRow> BuildRowAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        string productNo,
        int touchNo,
        string realtimeTouchNo,
        int testContextOffset,
        string touchResult,
        bool shouldReadTestValues,
        SchemePreviewItem schemeItem,
        DateTime refreshTime,
        CancellationToken cancellationToken)
    {
        var item = schemeItem.Item;
        var actual = schemeItem.ReadActual
            ? ResolveExpressionBinding(config.TestBase, testContextOffset, item.ActualExpression)
            : PlcExpressionBinding.Empty;
        var upper = schemeItem.EnableUpper
            ? ResolveExpressionBinding(config.TestBase, testContextOffset, item.UpperExpression)
            : PlcExpressionBinding.Empty;
        var lower = schemeItem.EnableLower
            ? ResolveExpressionBinding(config.TestBase, testContextOffset, item.LowerExpression)
            : PlcExpressionBinding.Empty;
        var result = schemeItem.EnableResult
            ? ResolveExpressionBinding(config.TestBase, testContextOffset, item.ResultExpression)
            : PlcExpressionBinding.Empty;

        return new ProductRealtimePreviewRow
        {
            StationNo = identity.StationNo,
            Station = $"工位{identity.StationNo}",
            ProductNo = productNo,
            ProductNum = identity.ProductNum,
            ProductModel = identity.ProductModel,
            TouchIndex = touchNo,
            TouchNo = realtimeTouchNo,
            TouchResult = touchResult,
            PointName = ResolvePointName(config),
            PointNoHeader = ResolvePointNoHeader(config),
            PointResultHeader = ResolvePointResultHeader(config),
            PointCountHeader = ResolvePointCountHeader(config),
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Unit = item.Unit ?? string.Empty,
            EnableActual = schemeItem.EnableActual,
            EnableUpper = schemeItem.EnableUpper,
            EnableLower = schemeItem.EnableLower,
            EnableResult = schemeItem.EnableResult,
            ActualHeader = ResolveDetailHeader(schemeItem.Detail, item, ProductRealtimePreviewRole.Actual),
            UpperHeader = ResolveDetailHeader(schemeItem.Detail, item, ProductRealtimePreviewRole.Upper),
            LowerHeader = ResolveDetailHeader(schemeItem.Detail, item, ProductRealtimePreviewRole.Lower),
            ResultHeader = ResolveDetailHeader(schemeItem.Detail, item, ProductRealtimePreviewRole.Result),
            ActualValue = await ResolvePreviewValue(schemeItem.ReadActual, shouldReadTestValues, actual, cancellationToken),
            UpperValue = await ResolvePreviewValue(schemeItem.EnableUpper, shouldReadTestValues, upper, cancellationToken),
            LowerValue = await ResolvePreviewValue(schemeItem.EnableLower, shouldReadTestValues, lower, cancellationToken),
            Result = await ResolvePreviewResult(schemeItem.EnableResult, shouldReadTestValues, result, cancellationToken),
            RefreshTimeText = refreshTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            ActualAddress = actual.Address,
            UpperAddress = upper.Address,
            LowerAddress = lower.Address,
            ResultAddress = result.Address,
            Sort = touchNo * 10000 + schemeItem.Sort
        };
    }

    private IReadOnlyList<SchemePreviewItem> ResolveSchemeItems(string schemeId)
    {
        var details = _testSchemeConfigService.GetDetails(schemeId)
            .OrderBy(detail => detail.DetailId)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<SchemePreviewItem>();
        }

        var allItems = _testSchemeConfigService.GetItems();
        return details
            .Select((detail, index) => new
            {
                Sort = (index + 1) * 10,
                Item = allItems.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Select(item =>
            {
                SchemeDetailRoleRules.ClearUnavailableRoles(item.Detail, item.Item!);
                return item;
            })
            .Where(item => ShouldIncludeInPreview(item.Detail))
            .Select(item => new SchemePreviewItem(item.Sort, item.Item!, item.Detail))
            .ToList();
    }

    private async Task<string> ReadExpressionTextAsync(
        string baseAddress,
        int contextOffset,
        string? expression,
        CancellationToken cancellationToken)
    {
        var result = await _plcExpressionReadService.ReadExpressionTextAsync(
            baseAddress,
            contextOffset,
            expression,
            cancellationToken: cancellationToken);
        return result.IsSuccess ? FormatValue(result.Value) : "--";
    }

    private async Task<string> ResolvePreviewValue(
        bool enabled,
        bool shouldReadTestValues,
        PlcExpressionBinding binding,
        CancellationToken cancellationToken)
    {
        if (!enabled)
        {
            return string.Empty;
        }

        if (!shouldReadTestValues)
        {
            return "--";
        }

        return await ReadValueTextAsync(binding, cancellationToken);
    }

    private async Task<string> ResolvePreviewResult(
        bool enabled,
        bool shouldReadTestValues,
        PlcExpressionBinding binding,
        CancellationToken cancellationToken)
    {
        if (!enabled)
        {
            return string.Empty;
        }

        if (!shouldReadTestValues)
        {
            return "--";
        }

        return FormatResult(await ReadValueTextAsync(binding, cancellationToken));
    }

    private async Task<string> ReadValueTextAsync(PlcExpressionBinding binding, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(binding.Address))
        {
            return "--";
        }

        var result = await _plcExpressionReadService.ReadBindingTextAsync(
            binding,
            cancellationToken: cancellationToken);
        return result.IsSuccess ? FormatValue(result.Value) : "--";
    }

    private PlcExpressionBinding ResolveExpressionBinding(string baseAddress, int contextOffset, string? expression)
    {
        return _plcExpressionReadService.TryResolve(baseAddress, contextOffset, expression, out var binding, out _)
            ? binding
            : PlcExpressionBinding.Empty;
    }

    private static string BuildTouchCountText(
        int configuredTouchCount,
        string actualTouchCount,
        string presetTouchCount,
        bool useProgramPointNumber,
        IReadOnlyList<string?> plcFaceResults)
    {
        var expectedCount = Math.Max(1, configuredTouchCount);
        if (useProgramPointNumber)
        {
            // 程序判断模式：已完成数按检测结果连续推算，预设数取产品工艺配置，全程不依赖 PLC。
            var completed = ProductRealtimePreviewRules.CountCompletedFaces(plcFaceResults);
            return $"{completed.ToString(CultureInfo.InvariantCulture)}/{expectedCount.ToString(CultureInfo.InvariantCulture)}";
        }

        // 实际焊点数来自 PLC；未配置或读取失败时显示 ?，提醒现场不要误以为读到了真实值。
        var actual = string.IsNullOrWhiteSpace(actualTouchCount) || actualTouchCount == "--"
            ? "?"
            : actualTouchCount;

        // 预设焊点数优先使用 PLC 读取值；读取不到时回退产品工艺配置中的焊点数量。
        var expected = string.IsNullOrWhiteSpace(presetTouchCount) || presetTouchCount == "--"
            ? expectedCount.ToString(CultureInfo.InvariantCulture)
            : presetTouchCount;

        return $"{actual}/{expected}";
    }

    private static string ResolvePointName(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointName) ?? "焊点";

    private static string ResolvePointNoHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointNoHeader) ?? $"{ResolvePointName(config)}序号";

    private static string ResolvePointResultHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointResultHeader) ?? $"{ResolvePointName(config)}结果";

    private static string ResolvePointCountHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointCountHeader) ?? $"{ResolvePointName(config)}数";

    private static string ResolveDetailHeader(BizSchemeDetail detail, DimTestItem item, ProductRealtimePreviewRole role)
    {
        var schemeRole = role switch
        {
            ProductRealtimePreviewRole.Actual => SchemeDetailValueRole.Actual,
            ProductRealtimePreviewRole.Upper => SchemeDetailValueRole.Upper,
            ProductRealtimePreviewRole.Lower => SchemeDetailValueRole.Lower,
            ProductRealtimePreviewRole.Result => SchemeDetailValueRole.Result,
            _ => SchemeDetailValueRole.Actual
        };
        return SchemeDetailRoleRules.ResolveHeader(detail, item, schemeRole);
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue;
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "--"
            : value.Trim().Trim('\0');
    }

    private static string FormatResult(string? value)
    {
        var result = FormatValue(value);
        if (result == "--")
        {
            return result;
        }

        return TestResultRules.ToDisplayText(result);
    }

    private void Publish(ProductRealtimePreviewSnapshot snapshot)
    {
        lock (_snapshotSync)
        {
            _snapshots[snapshot.StationNo] = snapshot;
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    private void PlcWeldCycleMonitorService_ProductReady(object? sender, PlcProductReadySnapshot e)
    {
        if (e.TaskId <= 0)
        {
            return;
        }

        var stationNo = e.NormalizedStationNo;
        lock (_snapshotSync)
        {
            var gate = GetOrCreateProductNumberGate(stationNo, e.TaskId);
            if (!gate.HasObservedReadySignal)
            {
                // 当前产品的首个上升沿只建立基线，不能让当前产品采集完成后提前跳到下一号。
                gate.HasObservedReadySignal = true;
                return;
            }

            gate.IsNextProductReady = true;
        }
    }

    private void PlcWeldCycleMonitorService_WeldPointCollected(object? sender, BizWeldPointRecord e)
    {
        if (!e.ProductCompleted
            || e.TaskId <= 0
            || string.IsNullOrWhiteSpace(e.ProductNo))
        {
            return;
        }

        var stationNo = NormalizeStationNo(e.StationNo);
        lock (_snapshotSync)
        {
            var gate = GetOrCreateProductNumberGate(stationNo, e.TaskId);
            // 采集完成事件可先于实时预览首帧到达，用它固化当前产品编号，避免首帧已是下一号时误切换。
            gate.PublishedProductNo ??= e.ProductNo.Trim();
        }
    }

    private ProductRealtimePreviewSnapshot ApplyProductNumberGate(
        ProductRealtimePreviewSnapshot snapshot,
        int taskId)
    {
        var candidateProductNo = snapshot.ProductNo?.Trim() ?? string.Empty;
        lock (_snapshotSync)
        {
            var gate = GetOrCreateProductNumberGate(snapshot.StationNo, taskId);
            if (IsUnavailableProductNumber(candidateProductNo))
            {
                return gate.PublishedProductNo is null
                    ? snapshot
                    : ReplaceProductNumber(snapshot, gate.PublishedProductNo);
            }

            if (gate.PublishedProductNo is null)
            {
                gate.PublishedProductNo = candidateProductNo;
                gate.IsNextProductReady = false;
                return snapshot;
            }

            if (string.Equals(gate.PublishedProductNo, candidateProductNo, StringComparison.OrdinalIgnoreCase))
            {
                return snapshot;
            }

            if (!gate.IsNextProductReady)
            {
                return ReplaceProductNumber(snapshot, gate.PublishedProductNo);
            }

            gate.PublishedProductNo = candidateProductNo;
            gate.IsNextProductReady = false;
            return snapshot;
        }
    }

    private void EnsureProductNumberGateTask(int stationNo, int taskId)
    {
        lock (_snapshotSync)
        {
            _ = GetOrCreateProductNumberGate(stationNo, taskId);
        }
    }

    private void ResetProductNumberGate(int stationNo)
    {
        lock (_snapshotSync)
        {
            _productNumberGates.Remove(NormalizeStationNo(stationNo));
        }
    }

    private ProductNumberPreviewGate GetOrCreateProductNumberGate(int stationNo, int taskId)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        if (!_productNumberGates.TryGetValue(normalizedStationNo, out var gate)
            || gate.TaskId != taskId)
        {
            gate = new ProductNumberPreviewGate(taskId);
            _productNumberGates[normalizedStationNo] = gate;
        }

        return gate;
    }

    private static ProductRealtimePreviewSnapshot ReplaceProductNumber(
        ProductRealtimePreviewSnapshot snapshot,
        string productNo)
    {
        foreach (var row in snapshot.Rows)
        {
            row.ProductNo = productNo;
        }

        return snapshot with { ProductNo = productNo };
    }

    private static bool IsUnavailableProductNumber(string productNo)
        => string.IsNullOrWhiteSpace(productNo)
            || string.Equals(productNo, "--", StringComparison.Ordinal);

    /// <summary>
    /// Publishes a lightweight failure snapshot so the monitor clears stale rows and shows why realtime refresh stopped.
    /// </summary>
    private void PublishStatusSnapshot(int stationNo, string message, ProductPreviewIdentity? identity = null)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var snapshot = new ProductRealtimePreviewSnapshot(
            normalizedStationNo,
            "--",
            identity?.ProductNum ?? string.Empty,
            identity?.ProductModel ?? string.Empty,
            string.Empty,
            string.Empty,
            "焊点",
            "--",
            DateTime.Now,
            Array.Empty<ProductRealtimePreviewRow>(),
            message);
        Publish(ApplyExistingProductNumberGate(snapshot));
    }

    private ProductRealtimePreviewSnapshot ApplyExistingProductNumberGate(ProductRealtimePreviewSnapshot snapshot)
    {
        int taskId;
        lock (_snapshotSync)
        {
            if (!_productNumberGates.TryGetValue(snapshot.StationNo, out var gate))
            {
                return snapshot;
            }

            taskId = gate.TaskId;
        }

        return ApplyProductNumberGate(snapshot, taskId);
    }

    private ProgramLookup? ResolveLocalProgram(ProgramDataRes program, IReadOnlyList<ProgramLookup> localPrograms)
    {
        // 本轮采集只使用调用方提供的同一份不可变程序快照。
        var programId = program.Id?.Trim();
        if (!string.IsNullOrWhiteSpace(programId))
        {
            var byMesProgramId = ResolveLocalProgramById(programId, null, localPrograms);
            if (byMesProgramId is not null)
            {
                return byMesProgramId;
            }
        }

        return localPrograms.FirstOrDefault(item =>
            SameText(item.ProgramName, program.ProgramName)
            && SameText(item.ProductNum, program.ProductNum));
    }

    private ProgramLookup? ResolveLocalProgramById(string? programId, string? deviceId, IReadOnlyList<ProgramLookup> localPrograms)
    {
        var normalizedProgramId = programId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        var localId = normalizedProgramId.StartsWith("local-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(normalizedProgramId[6..], out var id) ? id : 0;
        return localPrograms
            .Where(program => SameText(program.ProgramId, normalizedProgramId) || (localId > 0 && program.Id == localId))
            .OrderByDescending(program => SameText(program.DeviceId, deviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePlcText(string? value)
    {
        return value?.Trim().Trim('\0') ?? string.Empty;
    }

    private static string ResolveTouchResultBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchResultBase) ? config.TouchBase : config.TouchResultBase!.Trim();

    private static string NormalizeRealtimePointNumber(string? value)
    {
        var normalized = FormatValue(value);
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pointNo)
            && pointNo > 0
                ? pointNo.ToString(CultureInfo.InvariantCulture)
                : ProductionConstants.TestResults.NotAvailable;
    }

    private static bool IsTerminalPointResult(string? result)
        => TestResultRules.IsOk(result)
            || TestResultRules.IsNg(result)
            || TestResultRules.IsPreWeldNg(result);

    private static string ResolveTouchNoBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchNoBase) ? config.TouchBase : config.TouchNoBase!.Trim();

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private sealed record PreviewRowsResult(
        IReadOnlyList<ProductRealtimePreviewRow> Rows,
        IReadOnlyList<string?> FaceResults,
        IReadOnlyList<string?> PlcFaceResults,
        bool IsComplete,
        IReadOnlyList<string> Errors);

    private static int? ResolveRuntimeTouchCount(ProductionStationRuntimeState station, int? localTouchCount)
    {
        if (station.ActiveTask is not null)
        {
            return ProgramContentJsonRules.TryGetTouchCount(station.ActiveTask.ProgramContentSnapshot, out var taskTouchCount)
                ? taskTouchCount
                : null;
        }

        if (station.SelectedProgram is not null)
        {
            return ProgramContentJsonRules.TryGetTouchCount(station.SelectedProgram.ProgramContent, out var programTouchCount)
                ? programTouchCount
                : null;
        }

        return localTouchCount;
    }

    private sealed record ProductPreviewIdentity(int StationNo, string ProductNum, string ProductModel, int? TouchCount);

    private sealed class ProductNumberPreviewGate(int taskId)
    {
        public int TaskId { get; } = taskId;

        public string? PublishedProductNo { get; set; }

        public bool HasObservedReadySignal { get; set; }

        public bool IsNextProductReady { get; set; }
    }

    private enum ProductRealtimePreviewRole
    {
        Actual,
        Upper,
        Lower,
        Result
    }

    /// <summary>
    /// 判断方案明细是否需要出现在实时预览的读取范围内。
    /// 参与程序判定但未勾实时预览的测试项也必须读值，否则预览判定与采集落库判定不同源，
    /// 现场会看到预览 OK、落库 NG。这类项四个显示开关全为 false，界面侧会自动排除。
    /// </summary>
    private static bool ShouldIncludeInPreview(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyPreviewEnabled(detail)
            || SchemeDetailRoleRules.ShouldEvaluateProgramRole(detail, SchemeDetailValueRole.Actual);
    }

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail)
    {
        public bool EnableActual => Detail.EnableActual;

        public bool EnableUpper => Detail.EnableUpper;

        public bool EnableLower => Detail.EnableLower;

        public bool EnableResult => Detail.EnableResult;

        /// <summary>
        /// 实际值是否需要读取。显示或参与程序判定都要读；上限、下限、结果只可能被显示，读取条件不放宽。
        /// </summary>
        public bool ReadActual => Detail.EnableActual
            || SchemeDetailRoleRules.ShouldEvaluateProgramRole(Detail, SchemeDetailValueRole.Actual);
    }
}
