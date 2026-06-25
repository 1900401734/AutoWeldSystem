namespace AutoWeldSystem.Core.DTOs.Plc;

/// <summary>
/// Represents the latest PLC-side recipe code read for a station.
/// The value is read-only telemetry and does not imply a PC recipe write.
/// </summary>
public sealed record PlcRecipeCodeSnapshot
{
    /// <summary>
    /// Station number that produced this recipe snapshot.
    /// </summary>
    public int StationNo { get; init; }

    /// <summary>
    /// PLC-side recipe code after trimming whitespace and null terminators.
    /// </summary>
    public string RecipeCode { get; init; } = string.Empty;

    /// <summary>
    /// Whether the PLC recipe read succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Read result message, normally empty on success.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Time when this snapshot was produced.
    /// </summary>
    public DateTime ReadAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Creates a successful PLC recipe snapshot.
    /// </summary>
    public static PlcRecipeCodeSnapshot Success(int stationNo, string recipeCode)
    {
        return new PlcRecipeCodeSnapshot
        {
            StationNo = stationNo,
            RecipeCode = recipeCode,
            IsSuccess = true,
            ReadAt = DateTime.Now
        };
    }

    /// <summary>
    /// Creates a failed PLC recipe snapshot.
    /// </summary>
    public static PlcRecipeCodeSnapshot Failed(int stationNo, string message)
    {
        return new PlcRecipeCodeSnapshot
        {
            StationNo = stationNo,
            Message = message,
            IsSuccess = false,
            ReadAt = DateTime.Now
        };
    }
}
