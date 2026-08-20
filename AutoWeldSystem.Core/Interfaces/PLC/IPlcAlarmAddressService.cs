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
    /// 获取全部已启用报警地址。stationNo 仅为历史接口兼容参数，不再过滤报警地址。
    /// </summary>
    IReadOnlyList<BizPlcAlarmAddress> GetEnabledForStation(int stationNo);

    /// <summary>
    /// 保存全部报警地址配置。
    /// </summary>
    void SaveAll(IEnumerable<BizPlcAlarmAddress> alarms);
}
