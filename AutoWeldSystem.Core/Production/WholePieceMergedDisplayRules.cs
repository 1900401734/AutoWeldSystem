using System.Collections.Generic;
using System.Linq;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 整件检测四面合并显示规则。把 A/B 两行聚合结果转置成界面一行，
/// 使监控界面与 MES 上传、报表使用同一组数据。
/// </summary>
public static class WholePieceMergedDisplayRules
{
    public const string SideASuffix = "A";
    public const string SideBSuffix = "B";

    /// <summary>
    /// 生成合并显示列名。高度、宽度取四面最大值只占一列，其余测试项按 A/B 配对分列。
    /// 判定失败项要标到具体列上，因此列名规则必须只有这一处。
    /// </summary>
    public static string BuildColumnName(string? itemName, string? sideNo)
    {
        var normalizedName = itemName?.Trim() ?? string.Empty;
        return WholePieceAbAggregationRules.IsProductMaximumItem(normalizedName)
            ? normalizedName
            : normalizedName + (sideNo?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// 生成合并显示列。高度、宽度取四面最大值，只占一列；其余测试项按 A/B 配对，占两列。
    /// </summary>
    public static IReadOnlyList<WholePieceMergedColumn> BuildColumns(
        IEnumerable<WholePieceAbValueDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var columns = new List<WholePieceMergedColumn>();
        foreach (var definition in definitions)
        {
            var itemName = definition.ItemName?.Trim() ?? string.Empty;
            if (WholePieceAbAggregationRules.IsProductMaximumItem(itemName))
            {
                columns.Add(new WholePieceMergedColumn(
                    BuildColumnName(itemName, string.Empty),
                    definition.OutputKey,
                    string.Empty,
                    itemName));
                continue;
            }

            columns.Add(new WholePieceMergedColumn(
                BuildColumnName(itemName, SideASuffix),
                definition.OutputKey,
                SideASuffix,
                itemName));
            columns.Add(new WholePieceMergedColumn(
                BuildColumnName(itemName, SideBSuffix),
                definition.OutputKey,
                SideBSuffix,
                itemName));
        }

        return columns;
    }

    /// <summary>
    /// 按合并列取值。四面最大值列 A/B 两行数值相同，取任一行即可。
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildValues(
        IEnumerable<WholePieceMergedColumn> columns,
        IReadOnlyList<WholePieceAbOutputRow> abRows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(abRows);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            var row = string.IsNullOrEmpty(column.SideNo)
                ? abRows.FirstOrDefault()
                : abRows.FirstOrDefault(item => string.Equals(item.SideNo, column.SideNo, StringComparison.OrdinalIgnoreCase));
            values[column.ColumnName] = row is not null && row.Values.TryGetValue(column.OutputKey, out var value)
                ? value
                : string.Empty;
        }

        return values;
    }
}

/// <summary>
/// 合并显示列。SideNo 为空表示四面最大值的单列，A/B 表示配对列。
/// </summary>
public sealed record WholePieceMergedColumn(
    string ColumnName,
    string OutputKey,
    string SideNo,
    string ItemName);
