namespace AutoWeldSystem.Core.Constants;

public static class PermissionCodes
{
    public static class Pages
    {
        public const string Monitor = "page.monitor";
        public const string DataManage = "page.data.manage";
        public const string UserManage = "page.user.manage";
        public const string ProgramManage = "page.program.manage";
        public const string LogManage = "page.log.manage";
        public const string StateManage = "page.state.manage";
        public const string SystemSetting = "page.system.setting";
        public const string AddressManage = "page.address.manage";
    }

    public static class Buttons
    {
        public static class User
        {
            public const string Add = "button.user.add";
            public const string Edit = "button.user.edit";
            public const string Delete = "button.user.delete";
            public const string AssignRole = "button.user.assign-role";
            public const string ResetPassword = "button.user.reset-password";
        }

        public static class Role
        {
            public const string Add = "button.role.add";
            public const string Edit = "button.role.edit";
            public const string Delete = "button.role.delete";
            public const string Refresh = "button.role.refresh";
            public const string AssignPermissions = "button.role.assign-permissions";
        }

        public static class Program
        {
            public const string Add = "button.program.add";
            public const string Edit = "button.program.edit";
            public const string Delete = "button.program.delete";
            public const string Sync = "button.program.sync";
            public const string PullMes = "button.program.pull-mes";
            public const string Refresh = "button.program.refresh";
            public const string BuildName = "button.program.build-name";
        }

        public static class Data
        {
            public const string Query = "button.data.query";
            public const string Reset = "button.data.reset";
            public const string OpenReport = "button.data.open-report";
            public const string OpenReportFolder = "button.data.open-report-folder";
            public const string Delete = "button.data.delete";
        }

        public static class Monitor
        {
            public const string OnlineReport = "button.monitor.online-report";
            public const string LocalWorkOrder = "button.monitor.local-work-order";

            /// <summary>
            /// 监控页「合并显示」快捷开关。该开关写的是全局设置，只对管理员开放。
            /// </summary>
            public const string MergedDisplay = "button.monitor.merged-display";

            /// <summary>
            /// 监控页「面结果」快捷开关。该开关写的是全局设置，只对管理员开放。
            /// </summary>
            public const string FaceResultDisplay = "button.monitor.face-result-display";
        }

        public static class Auth
        {
            public const string SwitchUser = "button.auth.switch-user";
            public const string Logout = "button.auth.logout";
            public const string AddressPreview = "button.auth.address-preview";
        }

        public static class State
        {
            public const string RetrySelected = "button.state.retry-selected";
            public const string RetryAll = "button.state.retry-all";
            public const string Refresh = "button.state.refresh";
            public const string UploadAll = "button.state.upload-all";
            public const string Delete = "button.state.delete";
        }

        public static class Log
        {
            public const string Refresh = "button.log.refresh";
            public const string OpenFolder = "button.log.open-folder";
            public const string OpenSource = "button.log.open-source";
            public const string CopyDetails = "button.log.copy-details";
        }

        public static class SystemSetting
        {
            public const string Save = "button.system.save";
            public const string ConnectPlc = "button.system.connect-plc";
            public const string SyncDevice = "button.system.sync-device";
            public const string TestMes = "button.system.test-mes";
            public const string ChangePath = "button.system.change-path";
            public const string OpenPath = "button.system.open-path";
        }

        public static class Address
        {
            public const string Save = "button.address.save";
            public const string Refresh = "button.address.refresh";
            public const string Test = "button.address.test";
            public const string AddAlarm = "button.address.add-alarm";
            public const string DeleteAlarm = "button.address.delete-alarm";
            public const string PasteAlarm = "button.address.paste-alarm";
            public const string AddProductProcess = "button.address.add-product-process";
            public const string DeleteProductProcess = "button.address.delete-product-process";
            public const string PreviewProductProcessAddress = "button.address.preview-product-process-address";
            public const string AddScheme = "button.address.add-scheme";
            public const string DeleteScheme = "button.address.delete-scheme";
            public const string AddTestItem = "button.address.add-test-item";
            public const string DeleteTestItem = "button.address.delete-test-item";
        }
    }

    /// <summary>
    /// 页面内部页签权限。
    /// 页签权限只控制界面可见性，不改变后台业务任务的执行。
    /// </summary>
    public static class Tabs
    {
        public static class State
        {
            public const string WorkOrderInfo = "tab.state.work-order-info";
            public const string StartReport = "tab.state.start-report";
            public const string FinishReport = "tab.state.finish-report";
            public const string ProcessParameter = "tab.state.process-parameter";
            public const string ReportFile = "tab.state.report-file";
            public const string WorkOrderStatus = "tab.state.work-order-status";
            public const string DeviceStatus = "tab.state.device-status";
            public const string ProgramFile = "tab.state.program-file";

            /// <summary>
            /// 待上传数据页签的固定显示顺序。
            /// </summary>
            public static IReadOnlyList<string> All { get; } =
            [
                WorkOrderInfo,
                StartReport,
                FinishReport,
                ProcessParameter,
                ReportFile,
                WorkOrderStatus,
                DeviceStatus,
                ProgramFile
            ];

            /// <summary>
            /// 客户角色首次安装或升级时默认可见的页签。
            /// </summary>
            public static IReadOnlyList<string> CustomerDefaults { get; } =
            [
                WorkOrderInfo,
                DeviceStatus,
                ProgramFile
            ];
        }

        public static class Log
        {
            public const string MesInteraction = "tab.log.mes-interaction";
            public const string ProductionFlow = "tab.log.production-flow";
            public const string ProgramException = "tab.log.program-exception";
            public const string Device = "tab.log.device";
            public const string DeviceStatus = "tab.log.device-status";
            public const string Server = "tab.log.server";

            public static IReadOnlyList<string> All { get; } =
            [
                MesInteraction,
                ProductionFlow,
                ProgramException,
                Device,
                DeviceStatus,
                Server
            ];
        }

        public static class Address
        {
            public const string BusinessSignal = "tab.address.business-signal";
            public const string RecipeName = "tab.address.recipe-name";
            public const string Alarm = "tab.address.alarm";
            public const string ProductProcess = "tab.address.product-process";
            public const string TestPlan = "tab.address.test-plan";
            public const string PlanDetail = "tab.address.plan-detail";
            public const string TestItemDictionary = "tab.address.test-item-dictionary";

            public static IReadOnlyList<string> All { get; } =
            [
                BusinessSignal,
                RecipeName,
                Alarm,
                ProductProcess,
                TestPlan,
                PlanDetail,
                TestItemDictionary
            ];
        }
    }
}
