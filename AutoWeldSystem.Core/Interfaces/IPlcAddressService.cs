using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// PLC 地址配置服务。
/// 界面负责维护地址，PLC 通讯服务通过它读取心跳等关键地址。
/// </summary>
public interface IPlcAddressService
{
    IReadOnlyList<BizPlcAddress> GetAll();

    BizPlcAddress? GetByKey(string addressKey);

    BizPlcAddress? GetByKey(string logicalKey, int stationNo);

    void SaveAll(IEnumerable<BizPlcAddress> addresses);
}
