using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// Product history preview data for one weld task and one station.
/// The UI binds product rows as tree parents and weld point rows as children.
/// </summary>
public sealed class ProductHistorySnapshot
{
    public int TaskId { get; init; }

    public int StationNo { get; init; }

    public IReadOnlyList<ProductHistoryProduct> Products { get; init; } = [];
}

/// <summary>
/// Product-level summary shown as a parent row in the MonitorView history table.
/// </summary>
public sealed class ProductHistoryProduct
{
    public int TaskId { get; init; }

    public int StationNo { get; init; }

    public string ProductNo { get; init; } = string.Empty;

    public string Result { get; init; } = ProductionConstants.TestResults.Unknown;

    public string UploadStatus { get; init; } = ProductionConstants.UploadStatuses.Pending;

    public bool IsTest { get; init; }

    public int TouchCount { get; init; }

    public DateTime? LastRecordTime { get; init; }

    public IReadOnlyList<ProductHistoryPoint> Points { get; init; } = [];

    public bool CanMarkTest { get; init; }

    public string MarkDisabledReason { get; init; } = string.Empty;
}

/// <summary>
/// Weld point detail shown as a child row below one product.
/// </summary>
public sealed class ProductHistoryPoint
{
    public int Id { get; init; }

    public int SequenceNo { get; init; }

    public string TouchNo { get; init; } = string.Empty;

    public string Result { get; init; } = ProductionConstants.TestResults.Unknown;

    public string UploadStatus { get; init; } = ProductionConstants.UploadStatuses.Pending;

    public bool IsTest { get; init; }

    public DateTime RecordTime { get; init; }

    public string MaxElectric { get; init; } = string.Empty;

    public string MaxVoltage { get; init; } = string.Empty;

    public string ValidPower { get; init; } = string.Empty;

    public string Displacement { get; init; } = string.Empty;

    public string WeldTs { get; init; } = string.Empty;

    /// <summary>
    /// Raw collected values for this weld point.
    /// MonitorView uses this JSON to build history columns from the active test scheme.
    /// </summary>
    public string RawDataJson { get; init; } = string.Empty;
}

/// <summary>
/// Result returned after the operator marks or unmarks a product as a test weld part.
/// </summary>
public sealed class ProductHistoryMarkResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public ProductHistoryProduct? Product { get; init; }

    public static ProductHistoryMarkResult Success(ProductHistoryProduct product, string message)
        => new() { IsSuccess = true, Product = product, Message = message };

    public static ProductHistoryMarkResult Failed(string message)
        => new() { IsSuccess = false, Message = message };
}
