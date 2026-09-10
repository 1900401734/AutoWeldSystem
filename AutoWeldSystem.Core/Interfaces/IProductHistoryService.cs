using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Reads product history for MonitorView and applies product-level operator actions.
/// Keeping this logic in a service avoids direct database rules inside the UI.
/// </summary>
public interface IProductHistoryService
{
    /// <summary>
    /// Gets all completed products collected for the specified weld task and station.
    /// </summary>
    ProductHistorySnapshot GetSnapshot(int taskId, int stationNo);

    /// <summary>
    /// Marks or unmarks one completed product as a test weld part.
    /// The operation updates all weld point rows under the same product.
    /// </summary>
    ProductHistoryMarkResult SetProductTestFlag(int taskId, int stationNo, string productNo, bool isTest);

    /// <summary>
    /// 预约重焊/重测：下一次采集覆盖该产品。同任务同工位只保留一个预约目标。
    /// </summary>
    ProductHistoryMarkResult MarkReweld(int taskId, int stationNo, string productNo);

    ProductHistoryMarkResult CancelReweld(int taskId, int stationNo, string productNo);

    /// <summary>
    /// 软删产品：不再上传、不进报表与产量，已入队的过程参数上传任务置为跳过。
    /// </summary>
    ProductHistoryMarkResult DeleteProduct(int taskId, int stationNo, string productNo);

    ProductHistoryMarkResult RestoreProduct(int taskId, int stationNo, string productNo);
}
