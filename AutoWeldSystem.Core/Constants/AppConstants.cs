
namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// 系统全局静态常量字典
/// </summary>
public static class AppConstants
{
    public const string ApplicationName = "AutoWeldSystem";

    public static class Defaults
    {
        public const string InitialPassword = "123456";
    }

    /// <summary>
    /// 用户角色常量
    /// </summary>
    public static class Roles
    {
        public const string Developer = "Developer";
        public const string Admin = "Admin";
        public const string Operator = "Operator";
        public const string Readonly = "Readonly";
    }

    /// <summary>
    /// 国际化(多语言)语言代码常量
    /// </summary>
    public static class Languages
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";
    }

    /// <summary>
    /// PLC 类型常量。
    /// 统一收口后，界面层和持久化层都不需要各自手写字符串。
    /// </summary>
    public static class PlcTypes
    {
        public const string ModbusTcp = "ModbusTcp";
        public const string TcpSocket = "TcpSocket";
        public const string SiemensS71200 = "SiemensS7-1200";
    }

    /// <summary>
    /// PLC Logical Key
    /// </summary>
    public static class PlcLogicalKeys
    {
        public const string DeviceStatus = "device_status";
        public const string WorkId = "work_id";

        public const string PcHeartBeat = "pc_heartbeat";
        public const string PlcHeartBeat = "plc_heartbeat";

        public const string PcRecipeCode = "pc_recipe_code";
        public const string PlcRecipeCode = "plc_recipe_code";

        public const string ProductDataReady = "product_data_ready";
        public const string ProductCollectionFeedback = "product_collection_feedback";

        public const string WorkOrderStatus = "work_order_status";
        public const string DeviceMode = "device_mode";

        public const string TotalProduction = "total_production";
        public const string AcceptedQuantity = "accepted_quantity";
        public const string RejectedQuantity = "rejected_quantity";
    }

    /// <summary>
    /// PLC 地址数据类型常量。
    /// 这里保留为字符串，是为了方便表格下拉框和数据库保存。
    /// </summary>
    public static class PlcDataTypes
    {
        public const string Bool = "Bool";
        public const string Int16 = "Int16";
        public const string Int32 = "Int32";
        public const string Float = "Float";
        public const string String = "String";

        public static readonly string[] All = { Bool, Int16, Int32, Float, String };
    }

    /// <summary>
    /// MES固定状态码常量
    /// </summary>
    public static class MesStatus
    {
        public const string Success = "S";
        public const string Error = "E";
    }

    /// <summary>
    /// 本地日志分类目录名。
    /// 后续增加异常日志、生产日志、总控日志时，也统一从这里扩展。
    /// </summary>
    public static class LogCategories
    {
        public const string Mes = "MES";
        public const string ProductionFlow = "ProductionFlow";
        public const string MasterControl = "MasterControl";
        public const string ProgramException = "ProgramException";
    }

    /// <summary>
    /// 异常日志分类。业务异常代表可预见的外部条件失败，程序异常代表代码运行时错误。
    /// </summary>
    public static class ExceptionLogCategories
    {
        public const string Business = "Business";
        public const string Program = "Program";
    }

    /// <summary>
    /// MES 交互原因常量。
    /// 日志中保存业务原因，便于现场排查时快速定位是哪一步接口交互。
    /// </summary>
    public static class MesLogPurposes
    {
        public const string GetUserInfo = "获取员工信息";
        public const string GetWorkOrderInfo = "获取MES工单信息";
        public const string GetServerTime = "服务器校时";
        public const string TestConnection = "MES连通性测试";
        public const string SetDeviceId = "设置设备编号";
        public const string AddProgram = "新增程序";
        public const string UpdateProgram = "更新程序";
        public const string GetProgramList = "获取程序列表";
        public const string DownloadProgram = "下载程序";
        public const string DeleteProgram = "删除程序";
        public const string ReportDeviceStatus = "设备状态上报";
        public const string StartWork = "开工上报";
        public const string ChangeWorkStatus = "工单状态变更";
        public const string EndWork = "完工上报";
        public const string UploadReportFile = "报告文件上报";
        public const string UploadProcessParameters = "采集参数上传";
    }

    /// <summary>
    /// 程序同步状态
    /// </summary>
    public static class ProgramSyncStatus
    {
        public const string PendingCreate = "PendingCreate";
        public const string PendingUpdate = "PendingUpdate";
        public const string PendingDelete = "PendingDelete";
        public const string Synced = "Synced";
        public const string Failed = "Failed";
        public const string Deleted = "Deleted";
    }

    /// <summary>
    /// 程序待同步动作。
    /// Failed 状态下仍依靠该字段知道下一次重试应执行新增、更新还是删除。
    /// </summary>
    public static class ProgramSyncActions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }

    /// <summary>
    /// MES 程序备注动作。
    /// 客户接口中的 Remark 字段用于区分程序新增、修改和删除。
    /// </summary>
    public static class ProgramRemarkActions
    {
        public const string Create = "新增";
        public const string Update = "修改";
        public const string Delete = "删除";
    }
}
