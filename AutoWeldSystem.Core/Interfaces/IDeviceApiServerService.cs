namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 设备端 HTTP API 承载服务。
/// WinForms 程序启动时启动，退出时停止。
/// </summary>
public interface IDeviceApiServerService
{
    /// <summary>
    /// 启动本机轻量 HTTP 服务。
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止本机轻量 HTTP 服务。
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
