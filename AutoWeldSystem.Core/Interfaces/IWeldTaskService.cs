using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

public interface IWeldTaskService
{
    ProductionRuntimeState CurrentState { get; }

    event EventHandler? StateChanged;

    /// <summary>
    /// 同步服务器时间
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MesBaseResponse<MesServerTimeResponse>> SyncServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取工单信息
    /// </summary>
    /// <param name="workId"></param>
    /// <param name="stationNo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MesWorkOrderResponse?> GetWorkOrderInfoAsync(
        string workId,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 选择工位。支持多工位生产时，切换当前操作的工位，确保后续操作针对正确的工位进行。
    /// </summary>
    /// <param name="stationNo"></param>
    void SelectStation(int stationNo);

    /// <summary>
    /// 选择工序。根据工单信息，选择当前工位的工序，确保后续操作针对正确的工序进行。
    /// </summary>
    /// <param name="process"></param>
    /// <param name="stationNo"></param>
    void SelectProcess(ExpItemData process, int stationNo = ProductionConstants.Stations.DefaultStationNo);


    Task<IReadOnlyList<MesProgramListItemData>> LoadProgramsAsync(
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<MesProgramData?> DownloadProgramAsync(
        MesProgramListItemData program,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<MesBaseResponse<MesUserInfoResponse>> ValidateMesOperatorAsync(
        string employeeNumber,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<BizWeldTask> StartAsync(
        string employeeNumber,
        int actualQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<MesBaseResponse<object>> ChangeStatusAsync(
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

    Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default);

    void UpdateProgramContent(string content, int stationNo = ProductionConstants.Stations.DefaultStationNo);

    void Reset();
}
