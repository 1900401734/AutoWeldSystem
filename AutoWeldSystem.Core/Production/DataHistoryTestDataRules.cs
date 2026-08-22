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
