using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Shared MES device-status rules.
/// Keep MES status codes separate from PLC raw status values so services do not upload PLC-only values by mistake.
/// </summary>
public static class DeviceStatusReportRules
{
    /// <summary>
    /// Returns whether a status code is supported by the MES device-status API.
    /// </summary>
    public static bool IsMesDeviceStatusCode(string? statusCode)
        => ProductionConstants.MesDeviceStatuses.IsSupported(statusCode);

    /// <summary>
    /// Normalizes and validates a MES device-status code before it is written or uploaded.
    /// </summary>
    public static string NormalizeMesDeviceStatusCode(string statusCode)
    {
        var normalized = statusCode.Trim();
        if (IsMesDeviceStatusCode(normalized))
        {
            return normalized;
        }

        throw new InvalidOperationException($"Unsupported MES device status code: {statusCode}");
    }

    /// <summary>
    /// Converts a PLC alarm transition into the MES status that should be reported.
    /// Non-alarm transitions do not create a MES device-status event.
    /// </summary>
    public static string? ResolvePlcAlarmTransition(short? previousStatusCode, short? currentStatusCode)
    {
        var wasAlarm = previousStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm;
        var isAlarm = currentStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm;

        if (!wasAlarm && isAlarm)
        {
            return ProductionConstants.MesDeviceStatuses.Exception;
        }

        if (wasAlarm && currentStatusCode.HasValue && !isAlarm)
        {
            return ProductionConstants.MesDeviceStatuses.Recovered;
        }

        return null;
    }

    /// <summary>
    /// Selects the device id used by the MES report request.
    /// The latest system setting wins; the stored log value is only a fallback for empty settings.
    /// </summary>
    public static string ResolveReportDeviceId(string? currentDeviceId, string? fallbackDeviceId)
    {
        var current = NormalizeText(currentDeviceId);
        return string.IsNullOrWhiteSpace(current)
            ? NormalizeText(fallbackDeviceId)
            : current;
    }

    /// <summary>
    /// Formats the upload-state identity for device status rows, for example "0-开机".
    /// </summary>
    public static string FormatStatusIdentity(string? deviceStatus)
    {
        var code = NormalizeText(deviceStatus);
        return string.IsNullOrWhiteSpace(code)
            ? "-"
            : $"{code}-{GetStatusName(code)}";
    }

    /// <summary>
    /// Formats a station scope for device-status remarks.
    /// Station 0 means the status belongs to both stations.
    /// </summary>
    public static string FormatStationScope(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo
            ? "双工位"
            : $"工位{stationNo}";

    /// <summary>
    /// Appends station information to a remark while keeping blank remarks readable.
    /// </summary>
    public static string AppendStationRemark(string? remark, int stationNo)
    {
        var normalizedRemark = NormalizeText(remark);
        var stationText = FormatStationScope(stationNo);
        return string.IsNullOrWhiteSpace(normalizedRemark)
            ? $"工位：{stationText}"
            : $"{normalizedRemark}；工位：{stationText}";
    }

    /// <summary>
    /// 格式化 MES 异常备注；共享报警只保留原因，不附加工位前缀。
    /// </summary>
    public static string FormatExceptionRemark(string? alarmContent, int stationNo, bool isSharedAlarm)
    {
        var content = NormalizeAlarmContent(alarmContent);

        return isSharedAlarm
            ? $"{content}；"
            : $"工位{stationNo}：{content}；";
    }

    /// <summary>
    /// 生成逐地址异常恢复备注，报警地址仅保留在 JSONL 明细中，不进入 MES Remark。
    /// </summary>
    public static string FormatRecoveryRemark(string? alarmContent, int stationNo, bool isSharedAlarm)
    {
        var content = NormalizeAlarmContent(alarmContent);
        return isSharedAlarm
            ? $"异常恢复：{content}；"
            : $"异常恢复-工位{stationNo}：{content}；";
    }

    /// <summary>
    /// Returns true when a new device-status request should reuse the latest row instead of writing a duplicate.
    /// Software lifecycle events pass forceWrite=true so each startup/shutdown remains auditable.
    /// </summary>
    public static bool ShouldSuppressDuplicateStatus(
        BizDeviceStatusLog? latest,
        string normalizedStatus,
        int? weldTaskId,
        bool forceWrite,
        string? alarmAddress = null)
    {
        if (forceWrite || latest is null)
        {
            return false;
        }

        var isProgramBoundaryStatus = normalizedStatus is ProductionConstants.MesDeviceStatuses.ProgramStarted
            or ProductionConstants.MesDeviceStatuses.ProgramEnded;
        if (isProgramBoundaryStatus && weldTaskId is not null)
        {
            return latest.WeldTaskId == weldTaskId
                && string.Equals(latest.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase);
        }

        if ((normalizedStatus is ProductionConstants.MesDeviceStatuses.Exception
                or ProductionConstants.MesDeviceStatuses.Recovered)
            && !string.IsNullOrWhiteSpace(alarmAddress))
        {
            return string.Equals(latest.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase)
                && string.Equals(latest.AlarmAddress, alarmAddress.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(latest.DeviceStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the operator-facing name for the MES status code stored in the device-status log.
    /// </summary>
    public static string GetStatusName(string? deviceStatus)
    {
        return deviceStatus?.Trim() switch
        {
            ProductionConstants.MesDeviceStatuses.PoweredOn => "开机",
            ProductionConstants.MesDeviceStatuses.Stopped => "停机",
            ProductionConstants.MesDeviceStatuses.Exception => "异常",
            ProductionConstants.MesDeviceStatuses.Recovered => "异常恢复",
            ProductionConstants.MesDeviceStatuses.ProgramStarted => "程序执行开始",
            ProductionConstants.MesDeviceStatuses.ProgramEnded => "程序执行结束",
            _ => "未知"
        };
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static string NormalizeAlarmContent(string? value)
    {
        var content = NormalizeText(value).TrimEnd('；', ';').TrimEnd();
        return string.IsNullOrWhiteSpace(content) ? "设备异常" : content;
    }
}
