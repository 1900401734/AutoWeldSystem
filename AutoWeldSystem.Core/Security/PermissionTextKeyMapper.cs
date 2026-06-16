using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Security;

public static class PermissionTextKeyMapper
{
    public static string GetTextKey(string permissionCode)
    {
        return permissionCode switch
        {
            PermissionCodes.Pages.Monitor => TextKeys.Permission.PageMonitor,
            PermissionCodes.Pages.DataManage => TextKeys.Permission.PageDataManage,
            PermissionCodes.Pages.UserManage => TextKeys.Permission.PageUserManage,
            PermissionCodes.Pages.ProgramManage => TextKeys.Permission.PageProgramManage,
            PermissionCodes.Pages.LogManage => TextKeys.Permission.PageLogManage,
            PermissionCodes.Pages.StateManage => TextKeys.Permission.PageStateManage,
            PermissionCodes.Pages.SystemSetting => TextKeys.Permission.PageSystemSetting,
            PermissionCodes.Pages.AddressManage => TextKeys.Permission.PageAddressManage,
            PermissionCodes.Buttons.Monitor.ChangeWorkOrder => TextKeys.Permission.ButtonMonitorChangeWorkOrder,
            PermissionCodes.Buttons.Monitor.StartReport => TextKeys.Permission.ButtonMonitorStartReport,
            PermissionCodes.Buttons.Monitor.FinishReport => TextKeys.Permission.ButtonMonitorFinishReport,
            PermissionCodes.Buttons.Auth.SwitchUser => TextKeys.Permission.ButtonAuthSwitchUser,
            PermissionCodes.Buttons.Auth.Logout => TextKeys.Permission.ButtonAuthLogout,
            PermissionCodes.Buttons.Data.Export => TextKeys.Permission.ButtonDataExport,
            PermissionCodes.Buttons.Data.Query => TextKeys.Permission.ButtonDataQuery,
            PermissionCodes.Buttons.Data.Reset => TextKeys.Permission.ButtonDataReset,
            PermissionCodes.Buttons.Data.OpenReport => TextKeys.Permission.ButtonDataOpenReport,
            PermissionCodes.Buttons.Data.OpenReportFolder => TextKeys.Permission.ButtonDataOpenReportFolder,
            PermissionCodes.Buttons.User.Add => TextKeys.Permission.ButtonUserAdd,
            PermissionCodes.Buttons.User.Edit => TextKeys.Permission.ButtonUserEdit,
            PermissionCodes.Buttons.User.Delete => TextKeys.Permission.ButtonUserDelete,
            PermissionCodes.Buttons.User.AssignRole => TextKeys.Permission.ButtonUserAssignRole,
            PermissionCodes.Buttons.User.ResetPassword => TextKeys.Permission.ButtonUserResetPassword,
            PermissionCodes.Buttons.Role.Add => TextKeys.Permission.ButtonRoleAdd,
            PermissionCodes.Buttons.Role.Edit => TextKeys.Permission.ButtonRoleEdit,
            PermissionCodes.Buttons.Role.Delete => TextKeys.Permission.ButtonRoleDelete,
            PermissionCodes.Buttons.Role.AssignPermissions => TextKeys.Permission.ButtonRoleAssignPermissions,
            PermissionCodes.Buttons.Program.Add => TextKeys.Permission.ButtonProgramAdd,
            PermissionCodes.Buttons.Program.Edit => TextKeys.Permission.ButtonProgramEdit,
            PermissionCodes.Buttons.Program.Delete => TextKeys.Permission.ButtonProgramDelete,
            _ => string.Empty
        };
    }
}
