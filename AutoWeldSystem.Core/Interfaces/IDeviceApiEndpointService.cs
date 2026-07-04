using AutoWeldSystem.Core.DTOs.DeviceApi;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 设备端 HTTP 接口的业务服务。
/// 该接口只描述业务行为，具体 HTTP 承载由 UI 层负责。
/// </summary>
public interface IDeviceApiEndpointService
{
    /// <summary>
    /// 查询当前设备的 MES 设备状态。
    /// </summary>
    BasicRes<DeviceStatusQueryRes> GetDeviceStatus(string? deviceId);

    /// <summary>
    /// 接收平台下发的设备编号配置，并保存到本地设置。
    /// </summary>
    Task<BasicRes<DeviceIdSetRes>> SetDeviceIdAsync(
        AddDeviceReq request,
        CancellationToken cancellationToken = default);
}
