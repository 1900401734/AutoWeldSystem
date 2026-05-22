using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

public interface IWeldTaskService
{
    ProductionRuntimeState CurrentState { get; }

    event EventHandler? StateChanged;

    /// <summary>
    /// ͬ��������ʱ��
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MesBaseResponse<MesServerTimeResponse>> SyncServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ��ȡ������Ϣ
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
    /// ѡ��λ��֧�ֶ๤λ����ʱ���л���ǰ�����Ĺ�λ��ȷ���������������ȷ�Ĺ�λ���С�
    /// </summary>
    /// <param name="stationNo"></param>
    void SelectStation(int stationNo);

    /// <summary>
    /// ѡ���򡣸��ݹ�����Ϣ��ѡ��ǰ��λ�Ĺ���ȷ���������������ȷ�Ĺ�����С�
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

    void ApplyStartAdjustment(
        MesWorkOrderResponse workOrder,
        ExpItemData? process,
        string programContent,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    Task<MesBaseResponse<MesUserInfoResponse>> ValidateMesOperatorAsync(
        string employeeNumber,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);

    Task<BizWeldTask> StartAsync(
        string employeeNumber,
        int actualQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        bool employeeAlreadyValidated = false,
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
