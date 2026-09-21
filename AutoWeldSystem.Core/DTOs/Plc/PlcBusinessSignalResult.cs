namespace AutoWeldSystem.Core.DTOs.Plc;

/// <summary>
/// Result of reading or writing a configured PLC business signal.
/// Address is returned for logging and for troubleshooting field configuration.
/// </summary>
public sealed record PlcBusinessSignalResult(bool IsSuccess, string Value, string Address, string Message)
{
    public static PlcBusinessSignalResult Success(string value, string address)
        => new(true, value, address, string.Empty);

    public static PlcBusinessSignalResult Failed(string message, string address = "")
        => new(false, string.Empty, address, message);
}

/// <summary>
/// Result of PC-recipe write plus PLC-recipe readback validation.
/// </summary>
public sealed record PlcRecipeSyncResult(bool IsSuccess, string PcRecipeCode, string PlcRecipeCode, string Message)
{
    public static PlcRecipeSyncResult Success(string recipeCode, string plcRecipeCode)
        => new(true, recipeCode, plcRecipeCode, string.Empty);

    public static PlcRecipeSyncResult Failed(string recipeCode, string plcRecipeCode, string message)
        => new(false, recipeCode, plcRecipeCode, message);
}
