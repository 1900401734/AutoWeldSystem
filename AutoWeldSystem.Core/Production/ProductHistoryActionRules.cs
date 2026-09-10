using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品历史右键动作。
/// </summary>
public enum ProductHistoryAction
{
    MarkTest,
    UnmarkTest,
    Reweld,
    CancelReweld,
    Delete,
    Restore
}

/// <summary>
/// 产品历史右键菜单与门禁规则。
/// 门禁统一按“产品未上传”判定：可操作范围随上传模式自然变化（Batch 模式整个任务、Quantity 模式约一两件），
/// 已上传产品在 MES 侧无法撤回，不给出看起来能删其实删不掉的按钮。
/// 重焊/删除只在程序计数模式开放，PLC 计数模式下这两个动作会让本地产量与 PLC 计数器对不上。
/// </summary>
public static class ProductHistoryActionRules
{
    public const string DisabledReasonUploaded = "产品已上传、上传中或已跳过，不能修改。";

    /// <summary>
    /// 产品下每条焊点记录的上传状态都必须仍可改写。
    /// </summary>
    public static bool CanOperate(IEnumerable<BizWeldPointRecord> records, out string disabledReason)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.All(record => IsOperableUploadStatus(record.UploadStatus)))
        {
            disabledReason = string.Empty;
            return true;
        }

        disabledReason = DisabledReasonUploaded;
        return false;
    }

    public static bool IsOperableUploadStatus(string? status)
    {
        return status is ProductionConstants.UploadStatuses.Pending
            or ProductionConstants.UploadStatuses.Failed
            or ProductionConstants.UploadStatuses.Retrying;
    }

    /// <summary>
    /// 计算右键菜单应提供的动作。已删除产品只有撤销；待重焊产品用“取消重焊”替代“重焊”。
    /// </summary>
    public static IReadOnlyList<ProductHistoryAction> ResolveActions(
        bool isDeleted,
        bool isReweldPending,
        bool isTest,
        bool showTestFlag,
        bool isProgramCountMode)
    {
        if (isDeleted)
        {
            return isProgramCountMode
                ? new[] { ProductHistoryAction.Restore }
                : Array.Empty<ProductHistoryAction>();
        }

        var actions = new List<ProductHistoryAction>();
        if (showTestFlag)
        {
            actions.Add(isTest ? ProductHistoryAction.UnmarkTest : ProductHistoryAction.MarkTest);
        }

        if (isProgramCountMode)
        {
            actions.Add(isReweldPending ? ProductHistoryAction.CancelReweld : ProductHistoryAction.Reweld);
            actions.Add(ProductHistoryAction.Delete);
        }

        return actions;
    }

    /// <summary>
    /// 整件检测叫“重测”，点焊设备叫“重焊”，语义一致但现场用语不同。
    /// </summary>
    public static bool UsesRetestWording(string? processParameterDeviceType)
        => string.Equals(
            processParameterDeviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 撤销删除本身不受上传状态门禁限制：能被删除的产品必然未上传，删除后也不会被上传。
    /// </summary>
    public static bool RequiresOperableGate(ProductHistoryAction action)
        => action is not ProductHistoryAction.Restore and not ProductHistoryAction.CancelReweld;
}
