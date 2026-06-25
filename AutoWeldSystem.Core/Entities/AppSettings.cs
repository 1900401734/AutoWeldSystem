using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
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

    #endregion

    #region MES配置

    [SugarColumn(ColumnDescription = "是否使用产品编号过滤")]
    public bool UseProductNumberFilter { get; set; }

    [SugarColumn(ColumnDescription = "MES超时时间（秒）")]
    public int MesTimeoutSeconds { get; set; } = 10;

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
