namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// Production flow constants.
/// Keep stable database values here so services, UI, and upload logic do not scatter magic strings.
/// </summary>
public static class ProductionConstants
{
    /// <summary>
    /// Product number source modes.
    /// </summary>
    public static class ProductNoSources
    {
        public const string AutoIncrement = "AutoIncrement";
        public const string Plc = "PLC";
        public const string Manual = "Manual";
    }

    /// <summary>
    /// PLC address categories used by address management tabs and collection services.
    /// </summary>
    public static class PlcAddressCategories
    {
        public const string BusinessSignal = "BusinessSignal";
        public const string CollectionParameter = "CollectionParameter";
        public const string DeviceStatus = "DeviceStatus";
        public const string StationResult = "StationResult";
        public const string CentralServer = "CentralServer";
        public const string RemoteControl = "RemoteControl";
    }

    /// <summary>
    /// Local upload task types.
    /// </summary>
    public static class UploadTaskTypes
    {
        public const string ProcessParameter = "ProcessParameter";
        public const string ReportFile = "ReportFile";
        public const string ProgramFile = "ProgramFile";
        public const string DeviceStatus = "DeviceStatus";
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
        public const int ProgramFile = 1;
        public const int ReportFile = 2;
    }

    /// <summary>
    /// MES device status code values from MES接口.xlsx.
    /// </summary>
    public static class MesDeviceStatuses
    {
        public const string Stopped = "0";
        public const string PoweredOn = "1";
        public const string Exception = "4";
        public const string Recovered = "5";
        public const string ProgramStarted = "6";
        public const string ProgramEnded = "7";
    }

    /// <summary>
    /// MES work order status code values from MES接口.xlsx.
    /// </summary>
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
