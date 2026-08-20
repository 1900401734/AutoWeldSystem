using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.Center;

/// <summary>
/// Pure rules shared by center telemetry ingestion, dashboard projection, and tests.
/// </summary>
public static class CenterTelemetryRules
{
    /// <summary>
    /// Resolves the stable device key used by the center server.
    /// </summary>
    public static string ResolveDeviceKey(CenterTelemetrySnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.DeviceId.Trim();
    }

    /// <summary>
    /// Determines whether the device client is online based only on heartbeat freshness.
    /// </summary>
    public static bool IsClientOnline(DateTime? lastSeenAt, DateTime now, int offlineTimeoutSeconds)
    {
        if (lastSeenAt is null)
        {
            return false;
        }

        var timeout = Math.Max(1, offlineTimeoutSeconds);
        return now - lastSeenAt.Value <= TimeSpan.FromSeconds(timeout);
    }

    /// <summary>
    /// Builds the dashboard state without rewriting the PLC status when the client is offline.
    /// </summary>
    public static CenterDashboardDeviceStateDto BuildDashboardState(
        CenterDeviceRuntimeDto snapshot,
        DateTime now,
        int offlineTimeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new CenterDashboardDeviceStateDto
        {
            ClientOnline = IsClientOnline(snapshot.LastSeenAt, now, offlineTimeoutSeconds),
            PlcConnected = snapshot.PlcConnected,
            PlcConnectionState = snapshot.PlcConnectionState.Trim(),
            PlcDeviceStatusCode = snapshot.PlcDeviceStatusCode.Trim(),
            PlcDeviceStatusName = ResolveReportedStatusName(snapshot.PlcDeviceStatusCode, snapshot.PlcDeviceStatusName),
            AlarmMessage = snapshot.AlarmMessage.Trim(),
            LastSeenAt = snapshot.LastSeenAt,
            CollectedAt = snapshot.CollectedAt
        };
    }

    /// <summary>
    /// Normalizes an optional URL to the trailing-slash format used by HttpClient.
    /// </summary>
    public static string NormalizeBaseUrl(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? CenterServerConstants.DefaultBaseUrl
            : normalized.TrimEnd('/') + "/";
    }

    /// <summary>
    /// Keeps heartbeat intervals within a practical local-network range.
    /// </summary>
    public static int NormalizeHeartbeatIntervalSeconds(int value)
        => Math.Clamp(value, 2, 60);

    /// <summary>
    /// Keeps offline timeout above the heartbeat interval to avoid false offline states.
    /// </summary>
    public static int NormalizeOfflineTimeoutSeconds(int value)
        => Math.Clamp(value, 5, 300);

    /// <summary>
    /// Returns a stable Chinese name for known PLC status codes.
    /// </summary>
    public static string ResolvePlcStatusName(string? statusCode, string? fallbackName = null)
    {
        return statusCode?.Trim() switch
        {
            ProductionConstants.PlcDeviceStatuses.Text.Running => "运行",
            ProductionConstants.PlcDeviceStatuses.Text.Paused => "暂停/空闲",
            ProductionConstants.PlcDeviceStatuses.Text.Stopped => "停止",
            ProductionConstants.PlcDeviceStatuses.Text.Alarm => "报警",
            _ => string.IsNullOrWhiteSpace(fallbackName) ? "未知" : fallbackName.Trim()
        };
    }

    /// <summary>
    /// Preserves a source-aware status name and only derives a PLC name when the sender omitted it.
    /// </summary>
    public static string ResolveReportedStatusName(string? statusCode, string? reportedName)
        => string.IsNullOrWhiteSpace(reportedName)
            ? ResolvePlcStatusName(statusCode)
            : reportedName.Trim();

    /// <summary>
    /// 在工位与共享设备状态间选择较新的 JSONL 记录；时间相同时保留工位记录。
    /// </summary>
    public static BizDeviceStatusLog? ResolveLatestDeviceStatus(
        BizDeviceStatusLog? stationStatus,
        BizDeviceStatusLog? sharedStatus)
    {
        if (stationStatus is null)
        {
            return sharedStatus;
        }

        return sharedStatus is not null && sharedStatus.OccurredTime > stationStatus.OccurredTime
            ? sharedStatus
            : stationStatus;
    }

    /// <summary>
    /// 解析上报给中心的工位报警内容。
    /// PLC 实时报警优先；PLC 无值时只有设备状态为「异常」的 JSONL 备注才算报警——
    /// 开机/停机/异常恢复/程序执行开始/结束 都不是报警，其备注不得当作报警上送
    /// （这正是看板上出现「程序执行结束」的原因）。
    /// 同时剥离 MES 备注契约追加的「；工位：工位N」后缀：工位归属由 StationNo 表达，
    /// 文本里再带一次会与看板的工位标签重复。
    /// </summary>
    public static string ResolveAlarmMessage(string? plcAlarmMessage, BizDeviceStatusLog? stationStatus)
    {
        var plcAlarm = plcAlarmMessage?.Trim();
        if (!string.IsNullOrEmpty(plcAlarm))
        {
            return StripStationSuffix(plcAlarm);
        }

        // PLC 已恢复（AlarmMessage 为空）时不得再用历史备注顶替，否则报警永远不消失。
        if (stationStatus?.DeviceStatus?.Trim() != ProductionConstants.MesDeviceStatuses.Exception)
        {
            return string.Empty;
        }

        return StripStationSuffix(stationStatus.Remark);
    }

    /// <summary>
    /// 剥离 MES 备注中的「；工位：工位N」/「工位：双工位」后缀与「异常-工位N：」前缀。
    /// MES 上报契约本身保持不变，这里只做展示侧的反向清洗。
    /// </summary>
    public static string StripStationSuffix(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 后缀：全角/半角分号 + 「工位：xxx」，位于文本末尾。
        var separatorIndex = text.LastIndexOfAny([';', '；']);
        if (separatorIndex > 0
            && text[(separatorIndex + 1)..].TrimStart().StartsWith("工位：", StringComparison.Ordinal))
        {
            text = text[..separatorIndex].Trim();
        }

        // 前缀：兼容历史「异常-工位N：」/「工位N：」以及当前「异常：」格式。
        var prefixEnd = text.IndexOf('：', StringComparison.Ordinal);
        if (prefixEnd > 0)
        {
            var prefix = text[..prefixEnd];
            if (prefix.Contains("工位", StringComparison.Ordinal)
                || string.Equals(prefix, "异常", StringComparison.Ordinal)
                || string.Equals(prefix, "异常恢复", StringComparison.Ordinal))
            {
                text = text[(prefixEnd + 1)..].Trim();
            }
        }

        return text.TrimEnd('；', ';').Trim();
    }

    /// <summary>
    /// 报警内容缺失时的回退文案：状态码是报警但设备端没给出原因，
    /// 页面上也必须有可见提示，不能只留一条空横幅。
    /// </summary>
    public const string UnknownAlarmText = "报警（无详细信息）";

    /// <summary>
    /// 双工位设备的工位标签，沿用设备端「左/右」命名约定；单工位返回 null（不标注工位）。
    /// </summary>
    public static string? ResolveStationAlarmLabel(int stationNo, bool isDualStation)
    {
        if (!isDualStation)
        {
            return null;
        }

        return stationNo switch
        {
            1 => $"{StationDisplayNameRules.DefaultStation1DisplayName}工位",
            2 => $"{StationDisplayNameRules.DefaultStation2DisplayName}工位",
            _ => $"工位 {stationNo}"
        };
    }

    /// <summary>
    /// 组装报警展示文案，格式为「&lt;左/右工位：&gt;具体报警信息」。
    /// 尖括号部分仅双工位设备标注，单工位只显示报警内容本身。
    /// <paramref name="isAlarm"/> 为 false 时返回 null —— 异常恢复后横幅必须消失，
    /// 只凭报警文本非空来判断会让陈旧内容一直挂在卡片上。
    /// </summary>
    public static string? FormatStationAlarmText(
        bool isAlarm,
        string? alarmMessage,
        int stationNo,
        bool isDualStation)
    {
        if (!isAlarm)
        {
            return null;
        }

        var message = StripStationSuffix(alarmMessage);
        if (string.IsNullOrEmpty(message))
        {
            message = UnknownAlarmText;
        }

        var label = ResolveStationAlarmLabel(stationNo, isDualStation);
        return label is null ? message : $"{label}：{message}";
    }

    /// <summary>
    /// Normalizes empty system types into the generic dashboard group.
    /// </summary>
    public static string NormalizeSystemType(string? value)
        => string.IsNullOrWhiteSpace(value) ? CenterServerConstants.SystemTypes.Other : value.Trim();

    /// <summary>
    /// 计算遥测快照的内容签名，用于判断本周期是否需要推送全量数据。
    /// 刻意排除 HeartbeatAt 与 CollectedAt：这两个时间戳每周期必变，纳入会使签名永远不同。
    /// </summary>
    public static string BuildSnapshotSignature(CenterTelemetrySnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new System.Text.StringBuilder();
        builder.Append(request.DeviceId.Trim()).Append('\n');
        builder.Append(request.DeviceName.Trim()).Append('\n');
        builder.Append(request.SystemType.Trim()).Append('\n');

        foreach (var station in request.Stations.OrderBy(it => it.StationNo))
        {
            builder.Append(station.StationNo).Append('|');
            builder.Append(station.PlcConnected).Append('|');
            builder.Append(station.PlcConnectionState).Append('|');
            builder.Append(station.DeviceStatusCode).Append('|');
            builder.Append(station.DeviceStatusName).Append('|');
            builder.Append(station.AlarmMessage).Append('|');
            builder.Append(station.CurrentWorkOrder).Append('|');
            builder.Append(station.ProductJobNo).Append('|');
            builder.Append(station.ProductModel).Append('|');
            builder.Append(station.TodayTotalCount).Append('|');
            builder.Append(station.TodayQualifiedCount).Append('|');
            builder.Append(station.TodayFailedCount).Append('|');
            builder.Append(station.WorkOrderQuantity).Append('\n');
        }

        return builder.ToString();
    }
}
