
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

    public string UploadStatusText { get; init; } = string.Empty;

    public bool IsTest { get; init; }

    public string IsTestText { get; init; } = string.Empty;

    public string TouchCountText { get; init; } = string.Empty;

    public string RecordTimeText { get; init; } = string.Empty;

    public Dictionary<string, string> DynamicValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool CanMarkTest { get; init; }

    public string MarkDisabledReason { get; init; } = string.Empty;

    public List<ProductHistoryTableRow> Children { get; init; } = [];
}

