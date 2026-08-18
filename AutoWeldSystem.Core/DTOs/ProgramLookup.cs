using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 程序选择、筛选和配方匹配所需的轻量程序快照。
/// 不包含程序内容、执行文件和同步消息等大字段。
/// </summary>
public sealed record ProgramLookup
{
    public int Id { get; init; }
    public string? ProgramId { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string ProductNum { get; init; } = string.Empty;
    public string? ProductModel { get; init; }
    public string? RecipeCode { get; init; }
    public string? Station2RecipeCode { get; init; }
    public string? ComponentCode { get; init; }
    public string ProgramType { get; init; } = "0";
    public int SequenceNumber { get; init; }
    public string? Description { get; init; }
    public int VersionNumber { get; init; }
    public string SyncStatus { get; init; } = string.Empty;
    public DateTime UpdatedTime { get; init; }

    public BizProgram ToEntityStub()
        => new()
        {
            Id = Id,
            ProgramId = ProgramId,
            ProgramName = ProgramName,
            DeviceId = DeviceId,
            ProductNum = ProductNum,
            ProductModel = ProductModel,
            RecipeCode = RecipeCode,
            Station2RecipeCode = Station2RecipeCode,
            ComponentCode = ComponentCode,
            ProgramType = ProgramType,
            SequenceNumber = SequenceNumber,
            Description = Description,
            VersionNumber = VersionNumber,
            SyncStatus = SyncStatus,
            UpdatedTime = UpdatedTime
        };
}
