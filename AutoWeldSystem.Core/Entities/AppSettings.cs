using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.Production;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

[SugarTable(tableName: "App_Settings", tableDescription: "应用设置表")]
public class AppSettings
{
    [SugarColumn(IsPrimaryKey = true)]
    public int Id { get; set; } = 1;

    #region 设备管理

    [SugarColumn(Length = 100, ColumnDescription = "设备名称")]
    public string DeviceName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 300, ColumnDescription = "设备状态查询地址")]
    public string DeviceBaseUrl { get; set; } = "http://127.0.0.1:7098/";

    [SugarColumn(Length = 200, ColumnDescription = "MES基础URL")]
    public string MesBaseUrl { get; set; } = "http://114.132.45.118:7098/";

    #endregion

    #region PLC配置

    [SugarColumn(Length = 100, ColumnDescription = "PLC地址")]
    public string PlcIp { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "PLC端口")]
    public int PlcPort { get; set; } = 102;

    [SugarColumn(Length = 50, ColumnDescription = "PLC类型")]
    public string PlcType { get; set; } = AppConstants.PlcTypes.SiemensS71200;

    [SugarColumn(ColumnDescription = "是否启用PLC字符串数值处理", IsNullable = true)]
    public bool? EnablePlcStringNumericFormatting { get; set; } = true;

    [SugarColumn(Length = 20, ColumnDescription = "PLC字符串数值处理方式")]
    public string PlcStringNumericFormatMode { get; set; } = AppConstants.PlcStringNumericFormatModes.Truncate;

    [SugarColumn(ColumnDescription = "是否启用PLC报警读取", IsNullable = true)]
    public bool? EnablePlcAlarmReading { get; set; } = true;

    [SugarColumn(Length = 30, ColumnDescription = "PLC报警触发模式", IsNullable = true)]
    public string? PlcAlarmTriggerMode { get; set; } = AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress;

    #endregion

    #region 中心服务器配置

    [SugarColumn(ColumnDescription = "是否启用中心服务器同步")]
    public bool EnableCenterServerSync { get; set; }

    [SugarColumn(Length = 300, ColumnDescription = "中心服务器地址")]
    public string CenterServerBaseUrl { get; set; } = CenterServerConstants.DefaultBaseUrl;

    [SugarColumn(Length = 80, ColumnDescription = "中心服务器系统类型")]
    public string CenterServerSystemType { get; set; } = CenterServerConstants.SystemTypes.Other;

    [SugarColumn(ColumnDescription = "中心服务器心跳间隔秒")]
    public int CenterServerHeartbeatIntervalSeconds { get; set; } = CenterServerConstants.DefaultHeartbeatIntervalSeconds;

    #endregion

    #region 系统参数配置

    [SugarColumn(Length = 260, ColumnDescription = "数据目录")]
    public string DataDirectory { get; set; } = @"D:\AutoWeldData";

    [SugarColumn(Length = 260, ColumnDescription = "日志目录")]
    public string LogDirectory { get; set; } = @"D:\AutoWeldLogs";

    [SugarColumn(ColumnDescription = "是否启用开机自启", IsNullable = true)]
    public bool? EnableAutoStart { get; set; } = true;

    /// <summary>
    /// 是否优先使用 Windows 计划任务的最高权限开机自启。
    /// 启用后，程序开机启动时更容易拥有修改系统时间的权限。
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用最高权限开机自启", IsNullable = true)]
    public bool? EnableElevatedAutoStart { get; set; } = true;

    #endregion

    #region MES配置

    [SugarColumn(ColumnDescription = "是否使用产品编号过滤")]
    public bool UseProductNumberFilter { get; set; }

    /// <summary>
    /// Whether the operator employee number is captured via a modal dialog during start report.
    /// When disabled, the employee number is entered directly in the MonitorView MesUserNumber control.
    /// 默认启用，保持历史由弹窗收集员工号的行为。
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用操作员弹窗输入", IsNullable = true)]
    public bool? UseOperatorInputDialog { get; set; } = true;

    /// <summary>
    /// Whether MES device-status uploads are enabled. Local device-status logs are still written when disabled.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用设备状态上报", IsNullable = true)]
    public bool? EnableDeviceStatusReport { get; set; } = true;

    /// <summary>
    /// Whether MES work-order status uploads are enabled. Start/end reports are not controlled by this switch.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用工单状态上报", IsNullable = true)]
    public bool? EnableWorkOrderStatusReport { get; set; } = true;

    [SugarColumn(ColumnDescription = "MES超时时间（秒）")]
    public int MesTimeoutSeconds { get; set; } = 10;

    [SugarColumn(ColumnDescription = "MES心跳检测间隔（秒）")]
    public int MesHeartbeatIntervalSeconds { get; set; } = MesConnectionRules.DefaultHeartbeatIntervalSeconds;

    [SugarColumn(Length = 200, ColumnDescription = "MES员工信息接口路由")]
    public string MesUserRoute { get; set; } = MesEndpointRouteRules.UserDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES工单信息接口路由")]
    public string MesWorkOrderRoute { get; set; } = MesEndpointRouteRules.WorkOrderDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES服务器时间接口路由")]
    public string MesServerTimeRoute { get; set; } = MesEndpointRouteRules.ServerTimeDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES在线检测接口路由")]
    public string MesSysRoute { get; set; } = MesEndpointRouteRules.SysDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES程序管理接口路由")]
    public string MesProgramManageRoute { get; set; } = MesEndpointRouteRules.ProgramManageDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES开工上报接口路由")]
    public string MesStartWorkRoute { get; set; } = MesEndpointRouteRules.StartWorkDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES工单状态接口路由")]
    public string MesWorkStatusRoute { get; set; } = MesEndpointRouteRules.WorkStatusDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES完工上报接口路由")]
    public string MesEndWorkRoute { get; set; } = MesEndpointRouteRules.EndWorkDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES报告文件接口路由")]
    public string MesReportFileRoute { get; set; } = MesEndpointRouteRules.ReportFileDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES过程参数接口路由")]
    public string MesPostDataRoute { get; set; } = MesEndpointRouteRules.PostDataDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES设备编号接口路由")]
    public string MesDeviceRoute { get; set; } = MesEndpointRouteRules.DeviceDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "MES设备状态接口路由")]
    public string MesDeviceStatusRoute { get; set; } = MesEndpointRouteRules.DeviceStatusDefaultRoute;

    [SugarColumn(ColumnDescription = "是否启用PostData自定义Header", IsNullable = true)]
    public bool? EnablePostDataCustomHeader { get; set; } = false;

    [SugarColumn(Length = 100, ColumnDescription = "PostData自定义Header Key", IsNullable = true)]
    public string? PostDataHeaderKey { get; set; } = string.Empty;

    [SugarColumn(Length = 300, ColumnDescription = "PostData自定义Header Value", IsNullable = true)]
    public string? PostDataHeaderValue { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "过程参数设备类型")]
    public string ProcessParameterDeviceType { get; set; } = ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;

    [SugarColumn(ColumnDescription = "过程参数接口编码")]
    public ApiCode ProcessParameterApiCode { get; set; } = ApiCode.EMWeldDetail_001;

    [SugarColumn(Length = 100, ColumnDescription = "过程参数接口名称")]
    public string ProcessParameterApiName { get; set; } = "EMWeldDetail";

    /// <summary>
    /// Whether product history shows the test-weld flag and process-parameter uploads include IsTest for weld devices.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "产品历史显示试焊件")]
    public bool? ShowTestFlagInHistory { get; set; } = true;

    #endregion

    #region 生产配置

    [SugarColumn(ColumnDescription = "是否启用双工位")]
    public bool EnableDualStation { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "工位1显示名称")]
    public string Station1DisplayName { get; set; } = "左";

    [SugarColumn(Length = 50, ColumnDescription = "工位2显示名称")]
    public string Station2DisplayName { get; set; } = "右";

    [SugarColumn(ColumnDescription = "是否启用双工单")]
    public bool EnableDualWorkOrder { get; set; }

    [SugarColumn(ColumnDescription = "开工后是否校验PLC配方编号")]
    public bool ValidateRecipeAfterStart { get; set; }

    [SugarColumn(ColumnDescription = "完工上报时是否启用实际数量输入弹窗")]
    public bool EnableFinishExpQtyPrompt { get; set; }

    [SugarColumn(ColumnDescription = "上传模式")]
    public UploadMode UploadMode { get; set; } = UploadMode.Quantity;

    [SugarColumn(ColumnDescription = "上传批次大小")]
    public int UploadBatchSize { get; set; } = 1;

    [SugarColumn(ColumnDescription = "PLC心跳监测频率（毫秒）")]
    public int PlcHeartbeatReadIntervalMilliseconds { get; set; } = 300;

    #endregion

    [SugarColumn(Length = 50, ColumnDescription = "已同步到MES的设备编号", IsNullable = true)]
    public string? MesSyncedDeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 20, ColumnDescription = "语言")]
    public string Language { get; set; } = AppConstants.Languages.Chinese;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

    public AppSettings Clone() => (AppSettings)this.MemberwiseClone();
}
