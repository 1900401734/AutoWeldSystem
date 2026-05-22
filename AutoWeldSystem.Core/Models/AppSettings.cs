using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

[SugarTable("App_Settings", TableDescription = "应用设置表")]
public class AppSettings
{
    [SugarColumn(IsPrimaryKey = true)]
    public int Id { get; set; } = 1;

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "已同步到MES的设备编号", IsNullable = true)]
    public string? MesSyncedDeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "设备名称")]
    public string DeviceName { get; set; } = string.Empty;

    [SugarColumn(Length = 300, ColumnDescription = "设备状态查询地址", IsNullable = true)]
    public string? DeviceStatusUrl { get; set; } = string.Empty;

    [SugarColumn(Length = 200, ColumnDescription = "MES基础URL")]
    public string MesBaseUrl { get; set; } = "http://114.132.45.118:7098/";

    [SugarColumn(Length = 100, ColumnDescription = "PLC IP地址")]
    public string PlcIp { get; set; } = "192.168.1.100";

    [SugarColumn(ColumnDescription = "PLC端口")]
    public int PlcPort { get; set; } = 502;

    [SugarColumn(Length = 50, ColumnDescription = "PLC类型")]
    public string PlcType { get; set; } = AppConstants.PlcTypes.ModbusTcp;

    //[SugarColumn(ColumnDescription = "PLC站号")]
    //public byte PlcStation { get; set; } = 1;

    [SugarColumn(Length = 50, ColumnDescription = "PLC心跳地址", IsNullable = true)]
    public string? PlcHeartbeatAddress { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "PLC心跳间隔秒数")]
    public int PlcHeartbeatIntervalSeconds { get; set; } = 3;

    [SugarColumn(ColumnDescription = "PLC重连间隔秒数")]
    public int PlcReconnectIntervalSeconds { get; set; } = 5;

    [SugarColumn(ColumnDescription = "PLC连接超时毫秒数")]
    public int PlcConnectTimeoutMilliseconds { get; set; } = 3000;

    [SugarColumn(Length = 100, ColumnDescription = "总控IP地址")]
    public string MasterControlIp { get; set; } = "127.0.0.1";

    [SugarColumn(ColumnDescription = "总控端口")]
    public int MasterControlPort { get; set; } = 6000;

    [SugarColumn(Length = 260, ColumnDescription = "数据目录")]
    public string DataDirectory { get; set; } = @"D:\Production Data";

    [SugarColumn(Length = 260, ColumnDescription = "日志目录")]
    public string LogDirectory { get; set; } = @"D:\AutoWeldLogs";

    [SugarColumn(Length = 20, ColumnDescription = "语言")]
    public string Language { get; set; } = AppConstants.Languages.Chinese;

    [SugarColumn(ColumnDescription = "上传模式")]
    public UploadMode UploadMode { get; set; } = UploadMode.Batch;

    [SugarColumn(ColumnDescription = "上传批次大小")]
    public int UploadBatchSize { get; set; } = 10;

    [SugarColumn(ColumnDescription = "是否使用产品编号过滤")]
    public bool UseProductNumberFilter { get; set; } = true;

    [SugarColumn(Length = 30, ColumnDescription = "测试参数绑定方式")]
    public string TestParameterBindingMode { get; set; } = AppConstants.TestParameterBindingModes.ProductNumAndModel;

    [SugarColumn(ColumnDescription = "是否启用双工位双工单模式")]
    public bool EnableDualStationMode { get; set; }

    [SugarColumn(ColumnDescription = "开工前是否校验PLC配方编号")]
    public bool ValidateRecipeBeforeStart { get; set; }

    [SugarColumn(ColumnDescription = "是否启用程序调优")]
    public bool EnableProgramTuning { get; set; } = true;

    [SugarColumn(ColumnDescription = "是否启用程序文件上传")]
    public bool EnableProgramFileUpload { get; set; } = true;

    [SugarColumn(ColumnDescription = "MES超时时间（秒）")]
    public int MesTimeoutSeconds { get; set; } = 10;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
