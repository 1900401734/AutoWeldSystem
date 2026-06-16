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
            public const string BrowseFile = "button.program.browse-file";
            public const string BuildName = "button.program.build-name";
        }

        public static class Data
        {
            public const string Export = "button.data.export";
            public const string Query = "button.data.query";
            public const string Reset = "button.data.reset";
            public const string OpenReport = "button.data.open-report";
            public const string OpenReportFolder = "button.data.open-report-folder";
        }

        public static class Monitor
        {
            public const string ChangeWorkOrder = "button.monitor.change-work-order";
            public const string StartReport = "button.monitor.start-report";
            public const string FinishReport = "button.monitor.finish-report";
            public const string GetWorkOrder = "button.monitor.get-work-order";
            public const string EditWorkOrder = "button.monitor.edit-work-order";
            public const string LocalWorkOrder = "button.monitor.local-work-order";
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
            public const string ConnectMaster = "button.system.connect-master";
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
            public const string AddProductProcess = "button.address.add-product-process";
            public const string DeleteProductProcess = "button.address.delete-product-process";
            public const string PreviewProductProcessAddress = "button.address.preview-product-process-address";
            public const string AddScheme = "button.address.add-scheme";
            public const string DeleteScheme = "button.address.delete-scheme";
            public const string AddTestItem = "button.address.add-test-item";
            public const string DeleteTestItem = "button.address.delete-test-item";
        }
    }
}
