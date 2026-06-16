using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 通讯服务抽象，UI 和业务层只依赖该接口，不直接依赖 HSL 第三方库。
/// </summary>
public interface IPlcCommunicationService : IAsyncDisposable
{
    /// <summary>
    /// PLC 连接状态变化事件，监控界面通过它刷新 PLC 在线状态。
    /// </summary>
    event EventHandler<PlcConnectionSnapshot>? StatusChanged;

    /// <summary>
    /// 当前 PLC 连接状态快照，页面首次打开时可直接读取。
    /// </summary>
    PlcConnectionSnapshot Current { get; }

    /// <summary>
    /// 获取指定工位最近一次 PLC 状态快照；工位1仍可通过 Current 读取。
    /// </summary>
    PlcConnectionSnapshot GetCurrent(int stationNo);

    /// <summary>
    /// 启动后台连接循环，负责自动连接、心跳检测和断线重连。
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止后台连接循环并主动关闭 PLC 连接。
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 配置保存后调用，使用最新数据库参数重新连接 PLC。
    /// </summary>
    Task RestartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 Bool 类型地址。
    /// </summary>
    Task<PlcServiceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 16 位整数地址。
    /// </summary>
    Task<PlcServiceResult<short>> ReadInt16Async(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 32 位整数地址。
    /// </summary>
    Task<PlcServiceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取浮点数地址。
    /// </summary>
    Task<PlcServiceResult<float>> ReadFloatAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取字符串地址，length 用于告诉 PLC 库读取多少个字符。
    /// </summary>
    Task<PlcServiceResult<string>> ReadStringAsync(string address, ushort length, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入 Bool 类型地址。
    /// </summary>
    Task<PlcServiceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入 16 位整数地址。
    /// </summary>
    Task<PlcServiceResult> WriteInt16Async(string address, short value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入 32 位整数地址。
    /// </summary>
    Task<PlcServiceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入浮点数地址。
    /// </summary>
    Task<PlcServiceResult> WriteFloatAsync(string address, float value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入字符串地址。
    /// </summary>
    Task<PlcServiceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default);
}
