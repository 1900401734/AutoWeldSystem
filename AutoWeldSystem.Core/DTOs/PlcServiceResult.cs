namespace AutoWeldSystem.Core.DTOs;

public sealed record PlcServiceResult(bool IsSuccess, string Message)
{
    public static PlcServiceResult Success(string message = "") => new(true, message);

    public static PlcServiceResult Fail(string message) => new(false, message);
}

public sealed record PlcServiceResult<T>(bool IsSuccess, string Message, T? Value)
{
    public static PlcServiceResult<T> Success(T value, string message = "") => new(true, message, value);

    public static PlcServiceResult<T> Fail(string message) => new(false, message, default);
}
