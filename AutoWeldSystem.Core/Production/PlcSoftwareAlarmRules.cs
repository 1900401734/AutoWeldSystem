using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 统一判定本机软件报警，避免 PLC 原始设备状态与独立 Bool 报警信号相互覆盖。
/// </summary>
public static class PlcSoftwareAlarmRules
{
    /// <summary>
    /// PLC 已进入报警状态但没有可用报警原因时显示的通用提示。
    /// </summary>
    public const string GenericAlarmMessage = "PLC设备报警，未匹配到已启用的报警原因";

    /// <summary>
    /// 合并 PLC 原始设备状态与独立 Bool 报警信号。
    /// </summary>
    public static PlcSoftwareAlarmState Resolve(
        short? deviceStatusCode,
        bool hasActiveBoolSignal,
        IEnumerable<string?> activeBoolMessages)
    {
        var isActive = deviceStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm
            || hasActiveBoolSignal;
        if (!isActive)
        {
            return PlcSoftwareAlarmState.Inactive;
        }

        var messages = activeBoolMessages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var message = messages.Count > 0
            ? string.Join("；", messages)
            : GenericAlarmMessage;
        return new PlcSoftwareAlarmState(true, message);
    }

    /// <summary>
    /// 合并生产地址与报警地址中出现的工位，确保仅配置报警地址的工位也参与轮询。
    /// </summary>
    public static IReadOnlyList<int> ResolveStationNumbers(
        IEnumerable<int> productionStationNumbers,
        IEnumerable<BizPlcAlarmAddress> alarmAddresses)
    {
        var stationNumbers = productionStationNumbers
            .Where(stationNo => stationNo > ProductionConstants.Stations.SharedStationNo)
            .Concat(alarmAddresses
                .Where(alarm => alarm.Enabled && !string.IsNullOrWhiteSpace(alarm.Address))
                .Select(alarm => alarm.StationNo)
                .Where(stationNo => stationNo > ProductionConstants.Stations.SharedStationNo))
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();

        return stationNumbers.Count > 0
            ? stationNumbers
            : [ProductionConstants.Stations.DefaultStationNo];
    }

    /// <summary>
    /// 获取当前工位应读取的有效报警地址，包含共享报警地址。
    /// </summary>
    public static IReadOnlyList<BizPlcAlarmAddress> ResolveAlarmAddressesForStation(
        IEnumerable<BizPlcAlarmAddress> alarmAddresses,
        int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        return alarmAddresses
            .Where(alarm => alarm.Enabled && !string.IsNullOrWhiteSpace(alarm.Address))
            .Where(alarm => alarm.StationNo == ProductionConstants.Stations.SharedStationNo
                || alarm.StationNo == normalizedStationNo)
            .OrderBy(alarm => alarm.Sort)
            .ThenBy(alarm => alarm.Id)
            .ToList();
    }

    /// <summary>
    /// 聚合一轮 Bool 报警读取结果；读取失败仅返回给调用方记录日志，不参与报警判定。
    /// </summary>
    public static PlcAlarmSignalAggregation AggregateAlarmSignals(
        int stationNo,
        IEnumerable<PlcAlarmSignalReadResult> readResults)
    {
        var results = readResults.ToList();
        var activeResults = results
            .Where(result => result.IsSuccess && result.IsActive)
            .ToList();
        var messages = activeResults
            .Select(result => result.AlarmContent)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var failures = results
            .Where(result => !result.IsSuccess)
            .Select(result => new PlcAlarmReadFailure(
                result.Address.Trim(),
                result.FailureMessage.Trim()))
            .ToList();
        var scopeStationNo = activeResults.Any(result => result.StationNo <= ProductionConstants.Stations.SharedStationNo)
            ? ProductionConstants.Stations.SharedStationNo
            : NormalizeStationNo(stationNo);

        return new PlcAlarmSignalAggregation(
            activeResults.Count > 0,
            string.Join("；", messages),
            scopeStationNo,
            failures);
    }

    /// <summary>
    /// 将 Bool 报警聚合结果投影为本机软件报警与原始 PLC 外部报警字段。
    /// </summary>
    public static PlcProductionAlarmProjection ResolveProjection(
        short? deviceStatusCode,
        PlcAlarmSignalAggregation boolAlarm)
    {
        var softwareAlarm = Resolve(
            deviceStatusCode,
            boolAlarm.HasActiveSignal,
            [boolAlarm.Message]);
        if (deviceStatusCode != ProductionConstants.PlcDeviceStatuses.Alarm)
        {
            return new PlcProductionAlarmProjection(
                softwareAlarm.IsActive,
                softwareAlarm.Message,
                string.Empty,
                null);
        }

        var externalMessage = string.IsNullOrWhiteSpace(boolAlarm.Message)
            ? GenericAlarmMessage
            : boolAlarm.Message.Trim();
        return new PlcProductionAlarmProjection(
            softwareAlarm.IsActive,
            softwareAlarm.Message,
            externalMessage,
            boolAlarm.ScopeStationNo);
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
}

/// <summary>
/// 本机软件报警判定结果。
/// </summary>
public sealed record PlcSoftwareAlarmState(bool IsActive, string Message)
{
    public static PlcSoftwareAlarmState Inactive { get; } = new(false, string.Empty);
}

/// <summary>
/// 单个 PLC Bool 报警地址的读取结果。
/// </summary>
public sealed record PlcAlarmSignalReadResult(
    int StationNo,
    string Address,
    string AlarmContent,
    bool IsSuccess,
    bool IsActive,
    string FailureMessage);

/// <summary>
/// 单个 PLC Bool 报警地址读取失败信息。
/// </summary>
public sealed record PlcAlarmReadFailure(string Address, string Message);

/// <summary>
/// 当前工位一轮 PLC Bool 报警读取的聚合结果。
/// </summary>
public sealed record PlcAlarmSignalAggregation(
    bool HasActiveSignal,
    string Message,
    int ScopeStationNo,
    IReadOnlyList<PlcAlarmReadFailure> Failures)
{
    public static PlcAlarmSignalAggregation Empty(int stationNo)
        => new(
            false,
            string.Empty,
            stationNo <= ProductionConstants.Stations.SharedStationNo
                ? ProductionConstants.Stations.DefaultStationNo
                : stationNo,
            []);
}

/// <summary>
/// 生产快照中本机软件报警与外部原始 PLC 报警字段的投影结果。
/// </summary>
public sealed record PlcProductionAlarmProjection(
    bool IsSoftwareAlarmActive,
    string SoftwareAlarmMessage,
    string ExternalAlarmMessage,
    int? ExternalAlarmStationNo);
