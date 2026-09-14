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
    event EventHandler? ProgramLookupsChanged;
    IReadOnlyList<BizProgram> GetPrograms(bool includeDeleted = false);

    Task<IReadOnlyList<BizProgram>> GetProgramsAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgramLookup>> GetProgramLookupsAsync(
        CancellationToken cancellationToken = default);

    Task<BizProgram?> GetProgramAsync(
        int id,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ProgramSyncSummary> GetPendingSyncPrograms();

    Task<IReadOnlyList<ProgramSyncSummary>> GetPendingSyncProgramsAsync(
        CancellationToken cancellationToken = default);

    string BuildProgramName(string productNum, string componentCode, int sequenceNumber, string? description = null);

    /// <summary>
    /// 取指定产品工号下的下一个可用流水号，用于同工号新增程序。
    /// </summary>
    int GetNextSequenceNumber(string productNum);

    Task<int> GetNextSequenceNumberAsync(
        string productNum,
        CancellationToken cancellationToken = default);

    Task<BizProgram> SaveAsync(SaveProgramReq request, bool syncNow, CancellationToken cancellationToken = default);

    Task<SaveProgramResult> SaveWithSyncDecisionAsync(SaveProgramReq request, CancellationToken cancellationToken = default);

    Task<ProgramDeleteResult> DeleteLocalAsync(
        int id,
        string? remarkOverride = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, bool syncNow, string? remarkOverride = null, CancellationToken cancellationToken = default);

    Task SyncProgramAsync(int id, CancellationToken cancellationToken = default);

    Task SyncAllPendingAsync(CancellationToken cancellationToken = default);

    Task<int> PullFromMesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新所有本地程序的设备编号。
    /// 用于设备编号变更后统一更新历史程序，避免 MES 同步失败。
    /// </summary>
    Task UpdateAllProgramsDeviceIdAsync(string newDeviceId);

    /// <summary>
    /// 清理指定 ID 中仍异常或待同步的程序（仅删除本地主表，不同步 MES，保留历史版本）。
    /// 已同步成功或已终结的记录会跳过。
    /// </summary>
    /// <returns>实际删除的程序数量。</returns>
    Task<int> BatchDeleteLocalProgramsAsync(
        IEnumerable<int> programIds,
        CancellationToken cancellationToken = default);
}
