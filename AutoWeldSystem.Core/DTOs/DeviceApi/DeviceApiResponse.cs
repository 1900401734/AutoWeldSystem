namespace AutoWeldSystem.Core.DTOs.DeviceApi;

/// <summary>
/// Local device API response envelope. It intentionally excludes internal convenience fields.
/// </summary>
public sealed class DeviceApiResponse<T>
{
    public string Status { get; init; } = string.Empty;

    public string Msg { get; init; } = string.Empty;

    public T? Data { get; init; }
}
