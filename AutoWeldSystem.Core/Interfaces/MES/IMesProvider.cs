using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Upload;

namespace AutoWeldSystem.Core.Interfaces.MES;

public interface IMesProvider
{
    /// <summary>
    /// 获取员工信息。
    /// </summary>
    Task<BasicRes<UserInfoRes>> GetUserInfoAsync(string userNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取工单信息。
    /// </summary>
    Task<BasicRes<WorkOrderRes>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置设备编号。
    /// </summary>
    Task<BasicRes<object>> SetDeviceIdAsync(AddDeviceReq addDeviceRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务器时间。
    /// </summary>
    Task<BasicRes<ServerTimeRes>> GetServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定的 MES 地址测试在线检测接口连通性。
    /// 这个接口不会改写数据库中的正式配置，只用于设置页临时检测。
    /// </summary>
    Task<BasicRes<object>> TestConnectionAsync(string baseUrl, int timeoutSeconds, bool isWriteLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 探测 MES 是否在线，供心跳轮询使用。
    /// previousOnline 表示上一轮原始探测结果；仅当本次结果与其不同（或其为 null）时写交互日志，
    /// 避免心跳按秒刷爆 MES 日志。
    /// </summary>
    Task<BasicRes<object>> CheckSystemOnlineAsync(bool? previousOnline, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增程序。
    /// </summary>
    Task<BasicRes<ProgramDataRes>> AddExpProgramAsync(ProgramDataWriteReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新程序。
    /// </summary>
    Task<BasicRes<ProgramDataRes>> UpdateExpProgramAsync(ProgramDataWriteReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取程序列表。
    /// </summary>
    Task<BasicRes<List<MesProgramListItemData>>> GetProgramListAsync(string deviceId, string? productNum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载程序。
    /// </summary>
    Task<BasicRes<ProgramDataRes>> DownloadProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除程序。
    /// </summary>
    Task<BasicRes<object>> DeleteExpProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设备状态上报。
    /// </summary>
    Task<BasicRes<object>> ReportDeviceStatusAsync(ReportDeviceStatusReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 开工上报。
    /// </summary>
    Task<BasicRes<ExperimentStartRes>> StartWorkAsync(ExperimentStartReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 工单状态变更。
    /// </summary>
    Task<BasicRes<object>> ChangeWorkStatusAsync(ReportExperimentStatusReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完工上报。
    /// </summary>
    Task<BasicRes<object>> EndWorkAsync(ExperimentEndReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 报告文件上报。
    /// </summary>
    Task<BasicRes<object>> UploadReportFileAsync(UploadReportFileReq requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 采集参数上传。
    /// </summary>
    Task<BasicRes<object>> UploadProcessParametersAsync(IReadOnlyList<ProcessParameterUploadItem> requestData, CancellationToken cancellationToken = default);
}
