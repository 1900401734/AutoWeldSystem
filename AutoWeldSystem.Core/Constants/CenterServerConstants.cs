namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// Constants shared by center-server telemetry clients and the center dashboard.
/// </summary>
public static class CenterServerConstants
{
    /// <summary>
    /// Default device-to-center heartbeat interval in seconds.
    /// </summary>
    public const int DefaultHeartbeatIntervalSeconds = 5;

    /// <summary>
    /// Default timeout used by the center server to mark a client offline.
    /// </summary>
    public const int DefaultOfflineTimeoutSeconds = 15;

    /// <summary>
    /// Default local URL used when the center server address is not configured.
    /// </summary>
    public const string DefaultBaseUrl = "http://127.0.0.1:7099/";

    /// <summary>
    /// Logical system types used for dashboard grouping.
    /// </summary>
    public static class SystemTypes
    {
        public const string Electromagnetic = "Electromagnetic";
        public const string WholePiece = "WholePiece";
        public const string Other = "Other";
    }

}
