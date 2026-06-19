using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 报警地址配置服务。
/// </summary>
public interface IPlcAlarmAddressService
{
    /// <summary>
    /// 获取全部报警地址配置。
    /// </summary>
    IReadOnlyList<BizPlcAlarmAddress> GetAll();

    /// <summary>
    /// 获取指定工位可用的报警地址，包含共享工位 0 的报警点。
    /// </summary>
    IReadOnlyList<BizPlcAlarmAddress> GetEnabledForStation(int stationNo);

    /// <summary>
    /// 保存全部报警地址配置。
    /// </summary>
    void SaveAll(IEnumerable<BizPlcAlarmAddress> alarms);
}
