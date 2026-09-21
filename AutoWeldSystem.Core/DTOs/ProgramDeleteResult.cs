namespace AutoWeldSystem.Core.DTOs;

public sealed class ProgramDeleteResult
{
    public int Id { get; init; }

    public string ProgramName { get; init; } = string.Empty;

    public bool RequiresMesSync { get; init; }
}
