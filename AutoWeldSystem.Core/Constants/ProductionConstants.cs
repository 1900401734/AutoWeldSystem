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

        /// <summary>
        /// 服务器上报
        /// </summary>
        public const string CenterProductReport = "CenterProductReport";
    }

    /// <summary>
    /// 工单状态，由PC写入PLC
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
        public const short Unknown = 0;
        public const short Running = 1;
        public const short Paused = 2;
        public const short Stopped = 3;
        public const short Alarm = 4;

        /// <summary>
        /// PLC 状态码的字符串形式。遥测协议以字符串传输状态码，
        /// 中心服务器看板与上报服务共用这一组字面量，避免各处硬编码 "1"~"4"。
        /// </summary>
        public static class Text
        {
            public const string Unknown = "0";
            public const string Running = "1";
            public const string Paused = "2";
            public const string Stopped = "3";
            public const string Alarm = "4";
        }

        /// <summary>
        /// Determines whether the PLC status can be recorded and uploaded to MES.
        /// </summary>
        public static bool IsReportable(short statusCode)
        {
            return statusCode is Running or Paused or Stopped or Alarm;
        }

        /// <summary>
        /// Determines whether the stored status text is a PLC status that can be uploaded.
        /// </summary>
        public static bool IsReportable(string? statusCode)
        {
            return statusCode?.Trim() is Text.Running or Text.Paused or Text.Stopped or Text.Alarm;
        }
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
        public const string PoweredOn = "1";        // Software started
        public const string Stopped = "0";          // Software stopped
        public const string Exception = "4";        // PLC alarm
        public const string Recovered = "5";        // PLC alarm recovered
        public const string ProgramStarted = "6";   // Program execution started
        public const string ProgramEnded = "7";     // Program execution ended

        /// <summary>
        /// Determines whether the value is a MES device-status code supported by the upload API.
        /// </summary>
        public static bool IsSupported(string? statusCode)
        {
            return statusCode?.Trim() is PoweredOn
                or Stopped
                or Exception
                or Recovered
                or ProgramStarted
                or ProgramEnded;
        }
    }

    public static class MesWorkOrderStatuses
    {
        public const string StartedOrRestarted = "0";
        public const string Completed = "1";
        public const string Paused = "2";
    }

    /// <summary>
    /// 过程参数上传设备类型。
    /// </summary>
    public static class ProcessParameterDeviceTypes
    {
        public const string Electromagnetic = "Electromagnetic";
        public const string WholePieceCheck = "WholePieceCheck";
        public const string WholePieceWeld = "WholePieceWeld";
    }

    /// <summary>
    /// 整件检测结果来源。
    /// </summary>
    public static class InspectionResultSources
    {
        public const string Plc = "Plc";
        public const string Program = "Program";

        public static string Normalize(string? value)
            => string.Equals(value?.Trim(), Program, StringComparison.OrdinalIgnoreCase)
                ? Program
                : Plc;
    }

    /// <summary>
    /// 实时焊点/检测面编号来源。
    /// </summary>
    public static class RealtimePointNumberSources
    {
        public const string Plc = "Plc";
        public const string Program = "Program";

        public static string Normalize(string? value)
            => string.Equals(value?.Trim(), Program, StringComparison.OrdinalIgnoreCase)
                ? Program
                : Plc;
    }

    /// <summary>
    /// Station test result values read from PLC and shown in local production data.
    /// </summary>
    public static class TestResults
    {
        public const string NoResultRawValue = "0";
        public const string NgRawValue = "2";
        public const string OkRawValue = "3";
        public const string PreWeldNgRawValue = "4";
        public const string Ok = "OK";
        public const string Ng = "NG";
        public const string PreWeldNg = "焊前NG";
        public const string Unknown = "Unknown";
        public const string NotAvailable = "--";
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
