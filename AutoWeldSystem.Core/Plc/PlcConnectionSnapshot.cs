using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.Plc;

public sealed record PlcConnectionSnapshot(PlcConnectionState State,
    bool IsConnected,
    string Endpoint,
    DateTime? LastConnectedTime,
    DateTime? LastHeartbeatTime,
    string Message)
{
    /// <summary>
    /// 快照所属工位。旧调用不传该属性时默认表示工位1。
    /// </summary>
    public int StationNo { get; init; } = 1;
}
