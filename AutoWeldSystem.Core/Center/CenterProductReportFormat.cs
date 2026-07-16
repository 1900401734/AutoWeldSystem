using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.Core.Center;

/// <summary>
/// 生产报表共享格式定义。
/// 设备端先使用模板行号和格式常量；中心服务器列协议保持现状，后续任务再接入模板布局。
/// </summary>
public static class CenterProductReportFormat
{
    public const string WorksheetName = "生产报表";
    public const int TemplateMinimumColumnCount = 9;
    public const int DetailHeaderRow = 9;
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string DataWorksheetName = "_Data";
    public const string ColumnsWorksheetName = "_Columns";
    public const string ColumnStationNo = "station_no";
    public const string ColumnProductNo = "product_no";
    public const string ColumnProductResult = "product_result";
    public const string ColumnTouchNo = "touch_no";
    public const string ColumnTouchResult = "touch_result";
    public const string ColumnWorkOrder = "work_order";
    public const string ColumnBatch = "batch";
    public const string ColumnQuantity = "quantity";
    public const string ColumnPartName = "part_name";
    public const string ColumnProcessNo = "process_no";
    public const string ColumnOperator = "operator";
    public const string ColumnRecordTime = "record_time";

    private static readonly CenterProductReportColumn[] LeadingColumns =
    [
        new(ColumnStationNo, "工位", MergeByProduct: true),
        new(ColumnProductNo, "产品编号", MergeByProduct: true),
        new(ColumnProductResult, "产品结果", MergeByProduct: true),
        new(ColumnTouchNo, "焊点编号", MergeByProduct: false),
        new(ColumnTouchResult, "焊点结果", MergeByProduct: false)
    ];

    private static readonly CenterProductReportColumn[] TrailingColumns =
    [
        new(ColumnWorkOrder, "工号", MergeByProduct: true),
        new(ColumnBatch, "批次", MergeByProduct: true),
        new(ColumnQuantity, "数量", MergeByProduct: true),
        new(ColumnPartName, "零部件名称", MergeByProduct: true),
        new(ColumnProcessNo, "工序号", MergeByProduct: true),
        new(ColumnOperator, "操作人员", MergeByProduct: true),
        new(ColumnRecordTime, "日期", MergeByProduct: true)
    ];

    /// <summary>
    /// 根据动态字段名生成默认列定义。
    /// 仅在设备端没有传递列定义时使用。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> BuildColumns(IEnumerable<string> dynamicKeys)
    {
        var dynamicColumns = dynamicKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => new CenterProductReportColumn(key.Trim(), key.Trim(), MergeByProduct: false));

        return BuildColumns(dynamicColumns);
    }

    /// <summary>
    /// 按设备端列定义生成最终报表列，并自动补齐固定前置列和尾部列。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> BuildColumns(IEnumerable<CenterProductReportColumn> equipmentColumns)
    {
        var equipmentColumnList = equipmentColumns.ToList();
        var columns = LeadingColumns.Select(column => ApplyEquipmentOverride(column, equipmentColumnList)).ToList();
        var seen = new HashSet<string>(columns.Select(column => column.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var column in equipmentColumns)
        {
            var key = column.Key?.Trim();
            if (string.IsNullOrWhiteSpace(key) || IsTrailingColumn(key) || !seen.Add(key))
            {
                continue;
            }

            columns.Add(new CenterProductReportColumn(
                key,
                string.IsNullOrWhiteSpace(column.Title) ? key : column.Title.Trim(),
                column.MergeByProduct));
        }

        foreach (var column in TrailingColumns.Select(column => ApplyEquipmentOverride(column, equipmentColumnList)))
        {
            if (seen.Add(column.Key))
            {
                columns.Add(column);
            }
        }

        return columns;
    }

    /// <summary>
    /// 将设备端 DTO 转换为中心服务器内部列定义。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> FromDtos(IEnumerable<CenterProductReportColumnDto> columns)
    {
        return columns
            .Where(column => !string.IsNullOrWhiteSpace(column.Key))
            .Select(column => new CenterProductReportColumn(
                column.Key.Trim(),
                string.IsNullOrWhiteSpace(column.Title) ? column.Key.Trim() : column.Title.Trim(),
                column.MergeByProduct))
            .ToList();
    }

    /// <summary>
    /// 构造同一产品内需要合并单元格的分组键。
    /// </summary>
    public static string BuildProductMergeKey(int stationNo, string? workOrder, string? productNo)
    {
        return $"{stationNo}\u001F{workOrder?.Trim()}\u001F{productNo?.Trim()}";
    }

    private static bool IsTrailingColumn(string key)
    {
        return TrailingColumns.Any(column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static CenterProductReportColumn ApplyEquipmentOverride(
        CenterProductReportColumn defaultColumn,
        IReadOnlyList<CenterProductReportColumn> equipmentColumns)
    {
        var overrideColumn = equipmentColumns.FirstOrDefault(
            column => string.Equals(column.Key, defaultColumn.Key, StringComparison.OrdinalIgnoreCase));
        if (overrideColumn is null)
        {
            return defaultColumn;
        }

        return new CenterProductReportColumn(
            defaultColumn.Key,
            string.IsNullOrWhiteSpace(overrideColumn.Title) ? defaultColumn.Title : overrideColumn.Title.Trim(),
            defaultColumn.MergeByProduct);
    }
}

/// <summary>
/// 中心服务器 Excel 报表列定义。
/// </summary>
public sealed record CenterProductReportColumn(string Key, string Title, bool MergeByProduct);
