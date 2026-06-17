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
    /// 主界面导航文本键。
    public static class Main
    {
        /// <summary>
        /// 生产监控
        /// </summary>
        public const string NavMonitor = "main.nav.monitor";
        /// <summary>
        /// 数据管理
        /// </summary>
        public const string NavDataManage = "main.nav.data_manage";
        /// <summary>
        /// 用户管理
        /// </summary>
        public const string NavUserManage = "main.nav.user_manage";
        /// <summary>
        /// 程序管理
        /// </summary>
        public const string NavProgramManage = "main.nav.program_manage";
        /// <summary>
        /// 日志管理
        /// </summary>
        public const string NavLogManage = "main.nav.log_manage";
        /// <summary>
        /// 上传状态
        /// </summary>
        public const string NavStateManage = "main.nav.state_manage";
        /// <summary>
        /// 系统设置
        /// </summary>
        public const string NavSystemSetting = "main.nav.system_setting";
        /// <summary>
        /// 地址维护
        /// </summary>
        public const string NavAddressManage = "main.nav.address_manage";
        /// <summary>
        /// 当前用户未分配任何页面权限
        /// </summary>
        public const string EmptyPermissionPage = "main.message.no_page_permission";
    }

    /// <summary>
    /// 生产监控页文本键。
    /// </summary>
    public static class Monitor
    {
        public static class Button
        {
            /// <summary>
            /// 变更
            /// </summary>
            public const string ChangeWorkOrder = "monitor.button.change_work_order";
            /// <summary>
            /// 微调
            /// </summary>
            public const string EditWO = "monitor.button.edit";
            /// <summary>
            /// 离线开工
            /// </summary>
            public const string LocalWorkOrder = "monitor.button.local_work_order";
            /// <summary>
            /// 开工上报
            /// </summary>
            public const string StartReport = "monitor.button.start_report";
            /// <summary>
            /// 完工上报
            /// </summary>
            public const string FinishReport = "monitor.button.finish_report";
            /// <summary>
            /// 切换用户
            /// </summary>
            public const string SwitchUser = "monitor.button.switch_user";
            /// <summary>
            /// 退出登录
            /// </summary>
            public const string Logout = "monitor.button.logout";
        }

        public static class Label
        {
            /// <summary>
            /// 序号
            /// </summary>
            public const string SequenceNo = "monitor.label.sequence_no";

            // ----- 连接状态 -----

            /// <summary>
            /// PLC状态
            /// </summary>
            public const string PlcState = "monitor.label.plc_state";
            /// <summary>
            /// MES状态
            /// </summary>
            public const string MesState = "monitor.label.mes_state";
            /// <summary>
            /// 设备状态
            /// </summary>
            public const string DeviceState = "monitor.label.device_state";
            /// <summary>
            /// 工单状态
            /// </summary>
            public const string WorkOrderState = "monitor.label.work_order_state";

            // ----- 工单信息 -----

            /// <summary>
            /// 当前用户
            /// </summary>
            public const string CurrentUser = "monitor.label.current_user";
            /// <summary>
            /// 当前语言
            /// </summary>
            public const string CurrentLang = "monitor.label.current_lang";
            /// <summary>
            /// 当前工位
            /// </summary>
            public const string Station = "monitor.label.station";
            /// <summary>
            /// 工单号
            /// </summary>
            public const string WorkOrderNo = "monitor.label.work_order_no";
            /// <summary>
            /// 程序名称
            /// </summary>
            public const string ProgramName = "monitor.label.program_name";
            /// <summary>
            /// 产品工号
            /// </summary>
            public const string ProductNumber = "monitor.label.product_number";
            /// <summary>
            /// 产品型号
            /// </summary>
            public const string ProductModel = "monitor.label.product_model";
            /// <summary>
            /// 批次
            /// </summary>
            public const string Batch = "monitor.label.batch";
            /// <summary>
            /// 规格
            /// </summary>
            public const string Spec = "monitor.label.spec";
            /// <summary>
            /// 部件名称
            /// </summary>
            public const string PartName = "monitor.label.part_name";
            /// <summary>
            /// 零件图号
            /// </summary>
            public const string DrawingNo = "monitor.label.drawing_no";
            /// <summary>
            /// 工序号
            /// </summary>
            public const string ProcessNo = "monitor.label.process_no";
            /// <summary>
            /// 工序名称
            /// </summary>
            public const string ProcessName = "monitor.label.process_name";
            /// <summary>
            /// 生产数量
            /// </summary>
            public const string ProductionQuantity = "monitor.label.production_quantity";
        }

        public static class Group
        {
            /// <summary>
            /// 异常提示：
            /// </summary>
            public const string ExceptionTips = "monitor.group.exception_tips";
            /// <summary>
            /// 运行状态：
            /// </summary>
            public const string RunningStatus = "monitor.group.running_status";
            /// <summary>
            /// 生产指标
            /// </summary>
            public const string ProductionMetrics = "monitor.group.production_metrics";
        }

        public static class Title
        {
            /// <summary>
            /// 切换用户
            /// </summary>
            public const string SwitchUserTitle = "monitor.title.switch_user";
            /// <summary>
            /// 退出登录
            /// </summary>
            public const string LogoutTitle = "monitor.title.logout";

            /// <summary>
            /// 正常
            /// </summary>
            public const string Normal = "monitor.status.normal";
            /// <summary>
            /// 警告
            /// </summary>
            public const string Warning = "monitor.status.warning";
            /// <summary>
            /// 错误
            /// </summary>
            public const string Error = "monitor.status.error";
        }

        /// <summary>
        /// 用于界面层和服务层交互的消息文本键。
        /// </summary>
        public static class Message
        {
            /// <summary>
            /// 当前无运行工单，请先开工上报
            /// </summary>
            public const string FinishPrerequisiteMissing = "monitor.message.finish_prerequisite_missing";
            /// <summary>
            /// 完工上报成功
            /// </summary>
            public const string FinishSuccess = "monitor.message.finish_success";
            /// <summary>
            /// 确定退出当前登录状态吗？
            /// </summary>
            public const string LogoutConfirm = "monitor.message.logout_confirm";
            /// <summary>
            /// MES请求失败：{0}
            /// </summary>
            public const string MesRequestFailed = "monitor.message.mes_request_failed";
            /// <summary>
            /// 员工校验失败
            /// </summary>
            public const string OperatorValidationFailed = "monitor.message.operator_validation_failed";
            /// <summary>
            /// PLC已连接
            /// </summary>
            public const string PlcConnected = "monitor.message.plc_connected";
            /// <summary>
            /// PLC未连接
            /// </summary>
            public const string PlcDisconnected = "monitor.message.plc_disconnected";
            /// <summary>
            /// PLC通信异常
            /// </summary>
            public const string PlcFaulted = "monitor.message.plc_faulted";
            /// <summary>
            /// PLC正在重连
            /// </summary>
            public const string PlcReconnecting = "monitor.message.plc_reconnecting";
            /// <summary>
            /// 当前工单没有可选工序
            /// </summary>
            public const string ProcessRequired = "monitor.message.process_required";
            /// <summary>
            /// 程序下载失败
            /// </summary>
            public const string ProgramDownloadFailed = "monitor.message.program_download_failed";
            /// <summary>
            /// 当前工单和设备没有可选程序
            /// </summary>
            public const string ProgramListEmpty = "monitor.message.program_list_empty";
            /// <summary>
            /// 数量必须是大于 0 的整数
            /// </summary>
            public const string QuantityInvalid = "monitor.message.quantity_invalid";
            /// <summary>
            /// 已开工或存在尚未完工任务
            /// </summary>
            public const string StartBlockedByUnfinishedTask = "monitor.message.start_blocked_by_unfinished_task";
            /// <summary>
            /// 请先加载工单、选择工序并下载程序
            /// </summary>
            public const string StartPrerequisiteMissing = "monitor.message.start_prerequisite_missing";
            /// <summary>
            /// 开工上报成功
            /// </summary>
            public const string StartSuccess = "monitor.message.start_success";
            /// <summary>
            /// 确定返回登录界面切换用户吗？
            /// </summary>
            public const string SwitchUserConfirm = "monitor.message.switch_user_confirm";
            /// <summary>
            /// 工单二维码不能为空
            /// </summary>
            public const string WorkIdRequired = "monitor.message.work_id_required";
            /// <summary>
            /// 获取工单信息失败
            /// </summary>
            public const string WorkOrderLoadFailed = "monitor.message.work_order_load_failed";
            /// <summary>
            /// 工单和程序已准备完成
            /// </summary>
            public const string WorkOrderReady = "monitor.message.work_order_ready";
        }

        public static class RuntimeStatus
        {
            /// <summary>
            /// 正在下载程序...
            /// </summary>
            public const string DownloadingProgram = "monitor.runtime.downloading_program";
            /// <summary>
            /// 等待业务操作
            /// </summary>
            public const string Idle = "monitor.runtime.idle";
            /// <summary>
            /// 正在获取程序列表...
            /// </summary>
            public const string LoadingPrograms = "monitor.runtime.loading_programs";
            /// <summary>
            /// 正在获取工单信息...
            /// </summary>
            public const string LoadingWorkOrder = "monitor.runtime.loading_work_order";
            /// <summary>
            /// 正在完工上报...
            /// </summary>
            public const string SubmittingFinish = "monitor.runtime.submitting_finish";
            /// <summary>
            /// 正在开工上报...
            /// </summary>
            public const string SubmittingStart = "monitor.runtime.submitting_start";
            /// <summary>
            /// 正在校验员工信息...
            /// </summary>
            public const string ValidatingOperator = "monitor.runtime.validating_operator";
        }

        public static class RuntimeError
        {
            /// <summary>
            /// 操作失败
            /// </summary>
            public const string OperationFailed = "monitor.error.operation_failed";
            /// <summary>
            /// 生产数据采集失败
            /// </summary>
            public const string ProductionCollectFailed = "monitor.error.production_collect_failed";
            /// <summary>
            /// 工单号读取失败
            /// </summary>
            public const string WorkIdReadFailed = "monitor.error.work_id_read_failed";
        }

        public static class ProductionHint
        {
            /// <summary>
            /// PLC业务信号写入失败
            /// </summary>
            public const string BusinessSignalWriteFailed = "monitor.production_hint.business_signal_write_failed";
            /// <summary>
            /// PLC业务信号写入成功
            /// </summary>
            public const string BusinessSignalWriteSucceeded = "monitor.production_hint.business_signal_write_succeeded";
            /// <summary>
            /// PLC采集反馈失败
            /// </summary>
            public const string ProductCollectionFeedbackFailed = "monitor.production_hint.product_collection_feedback_failed";
            /// <summary>
            /// PLC采集反馈成功
            /// </summary>
            public const string ProductCollectionFeedbackSucceeded = "monitor.production_hint.product_collection_feedback_succeeded";
            /// <summary>
            /// 正在采集产品数据
            /// </summary>
            public const string ProductCollectionStart = "monitor.production_hint.product_collection_start";
            /// <summary>
            /// PLC触发采集数据
            /// </summary>
            public const string ProductDataReady = "monitor.production_hint.product_data_ready";
            /// <summary>
            /// 正在采集产品数据
            /// </summary>
            public const string ProductDataReadStart = "monitor.production_hint.product_data_read_start";
            /// <summary>
            /// 数据保存成功
            /// </summary>
            public const string ProductDataSaved = "monitor.production_hint.product_data_saved";
            /// <summary>
            /// 数据保存失败
            /// </summary>
            public const string ProductDataSaveFailed = "monitor.production_hint.product_data_save_failed";
            /// <summary>
            /// 配方编号校验失败
            /// </summary>
            public const string RecipeCodeValidationFailed = "monitor.production_hint.recipe_code_validation_failed";
            /// <summary>
            /// 配方编号校验通过
            /// </summary>
            public const string RecipeCodeValidationSucceeded = "monitor.production_hint.recipe_code_validation_succeeded";
            /// <summary>
            /// 配方编号下发失败
            /// </summary>
            public const string RecipeCodeWriteFailed = "monitor.production_hint.recipe_code_write_failed";
            /// <summary>
            /// 配方编号已下发
            /// </summary>
            public const string RecipeCodeWriteSucceeded = "monitor.production_hint.recipe_code_write_succeeded";
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

    /// <summary>
    /// 数据管理页文本键。
    /// </summary>
    public static class DataManage
    {
        /// <summary>
        /// 产品工号
        /// </summary>
        public const string ProductNum = "data.label.product_num";
        /// <summary>
        /// 批次
        /// </summary>
        public const string Batch = "data.label.batch";
        /// <summary>
        /// 工单号
        /// </summary>
        public const string WorkOrderId = "data.label.work_order_id";
        /// <summary>
        /// 日期
        /// </summary>
        public const string DateRange = "data.label.date_range";
        /// <summary>
        /// 原始采集数据
        /// </summary>
        public const string RawData = "data.label.raw_data";

        /// <summary>
        /// 模糊查询
        /// </summary>
        public const string FuzzySearch = "data.placeholder.fuzzy_search";
        /// <summary>
        /// 工单号
        /// </summary>
        public const string WorkOrderPlaceholder = "data.placeholder.work_order";

        /// <summary>
        /// 查询
        /// </summary>
        public const string Query = "data.button.query";
        /// <summary>
        /// 重置
        /// </summary>
        public const string Reset = "data.button.reset";
        /// <summary>
        /// 打开报告
        /// </summary>
        public const string OpenReport = "data.button.open_report";
        /// <summary>
        /// 打开所在目录
        /// </summary>
        public const string OpenReportFolder = "data.button.open_report_folder";

        /// <summary>
        /// 焊接参数
        /// </summary>
        public const string TabWeldParameters = "data.tab.weld_parameters";
        /// <summary>
        /// 采集数据
        /// </summary>
        public const string TabCollectionData = "data.tab.collection_data";
        /// <summary>
        /// 报告文件
        /// </summary>
        public const string TabReportFiles = "data.tab.report_files";

        /// <summary>
        /// 采集数据：{0} 条
        /// </summary>
        public const string CollectionSummary = "data.summary.collection";
        /// <summary>
        /// 焊接参数：{0} 条，动态测试项列：{1} 列
        /// </summary>
        public const string ParameterSummary = "data.summary.parameters";
        /// <summary>
        /// 报告文件：{0} 个
        /// </summary>
        public const string ReportSummary = "data.summary.reports";
        /// <summary>
        /// 历史工单：{0} 条
        /// </summary>
        public const string WorkOrderSummary = "data.summary.work_orders";

        /// <summary>
        /// 工单明细查询失败：{0}
        /// </summary>
        public const string DetailQueryFailed = "data.message.detail_query_failed";
        /// <summary>
        /// 正在加载...
        /// </summary>
        public const string Loading = "data.message.loading";
        /// <summary>
        /// 打开路径失败：{0}
        /// </summary>
        public const string OpenPathFailed = "data.message.open_path_failed";
        /// <summary>
        /// 历史工单查询失败：{0}
        /// </summary>
        public const string QueryFailed = "data.message.query_failed";
        /// <summary>
        /// 报告文件目录不存在：{0}
        /// </summary>
        public const string ReportDirectoryMissing = "data.message.report_directory_missing";
        /// <summary>
        /// 报告文件不存在：{0}
        /// </summary>
        public const string ReportFileMissing = "data.message.report_file_missing";
        /// <summary>
        /// 请先选择报告文件。
        /// </summary>
        public const string SelectReport = "data.message.select_report";
        /// <summary>
        /// 请选择历史工单
        /// </summary>
        public const string SelectWorkOrder = "data.message.select_work_order";

        /// <summary>
        /// 工位
        /// </summary>
        public const string ColumnStation = "data.column.station";
        /// <summary>
        /// 工单号
        /// </summary>
        public const string ColumnWorkOrderId = "data.column.work_order_id";
        /// <summary>
        /// 产品工号
        /// </summary>
        public const string ColumnProductNum = "data.column.product_num";
        /// <summary>
        /// 批次
        /// </summary>
        public const string ColumnBatch = "data.column.batch";
        /// <summary>
        /// 产品名称/部件名称
        /// </summary>
        public const string ColumnProductName = "data.column.product_name";
        /// <summary>
        /// 工序
        /// </summary>
        public const string ColumnProcess = "data.column.process";
        /// <summary>
        /// 配方
        /// </summary>
        public const string ColumnRecipe = "data.column.recipe";
        /// <summary>
        /// 计划数量
        /// </summary>
        public const string ColumnPlannedQty = "data.column.planned_qty";
        /// <summary>
        /// 实际数量
        /// </summary>
        public const string ColumnActualQty = "data.column.actual_qty";
        /// <summary>
        /// 合格数量
        /// </summary>
        public const string ColumnQualifiedQty = "data.column.qualified_qty";
        /// <summary>
        /// 不良数量
        /// </summary>
        public const string ColumnFailedQty = "data.column.failed_qty";
        /// <summary>
        /// 操作员工号
        /// </summary>
        public const string ColumnOperator = "data.column.operator";
        /// <summary>
        /// 开始时间
        /// </summary>
        public const string ColumnStartTime = "data.column.start_time";
        /// <summary>
        /// 结束时间
        /// </summary>
        public const string ColumnEndTime = "data.column.end_time";
        /// <summary>
        /// 任务状态
        /// </summary>
        public const string ColumnTaskStatus = "data.column.task_status";
        /// <summary>
        /// 上传状态
        /// </summary>
        public const string ColumnUploadStatus = "data.column.upload_status";
        /// <summary>
        /// 产品编号
        /// </summary>
        public const string ColumnProductNo = "data.column.product_no";
        /// <summary>
        /// 焊点编号
        /// </summary>
        public const string ColumnTouchNo = "data.column.touch_no";
        /// <summary>
        /// 焊点结果
        /// </summary>
        public const string ColumnTouchResult = "data.column.touch_result";
        /// <summary>
        /// 采集时间
        /// </summary>
        public const string ColumnRecordTime = "data.column.record_time";
        /// <summary>
        /// 序号
        /// </summary>
        public const string ColumnSequence = "data.column.sequence";
        /// <summary>
        /// 试焊件
        /// </summary>
        public const string ColumnIsTest = "data.column.is_test";
        /// <summary>
        /// 产品完成
        /// </summary>
        public const string ColumnProductCompleted = "data.column.product_completed";
        /// <summary>
        /// 文件名
        /// </summary>
        public const string ColumnFileName = "data.column.file_name";
        /// <summary>
        /// 格式
        /// </summary>
        public const string ColumnFileFormat = "data.column.file_format";
        /// <summary>
        /// 文件路径
        /// </summary>
        public const string ColumnFilePath = "data.column.file_path";
        /// <summary>
        /// 创建时间
        /// </summary>
        public const string ColumnCreatedTime = "data.column.created_time";
        /// <summary>
        /// 更新时间
        /// </summary>
        public const string ColumnUpdatedTime = "data.column.updated_time";
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
    /// 权限相关文本键。
    /// </summary>
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
        public const string ButtonDataQuery = "permission.button.data.query";
        public const string ButtonDataReset = "permission.button.data.reset";
        public const string ButtonDataOpenReport = "permission.button.data.open_report";
        public const string ButtonDataOpenReportFolder = "permission.button.data.open_report_folder";
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
        public const string LabelProgramId = "program.label.program_id";
        public const string LabelProductNum = "program.label.product_num";
        public const string LabelProductModel = "program.label.product_model";
        public const string LabelRecipeCode = "program.label.recipe_code";
        public const string LabelComponentCode = "program.label.component_code";
        public const string LabelSequenceNumber = "program.label.sequence_number";
        public const string LabelProgramType = "program.label.program_type";
        public const string LabelProgramFile = "program.label.program_file";
        public const string LabelCommitMessage = "program.label.commit_message";
        public const string LabelRemark = "program.label.remark";
        public const string LabelLocalRemark = "program.label.local_remark";
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
    /// 日志管理页文本键
    /// </summary>
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

        public const string TabBasic = "system.tab.basic";

        public const string GroupPlc = "system.group.plc";
        public const string GroupController = "system.group.controller";
        public const string GroupApplication = "system.group.application";
        public const string GroupDevice = "system.group.device";
        public const string GroupProduction = "system.group.production";
        public const string GroupMes = "system.group.mes";

        public const string LabelIp = "system.label.ip";
        public const string LabelPort = "system.label.port";
        public const string LabelType = "system.label.type";
        public const string LabelDeviceId = "system.label.device_id";
        public const string LabelDeviceName = "system.label.device_name";
        public const string LabelDeviceStatusUrl = "system.label.device_status_url";
        public const string LabelLogPath = "system.label.log_path";
        public const string LabelDataPath = "system.label.data_path";
        public const string LabelMesUrl = "system.label.mes_url";
        public const string UploadMode = "system.label.upload_mode";
        public const string UploadBatchSize = "system.label.upload_batch_size";
        public const string PlcHeartbeatRate = "system.label.plc_heartbeat_rate";

        public const string ChkUseProductNumberFilter = "system.checkbox.use_product_number_filter";
        public const string ChkValidateRecipeAfterStart = "system.checkbox.validate_recipe_after_start";
        public const string ChkEnableFinishExpQtyPrompt = "system.checkbox.enable_finish_exp_qty_prompt";
        public const string ChkEnableDualStation = "system.Checkbox.enable_dual_station";
        public const string ChkEnableDualWorkOrder = "system.Checkbox.enable_dual_work_order";

        public const string ButtonConnect = "system.button.connect";
        public const string ButtonSyncDevice = "system.button.sync_device";
        public const string ButtonChangePath = "system.button.change_path";
        public const string ButtonOpenFolder = "system.button.open_folder";
        public const string ButtonTestConnection = "system.button.test_connection";
        public const string ButtonApplyAll = "system.button.apply_all";

        public const string PlcTypeModbusTcp = "system.plc_type.modbus_tcp";
        public const string PlcTypeTcpSocket = "system.plc_type.tcp_socket";
        public const string PlcTypeSiemensS71200 = "system.plc_type.siemens_s7";

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
        public const string NameWeldCollectionAck = "address.name.weld_collection_ack";
        public const string NameWorkId = "address.name.work_id";
        public const string NameSerialNumber = "address.name.serial_number";
        public const string NameProgramName = "address.name.program_name";
        public const string NameProductModel = "address.name.product_model";
        public const string NameRecipeCode = "address.name.recipe_code";
        public const string NamePcRecipeCode = "address.name.pc_recipe_code";
        public const string NamePlcRecipeCode = "address.name.plc_recipe_code";
        public const string NameWorkOrderStatus = "address.name.work_order_status";
        public const string NameDeviceMode = "address.name.device_mode";
        public const string NameProductDataReady = "address.name.product_data_ready";
        public const string NameProductCollectionFeedback = "address.name.product_collection_feedback";
        public const string NameTotalProduction = "address.name.total_production";
        public const string NameTargetProduction = "address.name.target_production";
        public const string NameAcceptedQuantity = "address.name.accepted_quantity";
        public const string NameRejectedQuantity = "address.name.rejected_quantity";
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
    /// 设备状态Tag
    /// </summary>
    public static class DeviceStatus
    {
        /// <summary>
        /// 运行
        /// </summary>
        public const string Running = "device.status.running";
        /// <summary>
        /// 暂停
        /// </summary>
        public const string Paused = "device.status.paused";
        /// <summary>
        /// 停止
        /// </summary>
        public const string Stopped = "device.status.stopped";
        /// <summary>
        /// 报警
        /// </summary>
        public const string Alarm = "device.status.alarm";
        /// <summary>
        /// 未知
        /// </summary>
        public const string Unknown = "device.status.unknown";
    }

    /// <summary>
    /// 工单状态Tag
    /// </summary>
    public static class WorkOrderStatus
    {
        /// <summary>
        /// 未开工
        /// </summary>
        public const string NotStarted = "work_order.status.not_started";
        /// <summary>
        /// 生产中
        /// </summary>
        public const string InProgress = "work_order.status.in_progress";
        /// <summary>
        /// 已暂停
        /// </summary>
        public const string Paused = "work_order.status.paused";
        /// <summary>
        /// 已完工
        /// </summary>
        public const string Completed = "work_order.status.completed";
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
        // ----- 连接状态 -----

        /// <summary>
        /// 检测中
        /// </summary>
        public const string StateChecking = "mes.state.checking";
        /// <summary>
        /// 已连接
        /// </summary>
        public const string StateConnected = "mes.state.connected";
        /// <summary>
        /// 已断开
        /// </summary>
        public const string StateDisconnected = "mes.state.disconnected";

        // ----- 消息提示 -----

        /// <summary>
        /// HTTP {0}：{1}
        /// </summary>
        public const string HttpError = "mes.message.http_error";
        /// <summary>
        /// MES 请求失败：{0}
        /// </summary>
        public const string RequestException = "mes.message.request_exception";
        /// <summary>
        /// MES 请求超时，已超过 {0} 秒。
        /// </summary>
        public const string Timeout = "mes.message.timeout";
    }

    /// <summary>
    /// PLC 通讯状态文本键。
    /// </summary>
    public static class Plc
    {
        // ----- 连接状态 -----

        /// <summary>
        /// 已停止
        /// </summary>
        public const string StateStopped = "plc.state.stopped";
        /// <summary>
        /// 连接中
        /// </summary>
        public const string StateConnecting = "plc.state.connecting";
        /// <summary>
        /// 已连接
        /// </summary>
        public const string StateConnected = "plc.state.connected";
        /// <summary>
        /// 重连中
        /// </summary>
        public const string StateReconnecting = "plc.state.reconnecting";
        /// <summary>
        /// 已断开
        /// </summary>
        public const string StateDisconnected = "plc.state.disconnected";
        /// <summary>
        /// 异常
        /// </summary>
        public const string StateFaulted = "plc.state.faulted";

        // ----- 消息提示 -----

        /// <summary>
        /// PLC 地址不能为空。
        /// </summary>
        public const string MessageAddressRequired = "plc.message.address_required";
        /// <summary>
        /// PLC 已连接。
        /// </summary>
        public const string MessageAlreadyConnected = "plc.message.already_connected";
        /// <summary>
        /// PLC 已连接：{0}。
        /// </summary>
        public const string MessageConnected = "plc.message.connected";
        /// <summary>
        /// 正在连接 {0}。
        /// </summary>
        public const string MessageConnecting = "plc.message.connecting";
        /// <summary>
        /// PLC 心跳检测失败：{0}
        /// </summary>
        public const string MessageHeartbeatFailed = "plc.message.heartbeat_failed";
        /// <summary>
        /// PLC心跳无变化，已持续 {0:0.0} 秒；TCP连接仍可建立，等待 PLC 业务心跳恢复。
        /// </summary>
        public const string MessageHeartbeatNoChange = "plc.message.heartbeat_no_change";
        /// <summary>
        /// PLC 心跳地址为空，TCP 端口探测成功。
        /// </summary>
        public const string MessageHeartbeatSkipped = "plc.message.heartbeat_skipped";
        /// <summary>
        /// PLC 心跳检测成功。
        /// </summary>
        public const string MessageHeartbeatSucceeded = "plc.message.heartbeat_succeeded";
        /// <summary>
        /// PLC 未连接。
        /// </summary>
        public const string MessageNotConnected = "plc.message.not_connected";
        /// <summary>
        /// PLC 服务已停止。
        /// </summary>
        public const string MessageServiceStopped = "plc.message.service_stopped";
        /// <summary>
        /// PLC TCP 探测失败：{0}
        /// </summary>
        public const string MessageTcpProbeFailed = "plc.message.tcp_probe_failed";
        /// <summary>
        /// 连接超时
        /// </summary>
        public const string MessageTimeout = "plc.message.timeout";
        /// <summary>
        /// 不支持的 PLC 类型：{0}
        /// </summary>
        public const string MessageUnsupportedType = "plc.message.unsupported_type";
        /// <summary>
        /// PLC 写入成功。
        /// </summary>
        public const string MessageWriteSucceeded = "plc.message.write_succeeded";

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

        #region 业务信号表格
        public const string Sort = "grid.plc_address.sort";
        public const string Name = "grid.plc_address.name";
        public const string Station = "grid.plc_address.station";
        public const string Address = "grid.plc_address.address";
        public const string DataType = "grid.plc_address.data_type";
        public const string Length = "grid.plc_address.data_length";
        public const string Enabled = "grid.plc_address.enabled";
        public const string Description = "grid.plc_address.description";
        public const string UpdatedTime = "grid.plc_address.updated_time";
        #endregion

        public const string ProgramName = "grid.program.name";
        public const string ProgramId = "grid.program.id";
        public const string ProgramProductNum = "grid.program.product_num";
        public const string ProgramProductModel = "grid.program.product_model";
        public const string ProgramRecipeCode = "grid.program.recipe_code";
        public const string ProgramLocalRemark = "grid.program.local_remark";
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
