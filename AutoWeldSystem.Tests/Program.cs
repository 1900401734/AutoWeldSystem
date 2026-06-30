using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
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
    ("PLC debug write rules parse bool aliases", PlcDebugWriteRulesParseBoolAliases),
    ("PLC debug write rules normalize unsupported data type", PlcDebugWriteRulesNormalizeUnsupportedDataType),
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
    ("Process parameter upload payload reads product scope fields", ProcessParameterUploadPayloadReadsProductScopeFields),
    ("Device lifecycle connection logs only when state changes", DeviceLifecycleConnectionLogsOnlyWhenStateChanges),
    ("Device lifecycle alarm logs enter change and recovery", DeviceLifecycleAlarmLogsEnterChangeAndRecovery),
    ("Program name rules extract component code", ProgramNameRulesExtractComponentCode),
    ("Program name rules reject invalid component code", ProgramNameRulesRejectInvalidComponentCode),
    ("Offline program dropdown displays program name", OfflineProgramDropdownDisplaysProgramName),
    ("Offline start request follows inline monitor input", OfflineStartRequestFollowsInlineMonitorInput),
    ("Program MES sync ignores local-only fields", ProgramMesSyncIgnoresLocalOnlyFields),
    ("Program MES sync detects remote fields", ProgramMesSyncDetectsRemoteFields),
    ("Program MES save action uses update for remote program content", ProgramMesSaveActionUsesUpdateForRemoteProgramContent),
    ("Program MES current save action separates pending actions", ProgramMesCurrentSaveActionSeparatesPendingActions),
    ("Program MES executable action never creates when MES id exists", ProgramMesExecutableActionNeverCreatesWhenMesIdExists),
    ("Program remark rules default by action", ProgramRemarkRulesDefaultByAction),
    ("Program MES write payload omits recipe code", ProgramMesWritePayloadOmitsRecipeCode),
    ("Monitor report button rules follow MES and task state", MonitorReportButtonRulesFollowMesAndTaskState),
    ("Program content rows come from dictionary items", ProgramContentRowsComeFromDictionaryItems),
    ("Program content JSON keeps only rows with standard values", ProgramContentJsonKeepsOnlyRowsWithStandardValues),
    ("Program content JSON merges existing values and preserves unknown keys", ProgramContentJsonMergesExistingValuesAndPreservesUnknownKeys),
    ("Program content JSON rejects duplicate valued item names", ProgramContentJsonRejectsDuplicateValuedItemNames),
    ("Program file rules build safe json file and base64", ProgramFileRulesBuildSafeJsonFileAndBase64),
    ("Work-order auto query skips duplicates and running tasks", WorkOrderAutoQuerySkipsDuplicatesAndRunningTasks)
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

static void PlcDebugWriteRulesParseBoolAliases()
{
    AssertTrue(PlcDebugWriteRules.TryParseBool("1", out var one), "Bool 写入应接受 1。");
    AssertTrue(one, "1 应解析为 true。");
    AssertTrue(PlcDebugWriteRules.TryParseBool("true", out var lowerTrue), "Bool 写入应接受 true。");
    AssertTrue(lowerTrue, "true 应解析为 true。");
    AssertTrue(PlcDebugWriteRules.TryParseBool("0", out var zero), "Bool 写入应接受 0。");
    AssertFalse(zero, "0 应解析为 false。");
    AssertTrue(PlcDebugWriteRules.TryParseBool("FALSE", out var upperFalse), "Bool 写入应大小写不敏感。");
    AssertFalse(upperFalse, "FALSE 应解析为 false。");
    AssertFalse(PlcDebugWriteRules.TryParseBool("yes", out _), "Bool 写入不应接受含糊文本。");
}

static void PlcDebugWriteRulesNormalizeUnsupportedDataType()
{
    AssertEqual(AppConstants.PlcDataTypes.Bool, PlcDebugWriteRules.NormalizeDataType("Bool"), "已支持类型应保持不变。");
    AssertEqual(AppConstants.PlcDataTypes.Int16, PlcDebugWriteRules.NormalizeDataType("word"), "未知类型应回退到 Int16。");
    AssertEqual(AppConstants.PlcDataTypes.Int16, PlcDebugWriteRules.NormalizeDataType(null), "空类型应回退到 Int16。");
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

static void DeviceLifecycleConnectionLogsOnlyWhenStateChanges()
{
    AssertTrue(DeviceLifecycleLogRules.HasConnectionStatusChanged(null, currentConnected: false), "首次自检失败也需要记录，方便现场知道自检结果。");
    AssertFalse(DeviceLifecycleLogRules.HasConnectionStatusChanged(false, currentConnected: false), "持续失败不应重复写设备日志。");
    AssertTrue(DeviceLifecycleLogRules.HasConnectionStatusChanged(false, currentConnected: true), "失败恢复成功时需要记录设备日志。");

    var entry = DeviceLifecycleLogRules.CreateSelfCheckEntry(
        deviceId: "D-001",
        stationNo: 1,
        source: "PLC",
        connected: true,
        message: "PLC 已连接",
        occurredTime: new DateTime(2026, 6, 26, 8, 0, 0, 123));

    AssertEqual(AppConstants.DeviceLifecycleEventTypes.SelfCheck, entry.EventType, "连接自检日志必须使用 SelfCheck 事件类型。");
    AssertEqual("Success", entry.Status, "连接成功状态应保存为 Success。");
    AssertEqual("PLC自检成功", entry.Summary, "连接自检摘要应直接表达被检测对象和结果。");
    AssertEqual("D-001", entry.DeviceId, "设备日志必须携带设备编号。");
    AssertEqual(1, entry.StationNo, "PLC 自检日志必须携带工位。");
}

static void DeviceLifecycleAlarmLogsEnterChangeAndRecovery()
{
    var enter = DeviceLifecycleLogRules.DecideAlarmTransition(
        previousStatusCode: null,
        previousAlarmMessage: "",
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        currentAlarmMessage: "气压低");
    AssertTrue(enter.ShouldWrite, "PLC 首次进入报警时必须写设备日志。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.FaultAlarm, enter.EventType, "进入报警应记录 FaultAlarm。");

    var duplicate = DeviceLifecycleLogRules.DecideAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        previousAlarmMessage: "气压低",
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        currentAlarmMessage: "气压低");
    AssertFalse(duplicate.ShouldWrite, "报警状态和原因都未变化时不应重复写设备日志。");

    var changed = DeviceLifecycleLogRules.DecideAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        previousAlarmMessage: "气压低",
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        currentAlarmMessage: "安全门打开");
    AssertTrue(changed.ShouldWrite, "报警原因变化时需要再次写设备日志。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.FaultAlarm, changed.EventType, "报警原因变化仍属于 FaultAlarm。");

    var recovered = DeviceLifecycleLogRules.DecideAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        previousAlarmMessage: "安全门打开",
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Running,
        currentAlarmMessage: "");
    AssertTrue(recovered.ShouldWrite, "PLC 从报警恢复到非报警时必须写恢复日志。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.FaultRecovered, recovered.EventType, "报警恢复应记录 FaultRecovered。");
}

static void ProgramNameRulesExtractComponentCode()
{
    AssertTrue(
        ProgramNameRules.TryExtractComponentCode("D001_CX_ABC123_DH_001_P001", out var componentCode),
        "标准程序名称应能解析出零组件代码。");
    AssertEqual("ABC123", componentCode, "零组件代码必须取 _CX_ 与 _DH_ 之间的原始片段。");

    AssertTrue(
        ProgramNameRules.TryExtractComponentCode("D001_cx_ZJ-987_dh_001_P001", out var lowerCaseComponentCode),
        "MES 返回大小写不同的标记时仍应兼容。");
    AssertEqual("ZJ-987", lowerCaseComponentCode, "大小写兼容不能改变零组件代码本身。");
}

static void ProgramNameRulesRejectInvalidComponentCode()
{
    AssertFalse(
        ProgramNameRules.TryExtractComponentCode("D001_ABC123_DH_001_P001", out var missingStartCode),
        "缺少 _CX_ 标记时不能生成伪零组件代码。");
    AssertEqual(string.Empty, missingStartCode, "解析失败时输出必须为空。");

    AssertFalse(
        ProgramNameRules.TryExtractComponentCode("D001_CX_ABC123_001_P001", out var missingEndCode),
        "缺少 _DH_ 标记时不能生成伪零组件代码。");
    AssertEqual(string.Empty, missingEndCode, "缺少结束标记时输出必须为空。");

    AssertFalse(
        ProgramNameRules.TryExtractComponentCode("D001_CX__DH_001_P001", out var emptyCode),
        "零组件代码片段为空时不能生成伪零组件代码。");
    AssertEqual(string.Empty, emptyCode, "空片段解析失败时输出必须为空。");
}

static void OfflineProgramDropdownDisplaysProgramName()
{
    var programs = new[]
    {
        new BizProgram
        {
            Id = 7,
            ProgramName = "程序A",
            ProgramContent = "内容A",
            ProductNum = "P-001",
            RecipeCode = "3",
            UpdatedTime = new DateTime(2026, 6, 26, 8, 0, 0)
        },
        new BizProgram
        {
            Id = 8,
            ProgramName = "重复程序",
            ProgramContent = "内容B",
            ProductNum = "P-002",
            RecipeCode = "4",
            UpdatedTime = new DateTime(2026, 6, 26, 9, 0, 0)
        },
        new BizProgram
        {
            Id = 9,
            ProgramName = "重复程序",
            ProgramContent = "内容C",
            ProductNum = "P-003",
            RecipeCode = "5",
            UpdatedTime = new DateTime(2026, 6, 26, 10, 0, 0)
        }
    };

    var options = OfflineStartInputRules.BuildProgramNameOptions(programs);

    AssertEqual(3, options.Count, "可用本地程序应生成离线程序名称选项。");
    var uniqueOption = options.Single(option => option.Program.Id == 7);
    AssertEqual("程序A", uniqueOption.DisplayText, "唯一程序名称下拉必须优先显示程序名称。");
    AssertEqual("P-001", uniqueOption.Program.ProductNum, "选中程序名称后仍需保留产品工号用于联动回填。");
    AssertEqual("3", uniqueOption.Program.RecipeCode, "选中程序名称后仍需保留配方号用于联动回填。");

    var duplicateOption = options.Single(option => option.Program.Id == 8);
    AssertEqual("重复程序 | 产品工号=P-002 | 配方号=4", duplicateOption.DisplayText, "重名程序必须追加产品工号和配方号便于区分。");
}

static void OfflineStartRequestFollowsInlineMonitorInput()
{
    var option = OfflineStartInputRules.BuildProgramNameOptions(new[]
    {
        new BizProgram
        {
            Id = 9,
            ProgramId = "MES-P9",
            ProgramName = "离线程序",
            ProgramType = "1",
            ProgramContent = "{\"steps\":3}",
            ProductNum = "164#J",
            ProductModel = "M-164",
            RecipeCode = "5"
        }
    }).Single();
    var input = new OfflineStartInput(
        StationNo: 2,
        WorkOrderId: "WO-LOCAL",
        Batch: "B001",
        Spec: "S001",
        ProcessNo: "OP20",
        ProcessName: "离线焊接",
        PlannedQtyText: "12",
        ProductName: "引出线",
        DrawingNo: "DR-9");

    var request = OfflineStartInputRules.BuildRequest(input, option);

    AssertEqual(2, request.StationNo, "离线开工应使用当前 MonitorView 工位。");
    AssertEqual("WO-LOCAL", request.WorkOrderId, "离线开工应使用界面输入的工单号。");
    AssertEqual("引出线", request.ProductName, "离线开工应使用界面输入的产品名称。");
    AssertEqual("DR-9", request.DrawingNo, "离线开工应使用界面输入的图号。");
    AssertEqual("OP20", request.ProcessNo, "离线开工应使用界面输入的工序号。");
    AssertEqual(12, request.PlannedQty, "离线开工应使用界面输入的计划数量。");
    AssertEqual("164#J", request.ProductNum, "离线开工应使用选中程序关联的产品工号。");
    AssertEqual("M-164", request.ProductModel, "离线开工应使用选中程序关联的产品型号。");
    AssertEqual("5", request.RecipeCode, "离线开工应使用选中程序关联的配方号。");
    AssertEqual("{\"steps\":3}", request.ProgramContent, "离线开工应使用选中程序的程序内容。");
}

static void ProgramMesSyncIgnoresLocalOnlyFields()
{
    var original = BuildSyncedProgram();
    var edited = BuildSyncedProgram();
    original.ProgramContent = "{ \"高度\": \"12.5\", \"压力\": { \"min\": \"1\", \"max\": \"9\" } }";
    original.ProgramFile = ProgramFileRules.EncodeJsonToBase64(original.ProgramContent);
    edited.ProgramContent = "{\"压力\":{\"max\":\"9\",\"min\":\"1\"},\"高度\":\"12.5\"}";
    edited.ProgramFile = ProgramFileRules.EncodeJsonToBase64(edited.ProgramContent);
    edited.RecipeCode = "8";
    edited.ProductModel = "M-2";
    edited.ComponentCode = "CX-2";
    edited.Description = "只改本地备注";
    edited.SequenceNumber = 9;
    edited.ProgramFileName = "local-only.txt";

    AssertFalse(
        ProgramMesSyncRules.HasMesUploadFieldChanges(original, edited),
        "只修改本地辅助字段时不应触发 MES 更新。");
    AssertEqual(
        (string?)null,
        ProgramMesSyncRules.ResolveCurrentSaveAction(original, edited),
        "只修改本地辅助字段时，本次保存不应产生 MES 同步动作。");

    var legacyFileOriginal = BuildSyncedProgram();
    var regeneratedFileEdited = BuildSyncedProgram();
    legacyFileOriginal.ProgramContent = "{\"高度\":\"12.5\"}";
    legacyFileOriginal.ProgramFile = "历史旧格式文件内容";
    regeneratedFileEdited.ProgramContent = legacyFileOriginal.ProgramContent;
    regeneratedFileEdited.ProgramFile = ProgramFileRules.EncodeJsonToBase64(regeneratedFileEdited.ProgramContent);
    regeneratedFileEdited.RecipeCode = "9";
    regeneratedFileEdited.ProgramFileName = "9_P1.json";

    AssertFalse(
        ProgramMesSyncRules.HasMesUploadFieldChanges(legacyFileOriginal, regeneratedFileEdited),
        "只改配方号导致本地程序文件重新生成时，不应把派生的 ProgramFile 差异当成 MES 更新。");
    AssertEqual(
        (string?)null,
        ProgramMesSyncRules.ResolveCurrentSaveAction(legacyFileOriginal, regeneratedFileEdited),
        "只改配方号导致本地程序文件重新生成时，本次保存不应产生 MES 同步动作。");
}

static void ProgramMesSyncDetectsRemoteFields()
{
    var mesFields = new Action<BizProgram>[]
    {
        program => program.ProgramName = "P-Changed",
        program => program.DeviceId = "D-Changed",
        program => program.ProgramContent = "{\"steps\":2}",
        program => program.ProgramType = "1",
        program => program.ProductNum = "PN-Changed",
        program => program.ProgramFile = "BASE64-CHANGED",
        program => program.Remark = "用户填写备注"
    };

    foreach (var change in mesFields)
    {
        var original = BuildSyncedProgram();
        var edited = BuildSyncedProgram();
        change(edited);

        AssertTrue(
            ProgramMesSyncRules.HasMesUploadFieldChanges(original, edited),
            "MES 上传字段变化时必须触发 MES 更新。");
    }
}

static void ProgramMesSaveActionUsesUpdateForRemoteProgramContent()
{
    var original = BuildSyncedProgram();
    var edited = BuildSyncedProgram();
    edited.ProgramContent = "{\"高度\":\"13.0\"}";
    edited.ProgramFile = ProgramFileRules.EncodeJsonToBase64(edited.ProgramContent);

    var action = ProgramMesSyncRules.ResolveSaveAction(original, edited, hadPendingAction: false);

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        action,
        "已有 MES 程序 ID 的程序内容变化时，保存动作必须是 Update，不能重新 Create。");

    original.SyncAction = AppConstants.ProgramSyncActions.Create;
    original.SyncStatus = AppConstants.ProgramSyncStatus.PendingCreate;
    edited.SyncAction = AppConstants.ProgramSyncActions.Create;
    edited.SyncStatus = AppConstants.ProgramSyncStatus.PendingCreate;

    var staleCreateAction = ProgramMesSyncRules.ResolveSaveAction(original, edited, hadPendingAction: true);

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        staleCreateAction,
        "即使本地残留 Create 动作，只要已有 MES 程序 ID，修改程序内容也必须转为 Update。");
}

static void ProgramMesCurrentSaveActionSeparatesPendingActions()
{
    var original = BuildSyncedProgram();
    original.SyncAction = AppConstants.ProgramSyncActions.Update;
    original.SyncStatus = AppConstants.ProgramSyncStatus.PendingUpdate;

    var localOnlyEdited = BuildSyncedProgram();
    localOnlyEdited.SyncAction = AppConstants.ProgramSyncActions.Update;
    localOnlyEdited.SyncStatus = AppConstants.ProgramSyncStatus.PendingUpdate;
    localOnlyEdited.ProductModel = "M-Local";
    localOnlyEdited.RecipeCode = "8";
    localOnlyEdited.Description = "只改本地字段";
    localOnlyEdited.ProgramFileName = "P1.json";

    AssertEqual(
        (string?)null,
        ProgramMesSyncRules.ResolveCurrentSaveAction(original, localOnlyEdited),
        "已有历史待同步 Update 时，只改本地字段也不应成为本次保存的同步动作。");
    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        ProgramMesSyncRules.ResolveSaveAction(original, localOnlyEdited, hadPendingAction: true),
        "已有历史待同步 Update 时，本地字段保存应保留原待同步状态，不能误删。");

    var contentEdited = BuildSyncedProgram();
    contentEdited.ProgramContent = "{\"高度\":\"13.0\"}";
    contentEdited.ProgramFile = ProgramFileRules.EncodeJsonToBase64(contentEdited.ProgramContent);

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        ProgramMesSyncRules.ResolveCurrentSaveAction(original, contentEdited),
        "实际修改程序内容时，本次保存应产生 MES Update。");

    var remarkEdited = BuildSyncedProgram();
    remarkEdited.Remark = "人工备注";

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        ProgramMesSyncRules.ResolveCurrentSaveAction(original, remarkEdited),
        "用户自定义 MES 备注变化时，本次保存应产生 MES Update。");
}

static void ProgramMesExecutableActionNeverCreatesWhenMesIdExists()
{
    AssertEqual(
        null,
        ProgramMesSyncRules.ResolveExecutableSyncAction(null, "MES-1"),
        "没有待同步动作时，手动点击同步不应兜底调用新增接口。");

    AssertEqual(
        AppConstants.ProgramSyncActions.Create,
        ProgramMesSyncRules.ResolveExecutableSyncAction(AppConstants.ProgramSyncActions.Create, null),
        "本地新程序没有 MES ID 时，Create 动作才允许调用新增接口。");

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        ProgramMesSyncRules.ResolveExecutableSyncAction(AppConstants.ProgramSyncActions.Create, "MES-1"),
        "残留 Create 动作但已有 MES ID 时，执行同步必须转为 Update，避免重复新增。");

    AssertEqual(
        AppConstants.ProgramSyncActions.Update,
        ProgramMesSyncRules.ResolveExecutableSyncAction(AppConstants.ProgramSyncActions.Update, "MES-1"),
        "有 MES ID 的 Update 动作应调用更新接口。");

    AssertEqual(
        null,
        ProgramMesSyncRules.ResolveExecutableSyncAction(AppConstants.ProgramSyncActions.Update, null),
        "缺少 MES ID 的 Update 动作不能兜底调用新增接口。");
}

static void ProgramRemarkRulesDefaultByAction()
{
    AssertEqual(
        "更新",
        AppConstants.ProgramRemarkActions.Update,
        "MES 更新程序备注动作必须使用接口约定的“更新”。");

    AssertEqual(
        AppConstants.ProgramRemarkActions.Create,
        ProgramRemarkRules.ResolveForAction(null, AppConstants.ProgramSyncActions.Create),
        "新增程序未填写 MES 备注时应默认新增。");

    AssertEqual(
        "更新",
        ProgramRemarkRules.ResolveForAction(" ", AppConstants.ProgramSyncActions.Update),
        "更新程序未填写 MES 备注时应默认更新。");

    AssertEqual(
        "更新",
        ProgramRemarkRules.ResolveForAction("新增", AppConstants.ProgramSyncActions.Update),
        "历史默认新增备注遇到更新动作时不应被当成人工备注复用。");

    AssertEqual(
        "更新",
        ProgramRemarkRules.ResolveForAction("修改", AppConstants.ProgramSyncActions.Update),
        "历史默认修改备注遇到更新动作时应统一改为更新。");

    AssertEqual(
        "更新",
        ProgramRemarkRules.ResolveForAction("删除", AppConstants.ProgramSyncActions.Update),
        "系统动作删除备注遇到更新动作时不应被当成人工备注复用。");

    AssertEqual(
        AppConstants.ProgramRemarkActions.Delete,
        ProgramRemarkRules.ResolveForAction(null, AppConstants.ProgramSyncActions.Delete),
        "删除程序未填写 MES 备注时应默认删除。");

    AssertEqual(
        "人工备注",
        ProgramRemarkRules.ResolveForAction("  人工备注  ", AppConstants.ProgramSyncActions.Update),
        "用户填写 MES 备注时应优先保留用户内容。");
}

static void ProgramMesWritePayloadOmitsRecipeCode()
{
    var program = BuildSyncedProgram();
    program.RecipeCode = "99";
    program.ProgramFileName = "1001_P1.JSON";

    var payload = ProgramMesPayloadRules.ToWriteRequest(program, AppConstants.ProgramRemarkActions.Update);
    var json = JsonSerializer.Serialize(payload);
    using var document = JsonDocument.Parse(json);
    var fileType = document.RootElement.GetProperty(nameof(ProgramDataWriteReq.FileType));

    AssertFalse(
        json.Contains(nameof(ProgramDataRes.RecipeCode), StringComparison.OrdinalIgnoreCase),
        "MES 新增/更新程序请求不应包含 RecipeCode。");
    AssertEqual("P1", payload.ProgramName, "MES 写入请求仍应携带程序名称。");
    AssertEqual(AppConstants.ProgramRemarkActions.Update, payload.Remark, "MES 写入请求应携带解析后的备注。");
    AssertEqual(".json", payload.FileType, "MES 写入请求应携带程序文件扩展名字符串。");
    AssertEqual(JsonValueKind.String, fileType.ValueKind, "MES 写入请求中的 FileType 必须是字符串。");
    AssertEqual(".json", fileType.GetString(), "MES 写入请求中的 FileType 应使用带点小写扩展名。");
}

static void MonitorReportButtonRulesFollowMesAndTaskState()
{
    var idleOnline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: false);
    AssertTrue(idleOnline.ShowStartReportButton, "在线空闲时应显示开工上报。");
    AssertFalse(idleOnline.ShowFinishReportButton, "在线空闲时不应显示完工上报。");
    AssertTrue(idleOnline.OnlineReportEnabled, "MES 在线时在线上报按钮应可用。");
    AssertFalse(idleOnline.LocalWorkOrderEnabled, "MES 在线空闲时应禁用离线开工。");

    var runningOnline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: true,
        hasOfflineRunningTask: false);
    AssertFalse(runningOnline.ShowStartReportButton, "在线开工后不应继续显示开工上报。");
    AssertTrue(runningOnline.ShowFinishReportButton, "在线开工后应显示完工上报。");

    var offline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: false,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: false);
    AssertFalse(offline.OnlineReportEnabled, "MES 离线时在线上报按钮应禁用。");
    AssertTrue(offline.LocalWorkOrderEnabled, "MES 离线空闲时离线开工应可用。");

    var offlineTaskWhenMesBack = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: true);
    AssertTrue(offlineTaskWhenMesBack.LocalWorkOrderEnabled, "已有离线未完工任务时即使 MES 恢复也应允许本地完工。");
}

static void ProgramContentRowsComeFromDictionaryItems()
{
    var rows = ProgramContentJsonRules.BuildRows(
        new[]
        {
            new DimTestItem { ItemId = 2, ItemName = "高度" },
            new DimTestItem { ItemId = 1, ItemName = "压力" }
        },
        existingJson: null);

    AssertEqual(2, rows.Count, "测试项字典非空时，程序内容表格应按字典生成行。");
    AssertEqual("高度", rows[0].ItemName, "行名称必须来自测试项字典。");
    AssertTrue(rows[0].IsDictionaryItem, "字典生成的行需要标记为字典项，便于 UI 控制名称列只读。");
}

static void ProgramContentJsonKeepsOnlyRowsWithStandardValues()
{
    var rows = new[]
    {
        new ProgramContentItemRow { ItemName = "高度", StandardValue = "12.5", IsDictionaryItem = true },
        new ProgramContentItemRow { ItemName = "压力", StandardValue = "", IsDictionaryItem = true },
        new ProgramContentItemRow { ItemName = "", StandardValue = "should-skip", IsDictionaryItem = false }
    };

    var json = ProgramContentJsonRules.ToJson(rows);
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    AssertEqual(1, root.EnumerateObject().Count(), "只有设定值非空且测试项名称非空的行才应进入 ProgramContent JSON。");
    AssertEqual("12.5", root.GetProperty("高度").GetString(), "JSON value 必须按字符串保存，不做数值推断。");
    AssertFalse(root.TryGetProperty("压力", out _), "设定值为空的测试项不应上传。");
}

static void ProgramContentJsonMergesExistingValuesAndPreservesUnknownKeys()
{
    var rows = ProgramContentJsonRules.BuildRows(
        new[] { new DimTestItem { ItemId = 1, ItemName = "高度" } },
        "{\"高度\":\"12.5\",\"旧测试项\":\"A1\"}");

    AssertEqual(2, rows.Count, "绑定已有程序时，不在当前字典内的旧 key 也应显示，避免旧数据丢失。");
    AssertEqual("12.5", rows[0].StandardValue, "当前字典项应回填已有 JSON 中的设定值。");
    AssertEqual("旧测试项", rows[1].ItemName, "旧 JSON 中的未知 key 应追加为额外行。");
    AssertEqual("A1", rows[1].StandardValue, "未知 key 的值也需要保留。");
    AssertFalse(rows[1].IsDictionaryItem, "未知 key 不是当前字典项，UI 可按手动行处理。");
}

static void ProgramContentJsonRejectsDuplicateValuedItemNames()
{
    var rows = new[]
    {
        new ProgramContentItemRow { ItemName = "高度", StandardValue = "12.5" },
        new ProgramContentItemRow { ItemName = "高度", StandardValue = "13.0" }
    };

    AssertThrows<InvalidOperationException>(
        () => ProgramContentJsonRules.ToJson(rows),
        "重复测试项名称且都有设定值时必须阻止保存，避免 JSON key 覆盖。");
}

static void ProgramFileRulesBuildSafeJsonFileAndBase64()
{
    var fileName = ProgramFileRules.BuildFileName("  1001  ", "A:B/程序");
    var fileNameWithoutRecipe = ProgramFileRules.BuildFileName(null, "P1");
    var json = "{\"高度\":\"12.5\"}";
    var base64 = ProgramFileRules.EncodeJsonToBase64(json);
    var restored = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));

    AssertEqual("A_B_程序.json", fileName, "程序文件名应只使用程序名称，并替换非法文件名字符。");
    AssertEqual("P1.json", fileNameWithoutRecipe, "程序文件名不应依赖配方号。");
    AssertEqual(json, restored, "程序文件 Base64 必须可还原为原始 UTF-8 JSON。");
    AssertEqual(".json", ProgramFileRules.ResolveFileType("1001_P1.json"), "json 程序文件应返回 .json 文件类型。");
    AssertEqual(".txt", ProgramFileRules.ResolveFileType(@"C:\temp\a.TXT"), "文件类型应统一返回带点小写扩展名。");
    AssertEqual(".json", ProgramFileRules.ResolveFileType(""), "文件名为空时应默认按自动生成的 JSON 程序文件处理。");
}

static void WorkOrderAutoQuerySkipsDuplicatesAndRunningTasks()
{
    AssertTrue(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "WO-1",
            lastRequestedWorkId: null,
            queryInProgress: false),
        "MES 在线、空闲且读取到新工单号时应自动查询。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "WO-1",
            lastRequestedWorkId: "WO-1",
            queryInProgress: false),
        "同一工位同一工单号已处理时不应重复自动查询。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: true,
            workIdReadSuccess: true,
            workId: "WO-2",
            lastRequestedWorkId: null,
            queryInProgress: false),
        "运行中任务必须锁定当前工单，不允许扫码自动覆盖。");
}

static BizProgram BuildSyncedProgram()
{
    return new BizProgram
    {
        Id = 1,
        ProgramId = "MES-1",
        ProgramName = "P1",
        DeviceId = "D1",
        ProgramContent = "{\"steps\":1}",
        ProgramType = "0",
        ProductNum = "PN1",
        ProgramFile = "BASE64",
        Remark = "旧备注",
        RecipeCode = "1",
        ProductModel = "M-1",
        ComponentCode = "CX-1",
        Description = "本地备注",
        SequenceNumber = 1,
        ProgramFileName = "old.txt",
        SyncAction = null,
        SyncStatus = AppConstants.ProgramSyncStatus.Synced
    };
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

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"{message} Expected={typeof(TException).Name}, Actual={ex.GetType().Name}");
    }

    throw new InvalidOperationException($"{message} Expected={typeof(TException).Name}, Actual=no exception");
}
