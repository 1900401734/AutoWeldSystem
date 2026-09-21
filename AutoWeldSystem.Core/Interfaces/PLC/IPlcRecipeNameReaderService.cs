using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// 按工位读取 PLC 配方名称表。
/// </summary>
public interface IPlcRecipeNameReaderService
{
    /// <summary>
    /// 读取指定工位配置范围内的全部配方名称。
    /// </summary>
    Task<PlcRecipeNameReadResult> ReadStationAsync(
        int stationNo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 直接读取尚未保存的内存配置，供地址维护预览使用。
    /// </summary>
    Task<PlcRecipeNameReadResult> ReadConfigAsync(
        AutoWeldSystem.Core.Entities.BizPlcRecipeNameConfig config,
        CancellationToken cancellationToken = default);
}
