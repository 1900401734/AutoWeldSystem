using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;

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

    /// <summary>判断工位是否处于报警状态。</summary>
    public static bool IsAlarm(CenterDashboardStationDto station)
        => station.State.PlcDeviceStatusCode?.Trim() == ProductionConstants.PlcDeviceStatuses.Text.Alarm;

    /// <summary>设备下任一工位报警即视为设备报警。</summary>
    public static bool HasAlarm(CenterDashboardDeviceDto device)
        => device.Stations.Any(IsAlarm);

    /// <summary>
    /// 推断工位是否已开工。协议中没有开工布尔字段，
    /// 工单号非空即代表设备端存在一个未结束的任务。
    /// </summary>
    public static bool IsWorking(CenterDashboardStationDto station)
        => !string.IsNullOrWhiteSpace(station.CurrentWorkOrder);

    /// <summary>
    /// 设备卡片状态类名。优先级：离线 &gt; 报警 &gt; 运行 &gt; 暂停 &gt; 停止。
    /// 设备级 State.PlcDeviceStatusCode 恒为空，状态只能从工位集合推导。
    /// </summary>
    public static string DeviceCardClass(CenterDashboardDeviceDto device)
    {
        if (!device.State.ClientOnline)
        {
            return "offline-card";
        }

        if (HasAlarm(device))
        {
            return "alarm-card";
        }

        if (HasStationWithCode(device, ProductionConstants.PlcDeviceStatuses.Text.Running))
        {
            return "running-card";
        }

        if (HasStationWithCode(device, ProductionConstants.PlcDeviceStatuses.Text.Paused))
        {
            return "paused-card";
        }

        if (HasStationWithCode(device, ProductionConstants.PlcDeviceStatuses.Text.Stopped))
        {
            return "stopped-card";
        }

        return "unknown-card";
    }

    /// <summary>工位面板状态类名。</summary>
    public static string StationStateClass(CenterDashboardStationDto station)
    {
        return station.State.PlcDeviceStatusCode?.Trim() switch
        {
            ProductionConstants.PlcDeviceStatuses.Text.Running => "station-running",
            ProductionConstants.PlcDeviceStatuses.Text.Paused => "station-paused",
            ProductionConstants.PlcDeviceStatuses.Text.Stopped => "station-stopped",
            ProductionConstants.PlcDeviceStatuses.Text.Alarm => "station-alarm",
            _ => "station-unknown"
        };
    }

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
    /// 工位报警文案。状态码为报警但设备端未给出消息时给出回退文案，
    /// 避免报警工位在页面上完全没有可见提示。
    /// </summary>
    public static string? ResolveAlarmText(CenterDashboardStationDto station)
    {
        var message = station.State.AlarmMessage?.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            return message;
        }

        return IsAlarm(station) ? "报警（无详细信息）" : null;
    }

    private static bool HasStationWithCode(CenterDashboardDeviceDto device, string statusCode)
        => device.Stations.Any(station => station.State.PlcDeviceStatusCode?.Trim() == statusCode);
}
