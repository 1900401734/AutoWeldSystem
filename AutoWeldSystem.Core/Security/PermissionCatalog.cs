using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.Security;

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } = new List<PermissionDefinition>
    {
        new(PermissionCodes.Pages.Monitor, "Monitor", PermissionType.Page, Sort: 10),
        new(PermissionCodes.Pages.DataManage, "Data Management", PermissionType.Page, Sort: 20),
        new(PermissionCodes.Pages.UserManage, "User Management", PermissionType.Page, Sort: 30),
        new(PermissionCodes.Pages.ProgramManage, "Program Management", PermissionType.Page, Sort: 40),
        new(PermissionCodes.Pages.LogManage, "Log Management", PermissionType.Page, Sort: 50),
        new(PermissionCodes.Pages.StateManage, "Upload State", PermissionType.Page, Sort: 60),
        new(PermissionCodes.Pages.SystemSetting, "System Setting", PermissionType.Page, Sort: 70),
        new(PermissionCodes.Pages.AddressManage, "Address Management", PermissionType.Page, Sort: 80),

        new(PermissionCodes.Buttons.Monitor.ChangeWorkOrder, "Change Work Order", PermissionType.Button, PermissionCodes.Pages.Monitor, 110),
        new(PermissionCodes.Buttons.Monitor.StartReport, "Start Report", PermissionType.Button, PermissionCodes.Pages.Monitor, 120),
        new(PermissionCodes.Buttons.Monitor.FinishReport, "Finish Report", PermissionType.Button, PermissionCodes.Pages.Monitor, 130),
        new(PermissionCodes.Buttons.Auth.SwitchUser, "Switch User", PermissionType.Button, PermissionCodes.Pages.Monitor, 140),
        new(PermissionCodes.Buttons.Auth.Logout, "Logout", PermissionType.Button, PermissionCodes.Pages.Monitor, 150),

        new(PermissionCodes.Buttons.Data.Export, "Export Data", PermissionType.Button, PermissionCodes.Pages.DataManage, 210),

        new(PermissionCodes.Buttons.User.Add, "Add User", PermissionType.Button, PermissionCodes.Pages.UserManage, 310),
        new(PermissionCodes.Buttons.User.Edit, "Edit User", PermissionType.Button, PermissionCodes.Pages.UserManage, 320),
        new(PermissionCodes.Buttons.User.Delete, "Delete User", PermissionType.Button, PermissionCodes.Pages.UserManage, 330),
        new(PermissionCodes.Buttons.User.AssignRole, "Assign Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 340),
        new(PermissionCodes.Buttons.User.ResetPassword, "Reset Password", PermissionType.Button, PermissionCodes.Pages.UserManage, 350),
        new(PermissionCodes.Buttons.Role.Add, "Add Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 360),
        new(PermissionCodes.Buttons.Role.Edit, "Edit Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 370),
        new(PermissionCodes.Buttons.Role.Delete, "Delete Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 380),
        new(PermissionCodes.Buttons.Role.AssignPermissions, "Assign Permissions", PermissionType.Button, PermissionCodes.Pages.UserManage, 390),

        new(PermissionCodes.Buttons.Program.Add, "Add Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 410),
        new(PermissionCodes.Buttons.Program.Edit, "Edit Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 420),
        new(PermissionCodes.Buttons.Program.Delete, "Delete Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 430),
    };
}
