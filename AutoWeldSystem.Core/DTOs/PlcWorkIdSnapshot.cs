namespace AutoWeldSystem.Core.DTOs;

public sealed record PlcWorkIdSnapshot(
    bool IsSuccess,
    string WorkId,
    DateTime UpdatedTime,
    string Message);
