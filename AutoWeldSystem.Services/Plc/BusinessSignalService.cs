using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.PLC;
using System.Globalization;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC business-signal helper.
/// It keeps address lookup and data-type conversion in one place, so screens and production services can work with logical keys.
/// </summary>
public sealed class BusinessSignalService : IPlcBusinessSignalService
{
    private static readonly TimeSpan RecipePollInterval = TimeSpan.FromMilliseconds(200);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;

    public BusinessSignalService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
    }

    /// <summary>
    /// Reads a logical PLC signal and normalizes it to text.
    /// </summary>
    public async Task<PlcBusinessSignalResult> ReadTextAsync(string logicalKey, int stationNo, CancellationToken cancellationToken = default)
    {
        var address = ResolveAddress(logicalKey, stationNo);
        if (address is null)
        {
            return PlcBusinessSignalResult.Failed($"PLC business address '{logicalKey}' is not configured or disabled.");
        }

        var plcAddress = address.Address!.Trim();
        switch (NormalizeDataType(address.DataType))
        {
            case AppConstants.PlcDataTypes.Bool:
                var boolResult = await _plcCommunicationService.ReadBoolAsync(plcAddress, cancellationToken);
                return boolResult.IsSuccess
                    ? PlcBusinessSignalResult.Success(boolResult.Value ? "1" : "0", plcAddress)
                    : PlcBusinessSignalResult.Failed(boolResult.Message, plcAddress);
            case AppConstants.PlcDataTypes.Int32:
                var int32Result = await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken);
                return int32Result.IsSuccess
                    ? PlcBusinessSignalResult.Success(int32Result.Value.ToString(CultureInfo.InvariantCulture), plcAddress)
                    : PlcBusinessSignalResult.Failed(int32Result.Message, plcAddress);
            case AppConstants.PlcDataTypes.Float:
                var floatResult = await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken);
                return floatResult.IsSuccess
                    ? PlcBusinessSignalResult.Success(floatResult.Value.ToString(CultureInfo.InvariantCulture), plcAddress)
                    : PlcBusinessSignalResult.Failed(floatResult.Message, plcAddress);
            case AppConstants.PlcDataTypes.String:
                var stringResult = await _plcCommunicationService.ReadStringAsync(
                    plcAddress,
                    (ushort)Math.Max(1, address.DataLength),
                    cancellationToken);
                return stringResult.IsSuccess
                    ? PlcBusinessSignalResult.Success(NormalizePlcText(stringResult.Value), plcAddress)
                    : PlcBusinessSignalResult.Failed(stringResult.Message, plcAddress);
            default:
                var int16Result = await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken);
                return int16Result.IsSuccess
                    ? PlcBusinessSignalResult.Success(int16Result.Value.ToString(CultureInfo.InvariantCulture), plcAddress)
                    : PlcBusinessSignalResult.Failed(int16Result.Message, plcAddress);
        }
    }

    /// <summary>
    /// Writes a logical PLC signal after converting text to the configured PLC data type.
    /// </summary>
    public async Task<PlcBusinessSignalResult> WriteTextAsync(string logicalKey, int stationNo, string value, CancellationToken cancellationToken = default)
    {
        var address = ResolveAddress(logicalKey, stationNo);
        if (address is null)
        {
            return PlcBusinessSignalResult.Failed($"PLC business address '{logicalKey}' is not configured or disabled.");
        }

        var normalizedValue = NormalizePlcText(value);
        var plcAddress = address.Address!.Trim();
        var result = NormalizeDataType(address.DataType) switch
        {
            AppConstants.PlcDataTypes.Bool => await WriteBoolAsync(plcAddress, normalizedValue, cancellationToken),
            AppConstants.PlcDataTypes.Int32 => await WriteInt32Async(plcAddress, normalizedValue, cancellationToken),
            AppConstants.PlcDataTypes.Float => await WriteFloatAsync(plcAddress, normalizedValue, cancellationToken),
            AppConstants.PlcDataTypes.String => await _plcCommunicationService.WriteStringAsync(plcAddress, normalizedValue, cancellationToken),
            _ => await WriteInt16Async(plcAddress, normalizedValue, cancellationToken)
        };

        return result.IsSuccess
            ? PlcBusinessSignalResult.Success(normalizedValue, plcAddress)
            : PlcBusinessSignalResult.Failed(result.Message, plcAddress);
    }

    /// <summary>
    /// Writes the PC-side work-order status signal.
    /// </summary>
    public Task<PlcBusinessSignalResult> WriteWorkOrderStatusAsync(int stationNo, int status, CancellationToken cancellationToken = default)
        => WriteTextAsync(
            AppConstants.PlcLogicalKeys.WorkOrderStatus,
            stationNo,
            status.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

    /// <summary>
    /// Writes the PC-side device mode signal.
    /// </summary>
    public Task<PlcBusinessSignalResult> WriteDeviceModeAsync(int stationNo, int mode, CancellationToken cancellationToken = default)
        => WriteTextAsync(
            AppConstants.PlcLogicalKeys.DeviceMode,
            stationNo,
            mode.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

    /// <summary>
    /// Writes the PC recipe code and waits until PLC recipe code matches it.
    /// </summary>
    public async Task<PlcRecipeSyncResult> SyncRecipeCodeAsync(int stationNo, string recipeCode, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var normalizedRecipe = NormalizePlcText(recipeCode);
        if (string.IsNullOrWhiteSpace(normalizedRecipe))
        {
            return PlcRecipeSyncResult.Failed(string.Empty, string.Empty, "Recipe code is empty.");
        }

        var writeResult = await WriteTextAsync(
            AppConstants.PlcLogicalKeys.PcRecipeCode,
            stationNo,
            normalizedRecipe,
            cancellationToken);
        if (!writeResult.IsSuccess)
        {
            return PlcRecipeSyncResult.Failed(normalizedRecipe, string.Empty, writeResult.Message);
        }

        var deadline = DateTime.UtcNow + (timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : timeout);
        var lastReadValue = string.Empty;
        var lastMessage = string.Empty;
        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var readResult = await ReadTextAsync(AppConstants.PlcLogicalKeys.PlcRecipeCode, stationNo, cancellationToken);
            lastReadValue = NormalizePlcText(readResult.Value);
            lastMessage = readResult.Message;
            if (readResult.IsSuccess
                && string.Equals(lastReadValue, normalizedRecipe, StringComparison.OrdinalIgnoreCase))
            {
                return PlcRecipeSyncResult.Success(normalizedRecipe, lastReadValue);
            }

            await Task.Delay(RecipePollInterval, cancellationToken);
        }

        var detail = string.IsNullOrWhiteSpace(lastMessage)
            ? $"PLC recipe '{lastReadValue}' did not match PC recipe '{normalizedRecipe}' before timeout."
            : lastMessage;
        return PlcRecipeSyncResult.Failed(normalizedRecipe, lastReadValue, detail);
    }

    private BizPlcAddress? ResolveAddress(string logicalKey, int stationNo)
    {
        var address = _addressService.GetAddress(logicalKey, stationNo);
        return address is null || !address.Enabled || string.IsNullOrWhiteSpace(address.Address)
            ? null
            : address;
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static string NormalizePlcText(string? value)
        => (value ?? string.Empty).Trim().Trim('\0');

    private async Task<PlcServiceResult> WriteBoolAsync(string address, string value, CancellationToken cancellationToken)
    {
        if (value is "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return await _plcCommunicationService.WriteBoolAsync(address, true, cancellationToken);
        }

        if (value is "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return await _plcCommunicationService.WriteBoolAsync(address, false, cancellationToken);
        }

        return PlcServiceResult.Fail($"Value '{value}' cannot be written to a Bool PLC address.");
    }

    private async Task<PlcServiceResult> WriteInt16Async(string address, string value, CancellationToken cancellationToken)
    {
        return short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteInt16Async(address, parsed, cancellationToken)
            : PlcServiceResult.Fail($"Value '{value}' cannot be converted to Int16.");
    }

    private async Task<PlcServiceResult> WriteInt32Async(string address, string value, CancellationToken cancellationToken)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteInt32Async(address, parsed, cancellationToken)
            : PlcServiceResult.Fail($"Value '{value}' cannot be converted to Int32.");
    }

    private async Task<PlcServiceResult> WriteFloatAsync(string address, string value, CancellationToken cancellationToken)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteFloatAsync(address, parsed, cancellationToken)
            : PlcServiceResult.Fail($"Value '{value}' cannot be converted to Float.");
    }
}
