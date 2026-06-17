namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// Production flow constants.
/// Keep stable database values here so services, UI, and upload logic do not scatter magic strings.
/// </summary>
public static class ProductionConstants
{
    /// <summary>
    /// Station defaults.
    /// 当前版本的界面仍按单工位流程运行，因此默认写入 1；后续双工位界面再传入实际工位号。
    /// </summary>
    public static class Stations
    {
        public const int SharedStationNo = 0;
        public const int DefaultStationNo = 1;
    }

    /// <summary>
    /// Local upload task types.
    /// </summary>
    public static class UploadTaskTypes
    {
        /// <summary>
        /// 开工上报
        /// </summary>
        public const string StartReport = "StartReport";
        /// <summary>
        /// 完工上报
        /// </summary>
        public const string FinishReport = "FinishReport";
        /// <summary>
        /// 过程参数
        /// </summary>
        public const string ProcessParameter = "ProcessParameter";
        /// <summary>
        /// 报告文件
        /// </summary>
        public const string ReportFile = "ReportFile";
        /// <summary>
        /// 程序文件
        /// </summary>
        public const string ProgramFile = "ProgramFile";
        /// <summary>
        /// 工单状态
        /// </summary>
        public const string WorkOrderStatus = "WorkOrderStatus";
        /// <summary>
        /// 设备状态
        /// </summary>
        public const string DeviceStatus = "DeviceState";
    }

    /// <summary>
    /// PLC work-order status values written by the PC.
    /// Only 1 and 2 are valid. PLC register reset to 0 must be reconciled by the PC.
    /// </summary>
    public static class PlcWorkOrderStatuses
    {
        /// <summary>
        /// 开工允许生产
        /// </summary>
        public const int StartedAllowProduction = 1;
        /// <summary>
        /// 完工禁止生产
        /// </summary>
        public const int FinishedForbidProduction = 2;
    }

    /// <summary>
    /// PLC device mode values written by the PC.
    /// </summary>
    public static class PlcDeviceModes
    {
        public const int SingleOrDualSameWorkOrder = 1;
        public const int DualStationDualWorkOrder = 2;
    }

    /// <summary>
    /// PLC raw device status values read by the PC.
    /// </summary>
    public static class PlcDeviceStatuses
    {
        public const short Running = 1;
        public const short Paused = 2;
        public const short Stopped = 3;
        public const short Alarm = 4;
    }

    /// <summary>
    /// Local upload target systems.
    /// </summary>
    public static class UploadTargets
    {
        public const string Mes = "MES";
        public const string CentralServer = "CentralServer";
    }

    /// <summary>
    /// Common local upload statuses.
    /// </summary>
    public static class UploadStatuses
    {
        public const string Pending = "Pending";
        public const string Uploading = "Uploading";
        public const string Uploaded = "Uploaded";
        public const string Failed = "Failed";
        public const string Retrying = "Retrying";
        public const string Skipped = "Skipped";
    }

    /// <summary>
    /// Report file code values used in filenames.
    /// </summary>
    public static class ReportFileCodes
    {
        public const string Spreadsheet = "BG";
        public const string Curve = "QX";
    }

    /// <summary>
    /// MES file type values used by /api/ExpFile.
    /// </summary>
    public static class MesFileTypes
    {
        /// <summary>
        /// 加工程序文件
        /// </summary>
        public const int ProgramFile = 1;
        /// <summary>
        /// 报告文件（如加工记录表、曲线数据等）
        /// </summary>
        public const int ReportFile = 2;
    }

    /// <summary>
    /// MES device status code values from MES接口.xlsx.
    /// </summary>
    public static class MesDeviceStatuses
    {
        public const string Stopped = "0";          // 停机
        public const string PoweredOn = "1";        // 开机
        public const string Exception = "4";        // 异常
        public const string Recovered = "5";        // 异常恢复
        public const string ProgramStarted = "6";   // 程序执行开始
        public const string ProgramEnded = "7";     // 程序执行结束
    }

    public static class MesWorkOrderStatuses
    {
        public const string StartedOrRestarted = "0";
        public const string Completed = "1";
        public const string Paused = "2";
    }

    /// <summary>
    /// Station test result values. PLC raw value 3 means OK; all other values are NG.
    /// </summary>
    public static class TestResults
    {
        public const string OkRawValue = "3";
        public const string Ok = "OK";
        public const string Ng = "NG";
        public const string Unknown = "Unknown";
    }

    /// <summary>
    /// Local product instance statuses under one weld task.
    /// </summary>
    public static class ProductInstanceStatuses
    {
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Abandoned = "Abandoned";
    }
}
