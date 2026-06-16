using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 地址配置服务。
/// </summary>
public interface IPlcAddressService
{
    IReadOnlyList<BizPlcAddress> GetAll();

    /// <summary>
    /// 根据逻辑地址键和工位号获取PLC地址配置。
    /// </summary>
    /// <param name="logicalKey">逻辑地址键</param>
    /// <param name="stationNo">工位号</param>
    /// <returns>PLC地址配置</returns>
    BizPlcAddress? GetAddress(string logicalKey, int stationNo);

    void SaveAll(IEnumerable<BizPlcAddress> addresses);
}
