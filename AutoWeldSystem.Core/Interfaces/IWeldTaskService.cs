using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Core.Interfaces;

public interface IWeldTaskService
{
    ProductionRuntimeState CurrentState { get; }

    event EventHandler? StateChanged;

    /// <summary>
    /// 查询指定工位当前是否存在尚未完工的焊接任务。
    /// </summary>
    /// <param name="stationNo">工位号。</param>
    /// <returns>未完工任务；若没有则返回 null。</returns>
    BizWeldTask? GetUnfinishedTask(int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 将指定工位的本地未完工任务恢复到当前运行态。
    /// </summary>
    /// <param name="stationNo">工位号。</param>
    /// <returns>恢复成功的未完工任务；若没有可恢复任务则返回 null。</returns>
    BizWeldTask? RestoreUnfinishedTask(int stationNo = ProductionConstants.Stations.DefaultStationNo);

    Task<BasicRes<ServerTimeRes>> SyncServerTimeAsync(CancellationToken cancellationToken = default);

    Task<WorkOrderRes?> GetWorkOrderInfoAsync(string workId, int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    void SelectStation(int stationNo);

    void SelectProcess(ExpItemData process, int stationNo = ProductionConstants.Stations.DefaultStationNo);

    Task<IReadOnlyList<MesProgramListItemData>> LoadProgramsAsync(
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<ProgramDataRes?> DownloadProgramAsync(
        MesProgramListItemData program,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    void ApplyStartAdjustment(
        WorkOrderRes workOrder,
        ExpItemData? process,
        string programContent,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// Creates and starts a local work order without MES calls.
    /// The generated start report is queued for makeup upload after MES recovers.
    /// </summary>
    Task<BizWeldTask> StartLocalAsync(
        OfflineExperimentStartReq request,
        string operatorNumber,
        int actualQty,
        CancellationToken cancellationToken = default);

    Task<BasicRes<UserInfoRes>> ValidateMesOperatorAsync(
        string employeeNumber,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<BizWeldTask> StartAsync(
        string employeeNumber,
        int actualQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        bool employeeAlreadyValidated = false,
        CancellationToken cancellationToken = default);

    Task<BasicRes<object>> ChangeStatusAsync(
        string statusCode,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<BizWeldTask> FinishAsync(
        string employeeNumber,
        int actualQty,
        int qualifiedQty,
        int failedQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<BizWeldTask> FinishLocalAsync(
        string employeeNumber,
        int actualQty,
        int qualifiedQty,
        int failedQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the recipe code of an existing task and synchronizes the in-memory runtime state.
    /// </summary>
    /// <param name="taskId">Task database id.</param>
    /// <param name="recipeCode">Recipe code read from PLC or selected by the PC.</param>
    /// <param name="stationNo">Station context used to refresh the compatibility runtime state.</param>
    /// <returns>true when the task exists and was updated; otherwise false.</returns>
    bool TryUpdateRecipeCode(
        int taskId,
        string recipeCode,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default);

    void UpdateProgramContent(string content, int stationNo = ProductionConstants.Stations.DefaultStationNo);

    void Reset();
}
