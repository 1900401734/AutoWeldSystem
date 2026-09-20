using System.Reflection;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services.Plc;
using AutoWeldSystem.Services.Production;
using SqlSugar;

namespace AutoWeldSystem.Tests;

/// <summary>
/// 采集准入、整件事务与完工排空的行为回归：只用 SQLite 与假 PLC/MES，不做源码文本匹配。
/// </summary>
public static class TaskCollectionLifecycleTests
{
    public static void RunAll()
    {
        DrainAndAdmissionIsolation().GetAwaiter().GetResult();
        AtomicInsertAndRetestRollback();
        FinishWaitsForAcceptedCollectionAndRecounts().GetAwaiter().GetResult();
        StaleTaskIsRejectedWithoutPlcSideEffects().GetAwaiter().GetResult();
        PendingFeedbackKeepsAdmission().GetAwaiter().GetResult();
    }

    private static async Task DrainAndAdmissionIsolation()
    {
        using var fixture = new Fixture();
        fixture.Settings.Current.EnableDualStation = true;
        var task = fixture.InsertTask();
        using var first = fixture.Lifecycle.Accept(task, 1);
        using var second = fixture.Lifecycle.Accept(task, 2);
        using (var closing = fixture.Lifecycle.BeginClosing(task))
        {
            var waiting = closing.WaitForDrainAsync();
            Check(!waiting.IsCompleted, "共享任务必须等两个工位采集排空。");
            Throws<TaskRunRejectedException>(() => fixture.Lifecycle.Accept(task, 1));
            Throws<TaskRunRejectedException>(() => fixture.Lifecycle.BeginClosing(task));
            first.Dispose();
            Check(!waiting.IsCompleted, "另一个工位仍在途，不能提前完工。");
            second.Dispose();
            await waiting;
        }

        using (fixture.Lifecycle.Accept(task, 1))
        using (var closing = fixture.Lifecycle.BeginClosing(task))
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            await ThrowsAsync<OperationCanceledException>(() => closing.WaitForDrainAsync(cancellation.Token));
        }

        using (fixture.Lifecycle.Accept(task, 1)) { }
        Throws<TaskRunRejectedException>(() => fixture.Lifecycle.Accept(task, 3));

        var timeoutLifecycle = new TaskCollectionLifecycleCoordinator(fixture.Db, fixture.Settings, TimeSpan.FromMilliseconds(1));
        using (timeoutLifecycle.Accept(task, 1))
        using (var closing = timeoutLifecycle.BeginClosing(task))
            await ThrowsAsync<TimeoutException>(() => closing.WaitForDrainAsync());
        using (timeoutLifecycle.Accept(task, 1)) { }

        fixture.Db.Db.Updateable<BizWeldTask>().SetColumns(row => row.TaskStatus == "Completed")
            .SetColumns(row => row.EndTime == DateTime.Now).Where(row => row.Id == task.Id).ExecuteCommand();
        Throws<TaskRunRejectedException>(() => fixture.Lifecycle.Accept(task, 1));
        Throws<TaskRunRejectedException>(() => fixture.Lifecycle.BeginClosing(task));
        Check(!fixture.Lifecycle.HasActivity(task.Id), "拒绝后不得残留在途登记。");
    }

    private static void AtomicInsertAndRetestRollback()
    {
        using var fixture = new Fixture();
        var task = fixture.InsertTask();
        using var lease = fixture.Lifecycle.Accept(task, 1);
        var collection = fixture.Collection();
        fixture.Db.Db.Ado.ExecuteCommand("CREATE TRIGGER reject_point BEFORE INSERT ON Biz_WeldPointRecord WHEN NEW.TouchNo = '4' BEGIN SELECT RAISE(ABORT, 'fourth point rejected'); END;");
        Throws<Exception>(() => Save(collection, lease, Points(task, 1, 8), false));
        Check(fixture.Db.Db.Queryable<BizWeldPointRecord>().Count() == 0, "第4点失败必须回滚前3点，不能留半件。");
        fixture.Db.Db.Ado.ExecuteCommand("DROP TRIGGER reject_point;");

        var original = Points(task, 1, 8);
        Save(collection, lease, original, false);
        Check(original.All(point => point.Id > 0), "成功提交后内存对象必须拿到主键。");
        var before = Snapshot(fixture);
        fixture.Db.Db.Ado.ExecuteCommand("CREATE TRIGGER reject_update BEFORE UPDATE ON Biz_WeldPointRecord WHEN NEW.TouchNo = '4' BEGIN SELECT RAISE(ABORT, 'retest rejected'); END;");
        var retry = Points(task, 1, 8);
        retry.ForEach(point => point.ProductResult = "NG");
        Throws<Exception>(() => Save(collection, lease, retry, true));
        Check(before == Snapshot(fixture), "重焊更新中途失败必须整件回滚。");
        Check(retry.All(point => point.Id == 0), "回滚后内存对象不得保留已回滚的主键。");
        fixture.Db.Db.Ado.ExecuteCommand("DROP TRIGGER reject_update;");

        fixture.Db.Db.Ado.ExecuteCommand("CREATE TRIGGER reject_delete BEFORE DELETE ON Biz_WeldPointRecord WHEN OLD.TouchNo = '8' BEGIN SELECT RAISE(ABORT, 'stale point delete rejected'); END;");
        Throws<Exception>(() => Save(collection, lease, Points(task, 1, 7), true));
        Check(before == Snapshot(fixture), "重焊删多余点失败必须回滚覆盖与删除。");
        fixture.Db.Db.Ado.ExecuteCommand("DROP TRIGGER reject_delete;");

        Throws<BusinessOperationException>(() => Save(collection, lease, Points(task, 1, 8), false));
        Check(fixture.Db.Db.Queryable<BizWeldPointRecord>().Count() == 8, "撞号不能换号或再读PLC重试。");

        fixture.Db.Db.Updateable<BizWeldTask>().SetColumns(row => row.TaskStatus == "Completed")
            .SetColumns(row => row.EndTime == DateTime.Now).Where(row => row.Id == task.Id).ExecuteCommand();
        Throws<TaskRunRejectedException>(() => Save(collection, lease, Points(task, 2, 8), false));
        Check(fixture.Db.Db.Queryable<BizWeldPointRecord>().Count() == 8, "任务已完工时事务内复核必须拒绝并回滚。");
    }

    private static async Task FinishWaitsForAcceptedCollectionAndRecounts()
    {
        using var fixture = new Fixture();
        var task = fixture.InsertTask();
        fixture.Service.CurrentState.GetOrCreateStation(1).ActiveTask = task;
        var fourthPointRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new CycleReader(async (offset, expression) =>
        {
            if (expression == "touch" && offset == 3)
            {
                fourthPointRead.TrySetResult();
                await continueRead.Task;
            }
            return expression switch { "touch" => (offset + 1).ToString(), "result" => "OK", "count" => "8", _ => "1" };
        });
        var collection = fixture.Collection(reader);
        using var accepted = fixture.Lifecycle.Accept(task, 1);
        var collect = collection.CollectAcceptedAsync(accepted);
        await fourthPointRead.Task;
        var finish = fixture.Service.FinishAsync("U1", 999, 999, 0);
        Check(fixture.Mes.EndRequests.Count == 0 && !finish.IsCompleted, "第4点采集中不能发送MES完工。");
        continueRead.TrySetResult();
        var records = await collect;
        Check(records.Count == 8 && records.Count(point => point.ProductCompleted) == 1 && records[^1].ProductCompleted,
            "整件8点提交，只最后一点完成。");
        Check(!finish.IsCompleted, "PLC反馈与下游处理尚未释放凭据，完工不能提前。");
        accepted.Dispose();
        var finished = await finish;
        Check(finished.ActualQty == 1 && fixture.Mes.EndRequests.Single().ExpQty == 1, "排空后服务必须重算数量，不信调用方旧999。");
        Check(finished.TaskStatus == "Completed" && fixture.Db.Db.Queryable<BizWeldTask>().InSingle(task.Id).EndTime is not null,
            "排空成功后才本地Completed。");
        Throws<TaskRunRejectedException>(() => fixture.Lifecycle.Accept(task, 1));
    }

    private static async Task StaleTaskIsRejectedWithoutPlcSideEffects()
    {
        using var fixture = new Fixture();
        var task = fixture.InsertTask();
        fixture.Db.Db.Updateable<BizWeldTask>().SetColumns(row => row.TaskStatus == "Completed")
            .SetColumns(row => row.EndTime == DateTime.Now).Where(row => row.Id == task.Id).ExecuteCommand();
        // 内存仍持有“运行中”的旧任务，模拟已完工任务残留在另一实例或未清理的运行态里。
        fixture.Service.CurrentState.GetOrCreateStation(1).ActiveTask = task;
        short readyValue = 0;
        var writes = 0;
        var plc = LifecycleDispatchProxy.Create<IPlcCommunicationService>((method, _) => method.Name switch
        {
            "get_Current" or "GetCurrent" => Connected,
            nameof(IPlcCommunicationService.ReadInt16Async) => Task.FromResult(PlcServiceResult<short>.Success(readyValue)),
            _ when method.Name.StartsWith("Write", StringComparison.Ordinal) => throw new InvalidOperationException($"过期任务不可写PLC: {method.Name}（第 {++writes} 次）"),
            _ => throw new NotSupportedException(method.Name)
        });
        var readyEvents = 0;
        using var monitor = new WeldCycleMonitorService(FakeAddresses(), plc, fixture.Service, fixture.Collection(), null!,
            new FakeCenterProductForwardingService(), new FakeProgramExceptionLogService(), new FakeOperationLogService(), new NoopProductionLog());
        monitor.ProductReady += (_, _) => readyEvents++;
        var pollOnce = typeof(WeldCycleMonitorService).GetMethod("PollOnceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)pollOnce.Invoke(monitor, [CancellationToken.None])!;
        readyValue = 1;
        await (Task)pollOnce.Invoke(monitor, [CancellationToken.None])!;
        Check(writes == 0 && readyEvents == 0, "已完工任务的上升沿不得写PLC反馈、不得发布就绪事件。");
        Check(fixture.Db.Db.Queryable<BizWeldPointRecord>().Count() == 0, "已完工任务不得保存任何产品。");
        Check(fixture.Service.CurrentState.GetOrCreateStation(1).ActiveTask is null, "被拒绝的旧任务必须从内存运行态清除。");
        Check(!fixture.Lifecycle.HasActivity(task.Id), "拒绝后不得残留在途凭据。");
        await (Task)pollOnce.Invoke(monitor, [CancellationToken.None])!;
        Check(writes == 0, "清除后空闲轮询仍不得触碰PLC写入。");
    }

    private static async Task PendingFeedbackKeepsAdmission()
    {
        using var fixture = new Fixture();
        var task = fixture.InsertTask();
        fixture.Service.CurrentState.GetOrCreateStation(1).ActiveTask = task;
        var feedbackWorks = false;
        var plc = LifecycleDispatchProxy.Create<IPlcCommunicationService>((method, _) => method.Name switch
        {
            "get_Current" or "GetCurrent" => Connected,
            nameof(IPlcCommunicationService.ReadInt16Async) => Task.FromResult(PlcServiceResult<short>.Success(1)),
            nameof(IPlcCommunicationService.WriteInt16Async) => Task.FromResult(feedbackWorks ? PlcServiceResult.Success() : PlcServiceResult.Fail("offline")),
            _ => throw new NotSupportedException(method.Name)
        });
        var upload = LifecycleDispatchProxy.Create<IWeldPointUploadCoordinatorService>((_, _) => Task.CompletedTask);
        using var monitor = new WeldCycleMonitorService(FakeAddresses(), plc, fixture.Service, fixture.Collection(), upload,
            new FakeCenterProductForwardingService(), new FakeProgramExceptionLogService(), new FakeOperationLogService(), new NoopProductionLog());
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var getState = (Task)typeof(WeldCycleMonitorService).GetMethod("GetStationStateAsync", flags)!.Invoke(monitor, [1, CancellationToken.None])!;
        await getState;
        var state = getState.GetType().GetProperty("Result")!.GetValue(getState)!;
        var stateType = state.GetType();
        stateType.GetProperty("ReadySignalInitialized")!.SetValue(state, true);
        stateType.GetProperty("LastReadyHigh")!.SetValue(state, true);
        stateType.GetProperty("ProductDataReadyHandled")!.SetValue(state, true);
        stateType.GetProperty("PendingFeedbackValue")!.SetValue(state, (short)1);
        stateType.GetProperty("Lease")!.SetValue(state, fixture.Lifecycle.Accept(task, 1));
        var poll = typeof(WeldCycleMonitorService).GetMethod("PollProductCycleAsync", flags)!;
        await (Task)poll.Invoke(monitor, [task, state, CancellationToken.None])!;
        Check(fixture.Lifecycle.HasActivity(task.Id), "PLC反馈失败必须保留已受理凭据。");
        using var closing = fixture.Lifecycle.BeginClosing(task);
        var drained = closing.WaitForDrainAsync();
        Check(!drained.IsCompleted, "反馈重试之间完工仍需等待。");
        feedbackWorks = true;
        await (Task)poll.Invoke(monitor, [task, state, CancellationToken.None])!;
        await drained;
        Check(stateType.GetProperty("Lease")!.GetValue(state) is null, "反馈恢复成功后释放凭据。");
    }

    private static readonly PlcConnectionSnapshot Connected = new(PlcConnectionState.Connected, true, "fake", null, null, string.Empty);

    private static IPlcAddressService FakeAddresses()
        => LifecycleDispatchProxy.Create<IPlcAddressService>((method, args) => method.Name == nameof(IPlcAddressService.GetAddress)
            ? new BizPlcAddress { StationNo = 1, Enabled = true, LogicalKey = (string)args![0]!, Address = "fake", DataType = "Int16" }
            : Array.Empty<BizPlcAddress>());

    private static string Snapshot(Fixture fixture)
        => JsonSerializer.Serialize(fixture.Db.Db.Queryable<BizWeldPointRecord>().OrderBy(row => row.Id).ToList());

    private static List<BizWeldPointRecord> Points(BizWeldTask task, int product, int count)
        => Enumerable.Range(1, count).Select(index => new BizWeldPointRecord
        {
            TaskId = task.Id, StationNo = 1, ProductNo = product.ToString(), TouchNo = index.ToString(),
            ExpStartId = task.ExpStartId!, DeviceId = task.DeviceId, SN = task.SN, ProcessNo = task.ProcessNo,
            ProductCompleted = index == count, ProductResult = "OK", TestResult = "OK", RawDataJson = "{}", Ts = DateTime.Now,
            UploadStatus = "Pending"
        }).ToList();

    private static void Save(ProductCycleCollectionService collection, ITaskCollectionLease lease, List<BizWeldPointRecord> points, bool overwrite)
    {
        try
        {
            typeof(ProductCycleCollectionService).GetMethod("SaveRecords", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(collection, [lease, 1, points, true, overwrite]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"预期 {typeof(T).Name}，实际未拒绝。");
    }

    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new InvalidOperationException($"预期 {typeof(T).Name}，实际未拒绝。");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _file = Path.Combine(Path.GetTempPath(), $"task-lifecycle-{Guid.NewGuid():N}.db");

        public SqlSugarDbContext Db { get; }

        public FakeAppSettingsService Settings { get; } = new()
        {
            Current = new AppSettings
            {
                DeviceId = "D1",
                ProductionCountSource = ProductionConstants.ProductionCountSources.Program,
                ProcessParameterDeviceType = ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld,
                UploadMode = UploadMode.Batch
            }
        };

        public TaskCollectionLifecycleCoordinator Lifecycle { get; }

        public MesScenario Mes { get; } = new();

        public LifecycleTestService Service { get; }

        public LifecycleReportService Reports { get; } = new();

        public FakeProductProcessConfigService Processes { get; } = new(new Dictionary<int, BizProductProcessConfig>
        {
            [1] = new()
            {
                StationNo = 1, SchemeId = "S1", ProductBase = "P", TouchBase = "T", TestBase = "V",
                TouchHeaderLen = 1, TestAreaLen = 1, ProductNoExpr = "product", ProductResultExpr = "result",
                ActualTouchCountExpr = "count", TouchNoExpr = "touch", TouchResultExpr = "result"
            }
        });

        public Fixture()
        {
            // 沿用回归 harness 的独立 SQLite 接法，不读取应用配置或连接现场 MySQL。
            Db = new SqlSugarDbContext("unused");
            Db.Db.Dispose();
            var sqlite = new SqlSugarScope(new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = $"Data Source={_file};Pooling=False",
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SqlSugarDbContext).GetField("<Db>k__BackingField", flags)!.SetValue(Db, sqlite);
            typeof(SqlSugarDbContext).GetField("_initialized", flags)!.SetValue(Db, true);
            sqlite.CodeFirst.InitTables<BizWeldTask, BizWeldPointRecord, BizUploadTask, BizProductionReportFile>();
            sqlite.CodeFirst.InitTables<BizSchemeDetail, DimTestItem>();
            sqlite.Insertable(new DimTestItem { ItemId = 1, ItemName = "电流", ActualExpression = "value" }).ExecuteCommand();
            sqlite.Insertable(new BizSchemeDetail { SchemeId = "S1", ItemId = 1, EnableActual = true }).ExecuteCommand();
            Lifecycle = TaskCollectionLifecycleCoordinator.GetShared(Db, Settings);
            Service = new LifecycleTestService(Db, Mes.Provider, Settings, Lifecycle, Processes, Reports);
        }

        public BizWeldTask InsertTask() => Db.Db.Insertable(new BizWeldTask
        {
            DeviceId = "D1", StationNo = 1, SN = "WO1", ProductNum = "P1", Batch = "B1", ProcessNo = "OP1",
            ProgramId = "PROGRAM1", ProgramName = "PROGRAM1", RecipeCode = "1", ExpStartId = "MES1",
            StartTime = new DateTime(2026, 1, 1, 8, 0, 0), TaskStatus = "Running",
            ProgramContentSnapshot = "{\"焊点数量\":8}", StartAmount = 100
        }).ExecuteReturnEntity();

        public ProductCycleCollectionService Collection(IPlcExpressionReadService? reader = null)
            => new(Db, Processes, Settings, reader!, new FakeOperationLogService(), new NoopProductionLog(), Reports, Lifecycle);

        public void Dispose()
        {
            Db.Dispose();
            if (File.Exists(_file)) File.Delete(_file);
        }
    }

    private sealed class LifecycleTestService(SqlSugarDbContext db, IMesProvider mes, FakeAppSettingsService settings,
        ITaskCollectionLifecycleCoordinator lifecycle, IProductProcessConfigService processes, IProductionReportFileService reports)
        : WeldTaskService(db, mes, settings, new FakeOperationLogService(), new FakeLocalizationService(), new FakeUploadTaskService(),
            new FakeCenterProductForwardingService(), reports, new FakeDeviceLifecycleLogService(), new FakeDeviceStatusService(),
            new FakeSystemClockService(), new FakeDataHistoryMaintenanceService(), productProcessConfigService: processes,
            testSchemeConfigService: new FakeBoundaryTestSchemeService(), lifecycle: lifecycle)
    {
        protected override string ResolveProgramRecipeCode(ProgramDataRes program, string deviceId, int stationNo) => "1";
    }

    private sealed class LifecycleReportService : IProductionReportFileService
    {
        public BizProductionReportFile GenerateXlsxReport(BizWeldTask task) => ReserveXlsxReport(task);

        public BizProductionReportFile ReserveXlsxReport(BizWeldTask task)
            => new() { Id = task.Id, TaskId = task.Id, FileName = "test.xlsx", FilePath = "test.xlsx" };

        public bool ShouldUploadReportFile(BizWeldTask task) => false;

        public void ExportXlsx(int taskId, string filePath) => throw new NotSupportedException();
    }

    private sealed class MesScenario
    {
        public List<ExperimentStartReq> StartRequests { get; } = [];

        public List<ExperimentEndReq> EndRequests { get; } = [];

        public IMesProvider Provider => LifecycleDispatchProxy.Create<IMesProvider>((method, args) =>
        {
            if (method.Name == nameof(IMesProvider.StartWorkAsync))
            {
                var request = (ExperimentStartReq)args![0]!;
                StartRequests.Add(request);
                return Task.FromResult(new BasicRes<ExperimentStartRes> { Status = "S", Data = new() { Id = $"MES-NEW-{StartRequests.Count}" } });
            }
            if (method.Name == nameof(IMesProvider.EndWorkAsync))
            {
                EndRequests.Add((ExperimentEndReq)args![0]!);
                return Task.FromResult(new BasicRes<object> { Status = "S" });
            }
            throw new NotSupportedException(method.Name);
        });
    }

    private sealed class CycleReader(Func<int, string, Task<string>> read) : IPlcExpressionReadService
    {
        public PlcExpressionBinding Resolve(string? baseAddress, int contextOffset, string? expressionText)
            => new(contextOffset.ToString(), "Int16", contextOffset, expressionText ?? string.Empty);

        public bool TryResolve(string? baseAddress, int contextOffset, string? expressionText, out PlcExpressionBinding binding, out string message)
        {
            binding = Resolve(baseAddress, contextOffset, expressionText);
            message = string.Empty;
            return true;
        }

        public async Task<PlcServiceResult<string>> ReadBindingTextAsync(PlcExpressionBinding binding, string valueRole = "PLC地址", int stringLength = 32, CancellationToken cancellationToken = default)
            => PlcServiceResult<string>.Success(await read(binding.Rule, binding.Expression));

        public Task<PlcServiceResult<string>> ReadExpressionTextAsync(string? baseAddress, int contextOffset, string? expressionText, string valueRole = "PLC地址", int stringLength = 32, CancellationToken cancellationToken = default)
            => ReadBindingTextAsync(Resolve(baseAddress, contextOffset, expressionText), valueRole, stringLength, cancellationToken);

        public Task<PlcServiceResult<string>> ReadResolvedAddressTextAsync(string? address, string? dataType, int rule = 0, string valueRole = "PLC地址", int stringLength = 32, CancellationToken cancellationToken = default, int? decimalPlaces = null)
            => throw new NotSupportedException();
    }

    private sealed class NoopProductionLog : IProductionFlowLogService
    {
        public event EventHandler<ProductionFlowLogEntry>? LogWritten { add { } remove { } }

        public void Write(ProductionFlowLogEntry entry) { }

        public void Write(string step, string summary, string detail = "", string level = "Info", int stationNo = 0, string workOrderId = "", string productNo = "", string programId = "", string plcSignal = "", string plcAddress = "", long? durationMilliseconds = null) { }

        public IReadOnlyList<ProductionFlowLogEntry> GetByDate(DateTime date, int take = 500) => [];

        public string GetLogDirectory() => string.Empty;
    }
}

/// <summary>
/// 用委托实现接口的轻量代理，避免为每个用例手写整套假实现。
/// </summary>
public class LifecycleDispatchProxy : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);

    public static T Create<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = Create<T, LifecycleDispatchProxy>();
        ((LifecycleDispatchProxy)(object)proxy).Handler = handler;
        return proxy;
    }
}
