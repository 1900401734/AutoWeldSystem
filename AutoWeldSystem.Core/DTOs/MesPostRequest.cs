using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.DTOs;

public class MesPostRequest<T>
{
    public ApiCode ApiCode { get; set; }

    public string ApiName { get; set; } = string.Empty;

    public T Data { get; set; } = default!;
}
