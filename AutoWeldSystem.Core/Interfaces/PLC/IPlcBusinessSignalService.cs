using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// Reads and writes business-level PLC signals by logical address key.
/// UI code should depend instead of  on this servicehard-coding PLC data type conversions.
/// </summary>
public interface IPlcBusinessSignalService
{
    Task<PlcBusinessSignalResult> ReadTextAsync(string logicalKey, int stationNo, CancellationToken cancellationToken = default);

    Task<PlcBusinessSignalResult> WriteTextAsync(string logicalKey, int stationNo, string value, CancellationToken cancellationToken = default);

    Task<PlcBusinessSignalResult> WriteWorkOrderStatusAsync(int stationNo, int status, CancellationToken cancellationToken = default);

    Task<PlcBusinessSignalResult> WriteDeviceModeAsync(int stationNo, int mode, CancellationToken cancellationToken = default);

    Task<PlcRecipeSyncResult> SyncRecipeCodeAsync(int stationNo, string recipeCode, TimeSpan timeout, CancellationToken cancellationToken = default);
}
