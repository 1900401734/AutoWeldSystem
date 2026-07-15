using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.DTOs.DeviceApi;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Security;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Services.Mes;
using AutoWeldSystem.Services.Log;
using AutoWeldSystem.Services.Production;
using System.Net;
using System.Text;
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
    ("Alarm address import rules parse engineering document rows", AlarmAddressImportRulesParseEngineeringDocumentRows),
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
    ("MES device status rules use configured MES codes", MesDeviceStatusRulesUseConfiguredMesCodes),
    ("MES device status rules convert PLC alarm transitions", MesDeviceStatusRulesConvertPlcAlarmTransitions),
    ("MES device status rules use latest device id for report", MesDeviceStatusRulesUseLatestDeviceIdForReport),
    ("MES device status rules format status identity", MesDeviceStatusRulesFormatStatusIdentity),
    ("MES device status rules format station remarks", MesDeviceStatusRulesFormatStationRemarks),
    ("MES device status duplicate suppression honors lifecycle force write", MesDeviceStatusDuplicateSuppressionHonorsLifecycleForceWrite),
    ("Log timestamp display rules switch date visibility", LogTimestampDisplayRulesSwitchDateVisibility),
    ("Antd table selection helper maps selected indexes", AntdTableSelectionHelperMapsSelectedIndexes),
    ("Device status local log store resolves directories", DeviceStatusLocalLogStoreResolvesDirectories),
    ("Device status local log store writes and reads jsonl", DeviceStatusLocalLogStoreWritesAndReadsJsonl),
    ("Device status report keeps millisecond timestamp after MES upload", DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload),
    ("Device status local log store keeps latest state per log id", DeviceStatusLocalLogStoreKeepsLatestStatePerLogId),
    ("LogManageView device status tab exposes open folder button", LogManageViewDeviceStatusTabExposesOpenFolderButton),
    ("DataManageView static grids define bound columns", DataManageViewStaticGridsDefineBoundColumns),
    ("DataManageView ignores report selection while disposing", DataManageViewIgnoresReportSelectionWhileDisposing),
    ("DataManageView ignores work order selection while disposing", DataManageViewIgnoresWorkOrderSelectionWhileDisposing),
    ("DataManageView treats cancelled history queries as stale work", DataManageViewTreatsCancelledHistoryQueriesAsStaleWork),
    ("Device API status query returns current MES status", DeviceApiStatusQueryReturnsCurrentMesStatus),
    ("Device API status query rejects mismatched device id", DeviceApiStatusQueryRejectsMismatchedDeviceId),
    ("Device API set device id saves local settings as synced", DeviceApiSetDeviceIdSavesLocalSettingsAsSynced),
    ("Device API set device id rejects mismatched old device id", DeviceApiSetDeviceIdRejectsMismatchedOldDeviceId),
    ("Device API rules build and parse status url", DeviceApiRulesBuildAndParseStatusUrl),
    ("Device API HTTP self check rules write success and failure", DeviceApiHttpSelfCheckRulesWriteSuccessAndFailure),
    ("Device API status query writes lifecycle log every time", DeviceApiStatusQueryWritesLifecycleLogEveryTime),
    ("Device API status query mismatch writes failure lifecycle log", DeviceApiStatusQueryMismatchWritesFailureLifecycleLog),
    ("Device API set device id writes lifecycle logs", DeviceApiSetDeviceIdWritesLifecycleLogs),
    ("Device API ignores lifecycle log failure", DeviceApiIgnoresLifecycleLogFailure),
    ("Upload status display rules localize status text", UploadStatusDisplayRulesLocalizeStatusText),
    ("Upload status navigation uses pending upload data text", UploadStatusNavigationUsesPendingUploadDataText),
    ("State manage summary tab uses work order info text", StateManageSummaryTabUsesWorkOrderInfoText),
    ("State manage upload status display follows MES connection", StateManageUploadStatusDisplayFollowsMesConnection),
    ("State manage tabs are cataloged as role permissions", StateManageTabsAreCatalogedAsRolePermissions),
    ("Global permission checks separate developer and admin", GlobalPermissionChecksSeparateDeveloperAndAdmin),
    ("State tab defaults keep customer tabs configurable", StateTabDefaultsKeepCustomerTabsConfigurable),
    ("State manage view filters tabs by current permissions", StateManageViewFiltersTabsByCurrentPermissions),
    ("State manage device status tab supports multi delete", StateManageDeviceStatusTabSupportsMultiDelete),
    ("Skipped upload tasks are not retried", SkippedUploadTasksAreNotRetried),
    ("Status report settings default to enabled", StatusReportSettingsDefaultToEnabled),
    ("MES route settings default to current routes", MesRouteSettingsDefaultToCurrentRoutes),
    ("MES provider uses configured routes", MesProviderUsesConfiguredRoutes),
    ("MES provider applies PostData header from latest settings", MesProviderAppliesPostDataHeaderFromLatestSettings),
    ("Elevated auto start defaults to enabled", ElevatedAutoStartDefaultsToEnabled),
    ("Startup integration rules remove all when auto start is disabled", StartupIntegrationRulesRemoveAllWhenAutoStartIsDisabled),
    ("Startup integration rules prefer elevated scheduled task", StartupIntegrationRulesPreferElevatedScheduledTask),
    ("Startup integration result reports run key fallback", StartupIntegrationResultReportsRunKeyFallback),
    ("System clock sync skips small offset", SystemClockSyncSkipsSmallOffset),
    ("System clock sync changes large offset", SystemClockSyncChangesLargeOffset),
    ("System clock sync rejects invalid server time", SystemClockSyncRejectsInvalidServerTime),
    ("Weld task server time sync adjusts system clock", WeldTaskServerTimeSyncAdjustsSystemClock),
    ("Weld task server time sync skips clock on MES failure", WeldTaskServerTimeSyncSkipsClockOnMesFailure),
    ("Weld task server time sync reports clock failure", WeldTaskServerTimeSyncReportsClockFailure),
    ("Device lifecycle server time self check uses self check event", DeviceLifecycleServerTimeSelfCheckUsesSelfCheckEvent),
    ("Weld task server time sync writes device lifecycle success log", WeldTaskServerTimeSyncWritesDeviceLifecycleSuccessLog),
    ("Weld task server time sync writes device lifecycle failure logs", WeldTaskServerTimeSyncWritesDeviceLifecycleFailureLogs),
    ("Weld task server time sync ignores device lifecycle log failure", WeldTaskServerTimeSyncIgnoresDeviceLifecycleLogFailure),
    ("Device lifecycle self check summaries describe connection result", DeviceLifecycleSelfCheckSummariesDescribeConnectionResult),
    ("Device lifecycle software close entry records software close", DeviceLifecycleSoftwareCloseEntryRecordsSoftwareClose),
    ("Device lifecycle coordinator records software lifecycle statuses", DeviceLifecycleCoordinatorRecordsSoftwareLifecycleStatuses),
    ("Device lifecycle coordinator syncs software status timestamps", DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps),
    ("Device lifecycle stop triggers background status upload", DeviceLifecycleStopTriggersBackgroundStatusUpload),
    ("Device lifecycle stop reports status when lifecycle log fails", DeviceLifecycleStopReportsStatusWhenLifecycleLogFails),
    ("Device lifecycle connection logs only when state changes", DeviceLifecycleConnectionLogsOnlyWhenStateChanges),
    ("Device lifecycle alarm logs enter change and recovery", DeviceLifecycleAlarmLogsEnterChangeAndRecovery),
    ("Program name rules extract component code", ProgramNameRulesExtractComponentCode),
    ("Program name rules reject invalid component code", ProgramNameRulesRejectInvalidComponentCode),
    ("Offline program dropdown displays program name", OfflineProgramDropdownDisplaysProgramName),
    ("Offline program dropdown includes empty-content program", OfflineProgramDropdownIncludesEmptyContentProgram),
    ("Recipe code options sort numeric ascending", RecipeCodeOptionsSortNumericAscending),
    ("Product history preview sorts latest product first", ProductHistoryPreviewSortsLatestProductFirst),
    ("Offline start request follows inline monitor input", OfflineStartRequestFollowsInlineMonitorInput),
    ("Offline start allows empty part name and drawing number", OfflineStartAllowsEmptyPartNameAndDrawingNumber),
    ("Offline start requires work order and process number", OfflineStartRequiresWorkOrderAndProcessNumber),
    ("Program MES sync ignores local-only fields", ProgramMesSyncIgnoresLocalOnlyFields),
    ("Program MES sync detects remote fields", ProgramMesSyncDetectsRemoteFields),
    ("Program MES save action uses update for remote program content", ProgramMesSaveActionUsesUpdateForRemoteProgramContent),
    ("Program MES current save action separates pending actions", ProgramMesCurrentSaveActionSeparatesPendingActions),
    ("Program MES executable action never creates when MES id exists", ProgramMesExecutableActionNeverCreatesWhenMesIdExists),
    ("Program remark rules default by action", ProgramRemarkRulesDefaultByAction),
    ("Program MES write payload omits recipe code", ProgramMesWritePayloadOmitsRecipeCode),
    ("Program MES create payload clears file fields for empty content", ProgramMesCreatePayloadClearsFileFieldsForEmptyContent),
    ("Program content rules detect configured values", ProgramContentRulesDetectConfiguredValues),
    ("Program manage service clears automatic file for empty content", ProgramManageServiceClearsAutomaticFileForEmptyContent),
    ("Program manage view hides product model", ProgramManageViewHidesProductModel),
    ("Program manage save ignores product model input", ProgramManageSaveIgnoresProductModelInput),
    ("Monitor report button rules follow MES and task state", MonitorReportButtonRulesFollowMesAndTaskState),
    ("Monitor view uses one online report button", MonitorViewUsesOneOnlineReportButton),
    ("Monitor runtime tips use localized summaries", MonitorRuntimeTipsUseLocalizedSummaries),
    ("Monitor view shows operator validation success after employee validation", MonitorViewShowsOperatorValidationSuccessAfterEmployeeValidation),
    ("Monitor view keeps inline operator validation marker after binding", MonitorViewKeepsInlineOperatorValidationMarkerAfterBinding),
    ("Monitor view auto loads work order without query button", MonitorViewAutoLoadsWorkOrderWithoutQueryButton),
    ("Monitor view preserves online inputs during refresh", MonitorViewPreservesOnlineInputsDuringRefresh),
    ("Monitor view links program and recipe selections for start input", MonitorViewLinksProgramAndRecipeSelectionsForStartInput),
    ("Monitor view recipe dropdown uses sorted recipe options", MonitorViewRecipeDropdownUsesSortedRecipeOptions),
    ("Monitor view uses PLC recipe only for offline idle inputs", MonitorViewUsesPlcRecipeOnlyForOfflineIdleInputs),
    ("Monitor view reloads online programs after process change", MonitorViewReloadsOnlineProgramsAfterProcessChange),
    ("Monitor view defaults first process inputs after work order load", MonitorViewDefaultsFirstProcessInputsAfterWorkOrderLoad),
    ("Monitor view process selection uses shared input binder", MonitorViewProcessSelectionUsesSharedInputBinder),
    ("Monitor view exposes dual work order toggle beside work order", MonitorViewExposesDualWorkOrderToggleBesideWorkOrder),
    ("Monitor view saves dual work order toggle with old rules", MonitorViewSavesDualWorkOrderToggleWithOldRules),
    ("System setting view no longer edits dual work order", SystemSettingViewNoLongerEditsDualWorkOrder),
    ("Monitor view finish report uses start operator without prompt", MonitorViewFinishReportUsesStartOperatorWithoutPrompt),
    ("Monitor view clears product identity after finish report", MonitorViewClearsProductIdentityAfterFinishReport),
    ("Monitor view product history uses latest first ordering", MonitorViewProductHistoryUsesLatestFirstOrdering),
    ("Weld task finish uses MES start id for retry payloads", WeldTaskFinishUsesMesStartIdForRetryPayloads),
    ("Weld task restore unfinished task is idempotent", WeldTaskRestoreUnfinishedTaskIsIdempotent),
    ("Permission catalog omits get work order button", PermissionCatalogOmitsGetWorkOrderButton),
    ("Program content rows come from dictionary items", ProgramContentRowsComeFromDictionaryItems),
    ("Program content JSON keeps only rows with standard values", ProgramContentJsonKeepsOnlyRowsWithStandardValues),
    ("Program content JSON merges existing values and preserves unknown keys", ProgramContentJsonMergesExistingValuesAndPreservesUnknownKeys),
    ("Program content JSON rejects duplicate valued item names", ProgramContentJsonRejectsDuplicateValuedItemNames),
    ("Program file rules build safe json file and base64", ProgramFileRulesBuildSafeJsonFileAndBase64),
    ("All select controls limit dropdown items", AllSelectControlsLimitDropdownItems),
    ("Work-order auto query skips duplicates and running tasks", WorkOrderAutoQuerySkipsDuplicatesAndRunningTasks),
    ("Work-order input confirmation rules distinguish drafts and PLC values", WorkOrderInputConfirmationRulesDistinguishDraftsAndPlcValues),
    ("Monitor view confirms manual work orders and prioritizes PLC snapshots", MonitorViewConfirmsManualWorkOrdersAndPrioritizesPlcSnapshots),
    ("Program list filter returns all when disabled", ProgramListFilterReturnsAllWhenDisabled),
    ("Program list filter narrows by product number when enabled", ProgramListFilterNarrowsByProductNumberWhenEnabled),
    ("Program list filter returns all when work order product number is blank", ProgramListFilterReturnsAllWhenWorkOrderProductNumberIsBlank),
    ("Program content review rows apply modified values", ProgramContentReviewRowsApplyModifiedValues),
    ("Program content review keeps standard value when modified value empty", ProgramContentReviewKeepsStandardValueWhenModifiedValueEmpty),
    ("Program content review rejects duplicate item names", ProgramContentReviewRejectsDuplicateItemNames),
    ("LoadPrograms filters available programs by work order product number", LoadProgramsFiltersAvailableProgramsByWorkOrderProductNumber),
    ("Select list rules resolve selection by display text", SelectListRulesResolveSelectionByDisplayText),
    ("Select list rules disambiguate duplicate display texts by event index", SelectListRulesDisambiguateDuplicateDisplayTextsByEventIndex)
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

static void AlarmAddressImportRulesParseEngineeringDocumentRows()
{
    var text = """
        序号	变量名	地址	报警内容	备注
        1	左夹紧报警	DBnBit-900080	左夹紧气缸，未到位	停机
        2	右夹紧报警	DB9.DBX9.1	右夹紧气缸未到位
        3,安全门报警,DB9.10.2,安全门打开,禁止启动
        4,DB9.11.3,"真空异常,请检查气路"
        5	安全光栅触发	DBnBit-900087
        6	DBnBit-1100113	左安全光栅被挡住
        7	DBnBit-1100113	右安全光栅被挡住
        """;

    var rows = AlarmAddressImportRules.ParseClipboard(text);

    AssertEqual(7, rows.Count, "工程文档中的每一个报警地址都应被解析。");
    AssertEqual("DB9.8.0", rows[0].Address, "DBnBit 工程地址应转换为 PLC 可读地址。");
    AssertEqual("左夹紧气缸，未到位", rows[0].Content, "存在表头时应使用报警内容列，不应混入备注列。");
    AssertEqual("DB9.9.1", rows[1].Address, "DBx.DBXy.z 地址应规范为 DBx.y.z。");
    AssertEqual("右夹紧气缸未到位", rows[1].Content, "地址前存在序号和变量名时不应漏导。");
    AssertEqual("安全门打开", rows[2].Content, "存在内容表头时应只读取报警内容列，不能混入备注列。");
    AssertEqual("真空异常,请检查气路", rows[3].Content, "CSV 引号内的逗号应保留在同一个内容单元格。");
    AssertEqual("安全光栅触发", rows[4].Content, "地址在最后一列时应使用地址前最近的有效文本作为内容。");
    AssertEqual(1, rows[5].StationNo, "左工位报警应导入为工位 1，避免与右工位同地址时互相覆盖。");
    AssertEqual(2, rows[6].StationNo, "右工位报警应导入为工位 2，避免与左工位同地址时互相覆盖。");

    var noHeaderRows = AlarmAddressImportRules.ParseClipboard("3,安全门报警,DB9.10.2,安全门打开,禁止启动");
    AssertEqual("安全门打开，禁止启动", noHeaderRows[0].Content, "无表头逗号文本中，地址后的多段内容应合并为完整报警内容。");
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
    AssertTrue(weldJson.Contains("\"IsTest\":0", StringComparison.Ordinal), "点焊设备开启全局试焊件后，即使 false 也必须输出 IsTest=0。");

    var testItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-2",
        IsTest = ProcessParameterIsTestRules.Resolve(recordIsTest: true, showTestFlagInHistory: true, ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic)
    };
    var testJson = JsonSerializer.Serialize(testItem);
    AssertTrue(testJson.Contains("\"IsTest\":1", StringComparison.Ordinal), "电磁设备标记试焊件后必须输出 IsTest=1。");

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

static void MesDeviceStatusRulesUseConfiguredMesCodes()
{
    AssertEqual("1", ProductionConstants.MesDeviceStatuses.PoweredOn, "MES 设备状态 1 必须表示软件开机。");
    AssertEqual("0", ProductionConstants.MesDeviceStatuses.Stopped, "MES 设备状态 0 必须表示软件停机。");
    AssertTrue(DeviceStatusReportRules.IsMesDeviceStatusCode("0"), "0 是合法 MES 设备状态。");
    AssertTrue(DeviceStatusReportRules.IsMesDeviceStatusCode("7"), "7 是合法 MES 设备状态。");
    AssertFalse(DeviceStatusReportRules.IsMesDeviceStatusCode("2"), "PLC 原始状态 2 不应作为 MES 设备状态上传。");
    AssertFalse(DeviceStatusReportRules.IsMesDeviceStatusCode("3"), "PLC 原始状态 3 不应作为 MES 设备状态上传。");
    AssertEqual("停机", DeviceStatusReportRules.GetStatusName("0"), "状态名称需要按 MES 语义显示。");
    AssertEqual("开机", DeviceStatusReportRules.GetStatusName("1"), "状态名称需要按 MES 语义显示。");
    AssertEqual("程序执行结束", DeviceStatusReportRules.GetStatusName("7"), "状态 7 应显示程序执行结束。");
}

static void MesDeviceStatusRulesConvertPlcAlarmTransitions()
{
    var enter = DeviceStatusReportRules.ResolvePlcAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Running,
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm);
    var recovered = DeviceStatusReportRules.ResolvePlcAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Alarm,
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Running);
    var stillRunning = DeviceStatusReportRules.ResolvePlcAlarmTransition(
        previousStatusCode: ProductionConstants.PlcDeviceStatuses.Running,
        currentStatusCode: ProductionConstants.PlcDeviceStatuses.Paused);

    AssertEqual(ProductionConstants.MesDeviceStatuses.Exception, enter, "PLC 进入报警时应转换为 MES 设备状态 4。");
    AssertEqual(ProductionConstants.MesDeviceStatuses.Recovered, recovered, "PLC 从报警恢复到非报警时应转换为 MES 设备状态 5。");
    AssertEqual(null, stillRunning, "非报警之间变化不应产生 MES 设备状态上报。");
}

static void MesDeviceStatusRulesUseLatestDeviceIdForReport()
{
    AssertEqual(
        "NEW-DEVICE",
        DeviceStatusReportRules.ResolveReportDeviceId(" NEW-DEVICE ", "OLD-DEVICE"),
        "设备状态上报必须优先使用系统设置中的最新设备编号。");
    AssertEqual(
        "OLD-DEVICE",
        DeviceStatusReportRules.ResolveReportDeviceId(" ", " OLD-DEVICE "),
        "系统设置设备编号为空时才回退到历史日志或上传任务中的设备编号。");
}

static void MesDeviceStatusRulesFormatStatusIdentity()
{
    AssertEqual("0-停机", DeviceStatusReportRules.FormatStatusIdentity("0"), "停机状态标识应包含状态码和描述。");
    AssertEqual("1-开机", DeviceStatusReportRules.FormatStatusIdentity("1"), "开机状态标识应包含状态码和描述。");
    AssertEqual("6-程序执行开始", DeviceStatusReportRules.FormatStatusIdentity("6"), "程序执行开始状态标识应包含状态码和描述。");
}

static void MesDeviceStatusRulesFormatStationRemarks()
{
    AssertEqual("工位1", DeviceStatusReportRules.FormatStationScope(1), "工位 1 应显示为工位1。");
    AssertEqual("工位2", DeviceStatusReportRules.FormatStationScope(2), "工位 2 应显示为工位2。");
    AssertEqual("双工位", DeviceStatusReportRules.FormatStationScope(0), "共享报警点应显示为双工位。");
    AssertEqual(
        "程序执行开始；工位：工位2",
        DeviceStatusReportRules.AppendStationRemark("程序执行开始", 2),
        "程序开始/结束备注应追加工位说明。");
}

static void MesDeviceStatusDuplicateSuppressionHonorsLifecycleForceWrite()
{
    var stopped = new BizDeviceStatusLog
    {
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        WeldTaskId = null
    };

    AssertTrue(
        DeviceStatusReportRules.ShouldSuppressDuplicateStatus(
            stopped,
            ProductionConstants.MesDeviceStatuses.Stopped,
            weldTaskId: null,
            forceWrite: false),
        "普通状态重复时应继续复用最新记录，避免重复上传。");

    AssertFalse(
        DeviceStatusReportRules.ShouldSuppressDuplicateStatus(
            stopped,
            ProductionConstants.MesDeviceStatuses.Stopped,
            weldTaskId: null,
            forceWrite: true),
        "软件启动/关闭属于生命周期事件，即使状态码相同也必须新写一条记录。");
}

static void LogTimestampDisplayRulesSwitchDateVisibility()
{
    var value = new DateTime(2026, 7, 4, 9, 8, 7, 123);

    AssertEqual("09:08:07.123", LogTimestampDisplayRules.Format(value, showDate: false), "默认日志表格只显示当天时间。");
    AssertEqual("2026-07-04 09:08:07.123", LogTimestampDisplayRules.Format(value, showDate: true), "勾选显示日期后必须显示完整年月日。");
}

static void AntdTableSelectionHelperMapsSelectedIndexes()
{
    var helperCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Infrastructure", "AntdTableSelectionHelper.cs"),
        Encoding.UTF8);
    var addressViewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);

    AssertTrue(
        helperCode.Contains("GetSelectedRowsFromIndexes", StringComparison.Ordinal),
        "AntdUI Ctrl+A 可能只写入 SelectedIndexs，选择 helper 必须提供索引兜底读取路径。");
    AssertTrue(
        helperCode.Contains("table.SelectedIndexs", StringComparison.Ordinal)
            && helperCode.Contains("table.DataSource", StringComparison.Ordinal),
        "索引兜底应从 table.SelectedIndexs 映射到当前 table.DataSource 行对象。");
    AssertTrue(
        addressViewCode.Contains("AntdTableSelectionHelper.EnableMultiRowSelection(tableAlarmAddresses);", StringComparison.Ordinal),
        "报警地址表必须启用 AntdUI 多行选择。");
    AssertTrue(
        addressViewCode.Contains("MultiSelectTable_KeyDown", StringComparison.Ordinal)
            && addressViewCode.Contains("table.SetSelected(row, true)", StringComparison.Ordinal),
        "AntdUI 多选表应统一处理 Ctrl+A，确保可见行被真实选中。");
    AssertTrue(
        addressViewCode.Contains("GetSelectedAlarmRows()", StringComparison.Ordinal)
            && addressViewCode.Contains("DeleteAlarmAddress_Click", StringComparison.Ordinal),
        "报警地址删除按钮必须通过统一选择读取方法支持多行删除。");
}

static void DeviceStatusLocalLogStoreResolvesDirectories()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusLogs");
    var configured = new AppSettings { LogDirectory = root };
    var fallback = new AppSettings { LogDirectory = " " };

    AssertEqual(
        Path.Combine(root, AppConstants.LogCategories.DeviceStatus),
        DeviceStatusLocalLogStore.GetLogDirectory(configured),
        "配置了日志根目录时，设备状态日志应写入 DeviceStatus 子目录。");
    AssertEqual(
        Path.Combine(AppContext.BaseDirectory, "Logs", AppConstants.LogCategories.DeviceStatus),
        DeviceStatusLocalLogStore.GetLogDirectory(fallback),
        "日志根目录为空时，应回退到程序目录 Logs/DeviceStatus。");
}

static void DeviceStatusLocalLogStoreWritesAndReadsJsonl()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusLogTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var reportTime = new DateTime(2026, 7, 4, 9, 1, 2);
    var older = new BizDeviceStatusLog
    {
        Id = 1,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
        Source = "Software",
        OccurredTime = new DateTime(2026, 7, 4, 8, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Failed,
        ReportMessage = "MES 离线"
    };
    var latest = new BizDeviceStatusLog
    {
        Id = 2,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        Source = "Software",
        OccurredTime = new DateTime(2026, 7, 4, 9, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Uploaded,
        ReportTime = reportTime,
        ReportMessage = "成功"
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(older, settings), "第一条设备状态日志应写入 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(latest, settings), "第二条设备状态日志应写入 JSONL。");

        var filePath = Path.Combine(root, AppConstants.LogCategories.DeviceStatus, "2026-07-04.jsonl");
        AssertTrue(File.Exists(filePath), "设备状态日志文件必须按日期落盘。");

        var json = File.ReadAllText(filePath, Encoding.UTF8);
        AssertTrue(json.Contains("\"ReportStatus\": \"Uploaded\"", StringComparison.Ordinal), "本地 JSONL 必须记录最终上报状态。");
        AssertTrue(json.Contains("\"ReportMessage\": \"成功\"", StringComparison.Ordinal), "本地 JSONL 必须记录最终上报消息。");

        var logs = DeviceStatusLocalLogStore.Read(
            settings,
            new DateTime(2026, 7, 4),
            new DateTime(2026, 7, 4, 23, 59, 59),
            maxCount: 1);

        AssertEqual(1, logs.Count, "读取本地 JSONL 时应遵守 maxCount。");
        AssertEqual(2, logs[0].Id, "设备状态日志应按发生时间倒序读取。");
        AssertEqual(reportTime, logs[0].ReportTime, "读取出的设备状态日志应保留上报时间。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var reportMethod = ExtractMethodText(
        serviceCode,
        "private async Task<BizDeviceStatusLog> ReportStatusAsync",
        "private async Task ReportStatusInBackgroundAsync");
    var skippedMethod = ExtractMethodText(
        serviceCode,
        "private BizDeviceStatusLog MarkSkipped",
        "private BizUploadTask? FindExistingUploadTask");

    AssertFalse(
        reportMethod.Contains("InSingle(log.Id)", StringComparison.Ordinal),
        "MES 上传更新状态后不能重新从数据库读取 BizDeviceStatusLog，否则 MySQL 可能截断毫秒。");
    AssertTrue(
        reportMethod.Contains("return log;", StringComparison.Ordinal),
        "MES 上传更新状态后应返回保留原始 OccurredTime 的内存对象。");
    AssertTrue(
        reportMethod.Contains("Ts = log.OccurredTime.ToString(\"yyyy-MM-dd HH:mm:ss\")", StringComparison.Ordinal),
        "MES 设备状态接口时间格式仍应按接口约定保持到秒。");
    AssertFalse(
        skippedMethod.Contains("InSingle(log.Id)", StringComparison.Ordinal),
        "禁用 MES 上报时也不能用数据库回读对象覆盖本地毫秒时间。");
}

static void DeviceStatusLocalLogStoreKeepsLatestStatePerLogId()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusLogDedupeTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 7, 7, 17, 11, 42, 724);

    try
    {
        var pending = new BizDeviceStatusLog
        {
            Id = 100,
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
            Source = "Application",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending,
            ReportMessage = "Shutdown upload triggered."
        };
        var uploaded = new BizDeviceStatusLog
        {
            Id = 100,
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
            Source = "Application",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Uploaded,
            ReportMessage = "操作成功",
            ReportTime = occurredTime.AddSeconds(1)
        };

        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings), "待上传状态应写入本地日志。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(uploaded, settings), "重试成功状态应追加写入本地日志。");

        var logs = DeviceStatusLocalLogStore.Read(settings, occurredTime.Date, occurredTime.Date.AddDays(1).AddTicks(-1), 10);

        AssertEqual(1, logs.Count, "同一个设备状态日志 Id 只应显示最新状态。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, logs[0].ReportStatus, "本地日志读取应保留最新上传状态。");
        AssertEqual(occurredTime, logs[0].OccurredTime, "本地日志去重不能丢失原始毫秒。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void LogManageViewDeviceStatusTabExposesOpenFolderButton()
{
    var designer = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);

    AssertTrue(
        designer.Contains("btnOpenDeviceStatusFolder = new AntdUI.Button();", StringComparison.Ordinal),
        "设备状态日志页签应在 Designer.cs 中静态声明打开目录按钮。");
    AssertTrue(
        designer.Contains("deviceStatusToolbar.Controls.Add(btnOpenDeviceStatusFolder, 4, 0);", StringComparison.Ordinal),
        "设备状态日志打开目录按钮应放入页签右上角工具栏。");
    AssertTrue(
        designer.Contains("btnOpenDeviceStatusFolder.Tag = \"perm:button.log.open-folder:enabled\";", StringComparison.Ordinal),
        "设备状态日志打开目录按钮应复用日志打开目录权限。");
    AssertTrue(
        viewCode.Contains("btnOpenDeviceStatusFolder.Click += (_, _) => OpenDeviceStatusLogFolder();", StringComparison.Ordinal),
        "设备状态日志打开目录按钮必须绑定点击事件。");
    AssertTrue(
        viewCode.Contains("_deviceStatusService.GetLogDirectory()", StringComparison.Ordinal),
        "设备状态日志打开目录逻辑必须走 IDeviceStatusService.GetLogDirectory。");
}

static void DataManageViewStaticGridsDefineBoundColumns()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("dgvWorkOrders.Columns.AddRange", StringComparison.Ordinal),
        "历史工单表关闭自动生成列后，必须显式添加静态列。");
    AssertTrue(
        viewCode.Contains("dgvCollectionRecords.Columns.AddRange", StringComparison.Ordinal),
        "采集数据表关闭自动生成列后，必须显式添加静态列。");
    AssertTrue(
        viewCode.Contains("dgvReportFiles.Columns.AddRange", StringComparison.Ordinal),
        "报告文件表关闭自动生成列后，必须显式添加静态列。");

    var requiredBindings = new[]
    {
        "nameof(DataHistoryWorkOrderRow.WorkOrderId)",
        "nameof(DataHistoryCollectionRow.SequenceNo)",
        "nameof(DataHistoryReportFileRow.FileName)"
    };

    foreach (var binding in requiredBindings)
    {
        AssertTrue(
            viewCode.Contains(binding, StringComparison.Ordinal),
            $"DataManageView 静态列必须使用 {binding} 绑定 DTO 属性。");
    }
}

static void DataManageViewIgnoresReportSelectionWhileDisposing()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.Designer.cs"), Encoding.UTF8);
    var reportSelectionHandler = ExtractMethodText(
        viewCode,
        "private void ReportFiles_SelectionChanged",
        "private void OpenSelectedReport()");
    var selectedReportMethod = ExtractMethodText(
        viewCode,
        "private DataHistoryReportFileRow? GetSelectedReport()",
        "private void ClearTaskDetails()");

    AssertTrue(viewCode.Contains("private bool _disposing;", StringComparison.Ordinal), "DataManageView 必须记录释放中状态，避免 Dispose 期间继续处理选择事件。");
    AssertTrue(designerCode.Contains("BeginDispose();", StringComparison.Ordinal), "Dispose 必须在 components.Dispose 前标记释放状态。");
    AssertTrue(reportSelectionHandler.Contains("_disposing", StringComparison.Ordinal), "报告文件选择事件在释放中必须直接返回。");
    AssertTrue(selectedReportMethod.Contains("reportBindingSource.Count <= 0", StringComparison.Ordinal), "读取报告文件选择前必须先检查 BindingSource 是否为空。");
    AssertTrue(selectedReportMethod.Contains("reportBindingSource.Position", StringComparison.Ordinal), "读取报告文件选择前必须校验 BindingSource 当前索引。");
    AssertTrue(selectedReportMethod.Contains("reportBindingSource.Current as DataHistoryReportFileRow", StringComparison.Ordinal), "报告文件选择应从 BindingSource.Current 获取，避免 DataGridView.CurrentRow 在释放时触发 CurrencyManager[0]。");
    AssertFalse(selectedReportMethod.Contains("dgvReportFiles.CurrentRow", StringComparison.Ordinal), "GetSelectedReport 不能再访问 DataGridView.CurrentRow。");
}

static void DataManageViewIgnoresWorkOrderSelectionWhileDisposing()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);
    var workOrderSelectionHandler = ExtractMethodText(
        viewCode,
        "private async void WorkOrders_SelectionChanged",
        "private async Task QueryWorkOrdersAsync");
    var selectedWorkOrderMethod = ExtractMethodText(
        viewCode,
        "private DataHistoryWorkOrderRow? GetSelectedWorkOrder()",
        "private async Task QueryWorkOrdersAsync");
    var collectionSelectionHandler = ExtractMethodText(
        viewCode,
        "private void CollectionRecords_SelectionChanged",
        "private void ReportFiles_SelectionChanged");
    var selectedCollectionMethod = ExtractMethodText(
        viewCode,
        "private DataHistoryCollectionRow? GetSelectedCollectionRecord()",
        "private void RemoveDynamicParameterColumns()");
    var beginDisposeMethod = ExtractMethodText(
        viewCode,
        "private void BeginDispose()",
        "private void ClearTaskDetails()");

    AssertTrue(workOrderSelectionHandler.Contains("_disposing", StringComparison.Ordinal), "工单选择事件在释放中必须直接返回，避免 Dispose 清绑定时读取失效行。");
    AssertTrue(workOrderSelectionHandler.Contains("GetSelectedWorkOrder()", StringComparison.Ordinal), "工单选择事件必须通过 BindingSource 安全读取当前项。");
    AssertTrue(selectedWorkOrderMethod.Contains("workOrderBindingSource.Count <= 0", StringComparison.Ordinal), "读取工单选择前必须先检查 BindingSource 是否为空。");
    AssertTrue(selectedWorkOrderMethod.Contains("workOrderBindingSource.Position", StringComparison.Ordinal), "读取工单选择前必须校验 BindingSource 当前索引。");
    AssertTrue(selectedWorkOrderMethod.Contains("workOrderBindingSource.Current as DataHistoryWorkOrderRow", StringComparison.Ordinal), "工单选择应从 BindingSource.Current 获取。");
    AssertFalse(selectedWorkOrderMethod.Contains("dgvWorkOrders.CurrentRow", StringComparison.Ordinal), "GetSelectedWorkOrder 不能访问 DataGridView.CurrentRow。");
    AssertTrue(collectionSelectionHandler.Contains("_disposing", StringComparison.Ordinal), "采集记录选择事件在释放中必须直接返回。");
    AssertTrue(selectedCollectionMethod.Contains("collectionBindingSource.Current as DataHistoryCollectionRow", StringComparison.Ordinal), "采集记录明细也应从 BindingSource.Current 获取，避免释放时访问 DataGridView 行。");
    AssertFalse(selectedCollectionMethod.Contains("dgvCollectionRecords.CurrentRow", StringComparison.Ordinal), "GetSelectedCollectionRecord 不能访问 DataGridView.CurrentRow。");
    AssertTrue(beginDisposeMethod.Contains("dgvWorkOrders.SelectionChanged -= WorkOrders_SelectionChanged;", StringComparison.Ordinal), "释放时必须解绑工单选择事件。");
    AssertTrue(beginDisposeMethod.Contains("dgvCollectionRecords.SelectionChanged -= CollectionRecords_SelectionChanged;", StringComparison.Ordinal), "释放时必须解绑采集记录选择事件。");
}

static void DataManageViewTreatsCancelledHistoryQueriesAsStaleWork()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "DataHistoryQueryService.cs"), Encoding.UTF8);
    var runQueryMethod = ExtractMethodText(
        serviceCode,
        "private Task<T> RunQueryAsync<T>",
        "private PagedResult<DataHistoryWorkOrderRow> QueryWorkOrders");
    var queryWorkOrdersMethod = ExtractMethodText(
        viewCode,
        "private async Task QueryWorkOrdersAsync",
        "private async Task ResetQueryAsync");
    var loadDetailsMethod = ExtractMethodText(
        viewCode,
        "private async Task LoadTaskDetailsAsync",
        "private async Task LoadCollectionRecordsAsync");
    var loadCollectionMethod = ExtractMethodText(
        viewCode,
        "private async Task LoadCollectionRecordsAsync",
        "private void BindWeldParameters");

    AssertFalse(runQueryMethod.Contains("ThrowIfCancellationRequested", StringComparison.Ordinal), "历史查询服务不应在线程池委托中主动抛 OperationCanceledException，避免调试器停在 RunQueryAsync。");
    AssertFalse(runQueryMethod.Contains("}, cancellationToken);", StringComparison.Ordinal), "Task.Run 不应绑定 UI 查询取消令牌，否则取消可能在服务层表现为异常。");
    AssertTrue(runQueryMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "历史查询服务仍应识别已取消查询并跳过过期工作。");
    AssertFalse(queryWorkOrdersMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "工单查询取消后应直接返回，不应再抛取消异常。");
    AssertFalse(loadDetailsMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "明细查询取消后应直接返回，不应再抛取消异常。");
    AssertFalse(loadCollectionMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "采集分页查询取消后应直接返回，不应再抛取消异常。");
    AssertTrue(queryWorkOrdersMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "工单查询完成后必须检查取消状态，避免旧结果覆盖新界面。");
    AssertTrue(loadDetailsMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "明细查询完成后必须检查取消状态，避免旧结果覆盖新界面。");
    AssertTrue(loadCollectionMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "采集分页查询完成后必须检查取消状态，避免旧结果覆盖新界面。");
}

static void DeviceApiStatusQueryReturnsCurrentMesStatus()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "87261699027", DeviceName = "设备1" }
    };
    var statusService = new FakeDeviceStatusService
    {
        CurrentStatus = new BizDeviceStatusLog
        {
            DeviceId = "87261699027",
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped
        }
    };
    var service = CreateDeviceApiEndpointService(settings, statusService);

    var response = service.GetDeviceStatus("87261699027");

    AssertTrue(response.IsSuccess, "设备编号匹配时应返回成功。");
    AssertEqual("成功", response.Msg, "成功响应消息需与平台示例保持一致。");
    AssertTrue(response.Data is not null, "成功查询必须返回 Data 节点。");
    AssertEqual("87261699027", response.Data!.DeviceId, "返回设备编号必须来自当前本地设置。");
    AssertEqual(ProductionConstants.MesDeviceStatuses.Stopped, response.Data.DeviceStatus, "设备状态必须使用当前 MES 设备状态码。");
}

static void DeviceApiStatusQueryRejectsMismatchedDeviceId()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "87261699027" }
    };
    var statusService = new FakeDeviceStatusService
    {
        CurrentStatus = new BizDeviceStatusLog
        {
            DeviceId = "87261699027",
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped
        }
    };
    var service = CreateDeviceApiEndpointService(settings, statusService);

    var response = service.GetDeviceStatus("OTHER");

    AssertFalse(response.IsSuccess, "设备编号不匹配时必须拒绝查询。");
    AssertEqual(null, response.Data, "拒绝查询时不能泄露当前设备状态。");
    AssertEqual(0, statusService.GetCurrentStatusCallCount, "设备编号不匹配时不应读取当前状态。");
}

static void DeviceApiSetDeviceIdSavesLocalSettingsAsSynced()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "9999999990005",
            DeviceName = "旧设备",
            DeviceBaseUrl = "http://10.0.0.5:3000/",
            MesBaseUrl = "http://old-mes/",
            MesSyncedDeviceId = "9999999990005"
        }
    };
    var operations = new FakeOperationLogService();
    var service = CreateDeviceApiEndpointService(settings, operationLogService: operations);

    var response = service.SetDeviceIdAsync(new AddDeviceReq
    {
        OldDeviceId = "9999999990005",
        DeviceId = "99999999900053",
        DeviceName = "123",
        DevStatusUrl = "http://192.168.80.208:3000/api/DeviceStatus?DeviceId=123333",
        PostDataDomain = "http://192.168.101.65:8098/"
    }).GetAwaiter().GetResult();

    AssertTrue(response.IsSuccess, "远程设置设备编号成功时应返回成功。");
    AssertEqual("99999999900053", settings.Current.DeviceId, "平台下发的新设备编号必须保存到本地设置。");
    AssertEqual("123", settings.Current.DeviceName, "平台下发的设备名称必须保存到本地设置。");
    AssertEqual("99999999900053", settings.Current.MesSyncedDeviceId, "远程下发成功后应视为已同步到平台。");
    AssertEqual("http://192.168.80.208:3000/", settings.Current.DeviceBaseUrl, "DevStatusUrl 应反解保存为设备端 API 基地址。");
    AssertEqual("http://192.168.101.65:8098/", settings.Current.MesBaseUrl, "PostDataDomain 应保存为 MES 基地址。");
    AssertTrue(response.Data is not null, "成功设置设备编号必须返回 Data 节点。");
    AssertEqual(
        "http://192.168.80.208:3000/api/DeviceStatus?DeviceId=99999999900053",
        response.Data!.DevStatusUrl,
        "响应中的 DevStatusUrl 应按新设备编号重新生成。");
    AssertTrue(operations.Entries.Any(entry => entry.Action == "DeviceApi.SetDeviceId"), "远程修改设备编号需要写入操作日志。");
}

static void DeviceApiSetDeviceIdRejectsMismatchedOldDeviceId()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "LOCAL-D1",
            DeviceName = "旧设备",
            MesSyncedDeviceId = "SYNC-D1"
        }
    };
    var service = CreateDeviceApiEndpointService(settings);

    var response = service.SetDeviceIdAsync(new AddDeviceReq
    {
        OldDeviceId = "OTHER-D1",
        DeviceId = "NEW-D1",
        DeviceName = "新设备"
    }).GetAwaiter().GetResult();

    AssertFalse(response.IsSuccess, "OldDeviceId 既不匹配当前设备编号也不匹配已同步编号时必须拒绝。");
    AssertEqual("LOCAL-D1", settings.Current.DeviceId, "拒绝请求不能修改本地设备编号。");
    AssertEqual("SYNC-D1", settings.Current.MesSyncedDeviceId, "拒绝请求不能修改已同步设备编号。");
}

static void DeviceApiRulesBuildAndParseStatusUrl()
{
    var statusUrl = DeviceApiEndpointRules.BuildDeviceStatusUrl("http://192.168.80.208:3000", "ABC 123");

    AssertEqual(
        "http://192.168.80.208:3000/api/DeviceStatus?DeviceId=ABC%20123",
        statusUrl,
        "上报给 MES 的设备状态地址必须包含固定 API 路径和 URL 编码后的设备编号。");
    AssertTrue(
        DeviceApiEndpointRules.TryExtractBaseUrlFromStatusUrl(
            "http://192.168.80.208:3000/api/DeviceStatus?DeviceId=123333",
            out var baseUrl),
        "平台下发的设备状态地址应能反解出设备端 API 基地址。");
    AssertEqual("http://192.168.80.208:3000/", baseUrl, "反解出的设备端 API 基地址必须保留协议、主机和端口。");
}

static void DeviceApiHttpSelfCheckRulesWriteSuccessAndFailure()
{
    var successEntry = DeviceLifecycleLogRules.CreateDeviceApiHttpSelfCheckEntry(
        "D-001",
        "http://127.0.0.1:7098/",
        true,
        "HTTP 服务监听成功",
        new DateTime(2026, 7, 1, 8, 0, 0));
    var failure = DeviceLifecycleLogRules.CreateDeviceApiHttpSelfCheckEntry(
        "D-001",
        "http://127.0.0.1:7098/",
        false,
        "端口被占用",
        new DateTime(2026, 7, 1, 8, 0, 1));

    AssertEqual(AppConstants.DeviceLifecycleEventTypes.SelfCheck, successEntry.EventType, "HTTP 服务启动结果属于软件启动自检。");
    AssertEqual("DeviceApi", successEntry.Source, "HTTP 服务自检来源应标记为 DeviceApi。");
    AssertEqual("Success", successEntry.Status, "HTTP 服务监听成功应写 Success。");
    AssertEqual("HTTP服务启动成功", successEntry.Summary, "HTTP 服务监听成功摘要应明确。");
    AssertTrue(successEntry.Detail.Contains("DeviceBaseUrl=http://127.0.0.1:7098/", StringComparison.Ordinal), "详情必须记录监听基地址。");
    AssertEqual("Failed", failure.Status, "HTTP 服务监听失败应写 Failed。");
    AssertTrue(failure.Detail.Contains("端口被占用", StringComparison.Ordinal), "失败详情必须记录原因。");
}

static void DeviceApiStatusQueryWritesLifecycleLogEveryTime()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001" }
    };
    var statusService = new FakeDeviceStatusService
    {
        CurrentStatus = new BizDeviceStatusLog
        {
            DeviceId = "D-001",
            DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn
        }
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var service = CreateDeviceApiEndpointService(settings, statusService, lifecycleLogService: lifecycleLogs);

    service.GetDeviceStatus("D-001");
    service.GetDeviceStatus("D-001");

    AssertEqual(2, lifecycleLogs.Entries.Count, "设备状态查询按审计要求每次调用都要写设备日志。");
    AssertTrue(lifecycleLogs.Entries.All(entry => entry.EventType == AppConstants.DeviceLifecycleEventTypes.RemoteAccess), "状态查询应写远程访问事件。");
    AssertTrue(lifecycleLogs.Entries.All(entry => entry.Source == "DeviceApi"), "状态查询日志来源应为 DeviceApi。");
    AssertTrue(lifecycleLogs.Entries.All(entry => entry.Status == "Success"), "成功状态查询应写 Success。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("RequestType=GET /api/DeviceStatus", StringComparison.Ordinal), "详情必须记录接口类型。");
}

static void DeviceApiStatusQueryMismatchWritesFailureLifecycleLog()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001" }
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var service = CreateDeviceApiEndpointService(settings, lifecycleLogService: lifecycleLogs);

    var response = service.GetDeviceStatus("D-002");

    AssertFalse(response.IsSuccess, "设备编号不匹配仍应返回失败。");
    AssertEqual(1, lifecycleLogs.Entries.Count, "设备编号不匹配也要写设备日志。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.RemoteAccess, lifecycleLogs.Entries[0].EventType, "状态查询失败仍属于远程访问事件。");
    AssertEqual("Failed", lifecycleLogs.Entries[0].Status, "设备编号不匹配应写 Failed。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("RequestedDeviceId=D-002", StringComparison.Ordinal), "失败详情必须记录请求设备编号。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("CurrentDeviceId=D-001", StringComparison.Ordinal), "失败详情必须记录当前设备编号。");
}

static void DeviceApiSetDeviceIdWritesLifecycleLogs()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            DeviceName = "旧设备",
            DeviceBaseUrl = "http://127.0.0.1:7098/",
            MesSyncedDeviceId = "D-001"
        }
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var service = CreateDeviceApiEndpointService(settings, lifecycleLogService: lifecycleLogs);

    var success = service.SetDeviceIdAsync(new AddDeviceReq
    {
        OldDeviceId = "D-001",
        DeviceId = "D-002",
        DeviceName = "新设备",
        DevStatusUrl = "http://127.0.0.1:7098/api/DeviceStatus?DeviceId=D-002",
        PostDataDomain = "http://mes.local/"
    }).GetAwaiter().GetResult();

    AssertTrue(success.IsSuccess, "设备编号修改成功时接口应返回成功。");
    AssertEqual(1, lifecycleLogs.Entries.Count, "设备编号修改成功应写设备日志。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.RemoteConfigChanged, lifecycleLogs.Entries[0].EventType, "设备编号修改应写远程配置变更事件。");
    AssertEqual("Success", lifecycleLogs.Entries[0].Status, "设备编号修改成功应写 Success。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("OldDeviceId=D-001", StringComparison.Ordinal), "详情必须记录旧设备编号。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("NewDeviceId=D-002", StringComparison.Ordinal), "详情必须记录新设备编号。");

    lifecycleLogs.Entries.Clear();
    var failure = service.SetDeviceIdAsync(new AddDeviceReq
    {
        OldDeviceId = "OTHER",
        DeviceId = "D-003",
        DeviceName = "错误设备"
    }).GetAwaiter().GetResult();

    AssertFalse(failure.IsSuccess, "旧设备编号不匹配时接口应返回失败。");
    AssertEqual(1, lifecycleLogs.Entries.Count, "设备编号修改失败也应写设备日志。");
    AssertEqual("Failed", lifecycleLogs.Entries[0].Status, "设备编号修改失败应写 Failed。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("OldDeviceId=OTHER", StringComparison.Ordinal), "失败详情必须记录平台传入的旧设备编号。");
}

static void DeviceApiIgnoresLifecycleLogFailure()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001", MesSyncedDeviceId = "D-001" }
    };
    var statusService = new FakeDeviceStatusService
    {
        CurrentStatus = new BizDeviceStatusLog
        {
            DeviceId = "D-001",
            DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn
        }
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService { ThrowOnWrite = true };
    var service = CreateDeviceApiEndpointService(settings, statusService, lifecycleLogService: lifecycleLogs);

    var query = service.GetDeviceStatus("D-001");
    var set = service.SetDeviceIdAsync(new AddDeviceReq
    {
        OldDeviceId = "D-001",
        DeviceId = "D-002",
        DeviceName = "新设备"
    }).GetAwaiter().GetResult();

    AssertTrue(query.IsSuccess, "设备日志写入失败不能影响状态查询返回。");
    AssertTrue(set.IsSuccess, "设备日志写入失败不能影响设备编号设置返回。");
}

static void UploadStatusDisplayRulesLocalizeStatusText()
{
    AssertEqual("待上传", UploadStatusDisplayRules.GetDisplayText(ProductionConstants.UploadStatuses.Pending), "Pending 应显示为待上传。");
    AssertEqual("上传失败", UploadStatusDisplayRules.GetDisplayText(ProductionConstants.UploadStatuses.Failed, mesConnected: true), "MES 在线时 Failed 应显示为上传失败。");
    AssertEqual("待上传", UploadStatusDisplayRules.GetDisplayText(ProductionConstants.UploadStatuses.Failed, mesConnected: false), "MES 离线时 Failed 应显示为待上传。");
    AssertEqual("已跳过", UploadStatusDisplayRules.GetDisplayText(ProductionConstants.UploadStatuses.Skipped), "Skipped 应显示为已跳过。");
    AssertEqual("待上传", UploadStatusDisplayRules.GetDisplayText(UploadSummaryStatusResolver.NoData, mesConnected: true), "MES 在线时 NoData 应显示为待上传。");
    AssertEqual("无数据", UploadStatusDisplayRules.GetDisplayText(UploadSummaryStatusResolver.NoData, mesConnected: false), "MES 离线时 NoData 应保留无数据。");
}

static void UploadStatusNavigationUsesPendingUploadDataText()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Forms", "MainForm.Designer.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("segmentedItem6.Text = \"待上传数据 \";", StringComparison.Ordinal), "MainForm 底部上传状态入口初始文本必须改为待上传数据并保留尾随空格。");
    AssertFalse(designerCode.Contains("segmentedItem6.Text = \"上传状态\";", StringComparison.Ordinal), "MainForm 底部上传状态入口不应再显示上传状态。");
    AssertTrue(zhResources.Contains("name=\"main.nav.state_manage\"", StringComparison.Ordinal), "中文资源必须包含主导航上传状态入口键。");
    AssertTrue(zhResources.Contains("<value>待上传数据 </value>", StringComparison.Ordinal), "中文主导航上传状态入口必须显示待上传数据并保留尾随空格。");
    AssertTrue(enResources.Contains("name=\"main.nav.state_manage\"", StringComparison.Ordinal), "英文资源必须包含主导航上传状态入口键。");
    AssertTrue(enResources.Contains("<value>Pending Upload Data</value>", StringComparison.Ordinal), "英文主导航上传状态入口必须同步为 Pending Upload Data。");
}

static void StateManageSummaryTabUsesWorkOrderInfoText()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.Designer.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("tabSummary.Text = \"工单信息\";", StringComparison.Ordinal), "上传状态页总览页签 Designer 初始文本必须改为工单信息。");
    AssertTrue(viewCode.Contains("tabSummary.Text = \"工单信息\";", StringComparison.Ordinal), "语言刷新后总览页签必须保持工单信息。");
    AssertTrue(viewCode.Contains("return \"工单信息\";", StringComparison.Ordinal), "工单信息页签的统计前缀必须同步更新。");
    AssertTrue(viewCode.Contains("ShowWarning(\"工单信息请使用一键上传。\")", StringComparison.Ordinal), "工单信息页签禁用单项重试提示必须同步更新。");
    AssertTrue(viewCode.Contains("ShowInfo(\"已从工单信息隐藏选中的任务。\")", StringComparison.Ordinal), "隐藏任务提示必须同步更新为工单信息。");
    AssertFalse(viewCode.Contains("tabSummary.Text = \"上传总览\";", StringComparison.Ordinal), "运行时页签文本不应再恢复上传总览。");
    AssertFalse(viewCode.Contains("return \"上传总览\";", StringComparison.Ordinal), "统计前缀不应再使用上传总览。");
    AssertFalse(viewCode.Contains("ShowWarning(\"上传总览请使用一键上传。\")", StringComparison.Ordinal), "提示文本不应再使用上传总览。");
    AssertFalse(viewCode.Contains("ShowInfo(\"已从上传总览隐藏选中的任务。\")", StringComparison.Ordinal), "隐藏提示不应再使用上传总览。");
}

static void StateManageUploadStatusDisplayFollowsMesConnection()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("using AutoWeldSystem.Core.Interfaces.MES;", StringComparison.Ordinal), "上传状态页必须引用 MES 连接监控接口命名空间。");
    AssertTrue(viewCode.Contains("private readonly IMesConnectionMonitor _mesConnectionMonitor;", StringComparison.Ordinal), "上传状态页必须持有 MES 连接监控。");
    AssertTrue(viewCode.Contains("IMesConnectionMonitor mesConnectionMonitor", StringComparison.Ordinal), "上传状态页构造函数必须注入 MES 连接监控。");
    AssertTrue(viewCode.Contains("_mesConnectionMonitor.StatusChanged += MesConnectionMonitor_StatusChanged;", StringComparison.Ordinal), "上传状态页必须监听 MES 连接状态变化。");
    AssertTrue(viewCode.Contains("_mesConnectionMonitor.StatusChanged -= MesConnectionMonitor_StatusChanged;", StringComparison.Ordinal), "上传状态页销毁时必须解绑 MES 连接状态变化。");
    AssertTrue(viewCode.Contains("private void MesConnectionMonitor_StatusChanged", StringComparison.Ordinal), "上传状态页必须提供 MES 连接变化处理方法。");
    AssertTrue(viewCode.Contains("dgvPending.Invalidate();", StringComparison.Ordinal), "MES 连接变化后必须刷新表格显示文本。");
    AssertTrue(viewCode.Contains("UploadStatusDisplayRules.GetDisplayText(status, _mesConnectionMonitor.Current.IsConnected)", StringComparison.Ordinal), "上传状态显示必须按当前 MES 在线状态解析。");
}

static void StateManageTabsAreCatalogedAsRolePermissions()
{
    var expectedCodes = new[]
    {
        PermissionCodes.Tabs.State.WorkOrderInfo,
        PermissionCodes.Tabs.State.StartReport,
        PermissionCodes.Tabs.State.FinishReport,
        PermissionCodes.Tabs.State.ProcessParameter,
        PermissionCodes.Tabs.State.ReportFile,
        PermissionCodes.Tabs.State.WorkOrderStatus,
        PermissionCodes.Tabs.State.DeviceStatus,
        PermissionCodes.Tabs.State.ProgramFile
    };
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertSequenceEqual(expectedCodes, PermissionCodes.Tabs.State.All, "待上传数据的 8 个页签权限必须保持固定业务顺序。");
    AssertEqual(expectedCodes.Length, expectedCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count(), "页签权限编码必须唯一。");

    var definitions = PermissionCatalog.All
        .Where(permission => expectedCodes.Contains(permission.Code, StringComparer.OrdinalIgnoreCase))
        .OrderBy(permission => permission.Sort)
        .ToArray();
    AssertEqual(expectedCodes.Length, definitions.Length, "8 个待上传数据页签都必须注册到权限目录。");
    AssertTrue(definitions.All(permission => permission.Type == PermissionType.Tab), "待上传数据页签权限必须使用 Tab 类型。");
    AssertTrue(definitions.All(permission => permission.ParentCode == PermissionCodes.Pages.StateManage), "页签权限必须挂在待上传数据页面权限下。");

    foreach (var permissionCode in expectedCodes)
    {
        var textKey = PermissionTextKeyMapper.GetTextKey(permissionCode);
        AssertFalse(string.IsNullOrWhiteSpace(textKey), $"页签权限 {permissionCode} 必须映射本地化键。");
        AssertTrue(zhResources.Contains($"name=\"{textKey}\"", StringComparison.Ordinal), $"页签权限 {permissionCode} 必须包含中文名称。");
        AssertTrue(enResources.Contains($"name=\"{textKey}\"", StringComparison.Ordinal), $"页签权限 {permissionCode} 必须包含英文名称。");
    }
}

static void GlobalPermissionChecksSeparateDeveloperAndAdmin()
{
    try
    {
        GlobalContext.SetCurrentUser(new SysUser { Role = AppConstants.Roles.Admin }, Array.Empty<string>());
        AssertFalse(GlobalContext.HasPermission(PermissionCodes.Pages.StateManage), "admin 未获页面权限时不能绕过角色授权。");
        AssertFalse(GlobalContext.HasPermission(PermissionCodes.Buttons.State.Refresh), "admin 未获按钮权限时不能绕过角色授权。");
        AssertFalse(GlobalContext.HasPermission(PermissionCodes.Tabs.State.WorkOrderInfo), "admin 未获页签权限时不能绕过角色授权。");

        GlobalContext.SetCurrentUser(
            new SysUser { Role = AppConstants.Roles.Admin },
            new[]
            {
                PermissionCodes.Pages.StateManage,
                PermissionCodes.Buttons.State.Refresh,
                PermissionCodes.Tabs.State.WorkOrderInfo
            });
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Pages.StateManage), "admin 显式获得页面权限后应允许访问。");
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Buttons.State.Refresh), "admin 显式获得按钮权限后应允许操作。");
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Tabs.State.WorkOrderInfo), "admin 显式获得页签权限后应显示页签。");

        GlobalContext.SetCurrentUser(new SysUser { Role = AppConstants.Roles.Developer }, Array.Empty<string>());
        AssertTrue(GlobalContext.IsDeveloper, "Developer 角色必须被识别为开发者。");
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Pages.StateManage), "dev 必须保留页面全权限兜底。");
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Buttons.State.Refresh), "dev 必须保留按钮全权限兜底。");
        AssertTrue(GlobalContext.HasPermission(PermissionCodes.Tabs.State.ProcessParameter), "dev 必须保留页签全权限兜底。");
    }
    finally
    {
        GlobalContext.Clear();
    }
}

static void StateTabDefaultsKeepCustomerTabsConfigurable()
{
    var allPermissionCodes = PermissionCatalog.All.Select(permission => permission.Code).ToArray();
    var developerDefaults = RolePermissionInitializationRules.ResolveElevatedRoleDefaults(
        AppConstants.Roles.Developer,
        allPermissionCodes);
    var adminDefaults = RolePermissionInitializationRules.ResolveElevatedRoleDefaults(
        AppConstants.Roles.Admin,
        allPermissionCodes);

    AssertSequenceEqual(
        new[]
        {
            PermissionCodes.Tabs.State.WorkOrderInfo,
            PermissionCodes.Tabs.State.DeviceStatus,
            PermissionCodes.Tabs.State.ProgramFile
        },
        PermissionCodes.Tabs.State.CustomerDefaults,
        "客户默认只显示工单信息、设备状态和程序文件。");
    AssertTrue(PermissionCodes.Tabs.State.All.All(developerDefaults.Contains), "dev 默认权限必须包含全部待上传数据页签。");
    AssertTrue(PermissionCodes.Tabs.State.CustomerDefaults.All(adminDefaults.Contains), "admin 默认权限必须包含三个客户页签。");
    AssertFalse(adminDefaults.Contains(PermissionCodes.Tabs.State.StartReport), "admin 默认不应显示开工信息调试页签。");
    AssertFalse(adminDefaults.Contains(PermissionCodes.Tabs.State.ProcessParameter), "admin 默认不应显示过程参数调试页签。");
    AssertTrue(RolePermissionInitializationRules.ShouldAppendMissingDefaults(AppConstants.Roles.Developer), "启动时只应持续为 dev 补齐全部权限。");
    AssertFalse(RolePermissionInitializationRules.ShouldAppendMissingDefaults(AppConstants.Roles.Admin), "启动时不能重新补回 admin 已取消的权限。");

    var upgradeDefaults = RolePermissionInitializationRules.ResolveStateTabUpgradeDefaults(
        AppConstants.Roles.Admin,
        stateTabCatalogWasMissing: true,
        hasStateManagePagePermission: true);
    AssertSequenceEqual(PermissionCodes.Tabs.State.CustomerDefaults, upgradeDefaults, "首次升级时应为已有待上传页面权限的客户角色补齐三个默认页签。");
    AssertEqual(
        0,
        RolePermissionInitializationRules.ResolveStateTabUpgradeDefaults(
            AppConstants.Roles.Admin,
            stateTabCatalogWasMissing: false,
            hasStateManagePagePermission: true).Count,
        "页签权限目录已存在时不能在后续启动重新补回权限。");

    var userServiceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "SysUserService.cs"), Encoding.UTF8);
    AssertTrue(userServiceCode.Contains("stateTabCatalogWasMissing", StringComparison.Ordinal), "RBAC 初始化协调必须记录页签权限目录是否为首次创建。");
    AssertTrue(userServiceCode.Contains("ApplyStateTabUpgradeDefaults", StringComparison.Ordinal), "RBAC 初始化协调必须应用一次性客户页签升级授权。");
    AssertTrue(userServiceCode.Contains("RestoreConfigurableAdminPermissions", StringComparison.Ordinal), "RBAC 初始化后必须恢复管理员的真实配置，避免旧补权逻辑覆盖人工设置。");
    AssertTrue(userServiceCode.Contains("RolePermissionInitializationRules.ResolveElevatedRoleDefaults", StringComparison.Ordinal), "新安装管理员必须通过统一规则生成客户默认页签权限。");
}

static void StateManageViewFiltersTabsByCurrentPermissions()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var applyMethod = ExtractMethodText(
        viewCode,
        "private void ApplyTabPermissions()",
        "private void SetNoVisibleTabState()");
    var noVisibleMethod = ExtractMethodText(
        viewCode,
        "private void SetNoVisibleTabState()",
        "private void ConfigureGrid()");

    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.WorkOrderInfo", StringComparison.Ordinal), "工单信息页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.StartReport", StringComparison.Ordinal), "开工信息页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.FinishReport", StringComparison.Ordinal), "完工信息页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.ProcessParameter", StringComparison.Ordinal), "过程参数页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.ReportFile", StringComparison.Ordinal), "报告文件页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.WorkOrderStatus", StringComparison.Ordinal), "工单状态页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.DeviceStatus", StringComparison.Ordinal), "设备状态页签必须映射独立权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Tabs.State.ProgramFile", StringComparison.Ordinal), "程序文件页签必须映射独立权限。");
    AssertTrue(applyMethod.Contains("tabUploadCategories.TabPages.Clear();", StringComparison.Ordinal), "应用页签权限时必须先清空 TabPages，不能依赖 TabPage.Visible。");
    AssertTrue(applyMethod.Contains("GlobalContext.HasPermission(definition.PermissionCode)", StringComparison.Ordinal), "页签重建必须检查当前角色权限。");
    AssertTrue(applyMethod.Contains("tabUploadCategories.TabPages.Add(definition.Page)", StringComparison.Ordinal), "有权限的页签必须按固定定义顺序重新加入。");
    AssertTrue(viewCode.Contains("GlobalContext.SessionChanged += GlobalContext_SessionChanged;", StringComparison.Ordinal), "当前角色权限变化后页面必须立即刷新页签。");
    AssertTrue(viewCode.Contains("GlobalContext.SessionChanged -= GlobalContext_SessionChanged;", StringComparison.Ordinal), "页面销毁时必须解绑角色变化事件。");
    AssertTrue(noVisibleMethod.Contains("_bindingSource.DataSource = Array.Empty<object>();", StringComparison.Ordinal), "没有可见页签时必须清空旧数据。");
    AssertTrue(noVisibleMethod.Contains("dgvPending.Columns.Clear();", StringComparison.Ordinal), "没有可见页签时必须清空旧列。");
    AssertTrue(noVisibleMethod.Contains("TextKeys.StateManage.MessageNoVisibleTabs", StringComparison.Ordinal), "没有可见页签时必须显示明确提示。");
}

static void StateManageDeviceStatusTabSupportsMultiDelete()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.Designer.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("dgvPending.MultiSelect = true;", StringComparison.Ordinal), "待上传数据表格必须允许多行选择。");
    AssertTrue(viewCode.Contains("dgvPending.KeyDown += DgvPending_KeyDown;", StringComparison.Ordinal), "设备状态页签必须绑定 Ctrl+A 全选事件。");
    AssertTrue(viewCode.Contains("dgvPending.SelectedRows", StringComparison.Ordinal), "删除选中必须读取表格的多行选择结果。");
    AssertTrue(viewCode.Contains("IsDeviceStatusTab()", StringComparison.Ordinal), "多行删除逻辑必须限定在设备状态页签。");
    AssertTrue(viewCode.Contains("_uploadTaskService.DeleteTask(task.Id);", StringComparison.Ordinal), "设备状态多行删除必须复用上传任务删除服务。");
}

static void SkippedUploadTasksAreNotRetried()
{
    var skipped = new BizUploadTask
    {
        Status = ProductionConstants.UploadStatuses.Skipped
    };
    var pending = new BizUploadTask
    {
        Status = ProductionConstants.UploadStatuses.Pending
    };

    AssertFalse(UploadTaskVisibilityRules.ShouldRetry(skipped), "已跳过任务不应进入重试执行范围。");
    AssertTrue(UploadTaskVisibilityRules.ShouldRetry(pending), "待上传任务仍应进入重试执行范围。");
    AssertEqual(
        ProductionConstants.UploadStatuses.Skipped,
        UploadSummaryStatusResolver.AggregateUploadStatuses([ProductionConstants.UploadStatuses.Skipped]),
        "只有已跳过任务时，汇总状态也应显示已跳过。");
}

static void StatusReportSettingsDefaultToEnabled()
{
    var settings = new AppSettings();

    AssertTrue(settings.EnableDeviceStatusReport == true, "设备状态上报开关默认启用，避免升级后行为突变。");
    AssertTrue(settings.EnableWorkOrderStatusReport == true, "工单状态上报开关默认启用，避免升级后行为突变。");
}

static void MesRouteSettingsDefaultToCurrentRoutes()
{
    var settings = new AppSettings();

    AssertEqual("api/User", settings.MesUserRoute, "员工信息接口默认路由必须保持原值。");
    AssertEqual("api/ItemsOfBatchTech", settings.MesWorkOrderRoute, "工单接口默认路由必须保持原值。");
    AssertEqual("api/ServerTime", settings.MesServerTimeRoute, "服务器时间接口默认路由必须保持原值。");
    AssertEqual("api/ExpProgram", settings.MesProgramManageRoute, "程序管理五个操作必须共用 api/ExpProgram 默认路由。");
    AssertEqual("api/ExpStartV2", settings.MesStartWorkRoute, "开工上报接口默认路由必须保持原值。");
    AssertEqual("api/ExpStatus", settings.MesWorkStatusRoute, "工单状态接口默认路由必须保持原值。");
    AssertEqual("api/ExpEnd", settings.MesEndWorkRoute, "完工上报接口默认路由必须保持原值。");
    AssertEqual("api/ExpFile", settings.MesReportFileRoute, "报告文件接口默认路由必须保持原值。");
    AssertEqual("api/PostData", settings.MesPostDataRoute, "过程参数接口默认路由必须保持原值。");
    AssertEqual("api/Device", settings.MesDeviceRoute, "设备编号同步接口默认路由必须保持原值。");
    AssertEqual("api/DeviceStatusV2", settings.MesDeviceStatusRoute, "设备状态上报接口默认路由必须保持原值。");
    AssertFalse(settings.EnablePostDataCustomHeader == true, "PostData 自定义 Header 默认关闭，避免升级后影响现场接口。");
}

static void MesProviderUsesConfiguredRoutes()
{
    var handler = new RecordingHttpMessageHandler();
    var settings = new FakeAppSettingsService
    {
        Current = BuildCustomMesRouteSettings()
    };
    using var provider = CreateMesProvider(settings, handler);
    var tempReportFile = Path.GetTempFileName();

    try
    {
        File.WriteAllText(tempReportFile, "report");

        provider.GetUserInfoAsync("U001").GetAwaiter().GetResult();
        provider.GetWorkOrderInfoAsync("WO-1").GetAwaiter().GetResult();
        provider.GetServerTimeAsync().GetAwaiter().GetResult();
        provider.TestConnectionAsync("http://127.0.0.1:8080/", 3, isWriteLog: false).GetAwaiter().GetResult();
        provider.GetProgramListAsync("D-1", "P-1").GetAwaiter().GetResult();
        provider.DownloadProgramAsync("D-1", "MES-P1").GetAwaiter().GetResult();
        provider.AddExpProgramAsync(new ProgramDataWriteReq()).GetAwaiter().GetResult();
        provider.UpdateExpProgramAsync(new ProgramDataWriteReq()).GetAwaiter().GetResult();
        provider.DeleteExpProgramAsync("D-1", "MES-P1").GetAwaiter().GetResult();
        provider.StartWorkAsync(new ExperimentStartReq()).GetAwaiter().GetResult();
        provider.ChangeWorkStatusAsync(new ReportExperimentStatusReq()).GetAwaiter().GetResult();
        provider.EndWorkAsync(new ExperimentEndReq()).GetAwaiter().GetResult();
        provider.UploadReportFileAsync(new UploadReportFileReq { FilePath = tempReportFile }).GetAwaiter().GetResult();
        provider.UploadProcessParametersAsync([new ProcessParameterUploadItem()]).GetAwaiter().GetResult();
        provider.SetDeviceIdAsync(new AddDeviceReq()).GetAwaiter().GetResult();
        provider.ReportDeviceStatusAsync(new ReportDeviceStatusReq()).GetAwaiter().GetResult();

        AssertSequenceEqual(
            [
                "mes/user-custom",
                "mes/work-order-custom",
                "mes/server-time-custom",
                "mes/server-time-custom",
                "mes/program-custom",
                "mes/program-custom",
                "mes/program-custom",
                "mes/program-custom",
                "mes/program-custom",
                "mes/start-custom",
                "mes/status-custom",
                "mes/end-custom",
                "mes/report-file-custom",
                "mes/post-data-custom",
                "mes/device-custom",
                "mes/device-status-custom"
            ],
            handler.Requests.Select(request => request.Path).ToArray(),
            "MES 所有业务调用都应使用系统设置中的路由。");
    }
    finally
    {
        File.Delete(tempReportFile);
    }
}

static void MesProviderAppliesPostDataHeaderFromLatestSettings()
{
    var handler = new RecordingHttpMessageHandler();
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            MesBaseUrl = "http://127.0.0.1:7098/",
            EnablePostDataCustomHeader = false,
            PostDataHeaderKey = "X-Factory",
            PostDataHeaderValue = "old"
        }
    };

    using var provider = CreateMesProvider(settings, handler);
    var changedSettings = settings.Current.Clone();
    changedSettings.MesPostDataRoute = "mes/post-data-after-save";
    changedSettings.EnablePostDataCustomHeader = true;
    changedSettings.PostDataHeaderKey = "X-Factory";
    changedSettings.PostDataHeaderValue = "line-1";
    settings.Save(changedSettings);

    provider.UploadProcessParametersAsync([new ProcessParameterUploadItem()]).GetAwaiter().GetResult();

    var request = handler.Requests.Single();
    AssertEqual("mes/post-data-after-save", request.Path, "保存设置后，PostData 路由应立即使用最新配置。");
    AssertTrue(request.Headers.TryGetValue("X-Factory", out var value), "启用 PostData 自定义 Header 后，请求必须带上配置的 Key。");
    AssertEqual("line-1", value, "启用 PostData 自定义 Header 后，请求必须带上配置的 Value。");
}

static void ElevatedAutoStartDefaultsToEnabled()
{
    var settings = new AppSettings();

    AssertTrue(settings.EnableElevatedAutoStart != false, "以管理员权限开机自启应默认启用，保证现场开机校时优先可用。");

    var plan = StartupIntegrationRules.CreatePlan(settings);

    AssertEqual(StartupIntegrationMode.ElevatedScheduledTask, plan.Mode, "默认配置应优先使用最高权限计划任务。");
}

static void StartupIntegrationRulesRemoveAllWhenAutoStartIsDisabled()
{
    var settings = new AppSettings
    {
        EnableAutoStart = false,
        EnableElevatedAutoStart = true
    };

    var plan = StartupIntegrationRules.CreatePlan(settings);

    AssertEqual(StartupIntegrationMode.Disabled, plan.Mode, "关闭开机自启时应同时移除普通启动项和计划任务。");
    AssertFalse(plan.EnableRunKey, "关闭开机自启时不能保留普通 Run 启动项。");
    AssertFalse(plan.EnableElevatedTask, "关闭开机自启时不能保留最高权限计划任务。");
}

static void StartupIntegrationRulesPreferElevatedScheduledTask()
{
    var settings = new AppSettings
    {
        EnableAutoStart = true,
        EnableElevatedAutoStart = true
    };

    var plan = StartupIntegrationRules.CreatePlan(settings);

    AssertEqual(StartupIntegrationMode.ElevatedScheduledTask, plan.Mode, "启用最高权限自启时应优先使用计划任务。");
    AssertFalse(plan.EnableRunKey, "计划任务成功时应移除普通 Run 启动项，避免重复启动。");
    AssertTrue(plan.EnableElevatedTask, "最高权限计划任务应被启用。");
}

static void StartupIntegrationResultReportsRunKeyFallback()
{
    var result = StartupIntegrationResult.RunKeyFallback("计划任务创建失败，已回退为普通开机自启。");

    AssertFalse(result.Success, "计划任务失败后即使有普通自启兜底，也需要向系统设置页报告失败。");
    AssertFalse(result.UsedElevatedTask, "回退普通自启时不应声明已使用最高权限计划任务。");
    AssertTrue(result.FallbackToRunKey, "结果应明确标记已经回退到普通 Run 启动项。");
    AssertTrue(result.Message.Contains("计划任务创建失败", StringComparison.Ordinal), "失败消息应保留计划任务失败原因。");
}

static void SystemClockSyncSkipsSmallOffset()
{
    var localTime = new DateTime(2026, 7, 1, 8, 0, 0);
    var serverTime = localTime.AddSeconds(3);

    var result = SystemClockSyncRules.Decide(serverTime, localTime);

    AssertTrue(result.Success, "服务器时间格式正确时规则应成功返回。");
    AssertFalse(result.Changed, "时间差未超过 5 秒时不应修改系统时间。");
    AssertEqual(3d, result.OffsetSeconds, "时间差应按服务器时间减本机时间计算。");
}

static void SystemClockSyncChangesLargeOffset()
{
    var localTime = new DateTime(2026, 7, 1, 8, 0, 0);
    var serverTime = localTime.AddSeconds(6);

    var result = SystemClockSyncRules.Decide(serverTime, localTime);

    AssertTrue(result.Success, "服务器时间格式正确时规则应成功返回。");
    AssertTrue(result.Changed, "时间差超过 5 秒时应触发系统校时。");
    AssertEqual(6d, result.OffsetSeconds, "触发校时时仍需保留时间差。");
}

static void SystemClockSyncRejectsInvalidServerTime()
{
    var result = SystemClockSyncRules.TryParseServerTime("not-a-time", out _);

    AssertFalse(result.Success, "服务器时间格式非法时应返回失败。");
    AssertFalse(result.Changed, "服务器时间格式非法时不能修改系统时间。");
    AssertTrue(result.Message.Contains("服务器时间格式无效", StringComparison.Ordinal), "失败消息应说明服务器时间格式无效。");
}

static void WeldTaskServerTimeSyncAdjustsSystemClock()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = SuccessServerTime("2026-07-01 08:00:06")
    };
    var clock = new FakeSystemClockService
    {
        CurrentTime = new DateTime(2026, 7, 1, 8, 0, 0)
    };
    var operations = new FakeOperationLogService();
    var service = CreateWeldTaskService(mes, clock, operations);

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(1, clock.SetLocalTimeCallCount, "服务器时间和本机时间相差超过阈值时必须尝试修改系统时间。");
    AssertEqual(new DateTime(2026, 7, 1, 8, 0, 6), clock.LastRequestedTime, "系统时间应按服务器返回时间设置。");
    AssertTrue(service.CurrentState.LastServerSyncMessage?.Contains("已校时", StringComparison.Ordinal) == true, "运行状态应提示已完成校时。");
    AssertTrue(operations.Entries.Any(entry => entry.Detail.Contains("Changed=True", StringComparison.Ordinal)), "操作日志应记录校时结果。");
}

static void WeldTaskServerTimeSyncSkipsClockOnMesFailure()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = new BasicRes<ServerTimeRes> { Status = "E", Msg = "MES 离线" }
    };
    var clock = new FakeSystemClockService();
    var service = CreateWeldTaskService(mes, clock, new FakeOperationLogService());

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(0, clock.SetLocalTimeCallCount, "MES 校时接口失败时不应修改系统时间。");
    AssertEqual("MES 离线", service.CurrentState.LastServerSyncMessage, "MES 失败消息应写入运行状态。");
}

static void WeldTaskServerTimeSyncReportsClockFailure()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = SuccessServerTime("2026-07-01 08:00:06")
    };
    var clock = new FakeSystemClockService
    {
        CurrentTime = new DateTime(2026, 7, 1, 8, 0, 0),
        SetLocalTimeResult = SystemClockSyncResult.Failed(
            new DateTime(2026, 7, 1, 8, 0, 6),
            new DateTime(2026, 7, 1, 8, 0, 0),
            6,
            "无权限修改系统时间")
    };
    var service = CreateWeldTaskService(mes, clock, new FakeOperationLogService());

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(1, clock.SetLocalTimeCallCount, "系统时间差超过阈值时仍应尝试校时。");
    AssertTrue(service.CurrentState.LastServerSyncMessage?.Contains("无权限修改系统时间", StringComparison.Ordinal) == true, "系统校时失败原因应写入运行状态。");
}

static void DeviceLifecycleServerTimeSelfCheckUsesSelfCheckEvent()
{
    var result = SystemClockSyncResult.ChangedResult(
        new DateTime(2026, 7, 1, 8, 0, 6),
        new DateTime(2026, 7, 1, 8, 0, 0),
        6,
        "已校时");

    var entry = DeviceLifecycleLogRules.CreateServerTimeSelfCheckEntry(
        "D-001",
        result,
        new DateTime(2026, 7, 1, 8, 0, 7));

    AssertEqual(AppConstants.DeviceLifecycleEventTypes.SelfCheck, entry.EventType, "MES 校时属于开机自检，应复用 SelfCheck 事件类型。");
    AssertEqual("MES", entry.Source, "MES 校时自检来源应标记为 MES。");
    AssertEqual("Success", entry.Status, "校时成功应写入 Success 状态。");
    AssertEqual("MES服务器校时成功", entry.Summary, "摘要应明确展示 MES 服务器校时成功。");
    AssertTrue(entry.Detail.Contains("ServerTime=2026-07-01 08:00:06", StringComparison.Ordinal), "详情应包含服务器时间。");
    AssertTrue(entry.Detail.Contains("Changed=True", StringComparison.Ordinal), "详情应包含是否修改系统时间。");
}

static void WeldTaskServerTimeSyncWritesDeviceLifecycleSuccessLog()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = SuccessServerTime("2026-07-01 08:00:02")
    };
    var clock = new FakeSystemClockService
    {
        CurrentTime = new DateTime(2026, 7, 1, 8, 0, 0)
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var service = CreateWeldTaskService(mes, clock, new FakeOperationLogService(), lifecycleLogs);

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(0, clock.SetLocalTimeCallCount, "时间差未超过阈值时不应修改系统时间。");
    AssertEqual(1, lifecycleLogs.Entries.Count, "每次启动校时完成后都应写入一条设备自检日志。");
    AssertEqual("Success", lifecycleLogs.Entries[0].Status, "无需校时也属于自检成功。");
    AssertEqual("MES服务器校时成功", lifecycleLogs.Entries[0].Summary, "成功日志摘要应明确。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("Changed=False", StringComparison.Ordinal), "无需校时时详情应记录 Changed=False。");
}

static void WeldTaskServerTimeSyncWritesDeviceLifecycleFailureLogs()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = new BasicRes<ServerTimeRes> { Status = "E", Msg = "MES 离线" }
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var service = CreateWeldTaskService(mes, new FakeSystemClockService(), new FakeOperationLogService(), lifecycleLogs);

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(1, lifecycleLogs.Entries.Count, "MES 接口失败也应写入设备自检日志。");
    AssertEqual("Failed", lifecycleLogs.Entries[0].Status, "MES 接口失败应写入 Failed 状态。");
    AssertEqual("MES服务器校时失败", lifecycleLogs.Entries[0].Summary, "失败日志摘要应明确。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("MES 离线", StringComparison.Ordinal), "失败详情应包含 MES 返回原因。");

    mes.ServerTimeResponse = SuccessServerTime("not-a-time");
    lifecycleLogs.Entries.Clear();

    service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertEqual(1, lifecycleLogs.Entries.Count, "服务器时间格式非法也应写入设备自检日志。");
    AssertEqual("Failed", lifecycleLogs.Entries[0].Status, "服务器时间格式非法应写入 Failed 状态。");
    AssertTrue(lifecycleLogs.Entries[0].Detail.Contains("服务器时间格式无效", StringComparison.Ordinal), "失败详情应包含格式错误原因。");
}

static void WeldTaskServerTimeSyncIgnoresDeviceLifecycleLogFailure()
{
    var mes = new FakeMesProvider
    {
        ServerTimeResponse = SuccessServerTime("2026-07-01 08:00:06")
    };
    var clock = new FakeSystemClockService
    {
        CurrentTime = new DateTime(2026, 7, 1, 8, 0, 0)
    };
    var lifecycleLogs = new FakeDeviceLifecycleLogService
    {
        ThrowOnWrite = true
    };
    var service = CreateWeldTaskService(mes, clock, new FakeOperationLogService(), lifecycleLogs);

    var response = service.SyncServerTimeAsync().GetAwaiter().GetResult();

    AssertTrue(response.IsSuccess, "设备日志写入失败不能影响 MES 校时接口返回。");
    AssertEqual(1, clock.SetLocalTimeCallCount, "设备日志失败不能阻断系统校时。");
    AssertTrue(service.CurrentState.LastServerSyncMessage?.Contains("已校时", StringComparison.Ordinal) == true, "设备日志失败不能覆盖校时状态消息。");
}

static void DeviceLifecycleSelfCheckSummariesDescribeConnectionResult()
{
    var occurredTime = new DateTime(2026, 7, 1, 8, 0, 0);

    AssertEqual(
        "PLC连接成功",
        DeviceLifecycleLogRules.CreateSelfCheckEntry("D-001", 1, "PLC", true, "PLC 已连接", occurredTime).Summary,
        "PLC 自检摘要应展示实际连接结果。");
    AssertEqual(
        "MES连接失败",
        DeviceLifecycleLogRules.CreateSelfCheckEntry("D-001", 0, "MES", false, "MES 离线", occurredTime).Summary,
        "MES 自检摘要应展示实际连接结果。");
    AssertEqual(
        "看板连接成功",
        DeviceLifecycleLogRules.CreateSelfCheckEntry("D-001", 0, "CenterServer", true, "看板在线", occurredTime).Summary,
        "看板自检摘要应使用现场可读名称。");
    AssertEqual(
        "Camera连接失败",
        DeviceLifecycleLogRules.CreateSelfCheckEntry("D-001", 0, "Camera", false, "相机离线", occurredTime).Summary,
        "未知来源仍应按来源名称展示连接结果。");

    AssertEqual(
        "HTTP服务启动成功",
        DeviceLifecycleLogRules.CreateDeviceApiHttpSelfCheckEntry("D-001", "http://127.0.0.1:7098/", true, "监听成功", occurredTime).Summary,
        "HTTP 服务自检摘要应展示服务启动结果。");
    AssertEqual(
        "HTTP服务启动失败",
        DeviceLifecycleLogRules.CreateDeviceApiHttpSelfCheckEntry("D-001", "http://127.0.0.1:7098/", false, "端口被占用", occurredTime).Summary,
        "HTTP 服务自检失败摘要应展示服务启动失败。");
}

static void DeviceLifecycleSoftwareCloseEntryRecordsSoftwareClose()
{
    var occurredTime = new DateTime(2026, 7, 7, 18, 30, 0);
    var entry = DeviceLifecycleLogRules.CreateSoftwareStoppedEntry("D-001", occurredTime);

    AssertEqual(occurredTime, entry.OccurredTime, "软件关闭日志应保留实际关闭时间。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.SoftwareStopped, entry.EventType, "软件关闭应使用独立生命周期事件类型。");
    AssertEqual("Application", entry.Source, "软件关闭来源应标记为应用程序。");
    AssertEqual("Success", entry.Status, "正常关闭应写入成功状态。");
    AssertEqual("软件关闭", entry.Summary, "设备日志摘要必须显示软件关闭。");
    AssertTrue(entry.Detail.Contains("关闭", StringComparison.Ordinal), "详情应说明软件正在关闭或已关闭。");
}

static void DeviceLifecycleCoordinatorRecordsSoftwareLifecycleStatuses()
{
    var coordinatorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);

    AssertTrue(
        coordinatorCode.Contains("CreateSoftwareStoppedEntry", StringComparison.Ordinal),
        "程序关闭时必须写入“软件关闭”设备生命周期日志。");
    AssertTrue(
        CountOccurrences(coordinatorCode, "forceWrite: true") >= 2,
        "软件启动和关闭的设备状态日志都必须强制写入，不能被相同状态去重。");
}

static void DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");
    coordinator.Stop();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped),
        "停机设备状态日志应在停止后写入。");

    var softwareStarted = lifecycleLogs.Entries.Single(entry => entry.EventType == AppConstants.DeviceLifecycleEventTypes.SoftwareStarted);
    var poweredOn = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn);
    var softwareStopped = lifecycleLogs.Entries.Single(entry => entry.EventType == AppConstants.DeviceLifecycleEventTypes.SoftwareStopped);
    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);

    AssertEqual(softwareStarted.OccurredTime, poweredOn.OccurredTime, "设备日志的软件开启时间必须和设备状态开机时间一致。");
    AssertEqual(softwareStopped.OccurredTime, stopped.OccurredTime, "设备日志的软件关闭时间必须和设备状态停机时间一致。");
}

static void DeviceLifecycleStopTriggersBackgroundStatusUpload()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");

    coordinator.Stop();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped),
        "停机设备状态日志应在停止后写入。");

    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);
    AssertEqual(ProductionConstants.MesDeviceStatuses.Stopped, stopped.DeviceStatus, "停止协调器时必须写入停机状态。");
    AssertTrue(statusService.LastReportInBackground == true, "停机状态应触发后台上传，不能同步阻塞 UI。");
    AssertTrue(statusService.LastReportToMes == true, "停机状态应先尝试 MES 上传，而不是只进入待上传队列。");
}

static void DeviceLifecycleStopReportsStatusWhenLifecycleLogFails()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");

    lifecycleLogs.ThrowOnWrite = true;
    coordinator.Stop();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped),
        "软件关闭日志写入失败时，停机设备状态仍应写入。");

    AssertTrue(statusService.LastReportInBackground == true, "软件关闭日志失败也不能让停机状态改成同步上传。");
    AssertTrue(statusService.LastReportToMes == true, "软件关闭日志失败也必须先尝试 MES 停机状态上传。");
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
    AssertEqual("PLC连接成功", entry.Summary, "连接自检摘要应直接表达被检测对象和结果。");
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

static void OfflineProgramDropdownIncludesEmptyContentProgram()
{
    var programs = Enumerable.Range(1, 4)
        .Select(id => new BizProgram
        {
            Id = id,
            ProgramName = $"程序{id}",
            ProductNum = $"P-{id}",
            RecipeCode = $"{id}",
            ProgramContent = id == 4 ? string.Empty : "{}"
        })
        .ToArray();

    var options = OfflineStartInputRules.BuildProgramNameOptions(programs);

    AssertEqual(4, options.Count, "本地程序列表中的空内容程序也应显示在 MonitorView 下拉框中。");
    AssertTrue(options.Any(option => option.Program.Id == 4), "空内容程序不能因为 ProgramContent 为空而被下拉过滤。");
}

static void RecipeCodeOptionsSortNumericAscending()
{
    var options = OfflineStartInputRules.BuildRecipeCodeOptions(new[]
    {
        "3",
        "1",
        "10",
        "2",
        "4",
        " 2 ",
        string.Empty,
        null,
        "A2",
        "A1"
    });

    AssertSequenceEqual(
        new[] { "1", "2", "3", "4", "10", "A1", "A2" },
        options,
        "配方号候选列表应先按数字正序显示，非数字配方号排在数字后按文本正序显示。");
}

static void ProductHistoryPreviewSortsLatestProductFirst()
{
    var older = new ProductHistoryProduct
    {
        ProductNo = "P-001",
        LastRecordTime = new DateTime(2026, 7, 11, 8, 0, 0),
        Points =
        [
            new ProductHistoryPoint { TouchNo = "1", SequenceNo = 1, RecordTime = new DateTime(2026, 7, 11, 8, 0, 0) },
            new ProductHistoryPoint { TouchNo = "2", SequenceNo = 2, RecordTime = new DateTime(2026, 7, 11, 8, 0, 1) }
        ]
    };
    var newer = new ProductHistoryProduct
    {
        ProductNo = "P-002",
        LastRecordTime = new DateTime(2026, 7, 11, 8, 5, 0),
        Points =
        [
            new ProductHistoryPoint { TouchNo = "1", SequenceNo = 1, RecordTime = new DateTime(2026, 7, 11, 8, 5, 0) }
        ]
    };

    var sorted = ProductHistoryPreviewSortRules.OrderProductsLatestFirst([older, newer]);

    AssertEqual("P-002", sorted[0].ProductNo, "产品历史预览应按最近采集时间倒序显示，最新产品在首屏顶部。");
    AssertEqual("P-001", sorted[1].ProductNo, "较早产品应排在最新产品之后。");
    AssertEqual("1", sorted[1].Points[0].TouchNo, "产品内焊点明细应保持原有顺序，不因父级倒序被反转。");
    AssertEqual("2", sorted[1].Points[1].TouchNo, "产品内焊点明细应保持原有顺序。");
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
    AssertEqual("引出线", request.ProductName, "离线开工应使用界面输入的部件名称。");
    AssertEqual("DR-9", request.DrawingNo, "离线开工应使用界面输入的图号。");
    AssertEqual("OP20", request.ProcessNo, "离线开工应使用界面输入的工序号。");
    AssertEqual(12, request.PlannedQty, "离线开工应使用界面输入的计划数量。");
    AssertEqual("164#J", request.ProductNum, "离线开工应使用选中程序关联的产品工号。");
    AssertEqual("M-164", request.ProductModel, "离线开工应使用选中程序关联的产品型号。");
    AssertEqual("5", request.RecipeCode, "离线开工应使用选中程序关联的配方号。");
    AssertEqual("{\"steps\":3}", request.ProgramContent, "离线开工应使用选中程序的程序内容。");
}

static void OfflineStartAllowsEmptyPartNameAndDrawingNumber()
{
    var option = OfflineStartInputRules.BuildProgramNameOptions(new[]
    {
        new BizProgram
        {
            Id = 10,
            ProgramName = "离线程序",
            ProgramContent = "{}",
            ProductNum = "164#J",
            RecipeCode = "5"
        }
    }).Single();
    var emptyOptionalFields = new OfflineStartInput(
        StationNo: 1,
        WorkOrderId: "WO-EMPTY",
        Batch: string.Empty,
        Spec: string.Empty,
        ProcessNo: "OP10",
        ProcessName: string.Empty,
        PlannedQtyText: "1",
        ProductName: "   ",
        DrawingNo: "   ");

    var emptyRequest = OfflineStartInputRules.BuildRequest(emptyOptionalFields, option);

    AssertEqual(string.Empty, emptyRequest.ProductName, "离线开工的部件名称应允许为空并规范化为空字符串。");
    AssertEqual(string.Empty, emptyRequest.DrawingNo, "离线开工的图号应允许为空并规范化为空字符串。");

    var partOnlyRequest = OfflineStartInputRules.BuildRequest(
        emptyOptionalFields with { ProductName = "  引出线  " },
        option);
    AssertEqual("引出线", partOnlyRequest.ProductName, "非空部件名称应去除首尾空格。");
    AssertEqual(string.Empty, partOnlyRequest.DrawingNo, "仅填写部件名称时图号仍应允许为空。");

    var drawingOnlyRequest = OfflineStartInputRules.BuildRequest(
        emptyOptionalFields with { DrawingNo = "  DR-10  " },
        option);
    AssertEqual(string.Empty, drawingOnlyRequest.ProductName, "仅填写图号时部件名称仍应允许为空。");
    AssertEqual("DR-10", drawingOnlyRequest.DrawingNo, "非空图号应去除首尾空格。");
}

static void OfflineStartRequiresWorkOrderAndProcessNumber()
{
    var option = OfflineStartInputRules.BuildProgramNameOptions(new[]
    {
        new BizProgram
        {
            Id = 11,
            ProgramName = "离线程序",
            ProgramContent = "{}",
            ProductNum = "164#J",
            RecipeCode = "5"
        }
    }).Single();
    var validInput = new OfflineStartInput(
        StationNo: 1,
        WorkOrderId: "WO-REQUIRED",
        Batch: string.Empty,
        Spec: string.Empty,
        ProcessNo: "OP10",
        ProcessName: string.Empty,
        PlannedQtyText: "1",
        ProductName: string.Empty,
        DrawingNo: string.Empty);

    AssertInvalidOperationMessage(
        () => OfflineStartInputRules.BuildRequest(validInput with { WorkOrderId = "   " }, option),
        "工单号不能为空。",
        "离线开工必须校验工单号。");
    AssertInvalidOperationMessage(
        () => OfflineStartInputRules.BuildRequest(validInput with { ProcessNo = "   " }, option),
        "工序号不能为空。",
        "离线开工必须校验工序号且不得自动回填 OP10。");
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

static void ProgramMesCreatePayloadClearsFileFieldsForEmptyContent()
{
    var program = BuildSyncedProgram();
    program.ProgramId = null;
    program.ProgramContent = "  { \r\n }  ";
    program.ProgramFile = ProgramFileRules.EncodeJsonToBase64(program.ProgramContent);
    program.ProgramFileName = "P1.json";

    var payload = ProgramMesPayloadRules.ToCreateRequest(program, AppConstants.ProgramRemarkActions.Create);

    AssertEqual(string.Empty, payload.ProgramContent, "新增程序未填写设定值时，ProgramContent 应留空。");
    AssertEqual(string.Empty, payload.ProgramFile, "新增程序未填写设定值时，ProgramFile 应留空。");
    AssertEqual(string.Empty, payload.FileType, "新增程序未填写设定值时，FileType 应留空。");
}

static void ProgramContentRulesDetectConfiguredValues()
{
    AssertFalse(ProgramContentJsonRules.HasConfiguredValues(null), "空程序内容不应视为已填写设定值。");
    AssertFalse(ProgramContentJsonRules.HasConfiguredValues("  { \r\n }  "), "空 JSON 对象不应视为已填写设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("{\"高度\":\"12.5\"}"), "包含设定项的 JSON 对象应视为已填写设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("[\"历史内容\"]"), "非对象历史内容不应被误判为空设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("not-json"), "非法历史内容不应被误判为空设定值。");
}

static void ProgramManageServiceClearsAutomaticFileForEmptyContent()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    var applyRequestMethod = ExtractMethodText(
        serviceCode,
        "    private void ApplyRequest(BizProgram entity, SaveProgramReq request)",
        "    private AppSettings CurrentSettings");

    AssertTrue(applyRequestMethod.Contains("ProgramContentJsonRules.HasConfiguredValues(entity.ProgramContent)", StringComparison.Ordinal), "程序保存必须使用统一规则判断是否存在有效设定值。");
    AssertTrue(applyRequestMethod.Contains("var previousProgramFilePath", StringComparison.Ordinal), "程序保存覆盖名称前必须保留旧自动文件路径。");
    AssertTrue(applyRequestMethod.Contains("ClearProgramContentFile(entity, settings, previousProgramFilePath)", StringComparison.Ordinal), "清空设定值时必须同时清理旧名称对应的自动文件。");
    var contentCheckIndex = applyRequestMethod.IndexOf("ProgramContentJsonRules.HasConfiguredValues(entity.ProgramContent)", StringComparison.Ordinal);
    var writeFileIndex = applyRequestMethod.IndexOf("WriteProgramContentFile(entity, settings);", StringComparison.Ordinal);
    AssertTrue(writeFileIndex > contentCheckIndex, "写入本地程序文件必须位于有效设定值判断的条件分支内。");
    AssertTrue(serviceCode.Contains("entity.ProgramFile = string.Empty;", StringComparison.Ordinal), "清理自动文件后必须清空程序文件内容。");
    AssertTrue(serviceCode.Contains("entity.ProgramFileName = string.Empty;", StringComparison.Ordinal), "清理自动文件后必须清空程序文件名。");
}
static void ProgramManageViewHidesProductModel()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.Designer.cs"), Encoding.UTF8);
    var textKeysCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertFalse(designerCode.Contains("inputProductModel", StringComparison.Ordinal), "程序管理页 Designer 不应再声明产品型号输入框。");
    AssertFalse(designerCode.Contains("lblProductModel", StringComparison.Ordinal), "程序管理页 Designer 不应再声明产品型号标签。");
    AssertFalse(designerCode.Contains("tlpProductModel", StringComparison.Ordinal), "程序管理页 Designer 不应再声明产品型号布局行。");
    AssertFalse(viewCode.Contains("nameof(BizProgram.ProductModel)", StringComparison.Ordinal), "程序管理页列表不应再绑定产品型号列。");
    AssertFalse(viewCode.Contains("TextKeys.ProgramManage.LabelProductModel", StringComparison.Ordinal), "程序管理页不应再读取产品型号编辑标签资源。");
    AssertFalse(viewCode.Contains("TextKeys.Grid.ProgramProductModel", StringComparison.Ordinal), "程序管理页不应再读取产品型号列表表头资源。");
    AssertFalse(viewCode.Contains("Contains(program.ProductModel", StringComparison.Ordinal), "程序管理页搜索不应再匹配产品型号。");
    AssertFalse(viewCode.Contains("NormalizeSortText(program.ProductModel)", StringComparison.Ordinal), "程序管理页排序不应再使用产品型号。");
    AssertFalse(textKeysCode.Contains("program.label.product_model", StringComparison.Ordinal), "程序管理页产品型号编辑资源键应移除。");
    AssertFalse(textKeysCode.Contains("grid.program.product_model", StringComparison.Ordinal), "程序管理页产品型号列表资源键应移除。");
    AssertFalse(zhResources.Contains("program.label.product_model", StringComparison.Ordinal), "中文资源不应再包含程序管理产品型号编辑文本。");
    AssertFalse(zhResources.Contains("grid.program.product_model", StringComparison.Ordinal), "中文资源不应再包含程序管理产品型号列表文本。");
    AssertFalse(enResources.Contains("program.label.product_model", StringComparison.Ordinal), "英文资源不应再包含程序管理产品型号编辑文本。");
    AssertFalse(enResources.Contains("grid.program.product_model", StringComparison.Ordinal), "英文资源不应再包含程序管理产品型号列表文本。");
}

static void ProgramManageSaveIgnoresProductModelInput()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);

    AssertFalse(viewCode.Contains("request.ProductModel", StringComparison.Ordinal), "程序管理页保存请求不应再从界面组包产品型号。");
    AssertFalse(viewCode.Contains("inputProductModel", StringComparison.Ordinal), "程序管理页保存和绑定逻辑不应再访问产品型号输入框。");
    AssertFalse(serviceCode.Contains("entity.ProductModel = request.ProductModel", StringComparison.Ordinal), "保存程序时不应再把请求中的产品型号写回程序实体。");
    AssertFalse(serviceCode.Contains("request.ProductModel = request.ProductModel.Trim()", StringComparison.Ordinal), "保存程序请求规范化不应再处理产品型号。");
}

static void MonitorReportButtonRulesFollowMesAndTaskState()
{
    var idleOnline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: false);
    AssertTrue(idleOnline.ShowOnlineReportButton, "在线空闲时应显示在线上报按钮。");
    AssertEqual(MonitorOnlineReportAction.Start, idleOnline.OnlineReportAction, "在线空闲时在线按钮应执行开工上报。");
    AssertTrue(idleOnline.OnlineReportEnabled, "MES 在线时在线上报按钮应可用。");
    AssertFalse(idleOnline.LocalWorkOrderEnabled, "MES 在线空闲时应禁用离线开工。");

    var runningOnline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: true,
        hasOfflineRunningTask: false);
    AssertTrue(runningOnline.ShowOnlineReportButton, "在线开工后仍应显示在线上报按钮。");
    AssertEqual(MonitorOnlineReportAction.Finish, runningOnline.OnlineReportAction, "在线开工后在线按钮应执行完工上报。");

    var offline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: false,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: false);
    AssertFalse(offline.OnlineReportEnabled, "MES 离线且无在线未完工任务时在线上报按钮应禁用。");
    AssertTrue(offline.LocalWorkOrderEnabled, "MES 离线空闲时离线开工应可用。");

    var offlineWithOnlineTask = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: false,
        hasOnlineRunningTask: true,
        hasOfflineRunningTask: false);
    AssertEqual(MonitorOnlineReportAction.Finish, offlineWithOnlineTask.OnlineReportAction, "在线未完工任务耗时断网时在线按钮仍应执行完工上报。");
    AssertTrue(offlineWithOnlineTask.OnlineReportEnabled, "断网但有在线未完工任务时必须允许完工上报，由 FinishAsync 负责入队补传。");
    AssertFalse(offlineWithOnlineTask.LocalWorkOrderEnabled, "在线任务不应由离线按钮接管。");

    var readOnlyOfflineWithOnlineTask = MonitorReportButtonRules.Decide(
        isReadOnly: true,
        mesConnected: false,
        hasOnlineRunningTask: true,
        hasOfflineRunningTask: false);
    AssertFalse(readOnlyOfflineWithOnlineTask.OnlineReportEnabled, "只读工位即使断网且有在线未完工任务也不应允许完工上报。");
    AssertFalse(readOnlyOfflineWithOnlineTask.LocalWorkOrderEnabled, "只读工位不应启用离线开工按钮。");

    var bothRunningWhenOffline = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: false,
        hasOnlineRunningTask: true,
        hasOfflineRunningTask: true);
    AssertTrue(bothRunningWhenOffline.OnlineReportEnabled, "同时存在在线与离线未完工任务时应以在线完工按钮为准。");
    AssertFalse(bothRunningWhenOffline.LocalWorkOrderEnabled, "同时存在在线未完工任务时不应由离线按钮接管在线完工。");

    var offlineTaskWhenMesBack = MonitorReportButtonRules.Decide(
        isReadOnly: false,
        mesConnected: true,
        hasOnlineRunningTask: false,
        hasOfflineRunningTask: true);
    AssertTrue(offlineTaskWhenMesBack.LocalWorkOrderEnabled, "已有离线未完工任务时即使 MES 恢复也应允许本地完工。");
}

static void MonitorViewUsesOneOnlineReportButton()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);

    AssertFalse(designerCode.Contains("btnExpEnd", StringComparison.Ordinal), "监控页 Designer 不应再保留独立完工上报按钮。");
    AssertTrue(designerCode.Contains("btnOnlineReport", StringComparison.Ordinal), "监控页 Designer 应保留单一在线上报按钮。");
    AssertTrue(viewCode.Contains("PermissionCodes.Buttons.Monitor.StartReport", StringComparison.Ordinal), "在线按钮开工状态必须检查开工权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Buttons.Monitor.FinishReport", StringComparison.Ordinal), "在线按钮完工状态必须检查完工权限。");
    AssertTrue(viewCode.Contains("OnlineReport_Click", StringComparison.Ordinal), "在线按钮点击入口必须统一分派开工或完工流程。");
}

static void MonitorRuntimeTipsUseLocalizedSummaries()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var textKeysCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("btnClearErrorTips", StringComparison.Ordinal), "异常提示区域必须声明清除按钮。");
    AssertTrue(viewCode.Contains("btnClearErrorTips.Click += (_, _) => ClearRuntimeError();", StringComparison.Ordinal), "清除按钮必须复用当前运行异常清空逻辑。");
    AssertTrue(viewCode.Contains("btnClearErrorTips.Visible = hasError;", StringComparison.Ordinal), "清除按钮显隐必须跟随当前异常摘要。");

    var requiredKeys = new[]
    {
        "monitor.button.clear_error_tips",
        "monitor.runtime.program_confirmed",
        "monitor.runtime.work_order_loaded",
        "monitor.runtime.process_selected",
        "monitor.runtime.local_start_succeeded",
        "monitor.runtime.online_start_succeeded",
        "monitor.runtime.online_finish_succeeded",
        "monitor.runtime.local_finish_succeeded",
        "monitor.runtime.product_data_collected",
        "monitor.runtime.recipe_code_write_succeeded",
        "monitor.runtime.recipe_code_validation_succeeded",
        "monitor.runtime.test_flag_updated",
        "monitor.error.read_only_operation_blocked",
        "monitor.error.work_order_required",
        "monitor.error.active_task_blocks_edit",
        "monitor.error.program_name_required",
        "monitor.error.start_info_required",
        "monitor.error.test_flag_update_failed",
        "monitor.error.recipe_validation_failed",
        "monitor.error.business_signal_write_failed",
        "monitor.error.station_operation_busy",
        "monitor.error.station_report_failed",
        "monitor.error.finish_quantity_read_failed",
        "monitor.error.device_alarm"
    };

    foreach (var key in requiredKeys)
    {
        AssertTrue(textKeysCode.Contains(key, StringComparison.Ordinal), $"TextKeys 必须声明 {key}。");
        AssertTrue(zhResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"中文资源必须包含 {key}。");
        AssertTrue(enResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"英文资源必须包含 {key}。");
    }

    AssertTrue(viewCode.Contains("SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.ProgramConfirmed)", StringComparison.Ordinal), "加工程序确认提示必须保存本地化资源键。");
    AssertTrue(viewCode.Contains("SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.WorkOrderLoaded)", StringComparison.Ordinal), "工单获取完成提示必须保存本地化资源键。");
    AssertTrue(viewCode.Contains("SetRuntimeError(TextKeys.Monitor.RuntimeError.BusinessSignalWriteFailed)", StringComparison.Ordinal), "业务信号写入失败摘要必须保存本地化资源键。");
    AssertTrue(viewCode.Contains("SetRuntimeErrorWithSource(TextKeys.Monitor.RuntimeError.DeviceAlarm", StringComparison.Ordinal), "设备报警摘要必须保存带来源的本地化资源键。");
}

static void MonitorViewShowsOperatorValidationSuccessAfterEmployeeValidation()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var textKeysCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);
    var promptMethod = ExtractMethodText(
        viewCode,
        "private async Task<string> PromptValidatedOperatorAsync",
        "private void BindMesOperatorInfo");
    var inlineMethod = ExtractMethodText(
        viewCode,
        "private async Task ValidateOperatorInlineAsync",
        "private bool TryPromptNonNegativeInt");

    AssertTrue(textKeysCode.Contains("OperatorValidated", StringComparison.Ordinal), "TextKeys 必须声明员工身份校验通过运行状态键。");
    AssertTrue(textKeysCode.Contains("monitor.runtime.operator_validated", StringComparison.Ordinal), "TextKeys 必须声明 monitor.runtime.operator_validated。");
    AssertTrue(zhResources.Contains("name=\"monitor.runtime.operator_validated\"", StringComparison.Ordinal), "中文资源必须包含员工身份校验通过运行状态。");
    AssertTrue(zhResources.Contains("<value>员工身份校验通过</value>", StringComparison.Ordinal), "中文资源必须显示员工身份校验通过。");
    AssertTrue(enResources.Contains("name=\"monitor.runtime.operator_validated\"", StringComparison.Ordinal), "英文资源必须包含员工身份校验通过运行状态。");
    AssertTrue(enResources.Contains("<value>Operator validation succeeded.</value>", StringComparison.Ordinal), "英文资源必须显示员工身份校验通过。");

    var promptBindIndex = promptMethod.IndexOf("BindMesOperatorInfo(response.Data, form.EmployeeNumber);", StringComparison.Ordinal);
    var promptSuccessIndex = promptMethod.IndexOf("SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OperatorValidated);", StringComparison.Ordinal);
    var inlineBindIndex = inlineMethod.IndexOf("BindMesOperatorInfo(response.Data, employeeNumber);", StringComparison.Ordinal);
    var inlineSuccessIndex = inlineMethod.IndexOf("SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OperatorValidated);", StringComparison.Ordinal);

    AssertTrue(promptBindIndex >= 0, "弹窗校验成功后必须回填员工信息。");
    AssertTrue(promptSuccessIndex > promptBindIndex, "弹窗校验成功回填员工信息后必须同步显示员工身份校验通过。");
    AssertTrue(inlineBindIndex >= 0, "内联校验成功后必须回填员工信息。");
    AssertTrue(inlineSuccessIndex > inlineBindIndex, "内联校验成功回填员工信息后必须同步显示员工身份校验通过。");
    AssertTrue(promptMethod.Contains("TextKeys.Monitor.Message.OperatorValidationFailed", StringComparison.Ordinal), "弹窗校验失败仍应保留现有失败提示。");
    AssertTrue(inlineMethod.Contains("SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorValidationFailedInline);", StringComparison.Ordinal), "内联校验失败仍应保留现有失败提示。");
}

static void MonitorViewKeepsInlineOperatorValidationMarkerAfterBinding()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var startMethod = ExtractMethodText(
        viewCode,
        "private async Task RunStartReportAsync()",
        "private async Task RunFinishReportAsync()");
    var inlineMethod = ExtractMethodText(
        viewCode,
        "private async Task ValidateOperatorInlineAsync",
        "private bool TryPromptNonNegativeInt");
    var markerMethod = ExtractMethodText(
        viewCode,
        "private void MarkInlineOperatorValidated",
        "private bool IsInlineOperatorValidated");
    var checkMethod = ExtractMethodText(
        viewCode,
        "private bool IsInlineOperatorValidated",
        "private bool TryPromptNonNegativeInt");

    var bindIndex = inlineMethod.IndexOf("BindMesOperatorInfo(response.Data, employeeNumber);", StringComparison.Ordinal);
    var markerIndex = inlineMethod.IndexOf("MarkInlineOperatorValidated();", StringComparison.Ordinal);

    AssertTrue(bindIndex >= 0, "内联员工校验成功后必须先回填员工信息。");
    AssertTrue(markerIndex > bindIndex, "内联员工校验标记必须在员工号回填后更新，确保使用控件最终显示的员工号。");
    AssertTrue(markerMethod.Contains("_validatedOperatorNumber = MesUserNumber.Text.Trim();", StringComparison.Ordinal), "内联校验标记必须使用回填后的 MesUserNumber.Text。");
    AssertTrue(startMethod.Contains("if (!IsInlineOperatorValidated(employeeNumber))", StringComparison.Ordinal), "开工前员工号校验判断必须走统一 helper。");
    AssertTrue(checkMethod.Contains("string.Equals(employeeNumber.Trim(), _validatedOperatorNumber?.Trim(), StringComparison.Ordinal)", StringComparison.Ordinal), "员工号校验比较必须修剪两侧文本，避免 MES 返回值格式化后误判。");
}

static void MonitorViewAutoLoadsWorkOrderWithoutQueryButton()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);

    AssertFalse(designerCode.Contains("btnGetWO", StringComparison.Ordinal), "监控页 Designer 不应再声明获取工单按钮。");
    AssertFalse(designerCode.Contains("button.monitor.get-work-order", StringComparison.Ordinal), "监控页 Designer 不应再绑定获取工单按钮权限。");
    AssertFalse(viewCode.Contains("GetWorkOrder_Click", StringComparison.Ordinal), "监控页不应再保留按钮驱动的获取工单入口。");
    AssertFalse(viewCode.Contains("PrepareWorkOrderAsync", StringComparison.Ordinal), "监控页不应再保留按钮驱动的准备工单流程。");
    AssertTrue(viewCode.Contains("inputSN.KeyDown += WorkOrderInput_KeyDown;", StringComparison.Ordinal), "工单号输入框必须支持回车立即自动查询。");
    AssertFalse(viewCode.Contains("_manualWorkOrderQueryTimer", StringComparison.Ordinal), "工单号人工输入不应再使用防抖自动查询。");
    AssertTrue(viewCode.Contains("ConfirmManualWorkOrderInput", StringComparison.Ordinal), "工单号手动输入必须在回车后进入确认入口。");
    AssertTrue(viewCode.Contains("AutoLoadWorkOrderInfoAsync(stationNo, workId)", StringComparison.Ordinal), "PLC 和手动输入应复用同一套自动加载工单流程。");
}

static void MonitorViewPreservesOnlineInputsDuringRefresh()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("_pendingOnlineProgramName", StringComparison.Ordinal), "在线程序已选但未确认时必须缓存程序名称，避免 StateChanged 刷空下拉框。");
    AssertTrue(viewCode.Contains("ApplyOnlineProgramSelectionPreview", StringComparison.Ordinal), "在线选择程序后必须立即联动显示程序名称和配方号。");
    AssertTrue(viewCode.Contains("DownloadSelectedOnlineProgramAsync(programListItem, CurrentStationNo)", StringComparison.Ordinal), "在线程序下载必须使用事件解析出的程序项，避免 StateChanged 刷新后按控件索引取空。");
    AssertTrue(viewCode.Contains("SyncOnlineProgramSelectionAfterDownload(detail);", StringComparison.Ordinal), "在线程序确认后应保持下拉选中该程序，重复点击同一项仍会触发 SelectedIndexChanged，无需释放选中索引。");
    AssertTrue(viewCode.Contains("ResolveRecipeCodeForPendingProgram", StringComparison.Ordinal), "配方号应按已选程序从本地同步程序表解析。");
    AssertTrue(viewCode.Contains("ShouldPreserveDraftOperatorNumber", StringComparison.Ordinal), "在线员工号未校验时刷新必须保留正在输入的员工号。");
    AssertTrue(viewCode.Contains("ClearMesOperatorDisplayInfo();", StringComparison.Ordinal), "保留员工号时只应清空姓名、部门和班组显示。");
}

static void MonitorViewLinksProgramAndRecipeSelectionsForStartInput()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var wireEvents = ExtractMethodText(
        viewCode,
        "private void WireEvents()",
        "private void WireWeldPreviewGridEvents");
    var destroyMethod = ExtractMethodText(
        viewCode,
        "protected override void OnHandleDestroyed(EventArgs e)",
        "private void Station_SelectedIndexChanged");
    var recipeHandler = ExtractMethodText(
        viewCode,
        "private void RecipeCodeSelection_SelectedIndexChanged",
        "private void RealtimePreviewPaintTimer_Tick");
    var onlineReadOnlyMethod = ExtractMethodText(
        viewCode,
        "private void ApplyOnlineStartInputReadOnly(bool editable)",
        "private void BindOfflineEditableRuntimeState");
    var offlineReadOnlyMethod = ExtractMethodText(
        viewCode,
        "private void ApplyOfflineInputReadOnly(bool readOnly)",
        "private void SetWorkOrderInputText");
    var onlineOptionsMethod = ExtractMethodText(
        viewCode,
        "private void BindOnlineProgramNameOptions()",
        "private void SwitchStationFromUi");
    var offlineOptionsMethod = ExtractMethodText(
        viewCode,
        "private void BindOfflineProgramNameOptions()",
        "private void ApplyOfflineProgramNameOption");

    AssertTrue(wireEvents.Contains("selectRecipeCode.SelectedIndexChanged += RecipeCodeSelection_SelectedIndexChanged;", StringComparison.Ordinal), "配方号下拉必须参与开工输入选择事件。");
    AssertTrue(wireEvents.Contains("selectRecipeCode.WheelModifyEnabled = false;", StringComparison.Ordinal), "配方号下拉也应禁用鼠标滚轮误切换。");
    AssertTrue(destroyMethod.Contains("selectRecipeCode.SelectedIndexChanged -= RecipeCodeSelection_SelectedIndexChanged;", StringComparison.Ordinal), "销毁 MonitorView 时必须解绑配方号下拉事件。");
    AssertTrue(onlineReadOnlyMethod.Contains("selectRecipeCode.ReadOnly = fieldReadOnly;", StringComparison.Ordinal), "在线未开工且工单已加载时配方号应允许下拉选择。");
    AssertTrue(offlineReadOnlyMethod.Contains("selectRecipeCode.ReadOnly = readOnly;", StringComparison.Ordinal), "离线未开工时配方号应允许下拉选择。");
    AssertTrue(onlineOptionsMethod.Contains("BindOnlineRecipeCodeOptions(programs", StringComparison.Ordinal), "在线程序列表刷新时必须同步刷新配方号下拉选项。");
    AssertTrue(offlineOptionsMethod.Contains("BindOfflineRecipeCodeOptions(options", StringComparison.Ordinal), "离线程序列表刷新时必须同步刷新配方号下拉选项。");
    AssertTrue(recipeHandler.Contains("ResolveOnlineProgramListItemByRecipeCode", StringComparison.Ordinal), "在线选择配方号后必须反向解析 MES 程序并触发下载预览。");
    AssertTrue(recipeHandler.Contains("ApplyOfflineRecipeCodeSelection", StringComparison.Ordinal), "离线选择配方号后必须反向联动本地程序名称、产品工号和产品型号。");
    AssertTrue(recipeHandler.Contains("DownloadSelectedOnlineProgramAsync(programListItem, CurrentStationNo)", StringComparison.Ordinal), "在线切换配方号应复用程序选择的下载和微调弹窗流程。");
}

static void MonitorViewRecipeDropdownUsesSortedRecipeOptions()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var onlineMethod = ExtractMethodText(
        viewCode,
        "private void BindOnlineRecipeCodeOptions",
        "    #endregion");
    var offlineMethod = ExtractMethodText(
        viewCode,
        "private void BindOfflineRecipeCodeOptions",
        "private void ApplyOfflineProgramNameOption");

    AssertTrue(
        onlineMethod.Contains("OfflineStartInputRules.BuildRecipeCodeOptions", StringComparison.Ordinal),
        "在线配方号下拉必须使用共享规则排序，避免 MES 程序列表顺序导致配方号乱序显示。");
    AssertTrue(
        offlineMethod.Contains("OfflineStartInputRules.BuildRecipeCodeOptions", StringComparison.Ordinal),
        "离线配方号下拉必须使用共享规则排序，避免本地程序库顺序导致配方号乱序显示。");
}

static void MonitorViewUsesPlcRecipeOnlyForOfflineIdleInputs()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var displayResolver = ExtractMethodText(
        viewCode,
        "private string ResolveRecipeCodeForDisplay",
        "private bool HasPendingOnlineProgramSelection");
    var idleSnapshotMethod = ExtractMethodText(
        viewCode,
        "private void ApplyIdleRecipeCodeSnapshot",
        "private void ProductRealtimePreviewService_SnapshotChanged");
    var previewRefreshMethod = ExtractMethodText(
        viewCode,
        "private async Task RefreshSchemePreviewAsync(bool force)",
        "private ProductIdentity? ResolveOnlineProductIdentity");
    var programSelectionMethod = ExtractMethodText(
        viewCode,
        "private void ProgramNameSelection_SelectedIndexChanged",
        "private void RecipeCodeSelection_SelectedIndexChanged");
    var recipeSelectionMethod = ExtractMethodText(
        viewCode,
        "private void RecipeCodeSelection_SelectedIndexChanged",
        "private void RealtimePreviewPaintTimer_Tick");
    var stationSwitchMethod = ExtractMethodText(
        viewCode,
        "private void SwitchStationFromUi",
        "private void SelectStationForOperation");
    var runtimeBindingMethod = ExtractMethodText(
        viewCode,
        "private void BindProductionRuntimeState",
        "private bool IsOfflineInputEditable");

    AssertTrue(displayResolver.Contains("IsOfflineInputEditable(GetCurrentStationState())", StringComparison.Ordinal), "未开工显示 PLC 配方前必须确认当前是离线输入态。");
    AssertTrue(displayResolver.IndexOf("ResolveLocalProgramById(program.Id)", StringComparison.Ordinal) < displayResolver.IndexOf("_plcRecipeReconcileMonitorService.GetCurrent(CurrentStationNo)", StringComparison.Ordinal), "在线已选程序的配方号必须优先于 PLC 当前配方。");
    AssertTrue(idleSnapshotMethod.Contains("if (!IsOfflineInputEditable(state))", StringComparison.Ordinal), "PLC 空闲配方事件不能在在线空闲态覆盖配方号下拉。");
    AssertTrue(idleSnapshotMethod.Contains("ApplyOfflineRecipeCodeSelection(recipeCode)", StringComparison.Ordinal), "离线 PLC 配方变化仍要按配方号反查并联动本地程序信息。");
    AssertTrue(previewRefreshMethod.Contains("if (identity is null && IsOfflineInputEditable(GetCurrentStationState()))", StringComparison.Ordinal), "方案预览只有离线输入态才允许读取 PLC 配方反查产品身份。");
    AssertTrue(programSelectionMethod.Contains("MarkOfflineRecipeSelectionByUser", StringComparison.Ordinal), "离线选择程序名称必须标记为人工配方选择。");
    AssertTrue(recipeSelectionMethod.Contains("MarkOfflineRecipeSelectionByUser", StringComparison.Ordinal), "离线选择配方号必须标记为人工配方选择。");
    AssertTrue(idleSnapshotMethod.Contains("HasOfflineRecipeSelectionByUser", StringComparison.Ordinal), "PLC 配方快照必须识别当前工位的人工配方选择并避免覆盖。");
    AssertTrue(previewRefreshMethod.Contains("ResolveOfflineSelectedRecipeProductIdentity", StringComparison.Ordinal), "离线方案预览必须优先按当前本地配方解析产品工号。");
    AssertTrue(previewRefreshMethod.IndexOf("ResolveOfflineSelectedRecipeProductIdentity", StringComparison.Ordinal) < previewRefreshMethod.IndexOf("ReadPlcRecipeProductIdentityAsync", StringComparison.Ordinal), "本地当前配方的产品工号必须优先于 PLC 配方反查结果。");
    AssertTrue(stationSwitchMethod.Contains("ClearOfflineRecipeSelectionByUser", StringComparison.Ordinal), "切换工位必须清除上一工位和目标工位的人工离线配方标记。");
    AssertTrue(runtimeBindingMethod.Contains("ClearOfflineRecipeSelectionByUser(CurrentStationNo)", StringComparison.Ordinal), "离开离线可编辑态后必须清除人工离线配方标记。");
}

static void MonitorViewReloadsOnlineProgramsAfterProcessChange()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var processHandler = ExtractMethodText(
        viewCode,
        "private async void ProcessSelection_SelectedIndexChanged",
        "private void WorkOrderInput_TextChanged");

    AssertTrue(processHandler.Contains("ClearPendingOnlineProgramSelection();", StringComparison.Ordinal), "在线切换工序时必须清空上一工序的待确认程序选择。");
    AssertTrue(processHandler.Contains("ReloadProgramsAfterProcessSelectionAsync(CurrentStationNo)", StringComparison.Ordinal), "在线切换工序后必须重新拉取程序列表，否则 StateChanged 会把程序下拉刷为空。");
}

static void MonitorViewDefaultsFirstProcessInputsAfterWorkOrderLoad()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var loadMethod = ExtractMethodText(
        viewCode,
        "private async Task<bool> LoadWorkOrderInfoAsync",
        "private void HandleWorkOrderLoadFailure");

    var selectIndex = loadMethod.IndexOf("_weldTaskService.SelectProcess(defaultProcess, stationNo);", StringComparison.Ordinal);
    var bindIndex = loadMethod.IndexOf("ApplySelectedProcessInputs(defaultProcess);", StringComparison.Ordinal);

    AssertTrue(selectIndex >= 0, "获取工单成功后必须默认选择工序列表第一项。");
    AssertTrue(bindIndex > selectIndex, "默认选择第一道工序后必须立即回填工序名称、工序号和生产数量控件。");
}

static void MonitorViewProcessSelectionUsesSharedInputBinder()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var processHandler = ExtractMethodText(
        viewCode,
        "private async void ProcessSelection_SelectedIndexChanged",
        "private async Task<bool> ReloadProgramsAfterProcessSelectionAsync");
    var binder = ExtractMethodText(
        viewCode,
        "private void ApplySelectedProcessInputs(ExpItemData process)",
        "private void ClearProcessSelectionDisplay");

    AssertTrue(processHandler.Contains("ApplySelectedProcessInputs(process);", StringComparison.Ordinal), "手动切换工序也应复用同一个工序详情回填方法。");
    AssertTrue(binder.Contains("selectItemName.Text = GetProcessDisplayName(process);", StringComparison.Ordinal), "工序详情回填必须设置工序名称。");
    AssertTrue(binder.Contains("inputProcessNo.Text = process.ProcessNo ?? string.Empty;", StringComparison.Ordinal), "工序详情回填必须设置工序号。");
    AssertTrue(binder.Contains("inputStartAmount.Text = process.StartAmount.ToString(CultureInfo.InvariantCulture);", StringComparison.Ordinal), "工序详情回填必须设置生产数量。");
}

static void MonitorViewExposesDualWorkOrderToggleBesideWorkOrder()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("chkEnableDualWorkOrder = new AntdUI.Checkbox();", StringComparison.Ordinal), "监控页必须声明启用双工单复选框。");
    AssertTrue(designerCode.Contains("tlpStationInfo.ColumnCount = 3;", StringComparison.Ordinal), "工单号行必须预留双工单复选框列。");
    AssertTrue(designerCode.Contains("tlpStationInfo.Controls.Add(chkEnableDualWorkOrder, 2, 0);", StringComparison.Ordinal), "启用双工单复选框必须与工单号并排显示。");
    AssertTrue(viewCode.Contains("chkEnableDualWorkOrder.CheckedChanged += DualWorkOrder_CheckedChanged;", StringComparison.Ordinal), "监控页必须监听双工单快捷开关。");
    AssertTrue(viewCode.Contains("chkEnableDualWorkOrder.CheckedChanged -= DualWorkOrder_CheckedChanged;", StringComparison.Ordinal), "监控页销毁时必须解绑双工单快捷开关。");
    AssertTrue(viewCode.Contains("chkEnableDualWorkOrder.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDualWorkOrder);", StringComparison.Ordinal), "监控页双工单复选框必须复用现有本地化文本。");
}

static void MonitorViewSavesDualWorkOrderToggleWithOldRules()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var saveMethod = ExtractMethodText(
        viewCode,
        "private void SaveDualWorkOrderMode(bool enableDualWorkOrder)",
        "private void SyncDualWorkOrderToggle");

    AssertTrue(saveMethod.Contains("settings.EnableDualWorkOrder = enableDualWorkOrder;", StringComparison.Ordinal), "监控页切换双工单必须保存 EnableDualWorkOrder。");
    AssertTrue(saveMethod.Contains("settings.EnableDualStation = true;", StringComparison.Ordinal), "勾选双工单时必须沿用旧逻辑自动启用双工位。");
    AssertTrue(saveMethod.Contains("if (!CanSaveDualModeChange(previousSettings, settings))", StringComparison.Ordinal), "双工位/双工单变化必须保留未完工任务保护。");
    AssertTrue(saveMethod.Contains("_settingsService.Save(settings);", StringComparison.Ordinal), "监控页双工单快捷开关必须持久化到系统设置。");
    AssertTrue(viewCode.Contains("private bool HasAnyUnfinishedTask()", StringComparison.Ordinal), "监控页必须检查是否存在未完工任务。");
    AssertTrue(viewCode.Contains("_weldTaskService.GetUnfinishedTask(1) is not null", StringComparison.Ordinal), "未完工任务检查必须覆盖工位1。");
    AssertTrue(viewCode.Contains("_weldTaskService.GetUnfinishedTask(2) is not null", StringComparison.Ordinal), "未完工任务检查必须覆盖工位2。");
}

static void SystemSettingViewNoLongerEditsDualWorkOrder()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);

    AssertFalse(designerCode.Contains("tlpProductConfig.Controls.Add(chkEnableDualWorkOrder", StringComparison.Ordinal), "系统设置页不应再显示启用双工单复选框。");
    AssertFalse(viewCode.Contains("chkEnableDualWorkOrder.CheckedChanged", StringComparison.Ordinal), "系统设置页不应再处理双工单复选框事件。");
    AssertFalse(viewCode.Contains("var enableDualWorkOrder = chkEnableDualWorkOrder.Checked", StringComparison.Ordinal), "系统设置页不应再从双工单复选框读取保存值。");
    AssertTrue(viewCode.Contains("chkEnableDualStation.Checked = settings.EnableDualStation || settings.EnableDualWorkOrder;", StringComparison.Ordinal), "系统设置页仍需在双工单已启用时显示双工位为开启。");
    AssertTrue(viewCode.Contains("settings.EnableDualWorkOrder = enableDualStation && CurrentSettings.EnableDualWorkOrder;", StringComparison.Ordinal), "系统设置保存时应保留既有双工单设置，并在关闭双工位时同步关闭双工单。");
}

static void MonitorViewFinishReportUsesStartOperatorWithoutPrompt()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var onlineHandler = ExtractMethodText(
        viewCode,
        "private async void OnlineReport_Click",
        "private async Task RunStartReportAsync()");
    var finishMethod = ExtractMethodText(
        viewCode,
        "private async Task RunFinishReportAsync()",
        "#region WinForms 生命周期事件");

    AssertTrue(onlineHandler.Contains("activeTask is { IsOfflineCreated: false, EndTime: null }", StringComparison.Ordinal), "在线开工任务未完工时必须继续走在线完工入口。");
    AssertTrue(onlineHandler.Contains("await RunFinishReportAsync();", StringComparison.Ordinal), "在线开工后即使 MES 断线，也应由 FinishAsync 负责本地完工和补传队列。");
    AssertFalse(onlineHandler.Contains("FinishLocalWorkOrderAsync", StringComparison.Ordinal), "在线开工任务不应切换到本地完工入口。");
    AssertFalse(finishMethod.Contains("PromptValidatedOperatorAsync", StringComparison.Ordinal), "在线完工不应再弹员工号输入窗或二次校验员工身份。");
    AssertTrue(finishMethod.Contains("var employeeNumber = activeTask.UserNumber?.Trim() ?? string.Empty;", StringComparison.Ordinal), "在线完工员工号必须直接取开工任务保存的员工号。");
    AssertTrue(finishMethod.Contains("await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty, stationNo);", StringComparison.Ordinal), "在线完工必须把开工员工号传给 FinishAsync。");
}

static void MonitorViewClearsProductIdentityAfterFinishReport()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var finishMethod = ExtractMethodText(
        viewCode,
        "private async Task RunFinishReportAsync()",
        "#region WinForms 生命周期事件");
    var localFinishMethod = ExtractMethodText(
        viewCode,
        "private async Task FinishLocalWorkOrderAsync",
        "private async Task RefreshRecipeCodeFromPlcBeforeFinishAsync");
    var bindMethod = ExtractMethodText(
        viewCode,
        "private void BindProductionRuntimeState()",
        "private bool IsOfflineInputEditable");
    var schemePreviewMethod = ExtractMethodText(
        viewCode,
        "private void ApplySchemePreview(ProductIdentity identity, bool force)",
        "private IEnumerable<WeldParameterRow> BuildSchemePreviewRows");
    var identityResolver = ExtractMethodText(
        viewCode,
        "private ProductIdentity? ResolveDisplayProductIdentity",
        "private void ClearFinishedProductIdentity");
    var clearHelper = ExtractMethodText(
        viewCode,
        "private void ClearFinishedProductIdentity",
        "private void ApplyOnlineStartInputReadOnly");

    var onlineFinishIndex = finishMethod.IndexOf("await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty, stationNo);", StringComparison.Ordinal);
    var onlineClearIndex = finishMethod.IndexOf("ClearFinishedProductIdentity(stationNo);", StringComparison.Ordinal);
    var onlineRefreshIndex = finishMethod.IndexOf("RefreshProductionRuntimeState();", StringComparison.Ordinal);
    var localFinishIndex = localFinishMethod.IndexOf("await _weldTaskService.FinishLocalAsync(", StringComparison.Ordinal);
    var localClearIndex = localFinishMethod.IndexOf("ClearFinishedProductIdentity(stationNo);", StringComparison.Ordinal);
    var localRefreshIndex = localFinishMethod.IndexOf("RefreshProductionRuntimeState();", StringComparison.Ordinal);

    AssertTrue(onlineClearIndex > onlineFinishIndex && onlineClearIndex < onlineRefreshIndex, "在线完工成功后、刷新运行态前必须清除产品身份缓存。");
    AssertTrue(localClearIndex > localFinishIndex && localClearIndex < localRefreshIndex, "本地完工成功后、刷新运行态前必须清除产品身份缓存。");
    AssertTrue(bindMethod.Contains("var currentIdentity = ResolveDisplayProductIdentity(state);", StringComparison.Ordinal), "运行态绑定必须通过统一规则决定是否可用缓存产品身份。");
    AssertTrue(identityResolver.Contains("IsOfflineInputEditable(state)", StringComparison.Ordinal), "离线未开工时仍允许使用 PLC/配方解析出的产品身份。");
    AssertTrue(identityResolver.Contains("state.ActiveTask is not null", StringComparison.Ordinal), "运行中任务仍允许使用产品身份缓存。");
    AssertTrue(identityResolver.Contains("return null;", StringComparison.Ordinal), "在线空闲且无工单时必须禁用旧产品身份缓存。");
    AssertTrue(clearHelper.Contains("_currentProductIdentity = null;", StringComparison.Ordinal), "完工清理必须清空当前产品身份缓存。");
    AssertTrue(clearHelper.Contains("_lastSchemePreviewKey = string.Empty;", StringComparison.Ordinal), "完工清理必须清空方案预览键，避免旧产品预览复用。");
    AssertTrue(schemePreviewMethod.Contains("if (ShouldApplyProductIdentityToInputs(identity))", StringComparison.Ordinal), "方案预览写入产品工号/型号前必须检查当前状态是否允许回填。");
}

static void MonitorViewProductHistoryUsesLatestFirstOrdering()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var bindSnapshotMethod = ExtractMethodText(
        viewCode,
        "private void BindProductHistorySnapshot(ProductHistorySnapshot snapshot, BizWeldTask activeTask)",
        "    /// <summary>\r\n    /// 绑定产品历史行。");

    AssertTrue(bindSnapshotMethod.Contains("ProductHistoryPreviewSortRules.OrderProductsLatestFirst(snapshot.Products)", StringComparison.Ordinal), "MonitorView 绑定产品历史预览时必须按最近产品优先排序。");
    AssertFalse(bindSnapshotMethod.Contains("var rows = snapshot.Products\r\n            .Select(product => ToProductHistoryRow", StringComparison.Ordinal), "MonitorView 不应再直接按服务层原始顺序绑定历史预览。");
}
static void WeldTaskFinishUsesMesStartIdForRetryPayloads()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"), Encoding.UTF8);
    var finishMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BizWeldTask> FinishAsync(string employeeNumber, int actualQty, int qualifiedQty, int failedQty,",
        "public async Task<BizWeldTask> FinishLocalAsync(");
    var buildEndRequest = ExtractMethodText(
        serviceCode,
        "private static ExperimentEndReq BuildEndRequest(",
        "private static ReportExperimentStatusReq BuildStatusRequest");

    AssertTrue(finishMethod.Contains("ExpStartId = task.ExpStartId,", StringComparison.Ordinal), "在线完工即时请求必须使用开工 MES 返回的 ExpStartId。");
    AssertTrue(finishMethod.Contains("EnqueueFinishReportTask(\r\n            task,\r\n            finishRequest", StringComparison.Ordinal), "MES 断线时排队补传的完工任务必须复用同一个 finishRequest。");
    AssertFalse(finishMethod.Contains("LocalExpStartId", StringComparison.Ordinal), "在线完工路径不应把 LocalExpStartId 当成 MES 完工任务 ID。");
    AssertTrue(buildEndRequest.Contains("ExpStartId = task.ExpStartId ?? string.Empty", StringComparison.Ordinal), "离线补传完工请求也必须使用任务中的 MES ExpStartId。");
    AssertFalse(buildEndRequest.Contains("LocalExpStartId", StringComparison.Ordinal), "BuildEndRequest 不应把 LocalExpStartId 写入 MES ExpStartId 字段。");
}

static void WeldTaskRestoreUnfinishedTaskIsIdempotent()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"), Encoding.UTF8);
    var restoreMethod = ExtractMethodText(
        serviceCode,
        "public BizWeldTask? RestoreUnfinishedTask(int stationNo = ProductionConstants.Stations.DefaultStationNo)",
        "public async Task<BasicRes<ServerTimeRes>> SyncServerTimeAsync");

    var alreadyRestoredIndex = restoreMethod.IndexOf("if (alreadyRestored)", StringComparison.Ordinal);
    var returnIndex = alreadyRestoredIndex < 0
        ? -1
        : restoreMethod.IndexOf("return unfinishedTask;", alreadyRestoredIndex, StringComparison.Ordinal);
    var notifyIndex = restoreMethod.IndexOf("NotifyStateChanged();", StringComparison.Ordinal);

    AssertTrue(alreadyRestoredIndex >= 0, "恢复未完工任务时，同一任务已恢复必须有幂等分支。");
    AssertTrue(returnIndex > alreadyRestoredIndex, "同一任务已恢复时必须直接返回当前未完工任务。");
    AssertTrue(returnIndex < notifyIndex, "同一任务已恢复时必须在 NotifyStateChanged 前返回，避免 UI 刷新递归触发 StackOverflow。");
}

static void PermissionCatalogOmitsGetWorkOrderButton()
{
    var permissionCodes = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "PermissionCodes.cs"), Encoding.UTF8);
    var mapperCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Security", "PermissionTextKeyMapper.cs"), Encoding.UTF8);
    var textKeysCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    AssertFalse(PermissionCatalog.All.Any(permission => permission.Code == "button.monitor.get-work-order"), "权限目录不应再暴露已移除的获取工单按钮权限。");
    AssertFalse(permissionCodes.Contains("button.monitor.get-work-order", StringComparison.Ordinal), "权限常量不应再包含获取工单按钮权限码。");
    AssertFalse(mapperCode.Contains("ButtonMonitorGetWorkOrder", StringComparison.Ordinal), "权限文本映射不应再引用获取工单按钮资源键。");
    AssertFalse(textKeysCode.Contains("permission.button.monitor.get_work_order", StringComparison.Ordinal), "TextKeys 不应再声明获取工单按钮资源键。");
    AssertFalse(zhResources.Contains("permission.button.monitor.get_work_order", StringComparison.Ordinal), "中文资源不应再包含获取工单按钮资源键。");
    AssertFalse(enResources.Contains("permission.button.monitor.get_work_order", StringComparison.Ordinal), "英文资源不应再包含获取工单按钮资源键。");
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

static void AllSelectControlsLimitDropdownItems()
{
    var designerPaths = new[]
    {
        new[] { "AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Views", "ProgramManageView.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Views", "AddressManageView.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Forms", "MainForm.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Forms", "LoginForm.Designer.cs" },
        new[] { "AutoWeldSystem.UI", "Forms", "PlcWriteDebugForm.Designer.cs" }
    };

    foreach (var pathParts in designerPaths)
    {
        var designerCode = File.ReadAllText(GetRepoFilePath(pathParts), Encoding.UTF8);
        var selectControlNames = designerCode
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(line => line.Contains("= new AntdUI.Select();", StringComparison.Ordinal))
            .Select(line => line.Trim().Split('=')[0].Trim())
            .ToList();

        AssertTrue(selectControlNames.Count > 0, $"{string.Join(Path.DirectorySeparatorChar, pathParts)} 必须至少包含一个 AntdUI.Select 控件。");
        foreach (var controlName in selectControlNames)
        {
            AssertTrue(
                designerCode.Contains($"{controlName}.MaxCount = 10;", StringComparison.Ordinal),
                $"下拉控件 {controlName} 必须将 MaxCount 设为 10。");
        }
    }

    var addressViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"), Encoding.UTF8);
    var columnFactoryRegion = addressViewCode.Substring(addressViewCode.IndexOf("private AntdUI.ColumnSelect CreateProgramProductNumColumn", StringComparison.Ordinal), addressViewCode.IndexOf("private static AntdUI.ColumnSwitch CreateAddressEnabledColumn", StringComparison.Ordinal) - addressViewCode.IndexOf("private AntdUI.ColumnSelect CreateProgramProductNumColumn", StringComparison.Ordinal));
    AssertFalse(columnFactoryRegion.Contains("DropDownMaxCount = 10", StringComparison.Ordinal), "ColumnSelect 工厂不应直接设置不存在的 DropDownMaxCount 属性。");
    AssertTrue(addressViewCode.Contains("tableAddresses.CellBeginEdit += TableSelect_CellBeginEdit;", StringComparison.Ordinal), "地址表格下拉编辑前必须配置显示数量。 ");
    AssertTrue(addressViewCode.Contains("tableProcess.CellBeginEdit += TableSelect_CellBeginEdit;", StringComparison.Ordinal), "工艺表格下拉编辑前必须配置显示数量。 ");
    AssertTrue(addressViewCode.Contains("cell.DropDownMaxCount = 10;", StringComparison.Ordinal), "表格下拉单元格必须将 DropDownMaxCount 设为 10。");
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

    AssertTrue(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "  MANUAL-1  ",
            lastRequestedWorkId: null,
            queryInProgress: false),
        "手动输入的工单号也应复用自动查询规则，并在查询前修剪空白。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "MANUAL-1",
            lastRequestedWorkId: "manual-1",
            queryInProgress: false),
        "手动输入同一工单号时也不应重复自动查询。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "MANUAL-2",
            lastRequestedWorkId: null,
            queryInProgress: true),
        "同一工位已有自动查询任务时，手动输入不应发起重复请求。");
}

static void WorkOrderInputConfirmationRulesDistinguishDraftsAndPlcValues()
{
    AssertFalse(
        WorkOrderInputConfirmationRules.IsConfirmed("WO-100", string.Empty),
        "人工输入但尚未回车时不能视为已确认工单。");
    AssertTrue(
        WorkOrderInputConfirmationRules.IsConfirmed(" WO-100 ", "WO-100"),
        "回车确认后的工单号应忽略首尾空白并可用于开工。");
    AssertTrue(
        WorkOrderInputConfirmationRules.ShouldApplyPlcSnapshot(
            stationIsIdle: true,
            readSucceeded: true,
            workId: " PLC-200 "),
        "未开工时有效 PLC 快照应立即生效。");
    AssertFalse(
        WorkOrderInputConfirmationRules.ShouldApplyPlcSnapshot(
            stationIsIdle: false,
            readSucceeded: true,
            workId: "PLC-200"),
        "运行任务期间 PLC 快照不得覆盖任务关联工单。");
}

static void MonitorViewConfirmsManualWorkOrdersAndPrioritizesPlcSnapshots()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"), Encoding.UTF8);
    var inputChangedMethod = ExtractMethodText(viewCode, "private void WorkOrderInput_TextChanged", "private void WorkOrderInput_KeyDown");
    var inputKeyDownMethod = ExtractMethodText(viewCode, "private void WorkOrderInput_KeyDown", "private void OperatorInput_KeyDown");
    var plcSnapshotMethod = ExtractMethodText(viewCode, "private void ApplyWorkIdSnapshot", "private void QueueAutoWorkOrderQuery");
    var offlineRequestMethod = ExtractMethodText(viewCode, "private bool TryBuildOfflineStartRequest", "private void BindProcessSelection");
    var serviceMethod = ExtractMethodText(serviceCode, "public async Task<WorkOrderRes?> GetWorkOrderInfoAsync", "public void SelectStation");

    AssertTrue(inputChangedMethod.Contains("ClearConfirmedWorkOrderInput", StringComparison.Ordinal), "人工修改工单号时必须清除已确认状态。");
    AssertFalse(inputChangedMethod.Contains("QueueManualWorkOrderQuery", StringComparison.Ordinal), "人工输入过程中不得自动查询，必须等待回车确认。");
    AssertTrue(inputKeyDownMethod.Contains("ConfirmManualWorkOrderInput", StringComparison.Ordinal), "工单号回车必须进入人工确认入口。");
    AssertTrue(plcSnapshotMethod.Contains("ApplyPlcWorkOrderInput", StringComparison.Ordinal), "PLC 有效快照必须有独立入口，强制覆盖人工草稿。");
    AssertTrue(plcSnapshotMethod.Contains("StartWorkOrderLoadAsync", StringComparison.Ordinal), "在线 PLC 快照必须立即启动最新工单查询。");
    AssertTrue(offlineRequestMethod.Contains("GetConfirmedWorkOrderInput", StringComparison.Ordinal), "离线开工必须使用已确认工单号，而非未确认输入文本。");
    AssertTrue(viewCode.Contains("CancellationTokenSource", StringComparison.Ordinal), "工单查询必须维护取消令牌，避免旧人工查询覆盖 PLC 查询。");
    AssertTrue(serviceMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "服务层在 MES 返回后写入运行态前必须检查请求是否已经取消。");
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

static BasicRes<ServerTimeRes> SuccessServerTime(string currentTime)
{
    return new BasicRes<ServerTimeRes>
    {
        Status = "S",
        Msg = "OK",
        Data = new ServerTimeRes { CurrentTime = currentTime }
    };
}

static void ProgramListFilterReturnsAllWhenDisabled()
{
    var programs = new List<MesProgramListItemData>
    {
        new() { ProgramName = "P-A", ProductNum = "X-1" },
        new() { ProgramName = "P-B", ProductNum = "X-2" }
    };

    var filtered = ProgramListFilterRules.Filter(programs, useProductNumberFilter: false, workOrderProdNum: "X-1");
    AssertEqual(2, filtered.Count, "未开启按产品工号筛选时不应收窄程序列表。");
}

static void ProgramListFilterNarrowsByProductNumberWhenEnabled()
{
    var programs = new List<MesProgramListItemData>
    {
        new() { ProgramName = "P-A", ProductNum = "X-1" },
        new() { ProgramName = "P-B", ProductNum = "x-1" },
        new() { ProgramName = "P-C", ProductNum = "X-2" }
    };

    var filtered = ProgramListFilterRules.Filter(programs, useProductNumberFilter: true, workOrderProdNum: "X-1");
    AssertEqual(2, filtered.Count, "开启筛选后应按工单产品工号忽略大小写收窄。");
    AssertTrue(filtered.All(program => string.Equals(program.ProductNum, "X-1", StringComparison.OrdinalIgnoreCase)), "筛选结果产品工号必须与工单一致。");
}

static void ProgramListFilterReturnsAllWhenWorkOrderProductNumberIsBlank()
{
    var programs = new List<MesProgramListItemData>
    {
        new() { ProgramName = "P-A", ProductNum = "X-1" },
        new() { ProgramName = "P-B", ProductNum = "X-2" }
    };

    var filtered = ProgramListFilterRules.Filter(programs, useProductNumberFilter: true, workOrderProdNum: null);
    AssertEqual(2, filtered.Count, "工单产品工号空白时不应收窄程序列表。");
}

static void ProgramContentReviewRowsApplyModifiedValues()
{
    var rows = new List<ProgramContentReviewRow>
    {
        new() { ItemName = "高度", StandardValue = "12.5", ModifiedValue = "13.0" },
        new() { ItemName = "压力", StandardValue = "20", ModifiedValue = "" },
        new() { ItemName = "", StandardValue = "skip", ModifiedValue = "" }
    };

    var json = ProgramContentJsonRules.MergeReviewRowsToJson(rows);
    using var document = JsonDocument.Parse(json);
    AssertTrue(document.RootElement.GetProperty("高度").GetString() == "13.0", "修改值非空时应覆盖设定值进入 JSON。");
    AssertTrue(document.RootElement.GetProperty("压力").GetString() == "20", "修改值为空时应回退到设定值/标准值。");
    AssertFalse(document.RootElement.TryGetProperty("", out _), "测试项名称为空的行不应进入 JSON。");
}

static void ProgramContentReviewKeepsStandardValueWhenModifiedValueEmpty()
{
    var rows = new List<ProgramContentReviewRow>
    {
        new() { ItemName = "电流", StandardValue = "180", ModifiedValue = "  " }
    };

    var json = ProgramContentJsonRules.MergeReviewRowsToJson(rows);
    using var document = JsonDocument.Parse(json);
    AssertTrue(document.RootElement.GetProperty("电流").GetString() == "180", "空白修改值回退标准值后仍需进入 JSON。");
}

static void ProgramContentReviewRejectsDuplicateItemNames()
{
    var rows = new List<ProgramContentReviewRow>
    {
        new() { ItemName = "高度", StandardValue = "12.5", ModifiedValue = "13.0" },
        new() { ItemName = "高度", StandardValue = "12.5", ModifiedValue = "14.0" }
    };

    var ok = ProgramContentJsonRules.TryMergeReviewRowsToJson(rows, out _, out var errorMessage);
    AssertFalse(ok, "重复测试项名称必须阻止合并。");
    AssertTrue(errorMessage.Contains("重复", StringComparison.Ordinal), "合并失败错误信息应提示重复测试项。");
}

static void LoadProgramsFiltersAvailableProgramsByWorkOrderProductNumber()
{
    var mes = new FakeMesProvider
    {
        WorkOrderInfoResponse = new BasicRes<WorkOrderRes>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = "OK",
            Data = new WorkOrderRes
            {
                SN = "WO-1",
                ProdNum = "X-1",
                ExpItems = [new ExpItemData { ItemId = 1, ProcessNo = "OP10", ItemName = "焊接", StartAmount = 10 }]
            }
        },
        ProgramListResponse = new BasicRes<List<MesProgramListItemData>>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = "OK",
            Data = new List<MesProgramListItemData>
            {
                new() { Id = "1", ProgramName = "P-A", ProductNum = "X-1" },
                new() { Id = "3", ProgramName = "P-C", ProductNum = "X-2" }
            }
        }
    };
    var appSettings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001", UseProductNumberFilter = true }
    };
    var service = CreateWeldTaskService(
        mes,
        new FakeSystemClockService(),
        new FakeOperationLogService(),
        appSettingsService: appSettings);

    var workOrder = service.GetWorkOrderInfoAsync("WO-1").GetAwaiter().GetResult();
    AssertTrue(workOrder is not null, "工单信息应加载成功。");
    var programs = service.LoadProgramsAsync(ProductionConstants.Stations.DefaultStationNo).GetAwaiter().GetResult();
    AssertEqual(1, programs.Count, "开启按产品工号筛选后，AvailablePrograms 只应含匹配工单产品工号的程序。");
    AssertEqual("P-A", programs[0].ProgramName, "筛选后保留的应是产品工号匹配的程序。");
}

static WeldTaskService CreateWeldTaskService(
    FakeMesProvider mesProvider,
    ISystemClockService clockService,
    FakeOperationLogService operationLogService,
    FakeDeviceLifecycleLogService? lifecycleLogService = null,
    FakeAppSettingsService? appSettingsService = null)
{
    return new WeldTaskService(
        null!,
        mesProvider,
        appSettingsService ?? new FakeAppSettingsService(),
        operationLogService,
        new FakeLocalizationService(),
        new FakeUploadTaskService(),
        new FakeProductionReportFileService(),
        lifecycleLogService ?? new FakeDeviceLifecycleLogService(),
        new FakeDeviceStatusService(),
        clockService);
}

static DeviceLifecycleLogCoordinator CreateDeviceLifecycleLogCoordinator(
    FakeDeviceLifecycleLogService lifecycleLogService,
    FakeDeviceStatusService deviceStatusService)
{
    return new DeviceLifecycleLogCoordinator(
        new FakeAppSettingsService { Current = new AppSettings { DeviceId = "D-001" } },
        lifecycleLogService,
        new FakePlcCommunicationService(),
        new FakeMesConnectionMonitor(),
        new FakeCenterTelemetrySyncService(),
        new FakePlcProductionMonitorService(),
        deviceStatusService);
}

static DeviceApiEndpointService CreateDeviceApiEndpointService(
    FakeAppSettingsService? appSettingsService = null,
    FakeDeviceStatusService? deviceStatusService = null,
    FakeOperationLogService? operationLogService = null,
    FakeDeviceLifecycleLogService? lifecycleLogService = null)
{
    return new DeviceApiEndpointService(
        appSettingsService ?? new FakeAppSettingsService(),
        deviceStatusService ?? new FakeDeviceStatusService(),
        operationLogService ?? new FakeOperationLogService(),
        lifecycleLogService ?? new FakeDeviceLifecycleLogService());
}

static MesProvider CreateMesProvider(
    FakeAppSettingsService appSettingsService,
    RecordingHttpMessageHandler handler)
{
    return new MesProvider(
        new HttpClient(handler),
        appSettingsService,
        new FakeLocalizationService(),
        new FakeMesInteractionLogService());
}

static AppSettings BuildCustomMesRouteSettings()
{
    return new AppSettings
    {
        MesBaseUrl = "http://127.0.0.1:7098/",
        MesUserRoute = "mes/user-custom",
        MesWorkOrderRoute = "mes/work-order-custom",
        MesServerTimeRoute = "mes/server-time-custom",
        MesProgramManageRoute = "mes/program-custom",
        MesStartWorkRoute = "mes/start-custom",
        MesWorkStatusRoute = "mes/status-custom",
        MesEndWorkRoute = "mes/end-custom",
        MesReportFileRoute = "mes/report-file-custom",
        MesPostDataRoute = "mes/post-data-custom",
        MesDeviceRoute = "mes/device-custom",
        MesDeviceStatusRoute = "mes/device-status-custom"
    };
}

static string GetRepoFilePath(params string[] segments)
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
        if (File.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    throw new FileNotFoundException($"Cannot locate repository file: {string.Join("/", segments)}");
}

static int CountOccurrences(string text, string value)
{
    var count = 0;
    var index = 0;
    while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += value.Length;
    }

    return count;
}

static void SelectListRulesResolveSelectionByDisplayText()
{
    var displayTexts = new List<string?> { "P-A", "P-B", "P-C" };

    AssertEqual(
        1,
        SelectListRules.ResolveSelectedIndex(displayTexts, "P-B", 1),
        "事件索引与选中文本一致时应直接采信事件索引。");
    AssertEqual(
        1,
        SelectListRules.ResolveSelectedIndex(displayTexts, "P-B", 0),
        "AntdUI 筛选态下拉的事件索引指向筛选后子列表，必须按文本回查完整列表。");
    AssertEqual(
        2,
        SelectListRules.ResolveSelectedIndex(displayTexts, " P-C ", 9),
        "选中文本应修剪后匹配，事件索引越界不应影响文本解析。");
    AssertEqual(
        -1,
        SelectListRules.ResolveSelectedIndex(displayTexts, string.Empty, 0),
        "空文本代表清空选择，应返回 -1。");
    AssertEqual(
        -1,
        SelectListRules.ResolveSelectedIndex(displayTexts, "P-X", 0),
        "文本不在完整列表中时应返回 -1，不得回落到事件索引。");
    AssertEqual(
        -1,
        SelectListRules.ResolveSelectedIndex(new List<string?>(), "P-A", 0),
        "选项列表为空（程序列表重载间隙）时应返回 -1。");
}

static void SelectListRulesDisambiguateDuplicateDisplayTextsByEventIndex()
{
    var displayTexts = new List<string?> { "OP10 焊接", "OP20 检验", "OP10 焊接" };

    AssertEqual(
        2,
        SelectListRules.ResolveSelectedIndex(displayTexts, "OP10 焊接", 2),
        "显示文本重复时应优先采信与事件索引一致的项。");
    AssertEqual(
        0,
        SelectListRules.ResolveSelectedIndex(displayTexts, "OP10 焊接", 1),
        "事件索引与选中文本不符时应回退到首个文本匹配项。");
}

static string ExtractMethodText(string source, string startMarker, string endMarker)
{
    var start = source.IndexOf(startMarker, StringComparison.Ordinal);
    var end = source.IndexOf(endMarker, start < 0 ? 0 : start, StringComparison.Ordinal);

    AssertTrue(start >= 0, $"源码中必须包含 {startMarker}。");
    AssertTrue(end > start, $"源码中必须在 {startMarker} 后包含 {endMarker}。");

    return source[start..end];
}

static void WaitUntil(Func<bool> condition, string message)
{
    var deadline = DateTime.UtcNow.AddSeconds(2);
    while (DateTime.UtcNow < deadline)
    {
        if (condition())
        {
            return;
        }

        Thread.Sleep(10);
    }

    throw new InvalidOperationException(message);
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

static void AssertInvalidOperationMessage(Action action, string expectedMessage, string message)
{
    try
    {
        action();
    }
    catch (InvalidOperationException ex)
    {
        AssertEqual(expectedMessage, ex.Message, message);
        return;
    }

    throw new InvalidOperationException($"{message} Expected={nameof(InvalidOperationException)}, Actual=no exception");
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

sealed class FakeMesProvider : IMesProvider
{
    public List<ReportDeviceStatusReq> DeviceStatusRequests { get; } = new();

    public BasicRes<ServerTimeRes> ServerTimeResponse { get; set; } = new()
    {
        Status = "S",
        Msg = "OK",
        Data = new ServerTimeRes { CurrentTime = "2026-07-01 08:00:00" }
    };

    public BasicRes<object> DeviceStatusResponse { get; set; } = new()
    {
        Status = AppConstants.MesStatus.Success,
        Msg = "操作成功"
    };

    public BasicRes<WorkOrderRes>? WorkOrderInfoResponse { get; set; }

    public BasicRes<List<MesProgramListItemData>> ProgramListResponse { get; set; } = new()
    {
        Status = AppConstants.MesStatus.Success,
        Msg = "操作成功",
        Data = new List<MesProgramListItemData>()
    };

    public Task<BasicRes<ServerTimeRes>> GetServerTimeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(ServerTimeResponse);

    public Task<BasicRes<WorkOrderRes>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default)
        => Task.FromResult(WorkOrderInfoResponse ?? throw new NotSupportedException());

    public Task<BasicRes<UserInfoRes>> GetUserInfoAsync(string userNumber, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> SetDeviceIdAsync(AddDeviceReq addDeviceRequest, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<ServerTimeRes>> TestConnectionAsync(string baseUrl, int timeoutSeconds, bool isWriteLog, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<ProgramDataRes>> AddExpProgramAsync(ProgramDataWriteReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<ProgramDataRes>> UpdateExpProgramAsync(ProgramDataWriteReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<List<MesProgramListItemData>>> GetProgramListAsync(string deviceId, string? productNum = null, CancellationToken cancellationToken = default)
        => Task.FromResult(ProgramListResponse);

    public Task<BasicRes<ProgramDataRes>> DownloadProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> DeleteExpProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> ReportDeviceStatusAsync(ReportDeviceStatusReq requestData, CancellationToken cancellationToken = default)
    {
        DeviceStatusRequests.Add(requestData);
        return Task.FromResult(DeviceStatusResponse);
    }

    public Task<BasicRes<ExperimentStartRes>> StartWorkAsync(ExperimentStartReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> ChangeWorkStatusAsync(ReportExperimentStatusReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> EndWorkAsync(ExperimentEndReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> UploadReportFileAsync(UploadReportFileReq requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> UploadProcessParametersAsync(IReadOnlyList<ProcessParameterUploadItem> requestData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

sealed class FakeSystemClockService : ISystemClockService
{
    public DateTime CurrentTime { get; set; } = new(2026, 7, 1, 8, 0, 0);

    public int SetLocalTimeCallCount { get; private set; }

    public DateTime? LastRequestedTime { get; private set; }

    public SystemClockSyncResult? SetLocalTimeResult { get; set; }

    public DateTime GetLocalTime() => CurrentTime;

    public SystemClockSyncResult SetLocalTime(DateTime serverTime, DateTime localTimeBefore)
    {
        SetLocalTimeCallCount++;
        LastRequestedTime = serverTime;
        return SetLocalTimeResult
            ?? SystemClockSyncResult.ChangedResult(serverTime, localTimeBefore, (serverTime - localTimeBefore).TotalSeconds, "已校时");
    }
}

sealed class FakeAppSettingsService : IAppSettingsService
{
    public event EventHandler<AppSettingsChangedEventArgs>? SettingsChanged;

    public AppSettings Current { get; set; } = new();

    public AppSettings Get() => Current.Clone();

    public AppSettings Save(AppSettings settings)
    {
        var previous = Current.Clone();
        Current = settings.Clone();
        var changedProperties = ResolveChangedProperties(previous, Current);
        SettingsChanged?.Invoke(this, new AppSettingsChangedEventArgs(previous, Current, changedProperties));
        return Current.Clone();
    }

    private static IReadOnlyList<string> ResolveChangedProperties(AppSettings previous, AppSettings current)
    {
        return typeof(AppSettings)
            .GetProperties()
            .Where(property => property.Name is not nameof(AppSettings.Id) and not nameof(AppSettings.UpdatedTime))
            .Where(property => !Equals(property.GetValue(previous), property.GetValue(current)))
            .Select(property => property.Name)
            .ToArray();
    }
}

sealed class FakeOperationLogService : IOperationLogService
{
    public List<(string Action, string Detail, string Level)> Entries { get; } = new();

    public void Write(string action, string detail, string level = "Info")
        => Entries.Add((action, detail, level));

    public IReadOnlyList<SysOperationLog> GetRecent(int take = 200) => Array.Empty<SysOperationLog>();
}

sealed class FakePlcCommunicationService : IPlcCommunicationService
{
    public event EventHandler<PlcConnectionSnapshot>? StatusChanged
    {
        add { }
        remove { }
    }

    public PlcConnectionSnapshot Current { get; } = new(
        PlcConnectionState.Stopped,
        IsConnected: false,
        Endpoint: string.Empty,
        LastConnectedTime: null,
        LastHeartbeatTime: null,
        Message: string.Empty);

    public PlcConnectionSnapshot GetCurrent(int stationNo) => Current with { StationNo = stationNo };

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RestartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<PlcServiceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult<bool>.Fail("Not configured."));

    public Task<PlcServiceResult<short>> ReadInt16Async(string address, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult<short>.Fail("Not configured."));

    public Task<PlcServiceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult<int>.Fail("Not configured."));

    public Task<PlcServiceResult<float>> ReadFloatAsync(string address, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult<float>.Fail("Not configured."));

    public Task<PlcServiceResult<string>> ReadStringAsync(string address, ushort length, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult<string>.Fail("Not configured."));

    public Task<PlcServiceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult.Fail("Not configured."));

    public Task<PlcServiceResult> WriteInt16Async(string address, short value, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult.Fail("Not configured."));

    public Task<PlcServiceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult.Fail("Not configured."));

    public Task<PlcServiceResult> WriteFloatAsync(string address, float value, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult.Fail("Not configured."));

    public Task<PlcServiceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default)
        => Task.FromResult(PlcServiceResult.Fail("Not configured."));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakeMesConnectionMonitor : IMesConnectionMonitor
{
    public event EventHandler<MesConnectionSnapshot>? StatusChanged
    {
        add { }
        remove { }
    }

    public MesConnectionSnapshot Current { get; } = new(
        IsConnected: false,
        LastSuccessTime: null,
        UpdatedTime: default,
        Message: string.Empty);

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakeCenterTelemetrySyncService : ICenterTelemetrySyncService
{
    public event EventHandler<CenterTelemetryConnectionSnapshot>? StatusChanged
    {
        add { }
        remove { }
    }

    public CenterTelemetryConnectionSnapshot Current { get; } = new(
        IsConnected: false,
        UpdatedTime: default,
        Message: string.Empty);

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PushOnceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakePlcProductionMonitorService : IPlcProductionMonitorService
{
    public event EventHandler<PlcProductionSnapshot>? StatusChanged
    {
        add { }
        remove { }
    }

    public PlcProductionSnapshot Current { get; } = new(
        IsSuccess: false,
        DeviceStatusCode: null,
        TotalProduction: 0,
        TargetProduction: null,
        AcceptedQuantity: 0,
        RejectedQuantity: 0,
        UpdatedTime: default,
        Message: string.Empty);

    public PlcProductionSnapshot GetCurrent(int stationNo) => Current with { StationNo = stationNo };

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReloadAddressesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakeMesInteractionLogService : IMesInteractionLogService
{
    public event EventHandler<MesInteractionLogEntry>? LogWritten;

    public List<MesInteractionLogEntry> Entries { get; } = new();

    public void Write(MesInteractionLogEntry entry)
    {
        Entries.Add(entry);
        LogWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<MesInteractionLogEntry> GetByDate(DateTime date, int take = 500) => Entries;

    public string GetLogDirectory() => string.Empty;
}

sealed class FakeLocalizationService : ILocalizationService
{
    public string CurrentLanguage => AppConstants.Languages.Chinese;

    public event EventHandler? LanguageChanged;

    public string GetString(string key) => key;

    public string GetString(string key, params object[] args) => string.Format(key, args);

    public void SetLanguage(string cultureCode) => LanguageChanged?.Invoke(this, EventArgs.Empty);
}

sealed class FakeUploadTaskService : IUploadTaskService
{
    public event EventHandler<UploadTaskStatusChangedEventArgs>? TaskStatusChanged;

    public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false) => Array.Empty<UploadTaskSummary>();

    public IReadOnlyList<UploadTaskSummary> GetProcessParameterRows(bool includeCompleted = false) => Array.Empty<UploadTaskSummary>();

    public UploadTaskSummary? GetById(int id) => null;

    public BizUploadTask EnqueueOrUpdate(BizUploadTask task)
    {
        TaskStatusChanged?.Invoke(this, new UploadTaskStatusChangedEventArgs
        {
            UploadTaskId = task.Id,
            TaskType = task.TaskType,
            Status = task.Status
        });
        return task;
    }

    public Task<UploadTaskSummary?> ExecuteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<UploadTaskSummary?>(null);

    public Task<int> ExecuteAllPendingAsync(string taskType, CancellationToken cancellationToken = default) => Task.FromResult(0);

    public void RequestRetry(int id) { }

    public int RequestRetryAll(string taskType) => 0;

    public void DeleteTask(int id) { }

    public void HideWeldTaskUploadState(int weldTaskId) { }
}

sealed class FakeProductionReportFileService : IProductionReportFileService
{
    public BizProductionReportFile GenerateXlsxReport(BizWeldTask task) => new();
}

sealed class FakeDeviceLifecycleLogService : IDeviceLifecycleLogService
{
    public event EventHandler<DeviceLifecycleLogEntry>? LogWritten;

    public List<DeviceLifecycleLogEntry> Entries { get; } = new();

    public bool ThrowOnWrite { get; set; }

    public void Write(DeviceLifecycleLogEntry entry)
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("设备日志写入失败");
        }

        Entries.Add(entry);
        LogWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<DeviceLifecycleLogEntry> GetByDate(DateTime date, int take = 1000) => Array.Empty<DeviceLifecycleLogEntry>();

    public string GetLogDirectory() => string.Empty;
}

sealed class FakeDeviceStatusService : IDeviceStatusService
{
    public event EventHandler<BizDeviceStatusLog>? StatusChanged;

    public List<BizDeviceStatusLog> Logs { get; } = new();

    public BizDeviceStatusLog CurrentStatus { get; set; } = new();

    public int GetCurrentStatusCallCount { get; private set; }

    public bool? LastReportToMes { get; private set; }

    public bool? LastReportInBackground { get; private set; }

    public BizDeviceStatusLog GetCurrentStatus()
    {
        GetCurrentStatusCallCount++;
        return CurrentStatus;
    }

    public IReadOnlyList<BizDeviceStatusLog> GetLogs(DateTime? from = null, DateTime? to = null, int maxCount = 200) => Array.Empty<BizDeviceStatusLog>();

    public string GetLogDirectory() => string.Empty;

    public Task<BizDeviceStatusLog> ChangeStatusAsync(
        string deviceStatus,
        string? remark = null,
        string source = "Software",
        bool reportToMes = true,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        int? weldTaskId = null,
        string? workOrderId = null,
        DateTime? occurredTime = null,
        bool forceWrite = false,
        bool reportInBackground = false,
        CancellationToken cancellationToken = default)
    {
        LastReportToMes = reportToMes;
        LastReportInBackground = reportInBackground;
        var log = new BizDeviceStatusLog
        {
            DeviceStatus = deviceStatus,
            Remark = remark,
            Source = source,
            StationNo = stationNo,
            WeldTaskId = weldTaskId,
            WorkOrderId = workOrderId,
            OccurredTime = occurredTime ?? DateTime.Now
        };
        Logs.Add(log);
        StatusChanged?.Invoke(this, log);
        return Task.FromResult(log);
    }
}

sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    public List<RecordedHttpRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => string.Join(",", header.Value),
            StringComparer.OrdinalIgnoreCase);

        Requests.Add(new RecordedHttpRequest(
            request.Method.Method,
            request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty,
            request.RequestUri?.Query ?? string.Empty,
            body,
            headers));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Status\":\"S\",\"Msg\":\"成功\",\"Data\":null}", Encoding.UTF8, "application/json")
        };
    }
}

sealed record RecordedHttpRequest(
    string Method,
    string Path,
    string Query,
    string Body,
    IReadOnlyDictionary<string, string> Headers);
