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
using AutoWeldSystem.CenterServer.Configuration;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Services.Center;
using AutoWeldSystem.Services.Mes;
using AutoWeldSystem.Services.Log;
using AutoWeldSystem.Services.Production;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;

var tests = new (string Name, Action Run)[]
{
    ("Scheme detail role headers use centralized defaults", SchemeDetailRoleHeadersUseCentralizedDefaults),
    ("Scheme detail role grid defines localized bound columns", SchemeDetailRoleGridDefinesLocalizedBoundColumns),
    ("Scheme detail role names and monitor fallbacks are centralized", SchemeDetailRoleNamesAndMonitorFallbacksAreCentralized),
    ("Station display names have localized dual-station rules", StationDisplayNamesHaveLocalizedDualStationRules),
    ("Station display names load legacy defaults and collapse hidden row", StationDisplayNamesLoadLegacyDefaultsAndCollapseHiddenRow),
    ("Only configured test item expressions create available roles", OnlyConfiguredExpressionsCreateRoles),
    ("Collection does not imply local save or upload", CollectionDoesNotImplyOutput),
    ("MES-only collected roles stay visible in product history", MesOnlyCollectedRoleStaysVisibleInProductHistory),
    ("Disabled roles block save report and MES outputs", DisabledRoleBlocksEveryOutputChannel),
    ("Report file upload rule requires an enabled report role", ReportFileUploadRuleRequiresEnabledReportRole),
    ("Product cycle snapshots persist PLC product results", ProductCycleSnapshotsPersistPlcProductResults),
    ("Missing point results do not fall back to product results", MissingPointResultDoesNotFallBackToProductResult),
    ("Stored PLC product results drive history without point aggregation", StoredPlcProductResultsDriveHistoryWithoutPointAggregation),
    ("Production report writes customer template for single station", ProductionReportWritesCustomerTemplateForSingleStation),
    ("Production report writes configured dual station and product merges", ProductionReportWritesConfiguredDualStationAndProductMerges),
    ("Production report unions station-specific columns without cross values", ProductionReportUnionsStationSpecificColumnsWithoutCrossValues),
    ("Production and center reports reject conflicting point headers", ProductionAndCenterReportsRejectConflictingPointHeaders),
    ("Production report expands template beyond column J", ProductionReportExpandsTemplateBeyondColumnJ),
    ("Production report end-to-end matrix generates visual artifacts", ProductionReportEndToEndMatrixGeneratesVisualArtifacts),
    ("Production report rules reload latest persisted task", ProductionReportRulesReloadLatestPersistedTask),
    ("Production report rules select latest upload spreadsheet", ProductionReportRulesSelectLatestUploadSpreadsheet),
    ("Production report completion flow persists before final generation", ProductionReportCompletionFlowPersistsBeforeFinalGeneration),
    ("Finish report generation is local while MES upload follows ReportEnable", FinishReportGenerationIsLocalWhileMesUploadFollowsReportEnable),
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
    ("PLC software alarm rules merge raw status and bool signals", PlcSoftwareAlarmRulesMergeRawStatusAndBoolSignals),
    ("PLC alarm station discovery includes alarm-only stations", PlcAlarmStationDiscoveryIncludesAlarmOnlyStations),
    ("PLC alarm rules aggregate bool read results", PlcAlarmRulesAggregateBoolReadResults),
    ("PLC alarm projection keeps bool-only alarms local", PlcAlarmProjectionKeepsBoolOnlyAlarmsLocal),
    ("PLC production monitor reads bool alarms independently", PlcProductionMonitorReadsBoolAlarmsIndependently),
    ("PLC software alarms stay local to monitor view", PlcSoftwareAlarmsStayLocalToMonitorView),
    ("Pre-weld NG is treated as failed product result", PreWeldNgIsTreatedAsFailedProductResult),
    ("Center device key uses DeviceId only", CenterDeviceKeyUsesDeviceIdOnly),
    ("Center client online uses heartbeat freshness", CenterClientOnlineUsesHeartbeatFreshness),
    ("Center offline state keeps PLC status unchanged", CenterOfflineStateKeepsPlcStatusUnchanged),
    ("Center telemetry snapshot carries station runtime data", CenterTelemetrySnapshotCarriesStationRuntimeData),
    ("Center dashboard device totals are calculated from station data", CenterDashboardDeviceTotalsAreCalculatedFromStationData),
    ("Center product report request carries one completed product", CenterProductReportRequestCarriesOneCompletedProduct),
    ("Center forwarding business ids hash the full identity", CenterForwardingBusinessIdsHashFullIdentity),
    ("Center product report columns follow production Excel format", CenterProductReportColumnsFollowProductionExcelFormat),
    ("Center product report columns use forwarded equipment headers", CenterProductReportColumnsUseForwardedEquipmentHeaders),
    ("Center product report request carries production report fields", CenterProductReportRequestCarriesProductionReportFields),
    ("Center dynamic report columns use SaveEnable only", CenterDynamicReportColumnsUseSaveEnableOnly),
    ("Center product request uses PLC result and task timestamps", CenterProductRequestUsesPlcResultAndTaskTimestamps),
    ("Center product request resolves configured station name", CenterProductRequestResolvesConfiguredStationName),
    ("Center report product then finish update keeps detail rows", CenterReportProductThenFinishUpdateKeepsDetailRows),
    ("Center report keeps fixed details without dynamic save fields", CenterReportKeepsFixedDetailsWithoutDynamicSaveFields),
    ("Center report renders single and dual station columns", CenterReportRendersSingleAndDualStationColumns),
    ("Center report replaces duplicate product rows", CenterReportReplacesDuplicateProductRows),
    ("Center report isolates device and work order files", CenterReportIsolatesDeviceAndWorkOrderFiles),
    ("Center report path stays inside root for traversal names", CenterReportPathStaysInsideRootForTraversalNames),
    ("Center report path distinguishes sanitized collisions", CenterReportPathDistinguishesSanitizedCollisions),
    ("Center report keeps final header after late product retry", CenterReportKeepsFinalHeaderAfterLateProductRetry),
    ("Center report preserves corrupt existing workbook", CenterReportPreservesCorruptExistingWorkbook),
    ("Center dashboard skips unrelated corrupt formal workbooks", CenterDashboardSkipsUnrelatedCorruptFormalWorkbooks),
    ("Center report atomic update leaves no temporary files", CenterReportAtomicUpdateLeavesNoTemporaryFiles),
    ("Center report lock preserves file when same report is busy", CenterReportLockPreservesFileWhenSameReportIsBusy),
    ("Center report lock does not block a different report", CenterReportLockDoesNotBlockDifferentReport),
    ("Center report lock preserves concurrent products from different stores", CenterReportLockPreservesConcurrentProductsFromDifferentStores),
    ("Center dashboard ignores legacy temporary workbooks", CenterDashboardIgnoresLegacyTemporaryWorkbooks),
    ("Center report temporary path is not an xlsx file", CenterReportTemporaryPathIsNotXlsx),
    ("Center ingest validates product and finish requests separately", CenterIngestValidatesProductAndFinishRequestsSeparately),
    ("Center ingest accepts product and runs production side effects", CenterIngestAcceptsProductAndRunsProductionSideEffects),
    ("Center ingest accepts finish without points and runs production side effects", CenterIngestAcceptsFinishWithoutPointsAndRunsProductionSideEffects),
    ("Center finish update queues after task persistence", CenterFinishUpdateQueuesAfterTaskPersistence),
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
    ("Device status local log store removes selected log ids", DeviceStatusLocalLogStoreRemovesSelectedLogIds),
    ("Device status report keeps millisecond timestamp after MES upload", DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload),
    ("Device status local log store keeps latest state per log id", DeviceStatusLocalLogStoreKeepsLatestStatePerLogId),
    ("Device status pending source and task reconciliation are wired", DeviceStatusPendingSourceAndTaskReconciliationAreWired),
    ("Device status log deletion refresh is wired across views", DeviceStatusLogDeletionRefreshIsWiredAcrossViews),
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
    ("Device lifecycle coordinator reports Chinese software status remarks", DeviceLifecycleCoordinatorReportsChineseSoftwareStatusRemarks),
    ("Device lifecycle coordinator syncs software status timestamps", DeviceLifecycleCoordinatorSyncsSoftwareStatusTimestamps),
    ("Device lifecycle stop triggers background status upload", DeviceLifecycleStopTriggersBackgroundStatusUpload),
    ("Device lifecycle stop reports status when lifecycle log fails", DeviceLifecycleStopReportsStatusWhenLifecycleLogFails),
    ("Device lifecycle connection logs only when state changes", DeviceLifecycleConnectionLogsOnlyWhenStateChanges),
    ("Device lifecycle alarm logs enter change and recovery", DeviceLifecycleAlarmLogsEnterChangeAndRecovery),
    ("Program name rules extract component code", ProgramNameRulesExtractComponentCode),
    ("Program name rules reject invalid component code", ProgramNameRulesRejectInvalidComponentCode),
    ("Program name rules build and parse optional description", ProgramNameRulesBuildAndParseOptionalDescription),
    ("Program manage download backfills name fields", ProgramManageDownloadBackfillsNameFields),
    ("Offline program dropdown displays program name", OfflineProgramDropdownDisplaysProgramName),
    ("Offline program dropdown includes empty-content program", OfflineProgramDropdownIncludesEmptyContentProgram),
    ("Recipe code options sort numeric ascending", RecipeCodeOptionsSortNumericAscending),
    ("Product history preview sorts latest product first", ProductHistoryPreviewSortsLatestProductFirst),
    ("Offline start request follows inline monitor input", OfflineStartRequestFollowsInlineMonitorInput),
    ("Offline start allows empty part name and drawing number", OfflineStartAllowsEmptyPartNameAndDrawingNumber),
    ("Offline start requires work order and process number", OfflineStartRequiresWorkOrderAndProcessNumber),
    ("Program MES sync ignores local-only fields", ProgramMesSyncIgnoresLocalOnlyFields),
    ("Program MES description changes trigger update", ProgramMesDescriptionChangesTriggerUpdate),
    ("Program MES sync detects remote fields", ProgramMesSyncDetectsRemoteFields),
    ("Program MES save action uses update for remote program content", ProgramMesSaveActionUsesUpdateForRemoteProgramContent),
    ("Program MES current save action separates pending actions", ProgramMesCurrentSaveActionSeparatesPendingActions),
    ("Program MES executable action never creates when MES id exists", ProgramMesExecutableActionNeverCreatesWhenMesIdExists),
    ("Program remark rules default by action", ProgramRemarkRulesDefaultByAction),
    ("Program MES write payload omits recipe code", ProgramMesWritePayloadOmitsRecipeCode),
    ("Program MES create payload clears file fields for empty content", ProgramMesCreatePayloadClearsFileFieldsForEmptyContent),
    ("Program content rules detect configured values", ProgramContentRulesDetectConfiguredValues),
    ("Program manage service clears automatic file for empty content", ProgramManageServiceClearsAutomaticFileForEmptyContent),
    ("Program manage service removes renamed automatic file after write", ProgramManageServiceRemovesRenamedAutomaticFileAfterWrite),
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
    ("System setting view locks device management during unfinished tasks", SystemSettingViewLocksDeviceManagementDuringUnfinishedTasks),
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

static void PlcSoftwareAlarmRulesMergeRawStatusAndBoolSignals()
{
    var boolOnly = PlcSoftwareAlarmRules.Resolve(
        ProductionConstants.PlcDeviceStatuses.Running,
        hasActiveBoolSignal: true,
        ["安全门打开", "安全门打开", "气压低"]);
    AssertTrue(boolOnly.IsActive, "原始设备状态不是 4 时，任一 Bool 报警为 true 仍应触发软件报警。");
    AssertEqual("安全门打开；气压低", boolOnly.Message, "多个 Bool 报警内容应按读取顺序合并并去重。");

    var rawAlarm = PlcSoftwareAlarmRules.Resolve(
        ProductionConstants.PlcDeviceStatuses.Alarm,
        hasActiveBoolSignal: false,
        []);
    AssertTrue(rawAlarm.IsActive, "PLC 原始设备状态为 4 时必须触发软件报警。");
    AssertEqual(PlcSoftwareAlarmRules.GenericAlarmMessage, rawAlarm.Message, "状态 4 未匹配具体 Bool 原因时应使用通用报警提示。");

    var inactive = PlcSoftwareAlarmRules.Resolve(
        ProductionConstants.PlcDeviceStatuses.Running,
        hasActiveBoolSignal: false,
        ["读取失败不应成为报警"]);
    AssertFalse(inactive.IsActive, "没有原始状态 4 或成功置位的 Bool 地址时不应触发软件报警。");
    AssertEqual(string.Empty, inactive.Message, "软件报警未激活时不应保留报警内容。");
}

static void PlcAlarmStationDiscoveryIncludesAlarmOnlyStations()
{
    var alarms = new[]
    {
        new BizPlcAlarmAddress { StationNo = 0, Address = "DB1.0", AlarmContent = "共享报警", Enabled = true },
        new BizPlcAlarmAddress { StationNo = 2, Address = "DB2.0", AlarmContent = "右工位报警", Enabled = true },
        new BizPlcAlarmAddress { StationNo = 3, Address = " ", AlarmContent = "空地址", Enabled = true },
        new BizPlcAlarmAddress { StationNo = 4, Address = "DB4.0", AlarmContent = "禁用报警", Enabled = false }
    };

    var stations = PlcSoftwareAlarmRules.ResolveStationNumbers([1], alarms);
    AssertSequenceEqual([1, 2], stations, "生产地址工位与有效报警专用工位应合并、排序并去重。");

    var alarmOnlyStations = PlcSoftwareAlarmRules.ResolveStationNumbers([], alarms);
    AssertSequenceEqual([2], alarmOnlyStations, "仅存在工位专用报警配置时，该工位仍应参与生产轮询。");

    var productionOnlyStations = PlcSoftwareAlarmRules.ResolveStationNumbers([2], []);
    AssertSequenceEqual([2], productionOnlyStations, "关闭报警读取并传入空报警快照时，只应保留生产地址工位。");

    var sharedOnly = PlcSoftwareAlarmRules.ResolveStationNumbers([], [alarms[0]]);
    AssertSequenceEqual([ProductionConstants.Stations.DefaultStationNo], sharedOnly, "共享报警不应生成工位 0 轮询，且应保留默认工位兜底。");

    var stationTwoAlarms = PlcSoftwareAlarmRules.ResolveAlarmAddressesForStation(alarms, 2);
    AssertSequenceEqual(
        ["DB1.0", "DB2.0"],
        stationTwoAlarms.Select(alarm => alarm.Address).ToArray(),
        "工位报警读取应包含共享地址与当前工位地址，并排除空地址、禁用项和其他工位。");
}

static void PlcAlarmRulesAggregateBoolReadResults()
{
    var aggregation = PlcSoftwareAlarmRules.AggregateAlarmSignals(
        stationNo: 2,
        [
            new PlcAlarmSignalReadResult(0, "DB1.0", "安全门打开", IsSuccess: true, IsActive: true, FailureMessage: string.Empty),
            new PlcAlarmSignalReadResult(2, "DB2.0", "安全门打开", IsSuccess: true, IsActive: true, FailureMessage: string.Empty),
            new PlcAlarmSignalReadResult(2, "DB2.1", "气压低", IsSuccess: true, IsActive: false, FailureMessage: string.Empty),
            new PlcAlarmSignalReadResult(2, "DB2.2", "温度异常", IsSuccess: false, IsActive: false, FailureMessage: "读取超时")
        ]);

    AssertTrue(aggregation.HasActiveSignal, "任一成功读取的 Bool=true 应激活聚合报警。");
    AssertEqual("安全门打开", aggregation.Message, "激活报警内容应去重，false 与失败项不应进入提示。");
    AssertEqual(ProductionConstants.Stations.SharedStationNo, aggregation.ScopeStationNo, "共享报警置位时报警范围应为共享工位。");
    AssertEqual(1, aggregation.Failures.Count, "单个读取失败应保留为日志信息但不阻断其他结果。");
    AssertEqual("DB2.2", aggregation.Failures[0].Address, "读取失败应保留对应 PLC 地址。");

    var inactive = PlcSoftwareAlarmRules.AggregateAlarmSignals(
        stationNo: 2,
        [
            new PlcAlarmSignalReadResult(2, "DB2.0", "未触发", IsSuccess: true, IsActive: false, FailureMessage: string.Empty),
            new PlcAlarmSignalReadResult(2, "DB2.1", "读取失败", IsSuccess: false, IsActive: false, FailureMessage: "断线")
        ]);
    AssertFalse(inactive.HasActiveSignal, "false 与读取失败均不应误触发软件报警。");
    AssertEqual(string.Empty, inactive.Message, "没有置位信号时聚合报警内容应为空。");
    AssertEqual(1, inactive.Failures.Count, "读取失败应继续交给调用方记录业务日志。");
}

static void PlcAlarmProjectionKeepsBoolOnlyAlarmsLocal()
{
    var boolAggregation = new PlcAlarmSignalAggregation(
        HasActiveSignal: true,
        Message: "安全门打开",
        ScopeStationNo: 2,
        Failures: []);
    var boolOnly = PlcSoftwareAlarmRules.ResolveProjection(
        ProductionConstants.PlcDeviceStatuses.Running,
        boolAggregation);
    AssertTrue(boolOnly.IsSoftwareAlarmActive, "Bool-only 信号应触发本机软件报警。");
    AssertEqual("安全门打开", boolOnly.SoftwareAlarmMessage, "Bool-only 软件报警应显示实际报警内容。");
    AssertEqual(string.Empty, boolOnly.ExternalAlarmMessage, "Bool-only 报警不得写入 MES/生命周期/中心共用报警字段。");
    AssertEqual<int?>(null, boolOnly.ExternalAlarmStationNo, "Bool-only 报警不得生成外部报警工位范围。");

    var rawAlarm = PlcSoftwareAlarmRules.ResolveProjection(
        ProductionConstants.PlcDeviceStatuses.Alarm,
        PlcAlarmSignalAggregation.Empty(2));
    AssertTrue(rawAlarm.IsSoftwareAlarmActive, "原始设备状态 4 在没有 Bool 原因时仍应报警。");
    AssertEqual(PlcSoftwareAlarmRules.GenericAlarmMessage, rawAlarm.ExternalAlarmMessage, "原始状态 4 应生成可上报的通用报警内容。");
    AssertEqual<int?>(2, rawAlarm.ExternalAlarmStationNo, "原始状态 4 应保留当前工位范围。");
}

static void PlcProductionMonitorReadsBoolAlarmsIndependently()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "ProductionMonitorService.cs"),
        Encoding.UTF8);
    var pollMethod = ExtractMethodText(
        serviceCode,
        "private async Task PollOnceAsync",
        "private async Task<IReadOnlyList<BizPlcAddress>> GetAddressSnapshotAsync");

    AssertEqual(
        1,
        CountOccurrences(pollMethod, "_settingsService.Get().EnablePlcAlarmReading != false"),
        "每轮生产采集只能读取一次 PLC 报警开关，避免不同工位使用不同设置快照。");
    AssertTrue(
        pollMethod.Contains("IReadOnlyList<BizPlcAlarmAddress> alarmAddresses = alarmReadingEnabled\n            ? _plcAlarmAddressService.GetAll()\n            : [];", StringComparison.Ordinal),
        "关闭 PLC 报警读取时不得访问报警配置服务，且工位发现应接收空报警快照。");
    AssertSourceOrder(
        pollMethod,
        "var alarmReadingEnabled = _settingsService.Get().EnablePlcAlarmReading != false;",
        "_plcAlarmAddressService.GetAll()",
        "必须先读取报警开关，再决定是否加载报警配置。");
    AssertSourceOrder(
        pollMethod,
        "_plcAlarmAddressService.GetAll()",
        "ResolveStationNumbers(addresses, alarmAddresses)",
        "启用时应先加载一次报警配置快照，再据此扩展轮询工位。");
    AssertEqual(
        1,
        CountOccurrences(pollMethod, "_plcAlarmAddressService.GetAll()"),
        "每轮生产采集应只加载一次报警地址快照，供工位发现和各工位读取共同复用。");
    AssertTrue(
        pollMethod.Contains("ResolveStationNumbers(addresses, alarmAddresses)", StringComparison.Ordinal),
        "轮询工位必须同时由生产地址和报警地址配置发现。");
    AssertTrue(
        pollMethod.Contains("var alarmReadingEnabled = _settingsService.Get().EnablePlcAlarmReading != false;", StringComparison.Ordinal),
        "每轮采集应先读取报警开关，明确决定是否扫描 Bool 报警地址。");
    AssertTrue(
        pollMethod.Contains("alarmReadingEnabled\n                ? await ReadActiveAlarmSnapshotAsync(alarmAddresses, stationNo, cancellationToken)", StringComparison.Ordinal),
        "启用报警读取后应独立扫描当前工位及共享报警地址。");
    AssertSourceOrder(
        pollMethod,
        "await ReadActiveAlarmSnapshotAsync(alarmAddresses, stationNo, cancellationToken)",
        "if (deviceStatusAddress is null)",
        "设备状态地址缺失时也必须先完成独立 Bool 报警扫描。");
    AssertSourceOrder(
        pollMethod,
        "await ReadActiveAlarmSnapshotAsync(alarmAddresses, stationNo, cancellationToken)",
        "if (!statusResult.IsSuccess)",
        "设备状态读取失败时也必须先完成独立 Bool 报警扫描。");
    AssertSourceOrder(
        pollMethod,
        "await ReadActiveAlarmSnapshotAsync(alarmAddresses, stationNo, cancellationToken)",
        "if (ProductionConstants.PlcDeviceStatuses.IsReportable(plcStatusCode))",
        "Bool 报警读取不应再依赖原始设备状态先等于 4。");
    AssertTrue(
        pollMethod.Contains("alarmProjection.IsSoftwareAlarmActive", StringComparison.Ordinal)
        && pollMethod.Contains("alarmProjection.SoftwareAlarmMessage", StringComparison.Ordinal)
        && pollMethod.Contains("plcStatusCode,", StringComparison.Ordinal),
        "生产快照必须显式发布软件报警状态和内容，且不能改写原始 DeviceStatusCode。");
    AssertFalse(
        serviceCode.Contains("ReadActiveAlarmMessageAsync", StringComparison.Ordinal),
        "重复且未使用的报警读取方法应移除，避免两套聚合规则再次漂移。");
}

static void PlcSoftwareAlarmsStayLocalToMonitorView()
{
    var monitorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var applyDeviceStatus = ExtractMethodText(
        monitorCode,
        "private void ApplyDeviceStatus",
        "private void ClearDeviceAlarmRuntimeErrorIfCurrent");
    AssertTrue(
        applyDeviceStatus.Contains("snapshot.IsSoftwareAlarmActive", StringComparison.Ordinal)
        && applyDeviceStatus.Contains("snapshot.SoftwareAlarmMessage", StringComparison.Ordinal),
        "MonitorView 应直接使用快照中的软件报警状态和内容。");
    AssertFalse(
        applyDeviceStatus.Contains("EnablePlcAlarmReading", StringComparison.Ordinal),
        "MonitorView 不应再用设置开关屏蔽原始状态 4 触发的软件报警。");

    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetrySyncService.cs"),
        Encoding.UTF8);
    var buildStationSnapshot = ExtractMethodText(
        centerCode,
        "private CenterTelemetryStationSnapshot BuildStationSnapshot",
        "private BizDeviceStatusLog? GetLatestDeviceStatus");
    AssertTrue(
        buildStationSnapshot.Contains("AlarmMessage = FirstNonEmpty(production.AlarmMessage, latestStatus?.Remark)", StringComparison.Ordinal),
        "中心遥测应继续使用原始 PLC 报警内容。");
    AssertFalse(
        buildStationSnapshot.Contains("SoftwareAlarmMessage", StringComparison.Ordinal),
        "Bool-only 软件报警内容不得发送到中心服务器。");

    var lifecycleCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);
    var recordAlarmChange = ExtractMethodText(
        lifecycleCode,
        "private void RecordAlarmChange",
        "private static IEnumerable<int> ResolveStationNumbers");
    AssertFalse(
        recordAlarmChange.Contains("IsSoftwareAlarmActive", StringComparison.Ordinal)
        || recordAlarmChange.Contains("SoftwareAlarmMessage", StringComparison.Ordinal),
        "生命周期报警日志必须继续只跟随原始 PLC 状态 4 的转换。");
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

static void SchemeDetailRoleHeadersUseCentralizedDefaults()
{
    var item = new DimTestItem { ItemId = 1, ItemName = "峰值电流" };
    var detail = new BizSchemeDetail { ActualHeader = "  客户电流  " };

    AssertEqual("峰值电流", SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Actual), "实际值默认表头应直接使用测试项名称。");
    AssertEqual("峰值电流上限", SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Upper), "上限默认表头应保留角色后缀。");
    AssertEqual("峰值电流下限", SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Lower), "下限默认表头应保留角色后缀。");
    AssertEqual("峰值电流结果", SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Result), "结果默认表头应保留角色后缀。");
    AssertEqual("客户电流", SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Actual), "非空已存表头必须优先保留并去除首尾空白。");
}

static void SchemeDetailRoleGridDefinesLocalizedBoundColumns()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);
    var configureMethod = ExtractMethodText(
        viewCode,
        "private void ConfigureSchemeDetailRoleGrid()",
        "    #region 业务信号");

    AssertTrue(configureMethod.Contains("AutoGenerateColumns = false", StringComparison.Ordinal), "方案角色表格必须关闭自动生成列，避免 Source、ItemId 和 Role 泄漏。" );
    foreach (var propertyName in new[] { "ItemName", "RoleName", "HeaderText", "MesFieldName" })
    {
        AssertTrue(configureMethod.Contains($"DataPropertyName = nameof(SchemeDetailRoleTableRow.{propertyName})", StringComparison.Ordinal), $"方案角色表格必须显式绑定 {propertyName} 列。" );
    }
    foreach (var propertyName in new[] { "Enabled", "SaveEnabled", "ReportEnabled", "MesEnabled" })
    {
        AssertTrue(configureMethod.Contains($"AddSchemeDetailRoleCheckColumn(nameof(SchemeDetailRoleTableRow.{propertyName})", StringComparison.Ordinal), $"方案角色表格必须显式绑定 {propertyName} 复选列。" );
    }

    AssertFalse(configureMethod.Contains("DataPropertyName = nameof(SchemeDetailRoleTableRow.Source)", StringComparison.Ordinal), "Source 不得成为可见列。" );
    AssertFalse(configureMethod.Contains("DataPropertyName = nameof(SchemeDetailRoleTableRow.ItemId)", StringComparison.Ordinal), "ItemId 不得成为可见列。" );
    AssertFalse(configureMethod.Contains("DataPropertyName = nameof(SchemeDetailRoleTableRow.Role)", StringComparison.Ordinal), "Role 不得成为可见列。" );
    AssertTrue(configureMethod.Contains("_localizer.GetString(TextKeys.Address.ColumnDetailRole)", StringComparison.Ordinal), "方案角色表格列标题必须走本地化资源。" );
}

static void SchemeDetailRoleNamesAndMonitorFallbacksAreCentralized()
{
    var addressViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"), Encoding.UTF8);
    var languageChangedMethod = ExtractMethodText(addressViewCode, "protected override void OnLanguageChanged()", "private void ConfigureTables()");
    var createRowsMethod = ExtractMethodText(addressViewCode, "private IEnumerable<SchemeDetailRoleTableRow> CreateSchemeDetailRoleRows", "private static BizSchemeDetail CreateEmptySchemeDetail");
    var rowModel = ExtractMethodText(addressViewCode, "private sealed class SchemeDetailRoleTableRow", "private static bool GetEnabled");

    AssertTrue(languageChangedMethod.Contains("BindSchemeDetailRoleRows();", StringComparison.Ordinal), "切换语言后必须重建方案角色行，刷新已绑定的角色名称单元格。");
    AssertTrue(createRowsMethod.Contains("GetLocalizedSchemeDetailRoleName(role)", StringComparison.Ordinal), "创建方案角色行时必须传入本地化角色名称。");
    AssertTrue(rowModel.Contains("string roleName", StringComparison.Ordinal), "方案角色行模型应接收已本地化的角色名称。");
    AssertFalse(rowModel.Contains("=> Role switch", StringComparison.Ordinal), "方案角色行模型不应硬编码中文角色名称。");

    var textKeys = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"), Encoding.UTF8);
    foreach (var keyName in new[] { "DetailRoleActual", "DetailRoleUpper", "DetailRoleLower", "DetailRoleResult" })
    {
        AssertTrue(textKeys.Contains($"public const string {keyName}", StringComparison.Ordinal), $"必须声明角色本地化键 {keyName}。");
    }

    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var previewColumnsMethod = ExtractMethodText(monitorCode, "private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumns(WeldPreviewItem previewItem)", "private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumnsFromScheme");
    AssertTrue(previewColumnsMethod.Contains("SchemeDetailRoleRules.ResolveHeader(previewItem.ActualHeader, previewItem.Name, SchemeDetailValueRole.Actual)", StringComparison.Ordinal), "产品历史 Actual fallback 必须使用集中规则。");
    AssertFalse(previewColumnsMethod.Contains("$\"{previewItem.Name}实际值\"", StringComparison.Ordinal), "产品历史 Actual fallback 不得继续拼接“实际值”后缀。");
}

static void ReportFileUploadRuleRequiresEnabledReportRole()
{
    var ruleType = typeof(SchemeDetailRoleRules).Assembly.GetType(
        "AutoWeldSystem.Core.Production.ReportFileUploadRules");
    AssertTrue(ruleType is not null, "Core 必须提供 MES 报表文件上传纯规则。");
    var shouldUpload = ruleType!.GetMethod(
        "ShouldUploadReportFile",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
    AssertTrue(shouldUpload is not null, "报表文件上传纯规则必须公开 ShouldUploadReportFile。");

    bool Invoke(params BizSchemeDetail[] details)
        => (bool)(shouldUpload!.Invoke(null, [details]) ?? false);

    AssertFalse(Invoke(), "没有方案明细时不得创建 MES ReportFile 任务。");
    AssertFalse(Invoke(new BizSchemeDetail { EnableActual = true, SaveActual = true }), "SaveEnable 独占角色不得触发 MES 报表文件上传。");
    AssertFalse(Invoke(new BizSchemeDetail { EnableActual = true, MesActual = true }), "MesEnable 独占角色不得触发 MES 报表文件上传。");
    AssertFalse(Invoke(new BizSchemeDetail { EnableActual = false, ReportActual = true }), "未启用采集的 ReportEnable 配置不得触发上传。");
    AssertTrue(Invoke(new BizSchemeDetail { EnableActual = true, ReportActual = true }), "任一有效 Enable && ReportEnable 角色必须允许 MES 报表文件上传。");
}

static void MesOnlyCollectedRoleStaysVisibleInProductHistory()
{
    var detail = new BizSchemeDetail
    {
        EnableActual = true,
        SaveActual = false,
        ReportActual = false,
        MesActual = true
    };

    AssertTrue(
        SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Actual),
        "已采集且启用 MES 的角色即使未启用中心保存，也必须在产品历史中显示。");
}

static void DisabledRoleBlocksEveryOutputChannel()
{
    var detail = new BizSchemeDetail
    {
        EnableActual = false,
        SaveActual = true,
        ReportActual = true,
        MesActual = true
    };

    AssertFalse(SchemeDetailRoleRules.ShouldPersistRole(detail, SchemeDetailValueRole.Actual), "未启用采集的角色不得写入历史数据。");
    AssertFalse(SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Actual), "未启用采集的角色不得在产品历史中显示。");
    AssertFalse(SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Actual), "未启用采集的角色不得写入设备端报表。");
    AssertFalse(SchemeDetailRoleRules.ShouldUploadMesRole(detail, SchemeDetailValueRole.Actual), "未启用采集的角色不得上传 MES。");
}

static void ProductCycleSnapshotsPersistPlcProductResults()
{
    var productResultProperty = typeof(BizWeldPointRecord).GetProperty("ProductResult");
    AssertTrue(productResultProperty is not null, "焊点采集记录必须包含独立的 PLC 产品结果字段。");

    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductCycleCollectionService.cs"),
        Encoding.UTF8);
    AssertTrue(
        serviceCode.Contains("ProductResult = header.ProductResult,", StringComparison.Ordinal),
        "采集快照必须把标准化后的 PLC 产品结果写入实体字段。");
    AssertTrue(
        serviceCode.Contains("AddValue(values, \"product_result\", header.ProductResult);", StringComparison.Ordinal),
        "采集快照必须继续把标准化后的 PLC 产品结果写入 RawDataJson，兼容旧数据读取。");
}

static void MissingPointResultDoesNotFallBackToProductResult()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductCycleCollectionService.cs"),
        Encoding.UTF8);
    var readRecordMethod = ExtractMethodText(
        serviceCode,
        "private async Task<BizWeldPointRecord> ReadWeldPointRecordAsync(",
        "private async Task ReadTestItemValuesAsync(");

    AssertTrue(
        readRecordMethod.Contains("var testResult = NormalizeTestResult(touchResultRaw);", StringComparison.Ordinal),
        "焊点/拍照结果必须只标准化 touchResultRaw，不得读取产品结果作为回退。");
    AssertFalse(
        readRecordMethod.Contains("FirstValue(values, \"touch_result_raw\", \"product_result\")", StringComparison.Ordinal),
        "焊点/拍照结果缺失时不得回退到 product_result。");
    AssertEqual(
        ProductionConstants.TestResults.Unknown,
        TestResultRules.Normalize(null),
        "焊点/拍照结果缺失时必须保持 Unknown，即使产品结果为 OK。");
}

static void StoredPlcProductResultsDriveHistoryWithoutPointAggregation()
{
    var productHistoryResolver = typeof(ProductHistoryService).GetMethod(
        "ResolveProductResult",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    AssertTrue(productHistoryResolver is not null, "产品历史服务必须保留独立的产品结果解析入口。");

    var currentRecord = new BizWeldPointRecord
    {
        TestResult = ProductionConstants.TestResults.Ng,
        RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["product_result"] = ProductionConstants.TestResults.PreWeldNg
        })
    };
    SetOptionalStringProperty(currentRecord, "ProductResult", ProductionConstants.TestResults.Ok);
    var currentResult = (string?)productHistoryResolver!.Invoke(
        null,
        [new List<BizWeldPointRecord> { currentRecord }]);
    AssertEqual(
        ProductionConstants.TestResults.Ok,
        currentResult,
        "新记录必须优先读取实体 ProductResult，不得由 TestResult 聚合覆盖。");

    var legacyRecord = new BizWeldPointRecord
    {
        TestResult = ProductionConstants.TestResults.Ok,
        RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["product_result"] = ProductionConstants.TestResults.PreWeldNg
        })
    };
    SetOptionalStringProperty(legacyRecord, "ProductResult", null);
    var legacyResult = (string?)productHistoryResolver.Invoke(
        null,
        [new List<BizWeldPointRecord> { legacyRecord }]);
    AssertEqual(
        ProductionConstants.TestResults.PreWeldNg,
        legacyResult,
        "旧记录实体字段为空时必须从 RawDataJson.product_result 回退读取。");

    var missingRecord = new BizWeldPointRecord
    {
        TestResult = ProductionConstants.TestResults.Ng,
        RawDataJson = "{}"
    };
    SetOptionalStringProperty(missingRecord, "ProductResult", null);
    var missingResult = (string?)productHistoryResolver.Invoke(
        null,
        [new List<BizWeldPointRecord> { missingRecord }]);
    AssertEqual(
        ProductionConstants.TestResults.Unknown,
        missingResult,
        "实体和 JSON 都缺少产品结果时必须返回 Unknown，不得调用焊点结果聚合规则。");

    var dataHistoryResolver = typeof(DataHistoryQueryService).GetMethod(
        "ResolveProductResult",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    AssertTrue(dataHistoryResolver is not null, "数据历史服务必须使用统一的产品结果回退规则。");
    AssertEqual(
        ProductionConstants.TestResults.Ok,
        (string?)dataHistoryResolver!.Invoke(null, [currentRecord]),
        "数据历史必须优先读取实体 ProductResult。");
    AssertEqual(
        ProductionConstants.TestResults.PreWeldNg,
        (string?)dataHistoryResolver.Invoke(null, [legacyRecord]),
        "数据历史必须为旧记录读取 RawDataJson.product_result。");
    AssertEqual(
        ProductionConstants.TestResults.Unknown,
        (string?)dataHistoryResolver.Invoke(null, [missingRecord]),
        "数据历史缺少产品结果时必须返回 Unknown，不得使用 TestResult 推算。");

    var weldParameterProductResult = typeof(AutoWeldSystem.Core.DTOs.DataManagement.DataHistoryWeldParameterRow)
        .GetProperty("ProductResult");
    var collectionProductResult = typeof(AutoWeldSystem.Core.DTOs.DataManagement.DataHistoryCollectionRow)
        .GetProperty("ProductResult");
    AssertTrue(weldParameterProductResult is not null, "焊接参数历史行必须公开独立的 ProductResult。");
    AssertTrue(collectionProductResult is not null, "采集记录历史行必须公开独立的 ProductResult。");

    var dataHistoryCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DataHistoryQueryService.cs"),
        Encoding.UTF8);
    AssertEqual(
        2,
        CountOccurrences(dataHistoryCode, "ProductResult = ResolveProductResult(record),"),
        "焊接参数行和采集记录行都必须填充独立的 ProductResult。");
}

static void ProductionReportWritesCustomerTemplateForSingleStation()
{
    var startTime = new DateTime(2026, 7, 16, 8, 9, 10, DateTimeKind.Local);
    var task = BuildReportTask(startTime, endTime: null);
    var records = new[]
    {
        BuildReportPoint(task.Id, stationNo: 1, productNo: "P001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ng),
        BuildReportPoint(task.Id, stationNo: 1, productNo: "P001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok)
    };
    records[0].ProductResult = null;
    records[1].ProductResult = ProductionConstants.TestResults.Ok;

    var filePath = GenerateReportWorkbook(
        new AppSettings { EnableDualStation = false },
        task,
        records);

    try
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("生产报表");

        AssertEqual($"产品工号：{task.ProductNum}", worksheet.Cell("A1").GetString(), "产品工号必须写入模板 A1:C1 合并单元格。");
        AssertEqual($"图号：{task.DrawingNo}", worksheet.Cell("D1").GetString(), "图号必须写入模板 D1:F1 合并单元格。");
        AssertEqual($"批次：{task.Batch}", worksheet.Cell("G1").GetString(), "批次必须写入模板 G1:H1 合并单元格。");
        AssertEqual($"流转卡号：{task.SN}", worksheet.Cell("I1").GetString(), "流转卡号必须写入模板 I1:J1 合并单元格。");
        AssertEqual($"部件规格：{task.Spec}", worksheet.Cell("A3").GetString(), "部件规格必须写入模板 A3:C3 合并单元格。");
        AssertEqual($"型号：{task.ProductModel}", worksheet.Cell("D3").GetString(), "型号必须写入模板 D3:F3 合并单元格。");
        AssertEqual($"工序：{task.ProcessNo}", worksheet.Cell("G3").GetString(), "工序必须写入模板 G3:J3 合并单元格。");
        AssertEqual($"生产数量：{task.StartAmount}", worksheet.Cell("A5").GetString(), "生产数量必须只取 StartAmount。");
        AssertEqual($"合格数量：{task.QualifiedQty}", worksheet.Cell("D5").GetString(), "合格数量必须取 QualifiedQty。");
        AssertEqual("备注：", worksheet.Cell("G5").GetString(), "备注合并单元格必须只保留标签，不得写入业务值。");
        AssertEqual($"开始时间：{startTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("A7").GetString(), "开始时间必须来自持久化 StartTime 并使用模板格式。");
        AssertEqual("结束时间：", worksheet.Cell("D7").GetString(), "未完工任务的结束时间必须只保留标签。");
        AssertEqual($"操作人员：{task.UserNumber}", worksheet.Cell("G7").GetString(), "操作人员必须只取开工任务 UserNumber。");

        var detailHeaders = ReadHeaderRow(worksheet, rowNumber: 9);
        AssertSequenceEqual(
            new[] { "产品编号", "拍照编号", "拍照结果", "峰值电流", "产品结果" },
            detailHeaders,
            "单工位报表必须完全省略工位列，并保留固定公共列与 ReportEnable 动态列。");
        AssertFalse(detailHeaders.Contains("峰值电流上限"), "仅 SaveEnable 的动态角色不得进入设备报表。");
        AssertFalse(detailHeaders.Contains("峰值电流下限"), "仅 MesEnable 的动态角色不得进入设备报表。");
        foreach (var mergedRange in new[]
        {
            "A1:C1", "D1:F1", "G1:H1", "I1:J1",
            "A3:C3", "D3:F3", "G3:J3",
            "A5:C5", "D5:F5", "G5:J5",
            "A7:C7", "D7:F7", "G7:J7"
        })
        {
            AssertMerged(worksheet, mergedRange, "公共表头必须匹配客户模板合并范围。");
        }
        AssertTrue(
            worksheet.RangeUsed(XLCellsUsedOptions.All)!.RangeAddress.LastAddress.ColumnNumber <= 10,
            "固定公共字段未超过十列时不得扩展到 K 列以后。");
        AssertFalse(worksheet.Cell("A1").Style.Alignment.WrapText, "客户模板中文标签必须保持单行显示。");
        AssertTrue(worksheet.Cell("I1").Style.Alignment.ShrinkToFit, "公共表头必须自动缩小字体，避免长流转卡号在固定列宽内截断。");
        var expectedWidths = new[] { 5.8867d, 10.2188d, 10.4414d, 10.7773d, 9.8867d, 9d, 11d, 11d, 9.4414d, 4d };
        for (var columnIndex = 1; columnIndex <= expectedWidths.Length; columnIndex++)
        {
            AssertNearlyEqual(expectedWidths[columnIndex - 1], worksheet.Column(columnIndex).Width, 0.02d, $"第 {columnIndex} 列宽必须匹配客户模板。");
        }
        AssertTrue(worksheet.Cell("A9").Style.Font.Bold, "明细表头必须保持客户模板的粗体层级。");
        AssertEqual(XLBorderStyleValues.Thin, worksheet.Cell("A9").Style.Border.TopBorder, "明细表头必须保留细边框。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E10").GetString(), "产品结果必须读取 PLC ProductResult，不得聚合焊点结果。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("C10").GetString(), "点/拍照结果必须直接读取 TestResult。");
        AssertMerged(worksheet, "A10:A11", "同一产品的产品编号必须合并。");
        AssertMerged(worksheet, "E10:E11", "同一产品的产品结果必须合并。");
    }
    finally
    {
        DeleteReportFixture(filePath);
    }
}

static void ProductionReportWritesConfiguredDualStationAndProductMerges()
{
    var startTime = new DateTime(2026, 7, 16, 8, 9, 10, DateTimeKind.Local);
    var endTime = new DateTime(2026, 7, 16, 10, 11, 12, DateTimeKind.Local);
    var task = BuildReportTask(startTime, endTime);
    var records = new[]
    {
        BuildReportPoint(task.Id, stationNo: 1, productNo: "P001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(task.Id, stationNo: 1, productNo: "P001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(task.Id, stationNo: 2, productNo: "P001", sequenceNo: 3, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(task.Id, stationNo: 2, productNo: "P001", sequenceNo: 4, pointResult: ProductionConstants.TestResults.Ok)
    };
    foreach (var record in records)
    {
        record.ProductResult = record.StationNo == 1
            ? ProductionConstants.TestResults.Ng
            : ProductionConstants.TestResults.Ok;
    }

    var filePath = GenerateReportWorkbook(
        new AppSettings
        {
            EnableDualStation = true,
            Station1DisplayName = "  左工位  ",
            Station2DisplayName = "右工位"
        },
        task,
        records);

    try
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("生产报表");

        AssertEqual("工位", worksheet.Cell("A9").GetString(), "双工位报表必须生成工位列。");
        AssertEqual("左工位", worksheet.Cell("A10").GetString(), "工位 1 必须使用规范化后的配置名称。");
        AssertEqual("右工位", worksheet.Cell("A12").GetString(), "工位 2 必须使用规范化后的配置名称。");
        AssertEqual($"结束时间：{endTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D7").GetString(), "结束时间必须与持久化 EndTime 一致并使用模板格式。");
        AssertMerged(worksheet, "A10:A11", "工位 1 公共字段必须按工位和产品编号合并。");
        AssertMerged(worksheet, "B10:B11", "工位 1 产品编号必须合并。");
        AssertMerged(worksheet, "F10:F11", "工位 1 产品结果必须合并。");
        AssertMerged(worksheet, "A12:A13", "工位 2 公共字段必须形成独立合并范围。");
        AssertMerged(worksheet, "B12:B13", "相同产品编号跨工位不得合并成一个范围。");
        AssertMerged(worksheet, "F12:F13", "不同工位的产品结果必须独立合并。");
    }
    finally
    {
        DeleteReportFixture(filePath);
    }
}

static void ProductionReportUnionsStationSpecificColumnsWithoutCrossValues()
{
    var task = BuildReportTask(new DateTime(2026, 7, 17, 8, 0, 0), endTime: null);
    task.SN = "FLOW-STATION-UNION";
    var records = new[]
    {
        BuildReportPoint(task.Id, stationNo: 1, productNo: "LEFT-001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(task.Id, stationNo: 2, productNo: "RIGHT-001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok)
    };
    records[0].RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["max_electric"] = "1.11",
        ["displacement"] = "错误串位值"
    });
    records[1].RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["max_electric"] = "错误串位值",
        ["displacement"] = "2.22"
    });

    var stationDefinitions = new[]
    {
        (
            StationNo: 1,
            Config: new BizProductProcessConfig
            {
                StationNo = 1,
                SchemeId = "LEFT-SCHEME",
                PointNoHeader = "拍照编号",
                PointResultHeader = "拍照结果"
            },
            Item: new DimTestItem { ItemId = 1, ItemName = "峰值电流", ActualExpression = "0:F-0" },
            Detail: new BizSchemeDetail
            {
                SchemeId = "LEFT-SCHEME",
                ItemId = 1,
                DetailId = 1,
                EnableActual = true,
                ReportActual = true,
                ActualHeader = "左工位电流"
            }),
        (
            StationNo: 2,
            Config: new BizProductProcessConfig
            {
                StationNo = 2,
                SchemeId = "RIGHT-SCHEME",
                PointNoHeader = "拍照编号",
                PointResultHeader = "拍照结果"
            },
            Item: new DimTestItem { ItemId = 2, ItemName = "位移", ActualExpression = "0:F-0" },
            Detail: new BizSchemeDetail
            {
                SchemeId = "RIGHT-SCHEME",
                ItemId = 2,
                DetailId = 2,
                EnableActual = true,
                ReportActual = true,
                ActualHeader = "右工位位移"
            })
    };
    var reportPath = GenerateStationSpecificReportWorkbook(
        new AppSettings { EnableDualStation = true, Station1DisplayName = "左工位", Station2DisplayName = "右工位" },
        task,
        records,
        stationDefinitions);
    var centerDirectory = CreateCenterReportFixtureDirectory();

    try
    {
        using (var workbook = new XLWorkbook(reportPath))
        {
            var worksheet = workbook.Worksheet(CenterProductReportFormat.WorksheetName);
            AssertSequenceEqual(
                new[] { "工位", "产品编号", "拍照编号", "拍照结果", "左工位电流", "右工位位移", "产品结果" },
                ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
                "设备端双工位同任务必须按稳定顺序合并两套 ReportEnable 动态列。");
            AssertEqual("1.11", worksheet.Cell("E10").GetString(), "工位 1 必须读取本工位适用配置的动态值。");
            AssertEqual(string.Empty, worksheet.Cell("F10").GetString(), "工位 1 不得读取工位 2 专属动态值。");
            AssertEqual(string.Empty, worksheet.Cell("E11").GetString(), "工位 2 不得读取工位 1 专属动态值。");
            AssertEqual("2.22", worksheet.Cell("F11").GetString(), "工位 2 必须读取本工位适用配置的动态值。");
        }

        var leftRequest = BuildCenterWorkbookRequest(
            "DEVICE-UNION", task.SN, task.StartTime, null, 0, true, 1, "左工位", "LEFT-001", false, false, 1,
            [new CenterProductReportColumnDto { Key = "max_electric", Title = "左工位电流", MergeByProduct = false }]);
        leftRequest.Points[0].RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["max_electric"] = "1.11",
            ["displacement"] = "错误串位值"
        });
        var rightRequest = BuildCenterWorkbookRequest(
            "DEVICE-UNION", task.SN, task.StartTime, null, 0, true, 2, "右工位", "RIGHT-001", false, false, 1,
            [new CenterProductReportColumnDto { Key = "displacement", Title = "右工位位移", MergeByProduct = false }]);
        rightRequest.Points[0].RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["max_electric"] = "错误串位值",
            ["displacement"] = "2.22"
        });

        var centerStore = new CenterProductReportFileStore();
        centerStore.Upsert(centerDirectory, leftRequest);
        var centerPath = centerStore.Upsert(centerDirectory, rightRequest);
        using var centerWorkbook = new XLWorkbook(centerPath);
        var centerWorksheet = centerWorkbook.Worksheet(CenterProductReportFormat.WorksheetName);
        AssertSequenceEqual(
            new[] { "工位", "产品编号", "拍照编号", "拍照结果", "左工位电流", "右工位位移", "产品结果" },
            ReadHeaderRow(centerWorksheet, CenterProductReportFormat.DetailHeaderRow),
            "中心端必须保持与设备端一致的双工位动态列并集语义。");
        AssertEqual("1.11", centerWorksheet.Cell("E10").GetString(), "中心工位 1 不得串入工位 2 值。");
        AssertEqual(string.Empty, centerWorksheet.Cell("F10").GetString(), "中心工位 1 的工位 2 专属列必须为空。");
        AssertEqual(string.Empty, centerWorksheet.Cell("E11").GetString(), "中心工位 2 的工位 1 专属列必须为空。");
        AssertEqual("2.22", centerWorksheet.Cell("F11").GetString(), "中心工位 2 必须写入本工位专属值。");
    }
    finally
    {
        DeleteReportFixture(reportPath);
        DeleteDirectoryIfExists(centerDirectory);
    }
}

static void ProductionAndCenterReportsRejectConflictingPointHeaders()
{
    var task = BuildReportTask(new DateTime(2026, 7, 17, 8, 0, 0), endTime: null);
    task.SN = "FLOW-HEADER-CONFLICT";
    var records = new[]
    {
        BuildReportPoint(task.Id, stationNo: 1, productNo: "LEFT-001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(task.Id, stationNo: 2, productNo: "RIGHT-001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok)
    };
    var conflictingDefinitions = new[]
    {
        (
            StationNo: 1,
            Config: new BizProductProcessConfig { StationNo = 1, SchemeId = "S1", PointNoHeader = "拍照编号", PointResultHeader = "拍照结果" },
            Item: new DimTestItem { ItemId = 1, ItemName = "峰值电流", ActualExpression = "0:F-0" },
            Detail: new BizSchemeDetail { SchemeId = "S1", ItemId = 1, EnableActual = true, ReportActual = true }),
        (
            StationNo: 2,
            Config: new BizProductProcessConfig { StationNo = 2, SchemeId = "S2", PointNoHeader = "焊点编号", PointResultHeader = "焊点结果" },
            Item: new DimTestItem { ItemId = 2, ItemName = "位移", ActualExpression = "0:F-0" },
            Detail: new BizSchemeDetail { SchemeId = "S2", ItemId = 2, EnableActual = true, ReportActual = true })
    };

    var deviceRejected = false;
    try
    {
        GenerateStationSpecificReportWorkbook(
            new AppSettings { EnableDualStation = true },
            task,
            records,
            conflictingDefinitions);
    }
    catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
    {
        deviceRejected = true;
    }

    AssertTrue(deviceRejected, "设备端同一任务工位标题冲突时必须明确拒绝，不能用工位 1 标题解释工位 2。");

    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var firstRequest = BuildCenterWorkbookRequest(
            "DEVICE-HEADER", task.SN, task.StartTime, null, 0, true, 1, "左工位", "LEFT-001", false, false, 1,
            pointNoHeader: "拍照编号",
            pointResultHeader: "拍照结果");
        var secondRequest = BuildCenterWorkbookRequest(
            "DEVICE-HEADER", task.SN, task.StartTime, null, 0, true, 2, "右工位", "RIGHT-001", false, false, 1,
            pointNoHeader: "焊点编号",
            pointResultHeader: "焊点结果");
        var store = new CenterProductReportFileStore();
        var reportPath = store.Upsert(outputDirectory, firstRequest);
        var originalBytes = File.ReadAllBytes(reportPath);

        var centerRejected = false;
        try
        {
            store.Upsert(outputDirectory, secondRequest);
        }
        catch (InvalidOperationException)
        {
            centerRejected = true;
        }

        AssertTrue(centerRejected, "中心端同一任务固定采集点标题冲突时必须明确拒绝。");
        AssertSequenceEqual(originalBytes, File.ReadAllBytes(reportPath), "中心端标题冲突失败时不得改写已有正式报表。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void ProductionReportExpandsTemplateBeyondColumnJ()
{
    var task = BuildReportTask(new DateTime(2026, 7, 16, 8, 9, 10, DateTimeKind.Local), endTime: null);
    var records = new[]
    {
        BuildReportPoint(task.Id, stationNo: 1, productNo: "P001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok)
    };
    records[0].ProductResult = ProductionConstants.TestResults.Ok;
    var filePath = GenerateReportWorkbook(
        new AppSettings { EnableDualStation = false },
        task,
        records,
        extraDynamicColumnCount: 6);

    try
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("生产报表");
        var detailHeaders = ReadHeaderRow(worksheet, rowNumber: 9);

        AssertEqual(11, detailHeaders.Length, "六个扩展动态列应让单工位明细超过 J 并到达 K 列。");
        AssertMerged(worksheet, "I1:K1", "动态列超过 J 时首行最后一组公共字段必须扩展到 K。");
        AssertMerged(worksheet, "G3:K3", "动态列超过 J 时第三行最后一组公共字段必须扩展到 K。");
        AssertMerged(worksheet, "G5:K5", "动态列超过 J 时第五行最后一组公共字段必须扩展到 K。");
        AssertMerged(worksheet, "G7:K7", "动态列超过 J 时第七行最后一组公共字段必须扩展到 K。");
        AssertTrue(worksheet.Column(11).Width >= 12d, "K 及以后动态列必须保留可读的最小宽度。");
    }
    finally
    {
        DeleteReportFixture(filePath);
    }
}

static void ProductionReportEndToEndMatrixGeneratesVisualArtifacts()
{
    // 常规回归始终在唯一临时目录运行，避免污染工作区或覆盖正在被人工检查的最终样例。
    var workingDirectory = Path.Combine(Path.GetTempPath(), "AutoWeldSystem.Tests", "Task5", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workingDirectory);

    try
    {
        VerifyProductionReportEndToEndMatrix(workingDirectory);
    }
    finally
    {
        // 任何断言、工作簿写入或 artifact 发布失败都必须清理本次唯一临时目录。
        DeleteDirectoryIfExists(workingDirectory);
    }
}

/// <summary>
/// 生成并验证任务 5 的三份跨层真实 XLSX；临时目录生命周期由外层测试统一管理。
/// </summary>
static void VerifyProductionReportEndToEndMatrix(string workingDirectory)
{
    var singleSpotPath = Path.Combine(workingDirectory, "device-single-station-spot-welding.xlsx");
    var dualInspectionPath = Path.Combine(workingDirectory, "device-dual-station-inspection.xlsx");
    var centerCompletedPath = Path.Combine(workingDirectory, "center-server-completed.xlsx");

    var startTime = new DateTime(2026, 7, 17, 8, 9, 10, DateTimeKind.Local);
    var singleTask = BuildReportTask(startTime, endTime: null);
    singleTask.SN = "FLOW-TASK5-SPOT";
    var singleRecords = new[]
    {
        BuildReportPoint(singleTask.Id, stationNo: 1, productNo: "P-SPOT-001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ng),
        BuildReportPoint(singleTask.Id, stationNo: 1, productNo: "P-SPOT-001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok)
    };
    foreach (var record in singleRecords)
    {
        record.ProductResult = ProductionConstants.TestResults.Ok;
    }

    GenerateReportWorkbook(
        new AppSettings { EnableDualStation = false },
        singleTask,
        singleRecords,
        pointNoHeader: "焊点编号",
        pointResultHeader: "焊点结果",
        outputFilePath: singleSpotPath);

    using (var workbook = new XLWorkbook(singleSpotPath))
    {
        AssertSequenceEqual(new[] { "生产报表" }, workbook.Worksheets.Select(sheet => sheet.Name).ToArray(), "设备端单工位样例只能包含生产报表工作表。");
        var worksheet = workbook.Worksheet("生产报表");
        AssertTemplateHeaderMerges(worksheet);
        AssertSequenceEqual(
            new[] { "产品编号", "焊点编号", "焊点结果", "峰值电流", "产品结果" },
            ReadHeaderRow(worksheet, rowNumber: 9),
            "单工位点焊样例必须省略工位列，并只包含 ReportEnable 动态列。");
        AssertFalse(ReadHeaderRow(worksheet, rowNumber: 9).Contains("峰值电流上限"), "设备端 SaveEnable 独占列不得进入报表。");
        AssertFalse(ReadHeaderRow(worksheet, rowNumber: 9).Contains("峰值电流下限"), "设备端 MesEnable 独占列不得进入报表。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("C10").GetString(), "点焊结果必须直接读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("D10").GetString(), "设备端 ReportEnable 动态值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E10").GetString(), "点焊产品结果必须直接读取 PLC ProductResult。");
        AssertEqual("结束时间：", worksheet.Cell("D7").GetString(), "未完工设备任务的 EndTime 必须为空。");
    }

    var finishTime = new DateTime(2026, 7, 17, 10, 11, 12, DateTimeKind.Local);
    var dualTask = BuildReportTask(startTime, finishTime);
    dualTask.SN = "FLOW-TASK5-INSPECTION";
    var dualRecords = new[]
    {
        BuildReportPoint(dualTask.Id, stationNo: 1, productNo: "P-INSPECT-001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(dualTask.Id, stationNo: 1, productNo: "P-INSPECT-001", sequenceNo: 2, pointResult: ProductionConstants.TestResults.Ok),
        BuildReportPoint(dualTask.Id, stationNo: 2, productNo: "P-INSPECT-001", sequenceNo: 3, pointResult: ProductionConstants.TestResults.Ng),
        BuildReportPoint(dualTask.Id, stationNo: 2, productNo: "P-INSPECT-001", sequenceNo: 4, pointResult: ProductionConstants.TestResults.Ok)
    };
    foreach (var record in dualRecords)
    {
        record.ProductResult = record.StationNo == 1
            ? ProductionConstants.TestResults.Ng
            : ProductionConstants.TestResults.Ok;
    }

    GenerateReportWorkbook(
        new AppSettings
        {
            EnableDualStation = true,
            Station1DisplayName = "左工位",
            Station2DisplayName = "右工位"
        },
        dualTask,
        dualRecords,
        pointNoHeader: "拍照编号",
        pointResultHeader: "拍照结果",
        outputFilePath: dualInspectionPath);

    using (var workbook = new XLWorkbook(dualInspectionPath))
    {
        var worksheet = workbook.Worksheet("生产报表");
        AssertTemplateHeaderMerges(worksheet);
        AssertSequenceEqual(
            new[] { "工位", "产品编号", "拍照编号", "拍照结果", "峰值电流", "产品结果" },
            ReadHeaderRow(worksheet, rowNumber: 9),
            "双工位检测样例必须包含工位、拍照标题和 ReportEnable 动态列。");
        AssertEqual("左工位", worksheet.Cell("A10").GetString(), "同一任务的工位 1 必须进入双工位报表。");
        AssertEqual("右工位", worksheet.Cell("A12").GetString(), "同一任务的工位 2 必须进入双工位报表。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("D10").GetString(), "双工位点结果必须读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("E10").GetString(), "双工位 ReportEnable 动态值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("F10").GetString(), "工位 1 产品结果必须读取 PLC ProductResult。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("F12").GetString(), "工位 2 产品结果必须读取 PLC ProductResult。");
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D7").GetString(), "已完工设备任务必须精确使用持久化 EndTime。");
    }

    var centerOutputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var dynamicColumns = BuildCenterDynamicReportColumns(
            new BizSchemeDetail
            {
                EnableActual = true,
                SaveActual = true,
                ActualHeader = "峰值电流保存值",
                EnableUpper = true,
                ReportUpper = true,
                UpperHeader = "峰值电流报表上限",
                EnableLower = true,
                MesLower = true,
                LowerHeader = "峰值电流 MES 下限",
                EnableResult = true,
                SaveResult = true,
                ResultHeader = "峰值电流保存结果"
            },
            new DimTestItem
            {
                ItemId = 1,
                ItemName = "峰值电流",
                ActualExpression = "0:F-0",
                UpperExpression = "0:F-4",
                LowerExpression = "0:F-8",
                ResultExpression = "0:W-12"
            });
        AssertSequenceEqual(
            new[] { "峰值电流保存值", "峰值电流保存结果" },
            dynamicColumns.Select(column => column.Title).ToArray(),
            "中心样例的动态列必须只来自 SaveEnable，ReportEnable/MesEnable 独占列不得透传。");

        var productRequest = BuildCenterWorkbookRequest(
            "DEVICE-TASK5",
            "FLOW-TASK5-CENTER",
            startTime,
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P-CENTER-001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 2,
            dynamicColumns,
            pointNoHeader: "拍照编号",
            pointResultHeader: "拍照结果");
        productRequest.ProductResult = ProductionConstants.TestResults.Ng;
        productRequest.Points[0].TestResult = ProductionConstants.TestResults.Ok;
        var reportPath = WriteCenterReportWorkbook(centerOutputDirectory, productRequest);

        using (var unfinishedWorkbook = new XLWorkbook(reportPath))
        {
            AssertEqual("结束时间：", unfinishedWorkbook.Worksheet("生产报表").Cell("D7").GetString(), "中心产品请求生成的未完工报表 EndTime 必须为空。");
        }

        var finishRequest = BuildCenterWorkbookRequest(
            "DEVICE-TASK5",
            "FLOW-TASK5-CENTER",
            startTime,
            finishTime,
            qualifiedQty: 19,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: string.Empty,
            includeDynamicColumn: false,
            isTaskFinishUpdate: true,
            pointCount: 0);
        var completedReportPath = WriteCenterReportWorkbook(centerOutputDirectory, finishRequest);
        AssertEqual(reportPath, completedReportPath, "中心完成态更新必须复用同一设备和工单路径。");

        var isolatedRequest = BuildCenterWorkbookRequest(
            "DEVICE-TASK5",
            "FLOW-TASK5-CENTER-OTHER",
            startTime,
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P-CENTER-OTHER",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var isolatedPath = WriteCenterReportWorkbook(centerOutputDirectory, isolatedRequest);
        AssertFalse(string.Equals(completedReportPath, isolatedPath, StringComparison.OrdinalIgnoreCase), "不同工单的中心报表路径必须隔离，不能互相覆盖。");

        File.Copy(completedReportPath, centerCompletedPath, overwrite: true);
        using var workbook = new XLWorkbook(centerCompletedPath);
        AssertTrue(workbook.Worksheets.Any(sheet => sheet.Name == "生产报表"), "中心完成态样例必须包含可见生产报表工作表。");
        var worksheet = workbook.Worksheet("生产报表");
        AssertTemplateHeaderMerges(worksheet);
        AssertSequenceEqual(
            new[] { "产品编号", "拍照编号", "拍照结果", "峰值电流保存值", "峰值电流保存结果", "产品结果" },
            ReadHeaderRow(worksheet, rowNumber: 9),
            "中心完成态样例必须保留设备标题，并只显示 SaveEnable 动态列。");
        AssertFalse(ReadHeaderRow(worksheet, rowNumber: 9).Contains("峰值电流报表上限"), "中心报表不得串入 ReportEnable 独占列。");
        AssertFalse(ReadHeaderRow(worksheet, rowNumber: 9).Contains("峰值电流 MES 下限"), "中心报表不得串入 MesEnable 独占列。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("C10").GetString(), "中心点结果必须读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("D10").GetString(), "中心 SaveEnable 实际值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E10").GetString(), "中心 SaveEnable 结果值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("F10").GetString(), "中心产品结果必须读取 PLC ProductResult。");
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D7").GetString(), "中心完成态必须精确使用任务 EndTime。");
    }
    finally
    {
        DeleteDirectoryIfExists(centerOutputDirectory);
    }

    AssertTrue(File.Exists(singleSpotPath), "必须保留设备端单工位点焊视觉样例。");
    AssertTrue(File.Exists(dualInspectionPath), "必须保留设备端双工位检测视觉样例。");
    AssertTrue(File.Exists(centerCompletedPath), "必须保留中心服务器完成态视觉样例。");

    // 仅显式设置导出目录时发布最终样例；先完成全部断言，再原子替换目标文件。
    var artifactDirectory = Environment.GetEnvironmentVariable("AUTOWELD_TASK5_ARTIFACT_DIR");
    if (!string.IsNullOrWhiteSpace(artifactDirectory))
    {
        PublishReportArtifact(singleSpotPath, Path.Combine(artifactDirectory, Path.GetFileName(singleSpotPath)));
        PublishReportArtifact(dualInspectionPath, Path.Combine(artifactDirectory, Path.GetFileName(dualInspectionPath)));
        PublishReportArtifact(centerCompletedPath, Path.Combine(artifactDirectory, Path.GetFileName(centerCompletedPath)));
    }
}

static void ProductionReportRulesReloadLatestPersistedTask()
{
    var supplied = new BizWeldTask { Id = 42, ProductNum = "OLD" };
    var persisted = new BizWeldTask { Id = 42, ProductNum = "LATEST" };
    var loadedTaskId = 0;
    Func<int, BizWeldTask?> loader = taskId =>
    {
        loadedTaskId = taskId;
        return persisted;
    };

    var resolved = ProductionReportFileRules.ResolveLatestTask(supplied, loader);
    AssertTrue(ReferenceEquals(persisted, resolved), "已持久化任务存在时必须使用数据库最新对象。");
    AssertEqual(42, loadedTaskId, "最新任务解析必须按传入 TaskId 查询。");

    var unsaved = new BizWeldTask { Id = 0, ProductNum = "UNSAVED" };
    var loaderCalled = false;
    Func<int, BizWeldTask?> unsavedLoader = _ =>
    {
        loaderCalled = true;
        return persisted;
    };
    var unsavedResolved = ProductionReportFileRules.ResolveLatestTask(unsaved, unsavedLoader);
    AssertTrue(ReferenceEquals(unsaved, unsavedResolved), "未保存任务必须直接使用传入对象。");
    AssertFalse(loaderCalled, "未保存任务不得发起数据库查询。");
}

static void ProductionReportRulesSelectLatestUploadSpreadsheet()
{
    var now = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Local);
    var reports = new List<BizProductionReportFile>
    {
        BuildReportFile(1, taskId: 42, "correct-old.xlsx", now.AddMinutes(-5)),
        BuildReportFile(2, taskId: 42, "correct-new.xlsx", now),
        BuildReportFile(3, taskId: 42, "wrong-code.xlsx", now.AddMinutes(5), fileCode: "PDF"),
        BuildReportFile(4, taskId: 42, "wrong-format.xlsx", now.AddMinutes(6), fileFormat: "PDF"),
        BuildReportFile(5, taskId: 42, "wrong-mes-type.xlsx", now.AddMinutes(7), mesFileType: -1),
        BuildReportFile(6, taskId: 99, "wrong-task.xlsx", now.AddMinutes(8))
    };

    var selected = ProductionReportFileRules.SelectLatestUploadFilePath(reports, 42);
    AssertEqual("correct-new.xlsx", selected, "必须选择同任务最新的 Spreadsheet/XLSX/ReportFile 路径。");
}

static void ProductionReportCompletionFlowPersistsBeforeFinalGeneration()
{
    var reportServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductionReportFileService.cs"),
        Encoding.UTF8);
    var generateMethod = ExtractMethodText(
        reportServiceCode,
        "public BizProductionReportFile GenerateXlsxReport(BizWeldTask task)",
        "private BizProductionReportFile GetOrCreateReportRecord");
    AssertTrue(generateMethod.Contains("ProductionReportFileRules.ResolveLatestTask(", StringComparison.Ordinal), "GenerateXlsxReport 必须调用已验证的最新任务解析规则。");
    AssertTrue(generateMethod.Contains("InSingle(taskId)", StringComparison.Ordinal), "GenerateXlsxReport 必须把 TaskId 传给数据库读取入口。");
    AssertFalse(
        reportServiceCode.Contains("TestResultRules.ResolveProductResult(records.Select", StringComparison.Ordinal),
        "设备报表产品结果禁止调用焊点聚合计算。");

    var weldTaskServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"),
        Encoding.UTF8);
    var finishMethod = ExtractMethodText(
        weldTaskServiceCode,
        "public async Task<BizWeldTask> FinishAsync(",
        "public async Task<BizWeldTask> FinishLocalAsync(");
    AssertTrue(finishMethod.Contains("var finishTime = DateTime.Now;", StringComparison.Ordinal), "在线完工必须只捕获一次结束时间。");
    AssertTrue(finishMethod.Contains("EndTs = finishTime.ToString(\"yyyy-MM-dd HH:mm:ss\")", StringComparison.Ordinal), "MES 完工请求必须使用同一个结束时间。");
    AssertTrue(finishMethod.Contains("task.EndTime = finishTime;", StringComparison.Ordinal), "持久化 EndTime 必须使用同一个结束时间。");
    AssertSourceOrder(
        finishMethod,
        "_dbContext.Db.Updateable(task).ExecuteCommand();",
        "EnqueueFinishUploadTasks(task, settings.UploadMode)",
        "在线完工必须先持久化 EndTime 和统计，再生成最终报表并安排上传。");

    var finishLocalMethod = ExtractMethodText(
        weldTaskServiceCode,
        "public async Task<BizWeldTask> FinishLocalAsync(",
        "public Task RetryPendingUploadsAsync(");
    AssertTrue(finishLocalMethod.Contains("var finishTime = DateTime.Now;", StringComparison.Ordinal), "离线完工必须只捕获一次结束时间。");
    AssertTrue(finishLocalMethod.Contains("task.EndTime = finishTime;", StringComparison.Ordinal), "离线持久化 EndTime 必须使用捕获的结束时间。");
    AssertSourceOrder(
        finishLocalMethod,
        "_dbContext.Db.Updateable(task).ExecuteCommand();",
        "EnqueueFinishUploadTasks(task, CurrentSettings.UploadMode)",
        "离线完工必须先持久化 EndTime 和统计，再生成最终报表并安排上传。");
    var buildEndRequestMethod = ExtractMethodText(
        weldTaskServiceCode,
        "private static ExperimentEndReq BuildEndRequest(",
        "private static ReportExperimentStatusReq BuildStatusRequest(");
    AssertTrue(buildEndRequestMethod.Contains("var endTime = task.EndTime ?? DateTime.Now;", StringComparison.Ordinal), "离线 MES 完工请求必须优先复用持久化 EndTime。");
    AssertTrue(buildEndRequestMethod.Contains("EndTs = endTime.ToString(\"yyyy-MM-dd HH:mm:ss\")", StringComparison.Ordinal), "离线 MES 完工时间必须来自统一 endTime。");
    AssertTrue(buildEndRequestMethod.Contains("(endTime - task.StartTime).TotalHours", StringComparison.Ordinal), "离线 MES 工时必须使用统一 endTime。");

    var uploadTaskServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var buildReportRequestMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "private UploadReportFileReq? BuildReportFileRequest(BizUploadTask task)",
        "private UploadTaskSummary? FinishExecution(");
    AssertTrue(
        buildReportRequestMethod.Contains("ProductionReportFileRules.SelectLatestUploadFilePath(reportFiles, weldTask.Id)", StringComparison.Ordinal),
        "MES 报表上传必须调用已验证的最新 XLSX 选择规则。");
    AssertTrue(
        buildReportRequestMethod.Contains("FirstNonEmpty(latestReportFilePath, task.FilePath)", StringComparison.Ordinal),
        "MES 报表上传必须优先使用最新报表记录，再回退上传任务旧路径。");
}

static void FinishReportGenerationIsLocalWhileMesUploadFollowsReportEnable()
{
    static IReadOnlyList<BizUploadTask> InvokeFinishEnqueue(
        WeldTaskService service,
        BizWeldTask task)
    {
        var enqueue = typeof(WeldTaskService).GetMethod(
            "EnqueueFinishUploadTasks",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        AssertTrue(enqueue is not null, "完工流程必须保留上传任务编排入口。");
        return (IReadOnlyList<BizUploadTask>)(enqueue!.Invoke(service, [task, UploadMode.Realtime])
            ?? Array.Empty<BizUploadTask>());
    }

    var task = BuildReportTask(new DateTime(2026, 7, 17, 8, 0, 0), new DateTime(2026, 7, 17, 9, 0, 0));
    var noReportUploadTasks = new FakeUploadTaskService();
    var noReportFileService = new FakeProductionReportFileService
    {
        ShouldUploadReportFileResult = false,
        GeneratedReport = new BizProductionReportFile { FilePath = "local-fixed-fields.xlsx" }
    };
    var noReportService = CreateWeldTaskService(
        new FakeMesProvider(),
        new FakeSystemClockService(),
        new FakeOperationLogService(),
        uploadTaskService: noReportUploadTasks,
        reportFileService: noReportFileService);

    var noReportResult = InvokeFinishEnqueue(noReportService, task);
    AssertEqual(1, noReportFileService.GenerateCallCount, "无 ReportEnable 时仍必须生成固定公共字段本地 XLSX。");
    AssertFalse(
        noReportResult.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile),
        "无有效 ReportEnable 时不得创建 MES ReportFile 任务。");
    AssertFalse(
        noReportUploadTasks.Enqueued.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile),
        "无有效 ReportEnable 时上传队列不得出现 MES ReportFile 任务。");

    var enabledUploadTasks = new FakeUploadTaskService();
    var enabledReportFileService = new FakeProductionReportFileService
    {
        ShouldUploadReportFileResult = true,
        GeneratedReport = new BizProductionReportFile { FilePath = "report-enabled.xlsx" }
    };
    var enabledService = CreateWeldTaskService(
        new FakeMesProvider(),
        new FakeSystemClockService(),
        new FakeOperationLogService(),
        uploadTaskService: enabledUploadTasks,
        reportFileService: enabledReportFileService);

    var enabledResult = InvokeFinishEnqueue(enabledService, task);
    AssertEqual(1, enabledReportFileService.GenerateCallCount, "有 ReportEnable 时本地 XLSX 仍只生成一次。");
    AssertTrue(
        enabledResult.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile),
        "任一有效 ReportEnable 必须创建 MES ReportFile 任务。");
    AssertTrue(
        enabledUploadTasks.Enqueued.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile),
        "有效 ReportEnable 的 MES ReportFile 任务必须实际进入上传队列。");
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
    AssertEqual("产品编号", headers[0], "Single-station center details must start with product number.");
    AssertEqual("焊点编号", headers[1], "Center report must use the same point number column as equipment reports.");
    AssertEqual("焊点结果", headers[2], "Center report must use the same point result column as equipment reports.");
    AssertEqual("height", headers[3], "Dynamic saved values must be placed after point result columns.");
    AssertEqual("height_result", headers[4], "Dynamic saved result values must preserve equipment order.");
    AssertEqual("产品结果", headers[^1], "PLC product result must remain the final fixed detail column.");
    AssertFalse(headers.Contains("工位"), "Single-station center details must omit the station column.");
    AssertFalse(headers.Contains("工号"), "Task fields belong in the customer template header, not repeated detail columns.");
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

    AssertEqual("相机编号", headers[1], "Forwarded equipment point number header must override the center default.");
    AssertEqual("相机结果", headers[2], "Forwarded equipment point result header must override the center default.");
    AssertEqual("高度实际值", headers[3], "Forwarded equipment dynamic headers must be used in the center Excel report.");
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

static void CenterForwardingBusinessIdsHashFullIdentity()
{
    var buildBusinessId = typeof(CenterProductForwardingService).GetMethod(
        "BuildBusinessId",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(buildBusinessId is not null, "中心转发服务必须保留 BusinessId 生产入口。");

    var commonPrefix = new string('W', 120);
    var firstRequest = new CenterProductReportRequest
    {
        DeviceId = "DEVICE-A",
        StationNo = 1,
        WorkOrder = commonPrefix + "-A",
        ProductNo = new string('P', 120) + "-A"
    };
    var secondRequest = new CenterProductReportRequest
    {
        DeviceId = "DEVICE-A",
        StationNo = 1,
        WorkOrder = commonPrefix + "-B",
        ProductNo = new string('P', 120) + "-B"
    };
    var differentDeviceRequest = new CenterProductReportRequest
    {
        DeviceId = "DEVICE-B",
        StationNo = firstRequest.StationNo,
        WorkOrder = firstRequest.WorkOrder,
        ProductNo = firstRequest.ProductNo
    };
    var normalizedSameDeviceRequest = new CenterProductReportRequest
    {
        DeviceId = "  DEVICE-A  ",
        StationNo = firstRequest.StationNo,
        WorkOrder = firstRequest.WorkOrder,
        ProductNo = firstRequest.ProductNo
    };

    var firstId = (string?)buildBusinessId!.Invoke(null, [firstRequest]);
    var repeatedId = (string?)buildBusinessId.Invoke(null, [firstRequest]);
    var secondId = (string?)buildBusinessId.Invoke(null, [secondRequest]);
    var differentDeviceId = (string?)buildBusinessId.Invoke(null, [differentDeviceRequest]);
    var normalizedSameDeviceId = (string?)buildBusinessId.Invoke(null, [normalizedSameDeviceRequest]);

    AssertFalse(string.IsNullOrWhiteSpace(firstId), "中心转发 BusinessId 不得为空。");
    AssertTrue(firstId!.Length <= 100, "中心转发 BusinessId 必须保持在数据库 100 字符限制内。");
    AssertEqual(firstId, repeatedId, "相同完整身份必须生成稳定的 BusinessId。");
    AssertEqual(firstId, normalizedSameDeviceId, "DeviceId 首尾空格规范化后，相同完整身份必须保持稳定。");
    AssertFalse(string.Equals(firstId, differentDeviceId, StringComparison.Ordinal), "仅 DeviceId 不同的完整身份必须生成不同 BusinessId。");
    AssertFalse(string.Equals(firstId, secondId, StringComparison.Ordinal), "仅在旧截断尾部不同的完整身份必须生成不同 BusinessId。");
    AssertTrue(firstId.Contains(':', StringComparison.Ordinal), "BusinessId 必须保留可读前缀并附加完整身份哈希。");
}

static void CenterDynamicReportColumnsUseSaveEnableOnly()
{
    var item = new DimTestItem
    {
        ItemId = 1,
        ItemName = "峰值电流",
        ActualExpression = "0:F-0",
        UpperExpression = "0:F-4",
        LowerExpression = "0:F-8",
        ResultExpression = "0:W-12"
    };
    var detail = new BizSchemeDetail
    {
        EnableActual = true,
        SaveActual = true,
        ActualHeader = "峰值电流保存值",
        EnableUpper = true,
        ReportUpper = true,
        UpperHeader = "峰值电流报表上限",
        EnableLower = true,
        MesLower = true,
        LowerHeader = "峰值电流 MES 下限",
        EnableResult = true,
        SaveResult = true,
        ResultHeader = "峰值电流保存结果"
    };

    var columns = BuildCenterDynamicReportColumns(detail, item);

    AssertSequenceEqual(
        new[] { "峰值电流保存值", "峰值电流保存结果" },
        columns.Select(column => column.Title).ToArray(),
        "中心动态列只能包含已采集且 SaveEnable=true 的角色，ReportEnable/MesEnable 独占角色不得进入。");
}

static void CenterProductRequestUsesPlcResultAndTaskTimestamps()
{
    var startTime = new DateTime(2026, 7, 17, 8, 1, 2, DateTimeKind.Local);
    var task = BuildReportTask(startTime, endTime: null);
    task.ProductNum = string.Empty;
    var point = BuildReportPoint(task.Id, stationNo: 1, productNo: "P-CENTER-001", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok);
    point.ProductResult = ProductionConstants.TestResults.Ng;

    var request = BuildCenterProductRequest(
        new AppSettings { DeviceId = "DEVICE-01", EnableDualStation = false },
        task,
        stationNo: 1,
        [point]);

    AssertEqual(ProductionConstants.TestResults.Ng, request.ProductResult, "中心产品结果必须读取 PLC ProductResult，不得聚合焊点 TestResult。");
    AssertEqual(startTime, ReadCenterRequestProperty<DateTime>(request, "StartTime"), "中心产品请求开始时间必须只取任务 StartTime。");
    AssertTrue(ReadCenterRequestProperty<DateTime?>(request, "EndTime") is null, "产品完成时任务未完工，中心请求 EndTime 必须为空。");
    AssertEqual(task.QualifiedQty, ReadCenterRequestProperty<int>(request, "QualifiedQty"), "中心产品请求必须携带任务当前合格数量。");
    AssertFalse(ReadCenterRequestProperty<bool>(request, "IsTaskFinishUpdate"), "产品完成请求不得标记为工单完工更新。");
    AssertEqual(task.UserNumber, request.OperatorNo, "客户模板操作人员必须只取任务开工 UserNumber，不得被点操作员覆盖。");

    task.StartAmount = 0;
    task.ActualQty = 18;
    var zeroQuantityRequest = BuildCenterProductRequest(
        new AppSettings { DeviceId = "DEVICE-01", EnableDualStation = false },
        task,
        stationNo: 1,
        [point]);
    AssertEqual(0, zeroQuantityRequest.Quantity, "客户模板生产数量必须只取任务 StartAmount，不得回退 ActualQty。");
}

static void CenterProductRequestResolvesConfiguredStationName()
{
    var task = BuildReportTask(DateTime.Now, endTime: null);
    task.ProductNum = string.Empty;
    var point = BuildReportPoint(task.Id, stationNo: 2, productNo: "P-CENTER-002", sequenceNo: 1, pointResult: ProductionConstants.TestResults.Ok);
    point.ProductResult = ProductionConstants.TestResults.Ok;

    var singleStationRequest = BuildCenterProductRequest(
        new AppSettings { DeviceId = "DEVICE-01", EnableDualStation = false },
        task,
        stationNo: 1,
        [point]);
    var dualStationRequest = BuildCenterProductRequest(
        new AppSettings
        {
            DeviceId = "DEVICE-01",
            EnableDualStation = true,
            Station1DisplayName = " 左工位 ",
            Station2DisplayName = " 右工位 "
        },
        task,
        stationNo: 2,
        [point]);

    AssertEqual(string.Empty, ReadCenterRequestProperty<string>(singleStationRequest, "StationName"), "单工位请求不应要求或填充 StationName。");
    AssertEqual("右工位", ReadCenterRequestProperty<string>(dualStationRequest, "StationName"), "双工位请求必须携带规范化后的配置名称。");
}

static void CenterReportProductThenFinishUpdateKeepsDetailRows()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var startTime = new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Local);
        var productRequest = BuildCenterWorkbookRequest(
            deviceId: "DEVICE-01",
            workOrder: "FLOW-CENTER-001",
            startTime,
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: true,
            isTaskFinishUpdate: false,
            pointCount: 2);

        var reportPath = WriteCenterReportWorkbook(outputDirectory, productRequest);
        using (var workbook = new XLWorkbook(reportPath))
        {
            var worksheet = workbook.Worksheet(CenterProductReportFormat.WorksheetName);
            AssertEqual("产品工号：164#J", worksheet.Cell("A1").GetString(), "中心可见报表必须复用客户模板任务表头。");
            AssertEqual("结束时间：", worksheet.Cell("D7").GetString(), "产品请求生成报表时 EndTime 必须为空。");
            AssertEqual(2, CountCenterDataRows(workbook), "产品请求必须写入全部点明细。");
        }

        var finishTime = new DateTime(2026, 7, 17, 10, 30, 40, DateTimeKind.Local);
        var finishRequest = BuildCenterWorkbookRequest(
            deviceId: "DEVICE-01",
            workOrder: "FLOW-CENTER-001",
            startTime,
            endTime: finishTime,
            qualifiedQty: 19,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: string.Empty,
            includeDynamicColumn: false,
            isTaskFinishUpdate: true,
            pointCount: 0);

        var updatedPath = WriteCenterReportWorkbook(outputDirectory, finishRequest);
        AssertEqual(reportPath, updatedPath, "完工更新必须定位到同一设备和流转卡报表。");
        using var updatedWorkbook = new XLWorkbook(updatedPath);
        var updatedWorksheet = updatedWorkbook.Worksheet(CenterProductReportFormat.WorksheetName);
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", updatedWorksheet.Cell("D7").GetString(), "完工更新必须精确刷新任务 EndTime。");
        AssertEqual("合格数量：19", updatedWorksheet.Cell("D5").GetString(), "完工更新必须刷新最终 QualifiedQty。");
        AssertEqual(2, CountCenterDataRows(updatedWorkbook), "完工更新不得重复携带或追加产品点明细。");
        AssertEqual(11, updatedWorksheet.LastRowUsed()!.RowNumber(), "完工更新不得增加可见明细行数。");

        var artifactPath = Environment.GetEnvironmentVariable("AUTOWELD_CENTER_REPORT_ARTIFACT");
        if (!string.IsNullOrWhiteSpace(artifactPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(artifactPath))!);
            File.Copy(updatedPath, artifactPath, overwrite: true);
        }
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportKeepsFixedDetailsWithoutDynamicSaveFields()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-CENTER-002",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);

        var reportPath = WriteCenterReportWorkbook(outputDirectory, request);
        using var workbook = new XLWorkbook(reportPath);
        var worksheet = workbook.Worksheet(CenterProductReportFormat.WorksheetName);

        AssertSequenceEqual(
            new[] { "产品编号", "拍照编号", "拍照结果", "产品结果" },
            ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
            "没有 SaveEnable 动态项时，中心报表仍必须保留固定产品、点和结果列。");
        AssertEqual("P001", worksheet.Cell("A10").GetString(), "没有动态列时仍必须输出产品明细。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("D10").GetString(), "固定产品结果列必须保留 PLC 产品结果。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportRendersSingleAndDualStationColumns()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var singleRequest = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-SINGLE",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var dualRequest = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-DUAL",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: true,
            stationNo: 2,
            stationName: "右工位",
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);

        using var singleWorkbook = new XLWorkbook(WriteCenterReportWorkbook(outputDirectory, singleRequest));
        using var dualWorkbook = new XLWorkbook(WriteCenterReportWorkbook(outputDirectory, dualRequest));
        var singleSheet = singleWorkbook.Worksheet(CenterProductReportFormat.WorksheetName);
        var dualSheet = dualWorkbook.Worksheet(CenterProductReportFormat.WorksheetName);

        AssertFalse(ReadHeaderRow(singleSheet, CenterProductReportFormat.DetailHeaderRow).Contains("工位"), "单工位中心报表必须完全省略工位列。");
        AssertEqual("工位", dualSheet.Cell("A9").GetString(), "双工位中心报表必须保留工位列。");
        AssertEqual("右工位", dualSheet.Cell("A10").GetString(), "双工位中心报表必须显示设备端解析后的配置名称。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportReplacesDuplicateProductRows()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var firstRequest = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-IDEMPOTENT",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 2);
        var secondRequest = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-IDEMPOTENT",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        secondRequest.ProductResult = ProductionConstants.TestResults.Ng;
        secondRequest.Points[0].TestResult = ProductionConstants.TestResults.Ng;

        WriteCenterReportWorkbook(outputDirectory, firstRequest);
        var reportPath = WriteCenterReportWorkbook(outputDirectory, secondRequest);
        using var workbook = new XLWorkbook(reportPath);
        var worksheet = workbook.Worksheet(CenterProductReportFormat.WorksheetName);

        AssertEqual(1, CountCenterDataRows(workbook), "同一产品重试必须替换旧点行，不得重复累计。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("C10").GetString(), "幂等替换后必须显示最新点结果。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("D10").GetString(), "幂等替换后必须显示最新 PLC 产品结果。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportIsolatesDeviceAndWorkOrderFiles()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var first = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-001",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: true,
            stationNo: 1,
            stationName: "左工位",
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var sameDeviceAndWorkOrder = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-001",
            new DateTime(2026, 7, 18, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: true,
            stationNo: 2,
            stationName: "右工位",
            productNo: "P002",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        sameDeviceAndWorkOrder.DeviceName = "设备名称已变更";
        var differentDevice = BuildCenterWorkbookRequest(
            "DEVICE-02",
            "FLOW-001",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var differentWorkOrder = BuildCenterWorkbookRequest(
            "DEVICE-01",
            "FLOW-002",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);

        var firstPath = WriteCenterReportWorkbook(outputDirectory, first);
        var samePath = WriteCenterReportWorkbook(outputDirectory, sameDeviceAndWorkOrder);
        var differentDevicePath = WriteCenterReportWorkbook(outputDirectory, differentDevice);
        var differentWorkOrderPath = WriteCenterReportWorkbook(outputDirectory, differentWorkOrder);

        AssertEqual(firstPath, samePath, "同一设备编号和流转卡号必须定位同一中心报表，不应受日期、工位或设备名称变化影响。");
        AssertFalse(string.Equals(firstPath, differentDevicePath, StringComparison.OrdinalIgnoreCase), "不同设备编号必须隔离中心报表文件。");
        AssertFalse(string.Equals(firstPath, differentWorkOrderPath, StringComparison.OrdinalIgnoreCase), "不同流转卡号必须隔离中心报表文件。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportPathStaysInsideRootForTraversalNames()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var request = BuildCenterWorkbookRequest(
            "..",
            "..",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);

        var reportPath = new CenterProductReportFileStore().Upsert(outputDirectory, request);
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(outputDirectory)) + Path.DirectorySeparatorChar;
        var fullReportPath = Path.GetFullPath(reportPath);

        AssertTrue(fullReportPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase), "设备/SN 为 .. 时中心报表不得逃逸配置根目录。");
        AssertTrue(File.Exists(fullReportPath), "安全路径内必须生成中心报表。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportPathDistinguishesSanitizedCollisions()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var slashRequest = BuildCenterWorkbookRequest(
            "DEVICE/A",
            "FLOW/A",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var colonDeviceRequest = BuildCenterWorkbookRequest(
            "DEVICE:A",
            "FLOW/A",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var colonWorkOrderRequest = BuildCenterWorkbookRequest(
            "DEVICE/A",
            "FLOW:A",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var spacedDeviceRequest = BuildCenterWorkbookRequest(
            " DEVICE/A ",
            "FLOW/A",
            new DateTime(2026, 7, 17, 8, 0, 0),
            endTime: null,
            qualifiedQty: 0,
            enableDualStation: false,
            stationNo: 1,
            stationName: string.Empty,
            productNo: "P001",
            includeDynamicColumn: false,
            isTaskFinishUpdate: false,
            pointCount: 1);
        var store = new CenterProductReportFileStore();

        var slashPath = store.Upsert(outputDirectory, slashRequest);
        var colonDevicePath = store.Upsert(outputDirectory, colonDeviceRequest);
        var colonWorkOrderPath = store.Upsert(outputDirectory, colonWorkOrderRequest);
        var spacedDevicePath = store.Upsert(outputDirectory, spacedDeviceRequest);

        AssertFalse(string.Equals(slashPath, colonDevicePath, StringComparison.OrdinalIgnoreCase), "设备编号 A/B 与 A:B 不得碰撞。");
        AssertFalse(string.Equals(slashPath, colonWorkOrderPath, StringComparison.OrdinalIgnoreCase), "流转卡号 A/B 与 A:B 不得碰撞。");
        AssertFalse(string.Equals(slashPath, spacedDevicePath, StringComparison.OrdinalIgnoreCase), "哈希必须基于原始值，首尾空格不同的设备编号不得碰撞。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportKeepsFinalHeaderAfterLateProductRetry()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var startTime = new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Local);
        var productRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LATE", startTime, null, 1, false, 1, string.Empty, "P001", false, false, 2);
        var finishTime = new DateTime(2026, 7, 17, 10, 20, 30, DateTimeKind.Local);
        var finishRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LATE", startTime, finishTime, 19, false, 1, string.Empty, string.Empty, false, true, 0);
        var lateProductRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LATE", startTime, null, 2, false, 1, string.Empty, "P002", false, false, 1);

        store.Upsert(outputDirectory, productRequest);
        store.Upsert(outputDirectory, finishRequest);
        var reportPath = store.Upsert(outputDirectory, lateProductRequest);

        using var workbook = new XLWorkbook(reportPath);
        var worksheet = workbook.Worksheet(CenterProductReportFormat.WorksheetName);
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D7").GetString(), "迟到产品请求不得清空已完成 EndTime。");
        AssertEqual("合格数量：19", worksheet.Cell("D5").GetString(), "迟到产品请求不得回退最终 QualifiedQty。");
        AssertEqual(3, CountCenterDataRows(workbook), "迟到的新产品明细仍应正常追加。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportPreservesCorruptExistingWorkbook()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-CORRUPT", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var reportPath = store.Upsert(outputDirectory, request);
        var corruptBytes = Encoding.UTF8.GetBytes("not-an-xlsx-workbook");
        File.WriteAllBytes(reportPath, corruptBytes);

        var failed = false;
        try
        {
            store.Upsert(outputDirectory, request);
        }
        catch
        {
            failed = true;
        }

        AssertTrue(failed, "读取损坏的现有报表时必须失败并交给上传队列重试。");
        AssertSequenceEqual(corruptBytes, File.ReadAllBytes(reportPath), "损坏原文件不得被当作空报表覆盖。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterDashboardSkipsUnrelatedCorruptFormalWorkbooks()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var reportDate = new DateTime(2026, 7, 17, 8, 0, 0);
        var historicalRequest = BuildCenterWorkbookRequest(
            "DEVICE-VALID", "FLOW-HISTORY", reportDate, null, 0, false, 1, string.Empty, "P-HISTORY", false, false, 1);
        var corruptRequest = BuildCenterWorkbookRequest(
            "DEVICE-CORRUPT", "FLOW-CORRUPT-OTHER", reportDate, null, 0, false, 1, string.Empty, "P-CORRUPT", false, false, 1);
        var currentRequest = BuildCenterWorkbookRequest(
            "DEVICE-VALID", "FLOW-CURRENT", reportDate, null, 0, false, 1, string.Empty, "P-CURRENT", false, false, 1);

        store.Upsert(outputDirectory, historicalRequest);
        var corruptPath = store.Upsert(outputDirectory, corruptRequest);
        var corruptBytes = Encoding.UTF8.GetBytes("unrelated-corrupt-formal-xlsx");
        File.WriteAllBytes(corruptPath, corruptBytes);

        var currentPath = store.Upsert(outputDirectory, currentRequest);
        AssertTrue(File.Exists(currentPath), "存在无关损坏正式报表时，当前合法产品仍必须完成 ingest 写入。");
        var products = store.LoadProducts(outputDirectory, "DEVICE-VALID", stationNo: 1, reportDate);
        AssertSequenceEqual(
            new[] { "P-CURRENT", "P-HISTORY" },
            products.Select(product => product.ProductNo).OrderBy(productNo => productNo).ToArray(),
            "中心看板必须从其余有效正式报表汇总产品，并跳过无关损坏文件。");
        AssertSequenceEqual(corruptBytes, File.ReadAllBytes(corruptPath), "看板跳过损坏历史文件时不得改写其原字节。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportAtomicUpdateLeavesNoTemporaryFiles()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-ATOMIC", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);

        store.Upsert(outputDirectory, request);
        request.Points[0].TestResult = ProductionConstants.TestResults.Ng;
        store.Upsert(outputDirectory, request);

        var temporaryFiles = Directory.EnumerateFiles(outputDirectory, "*.tmp-*", SearchOption.AllDirectories).ToArray();
        AssertEqual(0, temporaryFiles.Length, "正常原子创建和替换后不得残留同目录临时文件。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportLockPreservesFileWhenSameReportIsBusy()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LOCKED", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var reportPath = store.Upsert(outputDirectory, request);
        var originalBytes = File.ReadAllBytes(reportPath);
        request.Points[0].TestResult = ProductionConstants.TestResults.Ng;

        using var occupiedLock = new FileStream(
            reportPath + ".lock",
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
        var failed = false;
        try
        {
            new CenterProductReportFileStore().Upsert(outputDirectory, request);
        }
        catch (IOException)
        {
            failed = true;
        }

        AssertTrue(failed, "同一报表锁被其他进程占用时，写入必须在有限等待后失败并进入上传重试。");
        AssertSequenceEqual(originalBytes, File.ReadAllBytes(reportPath), "获取同一路径锁失败时正式报表字节不得变化。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportLockDoesNotBlockDifferentReport()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var firstRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LOCK-A", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var secondRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-LOCK-B", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P002", false, false, 1);
        var firstPath = store.Upsert(outputDirectory, firstRequest);
        var originalBytes = File.ReadAllBytes(firstPath);
        firstRequest.Points[0].TestResult = ProductionConstants.TestResults.Ng;

        using var occupiedLock = new FileStream(
            firstPath + ".lock",
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
        var lockedWrite = Task.Run(() =>
        {
            try
            {
                store.Upsert(outputDirectory, firstRequest);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        });
        AssertTrue(
            SpinWait.SpinUntil(() => lockedWrite.Status == TaskStatus.Running, TimeSpan.FromSeconds(1)),
            "测试必须先让同一 Store 进入报表 A 的锁等待。");
        Thread.Sleep(100);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var secondPath = store.Upsert(outputDirectory, secondRequest);
        stopwatch.Stop();

        AssertTrue(lockedWrite.GetAwaiter().GetResult(), "报表 A 的独占锁被占用时，同一路径写入仍必须超时失败。");
        AssertSequenceEqual(originalBytes, File.ReadAllBytes(firstPath), "报表 A 获取锁失败时正式文件不得变化。");
        AssertTrue(File.Exists(secondPath), "一个报表锁被占用时，不同设备/流转卡报表仍必须独立写入。");
        AssertFalse(string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase), "不同报表必须使用不同的路径锁。");
        AssertTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"同一生产 Store 写报表 B 不得等待报表 A 的锁超时，实际耗时 {stopwatch.Elapsed.TotalMilliseconds:F0} ms。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportLockPreservesConcurrentProductsFromDifferentStores()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var firstRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-CONCURRENT", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var secondRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-CONCURRENT", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P002", false, false, 1);
        using var startSignal = new ManualResetEventSlim(false);
        var firstWrite = Task.Run(() =>
        {
            startSignal.Wait();
            return new CenterProductReportFileStore().Upsert(outputDirectory, firstRequest);
        });
        var secondWrite = Task.Run(() =>
        {
            startSignal.Wait();
            return new CenterProductReportFileStore().Upsert(outputDirectory, secondRequest);
        });

        startSignal.Set();
        Task.WaitAll(firstWrite, secondWrite);

        AssertEqual(firstWrite.Result, secondWrite.Result, "两个 Store 写同一设备和流转卡时必须定位同一正式报表。");
        using var workbook = new XLWorkbook(firstWrite.Result);
        var dataWorksheet = workbook.Worksheet(CenterProductReportFormat.DataWorksheetName);
        var productNoColumn = dataWorksheet.Row(1).CellsUsed()
            .Single(cell => string.Equals(cell.GetString(), "ProductNo", StringComparison.OrdinalIgnoreCase))
            .Address.ColumnNumber;
        var products = dataWorksheet
            .RowsUsed()
            .Skip(1)
            .Select(row => row.Cell(productNoColumn).GetString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToArray();
        AssertSequenceEqual(new[] { "P001", "P002" }, products, "跨 Store 并发写同一报表时不得丢失任一产品。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterDashboardIgnoresLegacyTemporaryWorkbooks()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var store = new CenterProductReportFileStore();
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-TEMP", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var reportPath = store.Upsert(outputDirectory, request);
        var reportDirectory = Path.GetDirectoryName(reportPath)!;
        var reportFileName = Path.GetFileName(reportPath);
        File.Copy(reportPath, Path.Combine(reportDirectory, $".{reportFileName}.tmp-complete.xlsx"));
        File.WriteAllText(
            Path.Combine(reportDirectory, $".{reportFileName}.tmp-corrupt.xlsx"),
            "not-an-xlsx-workbook",
            Encoding.UTF8);

        var products = store.LoadProducts(outputDirectory, request.DeviceId, request.StationNo, request.CompletedAt.Date);
        AssertEqual(1, products.Count, "中心看板只能读取正式报表，不得重复读取完整遗留临时文件。");

        var nextRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-TEMP", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P002", false, false, 1);
        var updatedPath = store.Upsert(outputDirectory, nextRequest);
        using var workbook = new XLWorkbook(updatedPath);
        AssertEqual(2, CountCenterDataRows(workbook), "损坏遗留临时文件不得阻断同一正式报表的后续 ingest。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterReportTemporaryPathIsNotXlsx()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var createdPaths = new List<string>();
        using var watcher = new FileSystemWatcher(outputDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        watcher.Created += (_, args) =>
        {
            lock (createdPaths)
            {
                createdPaths.Add(args.FullPath);
            }
        };

        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-TEMP-EXT", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);
        new CenterProductReportFileStore().Upsert(outputDirectory, request);

        SpinWait.SpinUntil(() =>
        {
            lock (createdPaths)
            {
                return createdPaths.Any(path => Path.GetFileName(path).Contains(".tmp-", StringComparison.OrdinalIgnoreCase));
            }
        }, TimeSpan.FromSeconds(2));

        string[] temporaryPaths;
        lock (createdPaths)
        {
            temporaryPaths = createdPaths
                .Where(path => Path.GetFileName(path).Contains(".tmp-", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        AssertTrue(temporaryPaths.Length > 0, "原子写入测试必须观察到本系统临时文件创建事件。");
        AssertTrue(
            temporaryPaths.All(path => !string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase)),
            "本系统临时文件扩展名不得匹配看板使用的 *.xlsx 正式报表模式。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterIngestValidatesProductAndFinishRequestsSeparately()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var configuration = new ConfigurationBuilder().Build();
        var settingsService = new CenterServerSettingsService(configuration);
        settingsService.Save(new CenterServerLocalSettings
        {
            DataDirectory = outputDirectory,
            LogDirectory = Path.Combine(outputDirectory, "Logs"),
            OfflineTimeoutSeconds = CenterServerConstants.DefaultOfflineTimeoutSeconds
        });
        var sideEffects = new FakeCenterProductReportIngestSideEffects();
        var service = new CenterProductReportIngestService(
            settingsService,
            new CenterProductReportFileStore(),
            sideEffects);
        var productRequest = new CenterProductReportRequest
        {
            DeviceId = "DEVICE-01",
            StationNo = 1,
            WorkOrder = "FLOW-VALIDATION",
            IsTaskFinishUpdate = false,
            Points = []
        };
        var finishRequest = new CenterProductReportRequest
        {
            DeviceId = "DEVICE-01",
            StationNo = 1,
            WorkOrder = string.Empty,
            IsTaskFinishUpdate = true,
            Points = []
        };
        var finishWithoutEndTimeRequest = new CenterProductReportRequest
        {
            DeviceId = "DEVICE-01",
            StationNo = 1,
            WorkOrder = "FLOW-VALIDATION",
            IsTaskFinishUpdate = true,
            EndTime = null,
            Points = []
        };

        var productResult = service.IngestAsync(productRequest).GetAwaiter().GetResult();
        var finishResult = service.IngestAsync(finishRequest).GetAwaiter().GetResult();
        var finishWithoutEndTimeResult = service.IngestAsync(finishWithoutEndTimeRequest).GetAwaiter().GetResult();

        AssertFalse(productResult.Success, "产品请求没有点明细时必须拒绝。");
        AssertTrue(productResult.Message.Contains("points", StringComparison.OrdinalIgnoreCase), "产品请求验证必须明确指出点明细缺失。");
        AssertEqual(0, sideEffects.Calls.Count, "协议验证失败时不得执行数据库、计数或通知副作用。");
        AssertFalse(finishResult.Success, "完工请求没有流转卡号时必须拒绝。");
        AssertTrue(finishResult.Message.Contains("WorkOrder", StringComparison.OrdinalIgnoreCase), "完工请求允许空 Points，但必须验证任务定位字段。");
        AssertFalse(finishWithoutEndTimeResult.Success, "完工请求没有最终 EndTime 时必须拒绝。");
        AssertTrue(finishWithoutEndTimeResult.Message.Contains("EndTime", StringComparison.OrdinalIgnoreCase), "完工请求空 Points 应进入完工字段验证，而不是产品点明细验证。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterIngestAcceptsProductAndRunsProductionSideEffects()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var settingsService = CreateCenterServerSettingsService(outputDirectory);
        var sideEffects = new FakeCenterProductReportIngestSideEffects();
        var service = new CenterProductReportIngestService(
            settingsService,
            new CenterProductReportFileStore(),
            sideEffects);
        var request = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-INGEST-PRODUCT", new DateTime(2026, 7, 17, 8, 0, 0), null, 0, false, 1, string.Empty, "P001", false, false, 1);

        var result = service.IngestAsync(request).GetAwaiter().GetResult();

        AssertTrue(result.Success, "合法产品请求必须通过公开 IngestAsync 成功写入。");
        AssertEqual(1, sideEffects.Calls.Count, "产品文件写入成功后必须执行一次生产副作用。");
        AssertFalse(sideEffects.Calls[0].Request.IsTaskFinishUpdate, "产品副作用必须保留产品请求类型。");
        AssertEqual(request.DeviceId, sideEffects.Calls[0].DeviceId, "生产副作用必须接收规范化后的设备编号。");
        AssertEqual(1, Directory.EnumerateFiles(outputDirectory, "*.xlsx", SearchOption.AllDirectories).Count(), "成功产品 ingest 必须生成正式报表。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterIngestAcceptsFinishWithoutPointsAndRunsProductionSideEffects()
{
    var outputDirectory = CreateCenterReportFixtureDirectory();
    try
    {
        var settingsService = CreateCenterServerSettingsService(outputDirectory);
        var sideEffects = new FakeCenterProductReportIngestSideEffects();
        var service = new CenterProductReportIngestService(
            settingsService,
            new CenterProductReportFileStore(),
            sideEffects);
        var startTime = new DateTime(2026, 7, 17, 8, 0, 0);
        var productRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-INGEST-FINISH", startTime, null, 0, false, 1, string.Empty, "P001", false, false, 1);
        var finishRequest = BuildCenterWorkbookRequest(
            "DEVICE-01", "FLOW-INGEST-FINISH", startTime, startTime.AddHours(2), 1, false, 1, string.Empty, string.Empty, false, true, 0);
        var productResult = service.IngestAsync(productRequest).GetAwaiter().GetResult();

        var finishResult = service.IngestAsync(finishRequest).GetAwaiter().GetResult();

        AssertTrue(productResult.Success, "完工前产品请求必须先成功写入同一报表。");
        AssertTrue(finishResult.Success, "完工请求即使 Points 为空也必须通过公开 IngestAsync 成功更新。");
        AssertEqual(2, sideEffects.Calls.Count, "产品和完工文件写入成功后必须各执行一次生产副作用。");
        AssertTrue(sideEffects.Calls[1].Request.IsTaskFinishUpdate, "第二次生产副作用必须对应完工请求。");
        AssertEqual(0, sideEffects.Calls[1].Request.Points.Count, "完工副作用不得依赖或补造产品点明细。");
    }
    finally
    {
        DeleteDirectoryIfExists(outputDirectory);
    }
}

static void CenterFinishUpdateQueuesAfterTaskPersistence()
{
    var enqueueMethod = typeof(ICenterProductForwardingService).GetMethod("EnqueueTaskFinishUpdate");
    AssertTrue(enqueueMethod is not null, "中心转发接口必须提供工单完工更新入队入口。");

    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"),
        Encoding.UTF8);
    var finishMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BizWeldTask> FinishAsync(",
        "public async Task<BizWeldTask> FinishLocalAsync(");
    var finishLocalMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BizWeldTask> FinishLocalAsync(",
        "public Task RetryPendingUploadsAsync(");

    AssertSourceOrder(
        finishMethod,
        "_dbContext.Db.Updateable(task).ExecuteCommand();",
        "_centerProductForwardingService.EnqueueTaskFinishUpdate(task);",
        "在线完工必须先持久化 EndTime 和统计，再把中心完工更新放入可重试队列。");
    AssertSourceOrder(
        finishLocalMethod,
        "_dbContext.Db.Updateable(task).ExecuteCommand();",
        "_centerProductForwardingService.EnqueueTaskFinishUpdate(task);",
        "离线完工必须先持久化 EndTime 和统计，再把中心完工更新放入可重试队列。");
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

static void DeviceStatusLocalLogStoreRemovesSelectedLogIds()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusDeleteTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var firstDay = new DateTime(2026, 7, 8, 9, 30, 0);
    var secondDay = firstDay.AddDays(1);
    var selected = new BizDeviceStatusLog
    {
        Id = 101,
        DeviceId = "D-DELETE",
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        OccurredTime = firstDay,
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var selectedUploaded = new BizDeviceStatusLog
    {
        Id = selected.Id,
        DeviceId = selected.DeviceId,
        DeviceStatus = selected.DeviceStatus,
        OccurredTime = firstDay,
        ReportStatus = ProductionConstants.UploadStatuses.Failed
    };
    var retained = new BizDeviceStatusLog
    {
        Id = 102,
        DeviceId = "D-RETAIN",
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        OccurredTime = firstDay.AddMinutes(1),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var otherDay = new BizDeviceStatusLog
    {
        Id = 103,
        DeviceId = "D-OTHER",
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        OccurredTime = secondDay,
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(selected, settings), "待删除日志必须能写入本地 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(selectedUploaded, settings), "同一日志 ID 的追加版本必须能写入本地 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(retained, settings), "未选日志必须能写入本地 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(otherDay, settings), "其他日期日志必须能写入本地 JSONL。");

        var removeMethod = typeof(DeviceStatusLocalLogStore).GetMethod("TryRemove");
        AssertTrue(removeMethod is not null, "设备状态本地日志必须提供按日志删除的方法。");

        var removed = removeMethod!.Invoke(null, new object?[] { new[] { selected }, settings });
        AssertEqual(true, removed, "删除本地设备状态日志必须成功。");

        var firstDayLogs = DeviceStatusLocalLogStore.Read(settings, firstDay.Date, firstDay.Date.AddDays(1).AddTicks(-1), 10);
        AssertSequenceEqual(new[] { retained.Id }, firstDayLogs.Select(entry => entry.Id).ToArray(), "删除后同一日志 ID 的所有追加版本都不能继续显示。");

        var secondDayLogs = DeviceStatusLocalLogStore.Read(settings, secondDay.Date, secondDay.Date.AddDays(1).AddTicks(-1), 10);
        AssertSequenceEqual(new[] { otherDay.Id }, secondDayLogs.Select(entry => entry.Id).ToArray(), "删除当天日志不能影响其他日期文件。");

        var allLogs = DeviceStatusLocalLogStore.Read(settings, from: null, to: null, maxCount: 10);
        AssertSequenceEqual(new[] { otherDay.Id, retained.Id }, allLogs.Select(entry => entry.Id).ToArray(), "无日期范围读取时必须覆盖全部设备状态日志文件。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusPendingSourceAndTaskReconciliationAreWired()
{
    var interfaceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Interfaces", "IDeviceStatusService.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"), Encoding.UTF8);
    var uploadTaskCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"), Encoding.UTF8);
    var summaryCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "DTOs", "Upload", "UploadTaskSummary.cs"), Encoding.UTF8);

    AssertTrue(DeviceStatusUploadVisibilityRules.ShouldInclude(ProductionConstants.UploadStatuses.Pending), "未上传设备状态日志必须进入待上传页签。");
    AssertTrue(DeviceStatusUploadVisibilityRules.ShouldInclude(ProductionConstants.UploadStatuses.Failed), "上传失败设备状态日志必须进入待上传页签。");
    AssertFalse(DeviceStatusUploadVisibilityRules.ShouldInclude(ProductionConstants.UploadStatuses.Uploaded), "已上传设备状态日志不能进入待上传页签。");
    AssertFalse(DeviceStatusUploadVisibilityRules.ShouldInclude(ProductionConstants.UploadStatuses.Skipped), "已跳过设备状态日志不能进入待上传页签。");
    AssertTrue(interfaceCode.Contains("EnsurePendingUploadTask", StringComparison.Ordinal), "设备状态服务必须暴露按日志幂等补建上传任务的方法。");
    AssertTrue(serviceCode.Contains("DeviceStatusUploadVisibilityRules.ShouldInclude", StringComparison.Ordinal), "设备状态服务必须按日志上报状态筛选待上传记录。");
    AssertTrue(serviceCode.Contains("var existing = FindExistingUploadTask(task);", StringComparison.Ordinal), "设备状态任务补建必须先按日志业务 ID 查找现有任务。");
    AssertTrue(serviceCode.Contains("existing.IsDeleted = false;", StringComparison.Ordinal), "日志来源有效时应恢复旧的软删除任务。");
    AssertTrue(uploadTaskCode.Contains("GetLogs(from: null, to: null, maxCount: 5000)", StringComparison.Ordinal), "设备状态上传任务查询必须以设备状态日志为来源。");
    AssertTrue(uploadTaskCode.Contains("EnsurePendingUploadTask", StringComparison.Ordinal), "设备状态上传任务查询必须补建缺失的关联任务。");
    AssertTrue(summaryCode.Contains("DeviceStatusLogId", StringComparison.Ordinal), "上传任务摘要必须携带设备状态日志 ID。");
}

static void DeviceStatusLogDeletionRefreshIsWiredAcrossViews()
{
    var interfaceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Interfaces", "IDeviceStatusService.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"), Encoding.UTF8);
    var logViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);
    var stateViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var uploadTaskCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"), Encoding.UTF8);

    AssertTrue(interfaceCode.Contains("event EventHandler? LogsChanged", StringComparison.Ordinal), "设备状态服务必须提供日志删除/变更事件。");
    AssertTrue(serviceCode.Contains("LogsChanged?.Invoke", StringComparison.Ordinal), "设备状态日志删除后必须发布日志变更事件。");
    AssertTrue(serviceCode.Contains("UseTran", StringComparison.Ordinal), "设备状态日志、数据库记录和上传任务删除必须使用事务。");
    AssertTrue(serviceCode.Contains("DeviceStatusLocalLogStore.TryRemove", StringComparison.Ordinal), "设备状态日志删除必须同步清理 JSONL 副本。");
    AssertTrue(logViewCode.Contains("_deviceStatusService.LogsChanged +=", StringComparison.Ordinal), "日志管理页必须监听设备状态日志变更事件。");
    AssertTrue(logViewCode.Contains("LoadDeviceStatusLogs();", StringComparison.Ordinal), "日志管理页收到日志变更后必须重新加载当前日期。");
    AssertTrue(stateViewCode.Contains("IDeviceStatusService deviceStatusService", StringComparison.Ordinal), "待上传页必须注入设备状态日志服务。");
    AssertTrue(stateViewCode.Contains("RefreshDeviceStatusLogIndex", StringComparison.Ordinal), "待上传页必须缓存日志来源以支持批量删除。");
    AssertTrue(stateViewCode.Contains("_deviceStatusService.DeleteLogs", StringComparison.Ordinal), "待上传设备状态删除必须通过设备状态日志服务执行。");
    AssertTrue(uploadTaskCode.Contains("_deviceStatusService.NotifyLogsChanged();", StringComparison.Ordinal), "上传任务完成后必须通知日志管理页刷新设备状态日志。");
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

static void DeviceLifecycleCoordinatorReportsChineseSoftwareStatusRemarks()
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

    var poweredOn = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn);
    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);

    AssertEqual("开机", poweredOn.Remark, "软件启动设备状态上报 Remark 必须使用中文“开机”。");
    AssertEqual("停机", stopped.Remark, "软件关闭设备状态上报 Remark 必须使用中文“停机”。");
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

static void ProgramNameRulesBuildAndParseOptionalDescription()
{
    var buildMethod = typeof(ProgramNameRules).GetMethod(
        "BuildProgramName",
        new[] { typeof(string), typeof(string), typeof(int), typeof(string), typeof(string) });
    AssertTrue(buildMethod is not null, "程序名称规则必须提供带可选备注的统一生成方法。");

    var withoutDescription = Convert.ToString(buildMethod!.Invoke(
        null,
        new object?[] { "KFJ123456", "3", 1, "3#J", null })) ?? string.Empty;
    AssertEqual("KFJ123456_CX_3_DH_001_3J", withoutDescription, "无备注时程序名称不能产生尾随下划线。");

    var withDescription = Convert.ToString(buildMethod.Invoke(
        null,
        new object?[] { "KFJ123456", "3", 1, "3#J", "左侧组件" })) ?? string.Empty;
    AssertEqual("KFJ123456_CX_3_DH_001_3J_左侧组件", withDescription, "有备注时程序名称必须追加 inputDescription。");
    AssertFalse(withDescription.Contains('#'), "程序名称中的工号不能包含 #。");

    var parseMethod = typeof(ProgramNameRules).GetMethod("TryParse");
    AssertTrue(parseMethod is not null, "程序名称规则必须提供下载回填所需的解析方法。");

    AssertTrue(ProgramNameRules.TryParse(withoutDescription, out var parsedWithoutDescription), "无备注名称必须可解析。");
    AssertEqual("KFJ123456", parsedWithoutDescription.DeviceId, "解析结果必须保留设备编号。");
    AssertEqual("3", parsedWithoutDescription.ComponentCode, "解析结果必须保留组件代码。");
    AssertEqual(1, parsedWithoutDescription.SequenceNumber, "解析结果必须回填流水号。");
    AssertEqual("3J", parsedWithoutDescription.ProductNum, "解析结果必须返回去除 # 的工号。");
    AssertEqual(string.Empty, parsedWithoutDescription.Description, "无备注名称解析后备注必须为空。");

    AssertTrue(ProgramNameRules.TryParse(withDescription, out var parsedWithDescription), "有备注名称必须可解析。");
    AssertEqual("左侧组件", parsedWithDescription.Description, "解析结果必须回填名称尾段备注。");
    AssertFalse(ProgramNameRules.TryParse("KFJ123456_CX_3_DH_001_3J_", out _), "空备注尾段不应被视为有效名称。");
}

static void ProgramManageDownloadBackfillsNameFields()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var interfaceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Interfaces", "IProgramManageService.cs"), Encoding.UTF8);

    AssertTrue(interfaceCode.Contains("string? description = null", StringComparison.Ordinal), "程序名称服务接口必须支持可选备注参数。");
    AssertTrue(serviceCode.Contains("ProgramNameRules.BuildProgramName", StringComparison.Ordinal), "程序名称服务必须委托统一名称规则生成名称。");
    AssertTrue(serviceCode.Contains("entity.SequenceNumber = parsedName.SequenceNumber", StringComparison.Ordinal), "MES 下载必须回填名称中的流水号。");
    AssertTrue(serviceCode.Contains("entity.Description = parsedName.Description", StringComparison.Ordinal), "MES 下载必须从名称回填 inputDescription 对应的 Description。");
    AssertTrue(serviceCode.Contains("? entity.ProgramName", StringComparison.Ordinal), "编辑已有程序且名称控件为空时必须保留原程序名称。");
    AssertTrue(viewCode.Contains("inputDescription.Text.Trim()", StringComparison.Ordinal), "程序管理页面必须把 inputDescription 传入名称生成。");
    AssertTrue(viewCode.Contains("_editingId <= 0", StringComparison.Ordinal), "新建与编辑程序必须区分名称生成策略。");
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

static void ProgramMesDescriptionChangesTriggerUpdate()
{
    var changes = new[]
    {
        (Original: string.Empty, Edited: "新增描述", Scenario: "新增"),
        (Original: "原始描述", Edited: "修改后的描述", Scenario: "修改"),
        (Original: "原始描述", Edited: string.Empty, Scenario: "删除")
    };
    foreach (var change in changes)
    {
        var original = BuildSyncedProgram();
        original.Description = change.Original;

        var edited = BuildSyncedProgram();
        edited.Description = change.Edited;

        AssertTrue(
            ProgramMesSyncRules.HasMesUploadFieldChanges(original, edited),
            $"Description {change.Scenario}时必须被识别为 MES 字段变化。");
        AssertEqual(
            AppConstants.ProgramSyncActions.Update,
            ProgramMesSyncRules.ResolveCurrentSaveAction(original, edited),
            $"Description {change.Scenario}时必须产生 Update 动作。");
    }

    var whitespaceOriginal = BuildSyncedProgram();
    whitespaceOriginal.Description = "  相同描述  ";
    var whitespaceEdited = BuildSyncedProgram();
    whitespaceEdited.Description = "相同描述";
    AssertFalse(
        ProgramMesSyncRules.HasMesUploadFieldChanges(whitespaceOriginal, whitespaceEdited),
        "Description 只有首尾空格变化时不应产生 MES 更新。");

    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    AssertTrue(serviceCode.Contains("descriptionChanged", StringComparison.Ordinal), "保存服务必须显式判断 inputDescription 是否发生变化。");
    AssertTrue(
        serviceCode.Contains("BuildProgramName(request.ProductNum, request.ComponentCode, request.SequenceNumber, request.LocalRemark)", StringComparison.Ordinal),
        "Description 变化时必须使用当前字段重新生成标准程序名称。");
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
    program.ProgramName = ProgramNameRules.BuildProgramName("D-1", "3", 1, "#3", "左侧组件");
    program.RecipeCode = "99";
    program.ProgramFileName = "1001_P1.JSON";

    var payload = ProgramMesPayloadRules.ToWriteRequest(program, AppConstants.ProgramRemarkActions.Update);
    var json = JsonSerializer.Serialize(payload);
    using var document = JsonDocument.Parse(json);
    var fileType = document.RootElement.GetProperty(nameof(ProgramDataWriteReq.FileType));

    AssertFalse(
        json.Contains(nameof(ProgramDataRes.RecipeCode), StringComparison.OrdinalIgnoreCase),
        "MES 新增/更新程序请求不应包含 RecipeCode。");
    AssertEqual("D-1_CX_3_DH_001_3_左侧组件", payload.ProgramName, "MES 写入请求应携带重建后的标准程序名称。");
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
    var writeFileIndex = applyRequestMethod.IndexOf("WriteProgramContentFile(entity, settings, previousProgramFilePath);", StringComparison.Ordinal);
    AssertTrue(writeFileIndex > contentCheckIndex, "写入本地程序文件必须位于有效设定值判断的条件分支内。");
    AssertTrue(serviceCode.Contains("entity.ProgramFile = string.Empty;", StringComparison.Ordinal), "清理自动文件后必须清空程序文件内容。");
    AssertTrue(serviceCode.Contains("entity.ProgramFileName = string.Empty;", StringComparison.Ordinal), "清理自动文件后必须清空程序文件名。");
}

static void ProgramManageServiceRemovesRenamedAutomaticFileAfterWrite()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    var applyRequestMethod = ExtractMethodText(
        serviceCode,
        "    private void ApplyRequest(BizProgram entity, SaveProgramReq request)",
        "    private AppSettings CurrentSettings");
    var writeMethod = ExtractMethodText(
        serviceCode,
        "    private static void WriteProgramContentFile(",
        "    private static void ClearProgramContentFile(");

    AssertTrue(
        applyRequestMethod.Contains("WriteProgramContentFile(entity, settings, previousProgramFilePath);", StringComparison.Ordinal),
        "写入改名后的自动程序文件时必须传入旧自动文件路径。");
    AssertTrue(
        writeMethod.Contains("!string.Equals(previousProgramFilePath, filePath, StringComparison.OrdinalIgnoreCase)", StringComparison.Ordinal),
        "只有新旧自动文件路径不同时才允许删除旧文件。");

    var writeIndex = writeMethod.IndexOf("File.WriteAllText(filePath", StringComparison.Ordinal);
    var deleteIndex = writeMethod.IndexOf("DeleteProgramContentFile(previousProgramFilePath);", StringComparison.Ordinal);
    AssertTrue(writeIndex >= 0 && deleteIndex > writeIndex, "必须先成功写入新文件，再删除旧名称对应的自动文件。");
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

static void SystemSettingViewLocksDeviceManagementDuringUnfinishedTasks()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("grpDeviceConfig.Enabled = !HasAnyUnfinishedTask();", StringComparison.Ordinal),
        "任一工位存在未完工任务时，系统设置页必须禁用整个设备管理模块。");
    AssertTrue(
        viewCode.Contains("protected override void OnVisibleChanged(EventArgs e)", StringComparison.Ordinal),
        "系统设置页重新显示时必须刷新设备管理模块的可编辑状态。");
    AssertTrue(
        viewCode.Contains("RefreshDeviceManagementEnabled();", StringComparison.Ordinal),
        "系统设置页加载和重新显示时必须调用设备管理状态刷新方法。");
    AssertTrue(
        viewCode.Contains("private bool CanSaveDeviceManagementChange(", StringComparison.Ordinal),
        "保存入口必须提供设备管理字段变更的防御性校验。");
    AssertTrue(
        viewCode.Contains("!HasDeviceIdentityChanged(previousSettings, newSettings) || !HasAnyUnfinishedTask()", StringComparison.Ordinal),
        "只有未完工期间修改设备管理字段时才应阻止保存，其它设置仍可保存。");
    AssertTrue(
        CountOccurrences(viewCode, "CanSaveDeviceManagementChange(previousSettings, settings)") >= 2,
        "整体保存和手动同步设备两个入口都必须执行设备管理变更校验。");
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
    FakeAppSettingsService? appSettingsService = null,
    FakeUploadTaskService? uploadTaskService = null,
    FakeProductionReportFileService? reportFileService = null)
{
    return new WeldTaskService(
        null!,
        mesProvider,
        appSettingsService ?? new FakeAppSettingsService(),
        operationLogService,
        new FakeLocalizationService(),
        uploadTaskService ?? new FakeUploadTaskService(),
        new FakeCenterProductForwardingService(),
        reportFileService ?? new FakeProductionReportFileService(),
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

static void StationDisplayNamesHaveLocalizedDualStationRules()
{
    var defaults = new AppSettings();
    AssertEqual("左", defaults.Station1DisplayName, "工位 1 默认显示名称应为“左”。");
    AssertEqual("右", defaults.Station2DisplayName, "工位 2 默认显示名称应为“右”。");

    var singleStation = StationDisplayNameRules.NormalizeAndValidate(false, "  ", "  ");
    AssertEqual(string.Empty, singleStation.Station1, "单工位模式不应强制填写名称。");
    AssertEqual(string.Empty, singleStation.Station2, "单工位模式不应强制填写名称。");

    var normalized = StationDisplayNameRules.NormalizeAndValidate(true, "  Left  ", "  Right  ");
    AssertEqual("Left", normalized.Station1, "工位 1 名称应去除首尾空格。");
    AssertEqual("Right", normalized.Station2, "工位 2 名称应去除首尾空格。");
    AssertThrows<ArgumentException>(() => StationDisplayNameRules.NormalizeAndValidate(true, "", "Right"), "双工位模式应拒绝空名称。");
    AssertThrows<ArgumentException>(() => StationDisplayNameRules.NormalizeAndValidate(true, "Same", " same "), "双工位模式应不区分大小写地拒绝重复名称。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    AssertTrue(viewCode.Contains("stationDisplayNameLayout.Visible = chkEnableDualStation.Checked;", StringComparison.Ordinal), "界面应仅在启用双工位时显示名称输入区。");

    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);
    foreach (var key in new[]
    {
        "system.label.station1_display_name",
        "system.label.station2_display_name",
        "system.placeholder.station_display_name",
        "system.message.station_display_name_required",
        "system.message.station_display_name_duplicate"
    })
    {
        AssertTrue(zhResources.Contains(key, StringComparison.Ordinal), $"中文资源必须包含 {key}。");
        AssertTrue(enResources.Contains(key, StringComparison.Ordinal), $"英文资源必须包含 {key}。");
    }
}

static void StationDisplayNamesLoadLegacyDefaultsAndCollapseHiddenRow()
{
    var loaded = StationDisplayNameRules.NormalizeForLoad(true, "  ", null);
    AssertEqual("左", loaded.Station1, "加载旧双工位配置时，空白工位 1 名称应回填“左”。");
    AssertEqual("右", loaded.Station2, "加载旧双工位配置时，空白工位 2 名称应回填“右”。");
    var collisionLoaded = StationDisplayNameRules.NormalizeForLoad(true, "", "左");
    AssertEqual("左", collisionLoaded.Station1, "旧数据回填与现有名称冲突时不应导致加载失败。");
    AssertEqual("右", collisionLoaded.Station2, "旧数据回填冲突时应恢复为可用的默认映射。");
    AssertThrows<ArgumentException>(() => StationDisplayNameRules.NormalizeForLoad(true, "A", " a "), "两个原始名称均非空时，加载流程也必须拒绝规范化后的重复名称。");
    AssertThrows<ArgumentException>(() => StationDisplayNameRules.NormalizeAndValidate(true, "  ", "右"), "用户主动保存双工位空名称时仍必须拒绝。");

    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "AppSettingsService.cs"), Encoding.UTF8);
    AssertTrue(serviceCode.Contains("Normalize(settings, useLegacyStationNameFallback: true);", StringComparison.Ordinal), "Get 加载流程必须启用旧数据名称回填。");
    AssertTrue(serviceCode.Contains("Normalize(settings, useLegacyStationNameFallback: false);", StringComparison.Ordinal), "Save 必须保持用户输入的严格校验。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    AssertFalse(viewCode.Contains("RowStyles[2]", StringComparison.Ordinal), "code-behind 不应直接修改 Designer 行样式。");
    AssertFalse(viewCode.Contains("StationDisplayNameRowHeight", StringComparison.Ordinal), "code-behind 不应保留名称行高度常量。");

    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    var productionLocation = ParseDesignerPointY(designerCode, "grpProductionConfig.Location");
    var productionHeight = ParseDesignerSizeHeight(designerCode, "grpProductionConfig.Size");
    var mesLocation = ParseDesignerPointY(designerCode, "grpMesConfig.Location");
    var mesHeight = ParseDesignerSizeHeight(designerCode, "grpMesConfig.Size");
    var centerServerLocation = ParseDesignerPointY(designerCode, "grpCenterServerConfig.Location");
    AssertTrue(productionLocation + productionHeight <= mesLocation, "生产配置与 MES 配置在提交布局中不得重叠。");
    AssertTrue(mesLocation + mesHeight <= centerServerLocation, "MES 配置与中心服务器配置在提交布局中不得重叠。");
    AssertTrue(designerCode.Contains("tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.AutoSize));", StringComparison.Ordinal), "Designer 必须将工位名称行设为 AutoSize，使隐藏容器时自动折叠。");
}

static int ParseDesignerPointY(string code, string propertyName)
{
    var match = System.Text.RegularExpressions.Regex.Match(code, $@"{System.Text.RegularExpressions.Regex.Escape(propertyName)} = new Point\(\d+, (\d+)\);");
    return match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : throw new InvalidOperationException($"未找到 {propertyName}。");
}

static int ParseDesignerSizeHeight(string code, string propertyName)
{
    var match = System.Text.RegularExpressions.Regex.Match(code, $@"{System.Text.RegularExpressions.Regex.Escape(propertyName)} = new Size\(\d+, (\d+)\);");
    return match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : throw new InvalidOperationException($"未找到 {propertyName}。");
}

static BizWeldTask BuildReportTask(DateTime startTime, DateTime? endTime)
{
    return new BizWeldTask
    {
        Id = 31001,
        DeviceId = "DEVICE-01",
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        ProductNum = "164#J",
        DrawingNo = "DR-001",
        Batch = "BATCH-01",
        SN = "FLOW-001",
        Spec = "SPEC-01",
        ProductModel = "MODEL-01",
        ProcessNo = "OP10",
        StartAmount = 20,
        ActualQty = 18,
        QualifiedQty = 17,
        StartTime = startTime,
        EndTime = endTime,
        UserNumber = "U001",
        EndOperatorNumber = "U999"
    };
}

static BizProductionReportFile BuildReportFile(
    int id,
    int taskId,
    string filePath,
    DateTime updatedTime,
    string? fileCode = null,
    string? fileFormat = null,
    int? mesFileType = null)
{
    return new BizProductionReportFile
    {
        Id = id,
        TaskId = taskId,
        FileCode = fileCode ?? ProductionConstants.ReportFileCodes.Spreadsheet,
        FileFormat = fileFormat ?? "XLSX",
        MesFileType = mesFileType ?? ProductionConstants.MesFileTypes.ReportFile,
        FilePath = filePath,
        UpdatedTime = updatedTime
    };
}

static BizWeldPointRecord BuildReportPoint(
    int taskId,
    int stationNo,
    string productNo,
    int sequenceNo,
    string pointResult)
{
    return new BizWeldPointRecord
    {
        TaskId = taskId,
        StationNo = stationNo,
        ProductNo = productNo,
        SequenceNo = sequenceNo,
        TouchNo = sequenceNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
        TestResult = pointResult,
        OperatorNo = "POINT-OPERATOR",
        Ts = new DateTime(2026, 7, 16, 8, 10, sequenceNo, DateTimeKind.Local),
        RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["max_electric"] = (1.20m + sequenceNo / 100m).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["max_electric_upper"] = "2.00",
            ["max_electric_lower"] = "1.00",
            ["product_result"] = ProductionConstants.TestResults.PreWeldNg
        })
    };
}

static IReadOnlyList<CenterProductReportColumnDto> BuildCenterDynamicReportColumns(
    BizSchemeDetail detail,
    DimTestItem item)
{
    var method = typeof(CenterProductForwardingService).GetMethod(
        "BuildDynamicReportColumns",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
        binder: null,
        types: [typeof(BizSchemeDetail), typeof(DimTestItem)],
        modifiers: null);
    AssertTrue(method is not null, "中心转发服务必须保留可验证的动态列纯规则入口。");
    var result = method!.Invoke(null, [detail, item]) as IEnumerable<CenterProductReportColumnDto>;
    AssertTrue(result is not null, "中心动态列规则必须返回列定义。");
    return result!.ToList();
}

static CenterProductReportRequest BuildCenterProductRequest(
    AppSettings settings,
    BizWeldTask task,
    int stationNo,
    IReadOnlyList<BizWeldPointRecord> records)
{
    var serviceType = typeof(CenterProductForwardingService);
    var service = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(serviceType);
    var method = serviceType.GetMethod(
        "BuildRequest",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(method is not null, "中心产品请求必须保留可验证的请求构造入口。");
    var request = method!.Invoke(service, [settings, task, stationNo, records]) as CenterProductReportRequest;
    AssertTrue(request is not null, "中心产品请求构造入口必须返回请求对象。");
    return request!;
}

static T ReadCenterRequestProperty<T>(CenterProductReportRequest request, string propertyName)
{
    var property = typeof(CenterProductReportRequest).GetProperty(propertyName);
    AssertTrue(property is not null, $"中心产品请求必须公开 {propertyName}。");
    var value = property!.GetValue(request);
    return value is null ? default! : (T)value;
}

static void SetCenterRequestProperty<T>(CenterProductReportRequest request, string propertyName, T value)
{
    var property = typeof(CenterProductReportRequest).GetProperty(propertyName);
    AssertTrue(property is not null, $"中心产品请求必须公开 {propertyName}。");
    property!.SetValue(request, value);
}

static CenterProductReportRequest BuildCenterWorkbookRequest(
    string deviceId,
    string workOrder,
    DateTime startTime,
    DateTime? endTime,
    int qualifiedQty,
    bool enableDualStation,
    int stationNo,
    string stationName,
    string productNo,
    bool includeDynamicColumn,
    bool isTaskFinishUpdate,
    int pointCount,
    IReadOnlyList<CenterProductReportColumnDto>? dynamicColumns = null,
    string pointNoHeader = "拍照编号",
    string pointResultHeader = "拍照结果")
{
    var columns = new List<CenterProductReportColumnDto>();
    if (enableDualStation)
    {
        columns.Add(new CenterProductReportColumnDto
        {
            Key = CenterProductReportFormat.ColumnStationNo,
            Title = "工位",
            MergeByProduct = true
        });
    }

    columns.Add(new CenterProductReportColumnDto
    {
        Key = CenterProductReportFormat.ColumnProductNo,
        Title = "产品编号",
        MergeByProduct = true
    });
    columns.Add(new CenterProductReportColumnDto
    {
        Key = CenterProductReportFormat.ColumnTouchNo,
        Title = pointNoHeader,
        MergeByProduct = false
    });
    columns.Add(new CenterProductReportColumnDto
    {
        Key = CenterProductReportFormat.ColumnTouchResult,
        Title = pointResultHeader,
        MergeByProduct = false
    });
    if (dynamicColumns is not null)
    {
        columns.AddRange(dynamicColumns);
    }
    else if (includeDynamicColumn)
    {
        columns.Add(new CenterProductReportColumnDto
        {
            Key = "max_electric",
            Title = "峰值电流保存值",
            MergeByProduct = false
        });
    }

    columns.Add(new CenterProductReportColumnDto
    {
        Key = CenterProductReportFormat.ColumnProductResult,
        Title = "产品结果",
        MergeByProduct = true
    });

    var request = new CenterProductReportRequest
    {
        DeviceId = deviceId,
        DeviceName = "自动焊设备",
        SystemType = CenterServerConstants.SystemTypes.Electromagnetic,
        StationNo = stationNo,
        WorkOrder = workOrder,
        Batch = "BATCH-01",
        Quantity = 20,
        PartName = "引出线",
        ProcessNo = "OP10",
        OperatorNo = "U001",
        ProductJobNo = "164#J",
        ProductNo = productNo,
        ProductModel = "MODEL-01",
        ProductResult = ProductionConstants.TestResults.Ok,
        CompletedAt = endTime ?? startTime.AddMinutes(1),
        ReportColumns = isTaskFinishUpdate ? [] : columns,
        Points = Enumerable.Range(1, pointCount)
            .Select(index => new CenterProductReportPointDto
            {
                SequenceNo = index,
                TouchNo = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                TestResult = ProductionConstants.TestResults.Ok,
                CollectedAt = startTime.AddMinutes(index),
                OperatorNo = "POINT-OPERATOR",
                RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["max_electric"] = (1.2m + index / 100m).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["max_electric_result"] = index % 2 == 0
                        ? ProductionConstants.TestResults.Ng
                        : ProductionConstants.TestResults.Ok,
                    ["report_only"] = "不得显示",
                    ["mes_only"] = "不得显示"
                })
            })
            .ToList()
    };

    SetCenterRequestProperty(request, "StationName", stationName);
    SetCenterRequestProperty(request, "DrawingNo", "DR-001");
    SetCenterRequestProperty(request, "Spec", "SPEC-01");
    SetCenterRequestProperty(request, "StartTime", startTime);
    SetCenterRequestProperty(request, "EndTime", endTime);
    SetCenterRequestProperty(request, "QualifiedQty", qualifiedQty);
    SetCenterRequestProperty(request, "IsTaskFinishUpdate", isTaskFinishUpdate);
    return request;
}

static string CreateCenterReportFixtureDirectory()
{
    var outputDirectory = Path.Combine(Path.GetTempPath(), "AutoWeldSystem.Tests", "CenterReports", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(outputDirectory);
    return outputDirectory;
}

static CenterServerSettingsService CreateCenterServerSettingsService(string outputDirectory)
{
    var settingsService = new CenterServerSettingsService(new ConfigurationBuilder().Build());
    settingsService.Save(new CenterServerLocalSettings
    {
        DataDirectory = outputDirectory,
        LogDirectory = Path.Combine(outputDirectory, "Logs"),
        OfflineTimeoutSeconds = CenterServerConstants.DefaultOfflineTimeoutSeconds
    });
    return settingsService;
}

static string WriteCenterReportWorkbook(string outputDirectory, CenterProductReportRequest request)
{
    var reportPath = new CenterProductReportFileStore().Upsert(outputDirectory, request);
    AssertTrue(File.Exists(reportPath), "中心文件存储 seam 必须生成真实 XLSX 文件。");
    return reportPath;
}

static int CountCenterDataRows(XLWorkbook workbook)
{
    var worksheet = workbook.Worksheet(CenterProductReportFormat.DataWorksheetName);
    return Math.Max(0, (worksheet.LastRowUsed()?.RowNumber() ?? 1) - 1);
}

static void DeleteDirectoryIfExists(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}

/// <summary>
/// 在全部工作簿断言通过后发布视觉样例。
/// 同卷临时文件可保证替换失败时旧样例保持完整，并避免留下半写入的 XLSX。
/// </summary>
static void PublishReportArtifact(string sourcePath, string destinationPath)
{
    var fullDestinationPath = Path.GetFullPath(destinationPath);
    var destinationDirectory = Path.GetDirectoryName(fullDestinationPath)!;
    Directory.CreateDirectory(destinationDirectory);
    var pendingPath = Path.Combine(
        destinationDirectory,
        $".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.pending");

    try
    {
        File.Copy(sourcePath, pendingPath, overwrite: true);
        File.Move(pendingPath, fullDestinationPath, overwrite: true);
    }
    finally
    {
        File.Delete(pendingPath);
    }
}

/// <summary>
/// 使用生产服务的“已解析工位配置”入口生成真实 XLSX，验证双工位列并集和逐行工位隔离。
/// </summary>
static string GenerateStationSpecificReportWorkbook(
    AppSettings settings,
    BizWeldTask task,
    IReadOnlyList<BizWeldPointRecord> records,
    IReadOnlyList<(int StationNo, BizProductProcessConfig Config, DimTestItem Item, BizSchemeDetail Detail)> stationDefinitions)
{
    var serviceType = typeof(ProductionReportFileService);
    var service = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(serviceType);
    var settingsField = serviceType.GetField("_currentSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(settingsField is not null, "生产报表服务必须保留当前设置快照。");
    settingsField!.SetValue(service, settings);

    var schemeReportItemType = GetNestedReportType(serviceType, "SchemeReportItem");
    var resolvedStationType = GetNestedReportType(serviceType, "ResolvedStationReportConfig");
    var resolvedStations = CreateGenericList(resolvedStationType);
    foreach (var definition in stationDefinitions)
    {
        var schemeItems = CreateGenericList(schemeReportItemType);
        schemeItems.Add(Activator.CreateInstance(schemeReportItemType, definition.Item, definition.Detail)
            ?? throw new InvalidOperationException("无法构造工位专属动态项。"));
        resolvedStations.Add(Activator.CreateInstance(
                resolvedStationType,
                definition.StationNo,
                definition.Config,
                schemeItems)
            ?? throw new InvalidOperationException("无法构造已解析工位报表配置。"));
    }

    var buildSchema = serviceType.GetMethod(
        "BuildReportSchemaForStations",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(buildSchema is not null, "生产报表服务必须提供已解析工位配置的 schema 构造入口。");
    var schema = buildSchema!.Invoke(null, [resolvedStations])
        ?? throw new InvalidOperationException("工位配置 schema 构造入口不得返回空值。");

    var outputDirectory = Path.Combine(Path.GetTempPath(), "AutoWeldSystem.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(outputDirectory);
    var filePath = Path.Combine(outputDirectory, "production-report-station-union.xlsx");
    var writeMethod = serviceType.GetMethod("WriteXlsx", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(writeMethod is not null, "生产报表服务必须保留 XLSX 写入入口。");
    writeMethod!.Invoke(service, [filePath, schema, records, task]);
    AssertTrue(File.Exists(filePath), "工位配置并集入口必须生成真实 XLSX 文件。");
    return filePath;
}

/// <summary>
/// 直接调用生产报表服务的内部写入路径生成真实 XLSX，避免回归测试依赖 MySQL。
/// 私有类型只用于搭建现有生产入口所需的 schema，最终断言始终基于 ClosedXML 重新打开的文件。
/// </summary>
static string GenerateReportWorkbook(
    AppSettings settings,
    BizWeldTask task,
    IReadOnlyList<BizWeldPointRecord> records,
    int extraDynamicColumnCount = 0,
    string pointNoHeader = "拍照编号",
    string pointResultHeader = "拍照结果",
    string? outputFilePath = null)
{
    var serviceType = typeof(ProductionReportFileService);
    var service = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(serviceType);
    var settingsField = serviceType.GetField("_currentSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(settingsField is not null, "生产报表服务必须保留当前设置快照。");
    settingsField!.SetValue(service, settings);

    var reportColumnType = GetNestedReportType(serviceType, "ReportColumn");
    var schemeReportItemType = GetNestedReportType(serviceType, "SchemeReportItem");
    var reportDisplayOptionsType = GetNestedReportType(serviceType, "ReportDisplayOptions");
    var reportSchemaType = GetNestedReportType(serviceType, "ReportSchema");
    var columns = CreateGenericList(reportColumnType);
    AddReportColumn(columns, reportColumnType, "station_no", "工位", mergeByProduct: true);
    AddReportColumn(columns, reportColumnType, "product_no", "产品编号", mergeByProduct: true);
    AddReportColumn(columns, reportColumnType, "touch_no", pointNoHeader, mergeByProduct: false);
    AddReportColumn(columns, reportColumnType, "touch_result", pointResultHeader, mergeByProduct: false);

    var detail = new BizSchemeDetail
    {
        EnableActual = true,
        ReportActual = true,
        ActualHeader = "峰值电流",
        EnableUpper = true,
        SaveUpper = true,
        ReportUpper = false,
        UpperHeader = "峰值电流上限",
        EnableLower = true,
        MesLower = true,
        ReportLower = false,
        LowerHeader = "峰值电流下限"
    };
    var item = new DimTestItem
    {
        ItemId = 1,
        ItemName = "峰值电流",
        ActualExpression = "0:F-0",
        UpperExpression = "0:F-4",
        LowerExpression = "0:F-8"
    };
    var schemeItem = Activator.CreateInstance(schemeReportItemType, item, detail)
        ?? throw new InvalidOperationException("无法构造生产报表动态项。");
    var buildItemColumns = serviceType.GetMethod("BuildItemColumns", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(buildItemColumns is not null, "生产报表服务必须保留动态列构造入口。");
    var dynamicColumns = (System.Collections.IEnumerable?)buildItemColumns!.Invoke(null, [schemeItem]);
    AssertTrue(dynamicColumns is not null, "动态列构造入口必须返回列定义。");
    foreach (var column in dynamicColumns!)
    {
        columns.Add(column);
    }

    for (var index = 0; index < extraDynamicColumnCount; index++)
    {
        AddReportColumn(
            columns,
            reportColumnType,
            $"extra_dynamic_{index + 1}",
            $"扩展动态列{index + 1}",
            mergeByProduct: false);
    }

    AddReportColumn(columns, reportColumnType, "product_result", "产品结果", mergeByProduct: true);
    var schemeItems = CreateGenericList(schemeReportItemType);
    schemeItems.Add(schemeItem);
    var displayOptions = Activator.CreateInstance(reportDisplayOptionsType, pointNoHeader, pointResultHeader)
        ?? throw new InvalidOperationException("无法构造生产报表显示配置。");
    var schema = Activator.CreateInstance(reportSchemaType, columns, schemeItems, displayOptions)
        ?? throw new InvalidOperationException("无法构造生产报表 schema。");

    var resolvedOutputPath = string.IsNullOrWhiteSpace(outputFilePath)
        ? null
        : Path.GetFullPath(outputFilePath);
    var outputDirectory = resolvedOutputPath is null
        ? Path.Combine(Path.GetTempPath(), "AutoWeldSystem.Tests", Guid.NewGuid().ToString("N"))
        : Path.GetDirectoryName(resolvedOutputPath)!;
    Directory.CreateDirectory(outputDirectory);
    var fileName = settings.EnableDualStation
        ? "production-report-dual-station.xlsx"
        : "production-report-single-station.xlsx";
    var filePath = resolvedOutputPath ?? Path.Combine(outputDirectory, fileName);
    var writeMethod = serviceType.GetMethod("WriteXlsx", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(writeMethod is not null, "生产报表服务必须保留 XLSX 写入入口。");
    writeMethod!.Invoke(service, [filePath, schema, records, task]);
    AssertTrue(File.Exists(filePath), "生产报表写入入口必须生成真实 XLSX 文件。");
    return filePath;
}

static Type GetNestedReportType(Type serviceType, string typeName)
{
    return serviceType.GetNestedType(typeName, System.Reflection.BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"生产报表服务缺少内部类型 {typeName}。");
}

static System.Collections.IList CreateGenericList(Type itemType)
{
    var listType = typeof(List<>).MakeGenericType(itemType);
    return (System.Collections.IList)(Activator.CreateInstance(listType)
        ?? throw new InvalidOperationException($"无法构造 {listType.Name}。"));
}

static void AddReportColumn(
    System.Collections.IList columns,
    Type reportColumnType,
    string key,
    string title,
    bool mergeByProduct)
{
    columns.Add(Activator.CreateInstance(reportColumnType, key, title, mergeByProduct)
        ?? throw new InvalidOperationException($"无法构造生产报表列 {title}。"));
}

static string[] ReadHeaderRow(IXLWorksheet worksheet, int rowNumber)
{
    var lastCell = worksheet.Row(rowNumber).LastCellUsed();
    AssertTrue(lastCell is not null, $"第 {rowNumber} 行必须存在报表明细表头。");
    return worksheet.Range(rowNumber, 1, rowNumber, lastCell!.Address.ColumnNumber)
        .Cells()
        .Select(cell => cell.GetString())
        .ToArray();
}

static void AssertMerged(IXLWorksheet worksheet, string rangeAddress, string message)
{
    AssertTrue(
        worksheet.MergedRanges.Any(range => string.Equals(range.RangeAddress.ToString(), rangeAddress, StringComparison.OrdinalIgnoreCase)),
        $"{message} Missing={rangeAddress}");
}

/// <summary>
/// 验证客户模板固定 A:J 区域的四行任务表头合并结构。
/// 动态列超过 J 的扩展行为由独立回归测试覆盖，本矩阵只验证三份代表样例的模板基线。
/// </summary>
static void AssertTemplateHeaderMerges(IXLWorksheet worksheet)
{
    foreach (var mergedRange in new[]
    {
        "A1:C1", "D1:F1", "G1:H1", "I1:J1",
        "A3:C3", "D3:F3", "G3:J3",
        "A5:C5", "D5:F5", "G5:J5",
        "A7:C7", "D7:F7", "G7:J7"
    })
    {
        AssertMerged(worksheet, mergedRange, "代表样例必须保持客户模板 A:J 合并结构。");
    }
}

static void AssertSourceOrder(string source, string firstMarker, string secondMarker, string message)
{
    var firstIndex = source.IndexOf(firstMarker, StringComparison.Ordinal);
    var secondIndex = source.IndexOf(secondMarker, StringComparison.Ordinal);
    AssertTrue(firstIndex >= 0, $"{message} MissingFirst={firstMarker}");
    AssertTrue(secondIndex >= 0, $"{message} MissingSecond={secondMarker}");
    AssertTrue(firstIndex < secondIndex, message);
}

static void DeleteReportFixture(string filePath)
{
    var qaDirectory = Environment.GetEnvironmentVariable("AUTOWELD_REPORT_QA_DIR");
    if (!string.IsNullOrWhiteSpace(qaDirectory))
    {
        Directory.CreateDirectory(qaDirectory);
        File.Copy(filePath, Path.Combine(qaDirectory, Path.GetFileName(filePath)), overwrite: true);
    }

    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
    {
        Directory.Delete(directory, recursive: true);
    }
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

static void SetOptionalStringProperty(object target, string propertyName, string? value)
{
    var property = target.GetType().GetProperty(propertyName);
    AssertTrue(property is not null, $"{target.GetType().Name} 必须包含 {propertyName} 属性。");
    property!.SetValue(target, value);
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
    }
}

static void AssertNearlyEqual(double expected, double actual, double tolerance, string message)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}, Tolerance={tolerance}");
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

sealed class FakeCenterProductForwardingService : ICenterProductForwardingService
{
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void EnqueueCompletedProduct(
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records)
    {
    }

    public void EnqueueTaskFinishUpdate(BizWeldTask task)
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakeCenterProductReportIngestSideEffects : ICenterProductReportIngestSideEffects
{
    public List<(string DataDirectory, string DeviceId, CenterProductReportRequest Request)> Calls { get; } = new();

    public Task ApplyAsync(
        string dataDirectory,
        string deviceId,
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((dataDirectory, deviceId, request));
        return Task.CompletedTask;
    }
}

sealed class FakeUploadTaskService : IUploadTaskService
{
    public event EventHandler<UploadTaskStatusChangedEventArgs>? TaskStatusChanged;

    public List<BizUploadTask> Enqueued { get; } = new();

    public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false) => Array.Empty<UploadTaskSummary>();

    public IReadOnlyList<UploadTaskSummary> GetProcessParameterRows(bool includeCompleted = false) => Array.Empty<UploadTaskSummary>();

    public UploadTaskSummary? GetById(int id) => null;

    public BizUploadTask EnqueueOrUpdate(BizUploadTask task)
    {
        Enqueued.Add(task);
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
    public bool ShouldUploadReportFileResult { get; set; }

    public int GenerateCallCount { get; private set; }

    public BizProductionReportFile GeneratedReport { get; set; } = new();

    public BizProductionReportFile GenerateXlsxReport(BizWeldTask task)
    {
        GenerateCallCount++;
        return GeneratedReport;
    }

    public bool ShouldUploadReportFile(BizWeldTask task) => ShouldUploadReportFileResult;
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

    public event EventHandler? LogsChanged;

    public void NotifyLogsChanged() => LogsChanged?.Invoke(this, EventArgs.Empty);

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

    public BizUploadTask EnsurePendingUploadTask(BizDeviceStatusLog log)
    {
        return new BizUploadTask
        {
            Id = log.Id,
            TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
            BusinessId = $"device-status:{log.Id}",
            Status = log.ReportStatus
        };
    }

    public string GetLogDirectory() => string.Empty;

    public int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs)
    {
        var logIds = logs.Select(log => log.Id).ToHashSet();
        var deletedCount = Logs.Count(log => logIds.Contains(log.Id));
        Logs.RemoveAll(log => logIds.Contains(log.Id));
        LogsChanged?.Invoke(this, EventArgs.Empty);
        return deletedCount;
    }

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
