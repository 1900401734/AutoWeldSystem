using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Interfaces;

public interface IMesProvider
{
    /// <summary>
    /// 获取员工信息。
    /// </summary>
    Task<MesBaseResponse<MesUserInfoResponse>> GetUserInfoAsync(string userNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取工单信息。
    /// </summary>
    Task<MesBaseResponse<MesWorkOrderResponse>> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置设备编号。
    /// </summary>
    Task<MesBaseResponse<object>> SetDeviceIdAsync(AddDeviceRequest addDeviceRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务器时间。
    /// </summary>
    Task<MesBaseResponse<MesServerTimeResponse>> GetServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 探测 MES 服务器时间接口是否可用。
    /// 该方法仅用于后台连接监控，不写交互日志，避免高频探测刷屏。
    /// </summary>
    Task<MesBaseResponse<MesServerTimeResponse>> ProbeServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定的 MES 地址测试接口连通性。
    /// 这个接口不会改写数据库中的正式配置，只用于设置页临时检测。
    /// </summary>
    Task<MesBaseResponse<MesServerTimeResponse>> TestConnectionAsync(string baseUrl, int timeoutSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增程序。
    /// </summary>
    Task<MesBaseResponse<MesProgramData>> AddExpProgramAsync(MesProgramData requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新程序。
    /// </summary>
    Task<MesBaseResponse<MesProgramData>> UpdateExpProgramAsync(MesProgramData requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取程序列表。
    /// </summary>
    Task<MesBaseResponse<List<MesProgramListItemData>>> GetProgramListAsync(string deviceId, string? productNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载程序。
    /// </summary>
    Task<MesBaseResponse<MesProgramData>> DownloadProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除程序。
    /// </summary>
    Task<MesBaseResponse<object>> DeleteExpProgramAsync(string deviceId, string programId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设备状态上报。
    /// </summary>
    Task<MesBaseResponse<object>> ReportDeviceStatusAsync(ReportDeviceStatusRequest requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 开工上报。
    /// </summary>
    Task<MesBaseResponse<ExpStartResponse>> StartWorkAsync(ExpStartRequest requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 工单状态变更。
    /// </summary>
    Task<MesBaseResponse<object>> ChangeWorkStatusAsync(ExpStatusRequest requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完工上报。
    /// </summary>
    Task<MesBaseResponse<object>> EndWorkAsync(ExpEndRequest requestData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 报告文件上报。
    /// </summary>
    Task<MesBaseResponse<object>> UploadReportFileAsync(ReportFileUploadRequest requestData, CancellationToken cancellationToken = default);
}
