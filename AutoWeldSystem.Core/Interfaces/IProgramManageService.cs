using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 加工程序管理服务。
/// 负责本地程序版本、MES 同步状态和手动重试，界面只调用服务，不直接访问数据库和 MES。
/// </summary>
public interface IProgramManageService
{
    IReadOnlyList<BizProgram> GetPrograms(bool includeDeleted = false);

    IReadOnlyList<BizProgramRevision> GetRevisions(int programLocalId);

    IReadOnlyList<ProgramSyncSummary> GetPendingSyncPrograms();

    string BuildProgramName(string productNum, string componentCode, int sequenceNumber);

    Task<BizProgram> SaveAsync(ProgramSaveRequest request, bool syncNow, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, bool syncNow, CancellationToken cancellationToken = default);

    Task SyncProgramAsync(int id, CancellationToken cancellationToken = default);

    Task SyncAllPendingAsync(CancellationToken cancellationToken = default);

    Task<int> PullFromMesAsync(string? productNum = null, CancellationToken cancellationToken = default);
}
