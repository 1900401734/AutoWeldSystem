using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using System.Text.Json;

var tests = new (string Name, Action Run)[]
{
    ("Only configured test item expressions create available roles", OnlyConfiguredExpressionsCreateRoles),
    ("Collection does not imply local save or upload", CollectionDoesNotImplyOutput),
    ("Unavailable roles are cleared before save", UnavailableRolesAreCleared),
    ("Running task with changed PLC recipe requests reconciliation", RunningTaskWithChangedPlcRecipeRequestsReconciliation),
    ("Finished PLC work-order status skips recipe reconciliation", FinishedWorkOrderStatusSkipsRecipeReconciliation),
    ("Recipe station scope shares only same-work-order dual station recipes", RecipeStationScopeSharesOnlySameWorkOrderDualStationRecipes),
    ("Idle station recipe readback does not reconcile", IdleStationRecipeReadbackDoesNotReconcile),
    ("PLC test result codes map to explicit result names", PlcTestResultCodesMapToExplicitResultNames),
    ("PLC string numeric formatter follows global disabled setting", PlcStringNumericFormatterFollowsGlobalDisabledSetting),
    ("PLC string numeric formatter truncates when enabled", PlcStringNumericFormatterTruncatesWhenEnabled),
    ("PLC string numeric formatter rounds when enabled", PlcStringNumericFormatterRoundsWhenEnabled),
    ("PLC string numeric formatter keeps non numeric text", PlcStringNumericFormatterKeepsNonNumericText),
    ("Pre-weld NG is treated as failed product result", PreWeldNgIsTreatedAsFailedProductResult),
    ("Center device key uses DeviceId only", CenterDeviceKeyUsesDeviceIdOnly),
    ("Center client online uses heartbeat freshness", CenterClientOnlineUsesHeartbeatFreshness),
    ("Center offline state keeps PLC status unchanged", CenterOfflineStateKeepsPlcStatusUnchanged),
    ("Center telemetry snapshot carries station runtime data", CenterTelemetrySnapshotCarriesStationRuntimeData),
    ("Center dashboard device totals are calculated from station data", CenterDashboardDeviceTotalsAreCalculatedFromStationData),
    ("Center product report request carries one completed product", CenterProductReportRequestCarriesOneCompletedProduct),
    ("Center product report columns follow production Excel format", CenterProductReportColumnsFollowProductionExcelFormat),
    ("Center product report columns use forwarded equipment headers", CenterProductReportColumnsUseForwardedEquipmentHeaders),
    ("Center product report request carries production report fields", CenterProductReportRequestCarriesProductionReportFields),
    ("Finished task clears station runtime", FinishedTaskClearsStationRuntime),
    ("Offline start request uses local task id", OfflineStartRequestUsesLocalTaskId),
    ("Upload task identity prefers MES id then local id", UploadTaskIdentityPrefersMesIdThenLocalId),
    ("Unfinished upload summary task stays visible until hidden", UnfinishedUploadSummaryTaskStaysVisibleUntilHidden),
    ("Upload summary status falls back to business facts", UploadSummaryStatusFallsBackToBusinessFacts),
    ("Deleted upload task is excluded from retry lists", DeletedUploadTaskIsExcludedFromRetryLists),
    ("Process parameter pending product rows are read only", ProcessParameterPendingProductRowsAreReadOnly),
    ("Process parameter IsTest follows global setting and device type", ProcessParameterIsTestFollowsGlobalSettingAndDeviceType),
    ("Quantity upload batches product scopes and unique task ids", QuantityUploadBatchesProductScopesAndUniqueTaskIds),
    ("Process parameter upload payload reads product scope fields", ProcessParameterUploadPayloadReadsProductScopeFields)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static void OnlyConfiguredExpressionsCreateRoles()
{
    var item = new DimTestItem
    {
        ItemId = 1,
        ItemName = "高度",
        ActualExpression = "0:F-0"
    };

    var roles = SchemeDetailRoleRules.GetAvailableRoles(item).ToArray();

    AssertEqual(1, roles.Length, "只填写实际值时，只应生成一个方案明细角色。");
    AssertEqual(SchemeDetailValueRole.Actual, roles[0], "实际值必须是唯一可配置角色。");
}

static void CollectionDoesNotImplyOutput()
{
    var detail = new BizSchemeDetail
    {
        EnableActual = true,
        SaveActual = false,
        ReportActual = false,
        MesActual = false
    };

    AssertFalse(SchemeDetailRoleRules.ShouldPersistRole(detail, SchemeDetailValueRole.Actual), "只启用采集不应写入历史 RawDataJson。");

    detail.SaveActual = true;
    AssertTrue(SchemeDetailRoleRules.ShouldPersistRole(detail, SchemeDetailValueRole.Actual), "启用保存后应写入历史 RawDataJson。");
}

static void UnavailableRolesAreCleared()
{
    var item = new DimTestItem
    {
        ItemId = 1,
        ItemName = "高度",
        ActualExpression = "0:F-0"
    };
    var detail = new BizSchemeDetail
    {
        EnableActual = true,
        EnableUpper = true,
        SaveUpper = true,
        ReportUpper = true,
        MesUpper = true,
        UpperMesFieldName = "TestItem2",
        UpperHeader = "高度上限"
    };

    SchemeDetailRoleRules.ClearUnavailableRoles(detail, item);

    AssertFalse(detail.EnableUpper, "未配置上限表达式时，不应保留上限采集。");
    AssertFalse(detail.SaveUpper, "未配置上限表达式时，不应保留上限保存。");
    AssertFalse(detail.ReportUpper, "未配置上限表达式时，不应保留上限报表。");
    AssertFalse(detail.MesUpper, "未配置上限表达式时，不应保留上限 MES。");
    AssertEqual(null, detail.UpperMesFieldName, "未配置上限表达式时，应清空上限 MES 字段名。");
    AssertEqual(null, detail.UpperHeader, "未配置上限表达式时，应清空上限表头。");
}

static void RunningTaskWithChangedPlcRecipeRequestsReconciliation()
{
    var decision = RecipeCodeReconcileRules.Decide(
        validateEnabled: true,
        plcConnected: true,
        hasRunningTask: true,
        expectedRecipeCode: "1",
        plcRecipeCode: "3",
        workOrderStatus: ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction);

    AssertTrue(decision.ShouldReconcile, "开工状态下 PLC 配方号与任务配方不一致时，应触发调和。");
    AssertEqual("1", decision.ExpectedRecipeCode, "调和目标必须使用当前任务配方号。");
    AssertEqual("3", decision.PlcRecipeCode, "调和日志必须保留 PLC 侧实际切换后的配方号。");
}

static void FinishedWorkOrderStatusSkipsRecipeReconciliation()
{
    var decision = RecipeCodeReconcileRules.Decide(
        validateEnabled: true,
        plcConnected: true,
        hasRunningTask: true,
        expectedRecipeCode: "1",
        plcRecipeCode: "3",
        workOrderStatus: ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction);

    AssertFalse(decision.ShouldReconcile, "PLC 工单状态为完工/禁止生产时，不应继续写回配方号。");
}

static void RecipeStationScopeSharesOnlySameWorkOrderDualStationRecipes()
{
    AssertSequenceEqual(
        [1, 2],
        RecipeStationScopeRules.ResolveSharedRecipeStations(enableDualStation: true, enableDualWorkOrder: false, stationNo: 1),
        "双工位非双工单时，配方写回应覆盖两个工位。");
    AssertSequenceEqual(
        [1],
        RecipeStationScopeRules.ResolveSharedRecipeStations(enableDualStation: true, enableDualWorkOrder: true, stationNo: 1),
        "双工单时，工位 1 配方不应覆盖工位 2。");
    AssertSequenceEqual(
        [2],
        RecipeStationScopeRules.ResolveSharedRecipeStations(enableDualStation: true, enableDualWorkOrder: true, stationNo: 2),
        "双工单时，工位 2 配方不应覆盖工位 1。");
    AssertSequenceEqual(
        [1],
        RecipeStationScopeRules.ResolveSharedRecipeStations(enableDualStation: false, enableDualWorkOrder: false, stationNo: 0),
        "单工位模式只应写回默认工位。");
    AssertSequenceEqual(
        [1, 2],
        RecipeStationScopeRules.ResolveMonitorStations(enableDualStation: true),
        "双工位模式需要读取两个工位的 PLC 配方快照。");
}

static void IdleStationRecipeReadbackDoesNotReconcile()
{
    var decision = RecipeCodeReconcileRules.Decide(
        validateEnabled: true,
        plcConnected: true,
        hasRunningTask: false,
        expectedRecipeCode: "1",
        plcRecipeCode: "3",
        workOrderStatus: ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction);

    AssertFalse(decision.ShouldReconcile, "未开工时只能读取 PLC 配方用于显示，不应写回 PcRecipeCode。");
}

static void PlcTestResultCodesMapToExplicitResultNames()
{
    AssertEqual(ProductionConstants.TestResults.Ng, TestResultRules.Normalize("2"), "PLC raw value 2 must mean NG.");
    AssertEqual(ProductionConstants.TestResults.Ok, TestResultRules.Normalize("3"), "PLC raw value 3 must mean OK.");
    AssertEqual(ProductionConstants.TestResults.PreWeldNg, TestResultRules.Normalize("4"), "PLC raw value 4 must mean pre-weld NG.");
    AssertEqual(ProductionConstants.TestResults.Unknown, TestResultRules.Normalize("0"), "PLC raw value 0 must not be treated as OK or NG.");
    AssertEqual(ProductionConstants.TestResults.NotAvailable, TestResultRules.ToDisplayText("0"), "PLC raw value 0 must display as --.");
    AssertEqual(ProductionConstants.TestResults.Unknown, TestResultRules.Normalize("1"), "Undefined PLC result values must stay Unknown.");
}

static void PlcStringNumericFormatterFollowsGlobalDisabledSetting()
{
    var result = PlcStringNumericFormatter.Format(
        "+00.12267",
        decimalPlaces: 3,
        enabled: false,
        mode: AppConstants.PlcStringNumericFormatModes.Truncate);

    AssertEqual("+00.12267", result, "Disabled global string numeric formatting must return the PLC text unchanged.");
}

static void PlcStringNumericFormatterTruncatesWhenEnabled()
{
    var result = PlcStringNumericFormatter.Format(
        "+00.12267",
        decimalPlaces: 3,
        enabled: true,
        mode: AppConstants.PlcStringNumericFormatModes.Truncate);

    AssertEqual("0.122", result, "Truncate mode must cut extra decimal characters without rounding.");
    AssertEqual(
        "0.200",
        PlcStringNumericFormatter.Format("+00.2", 3, enabled: true, mode: AppConstants.PlcStringNumericFormatModes.Truncate),
        "Truncate mode must pad missing decimal characters to the configured fixed length.");
}

static void PlcStringNumericFormatterRoundsWhenEnabled()
{
    var result = PlcStringNumericFormatter.Format(
        "+00.12267",
        decimalPlaces: 3,
        enabled: true,
        mode: AppConstants.PlcStringNumericFormatModes.Round);

    AssertEqual("0.123", result, "Round mode must round to the configured decimal places.");
    AssertEqual(
        "0.234",
        PlcStringNumericFormatter.Format("+00.2344", 3, enabled: true, mode: AppConstants.PlcStringNumericFormatModes.Round),
        "Round mode must keep values below half unchanged at the configured precision.");
}

static void PlcStringNumericFormatterKeepsNonNumericText()
{
    var result = PlcStringNumericFormatter.Format(
        "WO20260618",
        decimalPlaces: 3,
        enabled: true,
        mode: AppConstants.PlcStringNumericFormatModes.Round);

    AssertEqual("WO20260618", result, "Non numeric strings must not be changed even when global formatting is enabled.");
}

static void PreWeldNgIsTreatedAsFailedProductResult()
{
    var result = TestResultRules.ResolveProductResult([
        ProductionConstants.TestResults.Ok,
        ProductionConstants.TestResults.PreWeldNg
    ]);

    AssertEqual(ProductionConstants.TestResults.PreWeldNg, result, "Product result should preserve pre-weld NG when any point failed before welding.");
    AssertTrue(TestResultRules.IsFailed(result), "Pre-weld NG must be treated as a failed result.");
}

static void CenterDeviceKeyUsesDeviceIdOnly()
{
    var request = new CenterTelemetrySnapshotRequest
    {
        DeviceId = " EM-001 ",
        DeviceName = "Single station",
        SystemType = CenterServerConstants.SystemTypes.Electromagnetic
    };

    AssertEqual("EM-001", CenterTelemetryRules.ResolveDeviceKey(request), "Center devices must be registered by DeviceId.");
}

static void CenterClientOnlineUsesHeartbeatFreshness()
{
    var now = new DateTime(2026, 6, 22, 8, 0, 0);
    var freshHeartbeat = now.AddSeconds(-10);
    var staleHeartbeat = now.AddSeconds(-16);

    AssertTrue(CenterTelemetryRules.IsClientOnline(freshHeartbeat, now, 15), "Fresh heartbeat must mark the client online.");
    AssertFalse(CenterTelemetryRules.IsClientOnline(staleHeartbeat, now, 15), "Stale heartbeat must mark the client offline.");
}

static void CenterOfflineStateKeepsPlcStatusUnchanged()
{
    var snapshot = new CenterDeviceRuntimeDto
    {
        PlcDeviceStatusCode = ProductionConstants.PlcDeviceStatuses.Alarm.ToString(),
        PlcDeviceStatusName = "Alarm",
        LastSeenAt = new DateTime(2026, 6, 22, 8, 0, 0)
    };

    var view = CenterTelemetryRules.BuildDashboardState(
        snapshot,
        now: snapshot.LastSeenAt.Value.AddSeconds(30),
        offlineTimeoutSeconds: 15);

    AssertFalse(view.ClientOnline, "Stale heartbeat must only affect client online state.");
    AssertEqual("4", view.PlcDeviceStatusCode, "Offline client state must not rewrite PLC device status.");
}

static void CenterTelemetrySnapshotCarriesStationRuntimeData()
{
    var request = new CenterTelemetrySnapshotRequest
    {
        DeviceId = "EM-001",
        DeviceName = "单稳态型自动电焊设备",
        SystemType = CenterServerConstants.SystemTypes.Electromagnetic,
        Stations =
        [
            new CenterTelemetryStationSnapshot
            {
                StationNo = 1,
                DeviceStatusCode = "1",
                DeviceStatusName = "运行",
                CurrentWorkOrder = "WO-1",
                ProductJobNo = "163#J",
                TodayTotalCount = 10,
                TodayQualifiedCount = 9,
                TodayFailedCount = 1
            },
            new CenterTelemetryStationSnapshot
            {
                StationNo = 2,
                DeviceStatusCode = "4",
                DeviceStatusName = "报警",
                CurrentWorkOrder = "WO-2",
                ProductJobNo = "164#J",
                AlarmMessage = "气压低",
                TodayTotalCount = 3,
                TodayQualifiedCount = 3
            }
        ]
    };

    AssertEqual(2, request.Stations.Count, "Center telemetry must keep each station as an independent runtime snapshot.");
    AssertEqual("WO-2", request.Stations[1].CurrentWorkOrder, "Dual-station work orders must not overwrite each other.");
    AssertEqual("164#J", request.Stations[1].ProductJobNo, "Dual-station product job numbers must not overwrite each other.");
}

static void CenterDashboardDeviceTotalsAreCalculatedFromStationData()
{
    var device = new CenterDashboardDeviceDto
    {
        Stations =
        [
            new CenterDashboardStationDto { TodayTotalCount = 10, TodayQualifiedCount = 9, TodayFailedCount = 1 },
            new CenterDashboardStationDto { TodayTotalCount = 3, TodayQualifiedCount = 3, TodayFailedCount = 0 }
        ]
    };

    AssertEqual(13, device.TodayTotalCount, "Dashboard device total must be the sum of station totals.");
    AssertEqual(12, device.TodayQualifiedCount, "Dashboard device qualified count must be the sum of station qualified counts.");
    AssertEqual(1, device.TodayFailedCount, "Dashboard device failed count must be the sum of station failed counts.");
}

static void CenterProductReportRequestCarriesOneCompletedProduct()
{
    var request = new CenterProductReportRequest
    {
        DeviceId = "EM-001",
        StationNo = 2,
        WorkOrder = "WO-2",
        ProductJobNo = "164#J",
        ProductNo = "P0001",
        ProductResult = ProductionConstants.TestResults.Ok,
        ReportColumns =
        [
            new CenterProductReportColumnDto
            {
                Key = "item_1",
                Title = "高度实际值",
                MergeByProduct = false
            }
        ],
        Points =
        [
            new CenterProductReportPointDto
            {
                TouchNo = "1",
                TestResult = ProductionConstants.TestResults.Ok,
                RawDataJson = "{\"height\":\"1.23\"}"
            }
        ]
    };

    AssertEqual(2, request.StationNo, "Completed product forwarding must preserve the producing station.");
    AssertEqual("164#J", request.ProductJobNo, "Completed product forwarding must preserve the product job number.");
    AssertEqual("高度实际值", request.ReportColumns[0].Title, "Completed product forwarding must carry equipment-side Excel headers.");
    AssertEqual(1, request.Points.Count, "Completed product forwarding must include collected point rows.");
}

static void CenterProductReportColumnsFollowProductionExcelFormat()
{
    var columns = CenterProductReportFormat.BuildColumns(["height", "height_result"]);
    var headers = columns.Select(column => column.Title).ToArray();

    AssertEqual("生产报表", CenterProductReportFormat.WorksheetName, "Center report sheet name must match equipment Excel reports.");
    AssertEqual("工位", headers[0], "Center report must start with the same station column as equipment reports.");
    AssertEqual("产品编号", headers[1], "Center report must use the same product number column as equipment reports.");
    AssertEqual("产品结果", headers[2], "Center report must use the same product result column as equipment reports.");
    AssertEqual("焊点编号", headers[3], "Center report must use the same point number column as equipment reports.");
    AssertEqual("焊点结果", headers[4], "Center report must use the same point result column as equipment reports.");
    AssertEqual("height", headers[5], "Dynamic collected values must be placed after point result columns.");
    AssertEqual("工号", headers[^7], "Trailing work-order columns must follow the equipment report format.");
    AssertFalse(headers.Contains("设备编号"), "Center-only device columns must not be inserted into the Excel report table.");
    AssertFalse(headers.Contains("设备名称"), "Center-only device columns must not be inserted into the Excel report table.");
    AssertFalse(headers.Contains("系统类型"), "Center-only system columns must not be inserted into the Excel report table.");
}

static void CenterProductReportColumnsUseForwardedEquipmentHeaders()
{
    var columns = CenterProductReportFormat.BuildColumns([
        new CenterProductReportColumn(CenterProductReportFormat.ColumnTouchNo, "相机编号", MergeByProduct: false),
        new CenterProductReportColumn(CenterProductReportFormat.ColumnTouchResult, "相机结果", MergeByProduct: false),
        new CenterProductReportColumn("item_1", "高度实际值", MergeByProduct: false)
    ]);
    var headers = columns.Select(column => column.Title).ToArray();

    AssertEqual("相机编号", headers[3], "Forwarded equipment point number header must override the center default.");
    AssertEqual("相机结果", headers[4], "Forwarded equipment point result header must override the center default.");
    AssertEqual("高度实际值", headers[5], "Forwarded equipment dynamic headers must be used in the center Excel report.");
}

static void CenterProductReportRequestCarriesProductionReportFields()
{
    var request = new CenterProductReportRequest
    {
        Batch = "B001",
        Quantity = 20,
        PartName = "引出线",
        ProcessNo = "OP10",
        OperatorNo = "U001",
        Points =
        [
            new CenterProductReportPointDto
            {
                OperatorNo = "U002"
            }
        ]
    };

    AssertEqual("B001", request.Batch, "Center report request must preserve batch for the Excel report.");
    AssertEqual(20, request.Quantity, "Center report request must preserve quantity for the Excel report.");
    AssertEqual("引出线", request.PartName, "Center report request must preserve part name for the Excel report.");
    AssertEqual("OP10", request.ProcessNo, "Center report request must preserve process number for the Excel report.");
    AssertEqual("U001", request.OperatorNo, "Center report request must preserve task operator for the Excel report.");
    AssertEqual("U002", request.Points[0].OperatorNo, "Center report request must preserve point operator when available.");
}

static void FinishedTaskClearsStationRuntime()
{
    var task = new BizWeldTask
    {
        Id = 12,
        TaskStatus = "Completed"
    };
    var station = new ProductionStationRuntimeState
    {
        StationNo = 1,
        ActiveTask = task,
        CurrentWorkOrder = new AutoWeldSystem.Core.DTOs.Mes.Response.WorkOrderRes
        {
            SN = "WO-OLD"
        },
        SelectedProcess = new AutoWeldSystem.Core.DTOs.Mes.Response.ExpItemData
        {
            ProcessNo = "OP10"
        }
    };

    var cleared = WeldTaskRuntimeRules.ClearFinishedTask(station, task);

    AssertTrue(cleared, "Finished task should clear the station runtime it occupies.");
    AssertTrue(station.ActiveTask is null, "Completed task must not stay in ActiveTask after finish.");
    AssertTrue(station.CurrentWorkOrder is null, "Old work-order fields must be cleared after finish.");
    AssertTrue(station.SelectedProcess is null, "Old process fields must be cleared after finish.");
}

static void OfflineStartRequestUsesLocalTaskId()
{
    var request = new ExperimentStartReq
    {
        ProductName = "引出线",
        DrawingNo = "DR-001"
    };
    var task = new BizWeldTask
    {
        IsOfflineCreated = true,
        LocalExpStartId = "0123456789abcdef0123456789abcdef"
    };

    var changed = ExperimentStartRequestRules.ApplyOfflineStartId(task, request);

    AssertTrue(changed, "Offline start request should receive the locally generated task id.");
    AssertEqual(task.LocalExpStartId, request.Id, "MES start request Id must use the local 32-character task id.");
    AssertEqual("引出线", request.ProductName, "ProductName must remain the operator-entered product name.");
    AssertEqual("DR-001", request.DrawingNo, "DrawingNo must remain the operator-entered drawing number.");
}

static void UploadTaskIdentityPrefersMesIdThenLocalId()
{
    var onlineTask = new BizWeldTask
    {
        Id = 1,
        ExpStartId = "MES-1001",
        LocalExpStartId = "local-online"
    };
    var offlineTask = new BizWeldTask
    {
        Id = 2,
        LocalExpStartId = "local-offline"
    };
    var noIdentityTask = new BizWeldTask
    {
        Id = 26,
        LocalExpStartId = string.Empty
    };

    AssertEqual("MES-1001", UploadTaskIdentityRules.Resolve(onlineTask), "MES task id must be shown before local id.");
    AssertEqual("local-offline", UploadTaskIdentityRules.Resolve(offlineTask), "Offline rows must show the local task id before MES returns one.");
    AssertEqual("0000000000000000000000000000001a", UploadTaskIdentityRules.Resolve(noIdentityTask), "Rows without MES or local id must use the fixed local database id fallback.");
}

static void UnfinishedUploadSummaryTaskStaysVisibleUntilHidden()
{
    var runningTask = new BizWeldTask
    {
        Id = 10,
        TaskStatus = ProductionConstants.ProductInstanceStatuses.Running,
        EndTime = null,
        UploadStateHidden = false
    };

    AssertTrue(UploadSummaryVisibilityRules.ShouldShow(runningTask, pendingCount: 0), "未完工任务即使没有待处理项，也必须显示在上传总览。");

    runningTask.UploadStateHidden = true;
    AssertFalse(UploadSummaryVisibilityRules.ShouldShow(runningTask, pendingCount: 4), "用户手动隐藏后，总览不应继续显示该任务。");

    var completedTask = new BizWeldTask
    {
        Id = 11,
        EndTime = DateTime.Now,
        UploadStateHidden = false
    };
    AssertFalse(UploadSummaryVisibilityRules.ShouldShow(completedTask, pendingCount: 0), "已完工且没有待处理项时，总览应自动隐藏。");
    AssertTrue(UploadSummaryVisibilityRules.ShouldShow(completedTask, pendingCount: 1), "已完工但仍有待处理项时，总览仍需显示。");
}

static void UploadSummaryStatusFallsBackToBusinessFacts()
{
    var onlineStartedTask = new BizWeldTask
    {
        ExpStartId = "MES-1001"
    };

    AssertEqual(
        ProductionConstants.UploadStatuses.Uploaded,
        UploadSummaryStatusResolver.ResolveStartReportStatus(onlineStartedTask, Array.Empty<string>()),
        "在线开工已返回 ExpStartId 时，即使没有补传任务，总览也应显示开工已上传。");

    var uploadedRecords = new[]
    {
        BuildCompletedPoint(taskId: 1, stationNo: 1, productNo: "P001", sequenceNo: 1, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 1, stationNo: 1, productNo: "P001", sequenceNo: 2, uploadStatus: ProductionConstants.UploadStatuses.Uploaded)
    };
    AssertEqual(
        ProductionConstants.UploadStatuses.Uploaded,
        UploadSummaryStatusResolver.ResolveProcessParameterStatus(Array.Empty<string>(), uploadedRecords),
        "过程参数没有补传任务但焊点记录全部已上传时，总览应显示已上传。");

    var pendingRecords = new[]
    {
        BuildCompletedPoint(taskId: 1, stationNo: 1, productNo: "P002", sequenceNo: 3, uploadStatus: ProductionConstants.UploadStatuses.Pending)
    };
    AssertEqual(
        ProductionConstants.UploadStatuses.Pending,
        UploadSummaryStatusResolver.ResolveProcessParameterStatus(Array.Empty<string>(), pendingRecords),
        "产品历史仍有未上传过程参数时，总览应显示待上传。");
}

static void DeletedUploadTaskIsExcludedFromRetryLists()
{
    var activeTask = new BizUploadTask
    {
        Status = ProductionConstants.UploadStatuses.Pending,
        IsDeleted = false
    };
    var deletedTask = new BizUploadTask
    {
        Status = ProductionConstants.UploadStatuses.Pending,
        IsDeleted = true
    };

    AssertTrue(UploadTaskVisibilityRules.ShouldInclude(activeTask, includeCompleted: false), "未删除待上传任务应出现在明细列表。");
    AssertFalse(UploadTaskVisibilityRules.ShouldInclude(deletedTask, includeCompleted: true), "软删除上传任务不应出现在明细列表或重试范围。");
}

static void ProcessParameterPendingProductRowsAreReadOnly()
{
    var records = new[]
    {
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P001", sequenceNo: 1),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P001", sequenceNo: 2),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P002", sequenceNo: 3, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 7, stationNo: 2, productNo: "S2-P001", sequenceNo: 4)
    };
    var task = new BizWeldTask
    {
        Id = 7,
        StationNo = 1,
        ExpStartId = "MES-7",
        LocalExpStartId = "LOCAL-7",
        SN = "WO-7"
    };

    var rows = ProcessParameterUploadRowRules.CreatePendingProductRows(task, records, uploadBatchSize: 3);

    AssertEqual(2, rows.Count, "未上传产品历史应按工位和产品编号生成过程参数只读行。");
    AssertTrue(rows.All(row => row.IsVirtual), "产品历史补充行必须标记为虚拟行。");
    AssertTrue(rows.All(row => !row.CanRetry && !row.CanDelete), "虚拟行不能手动重试或删除。");
    AssertTrue(rows.Any(row => row.ProductNo == "P001" && row.StationNo == 1), "工位 1 未上传产品应显示。");
    AssertTrue(rows.Any(row => row.ProductNo == "S2-P001" && row.StationNo == 2), "双工位未上传产品应显示。");
    AssertFalse(rows.Any(row => row.ProductNo == "P002"), "已上传产品不应再显示为待上传过程参数。");
    AssertTrue(rows[0].DisplayMessage.Contains("批次", StringComparison.Ordinal), "数量模式未达阈值时应提示等待批次数量。");
}

static void ProcessParameterIsTestFollowsGlobalSettingAndDeviceType()
{
    var weldItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-1",
        IsTest = ProcessParameterIsTestRules.Resolve(recordIsTest: false, showTestFlagInHistory: true, ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld)
    };
    var weldJson = JsonSerializer.Serialize(weldItem);
    AssertTrue(weldJson.Contains("\"IsTest\":false", StringComparison.Ordinal), "点焊设备开启全局试焊件后，即使 false 也必须输出 IsTest 字段。");

    var testItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-2",
        IsTest = ProcessParameterIsTestRules.Resolve(recordIsTest: true, showTestFlagInHistory: true, ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic)
    };
    var testJson = JsonSerializer.Serialize(testItem);
    AssertTrue(testJson.Contains("\"IsTest\":true", StringComparison.Ordinal), "电磁设备标记试焊件后必须输出 IsTest=true。");

    var checkItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-3",
        IsTest = ProcessParameterIsTestRules.Resolve(recordIsTest: true, showTestFlagInHistory: true, ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck)
    };
    var checkJson = JsonSerializer.Serialize(checkItem);
    AssertFalse(checkJson.Contains("\"IsTest\"", StringComparison.Ordinal), "整件检测设备不应输出 IsTest 字段。");

    var disabledItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-4",
        IsTest = ProcessParameterIsTestRules.Resolve(recordIsTest: true, showTestFlagInHistory: false, ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld)
    };
    var disabledJson = JsonSerializer.Serialize(disabledItem);
    AssertFalse(disabledJson.Contains("\"IsTest\"", StringComparison.Ordinal), "全局关闭试焊件显示时不应输出 IsTest 字段。");
}

static void QuantityUploadBatchesProductScopesAndUniqueTaskIds()
{
    var records = new[]
    {
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P001", sequenceNo: 1),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P001", sequenceNo: 2),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P002", sequenceNo: 3),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P003", sequenceNo: 4),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "P004", sequenceNo: 5),
        BuildCompletedPoint(taskId: 7, stationNo: 2, productNo: "S2-P001", sequenceNo: 6),
        BuildCompletedPoint(taskId: 8, stationNo: 1, productNo: "OTHER-TASK", sequenceNo: 7),
        BuildCompletedPoint(taskId: 7, stationNo: 1, productNo: "UPLOADED", sequenceNo: 8, uploadStatus: ProductionConstants.UploadStatuses.Uploaded)
    };

    var firstBatch = ProcessParameterBatchUploadRules.TakeReadyProductNos(records, taskId: 7, stationNo: 1, batchSize: 2);
    AssertSequenceEqual(new[] { "P001", "P002" }, firstBatch, "数量上传应只截取当前任务、当前工位、未上传的前 N 件产品。");

    var firstBusinessId = ProcessParameterBatchUploadRules.BuildQuantityBusinessId(taskId: 7, stationNo: 1, firstBatch);
    var secondBusinessId = ProcessParameterBatchUploadRules.BuildQuantityBusinessId(taskId: 7, stationNo: 1, new[] { "P003", "P004" });
    AssertFalse(string.Equals(firstBusinessId, secondBusinessId, StringComparison.Ordinal), "不同批次必须生成不同 BusinessId，不能复用已上传任务。");
    AssertTrue(firstBusinessId.Length <= 100, "BusinessId 需要保持在 BizUploadTask 字段长度限制内。");

    var nextBatch = ProcessParameterBatchUploadRules.TakeReadyProductNos(records, taskId: 7, stationNo: 1, batchSize: 2, excludedProductNos: firstBatch);
    AssertSequenceEqual(new[] { "P003", "P004" }, nextBatch, "失败待重试批次中的产品不应阻塞后续新批次。");
}

static void ProcessParameterUploadPayloadReadsProductScopeFields()
{
    var productNoPayload = JsonSerializer.Serialize(new { ProductNo = "P100" });
    AssertSequenceEqual(
        new[] { "P100" },
        ProcessParameterUploadPayloadRules.ReadProductNos(productNoPayload),
        "新 payload 中的 ProductNo 必须可用于单件上传作用域过滤。");

    var legacyPayload = JsonSerializer.Serialize(new { ProductNumber = "OLD-P100" });
    AssertSequenceEqual(
        new[] { "OLD-P100" },
        ProcessParameterUploadPayloadRules.ReadProductNos(legacyPayload),
        "旧 payload 中的 ProductNumber 仍需兼容。");

    var batchPayload = JsonSerializer.Serialize(new { StationNo = 2, ProductNos = new[] { "P201", "P202" } });
    AssertSequenceEqual(
        new[] { "P201", "P202" },
        ProcessParameterUploadPayloadRules.ReadProductNos(batchPayload),
        "批量 payload 的 ProductNos 必须作为批次过滤范围。");
    AssertEqual(2, ProcessParameterUploadPayloadRules.ReadStationNo(batchPayload), "批量 payload 必须保留工位过滤条件。");
}

static BizWeldPointRecord BuildCompletedPoint(
    int taskId,
    int stationNo,
    string productNo,
    int sequenceNo,
    string uploadStatus = ProductionConstants.UploadStatuses.Pending)
{
    return new BizWeldPointRecord
    {
        TaskId = taskId,
        StationNo = stationNo,
        ProductNo = productNo,
        SequenceNo = sequenceNo,
        ProductCompleted = true,
        UploadStatus = uploadStatus,
        Ts = DateTime.Today.AddSeconds(sequenceNo)
    };
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
    => AssertTrue(!condition, message);

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message)
{
    if (expected.Count == actual.Count
        && expected.Where((item, index) => EqualityComparer<T>.Default.Equals(item, actual[index])).Count() == expected.Count)
    {
        return;
    }

    throw new InvalidOperationException(
        $"{message} Expected=[{string.Join(",", expected)}], Actual=[{string.Join(",", actual)}]");
}
