using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 报警地址配置服务。
/// 该服务只负责配置读写和基础校验，实际 PLC 读取由生产监控服务统一执行。
/// </summary>
public sealed class PlcAlarmAddressService(SqlSugarDbContext dbContext) : IPlcAlarmAddressService
{
    private readonly object _dbLock = new();

    public IReadOnlyList<BizPlcAlarmAddress> GetAll()
    {
        lock (_dbLock)
        {
            dbContext.InitDatabase();
            return dbContext.Db.Queryable<BizPlcAlarmAddress>()
                .OrderBy(it => it.Sort)
                .OrderBy(it => it.Id)
                .ToList()
                .Select(NormalizeLoadedAlarm)
                .ToList();
        }
    }

    public IReadOnlyList<BizPlcAlarmAddress> GetEnabledForStation(int stationNo)
    {
        _ = stationNo;
        return GetAll().Where(alarm => alarm.Enabled).ToList();
    }

    public void SaveAll(IEnumerable<BizPlcAlarmAddress> alarms)
    {
        var normalized = alarms
            .Select(CloneAndNormalize)
            .ToList();
        Validate(normalized);

        lock (_dbLock)
        {
            dbContext.InitDatabase();

            // 报警地址是小体量配置，整体替换能让删除、批量粘贴和排序行为保持简单明确。
            dbContext.Db.Deleteable<BizPlcAlarmAddress>().ExecuteCommand();
            if (normalized.Count > 0)
            {
                dbContext.Db.Insertable(normalized).ExecuteCommand();
            }
        }
    }

    private static BizPlcAlarmAddress CloneAndNormalize(BizPlcAlarmAddress alarm)
    {
        return new BizPlcAlarmAddress
        {
            StationNo = ProductionConstants.Stations.SharedStationNo,
            Address = alarm.Address.Trim(),
            AlarmContent = alarm.AlarmContent.Trim(),
            Enabled = alarm.Enabled,
            Sort = Math.Max(0, alarm.Sort),
            UpdatedTime = DateTime.Now
        };
    }

    private static void Validate(IReadOnlyList<BizPlcAlarmAddress> alarms)
    {
        foreach (var alarm in alarms)
        {
            if (string.IsNullOrWhiteSpace(alarm.Address))
            {
                throw new InvalidOperationException("报警地址不能为空。");
            }

            if (string.IsNullOrWhiteSpace(alarm.AlarmContent))
            {
                throw new InvalidOperationException("报警内容不能为空。");
            }
        }

        var duplicate = alarms
            .GroupBy(alarm => alarm.Address, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"报警地址“{duplicate.Key}”重复。");
        }
    }

    private static BizPlcAlarmAddress NormalizeLoadedAlarm(BizPlcAlarmAddress alarm)
    {
        alarm.StationNo = ProductionConstants.Stations.SharedStationNo;
        return alarm;
    }
}
