using System.Globalization;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DataManagement;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Applies the in-memory product-level filter and sort used by DataManageView.
/// </summary>
public static class DataHistoryTestDataRules
{
    public const string AllResults = "";

    /// <summary>
    /// 默认每页显示的产品行数量。
    /// </summary>
    public const int DefaultPageSize = 20;

    public static IReadOnlyList<DataHistoryTestDataRow> Apply(
        IEnumerable<DataHistoryTestDataRow> rows,
        string? productResult,
        string? sortColumnKey,
        bool descending)
    {
        var filtered = rows
            .Where(row => row.IsProductRow && MatchesProductResult(row.ProductResult, productResult))
            .ToList();

        if (string.IsNullOrWhiteSpace(sortColumnKey))
        {
            return filtered;
        }

        return filtered
            .Select((row, index) => (row, index))
            .OrderBy(item => GetValuePriority(item.row, sortColumnKey))
            .ThenBy(item => GetSortValue(item.row, sortColumnKey),
                new DynamicValueComparer(descending))
            .ThenBy(item => item.index)
            .Select(item => item.row)
            .ToList();
    }

    /// <summary>
    /// 取筛选、排序后产品行的一页。
    /// 单个工单可能有上百个产品，界面按产品行分页显示，产品下的测试记录始终跟随所属产品行。
    /// </summary>
    /// <param name="rows">已筛选、排序的产品行。</param>
    /// <param name="requestedPageIndex">请求的页码，小于 1 或越界时会被夹到有效范围。</param>
    /// <param name="requestedPageSize">请求的每页数量，非正数时回退为默认值。</param>
    /// <returns>当前页的产品行以及回写分页控件所需的页码、每页数量和总数。</returns>
    public static PagedResult<DataHistoryTestDataRow> GetPage(
        IReadOnlyList<DataHistoryTestDataRow> rows,
        int requestedPageIndex,
        int requestedPageSize)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var pageSize = requestedPageSize > 0 ? requestedPageSize : DefaultPageSize;
        var pageTotal = Math.Max(1, (rows.Count + pageSize - 1) / pageSize);
        var pageIndex = Math.Clamp(requestedPageIndex < 1 ? 1 : requestedPageIndex, 1, pageTotal);

        return new PagedResult<DataHistoryTestDataRow>
        {
            Items = rows.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = rows.Count,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    private static bool MatchesProductResult(string? productResult, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return string.Equals(filter, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase)
            ? TestResultRules.IsFailed(productResult)
            : string.Equals(filter, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
                ? TestResultRules.IsOk(productResult)
                : string.Equals(
                    TestResultRules.Normalize(productResult),
                    TestResultRules.Normalize(filter),
                    StringComparison.OrdinalIgnoreCase);
    }

    private static int GetValuePriority(DataHistoryTestDataRow row, string columnKey)
    {
        return IsBlank(GetSortValue(row, columnKey)) ? 1 : 0;
    }

    private static string GetSortValue(DataHistoryTestDataRow row, string columnKey)
    {
        if (row.DynamicValues.TryGetValue(columnKey, out var value) && !IsBlank(value ?? string.Empty))
        {
            return value?.Trim() ?? string.Empty;
        }

        return row.Children
            .Select(child => child.DynamicValues.TryGetValue(columnKey, out var childValue) ? childValue?.Trim() ?? string.Empty : string.Empty)
            .FirstOrDefault(value => !IsBlank(value)) ?? string.Empty;
    }

    private static bool IsBlank(string value)
        => string.IsNullOrWhiteSpace(value)
            || string.Equals(value, ProductionConstants.TestResults.NotAvailable, StringComparison.Ordinal);

    private sealed class DynamicValueComparer(bool descending) : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var left = x?.Trim() ?? string.Empty;
            var right = y?.Trim() ?? string.Empty;
            var leftNumber = decimal.TryParse(left, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var leftValue);
            var rightNumber = decimal.TryParse(right, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var rightValue);

            var result = leftNumber && rightNumber
                ? leftValue.CompareTo(rightValue)
                : StringComparer.CurrentCultureIgnoreCase.Compare(left, right);
            return descending ? -result : result;
        }
    }
}
