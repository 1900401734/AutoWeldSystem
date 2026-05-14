namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// 所有可本地化文本的键名常量。
/// 统一放在这里，避免各个页面各自手写字符串键。
/// </summary>
public static class TextKeys
{
    /// <summary>
    /// 通用文本键。
    /// </summary>
    public static class Common
    {
        public const string LanguageChinese = "common.language.chinese";
        public const string LanguageEnglish = "common.language.english";
        public const string TitleInfo = "common.title.info";
        public const string TitleWarning = "common.title.warning";
        public const string TitleError = "common.title.error";
        public const string TitleConfirmDelete = "common.title.confirm_delete";
        public const string TitleConfirmReset = "common.title.confirm_reset";
        public const string ActionAdd = "common.action.add";
        public const string ActionEdit = "common.action.edit";
        public const string ActionDelete = "common.action.delete";
        public const string ActionRefresh = "common.action.refresh";
        public const string ActionApply = "common.action.apply";
        public const string ActionSave = "common.action.save";
        public const string ActionCancel = "common.action.cancel";
        public const string ActionLogin = "common.action.login";
        public const string ActionSetRole = "common.action.set_role";
        public const string ActionResetPassword = "common.action.reset_password";
        public const string StatusEnabled = "common.status.enabled";
        public const string StatusNotLoggedIn = "common.status.not_logged_in";
        public const string SaveSuccess = "common.message.save_success";
        public const string SaveFailed = "common.message.save_failed";
        public const string StartupTimeSyncFailed = "common.message.startup_time_sync_failed";
        public const string StartupInitFailed = "common.message.startup_init_failed";
    }

    /// <summary>
    /// 登录相关文本键。
    /// </summary>
    public static class Auth
    {
        public const string LoginTitle = "auth.login.title";
        public const string LoginAppTitle = "auth.login.app_title";
        public const string LoginLabelAccount = "auth.login.label.account";
        public const string LoginLabelPassword = "auth.login.label.password";
        public const string LoginLabelLanguage = "auth.login.label.language";
        public const string LoginTip = "auth.login.tip";
        public const string EmptyCredentials = "auth.message.credentials_required";
        public const string InvalidCredentials = "auth.message.invalid_credentials";
        public const string UserDisabled = "auth.message.user_disabled";
        public const string RoleDisabled = "auth.message.role_disabled";
    }

    /// <summary>
    /// 操作工弹窗文本键。
    /// </summary>
    public static class Operator
    {
        public const string DialogTitle = "operator.dialog.title";
        public const string DialogLabel = "operator.dialog.label";
        public const string EmployeeNumberRequired = "operator.message.employee_number_required";
    }

    /// <summary>
    /// 主界面导航文本键。
    /// </summary>
    public static class Main
    {
        public const string NavMonitor = "main.nav.monitor";
        public const string NavDataManage = "main.nav.data_manage";
        public const string NavUserManage = "main.nav.user_manage";
        public const string NavProgramManage = "main.nav.program_manage";
        public const string NavLogManage = "main.nav.log_manage";
        public const string NavStateManage = "main.nav.state_manage";
        public const string NavSystemSetting = "main.nav.system_setting";
        public const string NavAddressManage = "main.nav.address_manage";
        public const string EmptyPermissionPage = "main.message.no_page_permission";
    }

    /// <summary>
    /// 监控页文本键。
    /// </summary>
    public static class Monitor
    {
        public static class Button
        {
            public const string ChangeWorkOrder = "monitor.button.change_work_order";
            public const string StartReport = "monitor.button.start_report";
            public const string FinishReport = "monitor.button.finish_report";
            public const string SwitchUser = "monitor.button.switch_user";
            public const string Logout = "monitor.button.logout";
        }

        public static class Label
        {
            public const string PlcState = "monitor.label.plc_state";
            public const string MesState = "monitor.label.mes_state";
            public const string DeviceStatus = "monitor.label.device_status";

            public const string CurrentUser = "monitor.label.current_user";
            public const string CurrentLang = "monitor.label.current_lang";
            public const string WorkOrderNo = "monitor.label.work_order_no";
            public const string ProgramName = "monitor.label.program_name";
            public const string ProductNo = "monitor.label.product_no";
            public const string ProductModel = "monitor.label.product_model";
            public const string BatchNo = "monitor.label.batch_no";
            public const string Spec = "monitor.label.spec";
            public const string PartName = "monitor.label.part_name";
            public const string DrawingNo = "monitor.label.drawing_no";
            public const string SequenceNo = "monitor.label.sequence_no";
            public const string ProcessNo = "monitor.label.process_no";
            public const string ProcessName = "monitor.label.process_name";
            public const string ProductionQuantity = "monitor.label.production_quantity";
        }

        public static class Group
        {
            public const string ExceptionTips = "monitor.group.exception_tips";
            public const string RunningStatus = "monitor.group.running_status";
            public const string ProductionMetrics = "monitor.group.production_metrics";
        }

        public static class Title
        {
            public const string AppTitle = "monitor.title.app";
            public const string SwitchUserTitle = "monitor.title.switch_user";
            public const string LogoutTitle = "monitor.title.logout";

            public const string Normal = "monitor.status.normal";
            public const string Warning = "monitor.status.warning";
            public const string Error = "monitor.status.error";
        }

        public static class Message
        {
            public const string SwitchUserConfirm = "monitor.message.switch_user_confirm";
            public const string LogoutConfirm = "monitor.message.logout_confirm";
            public const string PlcDisconnected = "monitor.message.plc_disconnected";
            public const string PlcReconnecting = "monitor.message.plc_reconnecting";
            public const string PlcConnected = "monitor.message.plc_connected";
            public const string PlcFaulted = "monitor.message.plc_faulted";
            public const string MesRequestFailed = "monitor.message.mes_request_failed";
            public const string WorkIdRequired = "monitor.message.work_id_required";
            public const string WorkOrderLoadFailed = "monitor.message.work_order_load_failed";
            public const string ProcessRequired = "monitor.message.process_required";
            public const string ProgramListEmpty = "monitor.message.program_list_empty";
            public const string ProgramDownloadFailed = "monitor.message.program_download_failed";
            public const string WorkOrderReady = "monitor.message.work_order_ready";
            public const string StartPrerequisiteMissing = "monitor.message.start_prerequisite_missing";
            public const string QuantityInvalid = "monitor.message.quantity_invalid";
            public const string OperatorValidationFailed = "monitor.message.operator_validation_failed";
            public const string StartSuccess = "monitor.message.start_success";
            public const string FinishPrerequisiteMissing = "monitor.message.finish_prerequisite_missing";
            public const string FinishSuccess = "monitor.message.finish_success";
        }

        public static class RuntimeStatus
        {
            public const string Idle = "monitor.runtime.idle";
            public const string LoadingWorkOrder = "monitor.runtime.loading_work_order";
            public const string LoadingPrograms = "monitor.runtime.loading_programs";
            public const string DownloadingProgram = "monitor.runtime.downloading_program";
            public const string ValidatingOperator = "monitor.runtime.validating_operator";
            public const string SubmittingStart = "monitor.runtime.submitting_start";
            public const string SubmittingFinish = "monitor.runtime.submitting_finish";
        }

        public static class RuntimeError
        {
            public const string WorkIdReadFailed = "monitor.error.work_id_read_failed";
            public const string ProductionCollectFailed = "monitor.error.production_collect_failed";
            public const string OperationFailed = "monitor.error.operation_failed";
        }

        public static class Dialog
        {
            public const string ScanWorkIdTitle = "monitor.dialog.scan_work_id_title";
            public const string ScanWorkIdPrompt = "monitor.dialog.scan_work_id_prompt";
            public const string SelectProcessTitle = "monitor.dialog.select_process_title";
            public const string SelectProcessPrompt = "monitor.dialog.select_process_prompt";
            public const string SelectProgramTitle = "monitor.dialog.select_program_title";
            public const string SelectProgramPrompt = "monitor.dialog.select_program_prompt";
            public const string ActualQuantityTitle = "monitor.dialog.actual_quantity_title";
            public const string ActualQuantityPrompt = "monitor.dialog.actual_quantity_prompt";
            public const string QualifiedQuantityTitle = "monitor.dialog.qualified_quantity_title";
            public const string QualifiedQuantityPrompt = "monitor.dialog.qualified_quantity_prompt";
            public const string FailedQuantityTitle = "monitor.dialog.failed_quantity_title";
            public const string FailedQuantityPrompt = "monitor.dialog.failed_quantity_prompt";
        }
    }

    public static class Permission
    {
        public const string PageMonitor = "permission.page.monitor";
        public const string PageDataManage = "permission.page.data_manage";
        public const string PageUserManage = "permission.page.user_manage";
        public const string PageProgramManage = "permission.page.program_manage";
        public const string PageLogManage = "permission.page.log_manage";
        public const string PageStateManage = "permission.page.state_manage";
        public const string PageSystemSetting = "permission.page.system_setting";
        public const string PageAddressManage = "permission.page.address_manage";
        public const string ButtonMonitorChangeWorkOrder = "permission.button.monitor.change_work_order";
        public const string ButtonMonitorStartReport = "permission.button.monitor.start_report";
        public const string ButtonMonitorFinishReport = "permission.button.monitor.finish_report";
        public const string ButtonAuthSwitchUser = "permission.button.auth.switch_user";
        public const string ButtonAuthLogout = "permission.button.auth.logout";
        public const string ButtonDataExport = "permission.button.data.export";
        public const string ButtonUserAdd = "permission.button.user.add";
        public const string ButtonUserEdit = "permission.button.user.edit";
        public const string ButtonUserDelete = "permission.button.user.delete";
        public const string ButtonUserAssignRole = "permission.button.user.assign_role";
        public const string ButtonUserResetPassword = "permission.button.user.reset_password";
        public const string ButtonRoleAdd = "permission.button.role.add";
        public const string ButtonRoleEdit = "permission.button.role.edit";
        public const string ButtonRoleDelete = "permission.button.role.delete";
        public const string ButtonRoleAssignPermissions = "permission.button.role.assign_permissions";
        public const string ButtonProgramAdd = "permission.button.program.add";
        public const string ButtonProgramEdit = "permission.button.program.edit";
        public const string ButtonProgramDelete = "permission.button.program.delete";
    }

    public static class Log
    {
        public const string TitleMesInteraction = "log.title.mes_interaction";
        public const string TabProductionFlow = "log.tab.production_flow";
        public const string TabProgramException = "log.tab.program_exception";
        public const string DescriptionMesInteraction = "log.description.mes_interaction";
        public const string DescriptionProgramException = "log.description.program_exception";
        public const string LabelDate = "log.label.date";
        public const string LabelKeyword = "log.label.keyword";
        public const string ButtonRefresh = "log.button.refresh";
        public const string ButtonOpenFolder = "log.button.open_folder";
        public const string ButtonOpenSource = "log.button.open_source";
        public const string ButtonCopyDetails = "log.button.copy_details";
        public const string ColumnSendTime = "log.column.send_time";
        public const string ColumnPurpose = "log.column.purpose";
        public const string ColumnMethod = "log.column.method";
        public const string ColumnHttpStatus = "log.column.http_status";
        public const string ColumnMesStatus = "log.column.mes_status";
        public const string ColumnSuccess = "log.column.success";
        public const string ColumnDuration = "log.column.duration";
        public const string ColumnUrl = "log.column.url";
        public const string ColumnOccurredTime = "log.column.occurred_time";
        public const string ColumnCategory = "log.column.category";
        public const string ColumnSeverity = "log.column.severity";
        public const string ColumnExceptionType = "log.column.exception_type";
        public const string ColumnMessage = "log.column.message";
        public const string ColumnSource = "log.column.source";
        public const string ColumnSourceLine = "log.column.source_line";
        public const string DetailRequest = "log.detail.request";
        public const string DetailResponse = "log.detail.response";
        public const string DetailBasicInfo = "log.detail.basic_info";
        public const string DetailStackTrace = "log.detail.stack_trace";
        public const string DetailContext = "log.detail.context";
        public const string DetailNoSelection = "log.detail.no_selection";
        public const string DetailNoExceptionSelection = "log.detail.no_exception_selection";
        public const string PlaceholderReserved = "log.placeholder.reserved";
        public const string ValueSuccess = "log.value.success";
        public const string ValueFailed = "log.value.failed";
        public const string ValueBusinessException = "log.value.business_exception";
        public const string ValueProgramException = "log.value.program_exception";
        public const string MessageSourceMissing = "log.message.source_missing";
        public const string MessageDetailsCopied = "log.message.details_copied";
        public const string MessageExceptionLogged = "log.message.exception_logged";
    }

    public static class DeviceStatus
    {
        public const string Running = "device.status.running";
        public const string Paused = "device.status.paused";
        public const string Stopped = "device.status.stopped";
        public const string Alarm = "device.status.alarm";
        public const string Unknown = "device.status.unknown";
    }

    public static class Production
    {
        public const string TotalProduction = "production.metric.total";
        public const string TargetProduction = "production.metric.target";
        public const string MesProductionQuantity = "production.metric.mes_quantity";
        public const string AcceptedQuantity = "production.metric.accepted";
        public const string RejectedQuantity = "production.metric.rejected";
        public const string AcceptedRate = "production.metric.accepted_rate";
        public const string RejectedRate = "production.metric.rejected_rate";
        public const string AchievementRate = "production.metric.achievement_rate";
        public const string MetricName = "production.grid.name";
        public const string MetricValue = "production.grid.value";
        public const string NotAvailable = "production.value.not_available";
    }

    /// <summary>
    /// MES 请求相关文本键。
    /// 这类消息由服务层和界面层共同复用。
    /// </summary>
    public static class Mes
    {
        public const string HttpError = "mes.message.http_error";
        public const string Timeout = "mes.message.timeout";
        public const string RequestException = "mes.message.request_exception";
        public const string StateChecking = "mes.state.checking";
        public const string StateConnected = "mes.state.connected";
        public const string StateDisconnected = "mes.state.disconnected";
    }

    /// <summary>
    /// 角色相关文本键。
    /// </summary>
    public static class Role
    {
        public const string TabTitle = "role.tab.permissions";
        public const string AddDialogTitle = "role.dialog.add_title";
        public const string EditDialogTitle = "role.dialog.edit_title";
        public const string LabelCode = "role.label.code";
        public const string LabelName = "role.label.name";
        public const string LabelDescription = "role.label.description";
        public const string LabelEnabled = "role.label.enabled";
        public const string NameRequired = "role.message.name_required";
        public const string CodeExists = "role.message.code_exists";
        public const string NotExists = "role.message.not_exists";
        public const string SelectFirst = "role.message.select_first";
        public const string PermissionsApplied = "role.message.permissions_applied";
        public const string DeleteConfirm = "role.message.delete_confirm";
        public const string DeleteBlocked = "role.message.delete_blocked";
        public const string CurrentSelection = "role.message.current_selection";
        public const string NoMatch = "role.message.no_match";
        public const string SelectLeft = "role.message.select_left";
        public const string PermissionHint = "role.message.permission_hint";
    }

    /// <summary>
    /// 用户相关文本键。
    /// </summary>
    public static class User
    {
        public const string TabTitle = "user.tab.manage";
        public const string AddDialogTitle = "user.dialog.add_title";
        public const string EditDialogTitle = "user.dialog.edit_title";
        public const string LabelName = "user.label.name";
        public const string LabelNumber = "user.label.number";
        public const string LabelPassword = "user.label.password";
        public const string LabelRole = "user.label.role";
        public const string NameRequired = "user.message.name_required";
        public const string NumberRequired = "user.message.number_required";
        public const string NumberExists = "user.message.number_exists";
        public const string NotExists = "user.message.not_exists";
        public const string InvalidRole = "user.message.invalid_role";
        public const string RoleRequired = "user.message.role_required";
        public const string SingleRoleOnly = "user.message.single_role_only";
        public const string NoEnabledRoles = "user.message.no_enabled_roles";
        public const string SelectFirst = "user.message.select_first";
        public const string DeleteConfirm = "user.message.delete_confirm";
        public const string DeleteSelfBlocked = "user.message.delete_self_blocked";
        public const string ResetPasswordConfirm = "user.message.reset_password_confirm";
        public const string ResetPasswordSuccess = "user.message.reset_password_success";
        public const string AddSuccessWithPassword = "user.message.add_success_with_password";
        public const string AddSuccessDefaultPassword = "user.message.add_success_default_password";
    }

    /// <summary>
    /// 程序管理页相关文本键。
    /// </summary>
    public static class ProgramManage
    {
        public const string ButtonSyncMes = "program.button.sync_mes";
        public const string ButtonPullMes = "program.button.pull_mes";
        public const string ButtonBuildName = "program.button.build_name";
        public const string ButtonBrowseFile = "program.button.browse_file";
        public const string CheckSyncNow = "program.checkbox.sync_now";
        public const string PlaceholderKeyword = "program.placeholder.keyword";
        public const string GroupRevisions = "program.group.revisions";
        public const string LabelProgramName = "program.label.program_name";
        public const string LabelProductNum = "program.label.product_num";
        public const string LabelProductModel = "program.label.product_model";
        public const string LabelComponentCode = "program.label.component_code";
        public const string LabelSequenceNumber = "program.label.sequence_number";
        public const string LabelProgramType = "program.label.program_type";
        public const string LabelWeldJobName = "program.label.weld_job_name";
        public const string LabelRobotJobName = "program.label.robot_job_name";
        public const string LabelCycleTime = "program.label.cycle_time";
        public const string LabelProgramFile = "program.label.program_file";
        public const string LabelCommitMessage = "program.label.commit_message";
        public const string LabelRemark = "program.label.remark";
        public const string LabelProgramContent = "program.label.program_content";
        public const string OptionParameterString = "program.option.parameter_string";
        public const string OptionFile = "program.option.file";
        public const string CommitCreate = "program.commit.create";
        public const string CommitUpdate = "program.commit.update";
        public const string CurrentNew = "program.message.current_new";
        public const string CurrentSelected = "program.message.current_selected";
        public const string SaveSuccess = "program.message.save_success";
        public const string SelectDelete = "program.message.select_delete";
        public const string DeleteConfirm = "program.message.delete_confirm";
        public const string SelectSync = "program.message.select_sync";
        public const string PullSuccess = "program.message.pull_success";
        public const string SequenceInvalid = "program.message.sequence_invalid";
        public const string CycleTimeInvalid = "program.message.cycle_time_invalid";
        public const string DialogSelectFile = "program.dialog.select_file";
        public const string DialogFileFilterAll = "program.dialog.file_filter_all";
        public const string StatusPendingCreate = "program.status.pending_create";
        public const string StatusPendingUpdate = "program.status.pending_update";
        public const string StatusPendingDelete = "program.status.pending_delete";
        public const string StatusSynced = "program.status.synced";
        public const string StatusFailed = "program.status.failed";
        public const string StatusDeleted = "program.status.deleted";
        public const string ActionCreate = "program.action.create";
        public const string ActionUpdate = "program.action.update";
        public const string ActionDelete = "program.action.delete";
    }

    /// <summary>
    /// 上传状态页相关文本键。
    /// </summary>
    public static class StateManage
    {
        public const string Title = "state.title";
        public const string Description = "state.description";
        public const string ButtonRetrySelected = "state.button.retry_selected";
        public const string ButtonRetryAll = "state.button.retry_all";
        public const string SummaryPendingPrograms = "state.summary.pending_programs";
        public const string MessageSelectPending = "state.message.select_pending";
    }

    /// <summary>
    /// 系统设置页相关文本键。
    /// 当前页面的静态标签、按钮文字和提示信息统一都走这里。
    /// </summary>
    public static class SystemSetting
    {
        public const string Title = "system.title";
        public const string Description = "system.description";
        public const string GroupPlc = "system.group.plc";
        public const string GroupController = "system.group.controller";
        public const string GroupApplication = "system.group.application";
        public const string LabelIp = "system.label.ip";
        public const string LabelPort = "system.label.port";
        public const string LabelType = "system.label.type";
        public const string LabelDeviceId = "system.label.device_id";
        public const string LabelDeviceName = "system.label.device_name";
        public const string LabelDeviceStatusUrl = "system.label.device_status_url";
        public const string LabelLogPath = "system.label.log_path";
        public const string LabelDataPath = "system.label.data_path";
        public const string LabelMesUrl = "system.label.mes_url";
        public const string LabelUseProductNumberFilter = "system.label.use_product_number_filter";
        public const string ButtonConnect = "system.button.connect";
        public const string ButtonSyncDevice = "system.button.sync_device";
        public const string ButtonChangePath = "system.button.change_path";
        public const string ButtonOpenFolder = "system.button.open_folder";
        public const string ButtonTestConnection = "system.button.test_connection";
        public const string ButtonApplyAll = "system.button.apply_all";
        public const string PlcTypeModbusTcp = "system.plc_type.modbus_tcp";
        public const string PlcTypeTcpSocket = "system.plc_type.tcp_socket";
        public const string PlcTypeSiemensS7 = "system.plc_type.siemens_s7";
        public const string MessageValueRequired = "system.message.value_required";
        public const string MessageInvalidIp = "system.message.invalid_ip";
        public const string MessageInvalidPort = "system.message.invalid_port";
        public const string MessageInvalidUrl = "system.message.invalid_url";
        public const string MessageFolderMissing = "system.message.folder_missing";
        public const string MessageSelectFolder = "system.message.select_folder";
        public const string MessageConnectionSuccess = "system.message.connection_success";
        public const string MessageConnectionFailed = "system.message.connection_failed";
        public const string MessageMesConnectionSuccess = "system.message.mes_connection_success";
        public const string MessageDeviceSyncSuccess = "system.message.device_sync_success";
        public const string MessageDeviceSyncFailed = "system.message.device_sync_failed";
    }

    /// <summary>
    /// PLC 通讯状态文本键。
    /// </summary>
    public static class Plc
    {
        public const string StateStopped = "plc.state.stopped";
        public const string StateConnecting = "plc.state.connecting";
        public const string StateConnected = "plc.state.connected";
        public const string StateReconnecting = "plc.state.reconnecting";
        public const string StateDisconnected = "plc.state.disconnected";
        public const string StateFaulted = "plc.state.faulted";
        public const string MessageServiceStopped = "plc.message.service_stopped";
        public const string MessageConnecting = "plc.message.connecting";
        public const string MessageAlreadyConnected = "plc.message.already_connected";
        public const string MessageConnected = "plc.message.connected";
        public const string MessageUnsupportedType = "plc.message.unsupported_type";
        public const string MessageHeartbeatSkipped = "plc.message.heartbeat_skipped";
        public const string MessageHeartbeatSucceeded = "plc.message.heartbeat_succeeded";
        public const string MessageHeartbeatFailed = "plc.message.heartbeat_failed";
        public const string MessageTcpProbeFailed = "plc.message.tcp_probe_failed";
        public const string MessageNotConnected = "plc.message.not_connected";
        public const string MessageAddressRequired = "plc.message.address_required";
        public const string MessageWriteSucceeded = "plc.message.write_succeeded";
        public const string MessageTimeout = "plc.message.timeout";
    }

    /// <summary>
    /// 地址维护页文本键。
    /// </summary>
    public static class Address
    {
        public const string Title = "address.title";
        public const string Description = "address.description";
        public const string ButtonSave = "address.button.save";
        public const string ButtonRefresh = "address.button.refresh";
        public const string ButtonTest = "address.button.test";
        public const string MessageSelectFirst = "address.message.select_first";
        public const string MessageAddressRequired = "address.message.address_required";
        public const string MessageSaveSuccess = "address.message.save_success";
        public const string MessageSaveFailed = "address.message.save_failed";
        public const string MessageTestSuccess = "address.message.test_success";
        public const string MessageTestFailed = "address.message.test_failed";
        public const string NamePcHeartbeat = "address.name.pc_heartbeat";
        public const string NamePlcHeartbeat = "address.name.plc_heartbeat";
        public const string NameDeviceStatus = "address.name.device_status";
        public const string NameWeldStart = "address.name.weld_start";
        public const string NameWeldEnd = "address.name.weld_end";
        public const string NameWorkId = "address.name.work_id";
        public const string NameSerialNumber = "address.name.serial_number";
        public const string NameProgramName = "address.name.program_name";
        public const string NameProductModel = "address.name.product_model";
        public const string NameTotalProduction = "address.name.total_production";
        public const string NameTargetProduction = "address.name.target_production";
        public const string NameAcceptedQuantity = "address.name.accepted_quantity";
        public const string NameRejectedQuantity = "address.name.rejected_quantity";
    }

    /// <summary>
    /// 表格列标题文本键。
    /// </summary>
    public static class Grid
    {
        public const string RoleCode = "grid.role.code";
        public const string RoleName = "grid.role.name";
        public const string RoleDescription = "grid.role.description";
        public const string RoleEnabled = "grid.role.enabled";
        public const string RoleUpdatedTime = "grid.role.updated_time";
        public const string UserId = "grid.user.id";
        public const string UserNumber = "grid.user.number";
        public const string UserName = "grid.user.name";
        public const string UserRole = "grid.user.role";
        public const string UserEnabled = "grid.user.enabled";
        public const string UserLastLoginTime = "grid.user.last_login";
        public const string UserUpdatedTime = "grid.user.updated_time";
        public const string PlcAddressKey = "grid.plc_address.key";
        public const string PlcAddressName = "grid.plc_address.name";
        public const string PlcAddressSort = "grid.plc_address.sort";
        public const string PlcAddress = "grid.plc_address.address";
        public const string PlcAddressDataType = "grid.plc_address.data_type";
        public const string PlcAddressDataLength = "grid.plc_address.data_length";
        public const string PlcAddressEnabled = "grid.plc_address.enabled";
        public const string PlcAddressDescription = "grid.plc_address.description";
        public const string PlcAddressUpdatedTime = "grid.plc_address.updated_time";
        public const string ProgramName = "grid.program.name";
        public const string ProgramProductNum = "grid.program.product_num";
        public const string ProgramVersionNumber = "grid.program.version_number";
        public const string ProgramSyncStatus = "grid.program.sync_status";
        public const string ProgramSyncAction = "grid.program.sync_action";
        public const string ProgramSyncMessage = "grid.program.sync_message";
        public const string ProgramCommitId = "grid.program.commit_id";
        public const string ProgramCommitMessage = "grid.program.commit_message";
        public const string ProgramCommitUser = "grid.program.commit_user";
        public const string ProgramCommitTime = "grid.program.commit_time";
        public const string ProgramUpdatedTime = "grid.program.updated_time";
        public const string ProgramLastSyncTime = "grid.program.last_sync_time";
    }
}
