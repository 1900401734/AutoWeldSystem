using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>显式演示模式，无数据库、采集服务或启动项副作用。</summary>
internal static class CenterDashboardDemoData
{
    public static CenterDashboardSnapshotDto Create()
    {
        var names = new[] { "动触镶组点焊设备", "静触点组点焊设备", "转换座点焊设备", "接触系统检测设备", "动和静触簧组点焊设备", "备用点焊设备", "待接入设备", "整件焊接设备", "辅助点焊设备", "恢复运行设备" };
        var totals = new[] { 1489, 866, 215, 994, 2342, 0, 0, 25, 80, 120 };
        var failed = new[] { 12, 3, 0, 18, 7, 0, 0, 1, 2, 0 };
        var targets = new[] { 2000, 1200, 600, 1200, 2200, 0, 0, 100, 100, 300 };
        var states = new (string Source, string Code, string Name, bool Online)[]
        {
            ("Lifecycle", "1", "开机", true),
            ("PLC", "1", "运行", true),
            ("PLC", "1", "运行", true),
            ("PLC", "4", "报警", true),
            ("PLC", "2", "暂停/空闲", true),
            ("Lifecycle", "0", "停机", false),
            ("PLC", "0", "未知", true),
            ("Lifecycle", "6", "程序执行开始", true),
            ("Lifecycle", "7", "程序执行结束", true),
            ("Lifecycle", "5", "异常恢复", true)
        };
        var snapshot = new CenterDashboardSnapshotDto { IsDemo = true };
        for (var index = 0; index < names.Length; index++)
        {
            var now = DateTime.Now;
            var state = states[index];
            var alarm = state.Code == "4";
            var device = new CenterDashboardDeviceDto
            {
                DeviceId = $"DEMO-{index + 1:00}",
                DeviceName = names[index],
                SystemType = index == 3 ? CenterServerConstants.SystemTypes.WholePiece : CenterServerConstants.SystemTypes.Electromagnetic,
                State = new CenterDashboardDeviceStateDto { ClientOnline = state.Online, LastSeenAt = state.Online ? now : now.AddMinutes(-5), CollectedAt = now },
                ReportSync = new CenterReportSyncSummaryDto { PendingCount = index == 3 ? 2 : 0, LastVerifiedAt = now.AddMinutes(-4) },
                Stations =
                [
                    new CenterDashboardStationDto
                    {
                        StationNo = 1,
                        State = new CenterDashboardDeviceStateDto
                        {
                            ClientOnline = state.Online,
                            PlcConnected = state.Online,
                            PlcDeviceStatusCode = state.Code,
                            PlcDeviceStatusName = state.Name,
                            AlarmMessage = alarm ? "气源压力不足，请检查设备气路" : string.Empty,
                            LastSeenAt = now,
                            CollectedAt = now
                        },
                        StatusSource = state.Source,
                        EffectiveAlarm = index switch
                        {
                            2 => new CenterEffectiveAlarmDto { IsActive = true, Message = "气源压力不足；安全门未关闭" },
                            3 => new CenterEffectiveAlarmDto { IsPendingConfirmation = true },
                            5 => new CenterEffectiveAlarmDto { IsActive = true, Message = "断线前的报警" },
                            _ => new CenterEffectiveAlarmDto()
                        },
                        ProductionDate = now.Date,
                        CurrentWorkOrder = $"DEMO-WO-{index + 1:000}",
                        TaskKey = $"demo-task-{index + 1}",
                        ProgramName = $"标准焊接程序 {index + 1:00}",
                        ProductJobNo = $"DEMO-P{index + 1:00}",
                        TodayTotalCount = totals[index],
                        TodayQualifiedCount = totals[index] - failed[index],
                        TodayFailedCount = failed[index],
                        TaskTotalCount = totals[index],
                        TaskQualifiedCount = totals[index] - failed[index],
                        TaskFailedCount = failed[index],
                        WorkOrderQuantity = targets[index]
                    }
                ]
            };
            if (index == 2)
            {
                // 两个工位共享设备报警，验证原因去重及原始运行状态保留。
                var second = System.Text.Json.JsonSerializer.Deserialize<CenterDashboardStationDto>(
                    System.Text.Json.JsonSerializer.Serialize(device.Stations[0]))!;
                second.StationNo = 2;
                second.TodayTotalCount = second.TodayQualifiedCount = second.TodayFailedCount = 0;
                device.Stations.Add(second);
            }
            snapshot.Devices.Add(device);
        }
        return snapshot;
    }
}
