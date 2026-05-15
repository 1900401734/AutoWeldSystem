using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

public interface IWeldTaskService
{
    ProductionRuntimeState CurrentState { get; }

    event EventHandler? StateChanged;

    Task<MesBaseResponse<MesServerTimeResponse>> SyncServerTimeAsync(CancellationToken cancellationToken = default);

    Task<MesWorkOrderResponse?> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default);

    void SelectStation(int stationNo);

    void SelectProcess(ExpItemData process);

    Task<IReadOnlyList<MesProgramListItemData>> LoadProgramsAsync(CancellationToken cancellationToken = default);

    Task<MesProgramData?> DownloadProgramAsync(MesProgramListItemData program, CancellationToken cancellationToken = default);

    Task<MesBaseResponse<MesUserInfoResponse>> ValidateMesOperatorAsync(string employeeNumber, CancellationToken cancellationToken = default);

    Task<BizWeldTask> StartAsync(
        string employeeNumber,
        int actualQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<MesBaseResponse<object>> ChangeStatusAsync(string statusCode, CancellationToken cancellationToken = default);

    Task<BizWeldTask> FinishAsync(string employeeNumber, int actualQty, int qualifiedQty, int failedQty, CancellationToken cancellationToken = default);

    Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default);

    void UpdateProgramContent(string content);

    void Reset();
}
