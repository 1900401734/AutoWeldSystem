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
    public const int DetailHeaderRow = 11;
    public const int DetailFirstDataRow = DetailHeaderRow + 1;
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string DataWorksheetName = "_Data";
    public const string ColumnsWorksheetName = "_Columns";
    public const string TaskWorksheetName = "_Task";
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
        [1] = 6.6d,
        [2] = 10.9333333333333d,
        [3] = 11.152380952381d,
        [4] = 11.4857142857143d,
        [5] = 10.6d,
        [6] = 9.71428571428571d,
        [7] = 11.7142857142857d,
        [8] = 11d,
        [9] = 10.152380952381d,
        [10] = 4.71428571428571d
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
    /// 生成客户模板第十一行使用的明细列。
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
        columns.Add(ResolveDetailColumn(ColumnTouchNo, ResolveDefaultPointNoTitle(equipmentColumnList), mergeByProduct: false, equipmentColumnList));

        var fixedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ColumnStationNo,
            ColumnProductNo,
            ColumnTouchNo,
            ColumnTouchResult,
            ColumnProductResult
        };
        columns.AddRange(equipmentColumnList.Where(column => !fixedKeys.Contains(column.Key)));
        columns.Add(ResolveDetailColumn(ColumnTouchResult, ResolveDefaultPointResultTitle(equipmentColumnList), mergeByProduct: false, equipmentColumnList));
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
            new(1, 1, 3, "流转卡号：", values.WorkOrder),
            new(1, 4, 6, "规格：", values.Spec),
            new(1, 7, normalizedLastColumn, "产品型号：", values.ProductModel),
            new(3, 1, 3, "产品工号：", values.ProductJobNo),
            new(3, 4, 6, "批次：", values.Batch),
            new(3, 7, normalizedLastColumn, "部件名称：", values.PartName),
            new(5, 1, 3, "部件图号：", values.DrawingNo),
            new(5, 4, 6, "工序名称：", values.ProcessName),
            new(5, 7, normalizedLastColumn, "工序号：", values.ProcessNo),
            new(7, 1, 3, "工单数量：", values.Quantity),
            new(7, 4, 6, "合格数量：", values.QualifiedQty),
            new(7, 7, normalizedLastColumn, "操作人员：", values.OperatorNo),
            new(9, 1, 3, "开始时间：", values.StartTime),
            new(9, 4, 6, "结束时间：", values.EndTime)
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
    /// 同一中心报表的采集点固定列必须保持同一标题，避免后到工位被已有标题错误解释。
    /// </summary>
    public static void EnsureCompatiblePointHeaders(
        IEnumerable<CenterProductReportColumn> existingColumns,
        IEnumerable<CenterProductReportColumn> incomingColumns)
    {
        var existing = existingColumns.ToList();
        var incoming = incomingColumns.ToList();
        foreach (var key in new[] { ColumnTouchNo, ColumnTouchResult })
        {
            var existingTitle = FindColumnTitle(existing, key);
            var incomingTitle = FindColumnTitle(incoming, key);
            if (!string.IsNullOrWhiteSpace(existingTitle)
                && !string.IsNullOrWhiteSpace(incomingTitle)
                && !string.Equals(existingTitle, incomingTitle, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"同一中心报表的采集点表头不一致：{existingTitle} / {incomingTitle}。");
            }
        }
    }

    /// <summary>
    /// 构造同一产品内需要合并单元格的分组键。
    /// </summary>
    public static string BuildProductMergeKey(int stationNo, string? workOrder, string? productNo)
    {
        return $"{stationNo}\u001F{workOrder?.Trim()}\u001F{productNo?.Trim()}";
    }

    public static string ResolvePointNoTitle(string? configuredTitle, bool wholePieceInspection)
    {
        var title = configuredTitle?.Trim();
        if (!string.IsNullOrWhiteSpace(title)
            && (!wholePieceInspection || !IsLegacyWeldPointTitle(title)))
        {
            return title;
        }

        return wholePieceInspection ? "检测面" : "焊点编号";
    }

    public static string ResolvePointResultTitle(string? configuredTitle, bool wholePieceInspection)
    {
        var title = configuredTitle?.Trim();
        if (!string.IsNullOrWhiteSpace(title)
            && (!wholePieceInspection || !IsLegacyWeldResultTitle(title)))
        {
            return title;
        }

        return wholePieceInspection ? "检测结果" : "焊点结果";
    }

    private static string ResolveDefaultPointNoTitle(IReadOnlyList<CenterProductReportColumn> equipmentColumns)
        => ResolvePointNoTitle(FindColumnTitle(equipmentColumns, ColumnTouchNo), wholePieceInspection: false);

    private static string ResolveDefaultPointResultTitle(IReadOnlyList<CenterProductReportColumn> equipmentColumns)
        => ResolvePointResultTitle(FindColumnTitle(equipmentColumns, ColumnTouchResult), wholePieceInspection: false);

    private static bool IsLegacyWeldPointTitle(string title)
        => title is "焊点序号" or "焊点编号" or "焊点号";

    private static bool IsLegacyWeldResultTitle(string title)
        => title is "焊点结果";

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

    private static string? FindColumnTitle(
        IReadOnlyList<CenterProductReportColumn> columns,
        string key)
    {
        return columns.FirstOrDefault(
            column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase))?.Title?.Trim();
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
    string PartName,
    string ProcessName,
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
