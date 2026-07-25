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
        var activeAlarms = activeResults
            .Select(result => new PlcActiveAlarm(
                result.StationNo,
                result.Address.Trim(),
                result.AlarmContent.Trim()))
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
            activeAlarms,
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
/// 已置位的单个 PLC 报警地址及其配置内容。
/// </summary>
public sealed record PlcActiveAlarm(int StationNo, string Address, string AlarmContent);

/// <summary>
/// 当前工位一轮 PLC Bool 报警读取的聚合结果。
/// </summary>
public sealed record PlcAlarmSignalAggregation(
    bool HasActiveSignal,
    string Message,
    int ScopeStationNo,
    IReadOnlyList<PlcActiveAlarm> ActiveAlarms,
    IReadOnlyList<PlcAlarmReadFailure> Failures)
{
    public static PlcAlarmSignalAggregation Empty(int stationNo)
        => new(
            false,
            string.Empty,
            stationNo <= ProductionConstants.Stations.SharedStationNo
                ? ProductionConstants.Stations.DefaultStationNo
                : stationNo,
            [],
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

/// <summary>
/// 当前设备报警周期中仍有效的报警快照。
/// </summary>
public sealed record PlcDeviceAlarmCycleState
{
    public PlcDeviceAlarmCycleState()
        : this([])
    {
    }

    public PlcDeviceAlarmCycleState(IEnumerable<PlcActiveAlarm> activeAlarms)
    {
        ActiveAlarms = activeAlarms
            .Where(alarm => !string.IsNullOrWhiteSpace(alarm.Address))
            .DistinctBy(PlcDeviceAlarmCycleRules.GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(PlcDeviceAlarmCycleRules.GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<PlcActiveAlarm> ActiveAlarms { get; }
}

/// <summary>
/// 设备状态与报警 Bool 地址的双条件事件判定。
/// </summary>
public static class PlcDeviceAlarmCycleRules
{
    /// <summary>
    /// 从设备状态 JSONL 的最新视图恢复尚未出现状态 5 的报警周期。
    /// </summary>
    public static PlcDeviceAlarmCycleState Restore(IEnumerable<BizDeviceStatusLog> logs)
    {
        var activeAlarms = new Dictionary<string, PlcActiveAlarm>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs.OrderBy(log => log.OccurredTime))
        {
            if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Recovered)
            {
                if (string.IsNullOrWhiteSpace(log.AlarmAddress))
                {
                    activeAlarms.Clear();
                }
                else
                {
                    activeAlarms.Remove(GetAlarmKey(log.StationNo, log.AlarmAddress));
                }
            }
            else if (log.DeviceStatus == ProductionConstants.MesDeviceStatuses.Exception
                && !string.IsNullOrWhiteSpace(log.AlarmAddress))
            {
                var alarm = new PlcActiveAlarm(
                    log.StationNo,
                    log.AlarmAddress.Trim(),
                    string.IsNullOrWhiteSpace(log.AlarmContent) ? "设备异常" : log.AlarmContent.Trim());
                activeAlarms[GetAlarmKey(alarm)] = alarm;
            }
        }

        return new PlcDeviceAlarmCycleState(activeAlarms.Values);
    }

    public static PlcDeviceAlarmCycleDecision Decide(
        PlcDeviceAlarmCycleState state,
        string? triggerMode,
        IReadOnlyDictionary<int, short?> deviceStatuses,
        IReadOnlyList<PlcAlarmSignalReadResult> readResults,
        IReadOnlyList<PlcActiveAlarm> configuredAlarms)
    {
        var mode = AppConstants.PlcAlarmTriggerModes.Normalize(triggerMode);
        var previousByKey = state.ActiveAlarms.ToDictionary(
            GetAlarmKey,
            StringComparer.OrdinalIgnoreCase);
        var configuredByKey = configuredAlarms
            .Where(alarm => !string.IsNullOrWhiteSpace(alarm.Address))
            .DistinctBy(GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(GetAlarmKey, StringComparer.OrdinalIgnoreCase);
        var preserveMissingConfigurations = deviceStatuses.Count == 0
            && configuredByKey.Count == 0
            && readResults.Count == 0;
        var resultsByKey = readResults
            .Where(result => !string.IsNullOrWhiteSpace(result.Address))
            .GroupBy(result => GetAlarmKey(result.StationNo, result.Address), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        var nextByKey = new Dictionary<string, PlcActiveAlarm>(StringComparer.OrdinalIgnoreCase);

        if (preserveMissingConfigurations)
        {
            foreach (var (key, alarm) in previousByKey.Where(pair => !configuredByKey.ContainsKey(pair.Key)))
            {
                nextByKey[key] = alarm;
            }
        }

        foreach (var (key, configuredAlarm) in configuredByKey)
        {
            if (!resultsByKey.TryGetValue(key, out var result) || !result.IsSuccess)
            {
                if (previousByKey.TryGetValue(key, out var previousAlarm))
                {
                    nextByKey[key] = previousAlarm;
                }
                continue;
            }

            var statusGate = ResolveStatusGate(mode, configuredAlarm.StationNo, deviceStatuses);
            if (result.IsActive && statusGate == true)
            {
                nextByKey[key] = previousByKey.GetValueOrDefault(key) ?? configuredAlarm;
            }
            else if (statusGate is null
                && previousByKey.TryGetValue(key, out var previousAlarm))
            {
                // 双条件模式的状态读取失败时，地址置位或归零都不足以证明已恢复。
                nextByKey[key] = previousAlarm;
            }
        }

        var newAlarms = nextByKey
            .Where(pair => !previousByKey.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .OrderBy(GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recoveredAlarms = previousByKey
            .Where(pair => !nextByKey.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .OrderBy(GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new PlcDeviceAlarmCycleDecision(
            newAlarms,
            recoveredAlarms,
            recoveredAlarms.Count > 0 && nextByKey.Count > 0 && newAlarms.Count == 0,
            new PlcDeviceAlarmCycleState(nextByKey.Values));
    }

    /// <summary>
    /// 报警身份包含配置工位，避免共享地址和工位地址相互覆盖。
    /// </summary>
    public static string GetAlarmKey(PlcActiveAlarm alarm) => GetAlarmKey(alarm.StationNo, alarm.Address);

    public static string GetAlarmKey(int stationNo, string address)
        => $"{Math.Max(ProductionConstants.Stations.SharedStationNo, stationNo)}:{AlarmAddressImportRules.NormalizeAddress(address)}";

    /// <summary>
    /// 将配置快照转换为规则输入，保持报警内容和共享范围。
    /// </summary>
    public static IReadOnlyList<PlcActiveAlarm> ToConfiguredAlarms(
        IEnumerable<BizPlcAlarmAddress> alarmAddresses)
        => alarmAddresses
            .Where(alarm => alarm.Enabled && !string.IsNullOrWhiteSpace(alarm.Address))
            .Select(alarm => new PlcActiveAlarm(
                Math.Max(ProductionConstants.Stations.SharedStationNo, alarm.StationNo),
                AlarmAddressImportRules.NormalizeAddress(alarm.Address),
                string.IsNullOrWhiteSpace(alarm.AlarmContent) ? "设备异常" : alarm.AlarmContent.Trim()))
            .DistinctBy(GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetAlarmKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool? ResolveStatusGate(
        string mode,
        int stationNo,
        IReadOnlyDictionary<int, short?> deviceStatuses)
    {
        if (mode == AppConstants.PlcAlarmTriggerModes.AddressOnly)
        {
            return true;
        }

        if (stationNo <= ProductionConstants.Stations.SharedStationNo)
        {
            if (deviceStatuses.Values.Any(status => status == ProductionConstants.PlcDeviceStatuses.Alarm))
            {
                return true;
            }

            return deviceStatuses.Count == 0 || deviceStatuses.Values.Any(status => status is null)
                ? null
                : false;
        }

        return !deviceStatuses.TryGetValue(stationNo, out var status) || status is null
            ? null
            : status == ProductionConstants.PlcDeviceStatuses.Alarm;
    }
}

public sealed record PlcDeviceAlarmCycleDecision(
    IReadOnlyList<PlcActiveAlarm> NewAlarms,
    IReadOnlyList<PlcActiveAlarm> RecoveredAlarms,
    bool ShouldReassertException,
    PlcDeviceAlarmCycleState NextState);
