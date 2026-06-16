namespace AutoWeldSystem.Core.DTOs.Mes.Response;

public class BasicRes<T>
{
    public string Status { get; set; } = string.Empty;

    public string Msg { get; set; } = string.Empty;

    public T? Data { get; set; }

    public bool IsSuccess => string.Equals(Status, nameof(Enums.Status.S), StringComparison.OrdinalIgnoreCase);
}
