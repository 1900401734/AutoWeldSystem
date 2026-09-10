
namespace AutoWeldSystem.UI.ViewModels;

/// <summary>
/// Row model used by AntdUI.Table for the product history tree.
/// </summary>
public sealed class ProductHistoryTableRow
{
    public bool IsProductRow { get; init; }

    public int TaskId { get; init; }

    public int StationNo { get; init; }

    public string ProductNo { get; init; } = string.Empty;

    public string TouchNo { get; init; } = string.Empty;

    public string NodeText { get; init; } = string.Empty;

    public string ResultText { get; init; } = string.Empty;

    public bool ShowTestFlag { get; init; } = true;

    public string UploadStatusText { get; init; } = string.Empty;

    public bool IsTest { get; init; }

    public string IsTestText { get; init; } = string.Empty;

    /// <summary>
    /// 产品已软删：行仍显示以便右键撤销，但只提供“撤销删除”。
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// 产品已预约重焊/重测，下一件采集覆盖它。
    /// </summary>
    public bool IsReweldPending { get; init; }

    public string TouchCountText { get; init; } = string.Empty;

    public string RecordTimeText { get; init; } = string.Empty;

    public Dictionary<string, string> DynamicValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool CanMarkTest { get; init; }

    public string MarkDisabledReason { get; init; } = string.Empty;

    /// <summary>
    /// 重焊/删除是否可操作，与试焊件共用“未上传”门禁。
    /// </summary>
    public bool CanOperate { get; init; }

    public string OperateDisabledReason { get; init; } = string.Empty;

    public List<ProductHistoryTableRow> Children { get; init; } = [];
}

