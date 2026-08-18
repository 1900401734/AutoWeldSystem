using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Response;
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
/// 该服务独立于产品就绪信号按固定周期读取 PLC，让界面显示当前设备数据。
/// </summary>
public sealed class ProductRealtimePreviewService : IProductRealtimePreviewService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProgramManageService _programManageService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly object _snapshotSync = new();
    private readonly Dictionary<int, ProductRealtimePreviewSnapshot> _snapshots = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public ProductRealtimePreviewService(
        IWeldTaskService weldTaskService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProgramManageService programManageService,
        IPlcAddressService plcAddressService,
        IPlcCommunicationService plcCommunicationService,
        IPlcExpressionReadService plcExpressionReadService,
        IProgramExceptionLogService exceptionLogService)
    {
        _weldTaskService = weldTaskService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _programManageService = programManageService;
        _plcAddressService = plcAddressService;
        _plcCommunicationService = plcCommunicationService;
        _plcExpressionReadService = plcExpressionReadService;
        _exceptionLogService = exceptionLogService;
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

        var localPrograms = (await _programManageService.GetProgramLookupsAsync(cancellationToken))
            .Select(lookup => lookup.ToEntityStub())
            .ToArray();
        foreach (var station in ResolvePreviewStations())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stationNo = NormalizeStationNo(station.StationNo);
            var identity = ResolveProductIdentity(station, localPrograms)
                ?? await ReadPlcProductIdentityAsync(stationNo, localPrograms, cancellationToken);
            if (identity is null || string.IsNullOrWhiteSpace(identity.ProductNum))
            {
                PublishStatusSnapshot(stationNo, "未识别到产品工号，请检查当前任务或 PLC 配方业务地址。");
                continue;
            }

            var config = ResolveProcessConfig(identity.ProductNum, station);
            if (config is null)
            {
                PublishStatusSnapshot(identity.StationNo, $"未找到产品工号 {identity.ProductNum} 的产品工艺配置。", identity);
                continue;
            }

            var snapshot = await BuildSnapshotAsync(identity, config, cancellationToken);
            Publish(snapshot);
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

    private ProductPreviewIdentity? ResolveProductIdentity(ProductionStationRuntimeState station, IReadOnlyList<BizProgram> localPrograms)
    {
        var localProgram = station.SelectedProgram is not null
            ? ResolveLocalProgram(station.SelectedProgram, localPrograms)
            : ResolveLocalProgramById(station.ActiveTask?.ProgramId, station.ActiveTask?.DeviceId, localPrograms);
        if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
        {
            return new ProductPreviewIdentity(
                NormalizeStationNo(station.StationNo),
                localProgram.ProductNum.Trim(),
                localProgram.ProductModel?.Trim() ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(station.CurrentWorkOrder?.ProdNum))
        {
            return new ProductPreviewIdentity(
                NormalizeStationNo(station.StationNo),
                station.CurrentWorkOrder.ProdNum.Trim(),
                station.CurrentWorkOrder.ProdModel?.Trim() ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(station.ActiveTask?.ProductNum))
        {
            return new ProductPreviewIdentity(
                NormalizeStationNo(station.StationNo),
                station.ActiveTask.ProductNum.Trim(),
                station.ActiveTask.ProductModel?.Trim() ?? string.Empty);
        }

        return null;
    }

    private BizProductProcessConfig? ResolveProcessConfig(string productNum, ProductionStationRuntimeState station)
    {
        if (station.ActiveTask is not null)
        {
            return _productProcessConfigService.FindActiveForTask(station.ActiveTask, station.StationNo);
        }

        return _productProcessConfigService.FindActive(productNum, station.StationNo);
    }

    /// <summary>
    /// No active MES task is required for preview: offline alignment identifies the product by PLC recipe code.
    /// </summary>
    private async Task<ProductPreviewIdentity?> ReadPlcProductIdentityAsync(int stationNo, IReadOnlyList<BizProgram> localPrograms, CancellationToken cancellationToken)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var recipeCode = await ReadBusinessAddressTextAsync(
            AppConstants.PlcLogicalKeys.PlcRecipeCode,
            normalizedStationNo,
            cancellationToken);
        var localProgram = ResolveLocalProgramByRecipeCode(recipeCode, normalizedStationNo, localPrograms);
        if (localProgram is null)
        {
            return null;
        }

        return new ProductPreviewIdentity(
            normalizedStationNo,
            localProgram.ProductNum.Trim(),
            localProgram.ProductModel?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Reads one configured PLC business address as text and quietly returns empty text when the address is not usable.
    /// </summary>
    private async Task<string> ReadBusinessAddressTextAsync(
        string logicalKey,
        int stationNo,
        CancellationToken cancellationToken)
    {
        var address = _plcAddressService.GetAddress(logicalKey, stationNo);
        if (address is null || !address.Enabled || string.IsNullOrWhiteSpace(address.Address))
        {
            return string.Empty;
        }

        try
        {
            var result = await _plcExpressionReadService.ReadResolvedAddressTextAsync(
                address.Address,
                address.DataType,
                stringLength: address.DataLength,
                cancellationToken: cancellationToken);
            return result.IsSuccess ? NormalizePlcText(result.Value) : string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<ProductRealtimePreviewSnapshot> BuildSnapshotAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        CancellationToken cancellationToken)
    {
        var refreshTime = DateTime.Now;
        var productNo = await ReadExpressionTextAsync(config.ProductBase, 0, config.ProductNoExpr, cancellationToken);
        var productResult = FormatResult(await ReadExpressionTextAsync(config.ProductBase, 0, config.ProductResultExpr, cancellationToken));
        var actualTouchCount = await ReadExpressionTextAsync(config.ProductBase, 0, config.ActualTouchCountExpr, cancellationToken);
        var presetTouchCount = await ReadExpressionTextAsync(config.ProductBase, 0, config.PresetTouchCountExpr, cancellationToken);
        var rows = await BuildRowsAsync(identity, config, FormatValue(productNo), refreshTime, cancellationToken);
        var message = rows.Count == 0
            ? "测试方案没有可显示的测试项，请检查方案明细和测试项字典。"
            : string.Empty;

        return new ProductRealtimePreviewSnapshot(
            identity.StationNo,
            FormatValue(productNo),
            identity.ProductNum,
            identity.ProductModel,
            config.SchemeId,
            BuildTouchCountText(config.TouchCount, actualTouchCount, presetTouchCount),
            ResolvePointName(config),
            productResult,
            refreshTime,
            rows,
            message);
    }

    private async Task<IReadOnlyList<ProductRealtimePreviewRow>> BuildRowsAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        string productNo,
        DateTime refreshTime,
        CancellationToken cancellationToken)
    {
        var schemeItems = ResolveSchemeItems(config.SchemeId);
        var rows = new List<ProductRealtimePreviewRow>();

        for (var touchNo = 1; touchNo <= Math.Max(1, config.TouchCount); touchNo++)
        {
            var touchContextOffset = (touchNo - 1) * config.TouchHeaderLen;
            var testContextOffset = (touchNo - 1) * config.TestAreaLen;
            var touchResult = FormatResult(await ReadExpressionTextAsync(
                ResolveTouchResultBase(config),
                touchContextOffset,
                config.TouchResultExpr,
                cancellationToken));
            foreach (var schemeItem in schemeItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rows.Add(await BuildRowAsync(
                    identity,
                    config,
                    productNo,
                    touchNo,
                    testContextOffset,
                    touchResult,
                    schemeItem,
                    refreshTime,
                    cancellationToken));
            }
        }

        return rows;
    }

    private async Task<ProductRealtimePreviewRow> BuildRowAsync(
        ProductPreviewIdentity identity,
        BizProductProcessConfig config,
        string productNo,
        int touchNo,
        int testContextOffset,
        string touchResult,
        SchemePreviewItem schemeItem,
        DateTime refreshTime,
        CancellationToken cancellationToken)
    {
        var item = schemeItem.Item;
        var actual = schemeItem.EnableActual
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
            TouchNo = touchNo.ToString(CultureInfo.InvariantCulture),
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
            ActualValue = schemeItem.EnableActual ? await ReadValueTextAsync(actual, cancellationToken) : string.Empty,
            UpperValue = schemeItem.EnableUpper ? await ReadValueTextAsync(upper, cancellationToken) : string.Empty,
            LowerValue = schemeItem.EnableLower ? await ReadValueTextAsync(lower, cancellationToken) : string.Empty,
            Result = schemeItem.EnableResult ? FormatResult(await ReadValueTextAsync(result, cancellationToken)) : string.Empty,
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
            .Where(item => HasAnyEnabledRole(item.Detail))
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

    private static string BuildTouchCountText(int configuredTouchCount, string actualTouchCount, string presetTouchCount)
    {
        // 实际焊点数来自 PLC；未配置或读取失败时显示 ?，提醒现场不要误以为读到了真实值。
        var actual = string.IsNullOrWhiteSpace(actualTouchCount) || actualTouchCount == "--"
            ? "?"
            : actualTouchCount;

        // 预设焊点数优先使用 PLC 读取值；读取不到时回退产品工艺配置中的焊点数量。
        var expected = string.IsNullOrWhiteSpace(presetTouchCount) || presetTouchCount == "--"
            ? Math.Max(1, configuredTouchCount).ToString(CultureInfo.InvariantCulture)
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

    /// <summary>
    /// Publishes a lightweight failure snapshot so the monitor clears stale rows and shows why realtime refresh stopped.
    /// </summary>
    private void PublishStatusSnapshot(int stationNo, string message, ProductPreviewIdentity? identity = null)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        Publish(new ProductRealtimePreviewSnapshot(
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
            message));
    }

    private BizProgram? ResolveLocalProgram(ProgramDataRes program, IReadOnlyList<BizProgram> localPrograms)
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

    private BizProgram? ResolveLocalProgramById(string? programId, string? deviceId, IReadOnlyList<BizProgram> localPrograms)
    {
        var normalizedProgramId = programId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        return localPrograms
            .Where(program => SameText(program.ProgramId, normalizedProgramId))
            .OrderByDescending(program => SameText(program.DeviceId, deviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private BizProgram? ResolveLocalProgramByRecipeCode(string? recipeCode, int stationNo, IReadOnlyList<BizProgram> localPrograms)
    {
        var normalizedRecipeCode = NormalizePlcText(recipeCode);
        if (string.IsNullOrWhiteSpace(normalizedRecipeCode))
        {
            return null;
        }

        return localPrograms
            .Where(program => ProgramRecipeMappingRules.Matches(program, stationNo, normalizedRecipeCode))
            .OrderByDescending(program => program.UpdatedTime)
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

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private sealed record ProductPreviewIdentity(int StationNo, string ProductNum, string ProductModel);

    private enum ProductRealtimePreviewRole
    {
        Actual,
        Upper,
        Lower,
        Result
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyCollectEnabled(detail);
    }

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail)
    {
        public bool EnableActual => Detail.EnableActual;

        public bool EnableUpper => Detail.EnableUpper;

        public bool EnableLower => Detail.EnableLower;

        public bool EnableResult => Detail.EnableResult;
    }
}
