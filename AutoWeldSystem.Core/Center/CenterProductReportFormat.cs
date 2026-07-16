using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.Core.Center;

/// <summary>
/// 生产报表共享格式定义。
/// 设备端与中心服务器共同使用客户模板表头、列宽和明细列协议，避免两套硬编码漂移。
/// </summary>
public static class CenterProductReportFormat
{
    public const string WorksheetName = "生产报表";
    public const int TemplateMinimumColumnCount = 10;
    public const int DetailHeaderRow = 9;
    public const int DetailFirstDataRow = DetailHeaderRow + 1;
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

    private static readonly IReadOnlyDictionary<int, double> TemplateColumnWidths = new Dictionary<int, double>
    {
        [1] = 5.8867d,
        [2] = 10.2188d,
        [3] = 10.4414d,
        [4] = 10.7773d,
        [5] = 9.8867d,
        [6] = 9d,
        [7] = 11d,
        [8] = 11d,
        [9] = 9.4414d,
        [10] = 4d
    };

    /// <summary>
    /// 根据动态字段名生成默认列定义。
    /// 仅在设备端没有传递列定义时使用。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> BuildColumns(IEnumerable<string> dynamicKeys)
    {
        var dynamicColumns = dynamicKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => new CenterProductReportColumn(key.Trim(), key.Trim(), MergeByProduct: false));

        return BuildDetailColumns(dynamicColumns);
    }

    /// <summary>
    /// 按设备端列定义生成最终报表列，并自动补齐固定前置列和尾部列。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> BuildColumns(IEnumerable<CenterProductReportColumn> equipmentColumns)
    {
        return BuildDetailColumns(equipmentColumns);
    }

    /// <summary>
    /// 生成客户模板第九行使用的明细列。
    /// 单工位由设备端省略工位列；固定列缺失时自动补齐，动态列保持设备端 SaveEnable 顺序。
    /// </summary>
    public static IReadOnlyList<CenterProductReportColumn> BuildDetailColumns(
        IEnumerable<CenterProductReportColumn> equipmentColumns)
    {
        var equipmentColumnList = equipmentColumns
            .Where(column => !string.IsNullOrWhiteSpace(column.Key))
            .DistinctBy(column => column.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var includeStation = equipmentColumnList.Any(
            column => string.Equals(column.Key, ColumnStationNo, StringComparison.OrdinalIgnoreCase));
        var columns = new List<CenterProductReportColumn>();

        if (includeStation)
        {
            columns.Add(ResolveDetailColumn(ColumnStationNo, "工位", mergeByProduct: true, equipmentColumnList));
        }

        columns.Add(ResolveDetailColumn(ColumnProductNo, "产品编号", mergeByProduct: true, equipmentColumnList));
        columns.Add(ResolveDetailColumn(ColumnTouchNo, "焊点编号", mergeByProduct: false, equipmentColumnList));
        columns.Add(ResolveDetailColumn(ColumnTouchResult, "焊点结果", mergeByProduct: false, equipmentColumnList));

        var fixedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ColumnStationNo,
            ColumnProductNo,
            ColumnTouchNo,
            ColumnTouchResult,
            ColumnProductResult
        };
        columns.AddRange(equipmentColumnList.Where(column => !fixedKeys.Contains(column.Key)));
        columns.Add(ResolveDetailColumn(ColumnProductResult, "产品结果", mergeByProduct: true, equipmentColumnList));
        return columns;
    }

    /// <summary>
    /// 按客户模板定义生成四行任务表头合并块；最后一组会扩展到实际末列。
    /// </summary>
    public static IReadOnlyList<CenterProductReportHeaderBlock> BuildTemplateHeaderBlocks(
        CenterProductReportHeaderValues values,
        int lastColumn)
    {
        var normalizedLastColumn = Math.Max(TemplateMinimumColumnCount, lastColumn);
        return
        [
            new(1, 1, 3, "产品工号：", values.ProductJobNo),
            new(1, 4, 6, "图号：", values.DrawingNo),
            new(1, 7, 8, "批次：", values.Batch),
            new(1, 9, normalizedLastColumn, "流转卡号：", values.WorkOrder),
            new(3, 1, 3, "部件规格：", values.Spec),
            new(3, 4, 6, "型号：", values.ProductModel),
            new(3, 7, normalizedLastColumn, "工序：", values.ProcessNo),
            new(5, 1, 3, "生产数量：", values.Quantity),
            new(5, 4, 6, "合格数量：", values.QualifiedQty),
            new(5, 7, normalizedLastColumn, "备注：", null),
            new(7, 1, 3, "开始时间：", values.StartTime),
            new(7, 4, 6, "结束时间：", values.EndTime),
            new(7, 7, normalizedLastColumn, "操作人员：", values.OperatorNo)
        ];
    }

    /// <summary>
    /// 读取客户 A:J 原始列宽；动态扩展列使用至少 12 的可读宽度。
    /// </summary>
    public static double ResolveTemplateColumnWidth(int columnIndex, double currentWidth)
    {
        return TemplateColumnWidths.TryGetValue(columnIndex, out var width)
            ? width
            : Math.Max(currentWidth, 12d);
    }

    /// <summary>
    /// 将客户模板标签和值拼成单行文本。
    /// </summary>
    public static string BuildHeaderText(string label, object? value)
    {
        var valueText = value switch
        {
            DateTime dateTime => dateTime.ToString(DateTimeFormat),
            _ => value?.ToString()?.Trim() ?? string.Empty
        };
        return string.Concat(label, valueText);
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

    private static CenterProductReportColumn ResolveDetailColumn(
        string key,
        string title,
        bool mergeByProduct,
        IReadOnlyList<CenterProductReportColumn> equipmentColumns)
    {
        var equipmentColumn = equipmentColumns.FirstOrDefault(
            column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase));
        return new CenterProductReportColumn(
            key,
            string.IsNullOrWhiteSpace(equipmentColumn?.Title) ? title : equipmentColumn.Title.Trim(),
            mergeByProduct);
    }

}

/// <summary>
/// 中心服务器 Excel 报表列定义。
/// </summary>
public sealed record CenterProductReportColumn(string Key, string Title, bool MergeByProduct);

/// <summary>
/// 客户模板任务级表头值。
/// </summary>
public sealed record CenterProductReportHeaderValues(
    string ProductJobNo,
    string DrawingNo,
    string Batch,
    string WorkOrder,
    string Spec,
    string ProductModel,
    string ProcessNo,
    int Quantity,
    int QualifiedQty,
    DateTime StartTime,
    DateTime? EndTime,
    string OperatorNo);

/// <summary>
/// 客户模板中的一个合并表头块。
/// </summary>
public sealed record CenterProductReportHeaderBlock(
    int Row,
    int StartColumn,
    int EndColumn,
    string Label,
    object? Value);
