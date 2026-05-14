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
            public const string AssignPermissions = "button.role.assign-permissions";
        }

        public static class Program
        {
            public const string Add = "button.program.add";
            public const string Edit = "button.program.edit";
            public const string Delete = "button.program.delete";
        }

        public static class Data
        {
            public const string Export = "button.data.export";
        }

        public static class Monitor
        {
            public const string ChangeWorkOrder = "button.monitor.change-work-order";
            public const string StartReport = "button.monitor.start-report";
            public const string FinishReport = "button.monitor.finish-report";
        }

        public static class Auth
        {
            public const string SwitchUser = "button.auth.switch-user";
            public const string Logout = "button.auth.logout";
        }
    }
}
