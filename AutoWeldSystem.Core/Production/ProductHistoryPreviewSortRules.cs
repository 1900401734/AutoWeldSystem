using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Provides display ordering rules for the MonitorView product history preview.
/// </summary>
public static class ProductHistoryPreviewSortRules
{
    /// <summary>
    /// Orders product summary rows so the newest completed product is shown first.
    /// </summary>
    public static IReadOnlyList<ProductHistoryProduct> OrderProductsLatestFirst(IEnumerable<ProductHistoryProduct> products)
    {
        return products
            .OrderByDescending(product => product.LastRecordTime ?? DateTime.MinValue)
            .ThenByDescending(product => product.ProductNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
