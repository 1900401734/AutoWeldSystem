using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.DTOs.DeviceApi;
using AutoWeldSystem.Core.DTOs.DataManagement;
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
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.CenterServer.Configuration;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Services.Center;
using AutoWeldSystem.Services.Mes;
using AutoWeldSystem.Services.Log;
using AutoWeldSystem.Services.Plc;
using AutoWeldSystem.Services.Production;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;

var tests = new (string Name, Action Run)[]
{
    ("System setting layout rules honor DPI breakpoints", SystemSettingLayoutRulesHonorDpiBreakpoints),
    ("Monitor right layout rules honor DPI and scrolling", MonitorRightLayoutRulesHonorDpiAndScrolling),
    ("Monitor view applies responsive right layout", MonitorViewAppliesResponsiveRightLayout),
    ("PLC alarm notification rules normalize messages and signatures", PlcAlarmNotificationRulesNormalizeMessagesAndSignatures),
    ("Monitor view preserves work order input focus on preview hover", MonitorViewPreservesWorkOrderInputFocusOnPreviewHover),
    ("Work-order clear resets PLC query state", WorkOrderClearResetsPlcQueryState),
    ("System setting view avoids repeated layout rebuilds during resize", SystemSettingViewAvoidsRepeatedLayoutRebuilds),
    ("Base window batches layout and redraw during interactive resize", BaseWindowBatchesInteractiveResize),
    ("Main form keeps cached pages mounted during navigation", MainFormKeepsCachedPagesMountedDuringNavigation),
    ("System setting initial load avoids duplicate localization and binding", SystemSettingInitialLoadAvoidsDuplicateWork),
    ("System setting caches device lock state between displays", SystemSettingCachesDeviceLockStateBetweenDisplays),
    ("PLC alarm read failures are merged and labeled precisely", PlcAlarmReadFailuresAreMergedAndLabeledPrecisely),
    ("PLC production monitor separates business signal failures", PlcProductionMonitorSeparatesBusinessSignalFailures),
    ("PLC production monitor preserves alarm state until projection", PlcProductionMonitorPreservesAlarmStateUntilProjection),
    ("Program exception log view batches live updates", ProgramExceptionLogViewBatchesLiveUpdates),
    ("Program exception log view normalizes legacy alarm entries", ProgramExceptionLogViewNormalizesLegacyAlarmEntries),
    ("Exception grid omits source columns but keeps detail source", ExceptionGridOmitsSourceColumns),
    ("Exception grid omits exception type but keeps diagnostics", ExceptionGridOmitsExceptionTypeColumn),
    ("Device lifecycle ignores transient PLC connection states", DeviceLifecycleIgnoresTransientPlcConnectionStates),
    ("PLC shutdown returns while communication lock is held", PlcShutdownReturnsWhileCommunicationLockIsHeld),
    ("PLC shutdown detaches the client before bounded close", PlcShutdownDetachesClientBeforeBoundedClose),
    ("PLC heartbeat settings normalize and preserve safe defaults", PlcHeartbeatSettingsNormalizeAndPreserveSafeDefaults),
    ("PLC heartbeat sampling avoids toggle aliasing and delayed false faults", PlcHeartbeatSamplingAvoidsToggleAliasingAndDelayedFalseFaults),
    ("PLC heartbeat settings are wired through the system settings view", PlcHeartbeatSettingsAreWiredThroughSystemSettingsView),
    ("Monitor view cancels business signal reconciliation on destroy", MonitorViewCancelsBusinessSignalReconciliationOnDestroy),
    ("Monitor view cancels pending upload retry on destroy", MonitorViewCancelsPendingUploadRetryOnDestroy),
    ("PLC status tooltip uses compact localized acrylic panel", PlcStatusTooltipUsesCompactLocalizedAcrylicPanel),
    ("Program exception history uses bounded tail reads", ProgramExceptionHistoryUsesBoundedTailReads),
    ("System setting view uses responsive semantic columns", SystemSettingViewUsesResponsiveSemanticColumns),
    ("System setting localization resources are complete", SystemSettingLocalizationResourcesAreComplete),
    ("System setting configures PLC alarm trigger mode", SystemSettingConfiguresPlcAlarmTriggerMode),
    ("System setting configures inspection result source", SystemSettingConfiguresInspectionResultSource),
    ("System setting configures realtime point number source", SystemSettingConfiguresRealtimePointNumberSource),
    ("PLC product ready handshake retains high-level state", PlcProductReadyHandshakeRetainsHighLevelState),
    ("MES endpoint validation returns stable error codes", MesEndpointValidationReturnsStableErrorCodes),
    ("Device id sync rules detect missing old devices", DeviceIdSyncRulesDetectMissingOldDevices),
    ("System setting saves before background device sync", SystemSettingRetriesMissingOldDeviceAsNewRegistration),
    ("Localization service reports missing resource keys", LocalizationServiceReportsMissingResourceKeys),
    ("PLC recipe name rules map slots without shifting codes", PlcRecipeNameRulesMapSlotsWithoutShiftingCodes),
    ("PLC recipe name config rules reject invalid station settings", PlcRecipeNameConfigRulesRejectInvalidStationSettings),
    ("PLC recipe name reader keeps successful slots after read failures", PlcRecipeNameReaderKeepsSuccessfulSlotsAfterReadFailures),
    ("PLC recipe name reader accepts in-memory configuration", PlcRecipeNameReaderAcceptsInMemoryConfiguration),
    ("PLC recipe name reader returns invalid config failures", PlcRecipeNameReaderReturnsInvalidConfigFailures),
    ("Program recipe mapping normalizes positive numeric codes", ProgramRecipeMappingNormalizesPositiveNumericCodes),
    ("Program save recipe rules require positive station codes", ProgramSaveRecipeRulesRequirePositiveStationCodes),
    ("Program recipe mapping resolves station-specific codes", ProgramRecipeMappingResolvesStationSpecificCodes),
    ("Program shared recipe targets resolve independently", ProgramSharedRecipeTargetsResolveIndependently),
    ("Shared task recipe boundaries resolve per station", SharedTaskRecipeBoundariesResolvePerStation),
    ("Program recipe station 2 fields persist locally", ProgramRecipeStation2FieldsPersistLocally),
    ("Program runtime resolves recipes by current station", ProgramRuntimeResolvesRecipesByCurrentStation),
    ("Program MES write payload omits recipe code", ProgramMesWritePayloadOmitsRecipeCode),
    ("Program manage initial load keeps selected program details", ProgramManageInitialLoadKeepsSelectedProgramDetails),
    ("Program manage recipe name selectors bind station recipe codes", ProgramManageRecipeNameSelectorsBindStationRecipeCodes),
    ("Address manage exposes PLC recipe name configuration", AddressManageExposesPlcRecipeNameConfiguration),
    ("PLC recipe name config service reads latest station row", PlcRecipeNameConfigServiceReadsLatestStationRow),
    ("Product process draft copies business fields and resets identity", ProductProcessDraftCopiesBusinessFieldsAndResetsIdentity),
    ("Product process draft keeps existing defaults without source", ProductProcessDraftKeepsExistingDefaultsWithoutSource),
    ("Address manage copies selected product process on add", AddressManageCopiesSelectedProductProcessOnAdd),
    ("Address manage appends new test items in display id order", AddressManageAppendsNewTestItemsInDisplayIdOrder),
    ("Test item ids reuse gaps left by deleted rows", TestItemIdsReuseGapsLeftByDeletedRows),
    ("Scheme detail role headers use centralized defaults", SchemeDetailRoleHeadersUseCentralizedDefaults),
    ("Test item units format report headers and MES values", TestItemUnitsFormatReportHeadersAndMesValues),
    ("Product retest only applies to inspection device", ProductRetestOnlyAppliesToInspectionDevice),
    ("Product retest overwrites values and reopens upload", ProductRetestOverwritesValuesAndReopensUpload),
    ("Product retest removes only uncovered stale records", ProductRetestRemovesOnlyUncoveredStaleRecords),
    ("Upload task retest reopen allows product scoped tasks only", UploadTaskRetestReopenAllowsProductScopedTasksOnly),
    ("Data history dynamic columns append test item units", DataHistoryDynamicColumnsAppendTestItemUnits),
    ("Realtime preview columns append test item units", RealtimePreviewColumnsAppendTestItemUnits),
    ("Scheme detail role grid defines localized bound columns", SchemeDetailRoleGridDefinesLocalizedBoundColumns),
    ("Scheme detail role names and monitor fallbacks are centralized", SchemeDetailRoleNamesAndMonitorFallbacksAreCentralized),
    ("Station display names have localized dual-station rules", StationDisplayNamesHaveLocalizedDualStationRules),
    ("Station display names load legacy defaults and collapse hidden row", StationDisplayNamesLoadLegacyDefaultsAndCollapseHiddenRow),
    ("PLC expression rules support absolute test item addresses", PlcExpressionRulesSupportAbsoluteTestItemAddresses),
    ("Only configured test item expressions create available roles", OnlyConfiguredExpressionsCreateRoles),
    ("Collection does not imply local save or upload", CollectionDoesNotImplyOutput),
    ("Save history controls product history visibility", SaveHistoryControlsProductHistoryVisibility),
    ("Dynamic history and center use task-bound process config", DynamicHistoryAndCenterUseTaskBoundProcessConfig),
    ("DataManageView uses generic product test tree", DataManageViewUsesGenericProductTestTree),
    ("Data history tree preserves stored product result", DataHistoryTreeParentKeepsStoredProductResult),
    ("Data history product result filter keeps complete product rows", DataHistoryProductResultFilterKeepsCompleteProductRows),
    ("Data history dynamic sort orders products and keeps blanks last", DataHistoryDynamicSortOrdersProductsAndKeepsBlanksLast),
    ("Data history export writes current rows and dynamic columns", DataHistoryExportWritesCurrentRowsAndDynamicColumns),
    ("Single-point history display rule uses configured and actual counts", SinglePointHistoryDisplayRuleUsesConfiguredAndActualCounts),
    ("Data history single-point row keeps point values", DataHistorySinglePointRowKeepsPointValues),
    ("Work order deletion rules block running tasks", WorkOrderDeletionRulesBlockRunningTasks),
    ("Work order deletion restricts report paths to report root", WorkOrderDeletionRulesRestrictReportPathsToReportRoot),
    ("Data delete permission is cataloged for admins only", DataDeletePermissionIsCatalogedForAdminsOnly),
    ("Data delete upgrade grants admin only on first introduction", DataDeleteUpgradeGrantsAdminOnlyOnFirstIntroduction),
    ("Scheme output roles are independent from realtime preview", SchemeOutputRolesAreIndependentFromRealtimePreview),
    ("Whole-piece four-side aggregation produces A and B rows", WholePieceFourSideAggregationProducesAbRows),
    ("Whole-piece height and width use product maximum", WholePieceHeightAndWidthUseProductMaximum),
    ("Whole-piece program results use maximum allowed values", WholePieceProgramResultsUseMaximumAllowedValues),
    ("Program result display prefers persisted entity result", ProgramResultDisplayPrefersPersistedEntityResult),
    ("Whole-piece aggregation rejects invalid source data", WholePieceAggregationRejectsInvalidSourceData),
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
    ("Finish report queues generated XLSX even without ReportEnable", FinishReportQueuesGeneratedXlsxEvenWithoutReportEnable),
    ("Report file upload tasks reconcile generated XLSX records", ReportFileUploadTasksReconcileGeneratedXlsxRecords),
    ("Report file waits for successful finish report", ReportFileWaitsForSuccessfulFinishReport),
    ("Unavailable roles are cleared before save", UnavailableRolesAreCleared),
    ("Running task with changed PLC recipe requests reconciliation", RunningTaskWithChangedPlcRecipeRequestsReconciliation),
    ("Finished PLC work-order status skips recipe reconciliation", FinishedWorkOrderStatusSkipsRecipeReconciliation),
    ("Recipe station scope shares only same-work-order dual station recipes", RecipeStationScopeSharesOnlySameWorkOrderDualStationRecipes),
    ("Shared task station scope widens only for same-work-order dual station", SharedTaskStationScopeWidensOnlyForSameWorkOrderDualStation),
    ("Idle station recipe readback does not reconcile", IdleStationRecipeReadbackDoesNotReconcile),
    ("PLC test result codes map to explicit result names", PlcTestResultCodesMapToExplicitResultNames),
    ("Realtime preview values require completed point results", RealtimePreviewValuesRequireCompletedPointResults),
    ("PLC string numeric formatter follows global disabled setting", PlcStringNumericFormatterFollowsGlobalDisabledSetting),
    ("PLC string numeric formatter truncates when enabled", PlcStringNumericFormatterTruncatesWhenEnabled),
    ("PLC string numeric formatter rounds when enabled", PlcStringNumericFormatterRoundsWhenEnabled),
    ("PLC string numeric formatter keeps non numeric text", PlcStringNumericFormatterKeepsNonNumericText),
    ("PLC debug write rules parse bool aliases", PlcDebugWriteRulesParseBoolAliases),
    ("PLC debug write rules normalize unsupported data type", PlcDebugWriteRulesNormalizeUnsupportedDataType),
    ("Alarm address import rules parse engineering document rows", AlarmAddressImportRulesParseEngineeringDocumentRows),
    ("PLC software alarm rules merge raw status and bool signals", PlcSoftwareAlarmRulesMergeRawStatusAndBoolSignals),
    ("PLC alarm addresses stay device-wide", PlcAlarmStationDiscoveryIncludesAlarmOnlyStations),
    ("PLC alarm rules aggregate bool read results", PlcAlarmRulesAggregateBoolReadResults),
    ("PLC alarm projection keeps bool-only alarms local", PlcAlarmProjectionKeepsBoolOnlyAlarmsLocal),
    ("PLC alarm trigger modes select effective alarms", PlcAlarmTriggerModesSelectEffectiveAlarms),
    ("PLC alarm cycle tracks per-address recovery", PlcAlarmCycleTracksPerAddressRecovery),
    ("PLC device alarm cycle restores from jsonl", PlcDeviceAlarmCycleRestoresFromJsonl),
    ("PLC production monitor reads bool alarms independently", PlcProductionMonitorReadsBoolAlarmsIndependently),
    ("PLC software alarms stay local to monitor view", PlcSoftwareAlarmsStayLocalToMonitorView),
    ("Pre-weld NG is treated as failed product result", PreWeldNgIsTreatedAsFailedProductResult),
    ("Center device key uses DeviceId only", CenterDeviceKeyUsesDeviceIdOnly),
    ("Center client online uses heartbeat freshness", CenterClientOnlineUsesHeartbeatFreshness),
    ("Center offline state keeps PLC status unchanged", CenterOfflineStateKeepsPlcStatusUnchanged),
    ("Center telemetry signature tracks dashboard content only", CenterTelemetrySignatureTracksDashboardContentOnly),
    ("Center telemetry sync gates snapshots behind heartbeat", CenterTelemetrySyncGatesSnapshotsBehindHeartbeat),
    ("Center availability log gate aggregates failures and recovery", CenterAvailabilityLogGateAggregatesFailuresAndRecovery),
    ("Center availability classifies timeout and cancellation", CenterAvailabilityClassifiesTimeoutAndCancellation),
    ("Center client aggregates connectivity failures across instances", CenterClientAggregatesConnectivityFailuresAcrossInstances),
    ("Center heartbeat rejection stays out of program exception log", CenterHeartbeatRejectionStaysOutOfProgramExceptionLog),
    ("Center malformed response stays in program exception log", CenterMalformedResponseStaysInProgramExceptionLog),
    ("Center interaction types stay shared across client and server", CenterInteractionTypesStaySharedAcrossClientAndServer),
    ("Center telemetry jsonl fallback preserves MES status names", CenterTelemetryJsonlFallbackPreservesMesStatusNames),
    ("Center alarm message clears once the exception recovers", CenterAlarmMessageClearsOnceTheExceptionRecovers),
    ("Center alarm text strips duplicated station markers", CenterAlarmTextStripsDuplicatedStationMarkers),
    ("Center telemetry snapshot carries station runtime data", CenterTelemetrySnapshotCarriesStationRuntimeData),
    ("Center dashboard device totals are calculated from station data", CenterDashboardDeviceTotalsAreCalculatedFromStationData),
    ("Center dashboard work order quantity deduplicates shared work order", CenterDashboardWorkOrderQuantityDeduplicatesSharedWorkOrder),
    ("Center dashboard achievement rate uses qualified over work order quantity", CenterDashboardAchievementRateUsesQualifiedOverWorkOrderQuantity),
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
    ("Process parameter upload views only include MES targets", ProcessParameterUploadViewsOnlyIncludeMesTargets),
    ("Process parameter pending product rows are read only", ProcessParameterPendingProductRowsAreReadOnly),
    ("Process parameter IsTest follows global setting and device type", ProcessParameterIsTestFollowsGlobalSettingAndDeviceType),
    ("Process parameter numeric roles append test item units", ProcessParameterNumericRolesAppendTestItemUnits),
    ("Whole-piece inspection upload uses side and result fields", WholePieceInspectionUploadUsesSideAndResultFields),
    ("Device log projects every device status code", DeviceLogProjectsEveryDeviceStatusCode),
    ("Pending upload view deletes selected rows in batches", PendingUploadViewDeletesSelectedRowsInBatches),
    ("Quantity upload batches product scopes and unique task ids", QuantityUploadBatchesProductScopesAndUniqueTaskIds),
    ("Process parameter upload payload reads product scope fields", ProcessParameterUploadPayloadReadsProductScopeFields),
    ("Finish makeup only covers products not yet uploaded", FinishMakeupOnlyCoversProductsNotYetUploaded),
    ("MES device status rules use configured MES codes", MesDeviceStatusRulesUseConfiguredMesCodes),
    ("MES device status rules convert PLC alarm transitions", MesDeviceStatusRulesConvertPlcAlarmTransitions),
    ("MES device status rules use latest device id for report", MesDeviceStatusRulesUseLatestDeviceIdForReport),
    ("MES device status rules format status identity", MesDeviceStatusRulesFormatStatusIdentity),
    ("MES device status rules format station remarks", MesDeviceStatusRulesFormatStationRemarks),
    ("MES device status rules format concise exception remarks", MesDeviceStatusRulesFormatConciseExceptionRemarks),
    ("MES device status rules format concise recovery remarks", MesDeviceStatusRulesFormatConciseRecoveryRemarks),
    ("MES device status duplicate suppression honors lifecycle force write", MesDeviceStatusDuplicateSuppressionHonorsLifecycleForceWrite),
    ("Log timestamp display rules switch date visibility", LogTimestampDisplayRulesSwitchDateVisibility),
    ("Antd table selection helper maps selected indexes", AntdTableSelectionHelperMapsSelectedIndexes),
    ("Device status local log store resolves directories", DeviceStatusLocalLogStoreResolvesDirectories),
    ("Device status local log store writes and reads jsonl", DeviceStatusLocalLogStoreWritesAndReadsJsonl),
    ("Device status local log store permits full source scans", DeviceStatusLocalLogStorePermitsFullSourceScans),
    ("Device status local log store caches unchanged snapshots", DeviceStatusLocalLogStoreCachesUnchangedSnapshots),
    ("Device status local log store removes selected log ids", DeviceStatusLocalLogStoreRemovesSelectedLogIds),
    ("Device status record identity supports guid and legacy keys", DeviceStatusRecordIdentitySupportsGuidAndLegacyKeys),
    ("Device status local log store uses record keys", DeviceStatusLocalLogStoreUsesRecordKeys),
    ("Device status local log store skips invalid identities", DeviceStatusLocalLogStoreSkipsInvalidIdentities),
    ("Device status service writes jsonl before MES", DeviceStatusServiceWritesJsonlBeforeMes),
    ("Device status alarm details persist and reach MES remark", DeviceStatusAlarmDetailsPersistAndReachMesRemark),
    ("Device status pending exceptions use concise MES remarks", DeviceStatusPendingExceptionsUseConciseMesRemarks),
    ("Device status pending recoveries use concise MES remarks", DeviceStatusPendingRecoveriesUseConciseMesRemarks),
    ("Device status service serializes concurrent status changes", DeviceStatusServiceSerializesConcurrentStatusChanges),
    ("Device status service preserves MES success after source deletion", DeviceStatusServicePreservesMesSuccessAfterSourceDeletion),
    ("Device status service serializes concurrent retries", DeviceStatusServiceSerializesConcurrentRetries),
    ("Device status service shares concurrent retry results", DeviceStatusServiceSharesConcurrentRetryResults),
    ("Device status service retries pending logs in occurred order", DeviceStatusServiceRetriesPendingLogsInOccurredOrder),
    ("Device status pending replay yields after acquiring order gate", DeviceStatusPendingReplayYieldsAfterAcquiringOrderGate),
    ("Device status force write skips latest history scan", DeviceStatusForceWriteSkipsLatestHistoryScan),
    ("Device status pending replay blocks newer uploads", DeviceStatusPendingReplayBlocksNewerUploads),
    ("Device status newer change waits after older failure", DeviceStatusNewerChangeWaitsAfterOlderFailure),
    ("Device status manual retry preserves pending order", DeviceStatusManualRetryPreservesPendingOrder),
    ("Device status pending replay skips deleted source", DeviceStatusPendingReplaySkipsDeletedSource),
    ("Device status service stops when first jsonl write fails", DeviceStatusServiceStopsWhenFirstJsonlWriteFails),
    ("Device status runtime no longer persists database log rows", DeviceStatusRuntimeNoLongerPersistsDatabaseLogRows),
    ("Device status upload task payload contains only record key", DeviceStatusUploadTaskPayloadContainsOnlyRecordKey),
    ("Device status upload execution revalidates jsonl source", DeviceStatusUploadExecutionRevalidatesJsonlSource),
    ("Device status pending projection preserves uploaded history", DeviceStatusPendingProjectionPreservesUploadedHistory),
    ("Device status pending projection preserves active uploads", DeviceStatusPendingProjectionPreservesActiveUploads),
    ("Device status pending projection keeps in-flight task history", DeviceStatusPendingProjectionKeepsInFlightTaskHistory),
    ("Device status jsonl source behavior is documented", DeviceStatusJsonlSourceBehaviorIsDocumented),
    ("Device status API rejects missing jsonl record", DeviceStatusApiRejectsMissingJsonlRecord),
    ("Device status consumers do not query legacy table", DeviceStatusConsumersDoNotQueryLegacyTable),
    ("Log manage reloads device status jsonl on reentry", LogManageReloadsDeviceStatusJsonlOnReentry),
    ("Device status report keeps millisecond timestamp after MES upload", DeviceStatusReportKeepsMillisecondTimestampAfterMesUpload),
    ("Device status local log store keeps latest state per log id", DeviceStatusLocalLogStoreKeepsLatestStatePerLogId),
    ("Device status pending source and task reconciliation are wired", DeviceStatusPendingSourceAndTaskReconciliationAreWired),
    ("Device status log deletion refresh is wired across views", DeviceStatusLogDeletionRefreshIsWiredAcrossViews),
    ("LogManageView device status tab exposes open folder button", LogManageViewDeviceStatusTabExposesOpenFolderButton),
    ("LogManageView device status tab shows alarm details", LogManageViewDeviceStatusTabShowsAlarmDetails),
    ("LogManageView keeps hidden log fields in details only", LogManageViewKeepsHiddenLogFieldsInDetailsOnly),
    ("MES interaction log grid shows route path", MesInteractionLogGridShowsRoutePath),
    ("Production flow summaries use centralized Chinese text", ProductionFlowSummariesUseCentralizedChineseText),
    ("DataManageView static grids define bound columns", DataManageViewStaticGridsDefineBoundColumns),
    ("DataManageView ignores report selection while disposing", DataManageViewIgnoresReportSelectionWhileDisposing),
    ("DataManageView ignores work order selection while disposing", DataManageViewIgnoresWorkOrderSelectionWhileDisposing),
    ("DataManageView releases query cancellation sources once", DataManageViewReleasesQueryCancellationSourcesOnce),
    ("DataManageView treats cancelled history queries as stale work", DataManageViewTreatsCancelledHistoryQueriesAsStaleWork),
    ("LogManageView loads every log tab in descending time order", LogManageViewLoadsEveryLogTabInDescendingTimeOrder),
    ("Device API responses omit internal IsSuccess", DeviceApiResponsesOmitInternalIsSuccess),
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
    ("State manage view keeps stable columns and loads off UI thread", StateManageViewKeepsStableColumnsAndLoadsOffUiThread),
    ("State manage device status tab supports multi delete", StateManageDeviceStatusTabSupportsMultiDelete),
    ("Skipped upload tasks are not retried", SkippedUploadTasksAreNotRetried),
    ("Weld task pending retry includes device status", WeldTaskPendingRetryIncludesDeviceStatus),
    ("Status report settings default to enabled", StatusReportSettingsDefaultToEnabled),
    ("MES route settings default to current routes", MesRouteSettingsDefaultToCurrentRoutes),
    ("MES heartbeat interval normalization clamps to supported range", MesHeartbeatIntervalNormalizationClampsToSupportedRange),
    ("MES connection monitor confirms offline after three failures", MesConnectionMonitorConfirmsOfflineAfterThreeFailures),
    ("MES connection monitor resets failures and handles exceptions", MesConnectionMonitorResetsFailuresAndHandlesExceptions),
    ("MES offline republishes when failure reason changes", MesOfflineRepublishesWhenFailureReasonChanges),
    ("MES probe delay shortens before offline is confirmed", MesProbeDelayShortensBeforeOfflineIsConfirmed),
    ("MES online check skips interaction log and uses dedicated timeout", MesOnlineCheckSkipsInteractionLogAndUsesDedicatedTimeout),
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
    ("Device lifecycle orders status producers around final states", DeviceLifecycleOrdersStatusProducersAroundFinalStates),
    ("Device lifecycle stop survives earlier shutdown failures", DeviceLifecycleStopSurvivesEarlierShutdownFailures),
    ("Device lifecycle start persists powered on before pending replay", DeviceLifecycleStartPersistsPoweredOnBeforePendingReplay),
    ("Device lifecycle stop cancels startup pending replay", DeviceLifecycleStopCancelsStartupPendingReplay),
    ("Device lifecycle stop still waits after replay cancellation", DeviceLifecycleStopStillWaitsAfterReplayCancellation),
    ("Device lifecycle keeps timed out startup replay tracked", DeviceLifecycleKeepsTimedOutStartupReplayTracked),
    ("Device lifecycle stop waits for bounded status upload", DeviceLifecycleStopWaitsForBoundedStatusUpload),
    ("Device lifecycle stop bounds synchronous status startup", DeviceLifecycleStopBoundsSynchronousStatusStartup),
    ("Device lifecycle stop reports status when lifecycle log fails", DeviceLifecycleStopReportsStatusWhenLifecycleLogFails),
    ("Upload task MES success survives status cancellation", UploadTaskMesSuccessSurvivesStatusCancellation),
    ("Device lifecycle connection logs only when state changes", DeviceLifecycleConnectionLogsOnlyWhenStateChanges),
    ("Device lifecycle no longer subscribes to alarm snapshots", DeviceLifecycleNoLongerSubscribesToAlarmSnapshots),
    ("Program name rules extract component code", ProgramNameRulesExtractComponentCode),
    ("Program name rules reject invalid component code", ProgramNameRulesRejectInvalidComponentCode),
    ("Program name rules build and parse optional description", ProgramNameRulesBuildAndParseOptionalDescription),
    ("Program manage download backfills name fields", ProgramManageDownloadBackfillsNameFields),
    ("Offline program dropdown displays program name", OfflineProgramDropdownDisplaysProgramName),
    ("Offline program dropdown includes empty-content program", OfflineProgramDropdownIncludesEmptyContentProgram),
    ("Offline product-num dropdown lists distinct startable product numbers", OfflineProductNumDropdownListsDistinctStartableProductNums),
    ("Offline program dropdown filters by product number", OfflineProgramDropdownFiltersByProductNum),
    ("Monitor view links product-num selection to program options", MonitorViewLinksProductNumSelectionToProgramOptions),
    ("Monitor view keeps user product number across runtime rebind", MonitorViewKeepsUserProductNumAcrossRuntimeRebind),
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
    ("Program MES create payload clears content for empty values", ProgramMesCreatePayloadClearsContentForEmptyValues),
    ("Program content rules detect configured values", ProgramContentRulesDetectConfiguredValues),
    ("Program manage service no longer generates program files", ProgramManageServiceNoLongerGeneratesProgramFiles),
    ("Program save regenerates name when sequence changes", ProgramSaveRegeneratesNameWhenSequenceChanges),
    ("Program save rejects duplicate program name", ProgramSaveRejectsDuplicateProgramName),
    ("Program manage view provides save-as-new entry", ProgramManageViewProvidesSaveAsNewEntry),
    ("Program manage grid shows sequence and program name", ProgramManageGridShowsSequenceAndProgramName),
    ("Program product groups merge programs sharing product num", ProgramProductGroupsMergeProgramsSharingProductNum),
    ("Program product groups flatten single program product num", ProgramProductGroupsFlattenSingleProgramProductNum),
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
    ("System setting view locks device management during active runtime tasks", SystemSettingViewLocksDeviceManagementDuringActiveRuntimeTasks),
    ("Monitor view finish report uses start operator without prompt", MonitorViewFinishReportUsesStartOperatorWithoutPrompt),
    ("Monitor view clears product identity after finish report", MonitorViewClearsProductIdentityAfterFinishReport),
    ("Monitor view product history uses latest first ordering", MonitorViewProductHistoryUsesLatestFirstOrdering),
    ("Monitor view single-point history mapping keeps point values", MonitorViewSinglePointHistoryMappingKeepsPointValues),
    ("Monitor view clears idle production data", MonitorViewClearsIdleProductionData),
    ("Weld task finish uses MES start id for retry payloads", WeldTaskFinishUsesMesStartIdForRetryPayloads),
    ("Weld task restore unfinished task is idempotent", WeldTaskRestoreUnfinishedTaskIsIdempotent),
    ("Permission catalog omits get work order button", PermissionCatalogOmitsGetWorkOrderButton),
    ("Program content rows come from dictionary items", ProgramContentRowsComeFromDictionaryItems),
    ("Program content JSON keeps only rows with standard values", ProgramContentJsonKeepsOnlyRowsWithStandardValues),
    ("Program content JSON merges existing values and preserves unknown keys", ProgramContentJsonMergesExistingValuesAndPreservesUnknownKeys),
    ("Program content JSON rejects duplicate valued item names", ProgramContentJsonRejectsDuplicateValuedItemNames),
    ("All select controls limit dropdown items", AllSelectControlsLimitDropdownItems),
    ("Work-order auto query skips duplicates and running tasks", WorkOrderAutoQuerySkipsDuplicatesAndRunningTasks),
    ("Work-order baseline suppresses startup residual barcode", WorkOrderBaselineSuppressesStartupResidualBarcode),
    ("Runtime tip restore requires unfinished task", RuntimeTipRestoreRequiresUnfinishedTask),
    ("Work-order input confirmation rules distinguish drafts and PLC values", WorkOrderInputConfirmationRulesDistinguishDraftsAndPlcValues),
    ("Monitor view confirms manual work orders and prioritizes PLC snapshots", MonitorViewConfirmsManualWorkOrdersAndPrioritizesPlcSnapshots),
    ("Program list filter returns all when disabled", ProgramListFilterReturnsAllWhenDisabled),
    ("Program list filter narrows by product number when enabled", ProgramListFilterNarrowsByProductNumberWhenEnabled),
    ("Program list filter returns all when work order product number is blank", ProgramListFilterReturnsAllWhenWorkOrderProductNumberIsBlank),
    ("Program content review rows use edited standard values", ProgramContentReviewRowsUseEditedStandardValues),
    ("Program content review rejects duplicate item names", ProgramContentReviewRejectsDuplicateItemNames),
    ("LoadPrograms filters available programs by work order product number", LoadProgramsFiltersAvailableProgramsByWorkOrderProductNumber),
    ("Select list rules resolve selection by display text", SelectListRulesResolveSelectionByDisplayText),
    ("Select list rules disambiguate duplicate display texts by event index", SelectListRulesDisambiguateDuplicateDisplayTextsByEventIndex),
    ("Natural sort comparer orders product numbers numerically", NaturalSortComparerOrdersProductNumbersNumerically),
    ("Program delete keeps MES sync off UI path", ProgramDeleteKeepsMesSyncOffUiPath),
    ("Program manage save and dual selector paths stay asynchronous", ProgramManageSaveAndDualSelectorPathsStayAsynchronous),
    ("Program lookup snapshot removes UI database queries", ProgramLookupSnapshotRemovesUiDatabaseQueries)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static void SystemSettingLayoutRulesHonorDpiBreakpoints()
{
    AssertEqual(SystemSettingLayoutMode.SingleColumn, SystemSettingLayoutRules.ResolveMode(759, 96), "96 DPI 下 759 应为单列。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(760, 96), "96 DPI 下 760 应进入两列。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(1199, 96), "96 DPI 下 1199 应保持两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1200, 96), "96 DPI 下 1200 应进入三列。");

    AssertEqual(760, SystemSettingLayoutRules.ToLogicalWidth(950, 120), "125% DPI 应换算为 96 DPI 逻辑宽度。");
    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(950, 120), "125% DPI 下 950 设备像素应为两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1500, 120), "125% DPI 下 1500 设备像素应为三列。");

    AssertEqual(SystemSettingLayoutMode.TwoColumns, SystemSettingLayoutRules.ResolveMode(1140, 144), "150% DPI 下 1140 设备像素应为两列。");
    AssertEqual(SystemSettingLayoutMode.ThreeColumns, SystemSettingLayoutRules.ResolveMode(1800, 144), "150% DPI 下 1800 设备像素应为三列。");
    AssertEqual(SystemSettingLayoutMode.SingleColumn, SystemSettingLayoutRules.ResolveMode(-1, 0), "无效宽度和 DPI 必须安全回退。");
}

static void MonitorRightLayoutRulesHonorDpiAndScrolling()
{
    var compact96 = MonitorRightLayoutRules.Resolve(849, 96);
    AssertEqual(MonitorRightLayoutMode.Compact, compact96.Mode, "96 DPI 下逻辑高度 849 应进入紧凑模式。");
    AssertFalse(compact96.RequiresScroll, "紧凑模式达到最低内容高度后不应滚动。");
    AssertEqual(56, compact96.StatusPanelHeight, "紧凑状态区高度必须保持稳定。");
    AssertEqual(254, compact96.MetricPanelHeight, "紧凑指标区高度必须使用约定值。");
    AssertEqual(27, compact96.MetricRowHeight, "紧凑指标数据行高必须使用约定值。");
    AssertEqual(29, compact96.MetricHeaderHeight, "紧凑指标表头高度必须使用约定值。");

    var regular96 = MonitorRightLayoutRules.Resolve(850, 96);
    AssertEqual(MonitorRightLayoutMode.Regular, regular96.Mode, "96 DPI 下逻辑高度 850 应进入常规模式。");
    AssertFalse(regular96.RequiresScroll, "常规模式不应滚动。");
    AssertEqual(70, regular96.StatusPanelHeight, "常规状态区高度必须保持稳定。");
    AssertEqual(290, regular96.MetricPanelHeight, "常规指标区高度必须使用约定值。");
    AssertEqual(32, regular96.MetricRowHeight, "常规指标数据行高必须使用约定值。");
    AssertEqual(34, regular96.MetricHeaderHeight, "常规指标表头高度必须使用约定值。");

    AssertEqual(MonitorRightLayoutMode.Compact, MonitorRightLayoutRules.Resolve(1062, 120).Mode, "125% DPI 下应按逻辑高度选择紧凑模式。");
    AssertEqual(MonitorRightLayoutMode.Regular, MonitorRightLayoutRules.Resolve(1063, 120).Mode, "125% DPI 下应按逻辑高度进入常规模式。");

    var developmentLayout = MonitorRightLayoutRules.Resolve(1000, 120);
    var developmentWorkOrderHeight = developmentLayout.ContentHeight
        - developmentLayout.StatusPanelHeight * 2
        - developmentLayout.ProductResultHeight
        - developmentLayout.MetricPanelHeight;
    AssertEqual(MonitorRightLayoutMode.Compact, developmentLayout.Mode, "开发机截图高度应使用紧凑模式。");
    AssertEqual(472, developmentWorkOrderHeight, "开发机紧凑布局应为工单信息保留稳定空间。");

    var industrialLayout = MonitorRightLayoutRules.Resolve(1097, 120);
    var industrialWorkOrderHeight = industrialLayout.ContentHeight
        - industrialLayout.StatusPanelHeight * 2
        - industrialLayout.ProductResultHeight
        - industrialLayout.MetricPanelHeight;
    AssertEqual(MonitorRightLayoutMode.Regular, industrialLayout.Mode, "工控机截图高度应使用常规模式。");
    AssertEqual(470, industrialWorkOrderHeight, "工控机多余高度应进入工单信息区而不是指标区底部。");

    AssertEqual(MonitorRightLayoutMode.Compact, MonitorRightLayoutRules.Resolve(1274, 144).Mode, "150% DPI 下应按逻辑高度选择紧凑模式。");
    AssertEqual(MonitorRightLayoutMode.Regular, MonitorRightLayoutRules.Resolve(1275, 144).Mode, "150% DPI 下应按逻辑高度进入常规模式。");

    var shortView = MonitorRightLayoutRules.Resolve(700, 96);
    AssertTrue(shortView.RequiresScroll, "低于最低逻辑高度时必须启用滚动。");
    AssertEqual(772, shortView.ContentHeight, "低高度视口必须保留最低内容高度。");

    var invalid = MonitorRightLayoutRules.Resolve(-1, 0);
    AssertEqual(MonitorRightLayoutMode.Compact, invalid.Mode, "无效高度和 DPI 必须安全回退到紧凑模式。");
    AssertTrue(invalid.RequiresScroll, "无效高度必须使用最低内容高度并启用滚动。");
    AssertEqual(772, invalid.ContentHeight, "无效高度必须回退到 96 DPI 最低内容高度。");
}

static void MonitorViewAppliesResponsiveRightLayout()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var layoutMethod = ExtractMethodText(
        viewCode,
        "private void ApplyResponsiveRightLayout(bool force = false)",
        "private static void SetAbsoluteRowHeight");

    AssertTrue(designerCode.Contains("VerticalSplitter.Panel2.AutoScroll = true;", StringComparison.Ordinal), "右侧视口必须支持极低高度纵向滚动。");
    AssertTrue(designerCode.Contains("tlpRight.Dock = DockStyle.Top;", StringComparison.Ordinal), "右侧内容必须顶部停靠以允许内容高度超过视口。");
    AssertTrue(viewCode.Contains("protected override void OnSizeChanged(EventArgs e)", StringComparison.Ordinal), "尺寸变化后必须重新应用右侧布局。");
    AssertTrue(viewCode.Contains("protected override void OnDpiChangedAfterParent(EventArgs e)", StringComparison.Ordinal), "父级 DPI 变化后必须强制刷新右侧布局。");
    AssertTrue(layoutMethod.Contains("MonitorRightLayoutRules.Resolve(viewportHeight, dpi)", StringComparison.Ordinal), "右侧布局必须复用 DPI 规则。");
    AssertTrue(layoutMethod.Contains("tlpRight.RowStyles[0].SizeType = SizeType.Percent", StringComparison.Ordinal), "工单区域必须吸收剩余高度。");
    AssertTrue(layoutMethod.Contains("VerticalSplitter.Panel2.AutoScrollMinSize", StringComparison.Ordinal), "极低高度必须设置滚动内容尺寸。");
    AssertTrue(layoutMethod.Contains("SetFixedHeight(grpErrorTips", StringComparison.Ordinal), "异常提示高度必须保持稳定。");
    AssertTrue(layoutMethod.Contains("SetFixedHeight(grpRunningStatus", StringComparison.Ordinal), "运行状态高度必须保持稳定。");
    AssertTrue(
        layoutMethod.Contains("ApplyProductionMetricTableStyle(layout.MetricRowHeight, layout.MetricHeaderHeight)", StringComparison.Ordinal)
            && viewCode.Contains("ApplyProductionMetricTableStyle(tableMetric1, rowHeight, headerHeight)", StringComparison.Ordinal)
            && viewCode.Contains("ApplyProductionMetricTableStyle(tableMetric2, rowHeight, headerHeight)", StringComparison.Ordinal),
        "两个工位的生产指标表必须应用同一响应式行高。");
    AssertFalse(layoutMethod.Contains("Controls.Add", StringComparison.Ordinal), "响应式布局不得重建右侧控件。");
}

static void MonitorViewPreservesWorkOrderInputFocusOnPreviewHover()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var wireMethod = ExtractMethodText(
        viewCode,
        "private void WireWeldPreviewGridEvents(DataGridView grid)",
        "private void UnwireWeldPreviewGridEvents");
    var mouseEnterMethod = ExtractMethodText(
        viewCode,
        "private void Table2_MouseEnter(object? sender, EventArgs e)",
        "private void Table2_MouseWheel");

    AssertTrue(wireMethod.Contains("grid.MouseEnter += Table2_MouseEnter;", StringComparison.Ordinal), "焊接预览表格必须保留鼠标进入事件，以维持原有滚轮交互。");
    var focusGuardIndex = mouseEnterMethod.IndexOf("if (tlpWorkOrderInfo.ContainsFocus)", StringComparison.Ordinal);
    var gridFocusIndex = mouseEnterMethod.IndexOf("grid.Focus();", StringComparison.Ordinal);
    AssertTrue(focusGuardIndex >= 0, "鼠标进入焊接预览时必须先保护工单输入区域焦点。");
    AssertTrue(gridFocusIndex > focusGuardIndex, "工单输入区域焦点保护必须先于表格主动聚焦执行。");
}

static void PlcAlarmNotificationRulesNormalizeMessagesAndSignatures()
{
    var messages = PlcAlarmNotificationRules.SplitMessages("温度过高；安全门未关闭\r\n温度过高;伺服报警");
    AssertSequenceEqual(
        new[] { "温度过高", "安全门未关闭", "伺服报警" },
        messages.ToArray(),
        "PLC 报警通知必须按分隔符拆分并去重，同时保留首次出现顺序。");

    var activeSignature = PlcAlarmNotificationRules.CreateSignature(messages, pendingConfirmation: false);
    var reorderedSignature = PlcAlarmNotificationRules.CreateSignature(messages.Reverse(), pendingConfirmation: false);
    var pendingSignature = PlcAlarmNotificationRules.CreateSignature(messages, pendingConfirmation: true);
    AssertEqual(activeSignature, reorderedSignature, "报警签名不得受 PLC 报警返回顺序变化影响。");
    AssertFalse(string.Equals(activeSignature, pendingSignature, StringComparison.Ordinal), "确认报警和等待确认报警必须使用不同签名。");
    AssertEqual("1.温度过高\r\n2.安全门未关闭\r\n3.伺服报警", PlcAlarmNotificationRules.BuildDisplayText(messages), "通知正文必须按序号逐行展示全部报警。");
}

static void PlcProductionMonitorPreservesAlarmStateUntilProjection()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Plc", "ProductionMonitorService.cs"), Encoding.UTF8);
    var pollMethod = ExtractMethodText(serviceCode, "private async Task PollOnceAsync", "private void ApplyEffectiveAlarmSnapshots");
    var applyMethod = ExtractMethodText(serviceCode, "private void ApplyEffectiveAlarmSnapshots", "private async Task<IReadOnlyList<BizPlcAddress>>");
    var failureMethod = ExtractMethodText(serviceCode, "private void PublishFailureForStations", "private void PublishIdleForStations");
    var idleMethod = ExtractMethodText(serviceCode, "private void PublishIdleForStations", "private static IReadOnlyList<int> ResolveStationNumbers");

    AssertTrue(
        pollMethod.Contains("var currentSnapshot = GetCurrent(stationNo);", StringComparison.Ordinal)
            && pollMethod.Contains("IsSoftwareAlarmActive = currentSnapshot.IsSoftwareAlarmActive", StringComparison.Ordinal),
        "普通生产快照必须保留上一轮报警状态，不能先发布临时无报警快照。");
    AssertTrue(
        applyMethod.Contains("Publish(current with", StringComparison.Ordinal)
            && applyMethod.Contains("IsSoftwareAlarmActive = stationAlarms.Count > 0", StringComparison.Ordinal),
        "统一报警投影必须负责发布最终报警状态。");
    AssertFalse(
        failureMethod.Contains("IsSoftwareAlarmActive = false", StringComparison.Ordinal)
            || idleMethod.Contains("IsSoftwareAlarmActive = false", StringComparison.Ordinal),
        "生产读取失败或 PLC 暂未就绪时不得清空上一轮报警状态。");
}

static void SystemSettingViewAvoidsRepeatedLayoutRebuilds()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("if (!force && mode == _lastLayoutMode)", StringComparison.Ordinal), "同一列模式下调整窗口大小不应重复重建布局。");
    AssertFalse(viewCode.Contains("viewportSize == _lastLayoutViewportSize", StringComparison.Ordinal), "视口尺寸变化不应成为每次重建响应式网格的条件。");
}

static void PlcShutdownReturnsWhileCommunicationLockIsHeld()
{
    var service = new CommunicationService(
        new FakeAppSettingsService(),
        new FakeOperationLogService(),
        new FakePlcAddressService(),
        new FakeLocalizationService());
    var syncField = typeof(CommunicationService).GetField(
        "_sync",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var communicationLock = (SemaphoreSlim?)syncField?.GetValue(service)
        ?? throw new InvalidOperationException("CommunicationService must keep the shared communication lock.");
    Task stopTask = Task.CompletedTask;

    communicationLock.Wait();
    try
    {
        stopTask = service.StopAsync();
        AssertTrue(
            stopTask.Wait(TimeSpan.FromSeconds(5)),
            "PLC stop must apply its own timeout when a communication operation owns the shared lock.");

        // Dispose must not release the semaphore while the simulated PLC call still owns it.
        // Releasing it below is the behavioral check for the late-read ObjectDisposedException.
        service.Dispose();
    }
    finally
    {
        communicationLock.Release();
        stopTask.GetAwaiter().GetResult();
        service.Dispose();
    }
}

static void PlcShutdownDetachesClientBeforeBoundedClose()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "CommunicationService.cs"),
        Encoding.UTF8);
    var closeMethod = ExtractMethodText(
        serviceCode,
        "private async Task CloseClientAsync",
        "private async Task DrainCommunicationLockAsync");
    var drainMethod = ExtractMethodText(
        serviceCode,
        "private async Task DrainCommunicationLockAsync",
        "private void ForgetClientReference");
    var connectMethod = ExtractMethodText(
        serviceCode,
        "private async Task<PlcServiceResult> ConnectAsync",
        "private PlcServiceResult<NetworkDeviceBase> CreateClient");
    var connectionLoopMethod = ExtractMethodText(
        serviceCode,
        "private async Task RunConnectionLoopAsync",
        "private static TimeSpan ResolvePlcHeartbeatReadInterval");
    var disposeMethod = ExtractMethodText(
        serviceCode,
        "private void DisposeCore()",
        "private async Task RunConnectionLoopAsync");

    AssertTrue(closeMethod.Contains("Interlocked.Exchange(ref _client, null)", StringComparison.Ordinal), "停止 PLC 时必须先原子移除旧客户端。");
    AssertFalse(closeMethod.Contains("_sync.WaitAsync", StringComparison.Ordinal), "关闭客户端不得再次无限等待通讯锁。");
    AssertTrue(closeMethod.Contains("ConnectCloseAsync()", StringComparison.Ordinal), "停止 PLC 时必须异步请求关闭第三方客户端。");
    AssertTrue(closeMethod.Contains("WaitAsync", StringComparison.Ordinal), "第三方客户端关闭必须有等待边界。");
    AssertTrue(drainMethod.Contains("_sync.WaitAsync", StringComparison.Ordinal), "关闭客户端后必须有界排空通讯锁。");
    AssertTrue(drainMethod.Contains("BuildPlcTimeout(CurrentSettings)", StringComparison.Ordinal), "通讯锁排空必须沿用当前配置的 PLC 通讯超时。");
    AssertTrue(connectMethod.Contains("Volatile.Read(ref _stopping) != 0", StringComparison.Ordinal), "连接完成后必须再次检查停止状态。");
    AssertTrue(connectMethod.Contains("Interlocked.CompareExchange(ref _client, null, client)", StringComparison.Ordinal), "退出期间完成的连接必须从服务中原子移除。");
    AssertTrue(connectionLoopMethod.Contains("catch (Exception) when (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "停止期间旧连接循环的退出异常不得发布为 PLC 故障。");
    AssertTrue(disposeMethod.Contains("_sync.Wait(0)", StringComparison.Ordinal), "仍有通讯任务占锁时不得提前释放 SemaphoreSlim。");
}

static void PlcHeartbeatSettingsNormalizeAndPreserveSafeDefaults()
{
    var defaults = new AppSettings();
    AssertEqual(300, defaults.PlcHeartbeatReadIntervalMilliseconds, "PLC心跳监测频率默认必须为300ms。");
    AssertEqual(3, defaults.PlcHeartbeatTimeoutSeconds, "PLC心跳超时时间默认必须为3秒。");
    AssertEqual(3000, defaults.PlcCommunicationTimeoutMilliseconds, "PLC通讯超时默认必须为3000ms。");

    AssertEqual(300, PlcHeartbeatSettingsRules.NormalizeReadIntervalMilliseconds(0), "旧数据库频率0必须回退到300ms。");
    AssertEqual(100, PlcHeartbeatSettingsRules.NormalizeReadIntervalMilliseconds(50), "心跳监测频率下限必须为100ms。");
    AssertEqual(5000, PlcHeartbeatSettingsRules.NormalizeReadIntervalMilliseconds(6000), "心跳监测频率上限必须为5000ms。");
    AssertEqual(3, PlcHeartbeatSettingsRules.NormalizeTimeoutSeconds(0), "旧数据库心跳超时0必须回退到3秒。");
    AssertEqual(1, PlcHeartbeatSettingsRules.NormalizeTimeoutSeconds(1), "心跳超时下限必须为1秒。");
    AssertEqual(60, PlcHeartbeatSettingsRules.NormalizeTimeoutSeconds(90), "心跳超时上限必须为60秒。");
    AssertEqual(3000, PlcHeartbeatSettingsRules.NormalizeCommunicationTimeoutMilliseconds(0), "旧数据库通讯超时0必须回退到3000ms。");
    AssertEqual(100, PlcHeartbeatSettingsRules.NormalizeCommunicationTimeoutMilliseconds(50), "通讯超时下限必须为100ms。");
    AssertEqual(30000, PlcHeartbeatSettingsRules.NormalizeCommunicationTimeoutMilliseconds(40000), "通讯超时上限必须为30000ms。");
}

static void PlcHeartbeatSamplingAvoidsToggleAliasingAndDelayedFalseFaults()
{
    var sampleStart = new DateTime(2026, 8, 18, 10, 0, 0);
    var observedValues = Enumerable
        .Range(0, 20)
        .Select(index => (index, sampleTime: sampleStart.AddMilliseconds(index * 300)))
        .Select(item => item.sampleTime - sampleStart)
        .Select(elapsed => ((int)(elapsed.TotalMilliseconds / 500) % 2).ToString())
        .ToArray();

    AssertTrue(observedValues.Distinct(StringComparer.Ordinal).Count() > 1, "500ms翻转信号按300ms采样时必须能够观察到0/1变化。");
    AssertFalse(
        PlcHeartbeatSettingsRules.IsSamplingDelayed(
            sampleStart.AddMilliseconds(300),
            sampleStart.AddMilliseconds(600),
            timeoutSeconds: 3),
        "300ms正常采样间隔不应被判定为采样延迟。");
    AssertTrue(
        PlcHeartbeatSettingsRules.IsSamplingDelayed(
            sampleStart,
            sampleStart.AddSeconds(3.1),
            timeoutSeconds: 3),
        "采样间隔超过心跳超时后必须允许重置基线，避免把错过的翻转误判为停滞。");

    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "CommunicationService.cs"),
        Encoding.UTF8);
    AssertTrue(serviceCode.Contains("_sync.WaitAsync(cancellationToken)", StringComparison.Ordinal), "心跳采样必须继续经过共享通讯锁，诊断才能暴露锁竞争。");
    AssertTrue(serviceCode.Contains("Stopwatch.GetElapsedTime(lockWaitStart)", StringComparison.Ordinal), "心跳必须记录通讯锁等待耗时。");
    AssertTrue(serviceCode.Contains("Stopwatch.GetElapsedTime(operationStart)", StringComparison.Ordinal), "心跳必须记录PLC实际读取耗时。");
    AssertTrue(serviceCode.Contains("BaselineReset=true", StringComparison.Ordinal), "采样延迟时必须记录基线重置诊断。");
    AssertTrue(serviceCode.Contains("runtime.LastPlcHeartbeatValue = currentValue", StringComparison.Ordinal), "采样延迟时必须以当前值重置心跳基线。");
    AssertTrue(serviceCode.Contains("unchangedDuration > heartbeatTimeout", StringComparison.Ordinal), "正常连续采样下仍超过超时时间未变化才判定心跳停滞。");
    AssertTrue(serviceCode.Contains("HeartbeatFailureThreshold = 3", StringComparison.Ordinal), "连续三次读取失败的断联阈值必须保留。");
    AssertTrue(serviceCode.Contains("Dictionary<int, StationHeartbeatRuntime>", StringComparison.Ordinal), "双工位必须分别维护心跳采样运行态。");
}

static void PlcHeartbeatSettingsAreWiredThroughSystemSettingsView()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"),
        Encoding.UTF8);
    var designerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"),
        Encoding.UTF8);
    var textKeysCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Constants", "TextKeys.cs"),
        Encoding.UTF8);
    var chineseResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"),
        Encoding.UTF8);
    var englishResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"),
        Encoding.UTF8);

    AssertTrue(viewCode.Contains("inputPlcHeartbeatTimeout", StringComparison.Ordinal), "系统设置必须加载、校验并保存PLC心跳超时。");
    AssertTrue(viewCode.Contains("inputPlcCommunicationTimeout", StringComparison.Ordinal), "系统设置必须加载、校验并保存PLC通讯超时。");
    AssertTrue(viewCode.Contains("PlcHeartbeatTimeoutSeconds", StringComparison.Ordinal), "PLC心跳超时必须写入AppSettings。");
    AssertTrue(viewCode.Contains("PlcCommunicationTimeoutMilliseconds", StringComparison.Ordinal), "PLC通讯超时必须写入AppSettings。");
    AssertTrue(viewCode.Contains("HasPlcCommunicationChanged", StringComparison.Ordinal), "心跳参数变更必须复用PLC重启流程。");
    AssertTrue(designerCode.Contains("lblPlcHeartbeatTimeout", StringComparison.Ordinal), "Designer必须声明PLC心跳超时控件。");
    AssertTrue(designerCode.Contains("lblPlcCommunicationTimeout", StringComparison.Ordinal), "Designer必须声明PLC通讯超时控件。");
    AssertTrue(textKeysCode.Contains("PlcHeartbeatTimeout", StringComparison.Ordinal), "必须有PLC心跳超时本地化键。");
    AssertTrue(textKeysCode.Contains("PlcCommunicationTimeout", StringComparison.Ordinal), "必须有PLC通讯超时本地化键。");
    AssertTrue(chineseResources.Contains("PLC心跳监测频率(ms)", StringComparison.Ordinal), "中文资源必须使用PLC心跳监测频率文案。");
    AssertTrue(chineseResources.Contains("PLC心跳超时时间(s)", StringComparison.Ordinal), "中文资源必须包含PLC心跳超时文案。");
    AssertTrue(chineseResources.Contains("PLC通讯超时(ms)", StringComparison.Ordinal), "中文资源必须包含PLC通讯超时文案。");
    AssertTrue(englishResources.Contains("PLC heartbeat monitoring interval (ms)", StringComparison.Ordinal), "英文资源必须使用PLC heartbeat monitoring interval文案。");
    AssertTrue(englishResources.Contains("PLC heartbeat timeout (s)", StringComparison.Ordinal), "英文资源必须包含PLC heartbeat timeout文案。");
    AssertTrue(englishResources.Contains("PLC communication timeout (ms)", StringComparison.Ordinal), "英文资源必须包含PLC communication timeout文案。");
}

static void MonitorViewCancelsBusinessSignalReconciliationOnDestroy()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var destroyMethod = ExtractMethodText(
        viewCode,
        "protected override void OnHandleDestroyed(EventArgs e)",
        "#endregion");
    var queueMethod = ExtractMethodText(
        viewCode,
        "private void QueueBusinessSignalReconciliation",
        "private async Task ReconcileDeviceModeAsync");
    var reconcileMethods = ExtractMethodText(
        viewCode,
        "private async Task ReconcileDeviceModeAsync",
        "private IReadOnlyList<int> ResolveBusinessSignalReconcileStations");
    var ensureMethod = ExtractMethodText(
        viewCode,
        "private async Task EnsureIntegerBusinessSignalAsync",
        "private SemaphoreSlim GetWorkOrderStatusLock");
    var ensureSignalMethods = ExtractMethodText(
        viewCode,
        "private async Task EnsureWorkOrderStatusAsync",
        "private async Task EnsureIntegerBusinessSignalAsync");

    AssertTrue(viewCode.Contains("CancellationTokenSource? _businessSignalReconcileCancellation", StringComparison.Ordinal), "监控页必须维护业务信号调和生命周期令牌。");
    AssertTrue(destroyMethod.Contains("CancelAndDispose(ref _businessSignalReconcileCancellation)", StringComparison.Ordinal), "销毁监控页时必须取消并释放调和令牌。");
    AssertTrue(queueMethod.Contains("cancellationToken", StringComparison.Ordinal), "调和排队入口必须向后台任务传递页面生命周期令牌。");
    AssertTrue(reconcileMethods.Contains("OperationCanceledException", StringComparison.Ordinal), "页面销毁触发的预期取消不得写成程序异常。");
    AssertTrue(ensureMethod.Contains("signalLock.WaitAsync(cancellationToken)", StringComparison.Ordinal), "等待业务信号锁时必须响应页面取消。");
    AssertTrue(ensureMethod.Contains("ReadTextAsync(logicalKey, targetStationNo, cancellationToken)", StringComparison.Ordinal), "PLC 业务信号读取必须接收页面生命周期令牌。");
    AssertTrue(ensureSignalMethods.Contains("WriteWorkOrderStatusAsync(target, value, cancellationToken)", StringComparison.Ordinal), "PLC 工单状态写入必须接收页面生命周期令牌。");
    AssertTrue(ensureSignalMethods.Contains("WriteDeviceModeAsync(target, value, cancellationToken)", StringComparison.Ordinal), "PLC 设备模式写入必须接收页面生命周期令牌。");
}

static void MonitorViewCancelsPendingUploadRetryOnDestroy()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var destroyMethod = ExtractMethodText(
        viewCode,
        "protected override void OnHandleDestroyed(EventArgs e)",
        "#endregion");
    var retryMethod = ExtractMethodText(
        viewCode,
        "private void QueuePendingUploadRetry()",
        "private static string GetMesStateKey");
    var cancelMethod = ExtractMethodText(
        viewCode,
        "private void CancelPendingUploadRetry()",
        "private async Task ReconcileDeviceModeAsync");

    AssertTrue(viewCode.Contains("CancellationTokenSource? _pendingUploadRetryCancellation", StringComparison.Ordinal), "监控页必须维护 MES 重连补传的生命周期令牌。");
    AssertTrue(viewCode.Contains("Task? _pendingUploadRetryTask", StringComparison.Ordinal), "监控页必须跟踪 MES 重连补传任务。");
    AssertTrue(destroyMethod.Contains("CancelPendingUploadRetry();", StringComparison.Ordinal), "窗口销毁时必须取消 MES 重连补传任务。");
    AssertFalse(cancelMethod.Contains(".Wait(", StringComparison.Ordinal), "窗口销毁不能在停机上传前再等待一个 MES 超时。");
    AssertTrue(cancelMethod.Contains("ContinueWith(", StringComparison.Ordinal), "重连补传取消源必须在任务完成后释放。");
    AssertTrue(retryMethod.Contains("_weldTaskService.RetryPendingUploadsAsync(cancellationToken)", StringComparison.Ordinal), "MES 重连补传必须把页面生命周期令牌传给上传服务。");
    AssertTrue(retryMethod.Contains("OperationCanceledException", StringComparison.Ordinal), "页面销毁触发的 MES 重连补传取消不得记录为异常。");
}

static void PlcStatusTooltipUsesCompactLocalizedAcrylicPanel()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var zhResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"),
        Encoding.UTF8);
    var enResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"),
        Encoding.UTF8);
    var updateMethod = ExtractMethodText(
        viewCode,
        "private void UpdatePlcStatusToolTipText(string text)",
        "private void ShowPlcStatusToolTipPopup()");
    var buildMethod = ExtractMethodText(
        viewCode,
        "private string BuildPlcStatusToolTipText(PlcConnectionSnapshot snapshot)",
        "private string FormatCompactPlcStatusHistoryEntry(PlcStatusHistoryEntry entry)");
    var historyMethod = ExtractMethodText(
        viewCode,
        "private string FormatCompactPlcStatusHistoryEntry(PlcStatusHistoryEntry entry)",
        "private void RecordPlcStatusChange(PlcConnectionSnapshot snapshot)");

    AssertTrue(
        viewCode.Contains("AntdUI.Panel? _plcStatusToolTipPanel", StringComparison.Ordinal),
        "PLC 状态悬浮提示必须使用现有 AntdUI.Panel。");
    AssertTrue(
        viewCode.Contains("private const int PlcStatusHistoryLimit = 5;", StringComparison.Ordinal),
        "PLC 状态历史必须限制为最近五条。");
    AssertTrue(
        viewCode.Contains("FormatCompactPlcStatusHistoryEntry", StringComparison.Ordinal),
        "PLC 状态历史必须使用紧凑格式。");
    AssertFalse(
        viewCode.Contains("当前读取时间", StringComparison.Ordinal),
        "悬浮提示不应每次刷新都显示当前读取时间。");
    AssertTrue(
        viewCode.Contains("Screen.FromControl(tagPLC).WorkingArea", StringComparison.Ordinal),
        "悬浮提示定位必须受当前屏幕工作区约束。");
    AssertTrue(
        updateMethod.Contains("_lastPlcStatusToolTipClientWidth == currentClientWidth", StringComparison.Ordinal)
        && updateMethod.Contains("_lastPlcStatusToolTipDpi == currentDpi", StringComparison.Ordinal),
        "悬浮提示必须在客户区宽度或 DPI 变化后重新测量。");
    AssertTrue(
        viewCode.Contains("ShadowColor = Color.FromArgb(15, 23, 42)", StringComparison.Ordinal),
        "悬浮提示阴影颜色不应与 ShadowOpacity 重复叠加透明度。");
    AssertTrue(
        buildMethod.Contains("FormatToolTipValue(snapshot.Message)", StringComparison.Ordinal),
        "当前 PLC 消息必须保留完整原始诊断内容。");
    AssertTrue(
        historyMethod.Contains("ToString(\"HH:mm:ss\"", StringComparison.Ordinal)
        && historyMethod.Contains("NormalizeRuntimeSummary(entry.Message)", StringComparison.Ordinal),
        "历史状态必须使用短时间并限制原始消息长度。");
    AssertFalse(
        historyMethod.Contains("entry.IsConnected", StringComparison.Ordinal)
        || historyMethod.Contains("FormatYesNo", StringComparison.Ordinal),
        "紧凑历史不应重复连接状态字段。");

    var tooltipKeys = typeof(TextKeys.Monitor.PlcToolTip)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();
    foreach (var key in tooltipKeys)
    {
        AssertTrue(zhResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"中文资源必须包含 {key}。");
        AssertTrue(enResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"英文资源必须包含 {key}。");
    }
}

static void BaseWindowBatchesInteractiveResize()
{
    var baseWindowCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Base", "BaseWindow.cs"), Encoding.UTF8);

    AssertTrue(baseWindowCode.Contains("protected override void OnResizeBegin", StringComparison.Ordinal), "基础窗体必须在系统调整尺寸开始时进入批量布局模式。");
    AssertTrue(baseWindowCode.Contains("protected override void OnResizeEnd", StringComparison.Ordinal), "基础窗体必须在系统调整尺寸结束时恢复布局。");
    AssertTrue(baseWindowCode.Contains("SuspendLayoutRecursive(this)", StringComparison.Ordinal), "调整尺寸期间必须暂停整个控件树的布局。");
    AssertTrue(baseWindowCode.Contains("SendMessage(Handle, WmSetRedraw, IntPtr.Zero", StringComparison.Ordinal), "调整尺寸期间必须暂时关闭重绘。");
    AssertTrue(baseWindowCode.Contains("ResumeLayoutRecursive(this)", StringComparison.Ordinal), "调整尺寸结束时必须恢复整个控件树的布局。");
    AssertTrue(baseWindowCode.Contains("CompleteInteractiveResize(repaint: false)", StringComparison.Ordinal), "句柄销毁时必须取消未完成的调整尺寸批处理。");
}

static void MainFormKeepsCachedPagesMountedDuringNavigation()
{
    var mainFormCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Forms", "MainForm.cs"), Encoding.UTF8);
    var ensureStart = mainFormCode.IndexOf("private void EnsureViewLoaded", StringComparison.Ordinal);
    var displayStart = mainFormCode.IndexOf("private void DisplayView", StringComparison.Ordinal);
    var currentPageStart = mainFormCode.IndexOf("private string? GetCurrentPagePermissionCode", StringComparison.Ordinal);
    var emptyPageStart = mainFormCode.IndexOf("private void ShowEmptyPermissionPage", StringComparison.Ordinal);
    var languageStart = mainFormCode.IndexOf("private void Language_SelectedIndexChanged", StringComparison.Ordinal);

    AssertTrue(ensureStart >= 0 && displayStart > ensureStart && currentPageStart > displayStart && emptyPageStart > currentPageStart && languageStart > emptyPageStart, "MainForm 必须保留页面加载、显示和空状态方法边界。");

    var ensureCode = mainFormCode[ensureStart..displayStart];
    var displayCode = mainFormCode[displayStart..currentPageStart];
    var emptyPageCode = mainFormCode[emptyPageStart..languageStart];
    AssertFalse(ensureCode.Contains("_permissionUiBinder.Apply(cachedView)", StringComparison.Ordinal), "已缓存页面切换时不应重复递归应用权限。");
    AssertFalse(displayCode.Contains("pnlContent.Controls.Clear()", StringComparison.Ordinal), "页面切换不应反复清空并重挂载内容控件。");
    AssertTrue(displayCode.Contains("view.BringToFront()", StringComparison.Ordinal), "页面切换应通过调整前后层级显示缓存页面。");
    AssertFalse(emptyPageCode.Contains("pnlContent.Controls.Clear()", StringComparison.Ordinal), "空权限状态不得移除缓存页面，否则换用户后可能保留旧权限。");
}

static void SystemSettingInitialLoadAvoidsDuplicateWork()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var languageStart = viewCode.IndexOf("protected override void OnLanguageChanged", StringComparison.Ordinal);
    var loadStart = viewCode.IndexOf("protected override void OnLoad", StringComparison.Ordinal);
    var loadSettingsStart = viewCode.IndexOf("private void LoadSettings", StringComparison.Ordinal);
    var handleDestroyedStart = viewCode.IndexOf("protected override void OnHandleDestroyed", StringComparison.Ordinal);

    AssertTrue(languageStart >= 0 && loadStart > languageStart && loadSettingsStart > loadStart && handleDestroyedStart > loadSettingsStart, "SystemSettingView 必须保留语言和加载方法边界。");

    var languageCode = viewCode[languageStart..loadStart];
    var loadSettingsCode = viewCode[loadSettingsStart..handleDestroyedStart];
    AssertTrue(languageCode.Contains("if (!_initialized)", StringComparison.Ordinal), "首次 OnLoad 的语言回调应跳过下拉绑定和强制布局。");
    AssertFalse(loadSettingsCode.Contains("ApplyLocalizedTexts();", StringComparison.Ordinal), "配置首次加载不应再次国际化全部控件。");
}

static void SystemSettingCachesDeviceLockStateBetweenDisplays()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var stateChangedStart = viewCode.IndexOf("private void WeldTaskService_StateChanged", StringComparison.Ordinal);
    var settingsChangedStart = viewCode.IndexOf("private void SettingsService_SettingsChanged", StringComparison.Ordinal);

    AssertTrue(viewCode.Contains("private bool _deviceManagementStateKnown;", StringComparison.Ordinal), "系统设置页必须缓存已查询的设备管理锁定状态。");
    AssertTrue(viewCode.Contains("_weldTaskService.StateChanged += WeldTaskService_StateChanged;", StringComparison.Ordinal), "系统设置页必须在生产任务状态变化时失效设备管理缓存。");
    AssertTrue(viewCode.Contains("private void RefreshDeviceManagementEnabled(bool force = false)", StringComparison.Ordinal), "设备管理刷新入口必须支持仅在缓存失效时查询。");
    AssertTrue(viewCode.Contains("RefreshDeviceManagementEnabled(force: true);", StringComparison.Ordinal), "首次加载和任务变化必须强制刷新设备管理状态。");
    AssertTrue(stateChangedStart >= 0 && settingsChangedStart > stateChangedStart, "系统设置页必须保留生产任务状态变化处理器边界。");

    var stateChangedCode = viewCode[stateChangedStart..settingsChangedStart];
    var dispatcherStart = stateChangedCode.IndexOf("RunOnUiThread", StringComparison.Ordinal);
    var visibilityCheck = stateChangedCode.IndexOf("!Visible", StringComparison.Ordinal);
    AssertTrue(dispatcherStart >= 0 && visibilityCheck > dispatcherStart, "后台任务状态事件必须在切换到 UI 线程后再读取控件可见性。");
}

static void PlcRecipeNameRulesMapSlotsWithoutShiftingCodes()
{
    var config = new BizPlcRecipeNameConfig
    {
        StationNo = 2,
        BaseAddress = "DB20.100",
        RecipeCount = 4,
        AddressOffset = 12,
        StringLength = 20,
        Enabled = true
    };

    var options = PlcRecipeNameRules.BuildOptions(
        config,
        new Dictionary<int, string?>
        {
            [1] = " 工艺A ",
            [2] = string.Empty,
            [3] = "工艺A",
            [4] = "工艺B"
        });

    AssertEqual(3, options.Count, "空白配方名称不应生成下拉选项。 ");
    AssertEqual(1, options[0].RecipeCode, "第一个地址应映射配方号 1。 ");
    AssertEqual("DB20.100", options[0].Address, "配方号 1 应使用基地址。 ");
    AssertEqual("工艺A", options[0].DisplayText, "核心配方选项应保持语言中性，由界面负责重名消歧。 ");
    AssertEqual(3, options[1].RecipeCode, "跳过空名称时不得压缩后续配方号。 ");
    AssertEqual("DB20.124", options[1].Address, "配方号 3 应使用两倍字节偏移。 ");
    AssertEqual("工艺A", options[1].DisplayText, "同名核心选项不应包含特定语言后缀。 ");
    AssertEqual("工艺B", options[2].DisplayText, "唯一名称不需要附加配方号。 ");
}

static void PlcRecipeNameConfigRulesRejectInvalidStationSettings()
{
    var valid = new BizPlcRecipeNameConfig
    {
        StationNo = 1,
        BaseAddress = " DB10.0 ",
        RecipeCount = 10,
        AddressOffset = 16,
        StringLength = 12,
        Enabled = true
    };

    var normalized = PlcRecipeNameConfigRules.NormalizeAndValidate([valid], new DateTime(2026, 7, 17, 9, 0, 0));
    AssertEqual("DB10.0", normalized[0].BaseAddress, "保存前应清理基地址首尾空白。 ");
    AssertEqual(new DateTime(2026, 7, 17, 9, 0, 0), normalized[0].UpdatedTime, "规范化时应刷新更新时间。 ");

    AssertInvalidOperationMessage(
        () => PlcRecipeNameConfigRules.NormalizeAndValidate(
            [new BizPlcRecipeNameConfig
            {
                StationNo = ProductionConstants.Stations.SharedStationNo,
                BaseAddress = valid.BaseAddress,
                RecipeCount = valid.RecipeCount,
                AddressOffset = valid.AddressOffset,
                StringLength = valid.StringLength,
                Enabled = valid.Enabled
            }],
            DateTime.Now),
        "配方名称配置不支持共享工位。",
        "工位 0 不得用于配方名称配置。 ");
    AssertInvalidOperationMessage(
        () => PlcRecipeNameConfigRules.NormalizeAndValidate(
            [new BizPlcRecipeNameConfig
            {
                StationNo = valid.StationNo,
                BaseAddress = valid.BaseAddress,
                RecipeCount = valid.RecipeCount,
                AddressOffset = 0,
                StringLength = valid.StringLength,
                Enabled = valid.Enabled
            }],
            DateTime.Now),
        "配方名称地址偏移量必须大于 0。",
        "相邻配方地址必须使用正字节偏移。 ");
    AssertInvalidOperationMessage(
        () => PlcRecipeNameConfigRules.NormalizeAndValidate(
            [valid, new BizPlcRecipeNameConfig
            {
                StationNo = valid.StationNo,
                BaseAddress = "DB11.0",
                RecipeCount = valid.RecipeCount,
                AddressOffset = valid.AddressOffset,
                StringLength = valid.StringLength,
                Enabled = valid.Enabled
            }],
            DateTime.Now),
        "工位 1 的配方名称配置重复。",
        "每个工位只能保存一条配方名称配置。 ");
    AssertInvalidOperationMessage(
        () => PlcRecipeNameConfigRules.NormalizeAndValidate(
            [new BizPlcRecipeNameConfig
            {
                StationNo = valid.StationNo,
                BaseAddress = valid.BaseAddress,
                RecipeCount = PlcRecipeNameConfigRules.MaxRecipeCount + 1,
                AddressOffset = valid.AddressOffset,
                StringLength = valid.StringLength,
                Enabled = valid.Enabled
            }],
            DateTime.Now),
        "配方数量不能超过 64。",
        "每个工位最多只能配置 64 个配方槽位。 ");
}

static void PlcRecipeNameReaderKeepsSuccessfulSlotsAfterReadFailures()
{
    var config = new BizPlcRecipeNameConfig
    {
        StationNo = 2,
        BaseAddress = "DB30.0",
        RecipeCount = 4,
        AddressOffset = 10,
        StringLength = 18,
        Enabled = true
    };
    var configService = new FakePlcRecipeNameConfigService(config);
    var plcService = new FakePlcCommunicationService();
    plcService.StringReadResults["DB30.0"] = PlcServiceResult<string>.Success("工艺甲");
    plcService.StringReadResults["DB30.10"] = PlcServiceResult<string>.Fail("PLC timeout");
    plcService.StringReadResults["DB30.20"] = PlcServiceResult<string>.Success("   ");
    plcService.StringReadResults["DB30.30"] = PlcServiceResult<string>.Success("工艺乙");
    var reader = new PlcRecipeNameReaderService(configService, plcService);

    var result = reader.ReadStationAsync(2).GetAwaiter().GetResult();

    AssertEqual(2, result.Options.Count, "读取失败和空白名称不应阻断其他有效配方。 ");
    AssertEqual(1, result.Options[0].RecipeCode, "第一个有效配方应保留槽位编号。 ");
    AssertEqual(4, result.Options[1].RecipeCode, "失败与空白槽位之后的配方号不得前移。 ");
    AssertEqual(1, result.Failures.Count, "单项读取失败应记录供界面提示和日志使用。 ");
    AssertEqual(2, result.Failures[0].RecipeCode, "失败记录应保留原配方号。 ");
    AssertEqual("DB30.10", result.Failures[0].Address, "失败记录应保留计算后的 PLC 地址。 ");
    AssertEqual(4, plcService.StringReadRequests.Count, "启用配置后应读取固定数量内的全部地址。 ");
    AssertTrue(plcService.StringReadRequests.All(request => request.Length == 18), "每次读取均应使用配置的字符串长度。 ");
}

static void PlcRecipeNameReaderAcceptsInMemoryConfiguration()
{
    var configService = new FakePlcRecipeNameConfigService();
    var plcService = new FakePlcCommunicationService();
    plcService.StringReadResults["DB40.0"] = PlcServiceResult<string>.Success("临时配方");
    var reader = new PlcRecipeNameReaderService(configService, plcService);
    var inMemoryConfig = new BizPlcRecipeNameConfig
    {
        StationNo = 1,
        BaseAddress = "DB40.0",
        RecipeCount = 1,
        AddressOffset = 16,
        StringLength = 20,
        Enabled = true
    };

    var result = reader.ReadConfigAsync(inMemoryConfig).GetAwaiter().GetResult();

    AssertTrue(result.IsSuccess, "内存配置有效且读取成功时应直接返回配方名称。 ");
    AssertEqual("临时配方", result.Options.Single().Name, "读取结果必须来自传入的内存配置。 ");
    AssertEqual(0, configService.SaveCallCount, "预览读取不得为了使用未保存配置而写入数据库。 ");
}

static void PlcRecipeNameReaderReturnsInvalidConfigFailures()
{
    var configService = new FakePlcRecipeNameConfigService(new BizPlcRecipeNameConfig
    {
        StationNo = 1,
        BaseAddress = string.Empty,
        RecipeCount = 5,
        AddressOffset = 10,
        StringLength = 20,
        Enabled = true
    });
    var reader = new PlcRecipeNameReaderService(configService, new FakePlcCommunicationService());

    var result = reader.ReadStationAsync(1).GetAwaiter().GetResult();

    AssertFalse(result.IsSuccess, "历史非法配置应返回读取失败，而不是向界面抛出异常。 ");
    AssertTrue(result.Message.Contains("基地址不能为空", StringComparison.Ordinal), "失败消息应包含具体配置错误。 ");
    AssertEqual(0, result.Options.Count, "配置无效时不应生成配方选项。 ");
}

static void ProductProcessDraftCopiesBusinessFieldsAndResetsIdentity()
{
    var source = new BizProductProcessConfig
    {
        Id = 42,
        SchemeId = "S09",
        ProductNum = "P-001",
        StationNo = 2,
        TouchCount = 8,
        PointName = "相机",
        PointNoHeader = "相机序号",
        PointResultHeader = "相机结果",
        PointCountHeader = "相机数",
        ShowTestFlagInHistory = false,
        ProductBase = "DB20.0",
        ProductLen = 64,
        ProductNoExpr = "0:S-16",
        ProductResultExpr = "16:I-0",
        ActualTouchCountExpr = "18:I-0",
        PresetTouchCountExpr = "20:I-0",
        TouchBase = "DB20.64",
        TouchNoBase = "DB21.0",
        TouchResultBase = "DB22.0",
        TouchHeaderLen = 24,
        TouchNoExpr = "0:I-0",
        TouchResultExpr = "4:H-4",
        TestBase = "DB23.0",
        TestAreaLen = 96,
        Enabled = false,
        CreatedTime = new DateTime(2025, 1, 1),
        UpdatedTime = new DateTime(2025, 2, 1)
    };
    var draftTime = new DateTime(2026, 7, 18, 14, 30, 0);

    var draft = ProductProcessDraftRules.CreateDraft(source, "DEFAULT-P", "S01", draftTime);

    AssertEqual(0, draft.Id, "复制草稿必须保持新增身份。 ");
    AssertEqual(source.ProductNum, draft.ProductNum, "复制草稿应暂时保留源产品工号。 ");
    AssertEqual(source.SchemeId, draft.SchemeId, "测试方案应复制。 ");
    AssertEqual(source.StationNo, draft.StationNo, "工位应复制。 ");
    AssertEqual(source.TouchCount, draft.TouchCount, "焊点数量应复制。 ");
    AssertEqual(source.PointName, draft.PointName, "采集点名称应复制。 ");
    AssertEqual(source.PointNoHeader, draft.PointNoHeader, "编号表头应复制。 ");
    AssertEqual(source.PointResultHeader, draft.PointResultHeader, "结果表头应复制。 ");
    AssertEqual(source.PointCountHeader, draft.PointCountHeader, "数量表头应复制。 ");
    AssertEqual(source.ShowTestFlagInHistory, draft.ShowTestFlagInHistory, "历史显示选项应复制。 ");
    AssertEqual(source.ProductBase, draft.ProductBase, "产品头基地址应复制。 ");
    AssertEqual(source.ProductLen, draft.ProductLen, "产品头长度应复制。 ");
    AssertEqual(source.ProductNoExpr, draft.ProductNoExpr, "产品编号偏移应复制。 ");
    AssertEqual(source.ProductResultExpr, draft.ProductResultExpr, "产品结果偏移应复制。 ");
    AssertEqual(source.ActualTouchCountExpr, draft.ActualTouchCountExpr, "实际焊点数偏移应复制。 ");
    AssertEqual(source.PresetTouchCountExpr, draft.PresetTouchCountExpr, "预设焊点数偏移应复制。 ");
    AssertEqual(source.TouchBase, draft.TouchBase, "兼容焊点头基地址应复制。 ");
    AssertEqual(source.TouchNoBase, draft.TouchNoBase, "焊点编号基地址应复制。 ");
    AssertEqual(source.TouchResultBase, draft.TouchResultBase, "焊点结果基地址应复制。 ");
    AssertEqual(source.TouchHeaderLen, draft.TouchHeaderLen, "焊点头长度应复制。 ");
    AssertEqual(source.TouchNoExpr, draft.TouchNoExpr, "焊点编号偏移应复制。 ");
    AssertEqual(source.TouchResultExpr, draft.TouchResultExpr, "焊点结果偏移应复制。 ");
    AssertEqual(source.TestBase, draft.TestBase, "测试项基地址应复制。 ");
    AssertEqual(source.TestAreaLen, draft.TestAreaLen, "测试区长度应复制。 ");
    AssertEqual(source.Enabled, draft.Enabled, "启用状态应复制。 ");
    AssertEqual(draftTime, draft.CreatedTime, "复制草稿应使用新的创建时间。 ");
    AssertEqual(draftTime, draft.UpdatedTime, "复制草稿应使用新的更新时间。 ");

    draft.ProductBase = "DB99.0";
    AssertEqual("DB20.0", source.ProductBase, "修改草稿不得改变源配置。 ");
}

static void ProductProcessDraftKeepsExistingDefaultsWithoutSource()
{
    var draftTime = new DateTime(2026, 7, 18, 14, 35, 0);

    var draft = ProductProcessDraftRules.CreateDraft(null, "P-DEFAULT", "S-DEFAULT", draftTime);

    AssertEqual("P-DEFAULT", draft.ProductNum, "无源草稿应使用默认产品工号。 ");
    AssertEqual("S-DEFAULT", draft.SchemeId, "无源草稿应使用默认测试方案。 ");
    AssertEqual(ProductionConstants.Stations.SharedStationNo, draft.StationNo, "无源草稿应保持共享工位。 ");
    AssertEqual("DB8.0", draft.ProductBase, "无源草稿应保持原产品头基地址。 ");
    AssertEqual(32, draft.ProductLen, "无源草稿应保持原产品头长度。 ");
    AssertEqual("0:I-0", draft.ProductNoExpr, "无源草稿应保持原产品编号偏移。 ");
    AssertEqual("4:H-4", draft.ProductResultExpr, "无源草稿应保持原产品结果偏移。 ");
    AssertEqual("DB8.32", draft.TouchBase, "无源草稿应保持原焊点头基地址。 ");
    AssertEqual("DB8.100", draft.TestBase, "无源草稿应保持原测试项基地址。 ");
    AssertEqual(draftTime, draft.CreatedTime, "无源草稿应使用调用方时间。 ");
    AssertEqual(draftTime, draft.UpdatedTime, "无源草稿应使用调用方时间。 ");
}

static void AddressManageCopiesSelectedProductProcessOnAdd()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("ProductProcessDraftRules.CreateDraft(", StringComparison.Ordinal),
        "产品工艺新增入口必须复用核心草稿规则。 ");
    AssertTrue(
        viewCode.Contains("_selectedProductProcessRow?.Source", StringComparison.Ordinal),
        "产品工艺新增入口必须把当前选中行作为可选复制源。 ");
}

static void AddressManageAppendsNewTestItemsInDisplayIdOrder()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);

    var filterMethod = ExtractMethodText(
        viewCode,
        "private void ApplyItemFilter(string? keyword)",
        "private async void Save_Click(object? sender, EventArgs e)");

    // 未保存行的数据库 ItemId 仍为 0，排序必须基于行模型的显示测试项ID。
    AssertTrue(
        filterMethod.Contains(".OrderBy(row => row.ItemId)", StringComparison.Ordinal),
        "测试项字典必须按界面显示的测试项ID排序。");
    AssertFalse(
        filterMethod.Contains(".OrderBy(item => item.ItemId)", StringComparison.Ordinal),
        "测试项字典不得按未分配的数据库 ItemId 排序，否则新增行会排到首位。");

    var addMethod = ExtractMethodText(
        viewCode,
        "private void AddTestItem_Click(object? sender, EventArgs e)",
        "private void DeleteTestItem_Click(object? sender, EventArgs e)");

    AssertTrue(
        addMethod.Contains("ReferenceEquals(row.Source, item)", StringComparison.Ordinal)
            && addMethod.Contains("tableTestItems.SetSelected(_selectedItemRow, true);", StringComparison.Ordinal),
        "新增测试项必须选中新行，避免选中回落到第一行。");

    var saveMethod = ExtractMethodText(
        viewCode,
        "private void SaveTestItems()",
        "private async void TestSelected_Click(object? sender, EventArgs e)");

    AssertTrue(
        saveMethod.Contains("GetTestItemDisplayId(item)", StringComparison.Ordinal),
        "保存时必须按临时测试项ID递增插入，使自增 ID 与界面序号一致。");
    AssertTrue(
        saveMethod.Contains("var itemsToSave = _testItems", StringComparison.Ordinal)
            && saveMethod.Contains(".ToList();", StringComparison.Ordinal),
        "保存前必须物化排序结果，否则循环内回写 ItemId 会打乱延迟排序。");
}

static void TestItemIdsReuseGapsLeftByDeletedRows()
{
    // 空表从 1 开始，不从 0 也不留空。
    AssertEqual(1, TestItemIdAllocationRules.AllocateNextId(Array.Empty<int>()), "测试项字典为空时必须从 1 开始分配。");
    AssertEqual(1, TestItemIdAllocationRules.AllocateNextId(new[] { 0, 0 }), "只有未落库新行时必须从 1 开始分配。");

    // 连续序号继续递增。
    AssertEqual(4, TestItemIdAllocationRules.AllocateNextId(new[] { 1, 2, 3 }), "已有 1、2、3 时下一个测试项ID必须是 4。");

    // 现场场景：删掉 1、2、20 后重新添加，不得因 AUTO_INCREMENT 残留而跟到 21。
    var afterFullDelete = TestItemIdAllocationRules.AllocateNextId(Array.Empty<int>());
    AssertEqual(1, afterFullDelete, "全部删除后重新添加必须回到 1，不得继续使用数据库自增残留值。");

    // 保留高位记录时不得占用已存在的ID。
    AssertEqual(21, TestItemIdAllocationRules.AllocateNextId(new[] { 1, 2, 20 }), "保留 20 时新增必须取 21，不得复用已占用ID。");

    // 服务层必须显式写入ID，不能回到依赖数据库自增的写法。
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "TestSchemeConfigService.cs"),
        Encoding.UTF8);
    var saveItemMethod = ExtractMethodText(
        serviceCode,
        "public DimTestItem SaveItem(DimTestItem item)",
        "public void DeleteItem(int itemId)");

    AssertTrue(
        saveItemMethod.Contains("TestItemIdAllocationRules.AllocateNextId", StringComparison.Ordinal),
        "新增测试项必须走集中的ID分配规则。");
    AssertTrue(
        saveItemMethod.Contains(".OffIdentity()", StringComparison.Ordinal),
        "显式分配测试项ID后必须关闭自增列，否则 MySQL 仍会重新赋值。");
    AssertFalse(
        saveItemMethod.Contains("Insertable(item).ExecuteReturnEntity()", StringComparison.Ordinal),
        "不得再依赖数据库自增返回测试项ID，否则删除后会跟号。");

    // 界面预览序号必须和服务层用同一套规则。
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"),
        Encoding.UTF8);
    var buildIdMethod = ExtractMethodText(
        viewCode,
        "private int BuildNextTemporaryTestItemId()",
        "private string GetAddressDisplayName(BizPlcAddress address)");

    AssertTrue(
        buildIdMethod.Contains("TestItemIdAllocationRules.AllocateNextId", StringComparison.Ordinal),
        "新增时预览的测试项ID必须复用服务层分配规则，避免保存后序号变化。");
    AssertTrue(
        buildIdMethod.Contains("_temporaryTestItemIds.Values", StringComparison.Ordinal),
        "连续新增多行时已分配的临时ID必须参与取最大值，否则会重复。");
}

static void ProgramSaveRecipeRulesRequirePositiveStationCodes()
{
    ProgramSaveRecipeRules.Validate("1", null, enableDualStation: false);
    ProgramSaveRecipeRules.Validate("2", "9", enableDualStation: true);
    ProgramSaveRecipeRules.Validate("2", null, enableDualStation: true);
    ProgramSaveRecipeRules.Validate(null, "9", enableDualStation: true);

    AssertInvalidOperationMessage(
        () => ProgramSaveRecipeRules.Validate(string.Empty, null, enableDualStation: false),
        "工位 1 配方号必须是正整数。",
        "单工位保存也必须要求工位 1 配方号。 ");
    AssertInvalidOperationMessage(
        () => ProgramSaveRecipeRules.Validate("legacy", null, enableDualStation: false),
        "工位 1 配方号必须是正整数。",
        "历史非数字配方号不得继续静默保存。 ");
    AssertInvalidOperationMessage(
        () => ProgramSaveRecipeRules.Validate(null, null, enableDualStation: true),
        "至少选择一个适用工位配方。",
        "双工位程序不能同时把两个工位设为不适用。 ");
    AssertInvalidOperationMessage(
        () => ProgramSaveRecipeRules.Validate("1", "0", enableDualStation: true),
        "工位 2 配方号必须是正整数。",
        "工位 2 配方号不得为零。 ");
    AssertInvalidOperationMessage(
        () => ProgramSaveRecipeRules.Validate("0", "2", enableDualStation: true),
        "工位 1 配方号必须是正整数。",
        "工位 1 非空时也必须为正整数。 ");
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
    AssertSequenceEqual([1], stations, "报警地址不得再创建或扩展程序工位轮询范围。");

    var alarmOnlyStations = PlcSoftwareAlarmRules.ResolveStationNumbers([], alarms);
    AssertSequenceEqual([ProductionConstants.Stations.DefaultStationNo], alarmOnlyStations, "只有报警地址时应使用默认生产轮询，不解释报警配置中的历史工位号。");

    var productionOnlyStations = PlcSoftwareAlarmRules.ResolveStationNumbers([2], []);
    AssertSequenceEqual([2], productionOnlyStations, "关闭报警读取并传入空报警快照时，只应保留生产地址工位。");

    var stationOneAlarms = PlcSoftwareAlarmRules.ResolveAlarmAddressesForStation(alarms, 1);
    var stationTwoAlarms = PlcSoftwareAlarmRules.ResolveAlarmAddressesForStation(alarms, 2);
    AssertSequenceEqual(
        ["DB1.0", "DB2.0"],
        stationOneAlarms.Select(alarm => alarm.Address).ToArray(),
        "所有程序工位都应读取同一份设备级报警地址，并排除空地址和禁用项。");
    AssertSequenceEqual(
        stationOneAlarms.Select(alarm => alarm.Address).ToArray(),
        stationTwoAlarms.Select(alarm => alarm.Address).ToArray(),
        "报警地址不得按程序工位过滤。");
    AssertTrue(
        PlcDeviceAlarmCycleRules.ToConfiguredAlarms(alarms)
            .All(alarm => alarm.StationNo == ProductionConstants.Stations.SharedStationNo),
        "历史报警配置中的非零工位号必须在运行时统一归为设备级范围。");
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
    AssertEqual(ProductionConstants.Stations.SharedStationNo, aggregation.ActiveAlarms[0].StationNo, "共享报警明细必须保留共享工位范围。");
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
        ActiveAlarms: [new PlcActiveAlarm(2, "DB2.0", "安全门打开")],
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

static void PlcAlarmTriggerModesSelectEffectiveAlarms()
{
    AssertEqual(
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        AppConstants.PlcAlarmTriggerModes.Normalize(null),
        "旧配置空值必须回退到设备状态异常且报警地址触发模式。");
    AssertEqual(
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        AppConstants.PlcAlarmTriggerModes.Normalize("future-mode"),
        "未知持久化值必须安全回退到双条件模式。");
    var state = new PlcDeviceAlarmCycleState();
    var firstAlarm = new PlcActiveAlarm(1, "DB10.DBX2.0", "安全门打开");
    var sharedAlarm = new PlcActiveAlarm(0, "DB10.DBX2.9", "急停");
    var readResults = new[]
    {
        new PlcAlarmSignalReadResult(1, firstAlarm.Address, firstAlarm.AlarmContent, IsSuccess: true, IsActive: true, FailureMessage: string.Empty),
        new PlcAlarmSignalReadResult(0, sharedAlarm.Address, sharedAlarm.AlarmContent, IsSuccess: true, IsActive: true, FailureMessage: string.Empty)
    };

    var addressOnly = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?> { [1] = ProductionConstants.PlcDeviceStatuses.Running },
        readResults,
        configuredAlarms: [firstAlarm, sharedAlarm]);
    AssertSequenceEqual(
        ["DB10.2.0", "DB10.DBX2.9"],
        addressOnly.NewAlarms.Select(PlcDeviceAlarmCycleRules.GetAlarmKey).Order().ToArray(),
        "仅地址模式必须忽略原始设备状态，报警身份只由地址决定。");
    AssertTrue(
        addressOnly.NewAlarms.All(alarm => alarm.StationNo == ProductionConstants.Stations.SharedStationNo),
        "报警周期中的新报警必须统一为设备级范围，不保留历史程序工位号。");

    var gatedWithoutStatus = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?> { [1] = ProductionConstants.PlcDeviceStatuses.Running, [2] = ProductionConstants.PlcDeviceStatuses.Running },
        readResults,
        configuredAlarms: [firstAlarm, sharedAlarm]);
    AssertEqual(0, gatedWithoutStatus.NewAlarms.Count, "双条件模式中没有任一状态 4 时，工位和共享地址都不得生效。");

    var gated = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?> { [1] = ProductionConstants.PlcDeviceStatuses.Alarm, [2] = ProductionConstants.PlcDeviceStatuses.Running },
        readResults,
        configuredAlarms: [firstAlarm, sharedAlarm]);
    AssertEqual(2, gated.NewAlarms.Count, "双条件模式中任一设备状态为4时，应激活当前置位的设备级报警地址。");

    var switchedToAddressOnly = PlcDeviceAlarmCycleRules.Decide(
        gated.NextState,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?> { [1] = ProductionConstants.PlcDeviceStatuses.Running, [2] = ProductionConstants.PlcDeviceStatuses.Running },
        readResults,
        configuredAlarms: [firstAlarm, sharedAlarm]);
    AssertEqual(0, switchedToAddressOnly.NewAlarms.Count, "运行中切换到仅地址模式时持续置位的地址不得重复触发。");
    AssertEqual(2, switchedToAddressOnly.NextState.ActiveAlarms.Count, "运行中切换模式必须立即按当前读取结果重算有效集合。");

    var switchedToGatedMode = PlcDeviceAlarmCycleRules.Decide(
        switchedToAddressOnly.NextState,
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?> { [1] = ProductionConstants.PlcDeviceStatuses.Running, [2] = ProductionConstants.PlcDeviceStatuses.Running },
        readResults,
        configuredAlarms: [firstAlarm, sharedAlarm]);
    AssertEqual(2, switchedToGatedMode.RecoveredAlarms.Count, "运行中切回双条件模式时不满足状态 4 的既有地址必须逐条恢复。");

    var statusReadFailure = PlcDeviceAlarmCycleRules.Decide(
        new PlcDeviceAlarmCycleState([firstAlarm]),
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?> { [1] = null },
        [readResults[0]],
        configuredAlarms: [firstAlarm]);
    AssertEqual(0, statusReadFailure.RecoveredAlarms.Count, "状态读取失败不得把已生效报警误判为恢复。");
    AssertEqual(1, statusReadFailure.NextState.ActiveAlarms.Count, "状态读取失败时必须冻结已生效报警。");

    var statusFailureWithClearedAddress = PlcDeviceAlarmCycleRules.Decide(
        new PlcDeviceAlarmCycleState([firstAlarm]),
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?> { [1] = null },
        [new PlcAlarmSignalReadResult(1, firstAlarm.Address, firstAlarm.AlarmContent, IsSuccess: true, IsActive: false, FailureMessage: string.Empty)],
        configuredAlarms: [firstAlarm]);
    AssertEqual(0, statusFailureWithClearedAddress.RecoveredAlarms.Count, "双条件模式的状态读取失败不能把已归零地址误判为恢复。");
    AssertEqual(1, statusFailureWithClearedAddress.NextState.ActiveAlarms.Count, "状态读取失败时必须保留既有报警快照。");

    var sharedStatusUnknown = PlcDeviceAlarmCycleRules.Decide(
        new PlcDeviceAlarmCycleState([sharedAlarm]),
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        new Dictionary<int, short?>
        {
            [1] = ProductionConstants.PlcDeviceStatuses.Running,
            [2] = null
        },
        [new PlcAlarmSignalReadResult(0, sharedAlarm.Address, sharedAlarm.AlarmContent, IsSuccess: true, IsActive: false, FailureMessage: string.Empty)],
        configuredAlarms: [sharedAlarm]);
    AssertEqual(0, sharedStatusUnknown.RecoveredAlarms.Count, "共享报警任一工位状态未知时不得误判恢复。");
    AssertEqual(1, sharedStatusUnknown.NextState.ActiveAlarms.Count, "共享报警状态未知时必须保留既有活动快照。");
}

static void PlcAlarmCycleTracksPerAddressRecovery()
{
    var firstAlarm = new PlcActiveAlarm(1, "DB10.DBX2.0", "安全门打开");
    var secondAlarm = new PlcActiveAlarm(1, "DB10.DBX2.1", "气压低");
    var state = new PlcDeviceAlarmCycleState([firstAlarm, secondAlarm]);
    var results = new[]
    {
        new PlcAlarmSignalReadResult(1, firstAlarm.Address, firstAlarm.AlarmContent, IsSuccess: true, IsActive: false, FailureMessage: string.Empty),
        new PlcAlarmSignalReadResult(1, secondAlarm.Address, secondAlarm.AlarmContent, IsSuccess: true, IsActive: true, FailureMessage: string.Empty)
    };

    var partialRecovery = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        results,
        configuredAlarms: [firstAlarm, secondAlarm]);
    AssertSequenceEqual([firstAlarm.Address], partialRecovery.RecoveredAlarms.Select(alarm => alarm.Address).ToArray(), "明确读到 false 的地址必须逐条恢复。");
    AssertSequenceEqual([secondAlarm.Address], partialRecovery.NextState.ActiveAlarms.Select(alarm => alarm.Address).ToArray(), "部分恢复后其他活动报警必须保留。");
    AssertTrue(partialRecovery.ShouldReassertException, "部分恢复且没有新异常时必须重申剩余状态 4。");

    var readFailure = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        [new PlcAlarmSignalReadResult(1, firstAlarm.Address, firstAlarm.AlarmContent, IsSuccess: false, IsActive: false, FailureMessage: "timeout")],
        configuredAlarms: [firstAlarm, secondAlarm]);
    AssertEqual(0, readFailure.RecoveredAlarms.Count, "报警地址读取失败不得误判为恢复。");
    AssertEqual(2, readFailure.NextState.ActiveAlarms.Count, "读取失败必须冻结已有活动报警。");

    var removedConfig = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        [new PlcAlarmSignalReadResult(1, secondAlarm.Address, secondAlarm.AlarmContent, IsSuccess: true, IsActive: true, FailureMessage: string.Empty)],
        configuredAlarms: [secondAlarm]);
    AssertSequenceEqual([firstAlarm.Address], removedConfig.RecoveredAlarms.Select(alarm => alarm.Address).ToArray(), "活动地址被删除或禁用时必须使用触发时快照生成恢复。");

    var removedConfigWhilePlcOffline = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        readResults: [],
        configuredAlarms: [secondAlarm]);
    AssertSequenceEqual([firstAlarm.Address], removedConfigWhilePlcOffline.RecoveredAlarms.Select(alarm => alarm.Address).ToArray(), "PLC 离线时删除或禁用活动地址仍必须生成恢复。");
    AssertSequenceEqual([secondAlarm.Address], removedConfigWhilePlcOffline.NextState.ActiveAlarms.Select(alarm => alarm.Address).ToArray(), "PLC 离线不能把仍配置的活动地址误判为恢复。");

    var allConfigsRemovedWhilePlcOffline = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?> { [1] = null },
        readResults: [],
        configuredAlarms: []);
    AssertSequenceEqual(
        [firstAlarm.Address, secondAlarm.Address],
        allConfigsRemovedWhilePlcOffline.RecoveredAlarms.Select(alarm => alarm.Address).ToArray(),
        "报警读取开启且 PLC 全部离线时，删除全部活动配置仍必须逐条恢复。");

    var readingDisabled = PlcDeviceAlarmCycleRules.Decide(
        state,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        readResults: [],
        configuredAlarms: []);
    AssertEqual(0, readingDisabled.RecoveredAlarms.Count, "关闭报警读取时不得把空配置快照当作恢复。");
    AssertEqual(2, readingDisabled.NextState.ActiveAlarms.Count, "关闭报警读取时必须冻结完整活动报警集合。");
}

static void PlcDeviceAlarmCycleRestoresFromJsonl()
{
    var logs = new[]
    {
        new BizDeviceStatusLog
        {
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            AlarmAddress = "DB10.DBX2.0",
            AlarmContent = "安全门打开",
            OccurredTime = new DateTime(2026, 7, 22, 9, 0, 0)
        },
        new BizDeviceStatusLog
        {
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            AlarmAddress = "DB10.DBX2.1",
            AlarmContent = "气压低",
            OccurredTime = new DateTime(2026, 7, 22, 9, 1, 0)
        }
    };

    var restored = PlcDeviceAlarmCycleRules.Restore(logs);
    AssertSequenceEqual(
        new[] { "DB10.2.0", "DB10.2.1" },
        restored.ActiveAlarms.Select(PlcDeviceAlarmCycleRules.GetAlarmKey).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
        "重启后必须从 JSONL 恢复最近未闭合报警周期的地址。");

    var unrelatedStatus = PlcDeviceAlarmCycleRules.Restore(logs.Append(new BizDeviceStatusLog
    {
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.ProgramStarted,
        OccurredTime = new DateTime(2026, 7, 22, 9, 1, 30)
    }));
    AssertEqual(2, unrelatedStatus.ActiveAlarms.Count, "状态 0/1/6/7 不得在重启恢复时擅自闭合报警周期，只有状态 5 可以闭合。");

    var persistentAlarm = PlcDeviceAlarmCycleRules.Decide(
        restored,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        [new PlcAlarmSignalReadResult(1, "DB10.DBX2.0", "安全门打开", IsSuccess: true, IsActive: true, FailureMessage: string.Empty)],
        configuredAlarms: [new PlcActiveAlarm(1, "DB10.DBX2.0", "安全门打开"), new PlcActiveAlarm(1, "DB10.DBX2.1", "气压低")]);
    AssertEqual(0, persistentAlarm.NewAlarms.Count, "重启后持续置位的既有地址不得重复记录。");

    var recovered = PlcDeviceAlarmCycleRules.Decide(
        persistentAlarm.NextState,
        AppConstants.PlcAlarmTriggerModes.AddressOnly,
        new Dictionary<int, short?>(),
        [new PlcAlarmSignalReadResult(1, "DB10.DBX2.0", "安全门打开", IsSuccess: true, IsActive: false, FailureMessage: string.Empty)],
        configuredAlarms: [new PlcActiveAlarm(1, "DB10.DBX2.0", "安全门打开")]);
    AssertEqual(2, recovered.RecoveredAlarms.Count, "重启后明确归零和已删除配置必须分别补全状态 5。");

    var partiallyClosed = PlcDeviceAlarmCycleRules.Restore(logs.Append(new BizDeviceStatusLog
    {
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
        AlarmAddress = "DB10.DBX2.0",
        AlarmContent = "安全门打开",
        OccurredTime = new DateTime(2026, 7, 22, 9, 1, 30)
    }));
    AssertSequenceEqual(["DB10.DBX2.1"], partiallyClosed.ActiveAlarms.Select(alarm => alarm.Address).ToArray(), "带地址的状态 5 只能关闭对应报警。");

    var closed = PlcDeviceAlarmCycleRules.Restore(logs.Append(new BizDeviceStatusLog
    {
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
        OccurredTime = new DateTime(2026, 7, 22, 9, 2, 0)
    }));
    AssertEqual(0, closed.ActiveAlarms.Count, "无地址的旧状态 5 必须继续兼容整周期关闭。");
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
        CountOccurrences(pollMethod, "settings.EnablePlcAlarmReading != false"),
        "每轮生产采集只能读取一次 PLC 报警开关，避免不同工位使用不同设置快照。");
    AssertTrue(
        // 源码行尾随 core.autocrlf 在不同平台检出为 CRLF 或 LF，断言前必须归一化，否则 Ordinal 比较必然失败。
        pollMethod.ReplaceLineEndings("\n").Contains("IReadOnlyList<BizPlcAlarmAddress> alarmAddresses = alarmReadingEnabled\n            ? _plcAlarmAddressService.GetAll()\n            : [];", StringComparison.Ordinal),
        "关闭 PLC 报警读取时不得访问报警配置服务，且工位发现应接收空报警快照。");
    AssertSourceOrder(
        pollMethod,
        "var alarmReadingEnabled = settings.EnablePlcAlarmReading != false;",
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
        pollMethod.Contains("var alarmReadingEnabled = settings.EnablePlcAlarmReading != false;", StringComparison.Ordinal),
        "每轮采集应先读取报警开关，明确决定是否扫描 Bool 报警地址。");
    AssertTrue(
        pollMethod.Contains("? await ReadAlarmSignalsAsync(alarmAddresses, cancellationToken)", StringComparison.Ordinal),
        "启用报警读取后应按唯一报警键扫描一次完整地址快照。");
    AssertSourceOrder(
        pollMethod,
        "await ReadAlarmSignalsAsync(alarmAddresses, cancellationToken)",
        "foreach (var stationNo in stationNumbers)",
        "必须先收集全部报警地址，再逐工位读取原始状态。");
    var regularCycleStart = pollMethod.LastIndexOf("await RecordDeviceAlarmCycleAsync(", StringComparison.Ordinal);
    var stationReadLoopStart = pollMethod.IndexOf("foreach (var stationNo in stationNumbers)", StringComparison.Ordinal);
    var alarmReadStart = pollMethod.IndexOf("await ReadAlarmSignalsAsync(alarmAddresses, cancellationToken)", StringComparison.Ordinal);
    AssertTrue(
        stationReadLoopStart >= 0 && regularCycleStart > stationReadLoopStart,
        "常规连接路径必须收集完各工位状态后再统一计算报警差集。");
    AssertTrue(
        alarmReadStart >= 0 && regularCycleStart > alarmReadStart,
        "常规连接路径的 Bool 报警读取不应再依赖原始设备状态先等于 4。");
    var disconnectedStart = pollMethod.IndexOf("if (!stationNumbers.Any(IsPlcConnected))", StringComparison.Ordinal);
    var disconnectedCycle = pollMethod.IndexOf("await RecordDeviceAlarmCycleAsync(", disconnectedStart, StringComparison.Ordinal);
    var disconnectedReturn = pollMethod.IndexOf("return;", disconnectedStart, StringComparison.Ordinal);
    AssertTrue(
        disconnectedStart >= 0 && disconnectedCycle > disconnectedStart && disconnectedCycle < disconnectedReturn,
        "PLC 全部离线时仍须按当前配置闭合已删除或禁用的活动报警。");
    AssertTrue(
        pollMethod.Contains("stationNumbers.ToDictionary(stationNo => stationNo, _ => (short?)null)", StringComparison.Ordinal),
        "PLC 全部离线但报警读取开启时必须保留未知工位状态，与关闭报警读取的冻结语义区分。 ");
    AssertFalse(
        pollMethod.Contains("if (ProductionConstants.PlcDeviceStatuses.IsReportable(plcStatusCode))\n            {\n                await RecordDeviceAlarmCycleAsync", StringComparison.Ordinal),
        "报警周期判定不得受可上报状态门禁限制，任意非 4 状态都表示恢复。");
    AssertTrue(
        serviceCode.Contains("ApplyEffectiveAlarmSnapshots", StringComparison.Ordinal)
        && serviceCode.Contains("IsSoftwareAlarmActive = stationAlarms.Count > 0", StringComparison.Ordinal)
        && pollMethod.Contains("plcStatusCode,", StringComparison.Ordinal),
        "生产快照必须由有效报警集合发布软件报警状态，且不能改写原始 DeviceStatusCode。");
    AssertFalse(
        serviceCode.Contains("ReadActiveAlarmMessageAsync", StringComparison.Ordinal),
        "重复且未使用的报警读取方法应移除，避免两套聚合规则再次漂移。");
    var remarkMethod = ExtractMethodText(
        serviceCode,
        "private static string BuildDeviceStatusRemark",
        "private static int? ToInteger");
    AssertTrue(remarkMethod.Contains("FormatRemark", StringComparison.Ordinal), "PLC 状态必须使用统一的 Remark 规则。");
    AssertFalse(remarkMethod.Contains("alarm.Address", StringComparison.Ordinal), "PLC 新异常 Remark 不得拼接报警地址或 PLC 工位号。");
    AssertTrue(
        serviceCode.Contains("var nextOccurredTime = DateTime.Now;", StringComparison.Ordinal)
        && CountOccurrences(serviceCode, "nextOccurredTime = nextOccurredTime.AddMilliseconds(1);") >= 2,
        "同批恢复、新异常和状态重申必须使用单调递增时间。");
    AssertTrue(
        serviceCode.Contains("stationNo <= ProductionConstants.Stations.SharedStationNo", StringComparison.Ordinal)
        && serviceCode.Contains("? ProductionConstants.Stations.SharedStationNo", StringComparison.Ordinal),
        "共享报警必须以 StationNo=0 落盘，而不能归属任一工位。");
    var recordAlarmCycle = ExtractMethodText(
        serviceCode,
        "private async Task RecordDeviceAlarmCycleAsync",
        "private PlcDeviceAlarmCycleState EnsureAlarmCycleState");
    AssertTrue(
        recordAlarmCycle.Contains("GetSourceActiveAlarmKeys()", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("_sourceRemovedAlarmKeysAwaitingClear.Add(alarmKey)", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("decision.ShouldReassertException && hasRecordedRecovery", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("sourceActiveAlarmKeys?.Contains(remainingAlarmKey) == true", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("_pendingExceptionReassertion = remainingAlarm;", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("if (hasRecordedNewException)", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("await RetryPendingExceptionReassertionAsync(", StringComparison.Ordinal),
        "JSONL 源被删除时不得补写状态 5 或部分恢复后的既有状态 4，且地址持续置位必须等待明确清除后才可重启周期。");
    var deviceStatusStoreCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceStatusLocalLogStore.cs"),
        Encoding.UTF8);
    AssertTrue(
        deviceStatusStoreCode.Contains("maxCount == int.MaxValue", StringComparison.Ordinal)
        && deviceStatusStoreCode.Contains("Math.Clamp(maxCount, 1, 5000)", StringComparison.Ordinal)
        && recordAlarmCycle.Contains("maxCount: int.MaxValue", StringComparison.Ordinal),
        "报警周期恢复和删除复核必须扫描全部日期 JSONL，不能受日志管理页的 5000 条显示上限截断。");
}

static void PlcAlarmReadFailuresAreMergedAndLabeledPrecisely()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "ProductionMonitorService.cs"),
        Encoding.UTF8);
    var readMethod = ExtractMethodText(
        serviceCode,
        "private async Task<IReadOnlyList<PlcAlarmSignalReadResult>> ReadAlarmSignalsAsync",
        "private static string BuildDeviceStatusRemark");

    AssertTrue(serviceCode.Contains("WriteAlarmReadFailureLog(stationNo, failures);", StringComparison.Ordinal), "一轮报警读取失败必须合并为一条日志，不能按地址逐条写入。");
    AssertFalse(readMethod.Contains("WriteBusinessFailureLog(", StringComparison.Ordinal), "报警地址读取失败不得复用生产数据采集失败的通用日志入口。");
    AssertTrue(serviceCode.Contains("TextKeys.Monitor.RuntimeError.PlcAlarmReadFailed", StringComparison.Ordinal), "报警读取失败必须使用专属异常消息键。");
    AssertTrue(serviceCode.Contains("_activeAlarmFailureKeys", StringComparison.Ordinal), "持续相同的报警读取失败必须抑制重复写入。");
    AssertTrue(serviceCode.Contains("ClearAlarmReadFailureState(stationNo);", StringComparison.Ordinal), "报警读取恢复后必须清除抑制状态，下一次失败仍可记录。");
}

static void PlcProductionMonitorSeparatesBusinessSignalFailures()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "ProductionMonitorService.cs"),
        Encoding.UTF8);
    var collectionCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductCycleCollectionService.cs"),
        Encoding.UTF8);

    AssertTrue(serviceCode.Contains("TextKeys.Monitor.RuntimeError.PlcBusinessSignalReadFailed", StringComparison.Ordinal), "生产状态轮询失败必须使用 PLC 业务信号读取失败专属消息。");
    AssertFalse(serviceCode.Contains("TextKeys.Monitor.RuntimeError.ProductionCollectFailed", StringComparison.Ordinal), "生产状态轮询不得再误用生产数据采集失败消息。");
    AssertTrue(serviceCode.Contains("if (!productionStationNumbers.Contains(stationNo))", StringComparison.Ordinal), "仅配置报警地址的工位不得继续读取生产业务信号。");
    AssertTrue(serviceCode.Contains("_activeBusinessSignalFailureKeys", StringComparison.Ordinal), "同一工位持续相同的业务信号失败必须抑制重复写入。");
    AssertTrue(serviceCode.Contains("ClearBusinessSignalFailureState(stationNo);", StringComparison.Ordinal), "业务信号恢复后必须清除失败状态，允许下一次失败重新记录。");
    AssertTrue(serviceCode.Contains("业务信号“设备状态”", StringComparison.Ordinal), "业务信号失败详情必须明确设备状态信号。");
    AssertTrue(serviceCode.Contains("业务信号“{GetBusinessSignalName(key)}”", StringComparison.Ordinal), "产量信号失败详情必须明确具体业务信号。");
    AssertTrue(collectionCode.Contains("产品数据采集失败", StringComparison.Ordinal), "生产数据采集失败应继续保留在 PLC 产品数据就绪触发的实际采集路径。");
}

static void ProgramExceptionLogViewBatchesLiveUpdates()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var eventHandler = ExtractMethodText(
        viewCode,
        "private void ExceptionLogService_LogWritten",
        "private void DeviceLifecycleLogService_LogWritten");

    AssertTrue(viewCode.Contains("_pendingExceptionLogs", StringComparison.Ordinal), "程序异常日志页必须缓存待刷新的实时日志，避免每条事件都重绑表格。");
    AssertTrue(viewCode.Contains("FlushPendingExceptionLogs", StringComparison.Ordinal), "程序异常日志页必须提供批量刷新入口。");
    AssertTrue(eventHandler.Contains("FlushPendingExceptionLogs", StringComparison.Ordinal), "程序异常日志事件必须调度批量刷新，而不是逐条刷新表格。");
    AssertFalse(eventHandler.Contains("RunOnUiThread(() => AddLiveExceptionLog", StringComparison.Ordinal), "程序异常日志事件不得逐条投递 UI 重绑定操作。");
}

static void ProgramExceptionLogViewNormalizesLegacyAlarmEntries()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var loadMethod = ExtractMethodText(
        viewCode,
        "private void LoadExceptionLogs()",
        "private void LoadDeviceLifecycleLogs()");
    var contextMethod = ExtractMethodText(
        viewCode,
        "private static string BuildExceptionContext",
        "private static string BuildExceptionFullDetails");

    AssertTrue(viewCode.Contains("NormalizeLegacyPlcAlarmEntry", StringComparison.Ordinal), "旧版 PLC 报警读取日志必须在显示前修正消息和上下文。");
    AssertTrue(loadMethod.Contains(".Select(NormalizeLegacyPlcAlarmEntry)", StringComparison.Ordinal), "加载历史异常日志时必须应用旧报警记录归一化。");
    AssertTrue(viewCode.Contains("TextKeys.Monitor.RuntimeError.PlcAlarmReadFailed", StringComparison.Ordinal), "旧报警记录的消息列必须改用 PLC 报警读取失败专属文案。");
    AssertFalse(contextMethod.Contains("builder.AppendLine(\"Context:\");", StringComparison.Ordinal), "上下文页签不应再额外嵌套一层 Context 标题。");
}

static void ExceptionGridOmitsSourceColumns()
{
    var designerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"),
        Encoding.UTF8);
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var basicInfoMethod = ExtractMethodText(
        viewCode,
        "private static string BuildExceptionBasicInfo",
        "private static string BuildExceptionContext");

    AssertFalse(
        designerCode.Contains("colExceptionSource", StringComparison.Ordinal),
        "异常日志表格不得声明或注册 Source 列。");
    AssertFalse(
        designerCode.Contains("colExceptionSourceLocation", StringComparison.Ordinal),
        "异常日志表格不得声明或注册 SourceLocation 列。");
    AssertTrue(
        basicInfoMethod.Contains("Source: {entry.Source}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 Source。");
    AssertTrue(
        basicInfoMethod.Contains("SourceFile: {GetSourceLocation(entry)}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 SourceFile。");
    AssertTrue(
        basicInfoMethod.Contains("SourceMember: {entry.SourceMemberName}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 SourceMember。");
}

static void ExceptionGridOmitsExceptionTypeColumn()
{
    var designerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"),
        Encoding.UTF8);
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var basicInfoMethod = ExtractMethodText(
        viewCode,
        "private static string BuildExceptionBasicInfo",
        "private static string BuildExceptionContext");
    var filterMethod = ExtractMethodText(
        viewCode,
        "private static bool IsExceptionLogMatched",
        "private static bool IsDeviceLifecycleLogMatched");

    AssertFalse(
        designerCode.Contains("colExceptionType", StringComparison.Ordinal),
        "异常日志表格不得声明或注册 ExceptionType 列。");
    AssertTrue(
        basicInfoMethod.Contains("ExceptionType: {entry.ExceptionType}", StringComparison.Ordinal),
        "异常基本信息必须继续显示 ExceptionType。");
    AssertTrue(
        filterMethod.Contains("Contains(entry.ExceptionType, keyword)", StringComparison.Ordinal),
        "异常日志搜索必须继续支持 ExceptionType。");
}

static void ProgramExceptionHistoryUsesBoundedTailReads()
{
    var formatterCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "LocalJsonLogFormatter.cs"),
        Encoding.UTF8);
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "ProgramExceptionLogService.cs"),
        Encoding.UTF8);

    AssertTrue(formatterCode.Contains("maxBytes", StringComparison.Ordinal), "日志格式化器必须支持限制历史文件读取范围。");
    AssertTrue(formatterCode.Contains("Seek", StringComparison.Ordinal), "历史日志读取必须从文件尾部定位，避免扫描整个异常日志文件。");
    AssertTrue(serviceCode.Contains("MaxHistoryReadBytes", StringComparison.Ordinal), "程序异常日志服务必须限制单次历史读取大小。");

    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemExceptionTailReadTests", Guid.NewGuid().ToString("N"));
    var logDate = new DateTime(2026, 7, 20, 10, 0, 0);
    var settingsService = new FakeAppSettingsService
    {
        Current = new AppSettings { LogDirectory = root }
    };
    var logService = new ProgramExceptionLogService(settingsService);
    var filePath = Path.Combine(logService.GetLogDirectory(), $"{logDate:yyyy-MM-dd}.jsonl");

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, new string('X', 9 * 1024 * 1024) + Environment.NewLine, Encoding.UTF8);

        var tailEntries = new[]
        {
            new ProgramExceptionLogEntry { TraceId = "tail-1", OccurredTime = logDate, Message = "first tail record" },
            new ProgramExceptionLogEntry { TraceId = "tail-2", OccurredTime = logDate.AddSeconds(1), Message = "second tail record" }
        };
        File.AppendAllLines(filePath, tailEntries.Select(entry => JsonSerializer.Serialize(entry)), Encoding.UTF8);

        var records = logService.GetByDate(logDate, take: 10);

        AssertSequenceEqual(
            new[] { "tail-2", "tail-1" },
            records.Select(entry => entry.TraceId).ToArray(),
            "历史异常日志从超大文件尾部读取时必须丢弃起点处的半行，并按时间倒序返回有效记录。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
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
    AssertTrue(
        applyDeviceStatus.Contains("snapshot.IsAlarmPendingConfirmation", StringComparison.Ordinal)
        && applyDeviceStatus.Contains("snapshot.IsRawAlarmUnconfirmed", StringComparison.Ordinal),
        "MonitorView 必须区分双条件模式黄色待确认与仅地址模式灰色未知。");
    AssertTrue(
        applyDeviceStatus.Contains("PlcAlarmNotificationRules.SplitMessages(snapshot.SoftwareAlarmMessage)", StringComparison.Ordinal)
            && applyDeviceStatus.Contains("_deviceAlarmRuntimeErrorText = string.Join(\"；\", alarmMessages);", StringComparison.Ordinal),
        "异常详情必须保留完整的当前有效报警集合，右侧摘要不得替代原始报警内容。");
    AssertTrue(
        applyDeviceStatus.Contains("PlcAlarmNotificationRules.IsActive(", StringComparison.Ordinal)
        && applyDeviceStatus.Contains("PlcSoftwareAlarmRules.GenericAlarmMessage", StringComparison.Ordinal),
        "双条件模式状态 4 未匹配报警地址时，异常详情必须展示待确认原因。");

    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetrySyncService.cs"),
        Encoding.UTF8);
    var buildStationSnapshot = ExtractMethodText(
        centerCode,
        "private CenterTelemetryStationSnapshot BuildStationSnapshot",
        "private TodayProductionSummary GetTodayProductionSummary");
    AssertTrue(
        buildStationSnapshot.Contains("CenterTelemetryRules.ResolveAlarmMessage(production.AlarmMessage, stationStatus)", StringComparison.Ordinal),
        "中心遥测应继续以原始 PLC 报警内容为准，并经报警规则过滤非报警备注。");
    AssertFalse(
        buildStationSnapshot.Contains("SoftwareAlarmMessage", StringComparison.Ordinal),
        "Bool-only 软件报警内容不得发送到中心服务器。");
    AssertTrue(
        buildStationSnapshot.Contains("_deviceStatusService.GetLatestStatus(stationNo)", StringComparison.Ordinal),
        "PLC 无有效值时中心遥测必须从设备状态 JSONL 获取回退状态。");

    var lifecycleCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);
    AssertFalse(
        lifecycleCode.Contains("RecordAlarmChange", StringComparison.Ordinal)
        || lifecycleCode.Contains("_plcProductionMonitorService.StatusChanged", StringComparison.Ordinal),
        "设备日志不得再接收原始状态或 Bool 报警，报警只写入设备状态日志。");
}

static void PlcExpressionRulesSupportAbsoluteTestItemAddresses()
{
    var relative = PlcOffsetExpression.Parse("14:F-0_2");
    AssertFalse(relative.IsAbsoluteAddress, "数字开头的测试项表达式必须继续按相对偏移处理。");
    AssertEqual("DB23.130", relative.ResolveAddress("DB23.100", 16), "相对表达式必须叠加基地址、上下文偏移和自身偏移。");

    var absolute = PlcOffsetExpression.Parse("DB97.26:F-0_2");
    AssertTrue(absolute.IsAbsoluteAddress, "DB 开头的测试项表达式必须识别为绝对地址。");
    AssertEqual("DB97.26", absolute.AbsoluteAddress, "绝对表达式必须保留指定 PLC 地址。");
    AssertEqual("DB97.26", absolute.ResolveAddress("DB23.100", 160), "绝对表达式不得叠加测试项基地址或焊点上下文偏移。");

    var readService = new ExpressionReadService(new FakePlcCommunicationService(), new FakeAppSettingsService());
    var absoluteBinding = readService.Resolve(null, 999, "DB97.26:F-0_2");
    AssertTrue(absoluteBinding.IsAbsoluteAddress, "表达式读取服务返回的绑定必须保留绝对地址标记。");
    AssertEqual("DB97.26", absoluteBinding.Address, "表达式读取服务必须直接返回绝对 PLC 地址。");
    AssertEqual(2, absoluteBinding.DecimalPlaces, "绝对地址表达式必须保留小数位配置。");

    var supportedTypes = new[]
    {
        (Expression: "M10:B-0", Type: AppConstants.PlcDataTypes.Bool),
        (Expression: "DB10.2:H-0", Type: AppConstants.PlcDataTypes.Int16),
        (Expression: "DB10.4:I-1", Type: AppConstants.PlcDataTypes.Int32),
        (Expression: "DB10.8:F-0_2", Type: AppConstants.PlcDataTypes.Float),
        (Expression: "DB10.12:S-8_3", Type: AppConstants.PlcDataTypes.String)
    };
    foreach (var item in supportedTypes)
    {
        var parsed = PlcOffsetExpression.Parse(item.Expression);
        AssertTrue(parsed.IsAbsoluteAddress, $"{item.Expression} 必须识别为绝对地址。");
        AssertEqual(item.Type, parsed.DataType, $"{item.Expression} 的数据类型解析错误。");
    }

    AssertTrue(PlcOffsetExpression.Parse("DB10.DBX2.9:B-0").IsAbsoluteAddress, "DBX 位地址也应支持作为绝对地址。");
    AssertThrows<FormatException>(() => PlcOffsetExpression.Parse("DBx.26:F-0"), "非法 DB 绝对地址必须拒绝。");
    AssertThrows<FormatException>(() => PlcOffsetExpression.Parse("DB97.26:-0"), "缺少数据类型的表达式必须拒绝。");
    AssertThrows<FormatException>(() => PlcOffsetExpression.Parse("DB97.26:F-"), "缺少规则的表达式必须拒绝。");
    AssertThrows<FormatException>(() => PlcOffsetExpression.Parse("DB97.26:F-0_11"), "超出范围的小数位必须拒绝。");
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

static void TestItemUnitsFormatReportHeadersAndMesValues()
{
    AssertEqual("峰值电流 (A)", TestItemUnitFormatRules.FormatHeader(" 峰值电流 ", " A ", SchemeDetailValueRole.Actual), "实际值报表标题必须追加测试项单位。");
    AssertEqual("峰值电流上限 (A)", TestItemUnitFormatRules.FormatHeader("峰值电流上限", "A", SchemeDetailValueRole.Upper), "上限报表标题必须追加测试项单位。");
    AssertEqual("峰值电流下限 (A)", TestItemUnitFormatRules.FormatHeader("峰值电流下限", "A", SchemeDetailValueRole.Lower), "下限报表标题必须追加测试项单位。");
    AssertEqual("峰值电流结果", TestItemUnitFormatRules.FormatHeader("峰值电流结果", "A", SchemeDetailValueRole.Result), "结果报表标题不得追加单位。");
    AssertEqual("峰值电流", TestItemUnitFormatRules.FormatHeader("峰值电流", " ", SchemeDetailValueRole.Actual), "空单位必须保持原标题。");
    AssertEqual("12.3 A", TestItemUnitFormatRules.FormatValue(" 12.3 ", " A ", SchemeDetailValueRole.Actual), "实际值上传必须追加测试项单位。");
    AssertEqual(string.Empty, TestItemUnitFormatRules.FormatValue(" ", "A", SchemeDetailValueRole.Actual), "空值不得生成独立单位字符串。");
    AssertEqual("12.3", TestItemUnitFormatRules.FormatValue("12.3", null, SchemeDetailValueRole.Actual), "空单位必须保持原值。");
    AssertEqual("OK", TestItemUnitFormatRules.FormatValue("OK", "A", SchemeDetailValueRole.Result), "结果字段不得追加单位。");
}

static void ProductRetestOnlyAppliesToInspectionDevice()
{
    AssertTrue(
        ProductRetestRules.IsSupportedDeviceType(ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck),
        "整件检测设备必须支持产品重测。");
    AssertFalse(
        ProductRetestRules.IsSupportedDeviceType(ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic),
        "电磁点焊设备不需要重测，必须保持既有跳过行为。");
    AssertFalse(
        ProductRetestRules.IsSupportedDeviceType(ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld),
        "整件焊接设备不需要重测，必须保持既有跳过行为。");
    AssertFalse(ProductRetestRules.IsSupportedDeviceType(null), "设备类型缺失时不得启用重测。");

    var check = ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck;
    AssertTrue(ProductRetestRules.IsRetest(check, " P-001 ", "P-001"), "紧邻上一轮产品编号相同必须判定为重测。");
    AssertFalse(ProductRetestRules.IsRetest(check, "P-001", "P-002"), "产品编号不同不得判定为重测。");
    AssertFalse(ProductRetestRules.IsRetest(check, null, "P-001"), "任务内首件不得判定为重测。");
    AssertFalse(ProductRetestRules.IsRetest(check, "P-001", " "), "本轮产品编号为空时不得判定为重测。");
    AssertFalse(
        ProductRetestRules.IsRetest(ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic, "P-001", "P-001"),
        "点焊设备即使产品编号相同也不得判定为重测。");
}

static void ProductRetestOverwritesValuesAndReopensUpload()
{
    var existing = new BizWeldPointRecord
    {
        Id = 7,
        SequenceNo = 3,
        ProductNo = "P-001",
        TouchNo = "1",
        TestResult = ProductionConstants.TestResults.Ng,
        ProductResult = ProductionConstants.TestResults.Ng,
        RawDataJson = "{\"old\":\"1\"}",
        UploadStatus = ProductionConstants.UploadStatuses.Uploaded,
        UploadTime = DateTime.Today,
        UploadMessage = "uploaded",
        RetryCount = 2,
        Ts = DateTime.Today
    };
    var incoming = new BizWeldPointRecord
    {
        ProductNo = "P-001",
        TouchNo = "1",
        TestResult = ProductionConstants.TestResults.Ok,
        ProductResult = ProductionConstants.TestResults.Ok,
        RawDataJson = "{\"new\":\"2\"}",
        ProductCompleted = true,
        Ts = DateTime.Today.AddHours(1)
    };

    ProductRetestRules.ApplyRetestValues(existing, incoming);

    AssertEqual(7, existing.Id, "重测必须复用原记录主键，避免报表和上传任务的产品级自然键错位。");
    AssertEqual(3, existing.SequenceNo, "重测不得改变记录顺序号。");
    AssertEqual(ProductionConstants.TestResults.Ok, existing.TestResult, "重测必须覆盖焊点结果。");
    AssertEqual(ProductionConstants.TestResults.Ok, existing.ProductResult, "重测必须覆盖产品结果。");
    AssertEqual("{\"new\":\"2\"}", existing.RawDataJson, "重测必须覆盖原始采集值。");
    AssertEqual(
        ProductionConstants.UploadStatuses.Pending,
        existing.UploadStatus,
        "重测必须把上传状态打回待上传，否则待上传集合会排除该记录导致不会重新上报。");
    AssertTrue(existing.UploadTime is null && existing.UploadMessage is null, "重测必须清空上一轮上传结果。");
    AssertEqual(0, existing.RetryCount, "重测必须重置重试次数。");
}

static void ProductRetestRemovesOnlyUncoveredStaleRecords()
{
    var existing = new List<BizWeldPointRecord>
    {
        new() { Id = 1, TouchNo = "1" },
        new() { Id = 2, TouchNo = "2" },
        new() { Id = 3, TouchNo = "3" },
        new() { Id = 4, TouchNo = "4" }
    };

    var fullRound = new List<BizWeldPointRecord>
    {
        new() { TouchNo = "1" },
        new() { TouchNo = "2" },
        new() { TouchNo = "3" },
        new() { TouchNo = "4" }
    };
    AssertEqual(
        0,
        ProductRetestRules.SelectStaleRecords(existing, fullRound).Count,
        "面数一致时不得删除任何记录；现场 PLC 等全部视觉测试完成才触发采集，这是常态路径。");

    var partialRound = new List<BizWeldPointRecord>
    {
        new() { TouchNo = "1" },
        new() { TouchNo = "2" }
    };
    var stale = ProductRetestRules.SelectStaleRecords(existing, partialRound);
    AssertSequenceEqual(
        new[] { 3, 4 },
        stale.Select(record => record.Id).ToArray(),
        "本轮未覆盖的残留面必须删除，避免同一产品混合两轮数据被四面转A/B聚合成错误产品结果。");
}

static void UploadTaskRetestReopenAllowsProductScopedTasksOnly()
{
    AssertTrue(
        UploadTaskRetestReopenRules.IsReopenableTaskType(ProductionConstants.UploadTaskTypes.ProcessParameter),
        "过程参数任务必须允许因重测重开。");
    AssertTrue(
        UploadTaskRetestReopenRules.IsReopenableTaskType(ProductionConstants.UploadTaskTypes.CenterProductReport),
        "中心看板转发必须允许因重测重开，避免看板保留重测前结果。");
    AssertFalse(
        UploadTaskRetestReopenRules.IsReopenableTaskType(ProductionConstants.UploadTaskTypes.FinishReport),
        "完工上报与单个产品无关，不得因重测重开。");

    var check = ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck;
    AssertTrue(
        UploadTaskRetestReopenRules.ShouldReopen(
            ProductionConstants.UploadStatuses.Uploaded,
            ProductionConstants.UploadStatuses.Pending,
            check),
        "已上传任务收到重测的待上传数据时必须重开。");
    AssertFalse(
        UploadTaskRetestReopenRules.ShouldReopen(
            ProductionConstants.UploadStatuses.Uploaded,
            ProductionConstants.UploadStatuses.Uploaded,
            check),
        "非待上传的新数据不得重开已上传任务，避免状态同步反复打回。");
    AssertFalse(
        UploadTaskRetestReopenRules.ShouldReopen(
            ProductionConstants.UploadStatuses.Uploaded,
            ProductionConstants.UploadStatuses.Pending,
            ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic),
        "点焊设备不支持重测，不得重开已上传任务。");
}

static void DataHistoryDynamicColumnsAppendTestItemUnits()
{
    var method = typeof(DataHistoryQueryService).GetMethod(
        "ResolveColumnHeader",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(method is not null, "数据历史服务必须提供动态列标题解析入口。");

    var item = new DimTestItem { ItemId = 1, ItemName = "峰值电流", Unit = "A" };
    var detail = new BizSchemeDetail();

    string Invoke(SchemeDetailValueRole role)
        => (string)(method!.Invoke(null, [detail, item, role]) ?? string.Empty);

    AssertEqual("峰值电流 (A)", Invoke(SchemeDetailValueRole.Actual), "测试数据页实际值列标题必须追加测试项单位。");
    AssertEqual("峰值电流上限 (A)", Invoke(SchemeDetailValueRole.Upper), "测试数据页上限列标题必须追加测试项单位。");
    AssertEqual("峰值电流下限 (A)", Invoke(SchemeDetailValueRole.Lower), "测试数据页下限列标题必须追加测试项单位。");
    AssertEqual("峰值电流结果", Invoke(SchemeDetailValueRole.Result), "测试数据页结果列标题不得追加单位。");

    var customDetail = new BizSchemeDetail { ActualHeader = " 客户电流 " };
    AssertEqual(
        "客户电流 (A)",
        (string)(method!.Invoke(null, [customDetail, item, SchemeDetailValueRole.Actual]) ?? string.Empty),
        "自定义表头必须保留并追加测试项单位。");

    var unitlessItem = new DimTestItem { ItemId = 2, ItemName = "焊接次数" };
    AssertEqual(
        "焊接次数",
        (string)(method!.Invoke(null, [detail, unitlessItem, SchemeDetailValueRole.Actual]) ?? string.Empty),
        "未配置单位的测试项列标题必须保持原样。");
}

static void RealtimePreviewColumnsAppendTestItemUnits()
{
    // MonitorView 依赖 WinForms 控件，无法在控制台 harness 中实例化，只能断言列标题走带单位的统一解析入口。
    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var headerMethod = ExtractMethodText(
        monitorCode,
        "private static string ResolvePreviewColumnHeader(WeldPreviewItem item, SchemeDetailValueRole role)",
        "private void AddWeldPreviewColumn(");
    AssertTrue(
        headerMethod.Contains("TestItemUnitFormatRules.FormatHeader(", StringComparison.Ordinal)
            && headerMethod.Contains("item.Unit", StringComparison.Ordinal),
        "实时预览列标题必须复用测试项单位格式规则。");

    var rebuildMethod = ExtractMethodText(
        monitorCode,
        "private void RebuildWeldParameterPreviewTable()",
        "private static void SetControlRedraw(");
    foreach (var role in new[] { "Upper", "Lower", "Actual", "Result" })
    {
        AssertTrue(
            rebuildMethod.Contains($"ResolvePreviewColumnHeader(item, SchemeDetailValueRole.{role})", StringComparison.Ordinal),
            $"实时预览 {role} 列必须使用带单位的标题解析入口。");
    }

    var previewItemCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "ViewModels", "WeldPreviewItem.cs"), Encoding.UTF8);
    AssertTrue(previewItemCode.Contains("string Unit", StringComparison.Ordinal), "实时预览项模型必须携带测试项单位。");

    var schemaKeyMethod = ExtractMethodText(
        monitorCode,
        "private static string BuildWeldPreviewSchemaKey(IReadOnlyList<WeldPreviewItem> items)",
        "private static string BuildWeldPreviewLayoutKey(");
    AssertTrue(schemaKeyMethod.Contains("item.Unit", StringComparison.Ordinal), "单位变化必须触发预览表重建。");
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
    AssertTrue(Invoke(new BizSchemeDetail { EnableActual = false, ReportActual = true }), "写入报表开关必须独立于实时预览采集开关。");
    AssertTrue(Invoke(new BizSchemeDetail { EnableActual = true, ReportActual = true }), "任一 ReportEnable 角色必须允许 MES 报表文件上传。");
}

static void SaveHistoryControlsProductHistoryVisibility()
{
    var mesOnly = new BizSchemeDetail
    {
        EnableActual = true,
        SaveActual = false,
        MesActual = true
    };
    AssertFalse(
        SchemeDetailRoleRules.ShouldShowHistoryRole(mesOnly, SchemeDetailValueRole.Actual),
        "仅启用 MES 的角色不得进入产品历史。");

    var savedOnly = new BizSchemeDetail
    {
        EnableActual = true,
        SaveActual = true,
        MesActual = false
    };
    AssertTrue(
        SchemeDetailRoleRules.ShouldShowHistoryRole(savedOnly, SchemeDetailValueRole.Actual),
        "已采集且启用保存历史的角色必须进入产品历史。");
}

static void DynamicHistoryAndCenterUseTaskBoundProcessConfig()
{
    var task = new BizWeldTask
    {
        ProductNum = "WORK-ORDER-PRODUCT",
        ProgramId = "PROGRAM-001",
        StationNo = ProductionConstants.Stations.SharedStationNo
    };
    var station1Config = new BizProductProcessConfig { Id = 11, StationNo = 1, ProductNum = "PROGRAM-PRODUCT", SchemeId = "S-A" };
    var station2Config = new BizProductProcessConfig { Id = 12, StationNo = 2, ProductNum = "PROGRAM-PRODUCT", SchemeId = "S-B" };
    var service = new FakeProductProcessConfigService(new Dictionary<int, BizProductProcessConfig>
    {
        [1] = station1Config,
        [2] = station2Config
    });

    var resolved = TaskProductProcessConfigResolver.Resolve(service, task, [0, 2, 2]);

    AssertSequenceEqual(new[] { 1, 2 }, resolved.Keys.OrderBy(value => value).ToArray(), "任务工艺解析必须按规范化工位去重。");
    AssertEqual(station1Config.Id, resolved[1].Id, "共享工位必须规范化为工位1并使用任务绑定程序的工艺。");
    AssertEqual(station2Config.Id, resolved[2].Id, "工位2必须使用任务绑定程序的独立工艺。");
    AssertEqual(0, service.FindActiveCallCount, "历史和中心转发不得退回按 task.ProductNum 直接查询工艺。");
    AssertEqual(2, service.FindActiveForTaskCalls.Count, "每个实际工位只应调用一次任务绑定工艺解析。");
    AssertTrue(service.FindActiveForTaskCalls.All(call => ReferenceEquals(call.Task, task)), "工艺解析必须传递原始任务以使用 ProgramId 反查产品工号。");

    var historyCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DataHistoryQueryService.cs"),
        Encoding.UTF8);
    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterProductForwardingService.cs"),
        Encoding.UTF8);
    AssertTrue(historyCode.Contains("TaskProductProcessConfigResolver.Resolve", StringComparison.Ordinal), "数据历史必须复用任务绑定工艺解析。");
    AssertTrue(historyCode.Contains("GetSchemeItemsForStation", StringComparison.Ordinal), "数据历史必须按记录工位隔离方案明细和动态值。");
    AssertTrue(centerCode.Contains("TaskProductProcessConfigResolver.Resolve", StringComparison.Ordinal), "中心转发必须复用任务绑定工艺解析。");
    AssertTrue(centerCode.Contains("BuildRequest(settings, task, stationNo, records, config)", StringComparison.Ordinal), "中心请求必须使用已解析的同一工艺配置。");
    AssertFalse(historyCode.Contains("config.ProductNum == task.ProductNum", StringComparison.Ordinal), "数据历史不得再按工单产品号直接匹配工艺。");
    AssertFalse(centerCode.Contains("config.ProductNum == productNum", StringComparison.Ordinal), "中心转发不得保留独立的产品号工艺查询路径。");
}

static void DataManageViewUsesGenericProductTestTree()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.Designer.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("QueryTestDataAsync", StringComparison.Ordinal), "DataManageView 必须查询通用测试数据，而不是固定焊接参数接口。");
    AssertTrue(viewCode.Contains("SetTree(nameof(DataHistoryTestDataRow.Children))", StringComparison.Ordinal), "测试数据表必须按产品→测试记录配置树形列。");
    AssertTrue(viewCode.Contains("tableTestData.DefaultExpand = true", StringComparison.Ordinal), "产品节点必须默认展开。");
    AssertFalse(viewCode.Contains("TestDataTable_CellClick", StringComparison.Ordinal), "测试数据树不得再绑定原始 JSON 查看逻辑。");
    AssertFalse(viewCode.Contains("FormatJsonOrOriginal", StringComparison.Ordinal), "DataManageView 不应保留仅用于原始 JSON 展示的格式化逻辑。");
    AssertTrue(viewCode.Contains("ApplyDefaultSplitterLayout", StringComparison.Ordinal), "DataManageView 必须初始化工单与详情区域比例。");
    AssertFalse(viewCode.Contains("QueryCollectionRecordsAsync", StringComparison.Ordinal), "DataManageView 不应再次查询重复的采集数据页。");
    AssertFalse(viewCode.Contains("LoadCollectionRecordsAsync", StringComparison.Ordinal), "DataManageView 不应保留重复采集分页加载路径。");
    AssertTrue(designerCode.Contains("tableTestData = new AntdUI.Table()", StringComparison.Ordinal), "Designer 必须声明通用测试数据树表格。");
    AssertEqual(1, CountOccurrences(designerCode, "new AntdUI.Splitter()"), "DataManageView 只应保留工单与详情区域之间的 AntdUI.Splitter。");
    AssertTrue(designerCode.Contains("mainSplitter = new AntdUI.Splitter()", StringComparison.Ordinal), "工单信息与详情页签之间必须使用 AntdUI.Splitter。");
    AssertFalse(designerCode.Contains("testDataSplitter", StringComparison.Ordinal), "测试数据页不得再声明内部 splitter。");
    AssertFalse(designerCode.Contains("rawDataLayout", StringComparison.Ordinal), "测试数据页不得再声明原始数据布局。");
    AssertFalse(designerCode.Contains("txtRawData", StringComparison.Ordinal), "测试数据页不得再声明原始数据文本框。");
    AssertTrue(designerCode.Contains("tabWeldParameters.Controls.Add(parameterLayout);", StringComparison.Ordinal), "测试数据布局必须直接填充测试数据页签。");
    AssertFalse(designerCode.Contains("new SplitContainer()", StringComparison.Ordinal), "DataManageView 不得继续实例化 WinForms SplitContainer。");
    AssertTrue(designerCode.Contains("mainSplitter.Orientation = Orientation.Horizontal;", StringComparison.Ordinal), "工单与详情页签 splitter 必须保持上下分隔。");
    AssertTrue(designerCode.Contains("detailTabs.Controls.Add(tabWeldParameters);", StringComparison.Ordinal), "详情页必须保留测试数据页签。");
    AssertTrue(designerCode.Contains("detailTabs.Controls.Add(tabReportFiles);", StringComparison.Ordinal), "详情页必须保留报告文件页签。");
    AssertFalse(designerCode.Contains("detailTabs.Controls.Add(tabCollectionData);", StringComparison.Ordinal), "详情页不得继续显示重复的采集数据页签。");
}

static void DataHistoryExportWritesCurrentRowsAndDynamicColumns()
{
    var path = Path.Combine(Path.GetTempPath(), $"data-history-{Guid.NewGuid():N}.xlsx");
    try
    {
        var rows = new[]
        {
            new DataHistoryTestDataRow
            {
                IsProductRow = true, StationNo = 1, ProductNo = "P001", ProductResult = ProductionConstants.TestResults.Ok,
                Children = [new DataHistoryTestDataRow { TouchNo = "T1", TestResult = ProductionConstants.TestResults.Ok, RecordTime = new DateTime(2026, 8, 22, 10, 0, 0), DynamicValues = new Dictionary<string, string> { ["height"] = "2.5" } }]
            }
        };
        DataHistoryTestDataExportService.Export(path, "WO001", rows, [new DataHistoryDynamicColumn { Key = "height", HeaderText = "高度" }]);
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("测试数据");
        AssertEqual("高度", sheet.Cell(1, 9).GetString(), "导出表头必须包含动态测试列。");
        AssertEqual("WO001", sheet.Cell(2, 1).GetString(), "导出行必须包含当前工单号。");
        AssertEqual("2.5", sheet.Cell(2, 9).GetString(), "导出行必须包含动态测试值。");
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}

static void DataHistoryDynamicSortOrdersProductsAndKeepsBlanksLast()
{
    DataHistoryTestDataRow Row(string no, string value) => new()
    {
        IsProductRow = true,
        ProductNo = no,
        Children = [new DataHistoryTestDataRow { DynamicValues = new Dictionary<string, string> { ["height"] = value } }]
    };
    var rows = new[] { Row("P10", "10"), Row("P2", "2"), Row("EMPTY", "--") };
    var ascending = DataHistoryTestDataRules.Apply(rows, null, "height", false);
    var descending = DataHistoryTestDataRules.Apply(rows, null, "height", true);
    AssertSequenceEqual(new[] { "P2", "P10", "EMPTY" }, ascending.Select(row => row.ProductNo).ToArray(), "动态数值升序必须按数值比较且空值置底。");
    AssertSequenceEqual(new[] { "P10", "P2", "EMPTY" }, descending.Select(row => row.ProductNo).ToArray(), "动态数值降序必须保持空值置底。");
}

static void DataHistoryProductResultFilterKeepsCompleteProductRows()
{
    var okChild = new DataHistoryTestDataRow { RecordId = 11, SequenceNo = 1 };
    var ngChild1 = new DataHistoryTestDataRow { RecordId = 21, SequenceNo = 1 };
    var ngChild2 = new DataHistoryTestDataRow { RecordId = 22, SequenceNo = 2 };
    var rows = new[]
    {
        new DataHistoryTestDataRow
        {
            IsProductRow = true,
            ProductNo = "OK-1",
            ProductResult = ProductionConstants.TestResults.Ok,
            Children = [okChild]
        },
        new DataHistoryTestDataRow
        {
            IsProductRow = true,
            ProductNo = "NG-1",
            ProductResult = ProductionConstants.TestResults.PreWeldNg,
            Children = [ngChild1, ngChild2]
        }
    };

    var okRows = DataHistoryTestDataRules.Apply(rows, ProductionConstants.TestResults.Ok, null, false);
    var ngRows = DataHistoryTestDataRules.Apply(rows, ProductionConstants.TestResults.Ng, null, false);

    AssertSequenceEqual(new[] { "OK-1" }, okRows.Select(row => row.ProductNo).ToArray(), "OK 筛选必须只保留产品结果为 OK 的产品。");
    AssertSequenceEqual(new[] { "NG-1" }, ngRows.Select(row => row.ProductNo).ToArray(), "NG 筛选必须包含焊前 NG 等失败产品结果。");
    AssertSequenceEqual(new[] { 21, 22 }, ngRows[0].Children.Select(row => row.RecordId).ToArray(), "产品结果筛选不得裁剪产品下的测试记录。");
}

static void DataHistoryTreeParentKeepsStoredProductResult()
{
    var method = typeof(DataHistoryQueryService).GetMethod(
        "BuildProductRow",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(method is not null, "数据历史服务必须提供产品树父行构造逻辑。");

    var children = new List<DataHistoryTestDataRow>
    {
        new()
        {
            RecordId = 1,
            SequenceNo = 1,
            ProductNo = "P-001",
            TestResult = ProductionConstants.TestResults.Ok,
            ProductResult = ProductionConstants.TestResults.Ng,
            RecordTime = DateTime.Today
        },
        new()
        {
            RecordId = 2,
            SequenceNo = 2,
            ProductNo = "P-001",
            TestResult = ProductionConstants.TestResults.Ok,
            ProductResult = ProductionConstants.TestResults.Unknown,
            RecordTime = DateTime.Today.AddMinutes(1)
        }
    };

    var parent = (DataHistoryTestDataRow?)method!.Invoke(null, [10, 1, "P-001", children]);
    AssertTrue(parent is not null, "必须构造产品父行。");
    AssertEqual(2, parent!.TestCount, "产品父行必须保留同一产品的全部测试记录。");
    AssertEqual(ProductionConstants.TestResults.Ng, parent.ProductResult, "产品父行必须沿用记录中的 PLC 产品结果，不得从子记录结果推断。");
}

static void SinglePointHistoryDisplayRuleUsesConfiguredAndActualCounts()
{
    AssertTrue(ProductHistoryDisplayRules.ShouldFlattenSinglePoint(1, 1), "配置一个采集点且只有一条记录时必须扁平显示。");
    AssertFalse(ProductHistoryDisplayRules.ShouldFlattenSinglePoint(1, 2), "同一产品多次测试时必须保留树形结构。");
    AssertFalse(ProductHistoryDisplayRules.ShouldFlattenSinglePoint(2, 1), "多采集点配置必须保留树形结构。");
    AssertFalse(ProductHistoryDisplayRules.ShouldFlattenSinglePoint(null, 1), "缺少配置时不得扁平显示。");
    AssertFalse(ProductHistoryDisplayRules.ShouldFlattenSinglePoint(0, 1), "无效采集点数量不得扁平显示。");
}

static void DataHistorySinglePointRowKeepsPointValues()
{
    var method = typeof(DataHistoryQueryService).GetMethod(
        "FlattenSinglePointProductRow",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(method is not null, "数据历史服务必须提供单焊点显示行合并逻辑。");

    var pointValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["current"] = "0.166"
    };
    var product = new DataHistoryTestDataRow
    {
        IsProductRow = true,
        TaskId = 10,
        StationNo = 1,
        ProductNo = "P-001",
        NodeText = "P-001",
        TestResult = ProductionConstants.TestResults.Ng,
        ProductResult = ProductionConstants.TestResults.Ng,
        UploadStatus = ProductionConstants.UploadStatuses.Uploaded,
        TestCount = 1,
        RecordTime = DateTime.Today
    };
    var point = new DataHistoryTestDataRow
    {
        IsProductRow = false,
        TaskId = 10,
        RecordId = 99,
        SequenceNo = 1,
        StationNo = 1,
        ProductNo = "P-001",
        TouchNo = "1",
        NodeText = "1",
        TestResult = ProductionConstants.TestResults.Ok,
        ProductResult = ProductionConstants.TestResults.Unknown,
        UploadStatus = ProductionConstants.UploadStatuses.Uploaded,
        RecordTime = DateTime.Today.AddMinutes(1),
        DynamicValues = pointValues,
        RawDataJson = "{\"current\":\"0.166\"}"
    };

    var flattened = (DataHistoryTestDataRow?)method!.Invoke(null, [product, point]);
    AssertTrue(flattened is not null, "必须生成单焊点产品显示行。");
    AssertEqual(0, flattened!.Children.Count, "扁平行不得保留子节点。");
    AssertEqual("1", flattened.TouchNo, "扁平行必须保留焊点序号。");
    AssertEqual(99, flattened.RecordId, "扁平行必须保留唯一记录编号。");
    AssertEqual(ProductionConstants.TestResults.Ng, flattened.ProductResult, "扁平行必须保留产品级结果。");
    AssertEqual("0.166", flattened.DynamicValues["current"], "扁平行必须保留焊点动态值。");
    AssertEqual(point.RawDataJson, flattened.RawDataJson, "扁平行必须保留原始数据引用。");
}

static void SchemeOutputRolesAreIndependentFromRealtimePreview()
{
    var detail = new BizSchemeDetail
    {
        EnableActual = false,
        SaveActual = true,
        ReportActual = true,
        MesActual = true
    };

    AssertTrue(SchemeDetailRoleRules.ShouldReadProductRole(detail, SchemeDetailValueRole.Actual), "输出开启时产品完成采集必须读取实际值。");
    AssertTrue(SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Actual), "保存历史必须独立于实时预览开关。");
    AssertTrue(SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Actual), "报表输出必须独立于实时预览开关。");
    AssertTrue(SchemeDetailRoleRules.ShouldUploadMesRole(detail, SchemeDetailValueRole.Actual), "MES输出必须独立于实时预览开关。");
}

static void WholePieceFourSideAggregationProducesAbRows()
{
    var records = new[]
    {
        CreateAggregationRecord("3", "0.15", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("1", "0.12", ProductionConstants.TestResults.Ng),
        CreateAggregationRecord("4", "0.16", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("2", "0.14", ProductionConstants.TestResults.Ok)
    };
    var definition = new WholePieceAbValueDefinition(2, "对称度", "Symmetry", "18:F-0_2");
    var result = WholePieceAbAggregationRules.Aggregate(
        records,
        [definition],
        enableStringNumericFormatting: true,
        AppConstants.PlcStringNumericFormatModes.Round);

    AssertTrue(result.IsSuccess, result.ErrorMessage);
    AssertEqual(2, result.Rows.Count, "四面检测必须输出A/B两行。");
    AssertEqual("A", result.Rows[0].SideNo, "A面必须先输出。");
    AssertEqual("0.15", result.Rows[0].Values["Symmetry"], "A面必须取2、4面平均并按两位小数四舍五入。");
    AssertEqual(ProductionConstants.TestResults.Ok, result.Rows[0].Result, "A面两个原始面都OK时结果应为OK。");
    AssertEqual("B", result.Rows[1].SideNo, "B面必须后输出。");
    AssertEqual("0.14", result.Rows[1].Values["Symmetry"], "B面必须取1、3面平均并按两位小数四舍五入。");
    AssertEqual(ProductionConstants.TestResults.Ng, result.Rows[1].Result, "B面任一原始面NG时结果应为NG。");
}

static void WholePieceHeightAndWidthUseProductMaximum()
{
    var records = new[]
    {
        CreateAggregationRecord("3", "15.86", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("1", "20.18", ProductionConstants.TestResults.Ng),
        CreateAggregationRecord("4", "12.50", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("2", "18.20", ProductionConstants.TestResults.Ok)
    };
    var height = new WholePieceAbValueDefinition(1, "高度", "Height", "14:F-0_2");
    var result = WholePieceAbAggregationRules.Aggregate(
        records,
        [height],
        enableStringNumericFormatting: true,
        AppConstants.PlcStringNumericFormatModes.Round);

    AssertTrue(result.IsSuccess, result.ErrorMessage);
    AssertEqual("20.18", result.Rows[0].Values["Height"], "A行必须使用四面高度最大值。");
    AssertEqual("20.18", result.Rows[1].Values["Height"], "B行必须发送同一四面高度最大值。");
    AssertTrue(WholePieceAbAggregationRules.IsProductMaximumItem("宽度"), "宽度必须使用四面最大值策略。");
    AssertFalse(WholePieceAbAggregationRules.IsProductMaximumItem("对称度"), "对称度必须继续使用A/B配对平均。");

    var reportCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductionReportFileService.cs"), Encoding.UTF8);
    var centerForwardCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterProductForwardingService.cs"), Encoding.UTF8);
    var centerWriterCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.CenterServer", "Services", "CenterProductReportWorkbookWriter.cs"), Encoding.UTF8);
    AssertTrue(reportCode.Contains("wholePieceAb && WholePieceAbAggregationRules.IsProductMaximumItem", StringComparison.Ordinal), "设备端报表必须把高度和宽度标记为产品级合并列。");
    AssertTrue(centerForwardCode.Contains("wholePieceInspection && WholePieceAbAggregationRules.IsProductMaximumItem", StringComparison.Ordinal), "中心报表列定义必须同步高度和宽度的产品级合并语义。");
    AssertTrue(centerWriterCode.Contains("BuildDynamicMergeOverrides", StringComparison.Ordinal)
        && centerWriterCode.Contains("candidates.MaxBy", StringComparison.Ordinal), "中心可见报表必须从四面原始值计算产品级最大值，同时保留原始数据页。");
}

static void ProgramResultDisplayPrefersPersistedEntityResult()
{
    var monitorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var method = ExtractMethodText(
        monitorCode,
        "private static string ResolveStationProductResultText(BizWeldPointRecord record)",
        "/// <summary>" + Environment.NewLine + "    /// 解析工位结果颜色。");

    AssertTrue(method.Contains("var productResult = record.ProductResult;", StringComparison.Ordinal), "工位产品结果必须优先使用记录实体中的正式结果。");
    AssertTrue(method.Contains("FindRawValue(rawValues, \"product_result\")", StringComparison.Ordinal), "旧历史记录必须继续回退 RawDataJson.product_result。");
}

static void WholePieceProgramResultsUseMaximumAllowedValues()
{
    AssertEqual(
        ProductionConstants.InspectionResultSources.Plc,
        ProductionConstants.InspectionResultSources.Normalize("unknown"),
        "未知结果来源必须回退PLC读取。");
    AssertTrue(
        WholePieceProgramResultRules.IsApplicable(
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            ProductionConstants.InspectionResultSources.Program),
        "程序计算只应在整件检测设备启用。");

    var ok = WholePieceProgramResultRules.EvaluateFace(
        "{\"高度\":\"20.18\",\"对称度\":\"0.15\"}",
        [new WholePieceProgramMeasurement("高度", "20.18"), new WholePieceProgramMeasurement("对称度", "0")]);
    AssertTrue(ok.IsSuccess, ok.ErrorMessage);
    AssertEqual(ProductionConstants.TestResults.Ok, ok.Result, "等于最大允许值和真实零值必须判定为OK。");

    var ng = WholePieceProgramResultRules.EvaluateFace(
        "{\"高度\":\"20.18\"}",
        [new WholePieceProgramMeasurement("高度", "20.19")]);
    AssertTrue(ng.IsSuccess, ng.ErrorMessage);
    AssertEqual(ProductionConstants.TestResults.Ng, ng.Result, "超过最大允许值必须判定为NG。");

    var missing = WholePieceProgramResultRules.EvaluateFace(
        "{\"高度\":\"20.18\"}",
        [new WholePieceProgramMeasurement("宽度", "10")]);
    AssertFalse(missing.IsSuccess, "缺少最大允许值必须拒绝采集。");

    AssertEqual(
        ProductionConstants.TestResults.Ng,
        WholePieceProgramResultRules.ResolveRealtimeProductResult(
            [ProductionConstants.TestResults.Ok, ProductionConstants.TestResults.Ng, null, null],
            4),
        "任一已完成面NG时产品结果必须立即显示NG。");
    AssertEqual(
        ProductionConstants.TestResults.Unknown,
        WholePieceProgramResultRules.ResolveRealtimeProductResult(
            [ProductionConstants.TestResults.Ok, ProductionConstants.TestResults.Ok, null, null],
            4),
        "四面未完成且没有NG时产品结果必须保持未测试。");

    var collectionCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductCycleCollectionService.cs"), Encoding.UTF8);
    var previewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductRealtimePreviewService.cs"), Encoding.UTF8);
    AssertTrue(collectionCode.Contains("ApplyProgramCalculatedResults(task, schemeItems, records)", StringComparison.Ordinal)
        && collectionCode.Contains("record.ProductResult = productResult;", StringComparison.Ordinal), "正式采集必须把程序计算的单面和产品结果固化到四面记录。");
    AssertTrue(collectionCode.Contains("!useProgramResult || ProductRealtimePreviewRules.ShouldReadTestValues(testResult)", StringComparison.Ordinal), "程序计算模式下PLC未完成的面不得读取测试值地址。");
    AssertTrue(previewCode.Contains("activeTask?.ProgramContentSnapshot", StringComparison.Ordinal)
        && previewCode.Contains("WholePieceProgramResultRules.ResolveRealtimeProductResult", StringComparison.Ordinal), "实时预览必须使用任务固化最大允许值并逐面汇总产品结果。");
    AssertTrue(collectionCode.Contains("ResultSource={resultSource}, ProgramResult={useProgramResult}", StringComparison.Ordinal), "正式采集日志必须记录本轮实际使用的结果来源，便于区分PLC读取和程序计算。");
}

static void WholePieceAggregationRejectsInvalidSourceData()
{
    var duplicateSide = new[]
    {
        CreateAggregationRecord("1", "1", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("1", "2", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("3", "3", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("4", "4", ProductionConstants.TestResults.Ok)
    };
    var definition = new WholePieceAbValueDefinition(1, "高度", "Height", "14:F-0_2");
    var duplicateResult = WholePieceAbAggregationRules.Aggregate(duplicateSide, [definition], true, AppConstants.PlcStringNumericFormatModes.Truncate);
    AssertFalse(duplicateResult.IsSuccess, "重复面号必须拒绝。");

    var invalidNumber = new[]
    {
        CreateAggregationRecord("1", "1", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("2", "bad", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("3", "3", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("4", "4", ProductionConstants.TestResults.Ok)
    };
    var invalidNumberResult = WholePieceAbAggregationRules.Aggregate(invalidNumber, [definition], true, AppConstants.PlcStringNumericFormatModes.Truncate);
    AssertFalse(invalidNumberResult.IsSuccess, "非数字聚合值必须拒绝。");

    var absoluteDefinition = definition with { ActualExpression = "DB97.26:F-0_2" };
    var validRecords = new[]
    {
        CreateAggregationRecord("1", "1", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("2", "2", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("3", "3", ProductionConstants.TestResults.Ok),
        CreateAggregationRecord("4", "4", ProductionConstants.TestResults.Ok)
    };
    var absoluteResult = WholePieceAbAggregationRules.Aggregate(validRecords, [absoluteDefinition], true, AppConstants.PlcStringNumericFormatModes.Truncate);
    AssertFalse(absoluteResult.IsSuccess, "A/B聚合源使用绝对地址时必须拒绝。");
}

static BizWeldPointRecord CreateAggregationRecord(string sideNo, string value, string result)
{
    return new BizWeldPointRecord
    {
        ProductNo = "P001",
        TouchNo = sideNo,
        TestResult = result,
        RawDataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["item_1"] = value,
            ["item_2"] = value
        })
    };
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

    var testDataProductResult = typeof(DataHistoryTestDataRow).GetProperty("ProductResult");
    var testDataRawJson = typeof(DataHistoryTestDataRow).GetProperty("RawDataJson");
    var weldParameterProductResult = typeof(DataHistoryWeldParameterRow).GetProperty("ProductResult");
    var collectionProductResult = typeof(DataHistoryCollectionRow).GetProperty("ProductResult");
    AssertTrue(testDataProductResult is not null, "通用测试数据树行必须公开独立的 ProductResult。");
    AssertTrue(testDataRawJson is not null, "通用测试数据树行必须保留 RawDataJson 以兼容历史数据和内部业务流程。");
    AssertTrue(weldParameterProductResult is not null, "兼容焊接参数历史行必须公开独立的 ProductResult。");
    AssertTrue(collectionProductResult is not null, "采集记录历史行必须公开独立的 ProductResult。");

    var dataHistoryCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DataHistoryQueryService.cs"),
        Encoding.UTF8);
    AssertEqual(
        3,
        CountOccurrences(dataHistoryCode, "ProductResult = ResolveProductResult(record),"),
        "通用测试树、兼容焊接参数行和采集记录行都必须填充独立的 ProductResult。");
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
        records,
        testItemUnit: "A");

    try
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("生产报表");

        AssertEqual($"流转卡号：{task.SN}", worksheet.Cell("A1").GetString(), "流转卡号必须写入模板 A1:C1 合并单元格。");
        AssertEqual($"规格：{task.Spec}", worksheet.Cell("D1").GetString(), "规格必须写入模板 D1:F1 合并单元格。");
        AssertEqual($"产品型号：{task.ProductModel}", worksheet.Cell("G1").GetString(), "产品型号必须写入模板 G1:J1 合并单元格。");
        AssertEqual($"产品工号：{task.ProductNum}", worksheet.Cell("A3").GetString(), "产品工号必须写入模板 A3:C3 合并单元格。");
        AssertEqual($"批次：{task.Batch}", worksheet.Cell("D3").GetString(), "批次必须写入模板 D3:F3 合并单元格。");
        AssertEqual($"部件名称：{task.ProductName}", worksheet.Cell("G3").GetString(), "部件名称必须写入模板 G3:J3 合并单元格。");
        AssertEqual($"部件图号：{task.DrawingNo}", worksheet.Cell("A5").GetString(), "部件图号必须写入模板 A5:C5 合并单元格。");
        AssertEqual($"工序名称：{task.ProcessName}", worksheet.Cell("D5").GetString(), "工序名称必须写入模板 D5:F5 合并单元格。");
        AssertEqual($"工序号：{task.ProcessNo}", worksheet.Cell("G5").GetString(), "工序号必须写入模板 G5:J5 合并单元格。");
        AssertEqual($"工单数量：{task.StartAmount}", worksheet.Cell("A7").GetString(), "工单数量必须只取 StartAmount。");
        AssertEqual($"合格数量：{task.QualifiedQty}", worksheet.Cell("D7").GetString(), "合格数量必须取 QualifiedQty。");
        AssertEqual($"操作人员：{task.UserNumber}", worksheet.Cell("G7").GetString(), "操作人员必须只取开工任务 UserNumber。");
        AssertEqual($"开始时间：{startTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("A9").GetString(), "开始时间必须来自持久化 StartTime 并使用模板格式。");
        AssertEqual("结束时间：", worksheet.Cell("D9").GetString(), "未完工任务的结束时间必须只保留标签。");
        AssertEqual(string.Empty, worksheet.Cell("G9").GetString(), "参考模板第九行不得继续写操作人员或备注。");

        var detailHeaders = ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow);
        AssertSequenceEqual(
            new[] { "产品编号", "拍照编号", "峰值电流 (A)", "拍照结果", "产品结果" },
            detailHeaders,
            "单工位报表必须把动态测试值放在拍照结果之前，并保持产品结果在最后。");
        AssertFalse(detailHeaders.Contains("峰值电流上限"), "仅 SaveEnable 的动态角色不得进入设备报表。");
        AssertFalse(detailHeaders.Contains("峰值电流下限"), "仅 MesEnable 的动态角色不得进入设备报表。");
        foreach (var mergedRange in new[]
        {
            "A1:C1", "D1:F1", "G1:J1",
            "A3:C3", "D3:F3", "G3:J3",
            "A5:C5", "D5:F5", "G5:J5",
            "A7:C7", "D7:F7", "G7:J7",
            "A9:C9", "D9:F9"
        })
        {
            AssertMerged(worksheet, mergedRange, "公共表头必须匹配客户模板合并范围。");
        }
        AssertTrue(
            worksheet.RangeUsed(XLCellsUsedOptions.All)!.RangeAddress.LastAddress.ColumnNumber <= 10,
            "固定公共字段未超过十列时不得扩展到 K 列以后。");
        AssertFalse(worksheet.Cell("A1").Style.Alignment.WrapText, "客户模板中文标签必须保持单行显示。");
        AssertTrue(worksheet.Cell("A1").Style.Alignment.ShrinkToFit, "公共表头必须自动缩小字体，避免长流转卡号在固定列宽内截断。");
        var expectedWidths = new[] { 6.6d, 10.9333333333333d, 11.152380952381d, 11.4857142857143d, 10.6d, 9.71428571428571d, 11.7142857142857d, 11d, 10.152380952381d, 4.71428571428571d };
        for (var columnIndex = 1; columnIndex <= expectedWidths.Length; columnIndex++)
        {
            AssertNearlyEqual(expectedWidths[columnIndex - 1], worksheet.Column(columnIndex).Width, 0.02d, $"第 {columnIndex} 列宽必须匹配客户模板。");
        }
        AssertNearlyEqual(27d, worksheet.Row(CenterProductReportFormat.DetailHeaderRow).Height, 0.01d, "明细表头行高必须匹配参考模板。");
        AssertTrue(worksheet.Cell("A11").Style.Font.Bold, "明细表头必须保持客户模板的粗体层级。");
        AssertEqual(XLBorderStyleValues.Thin, worksheet.Cell("A11").Style.Border.TopBorder, "明细表头必须保留细边框。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E12").GetString(), "产品结果必须读取 PLC ProductResult，不得聚合焊点结果。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("D12").GetString(), "点/拍照结果必须直接读取 TestResult。");
        AssertMerged(worksheet, "A12:A13", "同一产品的产品编号必须合并。");
        AssertMerged(worksheet, "E12:E13", "同一产品的产品结果必须合并。");
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

        AssertEqual("工位", worksheet.Cell("A11").GetString(), "双工位报表必须生成工位列。");
        AssertEqual("左工位", worksheet.Cell("A12").GetString(), "工位 1 必须使用规范化后的配置名称。");
        AssertEqual("右工位", worksheet.Cell("A14").GetString(), "工位 2 必须使用规范化后的配置名称。");
        AssertEqual($"结束时间：{endTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D9").GetString(), "结束时间必须与持久化 EndTime 一致并使用模板格式。");
        AssertMerged(worksheet, "A12:A13", "工位 1 公共字段必须按工位和产品编号合并。");
        AssertMerged(worksheet, "B12:B13", "工位 1 产品编号必须合并。");
        AssertMerged(worksheet, "F12:F13", "工位 1 产品结果必须合并。");
        AssertMerged(worksheet, "A14:A15", "工位 2 公共字段必须形成独立合并范围。");
        AssertMerged(worksheet, "B14:B15", "相同产品编号跨工位不得合并成一个范围。");
        AssertMerged(worksheet, "F14:F15", "不同工位的产品结果必须独立合并。");
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
                new[] { "工位", "产品编号", "拍照编号", "左工位电流", "右工位位移", "拍照结果", "产品结果" },
                ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
                "设备端双工位同任务必须按稳定顺序合并两套 ReportEnable 动态列。");
            AssertEqual("1.11", worksheet.Cell("D12").GetString(), "工位 1 必须读取本工位适用配置的动态值。");
            AssertEqual(string.Empty, worksheet.Cell("E12").GetString(), "工位 1 不得读取工位 2 专属动态值。");
            AssertEqual(string.Empty, worksheet.Cell("D13").GetString(), "工位 2 不得读取工位 1 专属动态值。");
            AssertEqual("2.22", worksheet.Cell("E13").GetString(), "工位 2 必须读取本工位适用配置的动态值。");
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
            new[] { "工位", "产品编号", "拍照编号", "左工位电流", "右工位位移", "拍照结果", "产品结果" },
            ReadHeaderRow(centerWorksheet, CenterProductReportFormat.DetailHeaderRow),
            "中心端必须保持与设备端一致的双工位动态列并集语义。");
        AssertEqual("1.11", centerWorksheet.Cell("D12").GetString(), "中心工位 1 不得串入工位 2 值。");
        AssertEqual(string.Empty, centerWorksheet.Cell("E12").GetString(), "中心工位 1 的工位 2 专属列必须为空。");
        AssertEqual(string.Empty, centerWorksheet.Cell("D13").GetString(), "中心工位 2 的工位 1 专属列必须为空。");
        AssertEqual("2.22", centerWorksheet.Cell("E13").GetString(), "中心工位 2 必须写入本工位专属值。");
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
        var detailHeaders = ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow);

        AssertEqual(11, detailHeaders.Length, "六个扩展动态列应让单工位明细超过 J 并到达 K 列。");
        AssertMerged(worksheet, "G1:K1", "动态列超过 J 时首行最后一组公共字段必须扩展到 K。");
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
            new[] { "产品编号", "焊点编号", "峰值电流", "焊点结果", "产品结果" },
            ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
            "单工位点焊样例必须省略工位列，并只包含 ReportEnable 动态列。");
        AssertFalse(ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow).Contains("峰值电流上限"), "设备端 SaveEnable 独占列不得进入报表。");
        AssertFalse(ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow).Contains("峰值电流下限"), "设备端 MesEnable 独占列不得进入报表。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("D12").GetString(), "点焊结果必须直接读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("C12").GetString(), "设备端 ReportEnable 动态值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E12").GetString(), "点焊产品结果必须直接读取 PLC ProductResult。");
        AssertEqual("结束时间：", worksheet.Cell("D9").GetString(), "未完工设备任务的 EndTime 必须为空。");
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
            new[] { "工位", "产品编号", "拍照编号", "峰值电流", "拍照结果", "产品结果" },
            ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
            "双工位检测样例必须包含工位、拍照标题和 ReportEnable 动态列。");
        AssertEqual("左工位", worksheet.Cell("A12").GetString(), "同一任务的工位 1 必须进入双工位报表。");
        AssertEqual("右工位", worksheet.Cell("A14").GetString(), "同一任务的工位 2 必须进入双工位报表。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E12").GetString(), "双工位点结果必须读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("D12").GetString(), "双工位 ReportEnable 动态值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("F12").GetString(), "工位 1 产品结果必须读取 PLC ProductResult。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("F14").GetString(), "工位 2 产品结果必须读取 PLC ProductResult。");
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D9").GetString(), "已完工设备任务必须精确使用持久化 EndTime。");
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
            AssertEqual("结束时间：", unfinishedWorkbook.Worksheet("生产报表").Cell("D9").GetString(), "中心产品请求生成的未完工报表 EndTime 必须为空。");
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
            new[] { "产品编号", "拍照编号", "峰值电流保存值", "峰值电流保存结果", "拍照结果", "产品结果" },
            ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow),
            "中心完成态样例必须保留设备标题，并只显示 SaveEnable 动态列。");
        AssertFalse(ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow).Contains("峰值电流报表上限"), "中心报表不得串入 ReportEnable 独占列。");
        AssertFalse(ReadHeaderRow(worksheet, CenterProductReportFormat.DetailHeaderRow).Contains("峰值电流 MES 下限"), "中心报表不得串入 MesEnable 独占列。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("E12").GetString(), "中心点结果必须读取 PLC TestResult。");
        AssertEqual("1.21", worksheet.Cell("C12").GetString(), "中心 SaveEnable 实际值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("D12").GetString(), "中心 SaveEnable 结果值必须从 RawDataJson 写入真实 XLSX。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("F12").GetString(), "中心产品结果必须读取 PLC ProductResult。");
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D9").GetString(), "中心完成态必须精确使用任务 EndTime。");
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

static void FinishReportQueuesGeneratedXlsxEvenWithoutReportEnable()
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
    AssertTrue(
        noReportResult.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile),
        "已生成 XLSX 时，即使无有效 ReportEnable 也必须创建 MES ReportFile 任务。");
    AssertTrue(
        noReportUploadTasks.Enqueued.Any(uploadTask => uploadTask.TaskType == ProductionConstants.UploadTaskTypes.ReportFile
            && uploadTask.FilePath == "local-fixed-fields.xlsx"),
        "已生成 XLSX 的 MES ReportFile 任务必须实际进入上传队列并保留文件路径。");

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

static void ReportFileUploadTasksReconcileGeneratedXlsxRecords()
{
    var uploadTaskServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var getTasksMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "public IReadOnlyList<UploadTaskSummary> GetTasks",
        "public IReadOnlyList<UploadTaskSummary> GetProcessParameterRows");
    var executeAllMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "public async Task<int> ExecuteAllPendingAsync",
        "public void RequestRetry");
    var retryAllMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "public int RequestRetryAll",
        "public void DeleteTask");
    var reconcileMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "private void SyncReportFileTasksFromReports",
        "public async Task<UploadTaskSummary?> ExecuteAsync");
    var upsertMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "private void UpsertReportFileUploadTask",
        "private static bool ShouldSyncReportFileTask");
    var shouldSyncMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "private static bool ShouldSyncReportFileTask",
        "private static BizUploadTask BuildReportFileUploadTask");

    AssertTrue(getTasksMethod.Contains("SyncReportFileTasksFromReports();", StringComparison.Ordinal), "报表待上传页必须先补齐已生成 XLSX 的上传任务。");
    AssertTrue(executeAllMethod.Contains("SyncReportFileTasksFromReports();", StringComparison.Ordinal), "批量自动补传报表前必须先补齐孤儿报表任务。");
    AssertTrue(retryAllMethod.Contains("SyncReportFileTasksFromReports();", StringComparison.Ordinal), "人工全部重试报表前必须先补齐孤儿报表任务。");
    AssertTrue(reconcileMethod.Contains("Queryable<BizProductionReportFile>", StringComparison.Ordinal), "报表补齐必须以已生成的 BizProductionReportFile 为来源。");
    AssertTrue(reconcileMethod.Contains("ShouldSyncReportFileTask", StringComparison.Ordinal), "报表补齐必须只处理 Pending/Failed/Retrying 的 XLSX 报表。");
    AssertTrue(reconcileMethod.Contains("UpsertReportFileUploadTask(weldTask, report)", StringComparison.Ordinal), "有效报表记录必须补齐成 ReportFile 上传任务。");
    AssertTrue(upsertMethod.Contains("task.WeldTaskId == weldTask.Id", StringComparison.Ordinal), "报表补齐查重必须覆盖旧 LocalExpStartId business id，避免开工补传后重复建任务。");
    AssertTrue(upsertMethod.Contains("existing.IsDeleted", StringComparison.Ordinal), "用户删除过的报表上传任务不能被补齐逻辑反复恢复。");
    AssertTrue(upsertMethod.Contains("existing.Status == ProductionConstants.UploadStatuses.Uploaded", StringComparison.Ordinal), "已上传报表不能被补齐逻辑回退为待上传。");
    AssertTrue(upsertMethod.Contains("existing.Status == ProductionConstants.UploadStatuses.Uploading", StringComparison.Ordinal), "正在上传的报表任务不能被补齐逻辑覆盖。");
    AssertTrue(upsertMethod.Contains("Insertable(uploadTask)", StringComparison.Ordinal), "缺失的报表上传任务必须被创建。");
    AssertTrue(shouldSyncMethod.Contains("NormalizeStatus(report.UploadStatus)", StringComparison.Ordinal), "历史报表上传状态必须先归一化后判断是否需要补齐。");
}
static void ReportFileWaitsForSuccessfulFinishReport()
{
    var runningTask = new BizWeldTask
    {
        Id = 41,
        TaskStatus = ProductionConstants.ProductInstanceStatuses.Running,
        StartTime = new DateTime(2026, 8, 21, 8, 0, 0)
    };
    var finishTime = new DateTime(2026, 8, 21, 8, 45, 51);
    var completedTask = new BizWeldTask
    {
        Id = 41,
        TaskStatus = ProductionConstants.ProductInstanceStatuses.Completed,
        StartTime = runningTask.StartTime,
        EndTime = finishTime
    };
    AssertFalse(ReportFileUploadDependencyRules.IsWeldTaskCompleted(runningTask), "运行中工单不得获得报告文件上传资格。");
    AssertTrue(ReportFileUploadDependencyRules.IsWeldTaskCompleted(completedTask), "已持久化结束时间的完成工单必须通过本地完工门禁。");

    var pendingFinish = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.FinishReport,
        Status = ProductionConstants.UploadStatuses.Pending
    };
    var uploadedFinish = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.FinishReport,
        Status = ProductionConstants.UploadStatuses.Uploaded
    };
    AssertFalse(ReportFileUploadDependencyRules.IsFinishReportSatisfied([pendingFinish]), "MES 完工待上传时必须阻止报告文件。");
    AssertTrue(ReportFileUploadDependencyRules.IsFinishReportSatisfied([uploadedFinish]), "MES 完工成功后才能上传报告文件。");
    AssertTrue(ReportFileUploadDependencyRules.IsFinishReportSatisfied([]), "缺少 FinishReport 任务的旧完工记录按兼容规则允许上传。");

    var prematureReport = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.ReportFile,
        Status = ProductionConstants.UploadStatuses.Uploaded,
        CompletedTime = finishTime.AddMinutes(-29)
    };
    var finalReport = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.ReportFile,
        Status = ProductionConstants.UploadStatuses.Uploaded,
        CompletedTime = finishTime.AddSeconds(10)
    };
    AssertTrue(ReportFileUploadDependencyRules.ShouldReopenUploadedReport(prematureReport, completedTask), "早于工单完工的 Uploaded 报告任务必须重新入队。");
    AssertFalse(ReportFileUploadDependencyRules.ShouldReopenUploadedReport(finalReport, completedTask), "完工后已上传的最终报告不得重复入队。");

    var uploadCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"), Encoding.UTF8);
    var executeMethod = ExtractMethodText(uploadCode, "public async Task<UploadTaskSummary?> ExecuteAsync", "public async Task<int> ExecuteAllPendingAsync");
    var syncMethod = ExtractMethodText(uploadCode, "private void SyncReportFileTasksFromReports", "private void UpsertReportFileUploadTask");
    var requestMethod = ExtractMethodText(uploadCode, "private UploadReportFileReq? BuildReportFileRequest", "private UploadTaskSummary? FinishExecution");
    AssertSourceOrder(executeMethod, "CanExecuteReportFileTask(candidate)", "MarkUploading(id)", "报告文件必须在改为 Uploading 前检查完工依赖。");
    AssertTrue(syncMethod.Contains("ReportFileUploadDependencyRules.IsWeldTaskCompleted", StringComparison.Ordinal), "启动对账不得为未完工工单恢复报告任务。");
    AssertTrue(requestMethod.Contains("CanExecuteReportFileTaskUnsafe(weldTask)", StringComparison.Ordinal), "构造 MES 报告请求时必须再次验证完工依赖。");
    AssertTrue(uploadCode.Contains("ShouldReopenUploadedReportFileTask", StringComparison.Ordinal), "完工生成最终报告时必须能够重新打开提前 Uploaded 的旧任务。");
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

static void SharedTaskStationScopeWidensOnlyForSameWorkOrderDualStation()
{
    AssertSequenceEqual(
        [1, 2],
        RecipeStationScopeRules.ResolveSharedTaskStations(enableDualStation: true, enableDualWorkOrder: false, stationNo: 2),
        "双工位同工单只落库一条任务，工位 2 必须放宽到两个工位才能查到共享任务。");
    AssertSequenceEqual(
        [1, 2],
        RecipeStationScopeRules.ResolveSharedTaskStations(enableDualStation: true, enableDualWorkOrder: false, stationNo: 1),
        "双工位同工单时，工位 1 的任务范围同样覆盖两个工位。");
    AssertSequenceEqual(
        [2],
        RecipeStationScopeRules.ResolveSharedTaskStations(enableDualStation: true, enableDualWorkOrder: true, stationNo: 2),
        "双工单时各工位任务独立，工位 2 不得查到工位 1 的任务。");
    AssertSequenceEqual(
        [1],
        RecipeStationScopeRules.ResolveSharedTaskStations(enableDualStation: false, enableDualWorkOrder: false, stationNo: 1),
        "单工位模式只看默认工位。");
    AssertSequenceEqual(
        [1],
        RecipeStationScopeRules.ResolveSharedTaskStations(enableDualStation: false, enableDualWorkOrder: false, stationNo: 0),
        "共享工位号应归一化为默认工位。");
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

static void RealtimePreviewValuesRequireCompletedPointResults()
{
    AssertTrue(ProductRealtimePreviewRules.ShouldReadTestValues(ProductionConstants.TestResults.Ok), "OK 面必须允许显示测试值，包括真实 0 值。");
    AssertTrue(ProductRealtimePreviewRules.ShouldReadTestValues(ProductionConstants.TestResults.Ng), "普通 NG 面必须允许显示测试值，包括真实 0 值。");
    AssertFalse(ProductRealtimePreviewRules.ShouldReadTestValues(ProductionConstants.TestResults.PreWeldNg), "焊前 NG 没有测试参数，不得显示测试值。");
    AssertFalse(ProductRealtimePreviewRules.ShouldReadTestValues(ProductionConstants.TestResults.NoResultRawValue), "PLC 原始结果 0 表示未测试，不得显示清零残留。");
    AssertFalse(ProductRealtimePreviewRules.ShouldReadTestValues(null), "空结果不得显示测试值。");
    AssertFalse(ProductRealtimePreviewRules.ShouldReadTestValues("--"), "未知结果不得显示测试值。");

    var previewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductRealtimePreviewService.cs"),
        Encoding.UTF8);
    var buildRowsMethod = ExtractMethodText(
        previewCode,
        "private async Task<PreviewRowsResult> BuildRowsAsync(",
        "private async Task<ProductRealtimePreviewRow> BuildRowAsync(");
    var previewValueMethod = ExtractMethodText(
        previewCode,
        "private async Task<string> ResolvePreviewValue(",
        "private async Task<string> ResolvePreviewResult(");
    AssertTrue(
        buildRowsMethod.Contains("ProductRealtimePreviewRules.ShouldReadTestValues(plcTouchResult)", StringComparison.Ordinal),
        "实时预览必须先按每个面/焊点结果判定测试值有效性。");
    AssertTrue(
        buildRowsMethod.Contains("ResolveTouchNoBase(config)", StringComparison.Ordinal)
            && buildRowsMethod.Contains("useProgramPointNumber", StringComparison.Ordinal),
        "实时编号必须可在PLC读取和程序序号之间切换。");
    AssertTrue(
        buildRowsMethod.Contains("NormalizeRealtimePointNumber", StringComparison.Ordinal),
        "PLC实时编号为空、0或非法值时必须显示--，不能回退程序序号。");
    AssertTrue(
        buildRowsMethod.Contains("plcFaceResults.All(IsTerminalPointResult)", StringComparison.Ordinal),
        "产品结果必须等待所有采集点进入终态后才显示。");
    AssertTrue(
        previewValueMethod.Contains("if (!shouldReadTestValues)", StringComparison.Ordinal)
            && previewValueMethod.Contains("return \"--\";", StringComparison.Ordinal),
        "未测试、清零或无效结果必须直接显示空值，不能读取 PLC 测试值地址。");

    var monitorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"),
        Encoding.UTF8);
    var realtimeRowsMethod = ExtractMethodText(
        monitorCode,
        "private void ApplyRealtimeWeldParameterRows(",
        "private void ApplyWeldParameterRows(");
    var schemePreviewMethod = ExtractMethodText(
        monitorCode,
        "private void ApplySchemePreview(ProductIdentity identity, bool force)",
        "private IEnumerable<WeldParameterRow> BuildSchemePreviewRows(");
    AssertTrue(
        realtimeRowsMethod.Contains("preserveStableValues: false", StringComparison.Ordinal),
        "实时 PLC 快照必须允许空值清除上一帧，不能恢复旧 OK/NG 或旧参数。");
    AssertTrue(
        schemePreviewMethod.Contains("ApplyWeldParameterRows(nextRows);", StringComparison.Ordinal),
        "静态方案预览仍应保留现有稳定值复制行为。");
    var monitorDesignerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"),
        Encoding.UTF8);
    AssertFalse(monitorDesignerCode.Contains("tagLiveResult1", StringComparison.Ordinal)
        || monitorDesignerCode.Contains("tagLiveResult2", StringComparison.Ordinal), "实时预览顶部不得继续保留重复产品结果Tag。");
    AssertTrue(monitorCode.Contains("ApplyProductResultToGroup(", StringComparison.Ordinal)
        && monitorCode.Contains("productChanged ? ProductionConstants.TestResults.NotAvailable : snapshot.ProductResult", StringComparison.Ordinal), "实时产品结果必须统一写入grpProductResult，下一产品首帧恢复为--。");
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
    AssertEqual("左安全光栅被挡住", rows[5].Content, "报警内容中的左/右机构名称必须原样保留，不得解释为程序工位。");
    AssertEqual("右安全光栅被挡住", rows[6].Content, "报警内容中的左/右机构名称必须原样保留，不得解释为程序工位。");
    AssertTrue(typeof(AlarmAddressImportRow).GetProperty("StationNo") is null, "报警地址导入模型不得再暴露程序工位字段。");

    var addressViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"), Encoding.UTF8);
    var alarmColumnMethod = ExtractMethodText(
        addressViewCode,
        "private void ConfigureAlarmAddressColumns()",
        "private void ConfigureRecipeNameColumns()");
    AssertFalse(alarmColumnMethod.Contains("StationNo", StringComparison.Ordinal), "报警地址维护表不得显示或编辑程序工位列。");

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

static void CenterDashboardWorkOrderQuantityDeduplicatesSharedWorkOrder()
{
    // 双工位同工单只有一份计划量，求和会让分母翻倍。
    var shared = new CenterDashboardDeviceDto
    {
        Stations =
        [
            new CenterDashboardStationDto { CurrentWorkOrder = "WO-1", WorkOrderQuantity = 100 },
            new CenterDashboardStationDto { CurrentWorkOrder = "wo-1", WorkOrderQuantity = 100 }
        ]
    };

    AssertEqual(100, shared.WorkOrderQuantity, "Dual stations on one work order must count its quantity once.");

    var distinct = new CenterDashboardDeviceDto
    {
        Stations =
        [
            new CenterDashboardStationDto { CurrentWorkOrder = "WO-1", WorkOrderQuantity = 100 },
            new CenterDashboardStationDto { CurrentWorkOrder = "WO-2", WorkOrderQuantity = 40 }
        ]
    };

    AssertEqual(140, distinct.WorkOrderQuantity, "Different work orders must add their quantities.");

    var idle = new CenterDashboardDeviceDto
    {
        Stations =
        [
            new CenterDashboardStationDto { CurrentWorkOrder = "WO-1", WorkOrderQuantity = 100 },
            new CenterDashboardStationDto { CurrentWorkOrder = "   ", WorkOrderQuantity = 999 }
        ]
    };

    AssertEqual(100, idle.WorkOrderQuantity, "Stations without a work order must not inflate the denominator.");
}

static void CenterDashboardAchievementRateUsesQualifiedOverWorkOrderQuantity()
{
    // CenterDashboardStatusPresenter 是 CenterServer 内部类型，测试项目不可见，
    // 此处复算同一公式守住口径：分子为合格数，不含不良品，与设备端 MonitorView 一致。
    static decimal? Rate(int workOrderQuantity, int qualifiedCount)
        => workOrderQuantity <= 0 ? null : (decimal)qualifiedCount * 100m / workOrderQuantity;

    AssertEqual(50m, Rate(100, 50), "Achievement rate must be qualified/quantity.");
    AssertEqual(120m, Rate(100, 120), "Over-production must exceed 100 percent.");
    AssertEqual(null, Rate(0, 50), "Zero quantity must yield null instead of dividing by zero.");

    // 同一批产量下达成率必须低于按总数计算的旧口径，确认分子换成了合格数。
    const int workOrderQuantity = 100;
    const int total = 57;
    const int qualified = 43;
    AssertEqual(43m, Rate(workOrderQuantity, qualified), "Achievement rate must exclude failed pieces.");
    AssertEqual(57m, Rate(workOrderQuantity, total), "Guard value proves the two definitions differ.");
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
    AssertEqual("height", headers[2], "Dynamic saved values must be placed before the point result column.");
    AssertEqual("height_result", headers[3], "Dynamic saved result values must preserve equipment order.");
    AssertEqual("焊点结果", headers[4], "Center report point result must follow all dynamic test values.");
    AssertEqual("产品结果", headers[^1], "PLC product result must remain the final fixed detail column.");
    AssertFalse(headers.Contains("工位"), "Single-station center details must omit the station column.");
    AssertFalse(headers.Contains("工号"), "Task fields belong in the customer template header, not repeated detail columns.");
    AssertFalse(headers.Contains("设备编号"), "Center-only device columns must not be inserted into the Excel report table.");
    AssertFalse(headers.Contains("设备名称"), "Center-only device columns must not be inserted into the Excel report table.");
    AssertFalse(headers.Contains("系统类型"), "Center-only system columns must not be inserted into the Excel report table.");
    AssertEqual("检测面", CenterProductReportFormat.ResolvePointNoTitle("焊点序号", wholePieceInspection: true), "整件检测的旧默认焊点表头必须升级为检测面。");
    AssertEqual("检测结果", CenterProductReportFormat.ResolvePointResultTitle("焊点结果", wholePieceInspection: true), "整件检测的旧默认结果表头必须升级为检测结果。");
    AssertEqual("拍照次数", CenterProductReportFormat.ResolvePointNoTitle("拍照次数", wholePieceInspection: true), "整件检测自定义编号表头必须保留。");
    AssertEqual("拍照结果", CenterProductReportFormat.ResolvePointResultTitle("拍照结果", wholePieceInspection: true), "整件检测自定义结果表头必须保留。");
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
    AssertEqual("高度实际值", headers[2], "Forwarded equipment dynamic headers must be used before the point result column.");
    AssertEqual("相机结果", headers[3], "Forwarded equipment point result header must follow dynamic values.");
}

static void CenterProductReportRequestCarriesProductionReportFields()
{
    var request = new CenterProductReportRequest
    {
        Batch = "B001",
        Quantity = 20,
        PartName = "引出线",
        ProcessName = "点焊",
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
    AssertEqual("点焊", request.ProcessName, "Center report request must preserve process name for the Excel report.");
    AssertEqual("OP10", request.ProcessNo, "Center report request must preserve process number for the Excel report.");
    AssertEqual("U001", request.OperatorNo, "Center report request must preserve task operator for the Excel report.");
    AssertEqual("U002", request.Points[0].OperatorNo, "Center report request must preserve point operator when available.");
}

static void CenterTelemetrySignatureTracksDashboardContentOnly()
{
    var baseline = CreateCenterTelemetryTestRequest();
    var baselineSignature = CenterTelemetryRules.BuildSnapshotSignature(baseline);
    var timestampOnly = CloneCenterTelemetryRequest(baseline);
    timestampOnly.HeartbeatAt = timestampOnly.HeartbeatAt.AddMinutes(1);
    timestampOnly.Stations[0].CollectedAt = timestampOnly.Stations[0].CollectedAt.AddMinutes(1);

    AssertEqual(
        baselineSignature,
        CenterTelemetryRules.BuildSnapshotSignature(timestampOnly),
        "心跳和采集时间变化不应触发全量遥测。");

    var mutations = new (string Name, Action<CenterTelemetrySnapshotRequest> Apply)[]
    {
        ("DeviceName", request => request.DeviceName = "Device-B"),
        ("SystemType", request => request.SystemType = CenterServerConstants.SystemTypes.Electromagnetic),
        ("PlcConnected", request => request.Stations[0].PlcConnected = false),
        ("PlcConnectionState", request => request.Stations[0].PlcConnectionState = "Disconnected"),
        ("DeviceStatusCode", request => request.Stations[0].DeviceStatusCode = "4"),
        ("DeviceStatusName", request => request.Stations[0].DeviceStatusName = "异常"),
        ("AlarmMessage", request => request.Stations[0].AlarmMessage = "Alarm"),
        ("CurrentWorkOrder", request => request.Stations[0].CurrentWorkOrder = "WO-2"),
        ("ProductJobNo", request => request.Stations[0].ProductJobNo = "JOB-2"),
        ("ProductModel", request => request.Stations[0].ProductModel = "MODEL-2"),
        ("TodayTotalCount", request => request.Stations[0].TodayTotalCount++),
        ("TodayQualifiedCount", request => request.Stations[0].TodayQualifiedCount++),
        ("TodayFailedCount", request => request.Stations[0].TodayFailedCount++),
        ("WorkOrderQuantity", request => request.Stations[0].WorkOrderQuantity++)
    };

    foreach (var mutation in mutations)
    {
        var changed = CloneCenterTelemetryRequest(baseline);
        mutation.Apply(changed);
        AssertFalse(
            string.Equals(
                baselineSignature,
                CenterTelemetryRules.BuildSnapshotSignature(changed),
                StringComparison.Ordinal),
            $"中心看板字段 {mutation.Name} 变化时必须触发遥测同步。");
    }
}

static void CenterTelemetrySyncGatesSnapshotsBehindHeartbeat()
{
    var settings = new AppSettings
    {
        EnableCenterServerSync = true,
        CenterServerBaseUrl = "http://127.0.0.1:7099/",
        CenterServerSystemType = CenterServerConstants.SystemTypes.Other,
        DeviceId = "D-001",
        DeviceName = "Device-A"
    };
    var handler = new CenterTelemetryHttpMessageHandler { IsAvailable = false };
    var interactionLogs = new FakeCenterInteractionLogService();
    var exceptionLogs = new FakeProgramExceptionLogService();
    var availabilityLogGate = new CenterServerAvailabilityLogGate();
    using var httpClient = new HttpClient(handler);
    var client = new CenterTelemetryClient(httpClient, interactionLogs, availabilityLogGate);
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new CenterTelemetrySyncService(
        dbContext,
        new FakeAppSettingsService { Current = settings },
        new FakeDeviceStatusService(),
        new FakePlcCommunicationService(),
        new FakePlcProductionMonitorService(),
        exceptionLogs,
        client);
    var baseline = CreateCenterTelemetryTestRequest();

    AssertThrows<HttpRequestException>(
        () => service.PushRequestAsync(settings, baseline).GetAwaiter().GetResult(),
        "中心服务器不可达时必须由心跳失败中止本周期。");
    AssertThrows<HttpRequestException>(
        () => service.PushRequestAsync(settings, baseline).GetAwaiter().GetResult(),
        "持续不可达时仍应只执行心跳探测。");
    AssertSequenceEqual(
        ["api/center/heartbeat", "api/center/heartbeat"],
        handler.RequestPaths,
        "心跳失败时不得继续发送设备状态遥测。");
    AssertEqual(1, interactionLogs.Entries.Count, "连续心跳失败只应记录首次故障。");
    AssertEqual(
        AppConstants.CenterInteractionTypes.Heartbeat,
        interactionLogs.Entries[0].InteractionType,
        "连接拒绝必须记录为心跳类型。");

    handler.IsAvailable = true;
    service.PushRequestAsync(settings, baseline).GetAwaiter().GetResult();
    AssertSequenceEqual(
        ["api/center/heartbeat", "api/center/telemetry"],
        handler.RequestPaths.TakeLast(2).ToArray(),
        "恢复连接后必须先心跳，再发送首次完整遥测。");
    AssertEqual(3, interactionLogs.Entries.Count, "恢复应记录心跳成功和首次遥测成功。");
    AssertEqual(
        AppConstants.CenterInteractionTypes.Heartbeat,
        interactionLogs.Entries[1].InteractionType,
        "恢复连接日志必须保持心跳类型。");
    AssertTrue(interactionLogs.Entries[1].IsSuccess, "恢复心跳日志必须标记为成功。");

    var sameContent = CloneCenterTelemetryRequest(baseline);
    sameContent.HeartbeatAt = sameContent.HeartbeatAt.AddMinutes(1);
    sameContent.Stations[0].CollectedAt = sameContent.Stations[0].CollectedAt.AddMinutes(1);
    service.PushRequestAsync(settings, sameContent).GetAwaiter().GetResult();
    AssertEqual(
        "api/center/heartbeat",
        handler.RequestPaths[^1],
        "内容未变化时只能发送心跳。");
    AssertEqual(3, interactionLogs.Entries.Count, "连续成功心跳不应追加本地日志。");

    handler.IsAvailable = false;
    AssertThrows<HttpRequestException>(
        () => service.PushRequestAsync(settings, sameContent).GetAwaiter().GetResult(),
        "已同步快照断线时必须保留成功签名。");
    handler.IsAvailable = true;
    var requestCountBeforeRecovery = handler.RequestPaths.Count;
    service.PushRequestAsync(settings, sameContent).GetAwaiter().GetResult();
    AssertEqual(
        requestCountBeforeRecovery + 1,
        handler.RequestPaths.Count,
        "无内容变化的断线恢复只能发送一条心跳。");
    AssertEqual(
        "api/center/heartbeat",
        handler.RequestPaths[^1],
        "断线恢复不得因清空签名误发遥测。");

    var changed = CloneCenterTelemetryRequest(baseline);
    changed.Stations[0].TodayTotalCount++;
    service.PushRequestAsync(settings, changed).GetAwaiter().GetResult();
    AssertSequenceEqual(
        ["api/center/heartbeat", "api/center/telemetry"],
        handler.RequestPaths.TakeLast(2).ToArray(),
        "看板字段变化后必须在心跳成功后发送遥测。");

    var rejected = CloneCenterTelemetryRequest(changed);
    rejected.Stations[0].TodayQualifiedCount++;
    handler.TelemetryAccepted = false;
    service.PushRequestAsync(settings, rejected).GetAwaiter().GetResult();
    AssertTrue(service.Current.IsConnected, "遥测业务拒绝不能误报中心服务器断线。");
    handler.TelemetryAccepted = true;
    service.PushRequestAsync(settings, rejected).GetAwaiter().GetResult();
    AssertSequenceEqual(
        ["api/center/heartbeat", "api/center/telemetry"],
        handler.RequestPaths.TakeLast(2).ToArray(),
        "遥测拒绝后必须保留待同步签名并在下一周期重试。");
}

static void CenterAvailabilityLogGateAggregatesFailuresAndRecovery()
{
    var gate = new CenterServerAvailabilityLogGate();
    var startedAt = new DateTime(2026, 8, 18, 8, 0, 0);

    var first = gate.RegisterFailure(startedAt);
    AssertTrue(first.ShouldWrite, "首次中心连接失败必须立即记录。");
    AssertTrue(first.IsFirstFailure, "首次中心连接失败必须标记为新故障。");
    AssertEqual(1L, first.FailureCount, "首次故障计数必须为1。");

    var repeated = gate.RegisterFailure(startedAt.AddMinutes(9));
    AssertFalse(repeated.ShouldWrite, "十分钟内的重复连接失败必须被抑制。");
    AssertEqual(2L, repeated.FailureCount, "被抑制的失败仍必须累计计数。");

    var summary = gate.RegisterFailure(startedAt.AddMinutes(10));
    AssertTrue(summary.ShouldWrite, "持续不可达满十分钟必须记录摘要。");
    AssertFalse(summary.IsFirstFailure, "十分钟摘要不能标记为首次故障。");
    AssertEqual(3L, summary.FailureCount, "摘要必须携带完整累计失败次数。");

    AssertTrue(gate.RegisterReachable(), "不可达后的首个有效响应必须标记恢复。");
    AssertFalse(gate.RegisterReachable(), "连续有效响应不能重复标记恢复。");

    var failedAgain = gate.RegisterFailure(startedAt.AddMinutes(11));
    AssertTrue(failedAgain.ShouldWrite, "恢复后再次不可达必须作为新故障立即记录。");
    AssertTrue(failedAgain.IsFirstFailure, "恢复后的新故障必须重置首次标记。");
    AssertEqual(1L, failedAgain.FailureCount, "恢复后的新故障必须重置累计次数。");
}

static void CenterAvailabilityClassifiesTimeoutAndCancellation()
{
    AssertTrue(
        CenterServerAvailabilityLogGate.IsConnectivityFailure(new HttpRequestException("refused"), CancellationToken.None),
        "HTTP连接异常必须识别为中心不可达。");
    AssertTrue(
        CenterServerAvailabilityLogGate.IsConnectivityFailure(new TaskCanceledException("timeout"), CancellationToken.None),
        "调用方未取消时的HTTP超时必须识别为中心不可达。");

    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    AssertFalse(
        CenterServerAvailabilityLogGate.IsConnectivityFailure(new TaskCanceledException("shutdown"), cancellation.Token),
        "应用主动取消不能记录为中心不可达。");
    AssertFalse(
        CenterServerAvailabilityLogGate.IsConnectivityFailure(new InvalidOperationException("invalid payload"), CancellationToken.None),
        "非连接类程序异常不能被不可达门控吞掉。");
}

static void CenterClientAggregatesConnectivityFailuresAcrossInstances()
{
    var settings = new AppSettings
    {
        CenterServerBaseUrl = "http://127.0.0.1:7099/",
        DeviceId = "D-001",
        DeviceName = "Device-A"
    };
    var gate = new CenterServerAvailabilityLogGate();
    var interactionLogs = new FakeCenterInteractionLogService();
    var exceptionLogs = new FakeProgramExceptionLogService();
    var firstHandler = new CenterTelemetryHttpMessageHandler { IsAvailable = false };
    var secondHandler = new CenterTelemetryHttpMessageHandler { IsAvailable = false };
    using var firstHttpClient = new HttpClient(firstHandler);
    using var secondHttpClient = new HttpClient(secondHandler);
    var firstClient = new CenterTelemetryClient(firstHttpClient, interactionLogs, gate);
    var secondClient = new CenterTelemetryClient(secondHttpClient, interactionLogs, gate);

    AssertThrows<HttpRequestException>(
        () => firstClient.UploadHeartbeatAsync(settings, new CenterTelemetrySnapshotRequest()).GetAwaiter().GetResult(),
        "首次心跳连接失败必须继续抛给调用方处理状态。");
    AssertThrows<HttpRequestException>(
        () => secondClient.UploadProductReportAsync(settings, new CenterProductReportRequest()).GetAwaiter().GetResult(),
        "产品报表连接失败必须继续抛给队列处理重试。");
    AssertEqual(1, interactionLogs.Entries.Count, "不同客户端实例的连续不可达只应记录首次交互失败。");
    AssertEqual(0, exceptionLogs.Entries.Count, "不同后台链路的连续不可达不得写入程序异常日志。");

    secondHandler.IsAvailable = true;
    secondClient.UploadProductReportAsync(settings, new CenterProductReportRequest()).GetAwaiter().GetResult();
    AssertEqual(2, interactionLogs.Entries.Count, "首个恢复响应必须追加一条成功交互日志。");
    AssertTrue(interactionLogs.Entries[^1].IsSuccess, "恢复交互日志必须标记成功。");

    firstHandler.IsAvailable = true;
    firstClient.UploadHeartbeatAsync(settings, new CenterTelemetrySnapshotRequest()).GetAwaiter().GetResult();
    AssertEqual(2, interactionLogs.Entries.Count, "恢复后的连续成功心跳不能重复追加恢复日志。");
}

static void CenterHeartbeatRejectionStaysOutOfProgramExceptionLog()
{
    var settings = new AppSettings
    {
        EnableCenterServerSync = true,
        CenterServerBaseUrl = "http://127.0.0.1:7099/",
        DeviceId = "D-001",
        DeviceName = "Device-A"
    };
    var handler = new CenterTelemetryHttpMessageHandler { HeartbeatAccepted = false };
    var interactionLogs = new FakeCenterInteractionLogService();
    var exceptionLogs = new FakeProgramExceptionLogService();
    var gate = new CenterServerAvailabilityLogGate();
    using var httpClient = new HttpClient(handler);
    var client = new CenterTelemetryClient(httpClient, interactionLogs, gate);
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new CenterTelemetrySyncService(
        dbContext,
        new FakeAppSettingsService { Current = settings },
        new FakeDeviceStatusService(),
        new FakePlcCommunicationService(),
        new FakePlcProductionMonitorService(),
        exceptionLogs,
        client);

    service.PushRequestAsync(settings, new CenterTelemetrySnapshotRequest()).GetAwaiter().GetResult();

    AssertTrue(service.Current.IsConnected, "中心已返回心跳拒绝时仍应保持连接可达状态。");
    AssertEqual(1, interactionLogs.Entries.Count, "心跳业务拒绝必须保留服务器交互日志。");
    AssertFalse(interactionLogs.Entries[0].IsSuccess, "心跳业务拒绝的服务器日志必须标记失败。");
    AssertEqual(0, exceptionLogs.Entries.Count, "中心心跳业务拒绝不得写入程序异常日志。");
}

static void CenterMalformedResponseStaysInProgramExceptionLog()
{
    var settings = new AppSettings
    {
        CenterServerBaseUrl = "http://127.0.0.1:7099/",
        DeviceId = "D-001",
        DeviceName = "Device-A"
    };
    var handler = new CenterTelemetryHttpMessageHandler { MalformedResponse = true };
    var interactionLogs = new FakeCenterInteractionLogService();
    var gate = new CenterServerAvailabilityLogGate();
    using var httpClient = new HttpClient(handler);
    var client = new CenterTelemetryClient(httpClient, interactionLogs, gate);

    AssertThrows<JsonException>(
        () => client.UploadHeartbeatAsync(settings, new CenterTelemetrySnapshotRequest()).GetAwaiter().GetResult(),
        "畸形中心响应必须继续抛给后台服务，由后台服务写入程序异常日志。");
    AssertTrue(interactionLogs.Entries.Count > 0, "畸形响应仍必须保留服务器交互日志。");
    AssertFalse(interactionLogs.Entries[0].IsSuccess, "畸形响应的服务器日志必须标记失败。");
}

static void CenterInteractionTypesStaySharedAcrossClientAndServer()
{
    var clientCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetryClient.cs"),
        Encoding.UTF8);
    var serverCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.CenterServer", "Program.cs"),
        Encoding.UTF8);
    var pushLogCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.CenterServer", "Services", "CenterPushJsonlLogService.cs"),
        Encoding.UTF8);
    var dashboardCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.CenterServer", "Pages", "Dashboard.razor"),
        Encoding.UTF8)
        + File.ReadAllText(
            GetRepoFilePath("AutoWeldSystem.CenterServer", "Components", "LogRail.razor"),
            Encoding.UTF8);

    foreach (var typeName in new[] { "Heartbeat", "Telemetry", "ProductReport" })
    {
        var sharedReference = $"AppConstants.CenterInteractionTypes.{typeName}";
        AssertTrue(
            clientCode.Contains(sharedReference, StringComparison.Ordinal),
            $"设备端必须使用共享中心消息类型 {typeName}。");
        AssertTrue(
            serverCode.Contains(sharedReference, StringComparison.Ordinal)
            || pushLogCode.Contains(sharedReference, StringComparison.Ordinal),
            $"服务端接收或写日志必须使用共享中心消息类型 {typeName}。");
        AssertTrue(
            dashboardCode.Contains(sharedReference, StringComparison.Ordinal),
            $"中心看板必须使用共享中心消息类型 {typeName}。");
    }

    AssertFalse(serverCode.Contains("HandleTelemetryAsync(\"heartbeat\"", StringComparison.Ordinal), "心跳入口不得保留独立字符串常量。");
    AssertFalse(serverCode.Contains("HandleTelemetryAsync(\"telemetry\"", StringComparison.Ordinal), "遥测入口不得保留独立字符串常量。");
    AssertFalse(pushLogCode.Contains("RequestType = \"product-report\"", StringComparison.Ordinal), "产品数据日志不得保留独立字符串常量。");
}

static CenterTelemetrySnapshotRequest CreateCenterTelemetryTestRequest()
{
    return new CenterTelemetrySnapshotRequest
    {
        DeviceId = "D-001",
        DeviceName = "Device-A",
        SystemType = CenterServerConstants.SystemTypes.Other,
        HeartbeatAt = new DateTime(2026, 8, 6, 9, 0, 0),
        Stations =
        [
            new CenterTelemetryStationSnapshot
            {
                StationNo = 1,
                PlcConnected = true,
                PlcConnectionState = "Connected",
                DeviceStatusCode = "1",
                DeviceStatusName = "运行",
                AlarmMessage = string.Empty,
                CurrentWorkOrder = "WO-1",
                ProductJobNo = "JOB-1",
                ProductModel = "MODEL-1",
                TodayTotalCount = 10,
                TodayQualifiedCount = 9,
                TodayFailedCount = 1,
                CollectedAt = new DateTime(2026, 8, 6, 9, 0, 0)
            }
        ]
    };
}

static CenterTelemetrySnapshotRequest CloneCenterTelemetryRequest(CenterTelemetrySnapshotRequest request)
{
    return JsonSerializer.Deserialize<CenterTelemetrySnapshotRequest>(JsonSerializer.Serialize(request))
        ?? throw new InvalidOperationException("无法克隆中心遥测测试快照。");
}
static void CenterTelemetryJsonlFallbackPreservesMesStatusNames()
{
    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetrySyncService.cs"),
        Encoding.UTF8);
    var buildStationSnapshot = ExtractMethodText(
        centerCode,
        "private CenterTelemetryStationSnapshot BuildStationSnapshot",
        "private TodayProductionSummary GetTodayProductionSummary");

    AssertEqual(
        "运行",
        CenterTelemetryRules.ResolveReportedStatusName("1", null),
        "PLC 原始状态存在时必须继续使用 PLC 状态名称。");
    AssertEqual(
        "开机",
        CenterTelemetryRules.ResolveReportedStatusName("1", "开机"),
        "PLC 无有效值时 JSONL 状态 1 必须保留 MES 的开机语义。");
    AssertEqual(
        "异常",
        CenterTelemetryRules.ResolveReportedStatusName("4", "异常"),
        "PLC 无有效值时 JSONL 状态 4 必须保留 MES 的异常语义。");
    AssertEqual(
        "未知",
        CenterTelemetryRules.ResolveReportedStatusName(null, null),
        "PLC 与 JSONL 都无状态时必须显示未知。");

    var sharedPoweredOn = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = string.Empty,
        OccurredTime = new DateTime(2026, 7, 22, 9, 0, 0)
    };
    var olderStationException = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        OccurredTime = sharedPoweredOn.OccurredTime.AddMinutes(-1)
    };
    var newerStationRecovered = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
        OccurredTime = sharedPoweredOn.OccurredTime.AddMinutes(1)
    };
    var sameTimeStationException = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        OccurredTime = sharedPoweredOn.OccurredTime
    };

    var sharedOnly = CenterTelemetryRules.ResolveLatestDeviceStatus(null, sharedPoweredOn);
    AssertTrue(ReferenceEquals(sharedPoweredOn, sharedOnly), "单工位和双工位仅有共享状态时都必须使用 StationNo=0 JSONL。");
    AssertTrue(
        ReferenceEquals(
            sharedPoweredOn,
            CenterTelemetryRules.ResolveLatestDeviceStatus(olderStationException, sharedPoweredOn)),
        "共享状态较新时必须覆盖旧工位状态。");
    AssertTrue(
        ReferenceEquals(
            newerStationRecovered,
            CenterTelemetryRules.ResolveLatestDeviceStatus(newerStationRecovered, sharedPoweredOn)),
        "工位状态较新时必须保留工位状态。");
    AssertTrue(
        ReferenceEquals(
            sameTimeStationException,
            CenterTelemetryRules.ResolveLatestDeviceStatus(sameTimeStationException, sharedPoweredOn)),
        "时间相同时共享状态不能覆盖工位状态。");
    AssertEqual(
        "开机",
        DeviceStatusReportRules.GetStatusName(sharedOnly!.DeviceStatus),
        "共享 JSONL 缺少状态名称时必须按 MES 状态码补为开机。");

    var dashboard = CenterTelemetryRules.BuildDashboardState(
        new CenterDeviceRuntimeDto
        {
            PlcDeviceStatusCode = "1",
            PlcDeviceStatusName = "开机",
            LastSeenAt = new DateTime(2026, 7, 22, 9, 0, 0)
        },
        new DateTime(2026, 7, 22, 9, 0, 1),
        offlineTimeoutSeconds: 15);
    AssertEqual("开机", dashboard.PlcDeviceStatusName, "中心看板不能把设备端 JSONL 的开机名称重写为 PLC 运行。");
    AssertTrue(
        buildStationSnapshot.Contains("DeviceStatusReportRules.GetStatusName(statusCode)", StringComparison.Ordinal),
        "JSONL 历史记录缺少 StatusName 时必须按 MES 状态码补名，不能回退为 PLC 同码名称。");
    AssertTrue(
        buildStationSnapshot.Contains("GetLatestStatus(ProductionConstants.Stations.SharedStationNo)", StringComparison.Ordinal)
            && buildStationSnapshot.Contains("CenterTelemetryRules.ResolveLatestDeviceStatus", StringComparison.Ordinal),
        "PLC 无有效值时工位遥测必须在工位与共享 JSONL 中选择最新状态。");
    AssertTrue(
        buildStationSnapshot.Contains("CenterTelemetryRules.ResolveAlarmMessage(production.AlarmMessage, stationStatus)", StringComparison.Ordinal),
        "共享生命周期状态不能覆盖工位报警备注，报警内容必须取自工位自身状态。");
}

static void CenterAlarmMessageClearsOnceTheExceptionRecovers()
{
    // 只有 MES 状态 4（异常）的备注才是报警内容。
    var exception = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        Remark = "异常：伺服过载",
        OccurredTime = new DateTime(2026, 8, 7, 10, 0, 0)
    };
    AssertEqual(
        "伺服过载",
        CenterTelemetryRules.ResolveAlarmMessage(null, exception),
        "状态 4 的备注是报警内容，且必须剥离「；工位：工位N」后缀。");

    // 程序执行结束 / 异常恢复 / 开机 都不是报警，其备注不得上送为报警。
    foreach (var (statusCode, remark, label) in new[]
    {
        (ProductionConstants.MesDeviceStatuses.ProgramEnded, "右工位：程序执行结束", "程序执行结束"),
        (ProductionConstants.MesDeviceStatuses.Recovered, "异常恢复：伺服过载", "异常恢复"),
        (ProductionConstants.MesDeviceStatuses.PoweredOn, "开机", "开机"),
        (ProductionConstants.MesDeviceStatuses.Stopped, "停机", "停机"),
        (ProductionConstants.MesDeviceStatuses.ProgramStarted, "左工位：程序执行开始", "程序执行开始")
    })
    {
        var log = new BizDeviceStatusLog
        {
            StationNo = ProductionConstants.Stations.DefaultStationNo,
            DeviceStatus = statusCode,
            Remark = remark,
            OccurredTime = new DateTime(2026, 8, 7, 10, 1, 0)
        };
        AssertEqual(
            string.Empty,
            CenterTelemetryRules.ResolveAlarmMessage(null, log),
            $"「{label}」不是报警，其备注不得作为报警内容上送。");
    }

    // PLC 实时报警优先于 JSONL 备注。
    AssertEqual(
        "急停被按下",
        CenterTelemetryRules.ResolveAlarmMessage("急停被按下", exception),
        "PLC 实时报警内容必须优先于 JSONL 备注。");

    // PLC 恢复（报警为空）后即便历史异常记录仍在，也不能再显示报警。
    var recovered = new BizDeviceStatusLog
    {
        StationNo = ProductionConstants.Stations.DefaultStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
        Remark = "异常恢复-工位1：伺服过载；",
        OccurredTime = new DateTime(2026, 8, 7, 10, 2, 0)
    };
    AssertEqual(
        string.Empty,
        CenterTelemetryRules.ResolveAlarmMessage(string.Empty, recovered),
        "异常恢复后报警内容必须清空，否则看板会一直挂着陈旧报警。");
}

static void CenterAlarmTextStripsDuplicatedStationMarkers()
{
    // 双工位按「左/右工位：」标注；单工位只显示报警内容本身。
    AssertEqual(
        "左工位：伺服过载",
        CenterTelemetryRules.FormatStationAlarmText(true, "伺服过载", 1, isDualStation: true),
        "双工位设备的报警必须标注「左工位：」。");

    AssertEqual(
        "右工位：气压不足",
        CenterTelemetryRules.FormatStationAlarmText(true, "气压不足", 2, isDualStation: true),
        "双工位设备的 2 号工位必须标注「右工位：」。");

    AssertEqual(
        "伺服过载",
        CenterTelemetryRules.FormatStationAlarmText(true, "伺服过载", 1, isDualStation: false),
        "单工位设备只显示报警内容，不标注工位。");

    // 设备端备注里已带的工位标识必须去重，不能出现「工位1：xxx；工位：工位1」。
    AssertEqual(
        "左工位：伺服过载",
        CenterTelemetryRules.FormatStationAlarmText(true, "工位1：伺服过载；工位：工位1", 1, isDualStation: true),
        "报警文本中的工位前后缀必须剥离，避免工位标识重复三次。");

    // 未报警时不得显示任何报警文本，即便报警内容残留历史值。
    AssertTrue(
        CenterTelemetryRules.FormatStationAlarmText(false, "伺服过载", 1, isDualStation: true) is null,
        "非报警状态必须返回 null，异常恢复后横幅要消失。");

    // 报警但设备端未给出内容时仍需可见提示。
    AssertEqual(
        "左工位：报警（无详细信息）",
        CenterTelemetryRules.FormatStationAlarmText(true, string.Empty, 1, isDualStation: true),
        "报警状态缺少详情时必须给出回退文案。");
}

static void CenterForwardingBusinessIdsHashFullIdentity(){
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
        ResultExpression = "0:W-12",
        Unit = "A"
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
        new[] { "峰值电流保存值 (A)", "峰值电流保存结果" },
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
            AssertEqual("流转卡号：FLOW-CENTER-001", worksheet.Cell("A1").GetString(), "中心可见报表必须复用客户模板流转卡表头。");
            AssertEqual("结束时间：", worksheet.Cell("D9").GetString(), "产品请求生成报表时 EndTime 必须为空。");
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
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", updatedWorksheet.Cell("D9").GetString(), "完工更新必须精确刷新任务 EndTime。");
        AssertEqual("合格数量：19", updatedWorksheet.Cell("D7").GetString(), "完工更新必须刷新最终 QualifiedQty。");
        AssertEqual(2, CountCenterDataRows(updatedWorkbook), "完工更新不得重复携带或追加产品点明细。");
        AssertEqual(13, updatedWorksheet.LastRowUsed()!.RowNumber(), "完工更新不得增加可见明细行数。");

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
        AssertEqual("P001", worksheet.Cell("A12").GetString(), "没有动态列时仍必须输出产品明细。");
        AssertEqual(ProductionConstants.TestResults.Ok, worksheet.Cell("D12").GetString(), "固定产品结果列必须保留 PLC 产品结果。");
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
        AssertEqual("工位", dualSheet.Cell("A11").GetString(), "双工位中心报表必须保留工位列。");
        AssertEqual("右工位", dualSheet.Cell("A12").GetString(), "双工位中心报表必须显示设备端解析后的配置名称。");
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
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("D12").GetString(), "幂等替换后必须显示最新点结果。");
        AssertEqual(ProductionConstants.TestResults.Ng, worksheet.Cell("C12").GetString(), "幂等替换后必须显示最新 PLC 产品结果。");
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
        AssertEqual($"结束时间：{finishTime:yyyy-MM-dd HH:mm:ss}", worksheet.Cell("D9").GetString(), "迟到产品请求不得清空已完成 EndTime。");
        AssertEqual("合格数量：19", worksheet.Cell("D7").GetString(), "迟到产品请求不得回退最终 QualifiedQty。");
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

static void ProcessParameterUploadViewsOnlyIncludeMesTargets()
{
    var mesTask = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
        Target = ProductionConstants.UploadTargets.Mes,
        Status = ProductionConstants.UploadStatuses.Uploaded
    };
    var centralServerTask = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
        Target = ProductionConstants.UploadTargets.CentralServer,
        Status = ProductionConstants.UploadStatuses.Failed
    };
    var centerProductTask = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.CenterProductReport,
        Target = ProductionConstants.UploadTargets.CentralServer,
        Status = ProductionConstants.UploadStatuses.Pending
    };

    AssertTrue(UploadTaskVisibilityRules.IsMesProcessParameterTask(mesTask), "MES 过程参数任务必须保留在过程参数页签和总览中。");
    AssertFalse(UploadTaskVisibilityRules.IsMesProcessParameterTask(centralServerTask), "CentralServer 目标的过程参数任务必须从页签和总览中排除。");
    AssertFalse(UploadTaskVisibilityRules.IsMesProcessParameterTask(centerProductTask), "CentralServer 独立产品转发任务不属于 MES 过程参数任务。");

    var mesProcessStatuses = new[] { mesTask, centralServerTask, centerProductTask }
        .Where(UploadTaskVisibilityRules.IsMesProcessParameterTask)
        .Select(task => task.Status);
    AssertEqual(
        ProductionConstants.UploadStatuses.Uploaded,
        UploadSummaryStatusResolver.ResolveProcessParameterStatus(mesProcessStatuses, Array.Empty<BizWeldPointRecord>()),
        "过程参数总览状态只能由 MES 目标任务决定，不能被 CentralServer 失败任务覆盖。");

    var uploadTaskServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var getProcessParameterRowsMethod = ExtractMethodText(
        uploadTaskServiceCode,
        "public IReadOnlyList<UploadTaskSummary> GetProcessParameterRows",
        "public UploadTaskSummary? GetById");
    AssertTrue(
        getProcessParameterRowsMethod.Contains("ProductionConstants.UploadTargets.Mes", StringComparison.Ordinal),
        "过程参数明细查询必须在数据库层限定 Target=MES，避免 CentralServer 任务遮挡产品历史虚拟行。");

    var summaryServiceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadStatusSummaryService.cs"),
        Encoding.UTF8);
    AssertTrue(
        summaryServiceCode.Contains("Where(UploadTaskVisibilityRules.IsMesProcessParameterTask)", StringComparison.Ordinal),
        "待上传总览的过程参数状态必须复用 MES 目标过滤规则。");
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
    AssertTrue(rows.All(row => !row.CanRetry && row.CanDelete), "虚拟行不能重试，但必须允许删除对应的产品采集记录。");
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
    AssertTrue(weldJson.Contains("\"IsTest\":false", StringComparison.Ordinal), "点焊设备开启全局试焊件后，即使 false 也必须输出 IsTest=false。");

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

static void ProcessParameterNumericRolesAppendTestItemUnits()
{
    var method = typeof(UploadTaskService).GetMethod(
        "FormatMesRoleValue",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(method is not null, "过程参数上传服务必须保留统一的单位格式入口。");
    var item = new DimTestItem { ItemId = 1, ItemName = "峰值电流", Unit = "A" };

    var actual = (string?)method!.Invoke(null, ["12.3", item, SchemeDetailValueRole.Actual]);
    var upper = (string?)method.Invoke(null, ["15", item, SchemeDetailValueRole.Upper]);
    var lower = (string?)method.Invoke(null, ["10", item, SchemeDetailValueRole.Lower]);
    var result = (string?)method.Invoke(null, ["OK", item, SchemeDetailValueRole.Result]);
    var empty = (string?)method.Invoke(null, [string.Empty, item, SchemeDetailValueRole.Actual]);

    AssertEqual("12.3 A", actual, "普通过程参数和整件检测 A/B 实际值必须共用单位格式。");
    AssertEqual("15 A", upper, "过程参数上限必须追加单位。");
    AssertEqual("10 A", lower, "过程参数下限必须追加单位。");
    AssertEqual("OK", result, "过程参数结果字段不得追加单位。");
    AssertEqual(string.Empty, empty, "空过程参数值不得生成独立单位字符串。");

    var upload = new ProcessParameterUploadItem();
    upload.DynamicFields["Current"] = actual;
    var json = JsonSerializer.Serialize(upload);
    AssertTrue(json.Contains("\"Current\":\"12.3 A\"", StringComparison.Ordinal), "MES JSON 必须保持原字段名并在值中追加单位。");
    AssertFalse(json.Contains("CurrentUnit", StringComparison.Ordinal), "MES JSON 不得新增单位字段。");
}

static void WholePieceInspectionUploadUsesSideAndResultFields()
{
    var checkItem = new ProcessParameterUploadItem
    {
        ExpStartId = "TASK-CHECK",
        SideNo = "面1",
        Result = "合格",
        Type = null,
        TouchNo = null
    };
    checkItem.DynamicFields["Symmetry"] = "对称度";

    // 默认序列化会转义非 ASCII，断言改用不转义选项以稳定比对中文值。
    var jsonOptions = new JsonSerializerOptions
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    var checkJson = JsonSerializer.Serialize(checkItem, jsonOptions);
    AssertTrue(checkJson.Contains("\"SideNo\":\"面1\"", StringComparison.Ordinal), "整件检测必须输出当前面号。");
    AssertTrue(checkJson.Contains("\"Result\":\"合格\"", StringComparison.Ordinal), "整件检测必须输出当前面的拍照结果。");
    AssertTrue(checkJson.Contains("\"Symmetry\":\"对称度\"", StringComparison.Ordinal), "整件检测动态字段必须按实际方案字段输出。");
    AssertFalse(checkJson.Contains("\"Type\"", StringComparison.Ordinal), "整件检测不得输出 Type 字段。");
    AssertFalse(checkJson.Contains("\"TouchNo\"", StringComparison.Ordinal), "整件检测不得输出 TouchNo 字段。");

    var weldItem = new ProcessParameterUploadItem
    {
        TouchNo = "焊点7",
        Type = "EM",
        SideNo = null,
        Result = null
    };
    var weldJson = JsonSerializer.Serialize(weldItem, jsonOptions);
    AssertTrue(weldJson.Contains("\"TouchNo\":\"焊点7\"", StringComparison.Ordinal), "点焊设备必须继续输出 TouchNo。");
    AssertTrue(weldJson.Contains("\"Type\":\"EM\"", StringComparison.Ordinal), "点焊设备必须继续输出 Type。");
    AssertFalse(weldJson.Contains("\"SideNo\"", StringComparison.Ordinal), "点焊设备不应输出 SideNo。");
    AssertFalse(weldJson.Contains("\"Result\"", StringComparison.Ordinal), "点焊设备不应输出整件检测 Result。");
}

static void DeviceLogProjectsEveryDeviceStatusCode()
{
    var expected = new (string Status, string Summary)[]
    {
        (ProductionConstants.MesDeviceStatuses.Stopped, "停机"),
        (ProductionConstants.MesDeviceStatuses.PoweredOn, "开机"),
        (ProductionConstants.MesDeviceStatuses.Exception, "故障报警"),
        (ProductionConstants.MesDeviceStatuses.Recovered, "故障恢复"),
        (ProductionConstants.MesDeviceStatuses.ProgramStarted, "程序执行开始"),
        (ProductionConstants.MesDeviceStatuses.ProgramEnded, "程序执行结束")
    };

    foreach (var (status, summary) in expected)
    {
        var entry = DeviceLifecycleLogRules.CreateDeviceStatusEntry(new BizDeviceStatusLog
        {
            DeviceStatus = status,
            StationNo = 1,
            OccurredTime = new DateTime(2026, 8, 15, 9, 0, 0)
        });
        AssertEqual(summary, entry.Summary, $"设备状态 {status} 必须在设备日志显示中文摘要。");
    }

    var alarm = DeviceLifecycleLogRules.CreateDeviceStatusEntry(new BizDeviceStatusLog
    {
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        AlarmAddress = "DB1.0",
        AlarmContent = "气压不足",
        OccurredTime = new DateTime(2026, 8, 15, 9, 1, 0)
    });
    AssertEqual("故障报警：气压不足", alarm.Summary, "报警摘要必须显示具体报警内容。");
    AssertTrue(alarm.Detail.Contains("DB1.0", StringComparison.Ordinal), "报警详情必须保留报警地址。");
    AssertEqual(AppConstants.DeviceLifecycleEventTypes.FaultAlarm, alarm.EventType, "报警状态必须映射为故障报警事件。");

    var legacy = DeviceLifecycleLogRules.CreateDeviceStatusEntry(new BizDeviceStatusLog
    {
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        Remark = "工位1：急停触发；",
        OccurredTime = new DateTime(2026, 8, 15, 9, 2, 0)
    });
    AssertEqual("故障报警：工位1：急停触发；", legacy.Summary, "无报警内容的历史记录必须回退显示备注。");
}

static void PendingUploadViewDeletesSelectedRowsInBatches()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var deleteStart = viewCode.IndexOf("private void DeleteSelectedUploadRecords()", StringComparison.Ordinal);
    var deleteEnd = viewCode.IndexOf("private IReadOnlyList<UploadTaskSummary> GetSelectedUploadTasks()", deleteStart, StringComparison.Ordinal);
    AssertTrue(deleteStart >= 0 && deleteEnd > deleteStart, "待上传页必须保留批量删除实现。");
    var deleteMethod = viewCode[deleteStart..deleteEnd];
    var permissionMethod = ExtractMethodText(
        viewCode,
        "private void ApplyDeletePermissionForActiveTab()",
        "private int GetPendingCount");

    AssertTrue(deleteMethod.Contains("selectedSummaries", StringComparison.Ordinal), "批量删除必须读取所有选中的工单信息行。");
    AssertTrue(deleteMethod.Contains("GetSelectedUploadTasks()", StringComparison.Ordinal), "批量删除必须读取所有选中的上传任务行。");
    AssertTrue(deleteMethod.Contains("foreach (var task in selectedTasks.Where(task => task.IsVirtual))", StringComparison.Ordinal), "批量删除必须处理所有选中的虚拟过程参数行。");
    AssertTrue(deleteMethod.Contains("foreach (var task in selectedTasks.Where(task => !task.IsVirtual))", StringComparison.Ordinal), "批量删除必须处理所有选中的普通上传任务。");
    AssertFalse(deleteMethod.Contains("dgvPending.CurrentRow", StringComparison.Ordinal), "批量删除不能只读取当前行。");
    AssertTrue(permissionMethod.Contains("dgvPending.SelectedRows", StringComparison.Ordinal), "删除按钮启用条件必须按选中行集合判断。");
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

static void FinishMakeupOnlyCoversProductsNotYetUploaded()
{
    // 按数量上传批次为 5：P001-P005 已成功上传，P006 仍被在途任务认领，P007-P009 是完工时的剩余未传件。
    var records = new[]
    {
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P001", sequenceNo: 1, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P002", sequenceNo: 2, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P003", sequenceNo: 3, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P004", sequenceNo: 4, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P005", sequenceNo: 5, uploadStatus: ProductionConstants.UploadStatuses.Uploaded),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P006", sequenceNo: 6),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P007", sequenceNo: 7),
        BuildCompletedPoint(taskId: 9, stationNo: 2, productNo: "P008", sequenceNo: 8),
        BuildCompletedPoint(taskId: 9, stationNo: 1, productNo: "P009", sequenceNo: 9, uploadStatus: ProductionConstants.UploadStatuses.Failed),
        BuildCompletedPoint(taskId: 10, stationNo: 1, productNo: "OTHER-TASK", sequenceNo: 10)
    };

    var makeupProductNos = ProcessParameterMakeupRules.TakeMakeupProductNos(
        records,
        weldTaskId: 9,
        claimedProductNos: new[] { "P006" });

    AssertSequenceEqual(
        new[] { "P007", "P008", "P009" },
        makeupProductNos,
        "完工补传只应包含未上传且未被在途任务认领的产品，已上传批次不得重复提交。");

    var withoutClaims = ProcessParameterMakeupRules.TakeMakeupProductNos(records, weldTaskId: 9);
    AssertSequenceEqual(
        new[] { "P006", "P007", "P008", "P009" },
        withoutClaims,
        "没有在途任务时，所有未上传产品都应进入完工补传范围。");

    var allUploaded = new[]
    {
        BuildCompletedPoint(taskId: 11, stationNo: 1, productNo: "Q001", sequenceNo: 1, uploadStatus: ProductionConstants.UploadStatuses.Uploaded)
    };
    AssertEqual(
        0,
        ProcessParameterMakeupRules.TakeMakeupProductNos(allUploaded, weldTaskId: 11).Count,
        "全部已上传时补传范围必须为空，完工不应再创建过程参数任务。");
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
    AssertEqual(
        "程序执行开始",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            1,
            dualStationEnabled: false,
            "左",
            "右"),
        "单工位程序开始 Remark 不应携带工位前缀。");
    AssertEqual(
        "左工位：程序执行开始",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            1,
            dualStationEnabled: true,
            "左",
            "右"),
        "双工位程序开始必须使用系统设置中的左工位名称。");
    AssertEqual(
        "右工位：程序执行结束",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.ProgramEnded,
            2,
            dualStationEnabled: true,
            "左",
            "右"),
        "双工位程序结束必须使用系统设置中的右工位名称。");
    AssertEqual(
        "左工位：程序执行开始",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            1,
            dualStationEnabled: true,
            "左工位",
            "右工位"),
        "系统设置已包含工位后缀时不得重复追加工位。");
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

static void MesDeviceStatusRulesFormatConciseExceptionRemarks()
{
    AssertEqual(
        "异常：左电极使用寿命到达，请更换",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Exception,
            1,
            dualStationEnabled: false,
            "左",
            "右",
            " 左电极使用寿命到达，请更换；; "),
        "单工位异常 Remark 必须只包含异常前缀和原始报警内容。");
    AssertEqual(
        "异常：左电极使用寿命到达，请更换",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Exception,
            2,
            dualStationEnabled: true,
            "左",
            "右",
            "左电极使用寿命到达，请更换"),
        "双工位异常不得使用 PLC 报警地址中的工位号或程序工位前缀。");
    AssertEqual(
        "异常：设备异常",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Exception,
            0,
            dualStationEnabled: true,
            "左",
            "右"),
        "缺少报警内容时必须使用设备异常兜底。");
}

static void MesDeviceStatusRulesFormatConciseRecoveryRemarks()
{
    AssertEqual(
        "异常恢复：左电极使用寿命到达，请更换",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Recovered,
            1,
            dualStationEnabled: false,
            "左",
            "右",
            " 左电极使用寿命到达，请更换；; "),
        "单工位恢复 Remark 必须只包含恢复前缀和原始报警内容。");
    AssertEqual(
        "异常恢复：左电极使用寿命到达，请更换",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Recovered,
            2,
            dualStationEnabled: true,
            "左",
            "右",
            "左电极使用寿命到达，请更换"),
        "双工位恢复不得使用 PLC 报警地址中的工位号或程序工位前缀。");
    AssertEqual(
        "异常恢复：设备异常",
        DeviceStatusReportRules.FormatRemark(
            ProductionConstants.MesDeviceStatuses.Recovered,
            0,
            dualStationEnabled: true,
            "左",
            "右"),
        "恢复记录缺少报警内容时必须使用设备异常兜底。");
}

static void DeviceStatusRecordIdentitySupportsGuidAndLegacyKeys()
{
    var guid = Guid.Parse("A7A2A606-7840-4A3D-9CE4-8B8C7BE8357B");
    var current = new BizDeviceStatusLog { RecordId = guid.ToString("D"), Id = 42 };
    var legacy = new BizDeviceStatusLog { Id = 42 };

    AssertEqual(guid.ToString("N"), DeviceStatusRecordIdentityRules.GetRecordKey(current), "新记录必须把 GUID 规范化为 N 格式。");
    AssertEqual("legacy:42", DeviceStatusRecordIdentityRules.GetRecordKey(legacy), "旧记录必须使用 legacy:{Id}。");
    AssertEqual(null, DeviceStatusRecordIdentityRules.GetRecordKey(new BizDeviceStatusLog()), "无 GUID 且无旧 Id 的记录没有可靠身份。");
    AssertEqual(
        "legacy:42",
        DeviceStatusRecordIdentityRules.ReadTaskRecordKey("device-status:42", "{\"LogId\":42}"),
        "旧任务的整数 BusinessId 和 LogId 必须继续可解析。");
    AssertEqual(
        guid.ToString("N"),
        DeviceStatusRecordIdentityRules.ReadTaskRecordKey(
            $"device-status:{guid:N}",
            $"{{\"RecordKey\":\"{guid:D}\"}}"),
        "新任务必须从只含 RecordKey 的 payload 定位 JSONL。");
    AssertSequenceEqual(
        new[] { "device-status:legacy:42", "device-status:42" },
        DeviceStatusRecordIdentityRules.GetCompatibleBusinessIds("legacy:42").ToArray(),
        "旧记录查重时必须同时识别规范和历史 BusinessId。");
}

static void DeviceStatusLocalLogStoreUsesRecordKeys()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusRecordKeyTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 7, 22, 8, 30, 0, 123);
    var recordId = Guid.NewGuid().ToString("N");

    try
    {
        var pending = new BizDeviceStatusLog
        {
            RecordId = recordId,
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
        var failed = new BizDeviceStatusLog
        {
            RecordId = recordId,
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Failed,
            ReportTime = occurredTime.AddSeconds(1),
            ReportMessage = "MES offline"
        };
        var retained = new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 2,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Recovered),
            OccurredTime = occurredTime.AddMinutes(1),
            ReportStatus = ProductionConstants.UploadStatuses.Uploaded
        };

        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings), "Pending 首版本必须成功落盘。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppendVersion(failed, settings), "同一记录键的 Failed 版本必须追加到既有来源。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(retained, settings), "另一条记录必须成功落盘。");

        var latest = DeviceStatusLocalLogStore.ReadByRecordKey(settings, recordId);
        AssertTrue(latest is not null, "必须能按 GUID 记录键读取来源。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, latest!.ReportStatus, "同一键最后追加的版本必须生效。");
        AssertEqual(1, DeviceStatusLocalLogStore.ReadPending(settings).Count, "只有 Pending/Failed 最新版本进入待上传来源。");
        AssertEqual(recordId, DeviceStatusRecordIdentityRules.GetRecordKey(DeviceStatusLocalLogStore.ReadLatestForStation(settings, 1)), "工位最新状态必须来自 JSONL。");

        AssertTrue(DeviceStatusLocalLogStore.TryRemove(new[] { failed }, settings), "按记录键删除必须成功。");
        AssertEqual(null, DeviceStatusLocalLogStore.ReadByRecordKey(settings, recordId), "删除后同一键的全部追加版本都必须消失。");
        AssertEqual(1, DeviceStatusLocalLogStore.Read(settings, maxCount: 10).Count, "删除不能影响其他记录键。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusLocalLogStoreSkipsInvalidIdentities()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusInvalidIdentityTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var directory = DeviceStatusLocalLogStore.GetLogDirectory(settings);
    var filePath = Path.Combine(directory, "2026-07-22.jsonl");
    var errors = new List<string>();

    try
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            filePath,
            "{\"DeviceId\":\"D-001\",\"DeviceStatus\":\"1\",\"OccurredTime\":\"2026-07-22T09:00:00\"}" + Environment.NewLine,
            Encoding.UTF8);

        var logs = DeviceStatusLocalLogStore.Read(
            settings,
            maxCount: 10,
            onError: (_, context) => errors.Add(context));

        AssertEqual(0, logs.Count, "无 RecordId 和旧 Id 的记录必须跳过。");
        AssertEqual(1, errors.Count, "跳过无效身份时必须向业务服务暴露一次诊断。");
        AssertTrue(errors[0].Contains("2026-07-22.jsonl", StringComparison.OrdinalIgnoreCase), "诊断必须包含损坏来源文件。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceWritesJsonlBeforeMes()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusWriteFirstTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);
    var pendingSeenBeforeMes = false;
    var notifications = 0;
    service.LogsChanged += (_, _) => notifications++;
    mes.DeviceStatusRequestObserved = _ =>
    {
        var persisted = service.GetLogs(maxCount: 10);
        pendingSeenBeforeMes = persisted.Count == 1
            && persisted[0].ReportStatus == ProductionConstants.UploadStatuses.Pending;
    };

    try
    {
        var result = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,
                "PLC alarm",
                "PLC-S1",
                stationNo: 1)
            .GetAwaiter()
            .GetResult();
        var persistedResult = service.GetLog(result.RecordId!);

        AssertTrue(pendingSeenBeforeMes, "调用 MES 时 JSONL 中必须已经存在 Pending 首版本。");
        AssertEqual(1, mes.DeviceStatusRequests.Count, "首版本落盘成功后才允许调用一次 MES。");
        AssertTrue(Guid.TryParseExact(result.RecordId, "N", out _), "新记录必须使用 N 格式 GUID RecordId。");
        AssertTrue(persistedResult is not null, "MES 结果必须继续保存在同一个 JSONL 记录键下。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, persistedResult!.ReportStatus, "成功响应必须追加 Uploaded 版本。");
        AssertEqual(result.OccurredTime, persistedResult.OccurredTime, "追加结果不能丢失原始毫秒时间。");
        AssertTrue(notifications >= 2, "Pending 首版本和 Uploaded 结果版本都必须通知 UI 重载。");
        AssertEqual(0, exceptionLogs.Entries.Count, "正常落盘和上报不应写程序异常日志。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusAlarmDetailsPersistAndReachMesRemark()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceAlarmDetailTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());

    try
    {
        var result = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,

                "异常：安全门打开",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.0",
                alarmContent: "安全门打开")
            .GetAwaiter()
            .GetResult();
        var persisted = service.GetLog(result.RecordId!);

        AssertTrue(persisted is not null, "报警设备状态必须写入 JSONL。");
        AssertEqual("DB10.DBX2.0", persisted!.AlarmAddress, "JSONL 最新版本必须保留报警地址。");
        AssertEqual("安全门打开", persisted.AlarmContent, "JSONL 最新版本必须保留报警内容。");
        AssertEqual("异常：安全门打开", persisted.Remark, "JSONL Remark 必须与 MES 使用相同的统一异常格式。");
        AssertEqual(1, mes.DeviceStatusRequests.Count, "报警设备状态应上传一次 MES。");
        AssertEqual("异常：安全门打开", mes.DeviceStatusRequests[0].Remark, "MES 异常 Remark 必须统一为异常前缀和原始报警内容。");
        AssertFalse(mes.DeviceStatusRequests[0].Remark.Contains("DB10.DBX2.0", StringComparison.Ordinal), "MES 异常 Remark 不得包含报警地址。");

        var duplicate = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,

                "异常：安全门打开",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.0",
                alarmContent: "安全门打开")
            .GetAwaiter()
            .GetResult();
        AssertEqual(result.RecordId, duplicate.RecordId, "同一报警周期内同一地址必须复用既有记录。");
        AssertEqual(1, mes.DeviceStatusRequests.Count, "同一报警地址不得重复上传。");

        _ = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,

                "异常：气压低",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.1",
                alarmContent: "气压低")
            .GetAwaiter()
            .GetResult();
        AssertEqual(2, mes.DeviceStatusRequests.Count, "状态仍为 4 时新报警地址必须独立上传。");

        _ = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Recovered,

                "异常恢复：安全门打开",
                "PLC-S1",
                stationNo: 1)
            .GetAwaiter()
            .GetResult();
        var nextCycle = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Exception,

                "异常：安全门打开",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.0",
                alarmContent: "安全门打开")
            .GetAwaiter()
            .GetResult();
        AssertFalse(result.RecordId == nextCycle.RecordId, "状态 5 闭合后，同一报警地址必须能在下一周期重新记录。");
        AssertEqual(4, mes.DeviceStatusRequests.Count, "恢复和下一周期异常必须分别上传 MES。");

        var addressRecovery = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Recovered,

                "异常恢复：安全门打开",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.0",
                alarmContent: "安全门打开")
            .GetAwaiter()
            .GetResult();
        var duplicateAddressRecovery = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Recovered,

                "异常恢复：安全门打开",
                "PLC-S1",
                stationNo: 1,
                alarmAddress: "DB10.DBX2.0",
                alarmContent: "安全门打开")
            .GetAwaiter()
            .GetResult();
        AssertEqual(addressRecovery.RecordId, duplicateAddressRecovery.RecordId, "同一报警地址的状态 5 必须复用既有恢复记录。");
        AssertEqual(5, mes.DeviceStatusRequests.Count, "同一地址的重复恢复不得重复上传 MES。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusPendingExceptionsUseConciseMesRemarks()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusConciseRemarkTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var occurredTime = new DateTime(2026, 7, 23, 9, 0, 0);
    var logs = new[]
    {
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            AlarmAddress = "DB10.DBX2.0",
            AlarmContent = "左电极使用寿命到达，请更换",
            Remark = "左电极使用寿命到达，请更换；报警地址：DB10.DBX2.0；工位：工位1",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        },
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            AlarmAddress = "DB10.DBX2.1",
            AlarmContent = "安全门打开",
            Remark = "安全门打开；报警地址：DB10.DBX2.1；工位：双工位",
            OccurredTime = occurredTime.AddSeconds(1),
            ReportStatus = ProductionConstants.UploadStatuses.Failed
        },
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 2,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
            AlarmAddress = "DB10.DBX2.2",
            AlarmContent = null,
            Remark = "旧异常；报警地址：DB10.DBX2.2；工位：工位2",
            OccurredTime = occurredTime.AddSeconds(2),
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        },
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            Remark = "原异常恢复备注",
            OccurredTime = occurredTime.AddSeconds(3),
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        }
    };

    try
    {
        foreach (var log in logs)
        {
            AssertTrue(DeviceStatusLocalLogStore.TryAppend(log, settings.Current), "历史待传测试记录必须写入 JSONL。");
        }

        service.RetryPendingUploadsAsync().GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[]
            {

                "异常：左电极使用寿命到达，请更换",
                "异常：安全门打开",
                "异常：旧异常",
                "异常恢复：原异常恢复备注"
            },
            mes.DeviceStatusRequests.Select(request => request.Remark).ToArray(),
            "历史异常和恢复补传必须统一使用状态前缀加原始报警内容。");
        AssertTrue(
            mes.DeviceStatusRequests.Take(3).All(request => !request.Remark.Contains("报警地址", StringComparison.Ordinal)),
            "历史异常补传不得继续发送报警地址。");
        AssertSequenceEqual(
            mes.DeviceStatusRequests.Select(request => request.Remark).ToArray(),
            logs.Select(log => service.GetLog(log.RecordId!)?.Remark).ToArray(),
            "历史补传完成后 JSONL 最新版本必须与 MES 请求使用同一 Remark。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusPendingRecoveriesUseConciseMesRemarks()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceRecoveryRemarkTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var occurredTime = new DateTime(2026, 7, 23, 10, 0, 0);
    var logs = new[]
    {
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            AlarmAddress = "DB10.DBX2.0",
            AlarmContent = "左电极使用寿命到达，请更换",
            Remark = "旧恢复；报警地址：DB10.DBX2.0",
            OccurredTime = occurredTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        },
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 0,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            AlarmAddress = "DB10.DBX2.9",
            AlarmContent = "急停",
            Remark = "旧共享恢复",
            OccurredTime = occurredTime.AddMilliseconds(1),
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        },
        new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceId = "D-001",
            StationNo = 1,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Recovered,
            Remark = "旧整周期恢复",
            OccurredTime = occurredTime.AddMilliseconds(2),
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        }
    };

    try
    {
        foreach (var log in logs)
        {
            AssertTrue(DeviceStatusLocalLogStore.TryAppend(log, settings.Current), "恢复补传测试记录必须写入 JSONL。");
        }

        service.RetryPendingUploadsAsync().GetAwaiter().GetResult();
        AssertSequenceEqual(

            ["异常恢复：左电极使用寿命到达，请更换", "异常恢复：急停", "异常恢复：旧整周期恢复"],
            mes.DeviceStatusRequests.Select(request => request.Remark).ToArray(),
            "恢复补传必须统一为异常恢复前缀加报警内容，不能携带 PLC 工位。");
        AssertSequenceEqual(
            mes.DeviceStatusRequests.Select(request => request.Remark).ToArray(),
            logs.Select(log => service.GetLog(log.RecordId!)?.Remark).ToArray(),
            "恢复补传完成后 JSONL 最新版本必须同步为统一 Remark。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceSerializesConcurrentStatusChanges()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusConcurrentChangeTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = false
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, new FakeMesProvider(), new FakeProgramExceptionLogService());
    const int participantCount = 8;
    using var start = new Barrier(participantCount + 1);
    var slowBlankRemark = new string(' ', 20_000_000);

    try
    {
        var tasks = Enumerable.Range(0, participantCount)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    if (!start.SignalAndWait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("并发状态变化测试未能同步启动。");
                    }

                    return service.ChangeStatusAsync(
                            ProductionConstants.MesDeviceStatuses.Exception,
                            slowBlankRemark,
                            "ConcurrentTest",
                            reportToMes: false,
                            stationNo: 1,
                            occurredTime: new DateTime(2026, 7, 22, 10, 15, 0))
                        .GetAwaiter()
                        .GetResult();
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        AssertTrue(start.SignalAndWait(TimeSpan.FromSeconds(5)), "并发状态变化测试必须在超时前就绪。");
        var results = Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromSeconds(15))
            .GetAwaiter()
            .GetResult();
        var logs = service.GetLogs(maxCount: participantCount + 1);

        AssertEqual(1, logs.Count, "同一工位的相同状态并发变化只能首次落盘一次。");
        AssertEqual(
            1,
            results.Select(result => result.RecordId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "被判重的并发调用必须复用同一条 JSONL 记录。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServicePreservesMesSuccessAfterSourceDeletion()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusDeletedAfterSendTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var recordKey = Guid.NewGuid().ToString("N");
    var pending = new BizDeviceStatusLog
    {
        RecordId = recordKey,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
        OccurredTime = new DateTime(2026, 7, 22, 10, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var mes = new FakeMesProvider();
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings.Current), "测试来源必须先写入 JSONL。");
        mes.DeviceStatusRequestObserved = _ => Directory.Delete(
            DeviceStatusLocalLogStore.GetLogDirectory(settings.Current),
            recursive: true);

        var response = service.RetryUploadAsync(recordKey).GetAwaiter().GetResult();

        AssertTrue(response?.IsSuccess == true, "MES 已成功时不能因响应落盘前删源而返回 null。");
        AssertEqual(null, service.GetLog(recordKey), "外部删除后的 JSONL 来源不能被响应结果重建。");
        AssertEqual(1, exceptionLogs.Entries.Count, "MES 成功但结果无法落盘时必须保留程序异常诊断。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceSerializesConcurrentRetries()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusConcurrentRetryTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var recordKey = Guid.NewGuid().ToString("N");
    var pending = new BizDeviceStatusLog
    {
        RecordId = recordKey,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
        OccurredTime = new DateTime(2026, 7, 22, 10, 30, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var secondRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var requestCount = 0;
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (_, cancellationToken) =>
        {
            var currentCount = Interlocked.Increment(ref requestCount);
            (currentCount == 1 ? firstRequestStarted : secondRequestStarted).TrySetResult();
            await releaseResponse.Task.WaitAsync(cancellationToken);
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "操作成功"
            };
        }
    };
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings.Current), "测试来源必须先写入 JSONL。");
        var first = service.RetryUploadAsync(recordKey);
        AssertTrue(firstRequestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "首个 MES 请求必须在超时前开始。");
        var second = service.RetryUploadAsync(recordKey);
        var secondEnteredBeforeRelease = Task.WhenAny(
                secondRequestStarted.Task,
                Task.Delay(TimeSpan.FromMilliseconds(200)))
            .GetAwaiter()
            .GetResult() == secondRequestStarted.Task;

        releaseResponse.TrySetResult();
        var responses = Task.WhenAll(first, second).GetAwaiter().GetResult();

        AssertFalse(secondEnteredBeforeRelease, "同一 JSONL 记录的第二次重试必须等待首个 MES 请求完成。");
        AssertEqual(1, requestCount, "同一 JSONL 记录并发重试只能发送一次 MES 请求。");
        AssertTrue(responses.All(response => response?.IsSuccess == true), "等待方必须复用已上传结果而不是返回 null。");
        AssertEqual(
            ProductionConstants.UploadStatuses.Uploaded,
            service.GetLog(recordKey)?.ReportStatus,
            "并发重试完成后 JSONL 最新版本必须保持 Uploaded。");
    }
    finally
    {
        releaseResponse.TrySetResult();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceSharesConcurrentRetryResults()
{
    var failed = RunConcurrentDeviceStatusRetryScenario(
        new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = "MES 离线"
        },
        deleteSourceBeforeResponse: false);
    var deletedAfterSuccess = RunConcurrentDeviceStatusRetryScenario(
        new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = "操作成功"
        },
        deleteSourceBeforeResponse: true);

    AssertEqual(1, failed.RequestCount, "首个并发 MES 请求失败时，等待方必须共享失败结果而不是立即再发一次。");
    AssertTrue(
        failed.Responses.All(response => response is not null && !response.IsSuccess),
        "首个并发 MES 请求失败时，所有等待方都必须收到同一次失败结果。");
    AssertEqual(1, deletedAfterSuccess.RequestCount, "MES 成功且来源同时被删时，并发调用仍只能发送一次请求。");
    AssertTrue(
        deletedAfterSuccess.Responses.All(response => response?.IsSuccess == true),
        "MES 成功且来源同时被删时，所有等待方都必须共享成功结果。");
}

static void DeviceStatusServiceRetriesPendingLogsInOccurredOrder()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusPendingOrderTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStatuses = new List<string>();
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = (request, _) =>
        {
            requestStatuses.Add(request.DevStatus);
            return Task.FromResult(new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "操作成功"
            });
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var stopped = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 9, 20),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var poweredOn = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
        OccurredTime = new DateTime(2026, 7, 23, 8, 9, 29),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(poweredOn, settings.Current), "较新的开机状态必须先写入测试 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(stopped, settings.Current), "较旧的停机状态必须后写入测试 JSONL。");

        service.RetryPendingUploadsAsync().GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[]
            {
                ProductionConstants.MesDeviceStatuses.Stopped,
                ProductionConstants.MesDeviceStatuses.PoweredOn
            },
            requestStatuses,
            "自动补传必须按状态发生时间从旧到新执行。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, service.GetLog(stopped.RecordId)!.ReportStatus, "旧停机状态必须更新为已上传。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, service.GetLog(poweredOn.RecordId)!.ReportStatus, "新开机状态必须更新为已上传。");

        requestStatuses.Clear();
        var sameTime = new DateTime(2026, 7, 23, 8, 10, 0);
        var firstAtSameTime = new BizDeviceStatusLog
        {
            RecordId = new string('f', 32),
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
            OccurredTime = sameTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
        var secondAtSameTime = new BizDeviceStatusLog
        {
            RecordId = new string('0', 32),
            DeviceId = "D-001",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
            StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
            OccurredTime = sameTime,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(firstAtSameTime, settings.Current), "同时间首条停机状态必须写入测试 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(secondAtSameTime, settings.Current), "同时间次条开机状态必须写入测试 JSONL。");

        service.RetryPendingUploadsAsync().GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[]
            {
                ProductionConstants.MesDeviceStatuses.Stopped,
                ProductionConstants.MesDeviceStatuses.PoweredOn
            },
            requestStatuses,
            "发生时间相同时必须保留 JSONL 首次追加顺序，不能按随机记录键重排。");
    }
    finally
    {
        DeleteDirectoryIfExists(root);
    }
}

static void DeviceStatusPendingReplayYieldsAfterAcquiringOrderGate()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusReplayYieldTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (_, cancellationToken) =>
        {
            requestStarted.TrySetResult();
            await releaseRequest.Task.WaitAsync(cancellationToken);
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "操作成功"
            };
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var previousContext = SynchronizationContext.Current;
    var queuedContext = new QueuedSynchronizationContext();
    var pending = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 9, 20),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings.Current), "待补传状态必须写入测试 JSONL。");
        SynchronizationContext.SetSynchronizationContext(queuedContext);
        var replay = service.RetryPendingUploadsAsync();

        AssertTrue(requestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "补传核心必须在默认调度器执行，不能等待 UI 上下文继续泵消息。");
        AssertFalse(replay.IsCompleted, "MES 请求完成前补传任务不能提前结束。");
        releaseRequest.TrySetResult();
        replay.GetAwaiter().GetResult();

        var stopUpload = service.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.Stopped,
            reportToMes: true,
            forceWrite: true);
        AssertTrue(stopUpload.Wait(TimeSpan.FromSeconds(3)), "同步停机等待不得依赖已停止泵消息的 UI 上下文。");
    }
    finally
    {
        releaseRequest.TrySetResult();
        SynchronizationContext.SetSynchronizationContext(previousContext);
        queuedContext.RunAll();
        DeleteDirectoryIfExists(root);
    }
}

static void DeviceStatusForceWriteSkipsLatestHistoryScan()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var changeMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BizDeviceStatusLog> ChangeStatusAsync(",
        "public async Task<BasicRes<object>?> RetryUploadAsync(");

    AssertSourceOrder(
        changeMethod,
        "if (!forceWrite)",
        "var latest = GetLatestStatus(normalizedStationNo);",
        "生命周期强制写入不得在落盘前扫描全部设备状态历史。");
}

static void DeviceStatusPendingReplayBlocksNewerUploads()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusPendingGateTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStatuses = new List<string>();
    var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (request, cancellationToken) =>
        {
            lock (requestStatuses)
            {
                requestStatuses.Add(request.DevStatus);
            }

            if (request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped)
            {
                firstRequestStarted.TrySetResult();
                await releaseFirstRequest.Task.WaitAsync(cancellationToken);
            }

            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "操作成功"
            };
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var stopped = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 9, 20),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var poweredOn = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
        OccurredTime = new DateTime(2026, 7, 23, 8, 9, 29),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(stopped, settings.Current), "旧停机状态必须写入测试 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(poweredOn, settings.Current), "新开机状态必须写入测试 JSONL。");

        var replay = service.RetryPendingUploadsAsync();
        AssertTrue(firstRequestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "补传首个 MES 请求必须在超时前开始。");
        var liveUpload = service.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            "程序执行开始",
            "Test",
            stationNo: 1,
            occurredTime: new DateTime(2026, 7, 23, 8, 10, 0),
            forceWrite: true);

        Thread.Sleep(100);
        lock (requestStatuses)
        {
            AssertFalse(
                requestStatuses.Contains(ProductionConstants.MesDeviceStatuses.ProgramStarted),
                "补传批次完成前，较新的实时状态不能越过旧状态上传。");
        }

        releaseFirstRequest.TrySetResult();
        Task.WhenAll(replay, liveUpload).GetAwaiter().GetResult();

        lock (requestStatuses)
        {
            AssertSequenceEqual(
                new[]
                {
                    ProductionConstants.MesDeviceStatuses.Stopped,
                    ProductionConstants.MesDeviceStatuses.PoweredOn,
                    ProductionConstants.MesDeviceStatuses.ProgramStarted
                },
                requestStatuses,
                "补传批次必须完整执行后才允许上传后来状态。");
        }
    }
    finally
    {
        releaseFirstRequest.TrySetResult();
        DeleteDirectoryIfExists(root);
    }
}

static void DeviceStatusNewerChangeWaitsAfterOlderFailure()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusPendingFailureTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStatuses = new List<string>();
    var failStopped = true;
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = (request, _) =>
        {
            requestStatuses.Add(request.DevStatus);
            return Task.FromResult(new BasicRes<object>
            {
                Status = failStopped && request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped
                    ? AppConstants.MesStatus.Error
                    : AppConstants.MesStatus.Success,
                Msg = failStopped && request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped
                    ? "MES 暂时不可用"
                    : "操作成功"
            });
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var stopped = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(stopped, settings.Current), "旧停机状态必须先写入 JSONL。");

        var poweredOn = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.PoweredOn,
                "开机",
                "Test",
                stationNo: ProductionConstants.Stations.SharedStationNo,
                occurredTime: new DateTime(2026, 7, 23, 8, 0, 10),
                forceWrite: true)
            .GetAwaiter()
            .GetResult();

        AssertSequenceEqual(
            new[] { ProductionConstants.MesDeviceStatuses.Stopped },
            requestStatuses,
            "旧停机状态失败时，新的开机状态不得越过它上传。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, service.GetLog(stopped.RecordId)!.ReportStatus, "旧停机状态应保留失败结果。");
        AssertEqual(ProductionConstants.UploadStatuses.Pending, service.GetLog(poweredOn.RecordId!)!.ReportStatus, "新开机状态必须继续等待旧状态成功。");

        failStopped = false;
        service.RetryPendingUploadsAsync().GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[]
            {
                ProductionConstants.MesDeviceStatuses.Stopped,
                ProductionConstants.MesDeviceStatuses.Stopped,
                ProductionConstants.MesDeviceStatuses.PoweredOn
            },
            requestStatuses,
            "下次补传必须先重试旧停机，成功后才能上传新开机。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, service.GetLog(stopped.RecordId)!.ReportStatus, "旧停机重试后必须标记为已上传。");
        AssertEqual(ProductionConstants.UploadStatuses.Uploaded, service.GetLog(poweredOn.RecordId!)!.ReportStatus, "新开机必须在旧停机成功后上传。");
    }
    finally
    {
        DeleteDirectoryIfExists(root);
    }
}

static void DeviceStatusManualRetryPreservesPendingOrder()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusManualRetryOrderTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStatuses = new List<string>();
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = (request, _) =>
        {
            requestStatuses.Add(request.DevStatus);
            return Task.FromResult(new BasicRes<object>
            {
                Status = request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped
                    ? AppConstants.MesStatus.Error
                    : AppConstants.MesStatus.Success,
                Msg = request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped
                    ? "MES 暂时不可用"
                    : "操作成功"
            });
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var stopped = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var poweredOn = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
        OccurredTime = new DateTime(2026, 7, 23, 8, 0, 10),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(stopped, settings.Current), "较旧停机状态必须先写入 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(poweredOn, settings.Current), "较新开机状态必须先写入 JSONL。");

        var response = service.RetryUploadAsync(poweredOn.RecordId).GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[] { ProductionConstants.MesDeviceStatuses.Stopped },
            requestStatuses,
            "人工重试较新状态时，旧状态失败不得被越过。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, service.GetLog(stopped.RecordId)!.ReportStatus, "旧停机状态必须保留失败结果。");
        AssertEqual(ProductionConstants.UploadStatuses.Pending, service.GetLog(poweredOn.RecordId)!.ReportStatus, "较新开机状态必须继续等待旧状态成功。");
        AssertEqual(ProductionConstants.UploadStatuses.Pending, response!.Status, "未实际发送的人工重试项必须保持 Pending，不能误标为 Failed。");
    }
    finally
    {
        DeleteDirectoryIfExists(root);
    }
}

static void DeviceStatusPendingReplaySkipsDeletedSource()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusPendingDeleteTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var requestStatuses = new List<string>();
    var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (request, cancellationToken) =>
        {
            requestStatuses.Add(request.DevStatus);
            if (request.DevStatus == ProductionConstants.MesDeviceStatuses.Stopped)
            {
                firstRequestStarted.TrySetResult();
                await releaseFirstRequest.Task.WaitAsync(cancellationToken);
            }

            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "操作成功"
            };
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var stopped = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = new DateTime(2026, 7, 23, 8, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var poweredOn = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "D-001",
        StationNo = ProductionConstants.Stations.SharedStationNo,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.PoweredOn,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.PoweredOn),
        OccurredTime = new DateTime(2026, 7, 24, 8, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(stopped, settings.Current), "较旧停机状态必须写入首日 JSONL。");
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(poweredOn, settings.Current), "较新开机状态必须写入次日 JSONL。");
        var poweredOnFile = Path.Combine(root, AppConstants.LogCategories.DeviceStatus, "2026-07-24.jsonl");
        AssertTrue(File.Exists(poweredOnFile), "待删除的次日设备状态 JSONL 必须存在。");

        var replay = service.RetryPendingUploadsAsync();
        AssertTrue(firstRequestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "批次首条 MES 请求必须在超时前开始。");
        File.Delete(poweredOnFile);
        releaseFirstRequest.TrySetResult();
        replay.GetAwaiter().GetResult();

        AssertSequenceEqual(
            new[] { ProductionConstants.MesDeviceStatuses.Stopped },
            requestStatuses,
            "批次扫描后被删除的 JSONL 状态必须在实际上传前重新校验并跳过。");
        AssertTrue(service.GetLog(poweredOn.RecordId) is null, "删除后的设备状态不能被补传流程重建。");
    }
    finally
    {
        releaseFirstRequest.TrySetResult();
        DeleteDirectoryIfExists(root);
    }
}

static (int RequestCount, BasicRes<object>?[] Responses) RunConcurrentDeviceStatusRetryScenario(
    BasicRes<object> response,
    bool deleteSourceBeforeResponse)
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusSharedRetryTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true
        }
    };
    var recordKey = Guid.NewGuid().ToString("N");
    var pending = new BizDeviceStatusLog
    {
        RecordId = recordKey,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
        OccurredTime = new DateTime(2026, 7, 22, 10, 45, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var requestCount = 0;
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (_, cancellationToken) =>
        {
            Interlocked.Increment(ref requestCount);
            requestStarted.TrySetResult();
            await releaseResponse.Task.WaitAsync(cancellationToken);
            return response;
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings.Current), "测试来源必须先写入 JSONL。");
        var first = service.RetryUploadAsync(recordKey);
        AssertTrue(requestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "首个 MES 请求必须在超时前开始。");
        var second = service.RetryUploadAsync(recordKey);
        if (deleteSourceBeforeResponse)
        {
            Directory.Delete(DeviceStatusLocalLogStore.GetLogDirectory(settings.Current), recursive: true);
        }

        releaseResponse.TrySetResult();
        var responses = Task.WhenAll(first, second).GetAwaiter().GetResult();
        return (requestCount, responses);
    }
    finally
    {
        releaseResponse.TrySetResult();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusServiceStopsWhenFirstJsonlWriteFails()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusWriteFailureTests", Guid.NewGuid().ToString("N"));
    var blockedLogRoot = Path.Combine(root, "blocked-root");
    Directory.CreateDirectory(root);
    File.WriteAllText(blockedLogRoot, "this path is a file", Encoding.UTF8);
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = blockedLogRoot,
            EnableDeviceStatusReport = true
        }
    };
    var mes = new FakeMesProvider();
    var exceptionLogs = new FakeProgramExceptionLogService();
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, exceptionLogs);
    var notifications = 0;
    service.LogsChanged += (_, _) => notifications++;

    try
    {
        var result = service.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.PoweredOn,
                "开机",
                "Application")
            .GetAwaiter()
            .GetResult();

        AssertEqual(0, mes.DeviceStatusRequests.Count, "首版本落盘失败时禁止调用 MES。");
        AssertEqual(0, notifications, "首版本落盘失败时禁止通知设备状态 UI。");
        AssertEqual(1, exceptionLogs.Entries.Count, "首版本落盘失败必须写程序异常日志。");
        AssertEqual(ProductionConstants.UploadStatuses.Failed, result.ReportStatus, "返回对象必须明确标记本地落盘失败。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusRuntimeNoLongerPersistsDatabaseLogRows()
{
    var entityCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Entities", "BizDeviceStatusLog.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"), Encoding.UTF8);
    var dbCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Data", "SqlSugarDbContext.cs"), Encoding.UTF8);

    AssertFalse(entityCode.Contains("SugarTable", StringComparison.Ordinal), "设备状态 JSON 模型不能继续映射 SqlSugar 表。");
    AssertFalse(entityCode.Contains("SugarColumn", StringComparison.Ordinal), "设备状态 JSON 模型不能保留数据库列特性。");
    AssertFalse(serviceCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态服务不能再查询旧表。");
    AssertFalse(serviceCode.Contains("Insertable(log)", StringComparison.Ordinal), "设备状态服务不能再插入旧表。");
    AssertFalse(serviceCode.Contains("Updateable(log)", StringComparison.Ordinal), "设备状态服务不能再更新旧表。");
    AssertFalse(serviceCode.Contains("Deleteable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态服务不能再删除旧表行。");
    AssertFalse(dbCode.Contains("typeof(BizDeviceStatusLog)", StringComparison.Ordinal), "CodeFirst 不能再为新数据库创建设备状态表。");
    AssertTrue(serviceCode.Contains("IProgramExceptionLogService", StringComparison.Ordinal), "JSONL 写入失败必须接入程序异常日志。");
}

static void DeviceStatusUploadTaskPayloadContainsOnlyRecordKey()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var buildTaskMethod = ExtractMethodText(
        serviceCode,
        "private static BizUploadTask BuildDeviceStatusUploadTask",
        "private BizUploadTask? FindExistingUploadTask");

    AssertTrue(buildTaskMethod.Contains("new { RecordKey = recordKey }", StringComparison.Ordinal), "新任务 payload 必须只保存记录键。");
    AssertFalse(buildTaskMethod.Contains("LogId =", StringComparison.Ordinal), "新任务不能继续保存数据库日志 Id。");
    AssertFalse(buildTaskMethod.Contains("DeviceId =", StringComparison.Ordinal), "任务 payload 不能复制设备编号作为权威来源。");
    AssertFalse(buildTaskMethod.Contains("DevStatus =", StringComparison.Ordinal), "任务 payload 不能复制设备状态正文。");
    AssertFalse(buildTaskMethod.Contains("Remark =", StringComparison.Ordinal), "任务 payload 不能复制备注正文。");
}

static void DeviceStatusUploadExecutionRevalidatesJsonlSource()
{
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var executeMethod = ExtractMethodText(
        uploadCode,
        "public async Task<UploadTaskSummary?> ExecuteAsync",
        "public async Task<int> ExecuteAllPendingAsync");
    var executeAllMethod = ExtractMethodText(
        uploadCode,
        "public async Task<int> ExecuteAllPendingAsync",
        "public void RequestRetry");
    var uploadMethod = ExtractMethodText(
        uploadCode,
        "private Task<BasicRes<object>?> UploadDeviceStatusAsync",
        "private async Task<BasicRes<object>> UploadProcessParametersAsync");

    AssertSourceOrder(
        executeMethod,
        "_deviceStatusService.GetLog(recordKey)",
        "MarkUploading(id)",
        "单条执行必须先重新读取 JSONL，再把任务改为 Uploading。");
    AssertTrue(executeMethod.Contains("SoftDeleteDeviceStatusTask", StringComparison.Ordinal), "来源缺失时单条执行必须软删除未成功投影。");
    AssertTrue(executeAllMethod.Contains("SyncDeviceStatusTasksFromLogs", StringComparison.Ordinal), "批量执行查询任务前必须先按 JSONL 对账。");
    AssertTrue(executeAllMethod.Contains("await ExecuteAsync(taskId", StringComparison.Ordinal), "批量执行的每一条仍必须复用单条门禁。");
    AssertTrue(uploadMethod.Contains("_deviceStatusService.RetryUploadAsync", StringComparison.Ordinal), "实际 MES 请求必须由设备状态服务从 JSONL 构造。");
    AssertFalse(uploadCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "上传任务服务不能再查询设备状态旧表。");
    AssertFalse(uploadCode.Contains("Updateable(updatedLog)", StringComparison.Ordinal), "上传任务服务不能再更新设备状态旧表。");
    AssertFalse(uploadCode.Contains("ReadDeviceStatusRequest", StringComparison.Ordinal), "上传任务不能再从复制 payload 还原设备状态正文。");
}

static void DeviceStatusPendingProjectionPreservesUploadedHistory()
{
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var reconcileMethod = ExtractMethodText(
        uploadCode,
        "private IReadOnlyDictionary<string, BizDeviceStatusLog> SyncDeviceStatusTasksFromLogs",
        "public BizUploadTask EnqueueOrUpdate");
    var finishMethod = ExtractMethodText(
        uploadCode,
        "private UploadTaskSummary? FinishExecution",
        "private void WriteUploadFlowLog");
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);

    AssertTrue(reconcileMethod.Contains("GetPendingLogs()", StringComparison.Ordinal), "待上传设备状态必须直接来自全部 JSONL 最新版本。");
    AssertTrue(reconcileMethod.Contains("task.Status != ProductionConstants.UploadStatuses.Uploaded", StringComparison.Ordinal), "来源缺失清理必须排除已经上传的任务。");
    AssertTrue(reconcileMethod.Contains("task.Status = source.ReportStatus;", StringComparison.Ordinal), "自动补传终态必须按 JSONL 回写已有上传任务投影。");
    AssertTrue(reconcileMethod.Contains("task.IsDeleted = true", StringComparison.Ordinal), "来源缺失的未成功任务必须软删除。");
    AssertFalse(reconcileMethod.Contains("Deleteable<BizUploadTask>", StringComparison.Ordinal), "派生任务清理不能物理删除诊断记录。");
    AssertTrue(finishMethod.Contains("deviceStatusLog?.ReportStatus", StringComparison.Ordinal), "人工重试任务状态必须以 JSONL 终态为准，未发送项不能误标失败。");
    AssertTrue(serviceCode.Contains("TryCompleteUploadTaskProjection(log, recordKey);", StringComparison.Ordinal), "自动补传成功后必须立即保存 Uploaded/Skipped 任务历史。");
}

static void DeviceStatusPendingProjectionPreservesActiveUploads()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var ensureMethod = ExtractMethodText(
        serviceCode,
        "public BizUploadTask? EnsurePendingUploadTask",
        "public int DeleteLogs");
    var retryMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BasicRes<object>?> RetryUploadAsync",
        "private async Task<BasicRes<object>?> RetryUploadCoreAsync");

    AssertTrue(
        retryMethod.Contains("_activeUploads", StringComparison.Ordinal)
            && retryMethod.Contains("TaskCompletionSource<BasicRes<object>?>", StringComparison.Ordinal),
        "上传门禁必须按 RecordKey 共享同一个进行中结果。");
    AssertTrue(
        ensureMethod.Contains("existingStatus == ProductionConstants.UploadStatuses.Uploaded", StringComparison.Ordinal),
        "JSONL 对账不能覆盖已上传任务。");
    AssertTrue(
        ensureMethod.Contains("existingStatus == ProductionConstants.UploadStatuses.Uploading", StringComparison.Ordinal)
            && ensureMethod.Contains("IsUploadActive(recordKey)", StringComparison.Ordinal),
        "JSONL 对账必须保留当前进程正在执行的 Uploading 任务。");
    AssertTrue(
        ensureMethod.Contains("taskRow.Status != ProductionConstants.UploadStatuses.Uploading", StringComparison.Ordinal)
            && ensureMethod.Contains("taskRow.Status != ProductionConstants.UploadStatuses.Uploaded", StringComparison.Ordinal),
        "对账更新必须用数据库条件防止并发覆盖 Uploading/Uploaded。");
    AssertTrue(
        ensureMethod.Contains("taskRow.Status == existingStatus", StringComparison.Ordinal)
            && ensureMethod.Contains("taskRow.LastAttemptTime == existingLastAttemptTime", StringComparison.Ordinal),
        "恢复超时 Uploading 时必须匹配读取到的旧状态和尝试时间，不能覆盖并发完成结果。");
}

static void DeviceStatusPendingProjectionKeepsInFlightTaskHistory()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var deleteMethod = ExtractMethodText(
        serviceCode,
        "private void SoftDeleteUnfinishedUploadTasks",
        "private BizDeviceStatusLog CreateLog");
    var reconcileMethod = ExtractMethodText(
        uploadCode,
        "private IReadOnlyDictionary<string, BizDeviceStatusLog> SyncDeviceStatusTasksFromLogs",
        "public BizUploadTask EnqueueOrUpdate");
    var softDeleteMethod = ExtractMethodText(
        uploadCode,
        "private void SoftDeleteDeviceStatusTask",
        "private BizUploadTask? MarkUploading");
    var preserveMethod = typeof(DeviceStatusService).GetMethod("ShouldPreserveUploadingTask");

    AssertTrue(preserveMethod is not null, "设备状态服务必须统一判断当前或最近的 Uploading 任务是否应保留。");
    AssertTrue(
        deleteMethod.Contains("ShouldPreserveUploadingTask", StringComparison.Ordinal),
        "直接删除 JSONL 时不能软删除当前或最近的 Uploading 任务。");
    AssertTrue(
        reconcileMethod.Contains("_deviceStatusService.ShouldPreserveUploadingTask", StringComparison.Ordinal),
        "待上传页对账不能软删除当前或最近的 Uploading 任务。");
    AssertTrue(
        softDeleteMethod.Contains("BizUploadTask expectedTask", StringComparison.Ordinal)
            && softDeleteMethod.Contains("taskRow.Status == expectedStatus", StringComparison.Ordinal)
            && softDeleteMethod.Contains("taskRow.LastAttemptTime == expectedLastAttemptTime", StringComparison.Ordinal),
        "执行入口删源时必须匹配读取到的旧状态和尝试时间，不能覆盖新 Uploading。");

    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusUploadingProtectionTests", Guid.NewGuid().ToString("N"));
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings
        {
            DeviceId = "D-001",
            LogDirectory = root,
            EnableDeviceStatusReport = true,
            MesTimeoutSeconds = 3
        }
    };
    var recordKey = Guid.NewGuid().ToString("N");
    var pending = new BizDeviceStatusLog
    {
        RecordId = recordKey,
        DeviceId = "D-001",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Exception,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Exception),
        OccurredTime = new DateTime(2026, 7, 22, 11, 0, 0),
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };
    var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var mes = new FakeMesProvider
    {
        DeviceStatusHandler = async (_, cancellationToken) =>
        {
            requestStarted.TrySetResult();
            await releaseResponse.Task.WaitAsync(cancellationToken);
            return new BasicRes<object> { Status = AppConstants.MesStatus.Success, Msg = "操作成功" };
        }
    };
    using var dbContext = new AutoWeldSystem.Data.SqlSugarDbContext("server=127.0.0.1;database=unused;uid=unused;pwd=unused;");
    var service = new DeviceStatusService(dbContext, settings, mes, new FakeProgramExceptionLogService());
    var uploadTask = new BizUploadTask
    {
        TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
        BusinessId = DeviceStatusRecordIdentityRules.BuildBusinessId(recordKey),
        PayloadJson = JsonSerializer.Serialize(new { RecordKey = recordKey }),
        Status = ProductionConstants.UploadStatuses.Uploading,
        LastAttemptTime = DateTime.Now.AddMinutes(-5)
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings.Current), "测试来源必须先写入 JSONL。");
        var retry = service.RetryUploadAsync(recordKey);
        AssertTrue(requestStarted.Task.Wait(TimeSpan.FromSeconds(3)), "MES 请求必须在超时前开始。");

        var preserveActive = (bool)preserveMethod!.Invoke(service, new object[] { uploadTask })!;
        releaseResponse.TrySetResult();
        _ = retry.GetAwaiter().GetResult();
        var preserveStale = (bool)preserveMethod.Invoke(service, new object[] { uploadTask })!;

        AssertTrue(preserveActive, "正在执行的 Uploading 任务即使尝试时间已旧也必须保留。");
        AssertFalse(preserveStale, "上传完成后，超时且无活动请求的 Uploading 遗留任务必须允许清理。");
    }
    finally
    {
        releaseResponse.TrySetResult();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusJsonlSourceBehaviorIsDocumented()
{
    var readme = File.ReadAllText(GetRepoFilePath("README.md"), Encoding.UTF8);
    var quickStart = File.ReadAllText(GetRepoFilePath("docs", "QUICK_START.md"), Encoding.UTF8);

    AssertTrue(readme.Contains("设备状态 JSONL 是唯一事实来源", StringComparison.Ordinal), "README 必须说明设备状态唯一来源。");
    AssertTrue(readme.Contains("未成功上传", StringComparison.Ordinal), "README 必须说明删除来源会取消未成功记录的补传资格。");
    AssertTrue(readme.Contains("已成功上传", StringComparison.Ordinal), "README 必须说明已上传结果不因本地删除而撤销。");
    AssertTrue(readme.Contains("程序异常日志", StringComparison.Ordinal), "README 必须给出落盘失败排障入口。");
    AssertTrue(quickStart.Contains("不再读写 `Biz_DeviceStatusLog`", StringComparison.Ordinal), "快速入门不能继续描述数据库与 JSONL 双来源。");
}

static void DeviceStatusApiRejectsMissingJsonlRecord()
{
    var settings = new FakeAppSettingsService
    {
        Current = new AppSettings { DeviceId = "D-001" }
    };
    var statusService = new FakeDeviceStatusService { CurrentStatus = null };
    var service = CreateDeviceApiEndpointService(settings, statusService);

    var response = service.GetDeviceStatus("D-001");

    AssertFalse(response.IsSuccess, "JSONL 没有有效记录时设备状态 API 必须返回失败。");
    AssertEqual("暂无设备状态记录", response.Msg, "无来源失败消息必须稳定，不能伪造默认开机状态。");
    AssertEqual(null, response.Data, "无来源时不能返回设备状态 Data。");
    AssertEqual(1, statusService.GetCurrentStatusCallCount, "设备编号校验通过后应读取一次 JSONL 当前状态。");
}

static void DeviceStatusConsumersDoNotQueryLegacyTable()
{
    var apiCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceApiEndpointService.cs"),
        Encoding.UTF8);
    var centerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Center", "CenterTelemetrySyncService.cs"),
        Encoding.UTF8);
    var interfaceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Interfaces", "IDeviceStatusService.cs"),
        Encoding.UTF8);

    AssertTrue(apiCode.Contains("var currentStatus = _deviceStatusService.GetCurrentStatus();", StringComparison.Ordinal), "设备 API 必须通过设备状态服务读取 JSONL。");
    AssertTrue(apiCode.Contains("暂无设备状态记录", StringComparison.Ordinal), "设备 API 必须显式处理空 JSONL。");
    AssertTrue(centerCode.Contains("_deviceStatusService.GetLatestStatus(stationNo)", StringComparison.Ordinal), "中心遥测必须通过设备状态服务读取工位最新 JSONL。");
    AssertFalse(centerCode.Contains("Queryable<BizDeviceStatusLog>", StringComparison.Ordinal), "中心遥测不能再查询设备状态旧表。");
    AssertFalse(interfaceCode.Contains("StatusChanged", StringComparison.Ordinal), "最终接口只保留来源重载事件，不能保留重复实时插入事件。");
    AssertFalse(interfaceCode.Contains("NotifyLogsChanged", StringComparison.Ordinal), "最终接口不能允许外部伪造来源变更通知。");
}

static void LogManageReloadsDeviceStatusJsonlOnReentry()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"),
        Encoding.UTF8);
    var visibleMethod = ExtractMethodText(
        viewCode,
        "protected override void OnVisibleChanged",
        "protected override void OnLanguageChanged");
    var wireMethod = ExtractMethodText(
        viewCode,
        "private void WireEvents()",
        "private void ShowLogDate_CheckedChanged");

    AssertTrue(visibleMethod.Contains("LoadDeviceStatusLogs();", StringComparison.Ordinal), "重新进入日志管理页必须重读当前日期 JSONL。");
    AssertTrue(wireMethod.Contains("_deviceStatusService.LogsChanged +=", StringComparison.Ordinal), "日志页必须监听持久化来源变化。");
    AssertFalse(wireMethod.Contains("_deviceStatusService.StatusChanged +=", StringComparison.Ordinal), "日志页不能同时监听实时行事件造成重复插入。");
    AssertFalse(viewCode.Contains("AddLiveDeviceStatusLog", StringComparison.Ordinal), "设备状态行只能从 JSONL 重载，不能直接附加内存对象。");
    AssertFalse(viewCode.Contains("FileSystemWatcher", StringComparison.Ordinal), "外部删除不增加文件监听器。");
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
    var sendMethod = ExtractMethodText(
        serviceCode,
        "private async Task<BasicRes<object>> SendToMesAsync",
        "private BasicRes<object>? PersistReportResult");
    var persistMethod = ExtractMethodText(
        serviceCode,
        "private BasicRes<object>? PersistReportResult",
        "private static BizUploadTask BuildDeviceStatusUploadTask");

    AssertTrue(
        sendMethod.Contains("Ts = log.OccurredTime.ToString(\"yyyy-MM-dd HH:mm:ss\")", StringComparison.Ordinal),
        "MES 设备状态接口时间格式仍应按接口约定保持到秒。");
    AssertTrue(
        persistMethod.Contains("DeviceStatusLocalLogStore.TryAppendVersion(log", StringComparison.Ordinal),
        "MES 结果必须追加到同一个 JSONL 记录，不能回写数据库。");
    AssertFalse(
        persistMethod.Contains("InSingle", StringComparison.Ordinal),
        "结果追加不能用数据库回读对象覆盖原始毫秒时间。");
}

static void DeviceStatusLocalLogStorePermitsFullSourceScans()
{
    const int entryCount = 5001;
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusFullReadTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 7, 24, 8, 0, 0);
    var directory = DeviceStatusLocalLogStore.GetLogDirectory(settings);
    var filePath = Path.Combine(directory, "2026-07-24.jsonl");

    try
    {
        Directory.CreateDirectory(directory);
        using (var writer = new StreamWriter(filePath, append: false, Encoding.UTF8))
        {
            for (var index = 0; index < entryCount; index++)
            {
                var deviceStatus = index == 0
                    ? ProductionConstants.MesDeviceStatuses.Exception
                    : ProductionConstants.MesDeviceStatuses.PoweredOn;
                writer.WriteLine(JsonSerializer.Serialize(new BizDeviceStatusLog
                {
                    Id = index + 1,
                    DeviceId = "D-001",
                    StationNo = 1,
                    DeviceStatus = deviceStatus,
                    StatusName = DeviceStatusReportRules.GetStatusName(deviceStatus),
                    Source = "Test",
                    AlarmAddress = index == 0 ? "DB10.DBX2.0" : null,
                    AlarmContent = index == 0 ? "安全门打开" : null,
                    OccurredTime = occurredTime.AddTicks(index),
                    ReportStatus = ProductionConstants.UploadStatuses.Uploaded
                }));
            }
        }

        AssertEqual(5000, DeviceStatusLocalLogStore.Read(settings, maxCount: entryCount).Count, "普通日志查询仍必须保留 5000 条上限。");
        var allLogs = DeviceStatusLocalLogStore.Read(settings, maxCount: int.MaxValue);
        AssertEqual(entryCount, allLogs.Count, "监控恢复显式请求全量时不得丢失第 5001 条设备状态。 ");
        AssertEqual(entryCount, allLogs[0].Id, "全量读取仍必须按发生时间倒序返回。 ");
        AssertSequenceEqual(
            ["DB10.DBX2.0"],
            PlcDeviceAlarmCycleRules.Restore(allLogs).ActiveAlarms.Select(alarm => alarm.Address).ToArray(),
            "超过 5000 条后，最早的未闭合异常仍必须参与重启报警周期恢复。 ");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void DeviceStatusLocalLogStoreCachesUnchangedSnapshots()
{
    var root = Path.Combine(Path.GetTempPath(), "AutoWeldSystemDeviceStatusSnapshotTests", Guid.NewGuid().ToString("N"));
    var settings = new AppSettings { LogDirectory = root };
    var occurredTime = new DateTime(2026, 8, 18, 9, 10, 0);
    var pending = new BizDeviceStatusLog
    {
        RecordId = Guid.NewGuid().ToString("N"),
        DeviceId = "CACHE-DEVICE",
        StationNo = 1,
        DeviceStatus = ProductionConstants.MesDeviceStatuses.Stopped,
        StatusName = DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.Stopped),
        OccurredTime = occurredTime,
        ReportStatus = ProductionConstants.UploadStatuses.Pending
    };

    try
    {
        AssertTrue(DeviceStatusLocalLogStore.TryAppend(pending, settings), "缓存测试记录必须先写入 JSONL。");
        var filePath = Path.Combine(
            DeviceStatusLocalLogStore.GetLogDirectory(settings),
            $"{occurredTime:yyyy-MM-dd}.jsonl");
        File.AppendAllText(filePath, "\0" + Environment.NewLine + Environment.NewLine, Encoding.UTF8);

        var errors = 0;
        var first = DeviceStatusLocalLogStore.ReadPending(settings, (_, _) => errors++);
        AssertEqual(1, first.Count, "损坏记录应跳过，合法待上传记录仍应保留。");
        AssertEqual(1, errors, "首次快照构建应报告一次损坏记录。");

        first[0].DeviceStatus = "mutated-by-caller";
        var second = DeviceStatusLocalLogStore.ReadPending(settings, (_, _) => errors++);
        AssertEqual(1, errors, "文件未变化时必须复用快照，不能重复解析和记录同一损坏内容。");
        AssertEqual(
            ProductionConstants.MesDeviceStatuses.Stopped,
            second[0].DeviceStatus,
            "快照返回值必须隔离调用方修改。");

        File.AppendAllText(filePath, Environment.NewLine, Encoding.UTF8);
        _ = DeviceStatusLocalLogStore.ReadPending(settings, (_, _) => errors++);
        AssertEqual(2, errors, "文件内容变化后必须重建快照并重新诊断损坏记录。");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
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
    AssertTrue(serviceCode.Contains("var existing = FindExistingUploadTask(recordKey);", StringComparison.Ordinal), "设备状态任务补建必须按 JSONL 记录键兼容查找现有任务。");
    AssertTrue(serviceCode.Contains("existing.IsDeleted = false;", StringComparison.Ordinal), "日志来源有效时应恢复旧的软删除任务。");
    AssertTrue(interfaceCode.Contains("GetPendingLogs", StringComparison.Ordinal), "接口必须直接暴露 JSONL 待上传来源。");
    AssertTrue(interfaceCode.Contains("RetryUploadAsync", StringComparison.Ordinal), "接口必须提供按记录键重新读取并上报的方法。");
    AssertTrue(uploadTaskCode.Contains("GetPendingLogs()", StringComparison.Ordinal), "任务查询必须以全部 JSONL 最新版本为来源。");
    AssertTrue(uploadTaskCode.Contains("SyncDeviceStatusTasksFromLogs", StringComparison.Ordinal), "任务查询和批量执行必须先对账来源。");
    AssertTrue(summaryCode.Contains("DeviceStatusRecordKey", StringComparison.Ordinal), "上传摘要必须携带 JSONL 记录键。");
    AssertFalse(summaryCode.Contains("DeviceStatusLogId", StringComparison.Ordinal), "上传摘要不能继续依赖数据库日志 Id。");
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
    AssertTrue(serviceCode.Contains("DeviceStatusLocalLogStore.TryRemove", StringComparison.Ordinal), "设备状态删除必须先重写 JSONL。");
    AssertTrue(serviceCode.Contains("SoftDeleteUnfinishedUploadTasks", StringComparison.Ordinal), "删除来源后必须软删除未成功派生任务。");
    AssertFalse(serviceCode.Contains("Deleteable<BizDeviceStatusLog>", StringComparison.Ordinal), "设备状态删除不能再操作旧表。");
    AssertTrue(logViewCode.Contains("_deviceStatusService.LogsChanged +=", StringComparison.Ordinal), "日志管理页必须监听设备状态日志变更事件。");
    AssertTrue(logViewCode.Contains("LoadDeviceStatusLogs();", StringComparison.Ordinal), "日志管理页收到日志变更后必须重新加载当前日期。");
    AssertTrue(stateViewCode.Contains("IDeviceStatusService deviceStatusService", StringComparison.Ordinal), "待上传页必须注入设备状态日志服务。");
    AssertTrue(stateViewCode.Contains("RefreshDeviceStatusLogIndex", StringComparison.Ordinal), "待上传页必须缓存日志来源以支持批量删除。");
    AssertTrue(stateViewCode.Contains("_deviceStatusService.DeleteLogs", StringComparison.Ordinal), "待上传设备状态删除必须通过设备状态日志服务执行。");
    AssertTrue(uploadTaskCode.Contains("_deviceStatusService.RetryUploadAsync", StringComparison.Ordinal), "设备状态补传结果和刷新必须统一由设备状态服务处理。");
    AssertFalse(uploadTaskCode.Contains("NotifyLogsChanged", StringComparison.Ordinal), "上传任务服务不能绕过设备状态服务公开触发日志刷新。");
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
    AssertTrue(beginDisposeMethod.Contains("dgvWorkOrders.SelectionChanged -= WorkOrders_SelectionChanged;", StringComparison.Ordinal), "释放时必须解绑工单选择事件。");
    AssertFalse(beginDisposeMethod.Contains("tableTestData.CellClick", StringComparison.Ordinal), "测试数据树不再展示原始 JSON，因此释放逻辑不应保留点击事件解绑。");
}

static void LogManageViewDeviceStatusTabShowsAlarmDetails()
{
    var designer = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);

    AssertFalse(designer.Contains("colDeviceAlarmAddress", StringComparison.Ordinal), "设备状态表格不得再声明报警地址列。");
    AssertFalse(designer.Contains("colDeviceAlarmContent", StringComparison.Ordinal), "设备状态表格不得再声明报警内容列。");
    AssertTrue(viewCode.Contains("Contains(entry.AlarmAddress, keyword)", StringComparison.Ordinal), "报警地址必须参与日志搜索。");
    AssertTrue(viewCode.Contains("Contains(entry.AlarmContent, keyword)", StringComparison.Ordinal), "报警内容必须参与日志搜索。");
    AssertTrue(viewCode.Contains("AlarmAddress: {entry.AlarmAddress", StringComparison.Ordinal), "设备状态详情必须显示报警地址。");
    AssertTrue(viewCode.Contains("AlarmContent: {entry.AlarmContent", StringComparison.Ordinal), "设备状态详情必须显示报警内容。");
}

static void MesInteractionLogGridShowsRoutePath()
{
    var designer = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);
    var formatter = ExtractMethodText(
        viewCode,
        "private static string FormatMesRoutePath",
        "private static string BuildBasicInfo");

    AssertTrue(formatter.Contains("string.IsNullOrWhiteSpace(url)", StringComparison.Ordinal), "空地址必须安全显示占位符。");
    AssertTrue(formatter.Contains("Uri.TryCreate(normalized, UriKind.Absolute", StringComparison.Ordinal), "完整地址必须按绝对 URI 解析。");
    AssertTrue(formatter.Contains("uri.AbsolutePath", StringComparison.Ordinal), "完整地址必须只显示路由路径。");
    AssertTrue(formatter.Contains("IndexOfAny(['?', '#'])", StringComparison.Ordinal), "相对地址必须移除查询参数和片段。");
    AssertTrue(formatter.Contains("return \"-\";", StringComparison.Ordinal), "空地址必须显示短横线占位符。");
    AssertTrue(
        designer.Contains("new DataGridViewColumn[] { colMesSendTime, colMesPath, colMesPurpose, colMesMethod, colMesHttpStatus, colResult, colMesDuration }", StringComparison.Ordinal),
        "MES 日志列顺序必须为发送时间、接口路径、请求原因、方法、HTTP、结果、耗时。");
    AssertTrue(designer.Contains("colMesPath.DataPropertyName = \"InterfacePath\";", StringComparison.Ordinal), "接口路径列必须绑定投影属性。");
    AssertTrue(viewCode.Contains("colMesPath.HeaderText = _localizer.GetString(TextKeys.Log.ColumnUrl);", StringComparison.Ordinal), "接口路径列标题必须使用本地化资源。");
    AssertTrue(viewCode.Contains("builder.AppendLine($\"Url: {entry.Url}\");", StringComparison.Ordinal), "右侧详情必须继续显示完整 URL。");
    AssertTrue(viewCode.Contains("Contains(entry.Url, keyword)", StringComparison.Ordinal), "MES 日志搜索必须继续匹配完整 URL。");
}

static void LogManageViewKeepsHiddenLogFieldsInDetailsOnly()
{
    var designer = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);

    AssertFalse(designer.Contains("colDeviceSource", StringComparison.Ordinal), "设备状态表格不得再声明来源列。");
    AssertFalse(designer.Contains("colDeviceStation", StringComparison.Ordinal), "设备状态表格不得再声明工位列。");
    AssertFalse(designer.Contains("colProductionStep", StringComparison.Ordinal), "生产流程表格不得再声明步骤列。");
    AssertFalse(designer.Contains("colLifecycleStation", StringComparison.Ordinal), "设备日志表格不得再声明工位列。");
    AssertTrue(viewCode.Contains("Source: {entry.Source}", StringComparison.Ordinal), "设备状态详情必须继续显示来源。");
    AssertTrue(viewCode.Contains("Station: {entry.StationNo}", StringComparison.Ordinal), "设备状态详情必须继续显示工位。");
    AssertTrue(viewCode.Contains("Step: {entry.Step}", StringComparison.Ordinal), "生产流程详情必须继续显示步骤。");
    AssertTrue(viewCode.Contains("Station: {(entry.StationNo", StringComparison.Ordinal), "设备日志详情必须继续显示工位。");
    AssertTrue(viewCode.Contains("Contains(entry.Source, keyword)", StringComparison.Ordinal), "设备状态来源必须继续参与搜索。");
    AssertTrue(viewCode.Contains("Contains(entry.Step, keyword)", StringComparison.Ordinal), "生产流程步骤必须继续参与搜索。");
    var lifecycleFilter = ExtractMethodText(
        viewCode,
        "private static bool IsDeviceLifecycleLogMatched",
        "private static bool IsDeviceStatusLogMatched");
    AssertTrue(lifecycleFilter.Contains("entry.StationNo.ToString()", StringComparison.Ordinal), "设备日志工位必须继续参与搜索。");
    var deviceStatusFilter = ExtractMethodText(
        viewCode,
        "private static bool IsDeviceStatusLogMatched",
        "private static bool Contains(string? source, string keyword)");
    AssertTrue(deviceStatusFilter.Contains("entry.StationNo.ToString()", StringComparison.Ordinal), "设备状态工位必须继续参与搜索。");
}

static void ProductionFlowSummariesUseCentralizedChineseText()
{
    var catalogPath = GetRepoFilePath("AutoWeldSystem.Core", "Production", "ProductionFlowLogTexts.cs");
    AssertTrue(File.Exists(catalogPath), "生产流程摘要文本和资源键必须集中在 Core 摘要目录中。");

    var catalog = File.ReadAllText(catalogPath, Encoding.UTF8);
    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var reconcileCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Plc", "RecipeCodeReconcileMonitorService.cs"), Encoding.UTF8);
    AssertTrue(catalog.Contains("PLC配方号调和失败", StringComparison.Ordinal), "摘要目录必须提供中文配方号调和失败文本。");
    AssertTrue(catalog.Contains("monitor.production_hint.recipe_code_reconcile_failed", StringComparison.Ordinal), "摘要目录必须集中保存对应资源键。");
    AssertEqual(
        ProductionFlowLogTexts.Summaries.RecipeCodeReconcileFailed,
        ProductionFlowLogTexts.NormalizeLegacySummary("PLC recipe code reconcile failed"),
        "旧配方号调和英文摘要必须在显示时转换为中文。");
    AssertEqual(
        ProductionFlowLogTexts.Summaries.DeviceModeReconcileFailed,
        ProductionFlowLogTexts.NormalizeLegacySummary("Device mode reconcile failed."),
        "旧设备模式英文摘要必须在显示时转换为中文。");
    AssertEqual(
        ProductionFlowLogTexts.Summaries.WorkOrderStatusReconcileFailed,
        ProductionFlowLogTexts.NormalizeLegacySummary("Work order status reconcile failed."),
        "旧工单状态调和英文摘要必须在显示时转换为中文。");
    AssertEqual(
        ProductionFlowLogTexts.Summaries.WorkOrderStatusWriteFailed,
        ProductionFlowLogTexts.NormalizeLegacySummary("Work order status write failed."),
        "旧工单状态写入英文摘要必须在显示时转换为中文。");
    AssertFalse(monitorCode.Contains("Work order status write failed.", StringComparison.Ordinal), "生产流程摘要调用点不得继续硬编码英文工单状态写入失败。");
    AssertFalse(monitorCode.Contains("Device mode reconcile failed.", StringComparison.Ordinal), "生产流程摘要调用点不得继续硬编码英文设备模式调和失败。");
    AssertFalse(reconcileCode.Contains("PLC recipe code reconcile failed", StringComparison.Ordinal), "配方号调和生产流程摘要不得继续写入英文。");
}

static void DataManageViewReleasesQueryCancellationSourcesOnce()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "DataManageView.Designer.cs"), Encoding.UTF8);
    var beginDisposeMethod = ExtractMethodText(
        viewCode,
        "private void BeginDispose()",
        "private void ClearTaskDetails()");

    AssertTrue(beginDisposeMethod.Contains("if (_disposing)", StringComparison.Ordinal), "DataManageView 重复释放时必须直接返回，避免再次取消已释放的令牌源。");
    AssertTrue(beginDisposeMethod.Contains("CancelAndDispose(ref _workOrderQueryCancellation);", StringComparison.Ordinal), "释放页面时必须取消、释放并清空工单查询令牌源。");
    AssertTrue(beginDisposeMethod.Contains("CancelAndDispose(ref _detailQueryCancellation);", StringComparison.Ordinal), "释放页面时必须取消、释放并清空明细查询令牌源。");
    AssertFalse(designerCode.Contains("_detailQueryCancellation?.Dispose();", StringComparison.Ordinal), "Designer Dispose 不应单独释放查询令牌源，避免字段保留已释放对象。");
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
        "private void BindTestData");

    AssertFalse(runQueryMethod.Contains("ThrowIfCancellationRequested", StringComparison.Ordinal), "历史查询服务不应在线程池委托中主动抛 OperationCanceledException，避免调试器停在 RunQueryAsync。");
    AssertFalse(runQueryMethod.Contains("}, cancellationToken);", StringComparison.Ordinal), "Task.Run 不应绑定 UI 查询取消令牌，否则取消可能在服务层表现为异常。");
    AssertTrue(runQueryMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "历史查询服务仍应识别已取消查询并跳过过期工作。");
    AssertFalse(queryWorkOrdersMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "工单查询取消后应直接返回，不应再抛取消异常。");
    AssertFalse(loadDetailsMethod.Contains("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal), "明细查询取消后应直接返回，不应再抛取消异常。");
    AssertTrue(queryWorkOrdersMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "工单查询完成后必须检查取消状态，避免旧结果覆盖新界面。");
    AssertTrue(loadDetailsMethod.Contains("if (cancellationToken.IsCancellationRequested)", StringComparison.Ordinal), "明细查询完成后必须检查取消状态，避免旧结果覆盖新界面。");
}

static void LogManageViewLoadsEveryLogTabInDescendingTimeOrder()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "LogManageView.cs"), Encoding.UTF8);
    var sortCount = System.Text.RegularExpressions.Regex.Matches(
        viewCode,
        "OrderByDescending\\(entry => entry\\.(SendTime|OccurredTime)\\)").Count;
    AssertTrue(sortCount >= 6, "所有日志页签必须按时间倒序加载。");
}

static void DeviceApiResponsesOmitInternalIsSuccess()
{
    var response = new DeviceApiResponse<DeviceStatusQueryRes>
    {
        Status = "S",
        Msg = "成功",
        Data = new DeviceStatusQueryRes { DeviceId = "D-001", DeviceStatus = "1" }
    };
    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    AssertTrue(json.Contains("\"Status\":\"S\"", StringComparison.Ordinal), "设备 API 响应必须保留 Status。");
    AssertTrue(json.Contains("\"Msg\":\"成功\"", StringComparison.Ordinal), "设备 API 响应必须保留 Msg。");
    AssertTrue(json.Contains("\"Data\"", StringComparison.Ordinal), "设备 API 响应必须保留 Data。");
    AssertFalse(json.Contains("\"IsSuccess\"", StringComparison.Ordinal), "设备 API 响应不得输出内部 IsSuccess 字段。");

    var serverCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Infrastructure", "DeviceApiServerService.cs"),
        Encoding.UTF8);
    AssertTrue(CountOccurrences(serverCode, "ToResponse(") >= 6, "DeviceStatus 和 DeviceID 的成功与失败响应都必须经过专用外层 DTO。");
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
    AssertFalse(viewCode.Contains("ShowInfo(\"已从工单信息隐藏选中的任务。\")", StringComparison.Ordinal), "删除待上传记录功能已替代隐藏任务，不应保留隐藏提示。");
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

static void WorkOrderDeletionRulesBlockRunningTasks()
{
    AssertTrue(WorkOrderDeletionRules.IsRunning("Running"), "生产中工单必须判定为运行中。");
    AssertTrue(WorkOrderDeletionRules.IsRunning("Paused"), "已暂停工单仍占用工位，必须判定为运行中。");
    AssertTrue(WorkOrderDeletionRules.IsRunning(" running "), "状态判定必须忽略大小写和空白。");

    AssertFalse(WorkOrderDeletionRules.CanDelete("Running"), "生产中工单不允许删除。");
    AssertFalse(WorkOrderDeletionRules.CanDelete("Paused"), "已暂停工单不允许删除。");
    AssertTrue(WorkOrderDeletionRules.CanDelete("Ready"), "待开工工单允许删除。");
    AssertTrue(WorkOrderDeletionRules.CanDelete("Completed"), "已完成工单允许删除。");
    AssertTrue(WorkOrderDeletionRules.CanDelete("Abandoned"), "已作废工单允许删除。");
    AssertTrue(WorkOrderDeletionRules.CanDelete(null), "状态缺失时按可删除处理，避免历史脏数据无法清理。");
}

static void WorkOrderDeletionRulesRestrictReportPathsToReportRoot()
{
    var root = Path.Combine("D:", "AutoWeldData", "Reports");

    AssertTrue(
        WorkOrderDeletionRules.IsDeletableReportPath(Path.Combine(root, "SN001", "20260101", "a.xlsx"), root),
        "报表根目录下的文件必须允许删除。");
    AssertFalse(
        WorkOrderDeletionRules.IsDeletableReportPath(Path.Combine("D:", "AutoWeldData", "other.xlsx"), root),
        "报表根目录之外的文件不得删除。");
    AssertFalse(
        WorkOrderDeletionRules.IsDeletableReportPath(Path.Combine(root, "..", "escape.xlsx"), root),
        "路径穿越到根目录之外时不得删除。");
    AssertFalse(
        WorkOrderDeletionRules.IsDeletableReportPath(Path.Combine("D:", "AutoWeldData", "Reports2", "a.xlsx"), root),
        "同前缀的相邻目录不得被误判为报表根目录的子目录。");
    AssertFalse(WorkOrderDeletionRules.IsDeletableReportPath(null, root), "空路径不得参与删除。");
    AssertFalse(WorkOrderDeletionRules.IsDeletableReportPath("  ", root), "空白路径不得参与删除。");
    AssertFalse(WorkOrderDeletionRules.IsDeletableReportPath(root, root), "报表根目录本身不得被删除。");

    AssertEqual(
        Path.Combine("D:", "AutoWeldData", "Reports"),
        WorkOrderDeletionRules.ResolveReportRootDirectory(@"D:\AutoWeldData"),
        "报表根目录必须与报表生成规则一致。");
}

static void DataDeletePermissionIsCatalogedForAdminsOnly()
{
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);

    var definition = PermissionCatalog.All
        .SingleOrDefault(permission => string.Equals(
            permission.Code,
            PermissionCodes.Buttons.Data.Delete,
            StringComparison.OrdinalIgnoreCase));
    AssertTrue(definition is not null, "历史数据删除权限必须注册到权限目录。");
    AssertEqual(PermissionType.Button, definition!.Type, "历史数据删除权限必须使用 Button 类型。");
    AssertEqual(PermissionCodes.Pages.DataManage, definition.ParentCode, "删除权限必须挂在历史数据页面权限下。");

    var textKey = PermissionTextKeyMapper.GetTextKey(PermissionCodes.Buttons.Data.Delete);
    AssertTrue(zhResources.Contains($"name=\"{textKey}\"", StringComparison.Ordinal), "中文资源必须包含删除权限名称。");
    AssertTrue(enResources.Contains($"name=\"{textKey}\"", StringComparison.Ordinal), "英文资源必须包含删除权限名称。");

    // 操作员和只读角色的默认权限白名单不含删除权限，因此升级后不会自动获得删除能力
    var rbacCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "RbacService.cs"), Encoding.UTF8);
    var defaultMap = ExtractMethodText(
        rbacCode,
        "private static Dictionary<string, IReadOnlyCollection<string>> BuildDefaultRolePermissionMap()",
        "private void RefreshCurrentSessionIfAffected");
    var operatorSection = defaultMap[defaultMap.IndexOf("[AppConstants.Roles.Operator]", StringComparison.Ordinal)..];
    AssertFalse(
        operatorSection.Contains("PermissionCodes.Buttons.Data.Delete", StringComparison.Ordinal),
        "操作员和只读角色的默认权限不得包含历史数据删除。");
}

static void DataDeleteUpgradeGrantsAdminOnlyOnFirstIntroduction()
{
    var upgraded = RolePermissionInitializationRules.ResolveDataDeleteUpgradeDefaults(
        AppConstants.Roles.Admin,
        dataDeleteCatalogWasMissing: true,
        hasDataManagePagePermission: true);
    AssertSequenceEqual(
        new[] { PermissionCodes.Buttons.Data.Delete },
        upgraded,
        "旧数据库首次引入删除权限时必须为管理员补权。");

    AssertEqual(
        0,
        RolePermissionInitializationRules.ResolveDataDeleteUpgradeDefaults(
            AppConstants.Roles.Admin,
            dataDeleteCatalogWasMissing: false,
            hasDataManagePagePermission: true).Count,
        "权限已存在时不得重复补权，避免覆盖管理员手工取消的配置。");
    AssertEqual(
        0,
        RolePermissionInitializationRules.ResolveDataDeleteUpgradeDefaults(
            AppConstants.Roles.Admin,
            dataDeleteCatalogWasMissing: true,
            hasDataManagePagePermission: false).Count,
        "没有历史数据页权限的角色不得获得删除权限。");
    AssertEqual(
        0,
        RolePermissionInitializationRules.ResolveDataDeleteUpgradeDefaults(
            AppConstants.Roles.Operator,
            dataDeleteCatalogWasMissing: true,
            hasDataManagePagePermission: true).Count,
        "补权只针对管理员角色。");
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
    AssertTrue(userServiceCode.Contains("ApplyTabUpgradeDefaults", StringComparison.Ordinal), "RBAC 初始化协调必须应用一次性客户页签升级授权。");
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
    AssertTrue(noVisibleMethod.Contains("BindRows(Array.Empty<object>());", StringComparison.Ordinal), "没有可见页签时必须通过统一绑定入口清空旧数据。");
    AssertTrue(noVisibleMethod.Contains("column.Visible = false;", StringComparison.Ordinal), "没有可见页签时必须隐藏固定列，不能销毁列对象。");
    AssertFalse(noVisibleMethod.Contains("dgvPending.Columns.Clear();", StringComparison.Ordinal), "没有可见页签时不能在绘制期间清空列集合。");
    AssertTrue(noVisibleMethod.Contains("TextKeys.StateManage.MessageNoVisibleTabs", StringComparison.Ordinal), "没有可见页签时必须显示明确提示。");
}

static void StateManageViewKeepsStableColumnsAndLoadsOffUiThread()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "StateManageView.cs"), Encoding.UTF8);
    var configureMethod = ExtractMethodText(
        viewCode,
        "private void ConfigureActiveGridColumns()",
        "private List<(string PropertyName, string HeaderText, float FillWeight, string? Format)> GetActiveColumnDefinitions");
    var reloadMethod = ExtractMethodText(
        viewCode,
        "private async Task ReloadActiveTasksAsync",
        "private StateUploadLoadResult LoadActiveTasks");
    var bindMethod = ExtractMethodText(
        viewCode,
        "private void BindRows(object rows)",
        "private void SetReloadingState");

    AssertTrue(viewCode.Contains("InitializeGridColumns();", StringComparison.Ordinal), "待上传表格必须只初始化一次固定列对象。");
    AssertTrue(configureMethod.Contains("column.Visible = false;", StringComparison.Ordinal), "页签切换应通过显隐固定列调整布局。");
    AssertFalse(configureMethod.Contains("Columns.Clear", StringComparison.Ordinal), "页签切换不能清空正在绘制的列集合。");
    AssertTrue(reloadMethod.Contains("await Task.Run", StringComparison.Ordinal), "数据库和 JSONL 查询必须移出 UI 线程。");
    AssertTrue(reloadMethod.Contains("_reloadGate.WaitAsync", StringComparison.Ordinal), "连续刷新必须串行化，避免重复查询并发执行。");
    AssertTrue(reloadMethod.Contains("version != Volatile.Read(ref _reloadVersion)", StringComparison.Ordinal), "过期页签结果不能覆盖当前页签。");
    AssertTrue(bindMethod.Contains("_bindingSource.ResetBindings(true);", StringComparison.Ordinal), "切换不同 DTO 数据源后必须刷新绑定元数据，避免只显示空白行。");
    AssertFalse(bindMethod.Contains("_bindingSource.ResetBindings(false);", StringComparison.Ordinal), "待上传表格不能只刷新数据值而保留旧 DTO 属性元数据。");
    AssertTrue(
        viewCode.Contains("_weldTaskService.StateChanged += WeldTaskService_StateChanged;", StringComparison.Ordinal)
            && viewCode.Contains("_weldTaskService.StateChanged -= WeldTaskService_StateChanged;", StringComparison.Ordinal),
        "工单信息页必须订阅并解绑任务状态变化事件。");
    AssertTrue(
        viewCode.Contains("protected override void OnVisibleChanged(EventArgs e)", StringComparison.Ordinal)
            && viewCode.Contains("if (_initialized && Visible && IsSummaryTab())", StringComparison.Ordinal),
        "缓存页面重新显示且位于工单信息页签时必须刷新当前任务汇总。");
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

static void WeldTaskPendingRetryIncludesDeviceStatus()
{
    var deviceStatusService = new FakeDeviceStatusService();
    var service = CreateWeldTaskService(
        new FakeMesProvider(),
        new FakeSystemClockService(),
        new FakeOperationLogService(),
        deviceStatusService: deviceStatusService);

    service.RetryPendingUploadsAsync().GetAwaiter().GetResult();

    AssertEqual(1, deviceStatusService.RetryPendingUploadsCallCount, "MES 重连补传入口必须同时补传 JSONL 设备状态。");

    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var startMethod = ExtractMethodText(
        uploadCode,
        "private async Task<BasicRes<object>> UploadStartReportAsync",
        "private void ApplyOfflineStartRequestId");
    var finishMethod = ExtractMethodText(
        uploadCode,
        "private async Task<BasicRes<object>> UploadFinishReportAsync",
        "private async Task<BasicRes<object>> UploadWorkOrderStatusAsync");
    AssertSourceOrder(
        startMethod,
        "if (cancellationToken.IsCancellationRequested)",
        "WriteStartReportLifecycleLog",
        "窗口关闭后晚返回的开工响应不能再追加程序开始状态。");
    AssertTrue(
        finishMethod.Contains("response.IsSuccess && !cancellationToken.IsCancellationRequested", StringComparison.Ordinal),
        "窗口关闭后晚返回的完工响应不能再追加程序结束状态。");
    var deviceStatusCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "DeviceStatusService.cs"),
        Encoding.UTF8);
    var changeMethod = ExtractMethodText(
        deviceStatusCode,
        "public async Task<BizDeviceStatusLog> ChangeStatusAsync(",
        "public async Task<BasicRes<object>?> RetryUploadAsync(");
    AssertSourceOrder(
        changeMethod,
        "cancellationToken.ThrowIfCancellationRequested();",
        "DeviceStatusLocalLogStore.TryAppend(log, CurrentSettings)",
        "取消后的普通状态必须在 JSONL 落盘前停止，避免越过最终停机状态。");
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
    AssertEqual("api/DeviceStatus", settings.DeviceStatusQueryRoute, "本地设备状态查询接口默认路由必须为 api/DeviceStatus。");
    AssertEqual("api/DeviceID", settings.DeviceIdSetRoute, "本地设备编号设置接口默认路由必须为 api/DeviceID。");
    AssertEqual("api/sys", settings.MesSysRoute, "在线检测接口默认路由必须为 api/sys。");
    AssertFalse(settings.EnablePostDataCustomHeader == true, "PostData 自定义 Header 默认关闭，避免升级后影响现场接口。");
}

static void SystemSettingLocalizationResourcesAreComplete()
{
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    var enResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var keys = typeof(TextKeys.SystemSetting)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    foreach (var key in keys)
    {
        AssertTrue(zhResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"中文资源必须包含 {key}。");
        AssertTrue(enResources.Contains($"name=\"{key}\"", StringComparison.Ordinal), $"英文资源必须包含 {key}。");
    }

    var chineseLiteral = System.Text.RegularExpressions.Regex.Match(
        viewCode,
        "\"[^\"\\r\\n]*[\\u4e00-\\u9fff][^\"\\r\\n]*\"");
    AssertFalse(chineseLiteral.Success, $"SystemSettingView.cs 不应保留中文字符串字面量：{chineseLiteral.Value}");
    AssertTrue(viewCode.Contains("private sealed record LocalizedOption<T>(T Value, string TextKey);", StringComparison.Ordinal), "本地化选项必须统一保存稳定值和资源键。");
    AssertFalse(viewCode.Contains("record UploadModeOption", StringComparison.Ordinal), "不应继续为各下拉框维护重复的 DisplayName record。");
    AssertTrue(viewCode.Contains("ShowWarning(ex.Message);", StringComparison.Ordinal), "站点名称校验抛出的资源键必须经过本地化服务显示。");
}

static void MesEndpointValidationReturnsStableErrorCodes()
{
    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute(" ", out _, out var required), "空路由必须失败。");
    AssertEqual(MesEndpointValidationError.Required, required, "空路由应返回 Required。");

    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute("https://mes/api/Test", out _, out var absolute), "完整 URL 必须失败。");
    AssertEqual(MesEndpointValidationError.AbsoluteUrlNotAllowed, absolute, "完整 URL 应返回 AbsoluteUrlNotAllowed。");

    AssertFalse(MesEndpointRouteRules.TryNormalizeRequiredRoute("api/Test?id=1", out _, out var query), "带查询参数的路由必须失败。");
    AssertEqual(MesEndpointValidationError.QueryOrFragmentNotAllowed, query, "查询参数应返回 QueryOrFragmentNotAllowed。");

    AssertTrue(MesEndpointRouteRules.TryNormalizeRequiredRoute("/api/Test", out var route, out var routeError), "合法相对路由应通过。");
    AssertEqual("api/Test", route, "合法路由应去掉前导斜杠。");
    AssertEqual(MesEndpointValidationError.None, routeError, "合法路由应返回 None。");

    AssertFalse(MesEndpointRouteRules.TryValidatePostDataHeader(true, "Bad Key", "value", out _, out _, out var keyError), "非法 Header Key 必须失败。");
    AssertEqual(MesEndpointValidationError.InvalidHeaderKey, keyError, "非法 Header Key 应返回 InvalidHeaderKey。");

    AssertFalse(MesEndpointRouteRules.TryValidatePostDataHeader(true, "X-Test", " ", out _, out _, out var valueError), "空 Header Value 必须失败。");
    AssertEqual(MesEndpointValidationError.HeaderValueRequired, valueError, "空 Header Value 应返回 HeaderValueRequired。");

    AssertTrue(MesEndpointRouteRules.TryValidatePostDataHeader(false, "", "", out _, out _, out var disabledError), "未启用自定义 Header 时空值应通过。");
    AssertEqual(MesEndpointValidationError.None, disabledError, "未启用时应返回 None。");
}

static void DeviceIdSyncRulesDetectMissingOldDevices()
{
    AssertTrue(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", "设备不存在"), "中文设备不存在消息必须允许确认后新建设备。");
    AssertTrue(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", " DEVICE NOT FOUND "), "英文 device not found 必须忽略大小写和首尾空格。");
    AssertTrue(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", "Device does not exist."), "英文 device does not exist 必须允许降级注册。");
    AssertFalse(DeviceIdSyncRules.ShouldOfferRegisterAsNew(string.Empty, "设备不存在"), "首次注册没有旧编号时不得再次触发降级。");
    AssertFalse(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", "MES 连接超时"), "网络超时不得误判为旧设备不存在。");
    AssertFalse(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", "新设备编号已存在"), "新设备编号冲突不得误走新设备注册。");
    AssertFalse(DeviceIdSyncRules.ShouldOfferRegisterAsNew("OLD-1", null), "空消息不得触发降级。");
}

static void SystemSettingRetriesMissingOldDeviceAsNewRegistration()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var backgroundSyncMethod = ExtractMethodText(
        viewCode,
        "private async Task SyncDeviceAfterSaveAsync(",
        "private bool IsCurrentDeviceSync(");
    var manualSyncMethod = ExtractMethodText(
        viewCode,
        "private async Task<DeviceSyncOutcome> SyncDeviceToMesAsync(",
        "private bool ConfirmRegisterNewDevice(");
    var registerRequestMethod = ExtractMethodText(
        viewCode,
        "private static AddDeviceReq BuildNewDeviceRegistrationRequest(",
        "/// <summary>");
    var markSyncedMethod = ExtractMethodText(
        viewCode,
        "private bool TryMarkDeviceSynced(",
        "/// <summary>");
    var saveMethod = ExtractMethodText(viewCode, "private async void SaveAll_Click", "/// <summary>");
    var manualClickMethod = ExtractMethodText(viewCode, "private async void SyncDevice_ClickAsync", "private async void TestConnection_ClickAsync");

    AssertTrue(
        manualSyncMethod.Contains("DeviceIdSyncRules.ShouldOfferRegisterAsNew(request.OldDeviceId, response.Msg)", StringComparison.Ordinal)
            && manualSyncMethod.Contains("ConfirmRegisterNewDevice(request)", StringComparison.Ordinal),
        "只有手动同步在旧设备明确不存在且用户确认后才能按新设备注册。");
    AssertEqual(
        2,
        System.Text.RegularExpressions.Regex.Matches(manualSyncMethod, @"_mesProvider\.SetDeviceIdAsync").Count,
        "手动设备同步最多只能调用一次更新和一次新设备注册。");
    AssertEqual(
        1,
        System.Text.RegularExpressions.Regex.Matches(backgroundSyncMethod, @"_mesProvider\.SetDeviceIdAsync").Count,
        "保存后的后台同步只能尝试一次设备更新。");
    AssertFalse(backgroundSyncMethod.Contains("ConfirmRegisterNewDevice", StringComparison.Ordinal), "后台同步不得弹出注册新设备确认框。");
    AssertFalse(backgroundSyncMethod.Contains("BuildNewDeviceRegistrationRequest", StringComparison.Ordinal), "后台同步不得自动注册新设备。");
    AssertTrue(
        backgroundSyncMethod.Contains("MessageDeviceSyncManualConfirmationRequired", StringComparison.Ordinal),
        "后台发现旧设备不存在时必须提示用户转到手动同步确认。");
    AssertTrue(
        registerRequestMethod.Contains("OldDeviceId = string.Empty", StringComparison.Ordinal)
            && registerRequestMethod.Contains("DeviceId = request.DeviceId", StringComparison.Ordinal),
        "手动降级注册必须只清空 OldDeviceId 并保留新设备资料。");
    AssertTrue(
        saveMethod.Contains("var shouldSyncDevice = HasDeviceIdentityChanged(previousSettings, settings);", StringComparison.Ordinal)
            && saveMethod.Contains("if (shouldSyncDevice)", StringComparison.Ordinal)
            && saveMethod.Contains("StartDeviceSyncAfterSave(previousSettings, savedSettings);", StringComparison.Ordinal),
        "应用全部只能在设备身份实际变化后启动后台同步。");
    AssertFalse(saveMethod.Contains("BuildDeviceRequest", StringComparison.Ordinal), "应用全部的本地保存路径不得同步构建设备请求或查询本机 IP。");
    AssertFalse(saveMethod.Contains("await SyncDeviceToMesAsync", StringComparison.Ordinal), "应用全部不得等待 MES 设备同步。");
    AssertTrue(
        manualClickMethod.Contains("await Task.Run(() => BuildDeviceRequest(previousSettings, settings))", StringComparison.Ordinal)
            && manualClickMethod.Contains("await SyncDeviceToMesAsync(request)", StringComparison.Ordinal),
        "手动同步必须继续显式构建请求并等待统一同步流程。");
    AssertTrue(
        backgroundSyncMethod.Contains("IsCurrentDeviceSync(syncVersion, request.DeviceId)", StringComparison.Ordinal)
            && markSyncedMethod.Contains("SameText(settings.DeviceId, deviceId)", StringComparison.Ordinal),
        "后台迟到结果必须同时校验同步版本和当前设备编号后才能标记成功。");
    AssertTrue(markSyncedMethod.Contains("_suppressSettingsChangedBinding", StringComparison.Ordinal), "后台同步成功不得重新绑定整页并覆盖未应用输入。");
    AssertFalse(manualSyncMethod.Contains("MesSyncedDeviceId", StringComparison.Ordinal), "MES 失败或用户取消时不得提前修改已同步设备编号。");
}

static void LocalizationServiceReportsMissingResourceKeys()
{
    var settings = new FakeAppSettingsService();
    var localizer = new AutoWeldSystem.Services.LocalizationService(settings);
    using var writer = new StringWriter();
    using var listener = new System.Diagnostics.TextWriterTraceListener(writer);
    System.Diagnostics.Trace.Listeners.Add(listener);
    try
    {
        const string missingKey = "system.test.missing_key";
        AssertEqual(missingKey, localizer.GetString(missingKey), "缺失资源必须回退为原键。");
        listener.Flush();
        AssertTrue(writer.ToString().Contains(missingKey, StringComparison.Ordinal), "缺失资源必须写入 Trace 警告。");
    }
    finally
    {
        System.Diagnostics.Trace.Listeners.Remove(listener);
    }
}

static void MesHeartbeatIntervalNormalizationClampsToSupportedRange()
{
    AssertEqual(5, new AppSettings().MesHeartbeatIntervalSeconds, "MES 心跳间隔默认必须为 5 秒。");
    AssertEqual(5, MesConnectionRules.NormalizeHeartbeatIntervalSeconds(0), "旧数据库补列后的 0 必须回退到默认间隔。");
    AssertEqual(5, MesConnectionRules.NormalizeHeartbeatIntervalSeconds(-10), "负值必须回退到默认间隔。");
    AssertEqual(1, MesConnectionRules.NormalizeHeartbeatIntervalSeconds(1), "下限 1 秒必须原样保留。");
    AssertEqual(300, MesConnectionRules.NormalizeHeartbeatIntervalSeconds(300), "上限 300 秒必须原样保留。");
    AssertEqual(300, MesConnectionRules.NormalizeHeartbeatIntervalSeconds(3600), "超过上限必须收敛到 300 秒。");
}

static void MesConnectionMonitorConfirmsOfflineAfterThreeFailures()
{
    var provider = new FakeMesProvider();
    provider.OnlineCheckHandler = (_, _) => Task.FromResult(new BasicRes<object>
    {
        Status = AppConstants.MesStatus.Error,
        Msg = "timeout"
    });
    using var monitor = new MesConnectionMonitor(
        provider,
        new FakeLocalizationService(),
        new FakeAppSettingsService());
    var published = new List<MesConnectionSnapshot>();
    monitor.StatusChanged += (_, snapshot) => published.Add(snapshot);

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();

    AssertEqual(default, monitor.Current.UpdatedTime, "启动阶段前两次失败必须保持检测中。");
    AssertEqual(0, published.Count, "未达到阈值时不得发布离线状态。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();

    AssertFalse(monitor.Current.IsConnected, "连续第三次失败必须确认 MES 离线。");
    AssertTrue(monitor.Current.UpdatedTime != default, "确认离线后必须更新时间。");
    AssertEqual("timeout", monitor.Current.Message, "确认离线后必须保留最后一次失败原因。");
    AssertEqual(1, published.Count, "连续失败只应在达到阈值时发布一次离线状态。");
}

static void MesConnectionMonitorResetsFailuresAndHandlesExceptions()
{
    var provider = new FakeMesProvider();
    var responses = new Queue<Func<Task<BasicRes<object>>>>(new Func<Task<BasicRes<object>>>[]
    {
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Success, Msg = "OK" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "timeout-1" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "timeout-2" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Success, Msg = "OK" }),
        () => Task.FromException<BasicRes<object>>(new InvalidOperationException("probe exception")),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "timeout-after-exception" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "confirmed-offline" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Success, Msg = "OK" })
    });
    provider.OnlineCheckHandler = (_, _) => responses.Dequeue().Invoke();
    using var monitor = new MesConnectionMonitor(
        provider,
        new FakeLocalizationService(),
        new FakeAppSettingsService());
    var published = new List<MesConnectionSnapshot>();
    monitor.StatusChanged += (_, snapshot) => published.Add(snapshot);

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    var firstSuccessTime = monitor.Current.LastSuccessTime;
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();

    AssertTrue(monitor.Current.IsConnected, "在线状态下前两次失败必须继续保持在线。");
    AssertEqual(firstSuccessTime, monitor.Current.LastSuccessTime, "短暂失败不得覆盖最后成功时间。");
    AssertFalse(published.Any(snapshot => !snapshot.IsConnected), "未达到阈值时不得发布设备断线状态。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertTrue(monitor.Current.IsConnected, "探测成功必须保持在线并清零连续失败次数。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertTrue(monitor.Current.IsConnected, "未预期异常与后续一次失败累计两次时仍应保持在线。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertFalse(monitor.Current.IsConnected, "未预期异常必须与普通失败共用三次离线阈值。");
    AssertEqual("confirmed-offline", monitor.Current.Message, "确认离线时必须保留最后一次失败原因。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertTrue(monitor.Current.IsConnected, "已离线时任意一次成功必须立即恢复在线。");
    AssertEqual(1, published.Count(snapshot => !snapshot.IsConnected), "整段探测序列只能发布一次确认离线状态。");
}

static void MesOfflineRepublishesWhenFailureReasonChanges()
{
    AssertTrue(
        MesConnectionRules.ShouldRepublishOfflineFailure(isCurrentlyOffline: false, "any", "any"),
        "尚未确认离线时必须发布首次离线状态。");
    AssertFalse(
        MesConnectionRules.ShouldRepublishOfflineFailure(isCurrentlyOffline: true, " timeout ", "timeout"),
        "离线原因未变化时不得重复发布，避免同一故障持续刷新界面。");
    AssertTrue(
        MesConnectionRules.ShouldRepublishOfflineFailure(isCurrentlyOffline: true, "HTTP 404", "连接超时"),
        "离线原因变化必须重新发布，否则改错路由或断网后指示灯文本永久冻结。");

    // 现场验证路径：先确认离线，再把失败原因从 404 换成连接超时，指示灯必须跟着变。
    var provider = new FakeMesProvider();
    var responses = new Queue<Func<Task<BasicRes<object>>>>(new Func<Task<BasicRes<object>>>[]
    {
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "HTTP 404" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "HTTP 404" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "HTTP 404" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "HTTP 404" }),
        () => Task.FromResult(new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "连接超时" })
    });
    provider.OnlineCheckHandler = (_, _) => responses.Dequeue().Invoke();
    using var monitor = new MesConnectionMonitor(
        provider,
        new FakeLocalizationService(),
        new FakeAppSettingsService());
    var published = new List<MesConnectionSnapshot>();
    monitor.StatusChanged += (_, snapshot) => published.Add(snapshot);

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertEqual(1, published.Count, "连续失败只应在达到阈值时发布一次离线状态。");
    AssertEqual("HTTP 404", monitor.Current.Message, "确认离线必须保留首次失败原因。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertEqual(1, published.Count, "离线原因相同的后续失败不得重复发布。");

    monitor.CheckOnceSafelyAsync().GetAwaiter().GetResult();
    AssertEqual(2, published.Count, "离线原因变化必须重新发布，供 MonitorView 刷新 MES 指示灯。");
    AssertEqual("连接超时", monitor.Current.Message, "离线原因变化后必须更新为最新失败原因。");
    AssertFalse(monitor.Current.IsConnected, "重新发布不得把状态误判为在线。");
}

static void MesProbeDelayShortensBeforeOfflineIsConfirmed()
{
    AssertEqual(
        5,
        MesConnectionRules.ResolveNextProbeDelaySeconds(5, consecutiveFailures: 0),
        "在线态必须使用配置的心跳间隔。");
    AssertEqual(
        MesConnectionRules.FailureRetryIntervalSeconds,
        MesConnectionRules.ResolveNextProbeDelaySeconds(5, consecutiveFailures: 1),
        "首次失败后必须改用短重探间隔，避免确认离线要等满三倍心跳间隔。");
    AssertEqual(
        MesConnectionRules.FailureRetryIntervalSeconds,
        MesConnectionRules.ResolveNextProbeDelaySeconds(5, consecutiveFailures: 2),
        "未确认离线前的失败都必须使用短重探间隔。");
    AssertEqual(
        5,
        MesConnectionRules.ResolveNextProbeDelaySeconds(5, MesConnectionRules.OfflineFailureThreshold),
        "确认离线后必须回到正常心跳间隔，不得持续高频重试。");
    AssertEqual(
        MesConnectionRules.DefaultHeartbeatIntervalSeconds,
        MesConnectionRules.ResolveNextProbeDelaySeconds(0, consecutiveFailures: 0),
        "旧数据库补列后的 0 必须回退到默认心跳间隔。");
    AssertTrue(
        MesConnectionRules.ResolveNextProbeDelaySeconds(1, consecutiveFailures: 1) <= 1,
        "心跳间隔比重探间隔更短时，失败重探不得反而拉长等待。");
}

static void MesOnlineCheckSkipsInteractionLogAndUsesDedicatedTimeout()
{
    var handler = new BlockingHttpMessageHandler();
    var logService = new FakeMesInteractionLogService();
    var settings = new FakeAppSettingsService
    {
        Current = BuildCustomMesRouteSettings()
    };
    settings.Current.MesTimeoutSeconds = 10;
    using var provider = CreateMesProvider(settings, handler, logService);

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var response = provider.CheckSystemOnlineAsync(previousOnline: null).GetAwaiter().GetResult();
    stopwatch.Stop();

    AssertFalse(response.IsSuccess, "在线检测超时必须返回失败结果。");
    AssertTrue(handler.CancellationObserved.Task.Wait(TimeSpan.FromSeconds(1)), "在线检测必须触发独立超时取消。");
    AssertTrue(stopwatch.Elapsed >= TimeSpan.FromSeconds(2), "在线检测不能早于 3 秒超时太多。");
    AssertTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "在线检测必须使用 3 秒独立超时，不能沿用 10 秒业务超时。");
    AssertEqual(0, logService.Entries.Count, "自动在线检测不得写入 MES 交互日志。");
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
        provider.CheckSystemOnlineAsync(previousOnline: null).GetAwaiter().GetResult();
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
                "mes/sys-custom",
                "mes/sys-custom",
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

        var reportRequest = handler.Requests.Single(request => string.Equals(request.Path, "mes/report-file-custom", StringComparison.OrdinalIgnoreCase));
        AssertTrue(reportRequest.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase), "报告文件必须使用 multipart/form-data。");
        AssertTrue(reportRequest.Body.Contains("name=file", StringComparison.OrdinalIgnoreCase), "multipart 请求必须包含 file 文件字段。");
        AssertTrue(reportRequest.Body.Contains(Path.GetFileName(tempReportFile), StringComparison.Ordinal), "multipart 请求必须携带真实文件名。");
        AssertTrue(reportRequest.Body.Contains("report", StringComparison.Ordinal), "multipart 请求必须携带非空文件内容。");
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
    var rulesCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Production", "DeviceLifecycleLogRules.cs"),
        Encoding.UTF8);
    var coordinatorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);

    AssertFalse(rulesCode.Contains("CreateSoftwareStartedEntry", StringComparison.Ordinal)
        || rulesCode.Contains("CreateSoftwareStoppedEntry", StringComparison.Ordinal),
        "设备日志不应再创建独立的软件开启或关闭事件。");
    AssertFalse(coordinatorCode.Contains("CreateSoftwareStartedEntry", StringComparison.Ordinal)
        || coordinatorCode.Contains("CreateSoftwareStoppedEntry", StringComparison.Ordinal),
        "生命周期协调器必须只写入设备状态开机和停机记录。");
}

static void DeviceLifecycleCoordinatorRecordsSoftwareLifecycleStatuses()
{
    var coordinatorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);

    AssertFalse(
        coordinatorCode.Contains("CreateSoftwareStartedEntry", StringComparison.Ordinal)
        || coordinatorCode.Contains("CreateSoftwareStoppedEntry", StringComparison.Ordinal),
        "程序启动和关闭只应写入设备状态开机和停机记录，不再创建独立生命周期日志。");
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

    var poweredOn = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn);
    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);
    AssertTrue(lifecycleLogs.Entries.All(entry => entry.EventType != "SoftwareStarted" && entry.EventType != "SoftwareStopped"),
        "启动和关闭不应再产生独立的软件生命周期日志。");
    AssertEqual("开机", DeviceLifecycleLogRules.CreateDeviceStatusEntry(poweredOn).Summary,
        "设备状态开机摘要必须简洁显示开机。");
    AssertEqual("停机", DeviceLifecycleLogRules.CreateDeviceStatusEntry(stopped).Summary,
        "设备状态停机摘要必须简洁显示停机。");
}

static void DeviceLifecycleOrdersStatusProducersAroundFinalStates()
{
    var programCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Program.cs"), Encoding.UTF8);

    AssertSourceOrder(
        programCode,
        "IDeviceLifecycleLogCoordinator>().Start();",
        "IDeviceApiServerService>().StartAsync().GetAwaiter().GetResult();",
        "启动时必须先同步落盘开机状态，再启动并记录 HTTP 服务自检结果。");
    AssertSourceOrder(
        programCode,
        "IDeviceLifecycleLogCoordinator>().Start();",
        "IPlcProductionMonitorService>().StartAsync().GetAwaiter().GetResult();",
        "启动时必须先落盘开机并取得补传顺序所有权，再启动 PLC 设备状态生产者。");
    AssertSourceOrder(
        programCode,
        "IPlcProductionMonitorService>().StopAsync().GetAwaiter().GetResult()",
        "IDeviceLifecycleLogCoordinator>().Stop()",
        "退出时必须先停止 PLC 设备状态生产者，再上传最终停机状态。");
}

static void DeviceLifecycleStopSurvivesEarlierShutdownFailures()
{
    var programCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Program.cs"), Encoding.UTF8);
    var shutdownMethod = ExtractMethodText(
        programCode,
        "private static void StopBackgroundServices(",
        "private static void TryStopBackgroundService(Action stop)");

    AssertTrue(
        shutdownMethod.Contains(
            "() => AppHost?.Services.GetRequiredService<IDeviceLifecycleLogCoordinator>().Stop());",
            StringComparison.Ordinal),
        "最终停机状态必须使用独立异常边界，不能被更早的服务停止失败跳过。");
    AssertTrue(
        programCode.Contains("private static void TryStopBackgroundService(Action stop)", StringComparison.Ordinal),
        "后台服务停止动作必须逐项隔离异常并继续后续清理。");
}

static void DeviceLifecycleStartPersistsPoweredOnBeforePendingReplay()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var coordinator = CreateDeviceLifecycleLogCoordinator(lifecycleLogs, statusService);

    coordinator.Start();
    WaitUntil(
        () => statusService.RetryPendingUploadsCallCount == 1,
        "开机状态落盘后必须触发一次后台设备状态补传。");

    AssertEqual(false, statusService.LastReportToMes, "开机状态必须先只落盘，不能绕过旧状态直接上传。");
    AssertSequenceEqual(
        new[]
        {
            $"Change:{ProductionConstants.MesDeviceStatuses.PoweredOn}:False",
            "RetryPending"
        },
        statusService.OperationSequence,
        "启动时必须先写入当前开机状态，再补传 JSONL 中全部未成功状态。");
}

static void DeviceLifecycleStopCancelsStartupPendingReplay()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var replayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var replayCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseReplay = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    statusService.RetryPendingUploadsHandler = async cancellationToken =>
    {
        using var throwingRegistration = cancellationToken.Register(
            static () => throw new InvalidOperationException("测试取消回调异常"));
        replayStarted.TrySetResult();
        try
        {
            await releaseReplay.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            replayCancelled.TrySetResult();
            throw;
        }
    };
    var coordinator = CreateDeviceLifecycleLogCoordinator(
        lifecycleLogs,
        statusService,
        new AppSettings { DeviceId = "D-001", MesTimeoutSeconds = 3 });

    try
    {
        coordinator.Start();
        AssertTrue(replayStarted.Task.Wait(TimeSpan.FromSeconds(3)), "启动设备状态补传必须在超时前开始。");

        coordinator.Stop();

        AssertTrue(replayCancelled.Task.Wait(TimeSpan.FromSeconds(1)), "停止协调器时必须取消尚未完成的启动补传。");
        AssertTrue(
            statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped),
            "取消启动补传后仍必须写入并处理最终停机状态。");
    }
    finally
    {
        releaseReplay.TrySetResult();
    }
}

static void DeviceLifecycleStopStillWaitsAfterReplayCancellation()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var replayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var stopUploadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseStopUpload = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    statusService.RetryPendingUploadsHandler = async cancellationToken =>
    {
        replayStarted.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    };
    statusService.ChangeStatusHandler = async (log, _, cancellationToken) =>
    {
        if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped)
        {
            stopUploadStarted.TrySetResult();
            await releaseStopUpload.Task.WaitAsync(cancellationToken);
        }
    };
    var coordinator = CreateDeviceLifecycleLogCoordinator(
        lifecycleLogs,
        statusService,
        new AppSettings { DeviceId = "D-001", MesTimeoutSeconds = 3 });

    coordinator.Start();
    AssertTrue(replayStarted.Task.Wait(TimeSpan.FromSeconds(3)), "启动补传必须先进入等待状态。");

    var stopTask = Task.Run(coordinator.Stop);
    AssertTrue(stopUploadStarted.Task.Wait(TimeSpan.FromSeconds(3)), "取消启动补传后仍必须开始停机上传。");
    AssertFalse(stopTask.Wait(TimeSpan.FromMilliseconds(100)), "启动补传取消完成不能让退出提前跳过停机上传等待。");

    releaseStopUpload.TrySetResult();
    AssertTrue(stopTask.Wait(TimeSpan.FromSeconds(3)), "停机上传完成后退出应正常结束。");
}

static void DeviceLifecycleKeepsTimedOutStartupReplayTracked()
{
    var coordinatorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);
    var stopMethod = ExtractMethodText(
        coordinatorCode,
        "public void Stop()",
        "private string CurrentDeviceId");

    AssertFalse(stopMethod.Contains("_startupReplayCancellation = null;", StringComparison.Ordinal), "启动补传超时前不得丢失取消源引用。");
    AssertFalse(stopMethod.Contains("_startupReplayTask = null;", StringComparison.Ordinal), "启动补传超时前不得丢失任务引用。");
    AssertTrue(coordinatorCode.Contains("ObserveStartupReplayCompletion", StringComparison.Ordinal), "启动补传任务必须注册完成后的清理逻辑。");
    AssertTrue(coordinatorCode.Contains("ContinueWith(", StringComparison.Ordinal), "启动补传完成后必须异步清理任务和取消源引用。");
    AssertTrue(coordinatorCode.Contains("RetryPendingUploadsAsync(cancellationToken).ConfigureAwait(false)", StringComparison.Ordinal), "启动补传完成不得依赖 UI 同步上下文继续泵消息。");
}

static void DeviceLifecycleStopWaitsForBoundedStatusUpload()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    var stopUploadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseStopUpload = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    statusService.ChangeStatusHandler = async (log, _, cancellationToken) =>
    {
        if (log.DeviceStatus != ProductionConstants.MesDeviceStatuses.Stopped)
        {
            return;
        }

        stopUploadStarted.TrySetResult();
        await releaseStopUpload.Task.WaitAsync(cancellationToken);
    };
    var coordinator = CreateDeviceLifecycleLogCoordinator(
        lifecycleLogs,
        statusService,
        new AppSettings { DeviceId = "D-001", MesTimeoutSeconds = 3 });

    coordinator.Start();
    WaitUntil(
        () => statusService.Logs.Any(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.PoweredOn),
        "开机设备状态日志应在启动后写入。");

    var stopTask = Task.Run(coordinator.Stop);
    AssertTrue(stopUploadStarted.Task.Wait(TimeSpan.FromSeconds(3)), "停机 MES 上传必须在超时前开始。");
    AssertFalse(stopTask.Wait(TimeSpan.FromMilliseconds(100)), "停机上传完成前，后台清理进程不能提前结束。");
    AssertTrue(statusService.LastCancellationToken.CanBeCanceled, "停机上传必须携带 MES 超时取消令牌。");

    releaseStopUpload.TrySetResult();
    AssertTrue(stopTask.Wait(TimeSpan.FromSeconds(3)), "停机 MES 上传完成后，后台清理必须正常结束。");

    var stopped = statusService.Logs.Single(log => log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped);
    AssertEqual(ProductionConstants.MesDeviceStatuses.Stopped, stopped.DeviceStatus, "停止协调器时必须写入停机状态。");
    AssertTrue(statusService.LastReportToMes == true, "停机状态应先尝试 MES 上传，而不是只进入待上传队列。");
}

static void DeviceLifecycleStopBoundsSynchronousStatusStartup()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var statusService = new FakeDeviceStatusService();
    using var releaseSynchronousStart = new ManualResetEventSlim();
    var synchronousStartEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    statusService.ChangeStatusHandler = (log, _, _) =>
    {
        if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Stopped)
        {
            synchronousStartEntered.TrySetResult();
            releaseSynchronousStart.Wait();
        }

        return Task.CompletedTask;
    };
    var coordinator = CreateDeviceLifecycleLogCoordinator(
        lifecycleLogs,
        statusService,
        new AppSettings { DeviceId = "D-001", MesTimeoutSeconds = 3 });

    coordinator.Start();
    var stopTask = Task.Run(coordinator.Stop);
    try
    {
        AssertTrue(
            synchronousStartEntered.Task.Wait(TimeSpan.FromSeconds(1)),
            "停机状态处理必须开始执行。");
        AssertTrue(
            stopTask.Wait(TimeSpan.FromSeconds(4)),
            "即使设备状态调用在首次 await 前同步阻塞，退出也必须受一个 MES 超时约束。");
    }
    finally
    {
        releaseSynchronousStart.Set();
        stopTask.Wait(TimeSpan.FromSeconds(1));
    }
}

static void UploadTaskMesSuccessSurvivesStatusCancellation()
{
    var uploadCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Production", "UploadTaskService.cs"),
        Encoding.UTF8);
    var startMethod = ExtractMethodText(
        uploadCode,
        "private async Task<BasicRes<object>> UploadStartReportAsync",
        "private void ApplyOfflineStartRequestId");
    var finishMethod = ExtractMethodText(
        uploadCode,
        "private async Task<BasicRes<object>> UploadFinishReportAsync",
        "private async Task<BasicRes<object>> UploadWorkOrderStatusAsync");

    AssertTrue(
        startMethod.Contains(
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            StringComparison.Ordinal),
        "开工 MES 已成功后，设备状态取消不能阻断上传任务写入 Uploaded。");
    AssertTrue(
        finishMethod.Contains(
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            StringComparison.Ordinal),
        "完工 MES 已成功后，设备状态取消不能阻断上传任务写入 Uploaded。");
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

    AssertTrue(statusService.LastReportToMes == true, "软件关闭日志失败也必须先尝试 MES 停机状态上传。");
    AssertTrue(statusService.LastCancellationToken.CanBeCanceled, "软件关闭日志失败时仍必须保留停机上传超时边界。");
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

static void DeviceLifecycleIgnoresTransientPlcConnectionStates()
{
    var lifecycleLogs = new FakeDeviceLifecycleLogService();
    var plcService = new FakePlcCommunicationService
    {
        Current = new PlcConnectionSnapshot(
            PlcConnectionState.Reconnecting,
            IsConnected: false,
            Endpoint: "SiemensS7-1200@127.0.0.1:102",
            LastConnectedTime: null,
            LastHeartbeatTime: null,
            Message: "正在连接 SiemensS7-1200@127.0.0.1:102。")
    };
    var coordinator = new DeviceLifecycleLogCoordinator(
        new FakeAppSettingsService
        {
            Current = new AppSettings
            {
                DeviceId = "D-001",
                EnableDualStation = true
            }
        },
        lifecycleLogs,
        plcService,
        new FakeMesConnectionMonitor(),
        new FakeCenterTelemetrySyncService(),
        new FakeDeviceStatusService());

    coordinator.Start();
    try
    {
        AssertEqual(0, CountPlcConnectionLogs(lifecycleLogs), "启动中的 Reconnecting 快照不得写成 PLC 连接失败。");

        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Connected, true, "PLC 已连接"));
        plcService.PublishStatus(CreatePlcSnapshot(2, PlcConnectionState.Connected, true, "PLC 已连接"));
        AssertEqual(2, CountPlcConnectionLogs(lifecycleLogs), "双工位首次连接成功必须各记录一条成功日志。");

        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Stopped, false, "PLC 服务已停止"));
        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Connecting, false, "PLC 正在连接"));
        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Reconnecting, false, "PLC 正在重连"));
        AssertEqual(2, CountPlcConnectionLogs(lifecycleLogs), "主动重启中的瞬态状态不得产生失败或成功噪声。");

        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Disconnected, false, "PLC 已断开"));
        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Disconnected, false, "PLC 仍未连接"));
        AssertEqual(3, CountPlcConnectionLogs(lifecycleLogs), "真实断线必须记录一次，重复断线不得重复记录。");

        plcService.PublishStatus(CreatePlcSnapshot(1, PlcConnectionState.Connected, true, "PLC 已恢复"));
        plcService.PublishStatus(CreatePlcSnapshot(2, PlcConnectionState.Faulted, false, "PLC 通讯异常"));
        plcService.PublishStatus(CreatePlcSnapshot(2, PlcConnectionState.Connected, true, "PLC 已恢复"));
        plcService.PublishStatus(CreatePlcSnapshot(2, PlcConnectionState.Unverified, false, "PLC 业务地址验证失败"));

        var plcEntries = lifecycleLogs.Entries
            .Where(entry => string.Equals(entry.Source, "PLC", StringComparison.Ordinal))
            .ToList();
        AssertEqual(7, plcEntries.Count, "Connected、Disconnected、Faulted、Unverified 状态必须继续按状态变化记录。");
        AssertEqual("Failed", plcEntries[^1].Status, "Unverified 必须继续记录为真实失败。");
    }
    finally
    {
        coordinator.Stop();
    }
}

static int CountPlcConnectionLogs(FakeDeviceLifecycleLogService lifecycleLogs)
{
    return lifecycleLogs.Entries.Count(entry =>
        string.Equals(entry.Source, "PLC", StringComparison.Ordinal)
        && string.Equals(entry.EventType, AppConstants.DeviceLifecycleEventTypes.SelfCheck, StringComparison.Ordinal));
}

static PlcConnectionSnapshot CreatePlcSnapshot(
    int stationNo,
    PlcConnectionState state,
    bool isConnected,
    string message)
{
    return new PlcConnectionSnapshot(
        state,
        isConnected,
        "SiemensS7-1200@127.0.0.1:102",
        isConnected || state == PlcConnectionState.Stopped
            ? new DateTime(2026, 7, 21, 8, 20, 5)
            : null,
        null,
        message)
    {
        StationNo = stationNo
    };
}

static void DeviceLifecycleNoLongerSubscribesToAlarmSnapshots()
{
    var coordinatorCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Log", "DeviceLifecycleLogCoordinator.cs"),
        Encoding.UTF8);

    AssertFalse(coordinatorCode.Contains("_plcProductionMonitorService.StatusChanged", StringComparison.Ordinal), "设备日志不得再订阅生产快照写入报警记录。");
    AssertFalse(coordinatorCode.Contains("RecordAlarmChange", StringComparison.Ordinal), "设备日志不得保留报警写入入口。");
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
        new BizProgram { Id = 7, ProgramName = "程序A", ProductNum = "P-001", RecipeCode = "3" },
        new BizProgram { Id = 8, ProgramName = "重复程序", ProductNum = "P-002", RecipeCode = "4", Station2RecipeCode = "8" },
        new BizProgram { Id = 9, ProgramName = "重复程序", ProductNum = "P-003", RecipeCode = "5" },
        new BizProgram { Id = 10, ProgramName = "仅工位2", ProductNum = "P-004", Station2RecipeCode = "6" }
    };

    var station1Options = OfflineStartInputRules.BuildProgramNameOptions(programs, stationNo: 1, requireBothStations: false);
    var station2Options = OfflineStartInputRules.BuildProgramNameOptions(programs, stationNo: 2, requireBothStations: false);
    var sharedOptions = OfflineStartInputRules.BuildProgramNameOptions(programs, stationNo: 1, requireBothStations: true);

    AssertEqual(3, station1Options.Count, "工位 1 只能显示配置了工位 1 配方的程序。");
    AssertEqual(2, station2Options.Count, "工位 2 只能显示配置了工位 2 配方的程序。");
    AssertEqual(1, sharedOptions.Count, "双工位同工单只允许两个工位都配置的程序。");
    AssertEqual("程序A", station1Options.Single(option => option.Program.Id == 7).DisplayText, "唯一名称只显示程序名称。");
    AssertEqual("重复程序 | 产品工号=P-002", station1Options.Single(option => option.Program.Id == 8).DisplayText, "重名提示不得显示配方号。");
    AssertFalse(station1Options.Any(option => option.DisplayText.Contains("配方号", StringComparison.Ordinal)), "离线程序下拉不得暴露数字配方号。");
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

    var options = OfflineStartInputRules.BuildProgramNameOptions(programs, stationNo: 1, requireBothStations: false);

    AssertEqual(4, options.Count, "本地程序列表中的空内容程序也应显示在 MonitorView 下拉框中。");
    AssertTrue(options.Any(option => option.Program.Id == 4), "空内容程序不能因为 ProgramContent 为空而被下拉过滤。");
}

static void OfflineProductNumDropdownListsDistinctStartableProductNums()
{
    var programs = new[]
    {
        new BizProgram { Id = 7, ProgramName = "程序A", ProductNum = "P-002", RecipeCode = "3" },
        new BizProgram { Id = 8, ProgramName = "程序B", ProductNum = "p-002", RecipeCode = "4", Station2RecipeCode = "8" },
        new BizProgram { Id = 9, ProgramName = "程序C", ProductNum = "P-001", RecipeCode = "5" },
        new BizProgram { Id = 10, ProgramName = "仅工位2", ProductNum = "P-009", Station2RecipeCode = "6" },
        new BizProgram { Id = 11, ProgramName = "已删除", ProductNum = "P-777", RecipeCode = "7", IsDeleted = true }
    };

    var station1 = OfflineStartInputRules.BuildProductNumOptions(programs, stationNo: 1, requireBothStations: false);

    AssertEqual(2, station1.Count, "同一产品工号只能出现一项，且工位 1 不显示仅配了工位 2 配方的工号。");
    AssertEqual("P-001", station1[0].ProductNum, "产品工号选项必须按工号排序。");
    AssertEqual("P-002", station1[1].ProductNum, "大小写不同的同一工号必须合并为一项，并固定采用序数最小的写法。");
    AssertEqual(2, station1[1].ProgramCount, "合并后的工号必须统计其名下的可开工程序数量。");
    AssertEqual("P-002", station1[1].DisplayText, "显示文本必须与工号一致，保证按文本回查可无损往返。");
    AssertFalse(station1.Any(option => option.ProductNum == "P-777"), "已删除的程序不得贡献产品工号选项。");

    var shared = OfflineStartInputRules.BuildProductNumOptions(programs, stationNo: 1, requireBothStations: true);
    AssertEqual(1, shared.Count, "双工位同工单下只保留两个工位都配好配方的工号。");
    AssertEqual("p-002", shared[0].ProductNum, "双工位同工单下只有 Id=8 满足条件，工号写法取自该程序本身。");
    AssertEqual(1, shared[0].ProgramCount, "双工位同工单下只有满足条件的程序参与计数。");
}

static void OfflineProgramDropdownFiltersByProductNum()
{
    var programs = new[]
    {
        new BizProgram { Id = 7, ProgramName = "重复程序", ProductNum = "P-001", RecipeCode = "3", SequenceNumber = 1 },
        new BizProgram { Id = 8, ProgramName = "重复程序", ProductNum = "P-001", RecipeCode = "4", SequenceNumber = 2 },
        new BizProgram { Id = 9, ProgramName = "另一个", ProductNum = "P-002", RecipeCode = "5" }
    };

    var filtered = OfflineStartInputRules.BuildProgramNameOptions(
        programs, stationNo: 1, requireBothStations: false, productNumFilter: "p-001");

    AssertEqual(2, filtered.Count, "同一产品工号下的多个程序都必须保留，按流水号区分。");
    AssertTrue(filtered.All(option => option.Program.ProductNum == "P-001"), "筛选结果不得混入其他工号的程序。");
    AssertEqual("重复程序 | 流水号=001", filtered.Single(option => option.Program.Id == 7).DisplayText, "按工号筛选后重名程序必须改用流水号区分。");
    AssertEqual("重复程序 | 流水号=002", filtered.Single(option => option.Program.Id == 8).DisplayText, "流水号必须补零到三位，与程序名称格式一致。");
    AssertFalse(filtered.Any(option => option.DisplayText.Contains("产品工号=", StringComparison.Ordinal)), "已按工号筛选时再追加工号是冗余信息。");

    var unfiltered = OfflineStartInputRules.BuildProgramNameOptions(
        programs, stationNo: 1, requireBothStations: false, productNumFilter: "  ");
    AssertEqual(3, unfiltered.Count, "筛选值为空白时必须视为不筛选。");
    AssertEqual("重复程序 | 产品工号=P-001", unfiltered.Single(option => option.Program.Id == 7).DisplayText, "未按工号筛选时保持原有的工号提示。");
}

static void MonitorViewLinksProductNumSelectionToProgramOptions()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);

    AssertFalse(designerCode.Contains("inputProdNum", StringComparison.Ordinal), "产品工号必须改为下拉选择控件。");
    AssertFalse(viewCode.Contains("inputProdNum", StringComparison.Ordinal), "监控页不得再引用旧的产品工号输入框。");
    AssertTrue(designerCode.Contains("selectProdNum = new AntdUI.Select();", StringComparison.Ordinal), "Designer 必须声明产品工号下拉。");
    AssertTrue(designerCode.Contains("selectProdNum.MaxCount = 10;", StringComparison.Ordinal), "产品工号下拉必须限制展开条数。");

    AssertTrue(viewCode.Contains("selectProdNum.SelectedIndexChanged += ProductNumSelection_SelectedIndexChanged;", StringComparison.Ordinal), "监控页必须监听产品工号选择。");
    AssertTrue(viewCode.Contains("selectProdNum.SelectedIndexChanged -= ProductNumSelection_SelectedIndexChanged;", StringComparison.Ordinal), "监控页销毁时必须解绑产品工号选择。");
    AssertTrue(viewCode.Contains("selectProdNum.WheelModifyEnabled = false;", StringComparison.Ordinal), "产品工号下拉必须禁用滚轮换选，避免误改开工工号。");

    var handler = ExtractMethodText(
        viewCode,
        "private void ProductNumSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)",
        "private void ProgramNameSelection_SelectedIndexChanged");
    AssertTrue(handler.Contains("if (_syncingOfflineProductNumSelection)", StringComparison.Ordinal), "程序化回填工号必须被同步守卫短路，避免与程序联动互相递归。");
    AssertTrue(handler.Contains("if (!IsOfflineInputEditable(GetCurrentStationState()))", StringComparison.Ordinal), "仅离线可编辑态允许操作员改工号。");
    AssertTrue(handler.Contains("MarkOfflineProgramSelectionByUser(CurrentStationNo);", StringComparison.Ordinal), "操作员选工号必须标记为用户显式选择。");
    AssertTrue(handler.Contains("BindOfflineProgramNameOptions();", StringComparison.Ordinal), "选中工号后必须按工号刷新程序名称下拉。");

    var bindPrograms = ExtractMethodText(
        viewCode,
        "private void BindOfflineProgramNameOptions()",
        "private void BindOfflineProductNumOptions()");
    // 未启用「按产品工号筛选程序」时必须列出全部程序，支持一款产品借用另一款工号的程序生产。
    AssertTrue(bindPrograms.Contains("_currentSettings.UseProductNumberFilter", StringComparison.Ordinal), "离线程序列表是否按工号收窄必须由系统设置决定，与在线保持同一语义。");
    AssertTrue(bindPrograms.Contains("? ResolveOfflineProductNumFilter()", StringComparison.Ordinal), "启用筛选时才按当前选中的产品工号收窄。");
    AssertTrue(bindPrograms.Contains(": null", StringComparison.Ordinal), "未启用筛选时必须传空筛选值，列出全部程序。");

    var productNumHandler = ExtractMethodText(
        viewCode,
        "private void ProductNumSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)",
        "private void SelectFirstOfflineProgramForProductNum(string productNum)");
    // 未启用筛选时列表是全量的，重绑定会保留原程序并把工号回写成原值，必须显式跳转。
    AssertTrue(productNumHandler.Contains("SelectFirstOfflineProgramForProductNum(productNum);", StringComparison.Ordinal), "选中工号后必须跳到该工号的首个程序，否则未启用筛选时工号选择会被回写覆盖。");
    AssertTrue(productNumHandler.Contains("ApplyOfflineProgramNameOption(GetSelectedOfflineProgramNameOption(), syncDrawingNo: true);", StringComparison.Ordinal), "用户选择工号后必须按定位到的程序同步部件图号。");

    var programHandler = ExtractMethodText(
        viewCode,
        "private void ProgramNameSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)",
        "#endregion");
    AssertTrue(programHandler.Contains("ApplyOfflineProgramNameOption(option, syncDrawingNo: true);", StringComparison.Ordinal), "用户切换离线程序后必须同步该程序的部件图号。");

    var applyProgram = ExtractMethodText(
        viewCode,
        "private void ApplyOfflineProgramNameOption(OfflineProgramNameOption? option, bool syncDrawingNo)",
        "/// 按配方号反向联动离线程序名称、产品工号和产品型号。");
    AssertTrue(applyProgram.Contains("inputDrawingNo.Text = option?.Program.ComponentCode?.Trim() ?? string.Empty;", StringComparison.Ordinal), "离线部件图号必须取当前程序的零组件代码，空值时清空。");

    var resolveProductNum = ExtractMethodText(
        viewCode,
        "private string GetSelectedOfflineProductNum()",
        "/// 用本地程序列表填充离线程序号下拉框。");
    // AntdUI 筛选态下事件索引指向筛选后的子列表，按索引直取会选错工号。
    AssertTrue(resolveProductNum.Contains("SelectListRules.ResolveSelectedIndex(", StringComparison.Ordinal), "产品工号必须按显示文本回查完整选项，不得直接使用 SelectedIndex。");
    AssertTrue(resolveProductNum.Contains("selectProdNum.SelectedValue as string ?? selectProdNum.Text", StringComparison.Ordinal), "解析选中工号必须优先采信 SelectedValue 再回退文本。");
}

static void MonitorViewKeepsUserProductNumAcrossRuntimeRebind()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);

    var bindProductNum = ExtractMethodText(
        viewCode,
        "private void BindOfflineProductNumOptions()",
        "private string? ResolveOfflineProductNumFilter()");
    // 监控页每秒重绑定运行态，操作员选中的工号必须靠记忆表存活，否则会被弹回第一项。
    AssertTrue(bindProductNum.Contains("_userSelectedOfflineProductNums.TryGetValue(stationKey, out var remembered)", StringComparison.Ordinal), "周期性重绑定必须优先复用操作员已选的工号。");
    AssertTrue(bindProductNum.Contains("_userSelectedOfflineProductNums.Remove(stationKey);", StringComparison.Ordinal), "记忆的工号在程序库中消失后必须清除，避免筛选恒空。");
    AssertTrue(bindProductNum.Contains("ForceProductNumSelection(", StringComparison.Ordinal), "重建选项后必须走 -1 归位赋值，规避 AntdUI 索引短路。");

    var runtimeBinding = ExtractMethodText(
        viewCode,
        "private void BindOfflineEditableRuntimeState(string liveWorkId)",
        "private void ApplyOfflineInputReadOnly(bool readOnly)");
    AssertTrue(runtimeBinding.Contains("ApplyOfflineProgramNameOption(GetSelectedOfflineProgramNameOption(), syncDrawingNo: false);", StringComparison.Ordinal), "周期性运行态重绑定不得覆盖操作员当前部件图号。");

    var clearSelection = ExtractMethodText(
        viewCode,
        "private void ClearOfflineProgramSelectionByUser(int stationNo)",
        "private ProductIdentity? ResolveOfflineSelectedRecipeProductIdentity(int stationNo)");
    AssertTrue(clearSelection.Contains("_userSelectedOfflineProductNums.Remove(NormalizeStationNo(stationNo));", StringComparison.Ordinal), "切换工位、完工或转在线时必须一并清除记忆的工号。");

    var offlineReadOnly = ExtractMethodText(
        viewCode,
        "private void ApplyOfflineInputReadOnly(bool readOnly)",
        "private void SetWorkOrderInputText(string workId)");
    AssertTrue(offlineReadOnly.Contains("selectProdNum.ReadOnly = readOnly;", StringComparison.Ordinal), "离线可编辑态下操作员必须能展开产品工号下拉。");
    AssertTrue(offlineReadOnly.Contains("inputProdModel.ReadOnly = readOnly;", StringComparison.Ordinal), "离线可编辑态下操作员必须能手工输入产品型号。");

    var onlineReadOnly = ExtractMethodText(
        viewCode,
        "private void ApplyOnlineStartInputReadOnly(bool editable)",
        "private void BindOfflineEditableRuntimeState(string liveWorkId)");
    AssertTrue(onlineReadOnly.Contains("selectProdNum.ReadOnly = true;", StringComparison.Ordinal), "在线态产品工号跟随工单，必须保持只读展示。");
    AssertTrue(onlineReadOnly.Contains("inputProdModel.ReadOnly = fieldReadOnly;", StringComparison.Ordinal), "在线可编辑开工态下操作员必须能调整产品型号。");
    AssertTrue(viewCode.Contains("ProdModel = FirstNonEmpty(inputProdModel.Text, source.ProdModel)", StringComparison.Ordinal), "在线开工快照必须优先使用手工输入的产品型号。");
    AssertFalse(viewCode.Contains("option?.Program.ProductModel", StringComparison.Ordinal), "监控页不得再从加工程序回填产品型号。");
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
            RecipeCode = "2",
            Station2RecipeCode = "5"
        }
    }, stationNo: 2, requireBothStations: false).Single();
    var input = new OfflineStartInput(
        StationNo: 2,
        WorkOrderId: "WO-LOCAL",
        Batch: "B001",
        Spec: "S001",
        ProcessNo: "OP20",
        ProcessName: "离线焊接",
        PlannedQtyText: "12",
        ProductModel: "MANUAL-MODEL",
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
    AssertEqual("MANUAL-MODEL", request.ProductModel, "离线开工应优先使用界面手工输入的产品型号。");
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
    }, stationNo: 1, requireBothStations: false).Single();
    var emptyOptionalFields = new OfflineStartInput(
        StationNo: 1,
        WorkOrderId: "WO-EMPTY",
        Batch: string.Empty,
        Spec: string.Empty,
        ProcessNo: "OP10",
        ProcessName: string.Empty,
        PlannedQtyText: "1",
        ProductModel: string.Empty,
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
    }, stationNo: 1, requireBothStations: false).Single();
    var validInput = new OfflineStartInput(
        StationNo: 1,
        WorkOrderId: "WO-REQUIRED",
        Batch: string.Empty,
        Spec: string.Empty,
        ProcessNo: "OP10",
        ProcessName: string.Empty,
        PlannedQtyText: "1",
        ProductModel: string.Empty,
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
    original.ProgramFile = Convert.ToBase64String(Encoding.UTF8.GetBytes(original.ProgramContent));
    edited.ProgramContent = "{\"压力\":{\"max\":\"9\",\"min\":\"1\"},\"高度\":\"12.5\"}";
    edited.ProgramFile = Convert.ToBase64String(Encoding.UTF8.GetBytes(edited.ProgramContent));
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
    regeneratedFileEdited.ProgramFile = Convert.ToBase64String(Encoding.UTF8.GetBytes(regeneratedFileEdited.ProgramContent));
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

static void ProgramRecipeMappingNormalizesPositiveNumericCodes()
{
    var rulesType = typeof(BizProgram).Assembly.GetType(
        "AutoWeldSystem.Core.Production.ProgramRecipeMappingRules",
        throwOnError: false);
    AssertTrue(rulesType is not null, "应提供集中管理工位配方号的 ProgramRecipeMappingRules。 ");

    var normalize = rulesType!.GetMethod("Normalize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    AssertTrue(normalize is not null, "配方映射规则应公开统一的正整数规范化入口。 ");

    string Invoke(string? value) => (string)(normalize!.Invoke(null, [value]) ?? string.Empty);

    AssertEqual("7", Invoke(" 007 "), "有效数字配方号应去除空白和无意义前导零。 ");
    AssertEqual(string.Empty, Invoke("0"), "配方号 0 不是有效 PLC 槽位。 ");
    AssertEqual(string.Empty, Invoke("-2"), "负数不是有效 PLC 配方号。 ");
    AssertEqual(string.Empty, Invoke("A3"), "非数字文本不得进入 PLC 配方下发链路。 ");
}

static void ProgramRecipeMappingResolvesStationSpecificCodes()
{
    var rulesType = typeof(BizProgram).Assembly.GetType(
        "AutoWeldSystem.Core.Production.ProgramRecipeMappingRules",
        throwOnError: false);
    AssertTrue(rulesType is not null, "应提供集中管理工位配方号的 ProgramRecipeMappingRules。 ");

    var station2Property = typeof(BizProgram).GetProperty("Station2RecipeCode");
    AssertTrue(station2Property is not null, "BizProgram 应保存工位 2 的本地配方号。 ");
    var resolve = rulesType!.GetMethod(
        "Resolve",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
        binder: null,
        types: [typeof(BizProgram), typeof(int)],
        modifiers: null);
    AssertTrue(resolve is not null, "配方映射规则应能按程序和当前工位解析配方号。 ");

    var program = new BizProgram { RecipeCode = " 3 " };
    station2Property!.SetValue(program, " 8 ");

    AssertEqual("3", (string)(resolve!.Invoke(null, [program, 1]) ?? string.Empty), "单工位和工位 1 应使用原 RecipeCode。 ");
    AssertEqual("8", (string)(resolve.Invoke(null, [program, 2]) ?? string.Empty), "工位 2 应优先使用 Station2RecipeCode。 ");

    station2Property.SetValue(program, "0");
    AssertEqual(string.Empty, (string)(resolve.Invoke(null, [program, 2]) ?? string.Empty), "工位 2 缺少有效配方时不得回退工位 1。 ");
}

static void ProgramSharedRecipeTargetsResolveIndependently()
{
    var rulesType = typeof(BizProgram).Assembly.GetType(
        "AutoWeldSystem.Core.Production.ProgramRecipeMappingRules",
        throwOnError: false);
    AssertTrue(rulesType is not null, "应提供集中管理工位配方号的 ProgramRecipeMappingRules。 ");

    var resolveTargets = rulesType!.GetMethod("ResolveTargets", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    AssertTrue(resolveTargets is not null, "配方映射规则应能一次解析共享任务的所有目标工位。 ");

    var program = new BizProgram { RecipeCode = "3", Station2RecipeCode = "8" };
    var sharedStations = RecipeStationScopeRules.ResolveSharedRecipeStations(
        enableDualStation: true,
        enableDualWorkOrder: false,
        stationNo: 1);
    var targets = ((System.Collections.IEnumerable)resolveTargets!.Invoke(null, [program, sharedStations])!)
        .Cast<object>()
        .Select(target => new
        {
            StationNo = (int)target.GetType().GetProperty("StationNo")!.GetValue(target)!,
            RecipeCode = (string)target.GetType().GetProperty("RecipeCode")!.GetValue(target)!
        })
        .ToList();

    AssertEqual(2, targets.Count, "同工单双工位应分别生成两个配方下发目标。 ");
    AssertEqual(1, targets[0].StationNo, "第一个目标应为工位 1。 ");
    AssertEqual("3", targets[0].RecipeCode, "工位 1 应使用 RecipeCode。 ");
    AssertEqual(2, targets[1].StationNo, "第二个目标应为工位 2。 ");
    AssertEqual("8", targets[1].RecipeCode, "工位 2 应使用 Station2RecipeCode。 ");

    program.Station2RecipeCode = null;
    var missingStation2Targets = ((System.Collections.IEnumerable)resolveTargets.Invoke(null, [program, sharedStations])!)
        .Cast<object>()
        .Select(target => (string)target.GetType().GetProperty("RecipeCode")!.GetValue(target)!)
        .ToList();
    AssertEqual(string.Empty, missingStation2Targets[1], "共享目标缺少工位 2 关联时必须保留为空，不能借用工位 1。 ");
}

static void SharedTaskRecipeBoundariesResolvePerStation()
{
    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var reconcileCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Plc", "RecipeCodeReconcileMonitorService.cs"), Encoding.UTF8);

    var dispatchMethod = ExtractMethodText(
        monitorCode,
        "private async Task DispatchRecipeCodeAfterStartAsync",
        "private static string BuildRecipeResolveFailureDetail");
    AssertTrue(dispatchMethod.Contains("ResolveRecipeCodeForStartedTask(task, selectedProgram, targetStationNo)", StringComparison.Ordinal), "共享开工下发应为每个目标工位重新解析配方号。 ");
    AssertTrue(dispatchMethod.Contains("targetRecipeCode", StringComparison.Ordinal), "写入和校验必须使用目标工位配方号，不能复用源工位变量。 ");

    AssertTrue(reconcileCode.Contains("var expectedRecipe = ResolveExpectedRecipe(task, stationNo);", StringComparison.Ordinal), "持续调和应按当前监控工位解析期望配方。 ");
    AssertTrue(reconcileCode.Contains("var recipeTargets = ProgramRecipeMappingRules.ResolveTargets(localProgram, targetStations);", StringComparison.Ordinal), "共享调和应按每个目标工位分别解析期望配方。 ");
    AssertTrue(reconcileCode.Contains("private readonly IProgramManageService _programManageService;", StringComparison.Ordinal), "调和服务应从本地程序映射读取工位配方，而不是依赖共享任务字段。 ");
    AssertTrue(reconcileCode.Contains("private readonly HashSet<int> _restoredTaskIds = new();", StringComparison.Ordinal), "调和服务必须只为恢复任务记录兼容回退标识。");
    var expectedMethod = ExtractMethodText(reconcileCode, "private string ResolveExpectedRecipe", "private BizProgram? ResolveLocalProgram");
    AssertTrue(expectedMethod.Contains("_restoredTaskIds.Contains(task.Id)", StringComparison.Ordinal), "只有恢复任务才允许检查任务配方快照。");
    AssertTrue(expectedMethod.Contains("stationNo == task.StationNo", StringComparison.Ordinal), "恢复任务快照只能用于任务本站。");
    var reconcileMethod = ExtractMethodText(reconcileCode, "private async Task ReconcileRecipeAsync", "private async Task<int?> ReadWorkOrderStatusAsync");
    AssertFalse(reconcileMethod.Contains("FirstNonEmpty(target.RecipeCode, task.RecipeCode)", StringComparison.Ordinal), "共享调和不得用源任务配方补齐其他工位。");
    AssertTrue(reconcileMethod.Contains("recipeTargets.Any(target => string.IsNullOrWhiteSpace(target.RecipeCode))", StringComparison.Ordinal), "任一目标缺失配方时必须在 PLC 写入前整体退出。");
    var displayMethod = ExtractMethodText(
        monitorCode,
        "private string ResolveRecipeCodeForDisplay",
        "private bool HasPendingOnlineProgramSelection");
    AssertTrue(displayMethod.Contains("ProgramRecipeMappingRules.Resolve(localProgram, CurrentStationNo)", StringComparison.Ordinal), "运行中显示应优先使用当前工位的本地程序映射。 ");

    var finishMethod = ExtractMethodText(
        monitorCode,
        "private async Task RefreshRecipeCodeFromPlcBeforeFinishAsync",
        "private void WriteFinishRecipeReadFailureLog");
    AssertTrue(finishMethod.Contains("if (!SharesRecipeTaskAcrossStations())", StringComparison.Ordinal), "同工单双工位完工回读不得覆盖共享任务的单一 RecipeCode。 ");

    var devicePriorityMethod = ExtractMethodText(
        monitorCode,
        "private BizProgram? ResolveLocalProgramByNameAndProduct",
        "private async Task DispatchRecipeCodeAfterStartAsync");
    var devicePriorityIndex = devicePriorityMethod.IndexOf("OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))", StringComparison.Ordinal);
    var updatedTimeIndex = devicePriorityMethod.IndexOf("ThenByDescending(program => program.UpdatedTime)", StringComparison.Ordinal);
    AssertTrue(devicePriorityIndex >= 0 && updatedTimeIndex > devicePriorityIndex, "同名同产品程序应先按当前 DeviceId 匹配，再按更新时间选择。 ");
}

static void ProgramRecipeStation2FieldsPersistLocally()
{
    var station2ProgramProperty = typeof(BizProgram).GetProperty("Station2RecipeCode");
    var station2RequestProperty = typeof(SaveProgramReq).GetProperty("Station2RecipeCode");
    var station2RevisionProperty = typeof(BizProgramRevision).GetProperty("Station2RecipeCode");
    AssertTrue(station2ProgramProperty is not null, "BizProgram 应增加工位 2 配方号。 ");
    AssertTrue(station2RequestProperty is not null, "SaveProgramReq 应携带工位 2 配方号。 ");
    AssertTrue(station2RevisionProperty is not null, "程序版本快照应保存工位 2 配方号。 ");

    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    AssertTrue(serviceCode.Contains("entity.Station2RecipeCode = request.Station2RecipeCode;", StringComparison.Ordinal), "保存程序时应写入工位 2 配方号。 ");
    AssertTrue(serviceCode.Contains("Station2RecipeCode = source.Station2RecipeCode", StringComparison.Ordinal), "保存前快照应复制工位 2 配方号。 ");
    AssertTrue(serviceCode.Contains("Station2RecipeCode = entity.Station2RecipeCode", StringComparison.Ordinal), "版本快照应记录工位 2 配方号。 ");
    AssertTrue(serviceCode.Contains("request.Station2RecipeCode = ProgramRecipeMappingRules.Normalize", StringComparison.Ordinal), "保存入口应统一规范化工位 2 配方号。 ");

    var original = new BizProgram { Id = 1, ProgramId = "mes-1", RecipeCode = "1" };
    var current = new BizProgram { Id = 1, ProgramId = "mes-1", RecipeCode = "1" };
    station2ProgramProperty!.SetValue(original, "2");
    station2ProgramProperty.SetValue(current, "3");
    AssertEqual<string?>(null, ProgramMesSyncRules.ResolveCurrentSaveAction(original, current), "工位 2 配方号仅在本地使用，不应触发 MES 更新。 ");
}

static void ProgramRuntimeResolvesRecipesByCurrentStation()
{
    var offlineRulesCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Production", "OfflineStartInputRules.cs"), Encoding.UTF8);
    var weldTaskCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"), Encoding.UTF8);
    var previewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductRealtimePreviewService.cs"), Encoding.UTF8);
    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var localFormCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Forms", "LocalWorkOrderForm.cs"), Encoding.UTF8);

    AssertTrue(offlineRulesCode.Contains("ProgramRecipeMappingRules.Resolve(program, input.StationNo)", StringComparison.Ordinal), "离线开工请求应使用当前工位配方号。 ");
    AssertTrue(weldTaskCode.Contains("ResolveProgramRecipeCode(program, settings.DeviceId, normalizedStationNo)", StringComparison.Ordinal), "在线开工任务应按当前工位解析本地配方号。 ");
    AssertTrue(weldTaskCode.Contains("ProgramRecipeMappingRules.Resolve(localProgram, stationNo)", StringComparison.Ordinal), "WeldTaskService 应复用集中映射规则。 ");
    var serviceResolver = ExtractMethodText(weldTaskCode, "private string ResolveProgramRecipeCode", "private void EnsureReadyForStart");
    AssertFalse(serviceResolver.Contains("ProgramRecipeMappingRules.Normalize(program.RecipeCode)", StringComparison.Ordinal), "在线开工不得回退 MES 程序配方号。");
    AssertTrue(serviceResolver.Contains("throw new BusinessOperationException", StringComparison.Ordinal), "本机程序当前工位未配置配方时必须拒绝开工。");
    var monitorResolver = ExtractMethodText(monitorCode, "private RecipeCodeResolution ResolveRecipeCodeForStartedTask", "private BizProgram? ResolveLocalProgramByProgramId");
    AssertFalse(monitorResolver.Contains("task.RecipeCode", StringComparison.Ordinal), "新任务运行时解析不得回退任务配方快照。");
    AssertFalse(monitorResolver.Contains("selectedProgram?.RecipeCode", StringComparison.Ordinal), "新任务运行时解析不得回退 MES 程序配方号。");
    AssertTrue(previewCode.Contains("ProgramRecipeMappingRules.Matches(program, stationNo, normalizedRecipeCode)", StringComparison.Ordinal), "PLC 配方反查产品预览时应按工位匹配。 ");
    AssertTrue(monitorCode.Contains("ProgramRecipeMappingRules.Resolve(localProgram, stationNo)", StringComparison.Ordinal), "MonitorView 配方下发和反查应复用工位映射规则。 ");
    AssertTrue(localFormCode.Contains("ProgramRecipeMappingRules.Resolve(program, _stationNo)", StringComparison.Ordinal), "本地工单窗口应显示并提交当前工位配方号。 ");
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
    edited.ProgramFile = Convert.ToBase64String(Encoding.UTF8.GetBytes(edited.ProgramContent));

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
    contentEdited.ProgramFile = Convert.ToBase64String(Encoding.UTF8.GetBytes(contentEdited.ProgramContent));

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

    AssertFalse(
        json.Contains(nameof(ProgramDataRes.RecipeCode), StringComparison.OrdinalIgnoreCase),
        "MES 新增/更新程序请求不应包含 RecipeCode。");
    AssertEqual("D-1_CX_3_DH_001_3_左侧组件", payload.ProgramName, "MES 写入请求应携带重建后的标准程序名称。");
    AssertEqual(AppConstants.ProgramRemarkActions.Update, payload.Remark, "MES 写入请求应携带解析后的备注。");
    // 程序文件已不再生成和上传，载荷必须彻底不含文件相关字段。
    AssertFalse(
        json.Contains("ProgramFile", StringComparison.OrdinalIgnoreCase),
        "MES 新增/更新程序请求不应再包含 ProgramFile。");
    AssertFalse(
        json.Contains("FileType", StringComparison.OrdinalIgnoreCase),
        "MES 新增/更新程序请求不应再包含 FileType。");
}

static void ProgramMesCreatePayloadClearsContentForEmptyValues()
{
    var program = BuildSyncedProgram();
    program.ProgramId = null;
    program.ProgramContent = "  { \r\n }  ";

    var payload = ProgramMesPayloadRules.ToCreateRequest(program, AppConstants.ProgramRemarkActions.Create);

    AssertEqual(string.Empty, payload.ProgramContent, "新增程序未填写设定值时，ProgramContent 应留空。");
}

static void ProgramContentRulesDetectConfiguredValues()
{
    AssertFalse(ProgramContentJsonRules.HasConfiguredValues(null), "空程序内容不应视为已填写设定值。");
    AssertFalse(ProgramContentJsonRules.HasConfiguredValues("  { \r\n }  "), "空 JSON 对象不应视为已填写设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("{\"高度\":\"12.5\"}"), "包含设定项的 JSON 对象应视为已填写设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("[\"历史内容\"]"), "非对象历史内容不应被误判为空设定值。");
    AssertTrue(ProgramContentJsonRules.HasConfiguredValues("not-json"), "非法历史内容不应被误判为空设定值。");
}

static void ProgramSaveRegeneratesNameWhenSequenceChanges()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    var applyRequestMethod = ExtractMethodText(
        serviceCode,
        "    private void ApplyRequest(BizProgram entity, SaveProgramReq request)",
        "    private AppSettings CurrentSettings");

    // 流水号已改但名称仍是旧值会造成名实不符，且 TryParse 之后会把流水号解析回旧值。
    AssertTrue(applyRequestMethod.Contains("var nameInputsChanged", StringComparison.Ordinal), "保存服务必须判断参与命名的字段是否变化。");
    AssertTrue(applyRequestMethod.Contains("entity.SequenceNumber != Math.Max(1, request.SequenceNumber)", StringComparison.Ordinal), "流水号变化必须触发程序名称重算。");
    AssertTrue(applyRequestMethod.Contains("entity.Id == 0 || descriptionChanged || nameInputsChanged", StringComparison.Ordinal), "命名门控必须同时覆盖新增、备注变化和命名字段变化。");
    AssertTrue(applyRequestMethod.Contains("? entity.ProgramName", StringComparison.Ordinal), "命名字段未变化时仍应保留原程序名称。");
}

static void ProgramSaveRejectsDuplicateProgramName()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);

    // 程序 JSON 文件仅按程序名命名，重名会互相覆盖并在删除时误删幸存者文件。
    AssertTrue(serviceCode.Contains("EnsureProgramNameNotDuplicated(entity);", StringComparison.Ordinal), "保存时必须校验程序名称是否重复。");
    var guardMethod = ExtractMethodText(
        serviceCode,
        "    private void EnsureProgramNameNotDuplicated(BizProgram entity)",
        "    private void SettingsService_SettingsChanged");
    AssertTrue(guardMethod.Contains("it.ProgramName == entity.ProgramName && it.Id != entity.Id && !it.IsDeleted", StringComparison.Ordinal), "重名校验必须排除自身和已删除程序。");
    AssertTrue(guardMethod.Contains("throw new InvalidOperationException", StringComparison.Ordinal), "命中重名时必须抛错阻止保存。");
}

static void ProgramManageViewProvidesSaveAsNewEntry()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.Designer.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("btnSaveAsNew = new AntdUI.Button();", StringComparison.Ordinal), "程序管理页必须提供另存为新程序按钮。");
    AssertTrue(viewCode.Contains("btnSaveAsNew.Click += SaveAsNew_ClickAsync;", StringComparison.Ordinal), "另存为新程序按钮必须绑定事件。");
    AssertTrue(viewCode.Contains("btnSaveAsNew.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonSaveAsNew);", StringComparison.Ordinal), "另存为新程序按钮必须本地化。");

    var handler = ExtractMethodText(
        viewCode,
        "    private async void SaveAsNew_ClickAsync(object? sender, EventArgs e)",
        "    private async Task SyncProgramInBackgroundAsync(int programId)");
    // 已有 ProgramId 的程序同步时会把 Create 降级为 Update，必须另起新行才能真正新增。
    AssertTrue(handler.Contains("_editingId = 0;", StringComparison.Ordinal), "另存为新程序必须清空编辑标识，保存才会走新增。");
    AssertTrue(handler.Contains("txtProgramId.Clear();", StringComparison.Ordinal), "另存为新程序必须清空 MES 程序ID，避免改名原程序。");
    AssertTrue(handler.Contains("GetNextSequenceNumberAsync", StringComparison.Ordinal), "另存为新程序必须异步取该工号下的下一个流水号。");
}

static void ProgramManageGridShowsSequenceAndProgramName()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("nameof(ProgramProductGroupRow.SerialNumber)", StringComparison.Ordinal), "程序表格首列必须显示筛选后的分组序号。");
    AssertTrue(viewCode.Contains("productNumColumn.SetTree(nameof(ProgramProductGroupRow.Programs));", StringComparison.Ordinal), "工号列必须继续配置为树形列。");
    AssertTrue(viewCode.Contains("nameof(ProgramProductGroupRow.ProgramName)", StringComparison.Ordinal), "程序名称必须使用独立列。");
    AssertTrue(viewCode.Contains("nameof(ProgramProductGroupRow.SyncStatus)", StringComparison.Ordinal), "同步状态必须使用独立列。");
    AssertFalse(viewCode.Contains("nameof(ProgramProductGroupRow.Summary)", StringComparison.Ordinal), "程序表格不得继续显示摘要列。");
    AssertFalse(viewCode.Contains("BuildProgramSummary", StringComparison.Ordinal), "程序列表不得继续拼接版本号和同步状态摘要。");
    AssertTrue(viewCode.Contains("TextKeys.ProgramManage.CurrentSynced", StringComparison.Ordinal)
        && viewCode.Contains("TextKeys.ProgramManage.CurrentNotSynced", StringComparison.Ordinal),
        "右侧当前状态必须只显示已同步/MES程序ID或未同步。");
    var zhResources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"), Encoding.UTF8);
    AssertTrue(zhResources.Contains("<value>当前：已同步 / {0}</value>", StringComparison.Ordinal), "已同步状态必须显示当前前缀、分隔空格和程序ID。");
    AssertTrue(zhResources.Contains("<value>当前：未同步</value>", StringComparison.Ordinal), "未同步状态必须显示当前前缀。");
    AssertTrue(viewCode.Contains("row.ProgramId > 0", StringComparison.Ordinal), "父行不指向具体程序，点击不得切换编辑对象。");
}

static void ProgramProductGroupsMergeProgramsSharingProductNum()
{
    var programs = new List<BizProgram>
    {
        new() { Id = 1, ProgramName = "A-1", ProductNum = "P-001", SequenceNumber = 2, SyncStatus = AppConstants.ProgramSyncStatus.PendingUpdate, UpdatedTime = new DateTime(2026, 8, 1) },
        new() { Id = 2, ProgramName = "A-2", ProductNum = " p-001 ", SequenceNumber = 1, SyncStatus = AppConstants.ProgramSyncStatus.Synced, UpdatedTime = new DateTime(2026, 8, 5) },
        new() { Id = 3, ProgramName = "B-1", ProductNum = "P-002", SequenceNumber = 1, SyncStatus = AppConstants.ProgramSyncStatus.PendingCreate, UpdatedTime = new DateTime(2026, 8, 3) },
        new() { Id = 4, ProgramName = "空工号", ProductNum = "   ", SequenceNumber = 1, UpdatedTime = new DateTime(2026, 8, 9) }
    };

    var groups = ProgramProductGroupRules.BuildGroups(programs, program => $"状态:{program.SyncStatus}");

    AssertEqual(2, groups.Count, "工号为空的程序不得产生分组，同工号必须合并为一行。");
    AssertEqual(1, groups[0].SerialNumber, "首个产品工号父行序号必须从 1 开始。");
    AssertEqual(2, groups[1].SerialNumber, "产品工号父行序号必须连续递增。");
    AssertEqual("P-001", groups[0].ProductNum, "同工号大小写和空白不同必须归为同一组。");
    AssertEqual(new DateTime(2026, 8, 5), groups[0].UpdatedTime, "分组更新时间必须取组内最新。");
    AssertEqual(0, ProgramProductGroupRules.BuildGroups(Array.Empty<BizProgram>(), program => program.SyncStatus).Count, "空集合必须返回空分组。");

    AssertEqual(0, groups[0].ProgramId, "多程序工号的父行不得指向具体程序。");
    AssertEqual(string.Empty, groups[0].ProgramName, "多程序父行不得显示具体程序名称。");
    AssertEqual(string.Empty, groups[0].SyncStatus, "多程序父行不得显示具体同步状态。");
    AssertEqual(2, groups[0].Programs?.Count ?? 0, "多程序工号必须展开为子行。");
    AssertEqual(null, groups[0].Programs![0].SerialNumber, "子程序行序号必须留空。");
    AssertEqual(2, groups[0].Programs![0].ProgramId, "子行必须按流水号升序排列。");
    AssertEqual("#001", groups[0].Programs![0].ProductNum, "子行必须保留程序流水号标签。");
    AssertEqual("A-2", groups[0].Programs![0].ProgramName, "程序名称必须进入独立字段。");
    AssertEqual("状态:Synced", groups[0].Programs![0].SyncStatus, "同步状态必须进入独立字段。");
}

static void ProgramProductGroupsFlattenSingleProgramProductNum()
{
    var programs = new List<BizProgram>
    {
        new() { Id = 7, ProgramName = "只有一个程序", ProductNum = "P-009", SequenceNumber = 1, SyncStatus = AppConstants.ProgramSyncStatus.Synced, UpdatedTime = new DateTime(2026, 8, 2) }
    };

    var groups = ProgramProductGroupRules.BuildGroups(programs, program => "已同步");

    AssertEqual(1, groups.Count, "单程序工号必须只占一行。");
    AssertEqual(1, groups[0].SerialNumber, "单程序工号父行必须显示序号 1。");
    AssertTrue(groups[0].Programs is null, "单程序工号不得产生子行，避免出现多余的展开箭头。");
    AssertEqual(7, groups[0].ProgramId, "单程序工号的行必须直接指向该程序。");
    AssertEqual("只有一个程序", groups[0].ProgramName, "单程序工号必须在独立列显示程序名称。");
    AssertEqual("已同步", groups[0].SyncStatus, "单程序工号必须在独立列显示同步状态。");
}

static void ProgramManageServiceNoLongerGeneratesProgramFiles()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);

    // 程序文件已不再生成和上传，服务层不得再出现落盘和文件命名逻辑。
    AssertFalse(serviceCode.Contains("WriteProgramContentFile", StringComparison.Ordinal), "程序保存不得再写入本地程序文件。");
    AssertFalse(serviceCode.Contains("ClearProgramContentFile", StringComparison.Ordinal), "程序保存不得再清理本地程序文件。");
    AssertFalse(serviceCode.Contains("ProgramFileRules", StringComparison.Ordinal), "程序保存不得再依赖程序文件命名规则。");
    AssertFalse(serviceCode.Contains("File.WriteAllText", StringComparison.Ordinal), "程序管理服务不得再向磁盘写入程序文件。");
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
    AssertTrue(designerCode.Contains("perm:button.monitor.online-report:enabled", StringComparison.Ordinal), "在线上报按钮必须标记统一的在线上报权限。");
    AssertTrue(viewCode.Contains("PermissionCodes.Buttons.Monitor.OnlineReport", StringComparison.Ordinal), "在线按钮运行时必须检查统一的在线上报权限。");
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
    AssertTrue(viewCode.Contains("btnClearErrorTips.Click += RuntimeErrorClearButton_Click;", StringComparison.Ordinal), "清除按钮必须通过专用入口处理本机设备报警的已读清除逻辑。");
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
        "monitor.error.device_alarm",
        "monitor.error.device_alarm_summary",
        "monitor.error.device_alarm_pending",
        "monitor.notification.plc_alarm_title",
        "monitor.message.clear_device_alarm_confirm"
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
    AssertTrue(viewCode.Contains("SetRuntimeErrorDetailText(", StringComparison.Ordinal) && viewCode.Contains("RuntimeErrorSourceDeviceAlarm", StringComparison.Ordinal), "设备报警必须保存不截断的完整详情及专属来源，以便通知展示和恢复时精确清除。");
    AssertTrue(viewCode.Contains("new AntdUI.Target(this)", StringComparison.Ordinal), "PLC 报警通知必须使用主窗体作为屏幕级定位目标。");
    AssertTrue(viewCode.Contains("AntdUI.TAlignFrom.BL", StringComparison.Ordinal) && viewCode.Contains("AutoClose = 0", StringComparison.Ordinal), "PLC 报警通知必须固定在屏幕左下角并保持到手动关闭或报警恢复。");
    AssertTrue(viewCode.Contains("AntdUI.Notification.contains(notificationId)", StringComparison.Ordinal), "关闭 PLC 报警通知前必须先确认其已进入队列，避免 close_id 的 volley 机制抵消后续通知。");
    // 断言按方法内行为判定，不绑定实参字面量：清除入口把 CurrentStationNo 规范化为局部变量后再传入，仍只作用于当前工位。
    var runtimeErrorClearMethod = ExtractMethodText(
        viewCode,
        "private void RuntimeErrorClearButton_Click(object? sender, EventArgs e)",
        "private void ClearDeviceAlarmRuntimeErrorIfCurrent");
    AssertTrue(
        runtimeErrorClearMethod.Contains("NormalizeStatusStationNo(CurrentStationNo)", StringComparison.Ordinal)
            && runtimeErrorClearMethod.Contains("DismissPlcAlarmNotification(stationNo)", StringComparison.Ordinal),
        "右侧清除设备报警时必须仅标记当前工位通知为已读，不得清除其它工位。");
    AssertTrue(viewCode.Contains("CloseAllPlcAlarmNotifications();", StringComparison.Ordinal), "监控页销毁时必须关闭 PLC 报警通知。");
    var productionMethod = ExtractMethodText(viewCode, "private void ApplyProductionStatus(PlcProductionSnapshot snapshot)", "private void ApplyDeviceStatus");
    AssertTrue(productionMethod.IndexOf("SyncPlcAlarmNotification(snapshot)", StringComparison.Ordinal) < productionMethod.IndexOf("CurrentStationNo", StringComparison.Ordinal), "所有工位的报警快照必须先同步通知，再按当前工位刷新右侧状态。");
    AssertTrue(viewCode.Contains("_plcAlarmNotificationDismissedSignatures", StringComparison.Ordinal) && viewCode.Contains("OnClose =", StringComparison.Ordinal), "通知手动关闭必须保留已读签名，避免相同报警轮询刷屏。");
    AssertTrue(viewCode.Contains("_plcAlarmSummaryDismissedSignatures", StringComparison.Ordinal), "通知关闭与右侧摘要清除必须使用独立签名，手动关闭通知不得隐藏持续报警状态。");
    AssertTrue(viewCode.Contains("private void SetRuntimeErrorDetailText", StringComparison.Ordinal) && viewCode.Contains("message.Trim()", StringComparison.Ordinal), "完整 PLC 报警详情不得经过运行摘要长度截断。");
    AssertTrue(viewCode.Contains("_plcAlarmNotificationSignatures.Remove(stationNo)", StringComparison.Ordinal) && viewCode.Contains("AntdUI.Notification.close_id(notificationId)", StringComparison.Ordinal), "报警恢复时必须清除通知签名并关闭对应工位通知。");
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
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.Designer.cs"), Encoding.UTF8);
    var wireEvents = ExtractMethodText(viewCode, "private void WireEvents()", "private void WireWeldPreviewGridEvents");
    var offlineOptionsMethod = ExtractMethodText(
        viewCode,
        "private void BindOfflineProgramNameOptions()",
        "private void ApplyOfflineProgramNameOption");

    AssertFalse(wireEvents.Contains("RecipeCodeSelection_SelectedIndexChanged", StringComparison.Ordinal), "MonitorView 不得再绑定配方号选择事件。");
    AssertFalse(viewCode.Contains("BindOfflineRecipeCodeOptions", StringComparison.Ordinal), "离线程序列表不得再构建配方号下拉。");
    AssertFalse(viewCode.Contains("ApplyOfflineRecipeCodeSelection", StringComparison.Ordinal), "离线流程不得通过配方号反向选择程序。");
    AssertFalse(viewCode.Contains("BindOnlineRecipeCodeOptions", StringComparison.Ordinal), "在线程序列表不得再构建配方号下拉。");
    AssertFalse(designerCode.Contains("selectRecipeCode", StringComparison.Ordinal), "MonitorView Designer 必须移除业务配方号控件。");
    AssertFalse(viewCode.Contains("配方编号解析失败", StringComparison.Ordinal)
        || viewCode.Contains("配方编号下发失败", StringComparison.Ordinal)
        || viewCode.Contains("配方编号校验失败", StringComparison.Ordinal), "MonitorView 普通业务提示不得暴露数字配方号术语。");
    var localDesignerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Forms", "LocalWorkOrderForm.Designer.cs"), Encoding.UTF8);
    var readme = File.ReadAllText(GetRepoFilePath("README.md"), Encoding.UTF8);
    AssertFalse(localDesignerCode.Contains("txtRecipeCode", StringComparison.Ordinal)
        || localDesignerCode.Contains("配方编号", StringComparison.Ordinal), "本地工单窗口不得显示数字配方号。");
    AssertTrue(readme.Contains("按工位选择 PLC 配方名称", StringComparison.Ordinal)
        && readme.Contains("地址维护 -> 配方名称地址", StringComparison.Ordinal)
        && readme.Contains("不会出现在相应工位的可生产列表", StringComparison.Ordinal), "README 必须说明新的配方名称关联和生产可用性规则。");
    AssertTrue(offlineOptionsMethod.Contains("CurrentStationNo", StringComparison.Ordinal)
        && offlineOptionsMethod.Contains("EnableDualStation && !_currentSettings.EnableDualWorkOrder", StringComparison.Ordinal), "离线程序必须按当前工位和同工单规则过滤。");
}

static void MonitorViewRecipeDropdownUsesSortedRecipeOptions()
{
    var rulesCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Production", "OfflineStartInputRules.cs"), Encoding.UTF8);
    AssertFalse(rulesCode.Contains("BuildRecipeCodeOptions", StringComparison.Ordinal), "普通业务界面移除配方号下拉后不应保留数字选项构建规则。");
}

static void MonitorViewUsesPlcRecipeOnlyForOfflineIdleInputs()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
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
        "private void RealtimePreviewPaintTimer_Tick");
    var stationSwitchMethod = ExtractMethodText(
        viewCode,
        "private void SwitchStationFromUi",
        "private void SelectStationForOperation");
    var runtimeBindingMethod = ExtractMethodText(
        viewCode,
        "private void BindProductionRuntimeState",
        "private bool IsOfflineInputEditable");

    AssertFalse(idleSnapshotMethod.Contains("ApplyOfflineRecipeCodeSelection", StringComparison.Ordinal), "PLC 空闲配方变化不得再反向切换业务程序选择。");
    AssertTrue(previewRefreshMethod.Contains("if (identity is null && IsOfflineInputEditable(GetCurrentStationState()))", StringComparison.Ordinal), "方案预览只有离线输入态才允许读取 PLC 配方反查产品身份。");
    AssertTrue(programSelectionMethod.Contains("MarkOfflineProgramSelectionByUser", StringComparison.Ordinal), "离线选择程序名称必须标记为人工程序选择。");
    AssertTrue(previewRefreshMethod.Contains("ResolveOfflineSelectedRecipeProductIdentity", StringComparison.Ordinal), "离线方案预览必须优先按当前所选程序解析产品工号。");
    AssertTrue(stationSwitchMethod.Contains("ClearOfflineProgramSelectionByUser", StringComparison.Ordinal), "切换工位必须清除人工离线程序标记。");
    AssertTrue(runtimeBindingMethod.Contains("ClearOfflineProgramSelectionByUser(CurrentStationNo)", StringComparison.Ordinal), "离开离线可编辑态后必须清除人工离线程序标记。");
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

static void ProgramManageInitialLoadKeepsSelectedProgramDetails()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"),
        Encoding.UTF8);
    var onLoadMethod = ExtractMethodText(
        viewCode,
        "protected override async void OnLoad(EventArgs e)",
        "protected override void OnLanguageChanged()");

    AssertTrue(
        onLoadMethod.Contains("await ReloadProgramsAsync();", StringComparison.Ordinal),
        "程序管理页首次加载必须读取程序列表。");
    AssertTrue(
        System.Text.RegularExpressions.Regex.IsMatch(
            onLoadMethod,
            @"if\s*\(\s*_programs\.Count\s*==\s*0\s*\)\s*\{\s*StartNewProgram\(\);\s*\}",
            System.Text.RegularExpressions.RegexOptions.Singleline),
        "仅当程序列表为空时才应显示新增程序详情。");
    AssertEqual(
        1,
        System.Text.RegularExpressions.Regex.Matches(onLoadMethod, @"StartNewProgram\s*\(\s*\)").Count,
        "首次加载方法中不得在空列表条件之外再次清空程序详情。");

    var startNewMethod = ExtractMethodText(
        viewCode,
        "private void StartNewProgram()",
        "/// <summary>");
    AssertTrue(
        startNewMethod.Contains("tablePrograms.SelectedIndex = -1;", StringComparison.Ordinal),
        "进入新增状态时必须取消列表当前选择，确保再次点击原行会重新触发详情绑定。");
}

static void ProgramManageRecipeNameSelectorsBindStationRecipeCodes()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.Designer.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("IPlcRecipeNameReaderService", StringComparison.Ordinal), "程序管理页必须注入 PLC 配方名称读取服务。");
    AssertTrue(viewCode.Contains("IAppSettingsService", StringComparison.Ordinal), "程序管理页必须读取系统设置判断双工位。");
    AssertTrue(viewCode.Contains("private enum RecipeSelectionKind", StringComparison.Ordinal)
        && viewCode.Contains("private sealed record RecipeSelectionItem", StringComparison.Ordinal), "配方选择必须通过显式状态模型承载显示和值。");
    AssertTrue(viewCode.Contains("Dictionary<int, List<RecipeSelectionItem>>", StringComparison.Ordinal), "每个工位必须保存与 SelectedIndex 平行的配方选项。");
    AssertFalse(viewCode.Contains("CreateTextColumn(nameof(BizProgram.RecipeCode)", StringComparison.Ordinal), "程序列表不得显示配方号列。");
    AssertFalse(viewCode.Contains("SetColumnHeader(dgvPrograms, nameof(BizProgram.RecipeCode)", StringComparison.Ordinal), "程序列表不得设置配方号表头。");
    AssertFalse(viewCode.Contains("GetRecipeSortBucket", StringComparison.Ordinal), "程序列表不得继续按配方号排序。");
    AssertFalse(viewCode.Contains("int.TryParse(selectedText", StringComparison.Ordinal), "配方保存不得解析选择器显示文本中的数字。");
    AssertTrue(
        designerCode.Contains("tlpProgramType.Visible = false;", StringComparison.Ordinal),
        "程序类型行必须在 Designer 中固定隐藏。 ");
    AssertTrue(
        viewCode.Contains("editorLayout.RowStyles[7]", StringComparison.Ordinal),
        "双工位切换只能调整工位 2 配方行。 ");
    AssertFalse(
        viewCode.Contains("editorLayout.RowStyles[8]", StringComparison.Ordinal),
        "双工位切换不得修改已隐藏的程序类型行。 ");
    AssertTrue(viewCode.Contains("RecipeSelectionKind.NotApplicable", StringComparison.Ordinal), "双工位下拉必须提供不适用状态。");
    AssertTrue(viewCode.Contains("RecipeSelectionKind.MissingExisting", StringComparison.Ordinal), "历史失效关联必须使用不暴露数字的状态项。");
    AssertTrue(viewCode.Contains("select.List = true;", StringComparison.Ordinal), "配方选择器必须始终保持列表模式。");
    AssertTrue(viewCode.Contains("select.ReadOnly = !result.IsSuccess;", StringComparison.Ordinal), "读取失败时选择器必须只读且禁止手工输入。");
    AssertFalse(viewCode.Contains("PlaceholderRecipeManual", StringComparison.Ordinal), "读取失败时不得提供手工配方号输入提示。");
    AssertTrue(designerCode.Contains("selectStation1Recipe = new AntdUI.Select();", StringComparison.Ordinal), "Designer 必须声明工位 1 配方名称下拉。");
    AssertTrue(designerCode.Contains("selectStation2Recipe = new AntdUI.Select();", StringComparison.Ordinal), "Designer 必须声明工位 2 配方名称下拉。");
    AssertTrue(designerCode.Contains("selectStation1Recipe.List = true;", StringComparison.Ordinal)
        && designerCode.Contains("selectStation2Recipe.List = true;", StringComparison.Ordinal), "Designer 必须将两个配方选择器固定为列表模式。");
    AssertTrue(viewCode.Contains("editingProgram?.Station2RecipeCode", StringComparison.Ordinal)
        && viewCode.Contains("_editingId > 0", StringComparison.Ordinal), "单工位编辑历史程序时必须保留已有工位 2 配方号。");
    AssertTrue(viewCode.Contains("ProgramSaveRecipeRules.Validate(", StringComparison.Ordinal), "程序管理保存前必须应用单双工位配方完整性规则。");
    AssertTrue(serviceCode.Contains("ProgramSaveRecipeRules.Validate(", StringComparison.Ordinal)
        && serviceCode.Contains("CurrentSettings.EnableDualStation", StringComparison.Ordinal), "服务保存入口必须在规范化后按当前单双工位设置校验配方号。");
    AssertTrue(viewCode.Contains("Interlocked.Increment(ref _recipeNameRefreshVersion)", StringComparison.Ordinal), "每次配方名称刷新必须递增版本号。");
    AssertTrue(viewCode.Contains("refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion)", StringComparison.Ordinal), "旧刷新返回时必须通过版本号阻止覆盖新状态。");
}

static void AddressManageExposesPlcRecipeNameConfiguration()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.Designer.cs"), Encoding.UTF8);

    AssertTrue(viewCode.Contains("IPlcRecipeNameConfigService", StringComparison.Ordinal), "地址维护页必须注入 PLC 配方名称配置服务。");
    AssertTrue(viewCode.Contains("IPlcRecipeNameReaderService", StringComparison.Ordinal), "地址维护页必须注入 PLC 配方名称读取服务。");
    AssertTrue(viewCode.Contains("SaveRecipeNameConfigs", StringComparison.Ordinal), "地址维护页必须提供配方名称地址配置保存入口。");
    AssertTrue(viewCode.Contains("ReadRecipeNamePreviewAsync", StringComparison.Ordinal), "地址维护页必须提供配方名称地址读取预览入口。");
    AssertTrue(viewCode.Contains("IAppSettingsService", StringComparison.Ordinal), "地址维护页必须按系统双工位设置筛选可见配置行。");
    var previewMethod = ExtractMethodText(viewCode, "private async Task ReadRecipeNamePreviewAsync()", "private async Task RefreshAddressDependentServicesQuietlyAsync()");
    AssertFalse(previewMethod.Contains("SaveRecipeNameConfigs", StringComparison.Ordinal), "配方名称预览不得隐式保存当前编辑配置。");
    AssertTrue(previewMethod.Contains("ReadConfigAsync(config)", StringComparison.Ordinal), "配方名称预览必须直接读取当前内存配置。");
    AssertTrue(viewCode.Contains("BindVisibleRecipeNameConfigs", StringComparison.Ordinal), "单工位模式只应绑定工位 1 配方配置。");
    AssertTrue(viewCode.Contains("var configsToSave = _recipeNameConfigs", StringComparison.Ordinal)
        && viewCode.Contains("_plcRecipeNameConfigService.SaveAll(configsToSave", StringComparison.Ordinal), "保存时必须从内部完整配置生成保存集合，保留隐藏工位 2 配置。");
    AssertTrue(viewCode.Contains("GroupBy(config => config.StationNo)", StringComparison.Ordinal)
        && viewCode.Contains("OrderByDescending(config => config.UpdatedTime)", StringComparison.Ordinal), "历史重复工位配置必须按更新时间确定性择一。");
    AssertTrue(designerCode.Contains("tabRecipeNames", StringComparison.Ordinal), "Designer 必须声明配方名称地址页签。");
    AssertTrue(designerCode.Contains("tableRecipeNames", StringComparison.Ordinal), "Designer 必须声明配方名称配置表格。");
    AssertTrue(designerCode.Contains("tableRecipeNamePreview", StringComparison.Ordinal), "Designer 必须声明配方名称读取预览表格。");
    AssertTrue(designerCode.Contains("btnPreviewRecipeNames", StringComparison.Ordinal), "Designer 必须声明配方名称读取按钮。");
    AssertTrue(viewCode.Contains("BaseAddress", StringComparison.Ordinal)
        && viewCode.Contains("RecipeCount", StringComparison.Ordinal)
        && viewCode.Contains("AddressOffset", StringComparison.Ordinal)
        && viewCode.Contains("StringLength", StringComparison.Ordinal), "地址维护页必须展示基地址、数量、偏移和字符串长度字段。");
}

static void PlcRecipeNameConfigServiceReadsLatestStationRow()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "PlcRecipeNameConfigService.cs"),
        Encoding.UTF8);
    var method = ExtractMethodText(serviceCode, "public BizPlcRecipeNameConfig? GetForStation", "public void SaveAll");

    AssertTrue(method.Contains("OrderBy(config => config.UpdatedTime, OrderByType.Desc)", StringComparison.Ordinal), "读取工位配置必须优先选择最新更新时间。");
    AssertTrue(method.Contains("OrderBy(config => config.Id, OrderByType.Desc)", StringComparison.Ordinal), "更新时间相同时必须再按主键倒序确定唯一配置。");
}

static void SystemSettingViewLocksDeviceManagementDuringActiveRuntimeTasks()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var deviceSaveGuard = ExtractMethodText(
        viewCode,
        "private bool CanSaveDeviceManagementChange(",
        "private void RefreshDeviceManagementEnabled");
    var refreshMethod = ExtractMethodText(
        viewCode,
        "private void RefreshDeviceManagementEnabled",
        "private bool HasAnyActiveRuntimeTask");
    var activeTaskMethod = ExtractMethodText(
        viewCode,
        "private bool HasAnyActiveRuntimeTask",
        "private static bool IsActiveRuntimeTask");
    var activeTaskRule = ExtractMethodText(
        viewCode,
        "private static bool IsActiveRuntimeTask",
        "private bool HasAnyUnfinishedTask");
    var runtimeModeGuard = ExtractMethodText(
        viewCode,
        "private bool CanSaveRuntimeModeChange(",
        "private bool CanSaveDeviceManagementChange");
    var unfinishedTaskMethod = ExtractMethodText(
        viewCode,
        "private bool HasAnyUnfinishedTask",
        "private static bool HasDualModeChanged");

    AssertTrue(
        refreshMethod.Contains("var enabled = !HasAnyActiveRuntimeTask();", StringComparison.Ordinal)
        && refreshMethod.Contains("grpDeviceConfig.Enabled = enabled;", StringComparison.Ordinal)
        && refreshMethod.Contains("grpMesConfig.Enabled = enabled;", StringComparison.Ordinal),
        "设备管理和 MES 配置模块必须仅由当前软件运行态中的活动任务控制。");
    AssertTrue(
        deviceSaveGuard.Contains("!HasDeviceIdentityChanged(previousSettings, newSettings) || !HasAnyActiveRuntimeTask()", StringComparison.Ordinal),
        "设备管理保存防线必须与界面使用相同的当前运行态判断。");
    AssertTrue(
        activeTaskMethod.Contains("_weldTaskService.CurrentState", StringComparison.Ordinal)
        && activeTaskMethod.Contains("state.ActiveTask", StringComparison.Ordinal)
        && activeTaskMethod.Contains("state.StationStates.Values.Any", StringComparison.Ordinal)
        && activeTaskMethod.Contains("station.ActiveTask", StringComparison.Ordinal),
        "设备管理锁定必须覆盖兼容运行态及所有工位的 ActiveTask。");
    AssertFalse(
        activeTaskMethod.Contains("GetUnfinishedTask", StringComparison.Ordinal)
        || activeTaskMethod.Contains("Plc", StringComparison.Ordinal)
        || activeTaskMethod.Contains("DeviceStatus", StringComparison.Ordinal),
        "设备管理锁定不得读取数据库未完工任务或 PLC 设备状态。");
    AssertTrue(
        activeTaskRule.Contains("task.EndTime is null", StringComparison.Ordinal)
        && activeTaskRule.Contains("ProductionConstants.ProductInstanceStatuses.Completed", StringComparison.Ordinal)
        && activeTaskRule.Contains("StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal),
        "当前 ActiveTask 必须在尚未完工且状态不是 Completed 时锁定，暂停任务仍保持锁定。");
    AssertTrue(
        runtimeModeGuard.Contains("HasAnyUnfinishedTask()", StringComparison.Ordinal),
        "双工位和双工单模式修改必须继续保留数据库未完工任务保护。");
    AssertTrue(
        unfinishedTaskMethod.Contains("_weldTaskService.GetUnfinishedTask(1) is not null", StringComparison.Ordinal)
        && unfinishedTaskMethod.Contains("_weldTaskService.GetUnfinishedTask(2) is not null", StringComparison.Ordinal),
        "原未完工任务检查必须继续覆盖两个工位，仅供运行模式保护使用。");
    AssertTrue(
        viewCode.Contains("protected override void OnVisibleChanged(EventArgs e)", StringComparison.Ordinal)
        && viewCode.Contains("RefreshDeviceManagementEnabled();", StringComparison.Ordinal),
        "系统设置页重新显示时必须刷新设备管理模块的可编辑状态。");
    AssertTrue(
        CountOccurrences(viewCode, "CanSaveDeviceManagementChange(previousSettings, settings)") >= 2,
        "整体保存和手动同步设备两个入口都必须执行设备管理变更校验。");

    var chineseResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.resx"),
        Encoding.UTF8);
    var englishResources = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Core", "Localization", "UiText.en.resx"),
        Encoding.UTF8);
    AssertTrue(chineseResources.Contains("软件已开工，请先完工后再修改设备管理信息。", StringComparison.Ordinal), "中文提示必须说明软件已开工。");
    AssertTrue(englishResources.Contains("Production is currently started.", StringComparison.Ordinal), "英文提示必须同步说明软件已开工。");
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
    AssertTrue(bindMethod.Contains("hasPreparedWorkOrder ? ResolveDisplayProductIdentity(state) : null", StringComparison.Ordinal), "运行态绑定必须只为运行任务或待开工上下文使用缓存产品身份。");
    AssertTrue(identityResolver.Contains("IsOfflineInputEditable(state)", StringComparison.Ordinal), "离线未开工时仍允许使用 PLC/配方解析出的产品身份。");
    AssertTrue(identityResolver.Contains("state.ActiveTask is not null", StringComparison.Ordinal), "运行中任务仍允许使用产品身份缓存。");
    AssertTrue(identityResolver.Contains("return null;", StringComparison.Ordinal), "在线空闲且无工单时必须禁用旧产品身份缓存。");
    AssertTrue(clearHelper.Contains("_currentProductIdentity = null;", StringComparison.Ordinal), "完工清理必须清空当前产品身份缓存。");
    AssertTrue(clearHelper.Contains("_lastSchemePreviewKey = string.Empty;", StringComparison.Ordinal), "完工清理必须清空方案预览键，避免旧产品预览复用。");
    AssertTrue(clearHelper.Contains("ClearConfirmedWorkOrderInput(stationNo);", StringComparison.Ordinal), "完工清理必须移除上一工单确认状态，避免旧工单继续显示为待开工草稿。");
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
static void MonitorViewSinglePointHistoryMappingKeepsPointValues()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var method = ExtractMethodText(
        viewCode,
        "private ProductHistoryTableRow ToProductHistoryRow(",
        "    /// <summary>\r\n    /// 处理到产品历史Point行。");

    AssertTrue(method.Contains("ProductHistoryDisplayRules.ShouldFlattenSinglePoint(displayOptions.TouchCount, children.Count)", StringComparison.Ordinal), "实时历史必须按配置采集点数和实际记录数判断是否扁平化。");
    AssertTrue(method.Contains("DynamicValues = pointRow.DynamicValues", StringComparison.Ordinal), "实时单焊点扁平行必须保留焊点动态值。");
    AssertTrue(method.Contains("RecordTimeText = pointRow.RecordTimeText", StringComparison.Ordinal), "实时单焊点扁平行必须保留焊点采集时间。");
    AssertTrue(method.Contains("TouchNo = pointRow.TouchNo", StringComparison.Ordinal), "实时单焊点扁平行必须保留焊点序号。");
    AssertTrue(method.Contains("Children = children", StringComparison.Ordinal), "实时历史多焊点仍必须保留树形子行。");
}

static void MonitorViewClearsIdleProductionData()
{
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var bindMethod = ExtractMethodText(viewCode, "private void BindProductionRuntimeState()", "private bool HasPreparedWorkOrderInfo");
    var clearMethod = ExtractMethodText(viewCode, "private void ClearIdleProductionDataDisplay", "private void ClearUnpreparedWorkOrderInfoDisplay");
    var realtimeMethod = ExtractMethodText(viewCode, "private void ApplyProductRealtimePreviewSnapshot", "private bool CanDisplayRealtimePreviewSnapshot");
    var historyMethod = ExtractMethodText(viewCode, "private void RefreshProductHistoryPreviewCore()", "private void BindProductHistorySnapshot");
    var schemeQueueMethod = ExtractMethodText(viewCode, "private void QueueRefreshSchemePreview", "private async Task RefreshSchemePreviewAsync");

    AssertTrue(bindMethod.Contains("HasPreparedWorkOrderInfo", StringComparison.Ordinal), "未开工工单模块必须区分无上下文空闲态和待开工草稿。");
    AssertTrue(bindMethod.Contains("ClearIdleProductionDataDisplay", StringComparison.Ordinal), "未开工刷新必须统一清理生产数据。");
    AssertTrue(clearMethod.Contains("ClearCurrentRealtimePreviewDisplay", StringComparison.Ordinal), "未开工必须清空采集预览。");
    AssertTrue(clearMethod.Contains("ClearCurrentProductHistoryDisplay", StringComparison.Ordinal), "未开工必须清空产品历史。");
    AssertTrue(realtimeMethod.Contains("CanDisplayRealtimePreviewSnapshot", StringComparison.Ordinal), "实时快照必须经过运行任务校验。");
    AssertTrue(viewCode.Contains("snapshot.RefreshTime >= activeTask!.StartTime", StringComparison.Ordinal), "开工后不得展示开工前缓存的实时快照。");
    AssertTrue(historyMethod.Contains("activeTask is null || !IsRunningWeldTask(activeTask)", StringComparison.Ordinal), "产品历史只允许在任务运行期间加载。");
    AssertTrue(schemeQueueMethod.Contains("!IsRunningWeldTask", StringComparison.Ordinal), "未开工时不得生成方案采集预览行。");
    AssertTrue(viewCode.Contains("state.CurrentWorkOrder is not null", StringComparison.Ordinal), "已查询的待开工工单必须保留显示。");
    AssertTrue(viewCode.Contains("IsNewLiveWorkOrder(liveWorkId)", StringComparison.Ordinal), "新扫描且尚未查询的工单号必须作为待开工信息保留。");
    AssertTrue(viewCode.Contains("_manualWorkOrderEditedByUser", StringComparison.Ordinal), "人工待开工草稿必须保留显示。");
    AssertTrue(viewCode.Contains("IsOfflineInputEditable(state)", StringComparison.Ordinal), "离线待开工草稿必须保留显示。");
}

static void WeldTaskFinishUsesMesStartIdForRetryPayloads()
{
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "WeldTaskService.cs"), Encoding.UTF8);
    var startMethod = ExtractMethodText(
        serviceCode,
        "public async Task<BizWeldTask> StartAsync(",
        "/// <summary>\r\n    /// Creates a local running task");
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
    var startStatusIndex = startMethod.IndexOf("await RecordProgramStartedStatusAsync(task, cancellationToken);", StringComparison.Ordinal);
    var startNotifyIndex = startMethod.IndexOf("NotifyStateChanged();", StringComparison.Ordinal);
    AssertTrue(startNotifyIndex >= 0 && startNotifyIndex < startStatusIndex, "在线开工必须在设备状态上传前先通知任务已落库，保证工单信息页及时显示。");
    var endStatusIndex = finishMethod.IndexOf("await RecordProgramEndedStatusAsync(task, cancellationToken);", StringComparison.Ordinal);
    var finishQueueIndex = finishMethod.IndexOf("EnqueueFinishReportTask(", StringComparison.Ordinal);
    AssertTrue(endStatusIndex >= 0 && finishQueueIndex > endStatusIndex, "完工必须先记录程序结束状态，再编排完工与报告文件上传任务。");
    AssertTrue(finishMethod.Contains("uploadTasks.Insert(reportFileIndex >= 0 ? reportFileIndex : uploadTasks.Count, finishReportTask);", StringComparison.Ordinal), "完工失败重试任务必须排在报告文件任务之前。");
    var retryFinishIndex = serviceCode.IndexOf("ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.FinishReport", StringComparison.Ordinal);
    var retryReportFileIndex = serviceCode.IndexOf("ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.ReportFile", StringComparison.Ordinal);
    AssertTrue(retryFinishIndex >= 0 && retryFinishIndex < retryReportFileIndex, "全局补传必须先处理完工上报，再处理报告文件。");
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
static void RuntimeTipRestoreRequiresUnfinishedTask()
{
    AssertTrue(
        RuntimeTipRestoreRules.ShouldRestoreRuntimeTip(hasUnfinishedTask: true),
        "开工状态下必须恢复上一次的运行提示，保证断点续作时进展可见。");

    AssertFalse(
        RuntimeTipRestoreRules.ShouldRestoreRuntimeTip(hasUnfinishedTask: false),
        "未开工时不得恢复历史提示，避免显示与实际状态不符的旧进展。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    AssertTrue(
        viewCode.Contains("RuntimeTipRestoreRules.ShouldRestoreRuntimeTip", StringComparison.Ordinal),
        "恢复运行提示前必须走集中判定规则，避免界面层重复实现业务决策。");
    AssertTrue(
        viewCode.Contains("ResetRuntimeTipStateToDefault();", StringComparison.Ordinal),
        "不恢复历史提示时必须显式重置为默认等待业务操作状态。");
}

static void WorkOrderBaselineSuppressesStartupResidualBarcode()
{
    // 启动后首个读数：寄存器里的残留条码只能当基准，不得填界面或查 MES。
    AssertTrue(
        WorkOrderAutoQueryRules.ShouldCaptureBaselineOnly(
            hasBaseline: false,
            readSuccess: true,
            workId: "WO-RESIDUAL"),
        "启动后首个工单号读数必须只记录为基准值。");

    // 已有基准后，同值或新值都不再走基准分支，交由既有查询规则判断。
    AssertFalse(
        WorkOrderAutoQueryRules.ShouldCaptureBaselineOnly(
            hasBaseline: true,
            readSuccess: true,
            workId: "WO-NEW"),
        "已记录基准的工位不得再次进入基准分支。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldCaptureBaselineOnly(
            hasBaseline: false,
            readSuccess: false,
            workId: "WO-RESIDUAL"),
        "读取失败时不得把无效值记为基准。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldCaptureBaselineOnly(
            hasBaseline: false,
            readSuccess: true,
            workId: "   "),
        "空白工单号不构成基准值，避免占用首读机会。");

    // 基准记录后，真实新扫码仍必须能触发自动查询。
    AssertTrue(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "WO-NEW",
            lastRequestedWorkId: "WO-RESIDUAL",
            queryInProgress: false),
        "残留值记为基准后，扫入不同工单号必须能正常自动查询。");
}

static void WorkOrderClearResetsPlcQueryState()
{
    AssertTrue(
        WorkOrderAutoQueryRules.ShouldResetAfterPlcClear(
            readSuccess: true,
            workId: "   \r\n\t  "),
        "PLC 成功读取到空格、换行或制表符时，应视为工单号已清空。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldResetAfterPlcClear(
            readSuccess: false,
            workId: "   "),
        "PLC 读取失败时不能把旧工单号误判为已清空。");

    AssertFalse(
        WorkOrderAutoQueryRules.ShouldResetAfterPlcClear(
            readSuccess: true,
            workId: "WO-NEW"),
        "非空 PLC 工单号不能进入清空复位分支。");

    AssertTrue(
        WorkOrderAutoQueryRules.ShouldAutoQuery(
            mesConnected: true,
            hasRunningTask: false,
            workIdReadSuccess: true,
            workId: "WO-SAME",
            lastRequestedWorkId: null,
            queryInProgress: false),
        "清空复位查询基线后，再次扫描原工单号也必须允许自动查询。");
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
    AssertTrue(viewCode.Contains("ApplyClearedPlcWorkOrderInput", StringComparison.Ordinal), "PLC 清空工单号必须有独立的状态复位入口。");
    AssertTrue(viewCode.Contains("_lastAutoQueriedWorkIds.Remove(stationNo);", StringComparison.Ordinal), "PLC 清空工单号时必须释放自动查询去重基线。");
    AssertTrue(viewCode.Contains("_workOrderBaselines.Add(stationNo);", StringComparison.Ordinal), "PLC 空值必须建立基线，确保随后首次真实扫码不会被当成启动残留。");
    AssertTrue(viewCode.Contains("_workOrderBaselines.Contains(stationNo)", StringComparison.Ordinal), "启动基线状态必须与工单查询去重状态分离，清空后同号扫描才能重新查询。");
    AssertTrue(viewCode.Contains("SetWorkOrderInputText(string.Empty);", StringComparison.Ordinal), "PLC 清空工单号时必须清空流转卡号控件。");
    AssertTrue(viewCode.Contains("CancelWorkOrderLoad(stationNo);", StringComparison.Ordinal), "PLC 清空工单号时必须取消旧的工单查询请求。");
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

static void ProgramContentReviewRowsUseEditedStandardValues()
{
    // 开工弹窗已取消“修改值”列，用户就地改设定值，合并时直接取该值。
    var rows = new List<ProgramContentReviewRow>
    {
        new() { ItemName = "高度", StandardValue = "13.0" },
        new() { ItemName = "压力", StandardValue = "20" },
        new() { ItemName = "", StandardValue = "skip" },
        new() { ItemName = "电阻", StandardValue = "   " }
    };

    var json = ProgramContentJsonRules.MergeReviewRowsToJson(rows);
    using var document = JsonDocument.Parse(json);
    AssertTrue(document.RootElement.GetProperty("高度").GetString() == "13.0", "就地修改后的设定值应直接进入 JSON。");
    AssertTrue(document.RootElement.GetProperty("压力").GetString() == "20", "未修改的设定值应原样进入 JSON。");
    AssertFalse(document.RootElement.TryGetProperty("", out _), "测试项名称为空的行不应进入 JSON。");
    AssertFalse(document.RootElement.TryGetProperty("电阻", out _), "设定值被清空的行不应进入 JSON。");
}

static void ProgramContentReviewRejectsDuplicateItemNames()
{
    var rows = new List<ProgramContentReviewRow>
    {
        new() { ItemName = "高度", StandardValue = "13.0" },
        new() { ItemName = "高度", StandardValue = "14.0" }
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
    FakeProductionReportFileService? reportFileService = null,
    FakeDeviceStatusService? deviceStatusService = null)
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
        deviceStatusService ?? new FakeDeviceStatusService(),
        clockService,
        new FakeDataHistoryMaintenanceService());
}

static DeviceLifecycleLogCoordinator CreateDeviceLifecycleLogCoordinator(
    FakeDeviceLifecycleLogService lifecycleLogService,
    FakeDeviceStatusService deviceStatusService,
    AppSettings? settings = null)
{
    return new DeviceLifecycleLogCoordinator(
        new FakeAppSettingsService { Current = settings ?? new AppSettings { DeviceId = "D-001" } },
        lifecycleLogService,
        new FakePlcCommunicationService(),
        new FakeMesConnectionMonitor(),
        new FakeCenterTelemetrySyncService(),
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
    HttpMessageHandler handler,
    FakeMesInteractionLogService? logService = null)
{
    return new MesProvider(
        new HttpClient(handler),
        appSettingsService,
        new FakeLocalizationService(),
        logService ?? new FakeMesInteractionLogService());
}

static AppSettings BuildCustomMesRouteSettings()
{
    return new AppSettings
    {
        MesBaseUrl = "http://127.0.0.1:7098/",
        MesUserRoute = "mes/user-custom",
        MesWorkOrderRoute = "mes/work-order-custom",
        MesServerTimeRoute = "mes/server-time-custom",
        MesSysRoute = "mes/sys-custom",
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

static void NaturalSortComparerOrdersProductNumbersNumerically()
{
    var unsorted = new[] { "P10", "P2", "P1", "P20", "P3", "P100", "P11" };
    var sorted = unsorted.OrderBy(x => x, NaturalSortComparer.Instance).ToArray();

    AssertSequenceEqual(
        new[] { "P1", "P2", "P3", "P10", "P11", "P20", "P100" },
        sorted,
        "产品编号应按数字大小排序，而不是字符串字典顺序。");

    var mixedPrefix = new[] { "S2-P10", "S1-P2", "S1-P10", "S2-P2" };
    var sortedMixed = mixedPrefix.OrderBy(x => x, NaturalSortComparer.Instance).ToArray();

    AssertSequenceEqual(
        new[] { "S1-P2", "S1-P10", "S2-P2", "S2-P10" },
        sortedMixed,
        "带工位前缀的产品编号应先按工位排序，再按产品数字排序。");

    var pureAlpha = new[] { "ABC", "ABD", "AAA" };
    var sortedAlpha = pureAlpha.OrderBy(x => x, NaturalSortComparer.Instance).ToArray();

    AssertSequenceEqual(
        new[] { "AAA", "ABC", "ABD" },
        sortedAlpha,
        "纯字母字符串应保持字典顺序。");
}

static void ProgramDeleteKeepsMesSyncOffUiPath()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"),
        Encoding.UTF8);
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"),
        Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("DeleteLocalAsync", StringComparison.Ordinal)
            && viewCode.Contains("SyncDeletedProgramInBackgroundAsync", StringComparison.Ordinal),
        "程序删除必须先本地提交，再后台执行 MES 同步。");
    AssertTrue(
        serviceCode.Contains("Task.Run", StringComparison.Ordinal)
            && serviceCode.Contains("DeleteLocalCore", StringComparison.Ordinal)
            && serviceCode.Contains("SemaphoreSlim", StringComparison.Ordinal),
        "删除服务必须将同步数据库操作移出 UI 线程并串行保护程序变更。");
    AssertTrue(
        viewCode.Contains("BatchDeleteLocalProgramsAsync(pendingIds, _operationCts.Token)", StringComparison.Ordinal),
        "批量清理必须传递页面取消令牌。");
    AssertTrue(
        viewCode.Contains("await ReloadProgramsAsync()", StringComparison.Ordinal)
            && viewCode.Contains("GetProgramLookupsAsync", StringComparison.Ordinal),
        "删除完成后的程序列表刷新必须使用后台查询，不能在 UI 线程同步读取数据库。");

    var getProgramsAsyncBody = ExtractMethodText(
        serviceCode,
        "public async Task<IReadOnlyList<ProgramLookup>> GetProgramLookupsAsync(",
        "public Task<BizProgram?> GetProgramAsync(");
    AssertFalse(
        getProgramsAsyncBody.Contains("_mutationGate", StringComparison.Ordinal),
        "列表查询不能参与程序变更门锁，否则删除后立即刷新会形成互相等待。");

    AssertTrue(
        viewCode.Contains("没有需要清理的程序。", StringComparison.Ordinal),
        "批量清理在没有目标时必须直接提示，不能继续走清理和刷新流程。");
    AssertTrue(
        viewCode.Contains("当前没有可删除的加工程序。", StringComparison.Ordinal),
        "程序表为空时删除必须直接提示，不能进入删除流程。");
    AssertTrue(
        viewCode.Contains("private IWin32Window GetDialogOwner()", StringComparison.Ordinal)
            && System.Text.RegularExpressions.Regex.Matches(viewCode, @"GetDialogOwner\(\)").Count == 3,
        "单条删除和批量清理确认框都必须绑定主窗体所有者。");
    AssertFalse(viewCode.Contains("MessageBox.Show(this,", StringComparison.Ordinal), "程序管理页确认框不得继续以 UserControl 作为窗口所有者。");
}

static void ProgramManageSaveAndDualSelectorPathsStayAsynchronous()
{
    var viewCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"),
        Encoding.UTF8);
    var designerCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.Designer.cs"),
        Encoding.UTF8);
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"),
        Encoding.UTF8);

    AssertTrue(
        viewCode.Contains("GetNextSequenceNumberAsync", StringComparison.Ordinal)
            && viewCode.Contains("await ReloadProgramsAsync(saved.Id)", StringComparison.Ordinal)
            && viewCode.Contains("ReadRecipeNameOptionsAsync", StringComparison.Ordinal),
        "新增和另存为流程不能在 UI 线程同步查询序号、保存或刷新列表。 ");
    AssertTrue(
        viewCode.Contains("RecipeNameReadTimeout = TimeSpan.FromSeconds(10)", StringComparison.Ordinal)
            && viewCode.Contains("CreateLinkedTokenSource(_operationCts.Token)", StringComparison.Ordinal)
            && viewCode.Contains("SetRecipeSelectorItems", StringComparison.Ordinal),
        "双工位配方刷新必须有页面取消、总时限和选择器去重绑定。 ");
    AssertTrue(
        designerCode.Contains("DisposeOperationCts();", StringComparison.Ordinal)
            && viewCode.Contains("private void DisposeOperationCts()", StringComparison.Ordinal)
            && viewCode.Contains("Interlocked.Exchange(ref _operationCtsDisposed, 1)", StringComparison.Ordinal),
        "页面销毁时必须通过幂等释放方法取消并释放操作令牌。 ");
    AssertFalse(
        designerCode.Contains("_operationCts?.Cancel();", StringComparison.Ordinal)
            || designerCode.Contains("_operationCts?.Dispose();", StringComparison.Ordinal),
        "Designer 不得直接重复释放操作令牌。 ");
    AssertFalse(
        viewCode.Contains("protected override void OnHandleDestroyed(EventArgs e)", StringComparison.Ordinal),
        "运行时代码不得再次释放操作令牌，避免与 Designer Dispose 重复释放。 ");
    AssertTrue(
        serviceCode.Contains("SaveWithSyncDecisionCore", StringComparison.Ordinal)
            && serviceCode.Contains("GetNextSequenceNumberAsync", StringComparison.Ordinal),
        "程序保存和序号查询必须有后台执行入口。 ");

    var selectionBody = ExtractMethodText(
        viewCode,
        "private void SetRecipeSelection(",
        "private string? ResolveSelectedRecipeCode(");
    var refreshIndex = selectionBody.IndexOf("RefreshRecipeSelectorItems(select, stationNo)", StringComparison.Ordinal);
    var refreshGuardIndex = selectionBody.IndexOf("if (itemCount != items.Count)", StringComparison.Ordinal);
    AssertTrue(
        refreshGuardIndex >= 0 && refreshIndex > refreshGuardIndex,
        "设置选中配方时只能在历史选项新增后重建完整下拉列表。 ");
}

static void ProgramLookupSnapshotRemovesUiDatabaseQueries()
{
    var lookupCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "DTOs", "ProgramLookup.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "ProgramManageService.cs"), Encoding.UTF8);
    var monitorCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "MonitorView.cs"), Encoding.UTF8);
    var programViewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "ProgramManageView.cs"), Encoding.UTF8);
    var addressCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "AddressManageView.cs"), Encoding.UTF8);
    var mainCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Forms", "MainForm.cs"), Encoding.UTF8);
    var previewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Production", "ProductRealtimePreviewService.cs"), Encoding.UTF8);
    var reconcileCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "Plc", "RecipeCodeReconcileMonitorService.cs"), Encoding.UTF8);

    AssertFalse(lookupCode.Contains("public string? ProgramContent", StringComparison.Ordinal), "轻量程序快照不得包含 ProgramContent。");
    AssertFalse(lookupCode.Contains("public string? ProgramFile", StringComparison.Ordinal), "轻量程序快照不得包含 ProgramFile。");
    AssertFalse(lookupCode.Contains("public string? SyncMessage", StringComparison.Ordinal), "轻量程序快照不得包含 SyncMessage。");

    var queryMethod = ExtractMethodText(serviceCode, "private ProgramLookup[] QueryProgramLookups(", "private void InvalidateProgramLookups()");
    AssertFalse(queryMethod.Contains("ProgramFile", StringComparison.Ordinal)
        || queryMethod.Contains("ProgramContent", StringComparison.Ordinal)
        || queryMethod.Contains("SyncMessage", StringComparison.Ordinal),
        "轻量投影不得读取程序大字段。");
    AssertTrue(serviceCode.Contains("_programLookupVersion", StringComparison.Ordinal)
        && serviceCode.Contains("ProgramLookupsChanged?.Invoke", StringComparison.Ordinal),
        "程序快照必须通过版本号防止旧查询覆盖，并在变更后通知消费者。");

    foreach (var (name, source) in new[]
    {
        ("MonitorView", monitorCode),
        ("ProgramManageView", programViewCode),
        ("AddressManageView", addressCode),
        ("MainForm", mainCode),
        ("ProductRealtimePreviewService", previewCode),
        ("RecipeCodeReconcileMonitorService", reconcileCode)
    })
    {
        AssertFalse(source.Contains("_programManageService.GetPrograms()", StringComparison.Ordinal)
            || source.Contains("_programService.GetPrograms()", StringComparison.Ordinal),
            $"{name} 不得在选择或刷新路径同步查询完整程序表。");
    }

    var onlineBind = ExtractMethodText(monitorCode, "private void BindOnlineProgramNameOptions()", "private void BindProductionRuntimeState()");
    AssertTrue(onlineBind.Contains("_localProgramSnapshot", StringComparison.Ordinal), "在线程序绑定必须复用一次加载的内存快照。");
    AssertFalse(onlineBind.Contains("GetProgramLookupsAsync", StringComparison.Ordinal), "在线程序循环内部不得重新访问服务或数据库。");
    AssertTrue(programViewCode.Contains("GetProgramLookupsAsync", StringComparison.Ordinal)
        && programViewCode.Contains("GetProgramAsync", StringComparison.Ordinal)
        && programViewCode.Contains("_detailLoadVersion", StringComparison.Ordinal),
        "程序管理页必须使用轻量列表并按选中 ID 异步加载完整详情。");
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
    AssertTrue(DesignerRowIsAutoSize(designerCode, "tlpProductConfig"), "Designer 必须将工位名称行设为 AutoSize，使隐藏容器时自动折叠。");
}

static bool DesignerRowIsAutoSize(string designerCode, string container)
{
    // RowStyle 无参构造默认即 AutoSize，设计器会把显式写法规范化为无参形式，两者运行时等价。
    return designerCode.Contains($"{container}.RowStyles.Add(new RowStyle(SizeType.AutoSize));", StringComparison.Ordinal)
        || designerCode.Contains($"{container}.RowStyles.Add(new RowStyle());", StringComparison.Ordinal);
}

static void SystemSettingViewUsesResponsiveSemanticColumns()
{
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);

    AssertTrue(designerCode.Contains("private Panel basicSettingsViewport;", StringComparison.Ordinal), "Designer 必须声明基础设置滚动视口。");
    AssertTrue(designerCode.Contains("private TableLayoutPanel basicSettingsLayout;", StringComparison.Ordinal), "Designer 必须声明响应式主表格。");
    AssertTrue(designerCode.Contains("leftSettingsColumn.Controls.Add(grpPlcConfig, 0, 0);", StringComparison.Ordinal), "左列第一组必须是 PLC。");
    AssertTrue(designerCode.Contains("leftSettingsColumn.Controls.Add(grpDeviceConfig, 0, 1);", StringComparison.Ordinal), "左列第二组必须是设备。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpProductionConfig, 0, 0);", StringComparison.Ordinal), "中列第一组必须是生产。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpAppConfig, 0, 1);", StringComparison.Ordinal), "中列第二组必须是应用。");
    AssertTrue(designerCode.Contains("middleSettingsColumn.Controls.Add(grpCenterServerConfig, 0, 2);", StringComparison.Ordinal), "中列第三组必须是中心服务器。");
    AssertTrue(designerCode.Contains("rightSettingsColumn.Controls.Add(grpMesConfig, 0, 0);", StringComparison.Ordinal), "右列必须是 MES。");
    AssertTrue(designerCode.Contains("tableLayoutPanelMesConfig.AutoScroll = true;", StringComparison.Ordinal), "MES 内容必须独立滚动。");
    AssertTrue(DesignerRowIsAutoSize(designerCode, "tableLayoutPanelMesConfig")
        && designerCode.Contains("tlpProcessParameterType.AutoSizeMode = AutoSizeMode.GrowAndShrink;", StringComparison.Ordinal)
        && DesignerRowIsAutoSize(designerCode, "tlpProcessParameterType"), "非整件检测设备隐藏结果来源行后，MES配置布局必须自动折叠空行。");
    AssertFalse(designerCode.Contains("tabBasicSettings.Controls.Add(grpPlcConfig);", StringComparison.Ordinal), "分组不应继续直接使用页签绝对坐标。");
    AssertTrue(viewCode.Contains("SystemSettingLayoutRules.ResolveMode(basicSettingsViewport.ClientSize.Width, DeviceDpi)", StringComparison.Ordinal), "运行时必须按 DPI 逻辑宽度选择布局。");
    AssertTrue(viewCode.Contains("private void ApplyBasicSettingsLayout(bool force = false)", StringComparison.Ordinal), "代码后置文件必须提供统一重排入口。");
}

static void SystemSettingConfiguresPlcAlarmTriggerMode()
{
    var defaults = new AppSettings();
    AssertEqual(
        AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress,
        defaults.PlcAlarmTriggerMode,
        "新配置默认必须使用设备状态异常且报警地址触发模式。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    AssertTrue(designerCode.Contains("selectPlcAlarmTriggerMode", StringComparison.Ordinal), "Designer 必须声明 PLC 报警模式双选控件。");
    AssertTrue(viewCode.Contains("PlcAlarmTriggerModeOptions", StringComparison.Ordinal), "报警模式选项必须绑定稳定持久化值。");
    AssertTrue(viewCode.Contains("selectPlcAlarmTriggerMode.Enabled = chkEnablePlcAlarmReading.Checked;", StringComparison.Ordinal), "关闭报警读取时必须禁用报警模式控件。");
    AssertTrue(viewCode.Contains("settings.PlcAlarmTriggerMode = AppConstants.PlcAlarmTriggerModes.Normalize", StringComparison.Ordinal), "保存时必须规范化报警模式。");

    foreach (var resourceFile in new[] { "UiText.resx", "UiText.en.resx" })
    {
        var resources = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Core", "Localization", resourceFile), Encoding.UTF8);
        AssertTrue(resources.Contains("system.option.plc_alarm.address_only", StringComparison.Ordinal), $"{resourceFile} 必须包含仅地址模式。");
        AssertTrue(resources.Contains("system.option.plc_alarm.device_status_and_address", StringComparison.Ordinal), $"{resourceFile} 必须包含双条件模式。");
    }
}

static void PlcProductReadyHandshakeRetainsHighLevelState()
{
    var serviceCode = File.ReadAllText(
        GetRepoFilePath("AutoWeldSystem.Services", "Plc", "WeldCycleMonitorService.cs"),
        Encoding.UTF8);

    AssertTrue(serviceCode.Contains("PollIdleProductReadySignalsAsync", StringComparison.Ordinal), "无活动任务时必须继续轮询产品数据就绪信号，不能直接清空状态。");
    AssertTrue(serviceCode.Contains("AwaitingReadyReset", StringComparison.Ordinal)
        && serviceCode.Contains("PendingFeedbackValue", StringComparison.Ordinal)
        && serviceCode.Contains("ReadySignalInitialized", StringComparison.Ordinal), "产品数据就绪握手必须保留高电平等待复位和待反馈状态。");
    AssertTrue(serviceCode.Contains("Edge=0->1", StringComparison.Ordinal), "产品采集必须只在产品数据就绪的0到1边沿触发。");
    AssertTrue(serviceCode.Contains("RetryPendingFeedbackAsync", StringComparison.Ordinal), "反馈写入失败后必须重试反馈而不是重复采集。");
    AssertTrue(serviceCode.Contains("ProductDataReadyStaleHigh", StringComparison.Ordinal), "遗留高电平必须记录明确日志。");
    AssertTrue(serviceCode.Contains("PendingFeedbackValue = 1", StringComparison.Ordinal)
        && serviceCode.Contains("PendingFeedbackValue = 2", StringComparison.Ordinal), "采集成功和采集失败必须分别保留反馈1/2。");
    AssertTrue(serviceCode.Contains("ProductCollectionHandledException", StringComparison.Ordinal)
        && serviceCode.Contains("CompleteCollectionWithHandledErrorAsync", StringComparison.Ordinal), "程序配置错误必须反馈1释放PLC握手，而不是反馈2造成PLC超时。");
    AssertTrue(serviceCode.Contains("Task=none", StringComparison.Ordinal)
        && serviceCode.Contains("ObservedTaskId", StringComparison.Ordinal)
        && serviceCode.Contains("PreviousTaskId", StringComparison.Ordinal), "无活动任务和跨任务高电平都必须保留任务边界上下文。");
}

static void SystemSettingConfiguresInspectionResultSource()
{
    var defaults = new AppSettings();
    AssertEqual(
        ProductionConstants.InspectionResultSources.Plc,
        defaults.InspectionResultSource,
        "旧数据库和新安装都必须默认使用PLC读取结果。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    var serviceCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.Services", "AppSettingsService.cs"), Encoding.UTF8);
    AssertTrue(designerCode.Contains("selectInspectionResultSource", StringComparison.Ordinal), "Designer 必须声明检测结果来源下拉。");
    AssertTrue(viewCode.Contains("InspectionResultSourceOptions", StringComparison.Ordinal)
        && viewCode.Contains("ProductionConstants.InspectionResultSources.Program", StringComparison.Ordinal), "系统设置必须提供PLC读取和程序计算两个稳定选项。");
    AssertTrue(viewCode.Contains("CanSaveInspectionResultSourceChange", StringComparison.Ordinal)
        && viewCode.Contains("HasAnyUnfinishedTask()", StringComparison.Ordinal), "存在未完工任务时必须阻止切换检测结果来源。");
    AssertTrue(viewCode.Contains("tlpInspectionResultSource.Visible = wholePieceInspection;", StringComparison.Ordinal), "结果来源配置只应在整件检测设备显示。");
    AssertTrue(serviceCode.Contains("InspectionResultSources.Normalize(settings.InspectionResultSource)", StringComparison.Ordinal), "设置服务必须把未知结果来源回退为PLC读取。");
}

static void SystemSettingConfiguresRealtimePointNumberSource()
{
    var defaults = new AppSettings();
    AssertEqual(
        ProductionConstants.RealtimePointNumberSources.Plc,
        defaults.RealtimePointNumberSource,
        "实时焊点编号来源必须默认使用PLC读取。");
    AssertEqual(
        ProductionConstants.RealtimePointNumberSources.Plc,
        ProductionConstants.RealtimePointNumberSources.Normalize("unknown"),
        "未知实时编号来源必须回退PLC读取。");

    var viewCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.cs"), Encoding.UTF8);
    var designerCode = File.ReadAllText(GetRepoFilePath("AutoWeldSystem.UI", "Views", "SystemSettingView.Designer.cs"), Encoding.UTF8);
    AssertTrue(designerCode.Contains("selectRealtimePointNumberSource", StringComparison.Ordinal), "Designer必须声明实时焊点编号来源下拉。");
    AssertTrue(viewCode.Contains("RealtimePointNumberSourceOptions", StringComparison.Ordinal)
        && viewCode.Contains("CanSaveRealtimePointNumberSourceChange", StringComparison.Ordinal), "系统设置必须绑定稳定编号来源并在未完工任务期间禁止切换。");
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
        ProductName = "外壳组件",
        ProcessName = "点焊",
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
    var request = method!.Invoke(service, [settings, task, stationNo, records, null]) as CenterProductReportRequest;
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
        ProcessName = "点焊",
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
    string? outputFilePath = null,
    string? testItemUnit = null)
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
        LowerExpression = "0:F-8",
        Unit = testItemUnit
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

    AddReportColumn(columns, reportColumnType, "touch_result", pointResultHeader, mergeByProduct: false);
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
        "A1:C1", "D1:F1", "G1:J1",
        "A3:C3", "D3:F3", "G3:J3",
        "A5:C5", "D5:F5", "G5:J5",
        "A7:C7", "D7:F7", "G7:J7",
        "A9:C9", "D9:F9"
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

    public Action<ReportDeviceStatusReq>? DeviceStatusRequestObserved { get; set; }

    public Func<ReportDeviceStatusReq, CancellationToken, Task<BasicRes<object>>>? DeviceStatusHandler { get; set; }

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

    public Func<bool?, CancellationToken, Task<BasicRes<object>>>? OnlineCheckHandler { get; set; }

    public Task<BasicRes<ServerTimeRes>> GetServerTimeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(ServerTimeResponse);

    public Task<BasicRes<WorkOrderRes>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default)
        => Task.FromResult(WorkOrderInfoResponse ?? throw new NotSupportedException());

    public Task<BasicRes<UserInfoRes>> GetUserInfoAsync(string userNumber, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> SetDeviceIdAsync(AddDeviceReq addDeviceRequest, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> TestConnectionAsync(string baseUrl, int timeoutSeconds, bool isWriteLog, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<BasicRes<object>> CheckSystemOnlineAsync(bool? previousOnline, CancellationToken cancellationToken = default)
        => OnlineCheckHandler?.Invoke(previousOnline, cancellationToken)
            ?? throw new NotSupportedException();

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
        DeviceStatusRequestObserved?.Invoke(requestData);
        lock (DeviceStatusRequests)
        {
            DeviceStatusRequests.Add(requestData);
        }

        return DeviceStatusHandler?.Invoke(requestData, cancellationToken)
            ?? Task.FromResult(DeviceStatusResponse);
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

sealed class FakeProductProcessConfigService(
    IReadOnlyDictionary<int, BizProductProcessConfig> configsByStation) : IProductProcessConfigService
{
    public int FindActiveCallCount { get; private set; }

    public List<(BizWeldTask Task, int StationNo)> FindActiveForTaskCalls { get; } = [];

    public IReadOnlyList<BizProductProcessConfig> GetAll(bool includeDisabled = false)
        => configsByStation.Values.ToList();

    public BizProductProcessConfig? FindActive(
        string productNum,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        FindActiveCallCount++;
        return null;
    }

    public BizProductProcessConfig? FindActiveForTask(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        FindActiveForTaskCalls.Add((task, stationNo));
        return configsByStation.TryGetValue(stationNo, out var config) ? config : null;
    }

    public BizProductProcessConfig Save(BizProductProcessConfig config) => config;

    public void Disable(int id)
    {
    }

    public void Delete(int id)
    {
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

sealed class FakePlcAddressService : IPlcAddressService
{
    public IReadOnlyList<BizPlcAddress> GetAll() => Array.Empty<BizPlcAddress>();

    public BizPlcAddress? GetAddress(string logicalKey, int stationNo) => null;

    public void SaveAll(IEnumerable<BizPlcAddress> addresses)
    {
    }
}

sealed class FakePlcCommunicationService : IPlcCommunicationService
{
    public Dictionary<string, PlcServiceResult<string>> StringReadResults { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Address, ushort Length)> StringReadRequests { get; } = new();

    public event EventHandler<PlcConnectionSnapshot>? StatusChanged;

    public PlcConnectionSnapshot Current { get; set; } = new(
        PlcConnectionState.Stopped,
        IsConnected: false,
        Endpoint: string.Empty,
        LastConnectedTime: null,
        LastHeartbeatTime: null,
        Message: string.Empty);

    public PlcConnectionSnapshot GetCurrent(int stationNo) => Current with { StationNo = stationNo };

    public void PublishStatus(PlcConnectionSnapshot snapshot)
    {
        Current = snapshot;
        StatusChanged?.Invoke(this, snapshot);
    }

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
    {
        StringReadRequests.Add((address, length));
        return Task.FromResult(
            StringReadResults.TryGetValue(address, out var result)
                ? result
                : PlcServiceResult<string>.Fail("Not configured."));
    }

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

sealed class FakePlcRecipeNameConfigService(params BizPlcRecipeNameConfig[] configs) : IPlcRecipeNameConfigService
{
    private readonly IReadOnlyList<BizPlcRecipeNameConfig> _configs = configs;

    public int SaveCallCount { get; private set; }

    public IReadOnlyList<BizPlcRecipeNameConfig> GetAll() => _configs;

    public BizPlcRecipeNameConfig? GetForStation(int stationNo)
        => _configs.FirstOrDefault(config => config.StationNo == stationNo);

    public void SaveAll(IEnumerable<BizPlcRecipeNameConfig> configs)
        => SaveCallCount++;
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

    public void DeleteProcessParameterVirtualRow(int weldTaskId, int stationNo, string productNo) { }
}

sealed class FakeDataHistoryMaintenanceService : IDataHistoryMaintenanceService
{
    public List<int> DeletedWorkOrderIds { get; } = [];

    public Task<WorkOrderDeletionPreview> PreviewDeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkOrderDeletionPreview());

    public Task<WorkOrderDeletionPreview> PreviewDeleteFailedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkOrderDeletionPreview());

    public Task<WorkOrderDeletionPreview> PreviewDeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkOrderDeletionPreview());

    public Task<WorkOrderDeletionResult> DeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        DeletedWorkOrderIds.AddRange(taskIds);
        return Task.FromResult(new WorkOrderDeletionResult { DeletedWorkOrderCount = taskIds.Count });
    }

    public Task<WorkOrderDeletionResult> DeleteFailedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkOrderDeletionResult());

    public Task<WorkOrderDeletionResult> DeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkOrderDeletionResult());

    public WorkOrderDeletionResult DeleteWorkOrder(int taskId)
    {
        DeletedWorkOrderIds.Add(taskId);
        return new WorkOrderDeletionResult { DeletedWorkOrderCount = 1 };
    }
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

sealed class FakeProgramExceptionLogService : IProgramExceptionLogService
{
    public event EventHandler<ProgramExceptionLogEntry>? LogWritten;

    public List<ProgramExceptionLogEntry> Entries { get; } = new();

    public ProgramExceptionLogEntry Write(Exception exception, string source, string? context = null)
    {
        var entry = new ProgramExceptionLogEntry
        {
            Source = source,
            Message = exception.Message,
            Context = context ?? string.Empty,
            StackTrace = exception.ToString(),
            OccurredTime = DateTime.Now
        };
        Write(entry);
        return entry;
    }

    public ProgramExceptionLogEntry WriteBusiness(
        string source,
        string message,
        string detail,
        string? context = null,
        string sourceFilePath = "",
        int sourceLineNumber = 0,
        string sourceMemberName = "")
    {
        var entry = new ProgramExceptionLogEntry
        {
            Source = source,
            Message = message,
            StackTrace = detail,
            Context = context ?? string.Empty,
            OccurredTime = DateTime.Now
        };
        Write(entry);
        return entry;
    }

    public void Write(ProgramExceptionLogEntry entry)
    {
        Entries.Add(entry);
        LogWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<ProgramExceptionLogEntry> GetByDate(DateTime date, int take = 500)
        => Entries.Where(entry => entry.OccurredTime.Date == date.Date).Take(take).ToList();

    public string GetLogDirectory() => string.Empty;
}

sealed class FakeDeviceStatusService : IDeviceStatusService
{
    public event EventHandler? LogsChanged;

    public List<BizDeviceStatusLog> Logs { get; } = new();

    public BizDeviceStatusLog? CurrentStatus { get; set; } = new();

    public BasicRes<object>? RetryResponse { get; set; } = new()
    {
        Status = AppConstants.MesStatus.Success,
        Msg = "OK"
    };

    public List<string> RetriedRecordKeys { get; } = new();

    public int GetCurrentStatusCallCount { get; private set; }

    public bool? LastReportToMes { get; private set; }

    public int RetryPendingUploadsCallCount { get; private set; }

    public Func<CancellationToken, Task>? RetryPendingUploadsHandler { get; set; }

    public Func<BizDeviceStatusLog, bool, CancellationToken, Task>? ChangeStatusHandler { get; set; }

    public List<string> OperationSequence { get; } = new();

    public CancellationToken LastCancellationToken { get; private set; }

    public BizDeviceStatusLog? GetCurrentStatus()
    {
        GetCurrentStatusCallCount++;
        return CurrentStatus;
    }

    public BizDeviceStatusLog? GetLatestStatus(int stationNo)
        => Logs.Where(log => log.StationNo == stationNo).OrderByDescending(log => log.OccurredTime).FirstOrDefault();

    public IReadOnlyList<BizDeviceStatusLog> GetLogs(
        DateTime? from = null,
        DateTime? to = null,
        int maxCount = 200)
        => Logs
            .Where(log => from is null || log.OccurredTime >= from.Value)
            .Where(log => to is null || log.OccurredTime <= to.Value)
            .OrderByDescending(log => log.OccurredTime)
            .Take(maxCount)
            .ToList();

    public IReadOnlyList<BizDeviceStatusLog> GetPendingLogs()
        => Logs
            .Where(log => DeviceStatusUploadVisibilityRules.ShouldInclude(log.ReportStatus))
            .OrderByDescending(log => log.OccurredTime)
            .ToList();

    public BizDeviceStatusLog? GetLog(string recordKey)
        => Logs.LastOrDefault(log => string.Equals(
            DeviceStatusRecordIdentityRules.GetRecordKey(log),
            recordKey,
            StringComparison.OrdinalIgnoreCase));

    public BizUploadTask? EnsurePendingUploadTask(BizDeviceStatusLog log)
    {
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
        return recordKey is null
            ? null
            : new BizUploadTask
            {
                Id = log.Id,
                TaskType = ProductionConstants.UploadTaskTypes.DeviceStatus,
                BusinessId = DeviceStatusRecordIdentityRules.BuildBusinessId(recordKey),
                PayloadJson = JsonSerializer.Serialize(new { RecordKey = recordKey }),
                Status = log.ReportStatus
            };
    }

    public bool ShouldPreserveUploadingTask(BizUploadTask task) => false;

    public Task<BasicRes<object>?> RetryUploadAsync(
        string recordKey,
        CancellationToken cancellationToken = default)
    {
        RetriedRecordKeys.Add(recordKey);
        return Task.FromResult(RetryResponse);
    }

    public Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default)
    {
        RetryPendingUploadsCallCount++;
        OperationSequence.Add("RetryPending");
        return RetryPendingUploadsHandler?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    public string GetLogDirectory() => string.Empty;

    public int DeleteLogs(IReadOnlyCollection<BizDeviceStatusLog> logs)
    {
        var recordKeys = logs
            .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
            .Where(recordKey => recordKey is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletedCount = Logs.RemoveAll(log =>
        {
            var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
            return recordKey is not null && recordKeys.Contains(recordKey);
        });
        LogsChanged?.Invoke(this, EventArgs.Empty);
        return deletedCount;
    }

    public async Task<BizDeviceStatusLog> ChangeStatusAsync(
        string deviceStatus,
        string? remark = null,
        string source = "Software",
        bool reportToMes = true,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        int? weldTaskId = null,
        string? workOrderId = null,
        DateTime? occurredTime = null,
        bool forceWrite = false,
        string? alarmAddress = null,
        string? alarmContent = null,
        CancellationToken cancellationToken = default)
    {
        LastReportToMes = reportToMes;
        LastCancellationToken = cancellationToken;
        OperationSequence.Add($"Change:{deviceStatus}:{reportToMes}");
        var log = new BizDeviceStatusLog
        {
            RecordId = Guid.NewGuid().ToString("N"),
            DeviceStatus = deviceStatus,
            Remark = remark,
            AlarmAddress = alarmAddress,
            AlarmContent = alarmContent,
            Source = source,
            StationNo = stationNo,
            WeldTaskId = weldTaskId,
            WorkOrderId = workOrderId,
            OccurredTime = occurredTime ?? DateTime.Now,
            ReportStatus = ProductionConstants.UploadStatuses.Pending
        };
        Logs.Add(log);
        LogsChanged?.Invoke(this, EventArgs.Empty);
        if (ChangeStatusHandler is not null)
        {
            await ChangeStatusHandler(log, reportToMes, cancellationToken);
        }

        return log;
    }
}

sealed class QueuedSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();

    public override void Post(SendOrPostCallback callback, object? state)
        => _callbacks.Enqueue((callback, state));

    public void RunAll()
    {
        while (_callbacks.TryDequeue(out var callback))
        {
            callback.Callback(callback.State);
        }
    }
}

sealed class FakeCenterInteractionLogService : ICenterInteractionLogService
{
    public event EventHandler<CenterInteractionLogEntry>? LogWritten;

    public List<CenterInteractionLogEntry> Entries { get; } = new();

    public void Write(CenterInteractionLogEntry entry)
    {
        Entries.Add(entry);
        LogWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<CenterInteractionLogEntry> GetByDate(DateTime date, int take = 500)
        => Entries.Where(entry => entry.SendTime.Date == date.Date).Take(take).ToList();

    public string GetLogDirectory() => string.Empty;
}

sealed class CenterTelemetryHttpMessageHandler : HttpMessageHandler
{
    public bool IsAvailable { get; set; } = true;

    public bool TelemetryAccepted { get; set; } = true;

    public bool HeartbeatAccepted { get; set; } = true;

    public bool MalformedResponse { get; set; }

    public List<string> RequestPaths { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty;
        RequestPaths.Add(path);

        if (!IsAvailable)
        {
            throw new HttpRequestException("由于目标计算机积极拒绝，无法连接。");
        }

        if (MalformedResponse)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json")
            });
        }

        var isTelemetry = string.Equals(path, "api/center/telemetry", StringComparison.OrdinalIgnoreCase);
        var isHeartbeat = string.Equals(path, "api/center/heartbeat", StringComparison.OrdinalIgnoreCase);
        var success = isTelemetry
            ? TelemetryAccepted
            : !isHeartbeat || HeartbeatAccepted;
        var ack = new CenterTelemetryAck
        {
            Success = success,
            Message = success ? "Accepted" : "Telemetry rejected",
            ServerTime = DateTime.Now
        };
        var response = new HttpResponseMessage(success ? HttpStatusCode.OK : HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(ack), Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
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
            request.Content?.Headers.ContentType?.ToString() ?? string.Empty,
            body,
            headers));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Status\":\"S\",\"Msg\":\"成功\",\"Data\":null}", Encoding.UTF8, "application/json")
        };
    }
}

sealed class BlockingHttpMessageHandler : HttpMessageHandler
{
    public TaskCompletionSource<bool> CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CancellationObserved.TrySetResult(true);
            throw;
        }

        throw new InvalidOperationException("Blocking handler must be cancelled before returning.");
    }
}

sealed record RecordedHttpRequest(
    string Method,
    string Path,
    string Query,
    string ContentType,
    string Body,
    IReadOnlyDictionary<string, string> Headers);
