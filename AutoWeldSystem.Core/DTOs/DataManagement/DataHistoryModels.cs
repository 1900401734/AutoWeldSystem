using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.DataManagement;

/// <summary>
/// Filter conditions used to query local work-order history.
/// </summary>
public sealed class DataHistoryQueryCriteria
{
    /// <summary>
    /// 查询条件：产品工号
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 查询条件：批次号
    /// </summary>
    public string Batch { get; init; } = string.Empty;

    /// <summary>
    /// 查询条件：工单号/流转卡号
    /// </summary>
    public string SN { get; init; } = string.Empty;

    /// <summary>
    /// 查询条件：日期范围 - 开始时间
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// 查询条件：日期范围 - 结束时间
    /// </summary>
    public DateTime EndTime { get; init; }
}

/// <summary>
/// One work-order row displayed in the data-management master grid.
/// </summary>
public sealed class DataHistoryWorkOrderRow
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public int TaskId { get; init; }

    /// <summary>
    /// 工位号
    /// </summary>
    public int StationNo { get; init; }

    /// <summary>
    /// 工单号/流转卡号
    /// </summary>
    public string WorkOrderId { get; init; } = string.Empty;

    /// <summary>
    /// 产品编号
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    /// </summary>
    public string Batch { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public string ProcessDisplay { get; init; } = string.Empty;

    public string RecipeCode { get; init; } = string.Empty;

    public int PlannedQty { get; init; }

    public int ActualQty { get; init; }

    public int QualifiedQty { get; init; }

    public int FailedQty { get; init; }

    public string OperatorNumber { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public string TaskStatus { get; init; } = string.Empty;

    public string UploadStatus { get; init; } = string.Empty;
}

/// <summary>
/// Defines one runtime-generated weld-parameter column.
/// </summary>
public sealed class DataHistoryDynamicColumn
{
    public string Key { get; init; } = string.Empty;

    public string HeaderText { get; init; } = string.Empty;
}

/// <summary>
/// One product or test-record row displayed in the data-management tree.
/// </summary>
public sealed class DataHistoryTestDataRow
{
    public bool IsProductRow { get; init; }

    public int TaskId { get; init; }

    public int RecordId { get; init; }

    public int SequenceNo { get; init; }

    public int StationNo { get; init; }

    public string ProductNo { get; init; } = string.Empty;

    public string TouchNo { get; init; } = string.Empty;

    public string NodeText { get; init; } = string.Empty;

    public string TestResult { get; init; } = ProductionConstants.TestResults.Unknown;

    /// <summary>
    /// PLC product-level result. It must not be inferred from child record results.
    /// </summary>
    public string ProductResult { get; init; } = ProductionConstants.TestResults.Unknown;

    public string UploadStatus { get; init; } = string.Empty;

    public int TestCount { get; init; }

    public DateTime? RecordTime { get; init; }

    public IReadOnlyDictionary<string, string> DynamicValues { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string RawDataJson { get; init; } = string.Empty;

    public List<DataHistoryTestDataRow> Children { get; init; } = [];
}

/// <summary>
/// Product/test-record tree and the dynamic columns required to display it.
/// </summary>
public sealed class DataHistoryTestDataResult
{
    public IReadOnlyList<DataHistoryDynamicColumn> DynamicColumns { get; init; }
        = Array.Empty<DataHistoryDynamicColumn>();

    public IReadOnlyList<DataHistoryTestDataRow> Rows { get; init; }
        = Array.Empty<DataHistoryTestDataRow>();

    public int RecordCount { get; init; }
}

/// <summary>
/// One weld-point row with dynamic test values.
/// </summary>
public sealed class DataHistoryWeldParameterRow
{
    public int StationNo { get; init; }

    public string ProductNo { get; init; } = string.Empty;

    public string TouchNo { get; init; } = string.Empty;

    public string TestResult { get; init; } = string.Empty;

    /// <summary>
    /// PLC product-level result. It must not be inferred from weld-point results.
    /// </summary>
    public string ProductResult { get; init; } = ProductionConstants.TestResults.Unknown;

    public DateTime RecordTime { get; init; }

    public IReadOnlyDictionary<string, string> DynamicValues { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Weld-parameter rows and the dynamic columns required to display them.
/// </summary>
public sealed class DataHistoryWeldParameterResult
{
    public IReadOnlyList<DataHistoryDynamicColumn> DynamicColumns { get; init; }
        = Array.Empty<DataHistoryDynamicColumn>();

    public IReadOnlyList<DataHistoryWeldParameterRow> Rows { get; init; }
        = Array.Empty<DataHistoryWeldParameterRow>();
}

/// <summary>
/// One raw weld-point collection record.
/// </summary>
public sealed class DataHistoryCollectionRow
{
    public int Id { get; init; }

    public int SequenceNo { get; init; }

    public int StationNo { get; init; }

    public string ProductNo { get; init; } = string.Empty;

    public string TouchNo { get; init; } = string.Empty;

    public string TestResult { get; init; } = string.Empty;

    /// <summary>
    /// PLC product-level result. It must not be inferred from weld-point results.
    /// </summary>
    public string ProductResult { get; init; } = ProductionConstants.TestResults.Unknown;

    public bool IsTest { get; init; }

    public bool ProductCompleted { get; init; }

    public string UploadStatus { get; init; } = string.Empty;

    public string OperatorNo { get; init; } = string.Empty;

    public DateTime RecordTime { get; init; }

    public string RawDataJson { get; init; } = string.Empty;
}

/// <summary>
/// One locally generated production report file.
/// </summary>
public sealed class DataHistoryReportFileRow
{
    public int Id { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string FileFormat { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string UploadStatus { get; init; } = string.Empty;

    public DateTime CreatedTime { get; init; }

    public DateTime UpdatedTime { get; init; }
}
