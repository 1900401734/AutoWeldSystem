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

        new(PermissionCodes.Buttons.Monitor.StartReport, "Start Report", PermissionType.Button, PermissionCodes.Pages.Monitor, 120),
        new(PermissionCodes.Buttons.Monitor.FinishReport, "Finish Report", PermissionType.Button, PermissionCodes.Pages.Monitor, 130),
        new(PermissionCodes.Buttons.Monitor.EditWorkOrder, "Edit Work Order", PermissionType.Button, PermissionCodes.Pages.Monitor, 150),
        new(PermissionCodes.Buttons.Monitor.LocalWorkOrder, "Local Work Order", PermissionType.Button, PermissionCodes.Pages.Monitor, 160),
        new(PermissionCodes.Buttons.Auth.SwitchUser, "Switch User", PermissionType.Button, PermissionCodes.Pages.Monitor, 170),
        new(PermissionCodes.Buttons.Auth.Logout, "Logout", PermissionType.Button, PermissionCodes.Pages.Monitor, 180),
        new(PermissionCodes.Buttons.Auth.AddressPreview, "PLC Address Preview", PermissionType.Button, PermissionCodes.Pages.Monitor, 190),

        new(PermissionCodes.Buttons.Data.Export, "Export Data", PermissionType.Button, PermissionCodes.Pages.DataManage, 210),
        new(PermissionCodes.Buttons.Data.Query, "Query History", PermissionType.Button, PermissionCodes.Pages.DataManage, 220),
        new(PermissionCodes.Buttons.Data.Reset, "Reset History Filter", PermissionType.Button, PermissionCodes.Pages.DataManage, 230),
        new(PermissionCodes.Buttons.Data.OpenReport, "Open Report File", PermissionType.Button, PermissionCodes.Pages.DataManage, 240),
        new(PermissionCodes.Buttons.Data.OpenReportFolder, "Open Report Folder", PermissionType.Button, PermissionCodes.Pages.DataManage, 250),

        new(PermissionCodes.Buttons.User.Add, "Add User", PermissionType.Button, PermissionCodes.Pages.UserManage, 310),
        new(PermissionCodes.Buttons.User.Edit, "Edit User", PermissionType.Button, PermissionCodes.Pages.UserManage, 320),
        new(PermissionCodes.Buttons.User.Delete, "Delete User", PermissionType.Button, PermissionCodes.Pages.UserManage, 330),
        new(PermissionCodes.Buttons.User.AssignRole, "Assign Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 340),
        new(PermissionCodes.Buttons.User.ResetPassword, "Reset Password", PermissionType.Button, PermissionCodes.Pages.UserManage, 350),
        new(PermissionCodes.Buttons.Role.Add, "Add Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 360),
        new(PermissionCodes.Buttons.Role.Edit, "Edit Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 370),
        new(PermissionCodes.Buttons.Role.Delete, "Delete Role", PermissionType.Button, PermissionCodes.Pages.UserManage, 380),
        new(PermissionCodes.Buttons.Role.Refresh, "Refresh Roles", PermissionType.Button, PermissionCodes.Pages.UserManage, 390),
        new(PermissionCodes.Buttons.Role.AssignPermissions, "Assign Permissions", PermissionType.Button, PermissionCodes.Pages.UserManage, 400),

        new(PermissionCodes.Buttons.Program.Add, "Add Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 410),
        new(PermissionCodes.Buttons.Program.Edit, "Edit Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 420),
        new(PermissionCodes.Buttons.Program.Delete, "Delete Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 430),
        new(PermissionCodes.Buttons.Program.Sync, "Sync Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 440),
        new(PermissionCodes.Buttons.Program.PullMes, "Pull MES Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 450),
        new(PermissionCodes.Buttons.Program.Refresh, "Refresh Program", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 460),
        new(PermissionCodes.Buttons.Program.BrowseFile, "Browse Program File", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 470),
        new(PermissionCodes.Buttons.Program.BuildName, "Build Program Name", PermissionType.Button, PermissionCodes.Pages.ProgramManage, 480),

        new(PermissionCodes.Buttons.Log.Refresh, "Refresh Logs", PermissionType.Button, PermissionCodes.Pages.LogManage, 510),
        new(PermissionCodes.Buttons.Log.OpenFolder, "Open Log Folder", PermissionType.Button, PermissionCodes.Pages.LogManage, 520),
        new(PermissionCodes.Buttons.Log.OpenSource, "Open Source", PermissionType.Button, PermissionCodes.Pages.LogManage, 530),
        new(PermissionCodes.Buttons.Log.CopyDetails, "Copy Details", PermissionType.Button, PermissionCodes.Pages.LogManage, 540),
        new(PermissionCodes.Buttons.Log.Delete, "Delete Logs", PermissionType.Button, PermissionCodes.Pages.LogManage, 550),

        new(PermissionCodes.Buttons.State.RetrySelected, "Retry Selected Upload", PermissionType.Button, PermissionCodes.Pages.StateManage, 610),
        new(PermissionCodes.Buttons.State.RetryAll, "Retry All Uploads", PermissionType.Button, PermissionCodes.Pages.StateManage, 620),
        new(PermissionCodes.Buttons.State.Refresh, "Refresh Upload State", PermissionType.Button, PermissionCodes.Pages.StateManage, 630),
        new(PermissionCodes.Buttons.State.UploadAll, "One Click Upload", PermissionType.Button, PermissionCodes.Pages.StateManage, 640),
        new(PermissionCodes.Buttons.State.Delete, "Delete Upload State", PermissionType.Button, PermissionCodes.Pages.StateManage, 650),
        new(PermissionCodes.Tabs.State.WorkOrderInfo, "Work Order Information Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 660),
        new(PermissionCodes.Tabs.State.StartReport, "Start Report Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 661),
        new(PermissionCodes.Tabs.State.FinishReport, "Finish Report Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 662),
        new(PermissionCodes.Tabs.State.ProcessParameter, "Process Parameter Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 663),
        new(PermissionCodes.Tabs.State.ReportFile, "Report File Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 664),
        new(PermissionCodes.Tabs.State.WorkOrderStatus, "Work Order Status Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 665),
        new(PermissionCodes.Tabs.State.DeviceStatus, "Device Status Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 666),
        new(PermissionCodes.Tabs.State.ProgramFile, "Program File Tab", PermissionType.Tab, PermissionCodes.Pages.StateManage, 667),

        new(PermissionCodes.Buttons.SystemSetting.Save, "Save Settings", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 710),
        new(PermissionCodes.Buttons.SystemSetting.ConnectPlc, "Test PLC Connection", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 720),
        new(PermissionCodes.Buttons.SystemSetting.SyncDevice, "Sync Device", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 740),
        new(PermissionCodes.Buttons.SystemSetting.TestMes, "Test MES Connection", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 750),
        new(PermissionCodes.Buttons.SystemSetting.ChangePath, "Change Path", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 760),
        new(PermissionCodes.Buttons.SystemSetting.OpenPath, "Open Path", PermissionType.Button, PermissionCodes.Pages.SystemSetting, 770),

        new(PermissionCodes.Buttons.Address.Save, "Save Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 810),
        new(PermissionCodes.Buttons.Address.Refresh, "Refresh Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 820),
        new(PermissionCodes.Buttons.Address.Test, "Test Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 830),
        new(PermissionCodes.Buttons.Address.AddAlarm, "Add Alarm Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 840),
        new(PermissionCodes.Buttons.Address.DeleteAlarm, "Delete Alarm Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 850),
        new(PermissionCodes.Buttons.Address.PasteAlarm, "Paste Alarm Addresses", PermissionType.Button, PermissionCodes.Pages.AddressManage, 860),
        new(PermissionCodes.Buttons.Address.AddProductProcess, "Add Product Process", PermissionType.Button, PermissionCodes.Pages.AddressManage, 870),
        new(PermissionCodes.Buttons.Address.DeleteProductProcess, "Delete Product Process", PermissionType.Button, PermissionCodes.Pages.AddressManage, 880),
        new(PermissionCodes.Buttons.Address.PreviewProductProcessAddress, "Preview Product Process Address", PermissionType.Button, PermissionCodes.Pages.AddressManage, 890),
        new(PermissionCodes.Buttons.Address.AddScheme, "Add Scheme", PermissionType.Button, PermissionCodes.Pages.AddressManage, 900),
        new(PermissionCodes.Buttons.Address.DeleteScheme, "Delete Scheme", PermissionType.Button, PermissionCodes.Pages.AddressManage, 910),
        new(PermissionCodes.Buttons.Address.AddTestItem, "Add Test Item", PermissionType.Button, PermissionCodes.Pages.AddressManage, 920),
        new(PermissionCodes.Buttons.Address.DeleteTestItem, "Delete Test Item", PermissionType.Button, PermissionCodes.Pages.AddressManage, 930),
    };
}
