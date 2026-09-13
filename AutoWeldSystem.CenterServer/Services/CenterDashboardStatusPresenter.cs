using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 看板展示层的状态推导。只负责把 DTO 翻译成 CSS 类名与展示口径，
/// 不含业务规则，因此留在中心服务器项目而不下沉到 Core。
/// </summary>
internal static class CenterDashboardStatusPresenter
{
    /// <summary>合格率良好阈值（百分比）。</summary>
    private const decimal GoodRateThreshold = 98m;

    /// <summary>合格率警告阈值（百分比），低于此值视为严重。</summary>
    private const decimal WarnRateThreshold = 95m;

    /// <summary>新设备以有效报警为准，null 才沿用旧协议的原始状态码 4。</summary>
    public static bool IsAlarm(CenterDashboardStationDto station)
        => station.EffectiveAlarm?.IsActive
            ?? (station.State.PlcDeviceStatusCode?.Trim() == ProductionConstants.PlcDeviceStatuses.Text.Alarm);

    /// <summary>设备下任一工位报警即视为设备报警。</summary>
    public static bool HasAlarm(CenterDashboardDeviceDto device)
        => device.Stations.Any(IsAlarm);

    /// <summary>总览标签与颜色取同一工位状态，避免双工位出现颜色与文案错配。</summary>
    public static (string ClassName, string Label) OverviewState(CenterDashboardDeviceDto device)
    {
        if (!device.State.ClientOnline)
        {
            return ("offline", "离线");
        }

        if (HasAlarm(device))
        {
            return ("alarm", "报警");
        }

        var state = device.Stations.Select(OverviewStationState)
            .OrderByDescending(state => state.Priority)
            .FirstOrDefault();
        return (state.ClassName ?? "unknown", state.Label ?? "待更新");
    }

    /// <summary>运行台数只认在线且已连接的 PLC 运行状态，不根据标签颜色推断。</summary>
    public static bool IsRunningDevice(CenterDashboardDeviceDto device)
        => device.State.ClientOnline && !HasAlarm(device)
            && !device.Stations.Any(station => station.EffectiveAlarm is { IsPendingConfirmation: true } or { IsRawAlarmUnconfirmed: true })
            && device.Stations.Any(station => station.State.PlcConnected
                && !IsLifecycleStatus(station)
                && station.State.PlcDeviceStatusCode?.Trim() == ProductionConstants.PlcDeviceStatuses.Text.Running);

    public static (string ClassName, string Label) StationStatus(CenterDashboardStationDto station)
    {
        var state = OverviewStationState(station);
        return (state.ClassName, state.Label);
    }

    private static (string ClassName, string Label, int Priority) OverviewStationState(CenterDashboardStationDto station)
    {
        if (IsAlarm(station)) return ("alarm", "报警", 100);
        if (station.EffectiveAlarm?.IsPendingConfirmation == true) return ("alarm-pending", "报警待确认", 95);
        if (station.EffectiveAlarm?.IsRawAlarmUnconfirmed == true) return ("unknown", "报警未确认", 90);

        var code = station.State.PlcDeviceStatusCode?.Trim();
        var label = station.State.PlcDeviceStatusName?.Trim();
        // 已收到明确无报警时，不能被尚未复位的原始码 4 或历史异常日志重新点亮。
        if (station.EffectiveAlarm is not null && code == ProductionConstants.PlcDeviceStatuses.Text.Alarm)
            return ("unknown", "无有效报警", 0);
        var unknown = ("unknown", string.IsNullOrEmpty(label) ? "待更新" : label, 0);
        if (IsLifecycleStatus(station))
        {
            // 两套协议的 1 含义不同：生命周期是开机，不是 PLC 正在运行。
            return code switch
            {
                ProductionConstants.MesDeviceStatuses.PoweredOn => ("powered-on", "开机", 50),
                ProductionConstants.MesDeviceStatuses.Stopped => ("stopped", "停机", 30),
                ProductionConstants.MesDeviceStatuses.Exception => ("alarm", "异常", 100),
                ProductionConstants.MesDeviceStatuses.Recovered => ("recovered", "异常恢复", 45),
                ProductionConstants.MesDeviceStatuses.ProgramStarted => ("program-started", "程序执行开始", 70),
                ProductionConstants.MesDeviceStatuses.ProgramEnded => ("program-ended", "程序执行结束", 40),
                _ => unknown
            };
        }

        if (!station.State.PlcConnected)
        {
            return ("unknown", "PLC未连接", 0);
        }

        return code switch
        {
            ProductionConstants.PlcDeviceStatuses.Text.Running => ("running", "运行", 80),
            ProductionConstants.PlcDeviceStatuses.Text.Paused => ("paused", "暂停/空闲", 60),
            ProductionConstants.PlcDeviceStatuses.Text.Stopped => ("stopped", "停止", 30),
            ProductionConstants.PlcDeviceStatuses.Text.Alarm => ("alarm", "报警", 100),
            _ => unknown
        };
    }

    private static bool IsLifecycleStatus(CenterDashboardStationDto station)
        => string.Equals(station.StatusSource?.Trim(), "Lifecycle", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(station.StatusSource)
                && station.State.PlcDeviceStatusName?.Trim() is "开机" or "停机" or "异常" or "异常恢复" or "程序执行开始" or "程序执行结束");

    /// <summary>
    /// 推断工位是否已开工。协议中没有开工布尔字段，
    /// 工单号非空即代表设备端存在一个未结束的任务。
    /// </summary>
    public static bool IsWorking(CenterDashboardStationDto station)
        => !string.IsNullOrWhiteSpace(station.CurrentWorkOrder);

    /// <summary>卡片与总览共用有效状态，报警不再给整张卡片加底色。</summary>
    public static string DeviceCardClass(CenterDashboardDeviceDto device)
        => $"{OverviewState(device).ClassName}-card";

    public static string StationStateClass(CenterDashboardStationDto station)
        => $"station-{StationStatus(station).ClassName}";

    /// <summary>
    /// 计算合格率百分比。总数为 0 时返回 null，由调用方显示 "--"，不做除零。
    /// </summary>
    public static decimal? QualifiedRate(int totalCount, int qualifiedCount)
    {
        if (totalCount <= 0)
        {
            return null;
        }

        return (decimal)qualifiedCount * 100m / totalCount;
    }

    /// <summary>合格率的着色类名，阈值集中在本类维护。</summary>
    public static string QualifiedRateClass(decimal? rate)
    {
        if (rate is null)
        {
            return "rate-none";
        }

        if (rate >= GoodRateThreshold)
        {
            return "rate-good";
        }

        return rate >= WarnRateThreshold ? "rate-warn" : "rate-crit";
    }

    /// <summary>
    /// 计算生产达成率百分比，口径为合格数 / 工单数量，与设备端保持一致。
    /// 工单数量为 0（未开工或旧版设备未上报）时返回 null，由调用方显示 "--"。
    /// 超产时可大于 100。
    /// </summary>
    public static decimal? AchievementRate(int workOrderQuantity, int qualifiedCount)
    {
        if (workOrderQuantity <= 0)
        {
            return null;
        }

        return (decimal)qualifiedCount * 100m / workOrderQuantity;
    }

    /// <summary>总览与详情共用任务数量，旧协议缺少任务累计时不回退到全天合格数。</summary>
    public static decimal? TaskAchievementRate(CenterDashboardStationDto station)
        => IsWorking(station) && station.TaskQualifiedCount.HasValue
            ? AchievementRate(station.WorkOrderQuantity, station.TaskQualifiedCount.Value)
            : null;

    /// <summary>未知同步状态不能作为零失败；只有全部设备上报时才显示汇总。</summary>
    public static (int? Pending, int? Failed) ReportSyncCounts(IReadOnlyCollection<CenterDashboardDeviceDto> devices)
        => devices.Count > 0 && devices.All(device => device.State.ClientOnline && device.ReportSync is not null)
            ? (devices.Sum(device => device.ReportSync!.PendingCount), devices.Sum(device => device.ReportSync!.FailedCount))
            : (null, null);

    /// <summary>
    /// 达成率着色类名。与合格率不同，达成率达标即视为良好，超产同样按良好显示。
    /// </summary>
    public static string AchievementRateClass(decimal? rate)
    {
        if (rate is null)
        {
            return "rate-none";
        }

        if (rate >= 100m)
        {
            return "rate-good";
        }

        return rate >= WarnRateThreshold ? "rate-warn" : "rate-crit";
    }

    /// <summary>共享报警只显示一次；不同工位独有的原因保留工位标签。</summary>
    public static IReadOnlyList<string> AlarmMessages(CenterDashboardDeviceDto device)
    {
        if (!device.State.ClientOnline) return [];
        var notices = device.Stations.SelectMany(station =>
        {
            var alarm = station.EffectiveAlarm;
            var confirmed = IsAlarm(station);
            var pending = !confirmed && alarm?.IsPendingConfirmation == true;
            var unconfirmed = !confirmed && !pending && alarm?.IsRawAlarmUnconfirmed == true;
            if (!confirmed && !pending && !unconfirmed) return Enumerable.Empty<(CenterDashboardStationDto Station, string Message)>();

            var message = alarm is null ? CenterTelemetryRules.StripStationSuffix(station.State.AlarmMessage) : alarm.Message;
            var messages = PlcAlarmNotificationRules.SplitMessages(message);
            if (messages.Count == 0)
            {
                messages = [confirmed ? CenterTelemetryRules.UnknownAlarmText : "PLC状态为报警，未匹配到有效报警地址"];
            }
            var prefix = pending ? "待确认：" : unconfirmed ? "未确认：" : string.Empty;
            return messages.Select(text => (Station: station, Message: prefix + text));
        });

        return notices.GroupBy(notice => notice.Message, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (device.Stations.Count <= 1 || group.Select(item => item.Station.StationNo).Distinct().Count() > 1)
                    return group.Key;
                var station = group.First().Station;
                var label = string.IsNullOrWhiteSpace(station.StationName)
                    ? ResolveStationLabel(station.StationNo, true)
                    : station.StationName.Trim();
                return $"{label}：{group.Key}";
            }).ToList();
    }

    /// <summary>双工位设备的工位标签，单工位返回 null。</summary>
    public static string? ResolveStationLabel(int stationNo, bool isDualStation)
        => CenterTelemetryRules.ResolveStationAlarmLabel(stationNo, isDualStation);
}
