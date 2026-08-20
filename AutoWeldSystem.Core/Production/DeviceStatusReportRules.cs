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
    /// 生成统一的设备状态 Remark。
    /// 报警地址的 PLC 工位号不参与 Remark；程序边界才使用应用配置中的程序工位映射。
    /// </summary>
    public static string FormatRemark(
        string? deviceStatus,
        int stationNo,
        bool dualStationEnabled,
        string? station1Name,
        string? station2Name,
        string? alarmContent = null,
        string? fallbackRemark = null)
    {
        var status = NormalizeText(deviceStatus);
        if (status is ProductionConstants.MesDeviceStatuses.Exception
            or ProductionConstants.MesDeviceStatuses.Recovered)
        {
            if (string.IsNullOrWhiteSpace(alarmContent)
                && !string.IsNullOrWhiteSpace(fallbackRemark))
            {
                // 旧 JSONL 可能只有“工位/报警地址”拼接后的 Remark，补传时统一提取报警内容。
                alarmContent = ExtractLegacyAlarmContent(fallbackRemark);
            }

            var content = NormalizeAlarmContent(alarmContent);
            return status == ProductionConstants.MesDeviceStatuses.Recovered
                ? $"异常恢复：{content}"
                : $"异常：{content}";
        }

        var statusName = GetStatusName(status);
        if (status is ProductionConstants.MesDeviceStatuses.PoweredOn
            or ProductionConstants.MesDeviceStatuses.Stopped)
        {
            return statusName;
        }

        if (status is ProductionConstants.MesDeviceStatuses.ProgramStarted
            or ProductionConstants.MesDeviceStatuses.ProgramEnded)
        {
            if (!dualStationEnabled || stationNo <= ProductionConstants.Stations.SharedStationNo)
            {
                return statusName;
            }

            var names = StationDisplayNameRules.NormalizeForLoad(true, station1Name, station2Name);
            var name = stationNo == 2 ? names.Station2 : names.Station1;
            return $"{NormalizeStationLabel(name)}：{statusName}";
        }

        return string.IsNullOrWhiteSpace(fallbackRemark) ? statusName : fallbackRemark.Trim();
    }

    private static string NormalizeStationLabel(string name)
        => name.EndsWith("工位", StringComparison.Ordinal) ? name : $"{name}工位";

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

    private static string ExtractLegacyAlarmContent(string value)
    {
        var text = NormalizeText(value);
        var suffixIndex = text.IndexOf("；报警地址：", StringComparison.Ordinal);
        if (suffixIndex < 0)
        {
            suffixIndex = text.IndexOf(";报警地址:", StringComparison.Ordinal);
        }

        if (suffixIndex >= 0)
        {
            text = text[..suffixIndex];
        }

        var stationSuffixIndex = text.IndexOf("；工位：", StringComparison.Ordinal);
        if (stationSuffixIndex < 0)
        {
            stationSuffixIndex = text.IndexOf(";工位:", StringComparison.Ordinal);
        }

        if (stationSuffixIndex >= 0)
        {
            text = text[..stationSuffixIndex];
        }

        var separatorIndex = text.IndexOf('：', StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            var prefix = text[..separatorIndex];
            if (prefix.Contains("工位", StringComparison.Ordinal)
                || string.Equals(prefix, "异常", StringComparison.Ordinal))
            {
                text = text[(separatorIndex + 1)..];
            }
        }

        return text.Trim().TrimEnd('；', ';').Trim();
    }

    private static string NormalizeAlarmContent(string? value)
    {
        var content = NormalizeText(value).TrimEnd('；', ';').TrimEnd();
        return string.IsNullOrWhiteSpace(content) ? "设备异常" : content;
    }
}
