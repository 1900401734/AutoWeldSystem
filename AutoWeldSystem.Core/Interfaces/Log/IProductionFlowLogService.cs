using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces.Log;

/// <summary>
/// 生产流程日志服务。
/// UI 可以订阅 LogWritten 实时刷新，业务服务只负责写入关键步骤。
/// </summary>
public interface IProductionFlowLogService
{
    event EventHandler<ProductionFlowLogEntry>? LogWritten;

    void Write(ProductionFlowLogEntry entry);

    void Write(
        string step,
        string summary,
        string detail = "",
        string level = "Info",
        int stationNo = 0,
        string workOrderId = "",
        string productNo = "",
        string programId = "",
        string plcSignal = "",
        string plcAddress = "",
        long? durationMilliseconds = null);

    IReadOnlyList<ProductionFlowLogEntry> GetByDate(DateTime date, int take = 500);

    string GetLogDirectory();
}
