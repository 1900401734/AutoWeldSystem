using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// 按连续地址读取 PLC 配方名称。
/// 单个地址失败时保留失败信息并继续读取后续槽位。
/// </summary>
public sealed class PlcRecipeNameReaderService(
    IPlcRecipeNameConfigService configService,
    IPlcCommunicationService plcCommunicationService) : IPlcRecipeNameReaderService
{
    public async Task<PlcRecipeNameReadResult> ReadStationAsync(
        int stationNo,
        CancellationToken cancellationToken = default)
    {
        var config = await Task.Run(
            () => configService.GetForStation(stationNo),
            cancellationToken);
        if (config is null)
        {
            return FailedResult(stationNo, "当前工位未配置 PLC 配方名称地址。");
        }

        return await ReadConfigAsync(config, cancellationToken);
    }

    public async Task<PlcRecipeNameReadResult> ReadConfigAsync(
        BizPlcRecipeNameConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var stationNo = config.StationNo;

        if (!config.Enabled)
        {
            return FailedResult(stationNo, "当前工位未启用 PLC 配方名称读取。");
        }

        BizPlcRecipeNameConfig normalizedConfig;
        try
        {
            normalizedConfig = PlcRecipeNameConfigRules.NormalizeAndValidate([config], DateTime.Now)[0];
        }
        catch (InvalidOperationException ex)
        {
            // 历史数据库或手工改库可能留下非法配置，读取入口应返回可展示错误而不是中断界面刷新。
            return FailedResult(stationNo, $"PLC 配方名称配置无效：{ex.Message}");
        }

        var names = new Dictionary<int, string?>();
        var failures = new List<PlcRecipeNameReadFailure>();
        for (var recipeCode = 1; recipeCode <= normalizedConfig.RecipeCount; recipeCode++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var address = PlcRecipeNameRules.ResolveAddress(normalizedConfig, recipeCode);
            try
            {
                var readResult = await plcCommunicationService.ReadStringAsync(
                    address,
                    (ushort)normalizedConfig.StringLength,
                    cancellationToken);
                if (readResult.IsSuccess)
                {
                    names[recipeCode] = readResult.Value;
                    continue;
                }

                failures.Add(new PlcRecipeNameReadFailure(
                    stationNo,
                    recipeCode,
                    address,
                    string.IsNullOrWhiteSpace(readResult.Message) ? "PLC 配方名称读取失败。" : readResult.Message));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add(new PlcRecipeNameReadFailure(stationNo, recipeCode, address, ex.Message));
            }
        }

        var options = PlcRecipeNameRules.BuildOptions(normalizedConfig, names);
        return new PlcRecipeNameReadResult(
            stationNo,
            failures.Count == 0,
            failures.Count == 0 ? string.Empty : $"{failures.Count} 个配方名称地址读取失败。",
            options,
            failures);
    }

    private static PlcRecipeNameReadResult FailedResult(int stationNo, string message)
        => new(
            stationNo,
            IsSuccess: false,
            message,
            Array.Empty<PlcRecipeNameOption>(),
            Array.Empty<PlcRecipeNameReadFailure>());
}
