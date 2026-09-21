namespace AutoWeldSystem.Core.Exceptions;

/// <summary>
/// 采集准入被拒绝：数据库已判定该任务不再处于运行态，本轮不得读写 PLC、保存或入队。
/// 与普通采集失败区分：普通失败仍要向 PLC 反馈 1 完成握手，拒绝则把握手交给正确的实例或超时兜底。
/// </summary>
public sealed class TaskRunRejectedException(string message) : Exception(message);
