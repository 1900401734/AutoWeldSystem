using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 加工程序管理服务。
/// 负责本地程序版本、MES 同步状态和手动重试，界面只调用服务，不直接访问数据库和 MES。
/// </summary>
public interface IProgramManageService
{
    IReadOnlyList<BizProgram> GetPrograms(bool includeDeleted = false);

    IReadOnlyList<ProgramSyncSummary> GetPendingSyncPrograms();

    string BuildProgramName(string productNum, string componentCode, int sequenceNumber, string? description = null);

    /// <summary>
    /// 取指定产品工号下的下一个可用流水号，用于同工号新增程序。
    /// </summary>
    int GetNextSequenceNumber(string productNum);

    Task<BizProgram> SaveAsync(SaveProgramReq request, bool syncNow, CancellationToken cancellationToken = default);

    Task<SaveProgramResult> SaveWithSyncDecisionAsync(SaveProgramReq request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, bool syncNow, string? remarkOverride = null, CancellationToken cancellationToken = default);

    Task SyncProgramAsync(int id, CancellationToken cancellationToken = default);

    Task SyncAllPendingAsync(CancellationToken cancellationToken = default);

    Task<int> PullFromMesAsync(CancellationToken cancellationToken = default);
}
