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

    /// <summary>
    /// XLSX 报表输出的小数位。为空表示沿用采集时按偏移量表达式格式化的位数。
    /// 采集时已按表达式小数位存库，这里只能减位或补零，恢复不了被截掉的精度。
    /// </summary>
    [SugarColumn(ColumnDescription = "报表输出小数位", IsNullable = true)]
    public int? ReportDecimalPlaces { get; set; }

    /// <summary>
    /// MES 过程参数上传的小数位。为空表示沿用采集时按偏移量表达式格式化的位数。
    /// 截断还是四舍五入由 <see cref="PlcStringNumericFormatMode"/> 决定，不单独配置。
    /// </summary>
    [SugarColumn(ColumnDescription = "过程参数上传小数位", IsNullable = true)]
    public int? ProcessParameterDecimalPlaces { get; set; }

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

    [SugarColumn(Length = 200, ColumnDescription = "本地设备状态查询接口路由")]
    public string DeviceStatusQueryRoute { get; set; } = MesEndpointRouteRules.DeviceStatusQueryDefaultRoute;

    [SugarColumn(Length = 200, ColumnDescription = "本地设备编号设置接口路由")]
    public string DeviceIdSetRoute { get; set; } = MesEndpointRouteRules.DeviceIdSetDefaultRoute;

    [SugarColumn(ColumnDescription = "是否启用PostData自定义Header", IsNullable = true)]
    public bool? EnablePostDataCustomHeader { get; set; } = false;

    [SugarColumn(Length = 100, ColumnDescription = "PostData自定义Header Key", IsNullable = true)]
    public string? PostDataHeaderKey { get; set; } = string.Empty;

    [SugarColumn(Length = 300, ColumnDescription = "PostData自定义Header Value", IsNullable = true)]
    public string? PostDataHeaderValue { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "过程参数设备类型")]
    public string ProcessParameterDeviceType { get; set; } = ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;

    [SugarColumn(Length = 20, ColumnDescription = "检测结果来源", IsNullable = true)]
    public string? InspectionResultSource { get; set; } = ProductionConstants.InspectionResultSources.Plc;

    [SugarColumn(Length = 20, ColumnDescription = "实时焊点编号来源", IsNullable = true)]
    public string? RealtimePointNumberSource { get; set; } = ProductionConstants.RealtimePointNumberSources.Plc;

    /// <summary>
    /// 整件检测四面数据在监控界面合并成一行显示，使界面与上传、报表口径一致。仅影响界面。
    /// 默认开启：合并视图与 MES 过程参数、XLSX 报表同源，现场核对时不必再做换算。
    /// </summary>
    [SugarColumn(ColumnDescription = "整件检测合并显示", IsNullable = true)]
    public bool? EnableWholePieceMergedDisplay { get; set; } = true;

    /// <summary>
    /// 整件检测逐面模式是否显示“面结果”列。关闭后只隐藏该列，面号和逐面实测值保留。
    /// 默认关闭：单面结果与合并后的产品结果口径不同，同屏显示容易被误读成互相矛盾。仅影响界面。
    /// </summary>
    [SugarColumn(ColumnDescription = "整件检测逐面结果显示", IsNullable = true)]
    public bool? EnableWholePieceFaceResultDisplay { get; set; } = false;

    /// <summary>
    /// 合并显示的生效口径。默认开启，未配置（null）按开启处理。
    /// 默认值只有这一处，避免各调用点各自写死空值兜底后与实体默认值相反。
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsWholePieceMergedDisplayEnabled => EnableWholePieceMergedDisplay != false;

    /// <summary>
    /// “面结果”列的生效口径。默认关闭，未配置（null）按关闭处理。
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsWholePieceFaceResultDisplayEnabled => EnableWholePieceFaceResultDisplay == true;

    /// <summary>
    /// 整件检测 A/B 配对聚合方式。高度取四面最大值、宽度只取 A 面，本设置只作用于其余测试项。
    /// 默认取最大值：单面视觉检测失败会回传 0，取平均会把 0 拉进结果反而更容易判 OK。
    /// </summary>
    [SugarColumn(Length = 20, ColumnDescription = "A/B配对聚合方式", IsNullable = true)]
    public string? PairedAggregationMode { get; set; } = ProductionConstants.PairedAggregationModes.Maximum;

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

    [SugarColumn(ColumnDescription = "PLC心跳超时时间（秒）")]
    public int PlcHeartbeatTimeoutSeconds { get; set; } = 3;

    [SugarColumn(ColumnDescription = "PLC通讯超时（毫秒）")]
    public int PlcCommunicationTimeoutMilliseconds { get; set; } = 3000;

    #endregion

    [SugarColumn(Length = 50, ColumnDescription = "已同步到MES的设备编号", IsNullable = true)]
    public string? MesSyncedDeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 20, ColumnDescription = "语言")]
    public string Language { get; set; } = AppConstants.Languages.Chinese;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

    public AppSettings Clone() => (AppSettings)this.MemberwiseClone();
}
