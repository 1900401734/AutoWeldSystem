using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Provides product-level history for MonitorView.
/// The database keeps one row per weld point, so this service groups rows into product parents.
/// </summary>
public sealed class ProductHistoryService : IProductHistoryService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public ProductHistoryService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ProductHistorySnapshot GetSnapshot(int taskId, int stationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var records = GetTaskStationRecords(taskId, stationNo);

            return new ProductHistorySnapshot
            {
                TaskId = taskId,
                StationNo = stationNo,
                Products = BuildProducts(records)
            };
        }
    }

    public ProductHistoryMarkResult SetProductTestFlag(int taskId, int stationNo, string productNo, bool isTest)
    {
        var normalizedProductNo = productNo.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProductNo))
        {
            return ProductHistoryMarkResult.Failed("产品编号为空，无法标记试焊件。");
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var records = GetTaskStationRecords(taskId, stationNo)
                .Where(record => string.Equals(record.ProductNo, normalizedProductNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (records.Count == 0 || !records.Any(record => record.ProductCompleted))
            {
                return ProductHistoryMarkResult.Failed("未找到已完成采集的产品，无法标记试焊件。");
            }

            if (!IsProductMarkable(records, out var disabledReason))
            {
                return ProductHistoryMarkResult.Failed(disabledReason);
            }

            // Product-level marking must update all weld points under the same ProductNumber,
            // otherwise the process-parameter upload payload would contain mixed IsTest values.
            _dbContext.Db.Updateable<BizWeldPointRecord>()
                .SetColumns(record => record.IsTest == isTest)
                .Where(record => record.TaskId == taskId
                    && record.StationNo == stationNo
                    && record.ProductNo == normalizedProductNo)
                .ExecuteCommand();

            var updatedRecords = GetTaskStationRecords(taskId, stationNo)
                .Where(record => string.Equals(record.ProductNo, normalizedProductNo, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var product = BuildProduct(updatedRecords);
            var message = isTest
                ? $"产品 {normalizedProductNo} 已标记为试焊件。"
                : $"产品 {normalizedProductNo} 已取消试焊件标记。";

            return product is null
                ? ProductHistoryMarkResult.Failed("试焊件标记已保存，但刷新产品历史失败。")
                : ProductHistoryMarkResult.Success(product, message);
        }
    }

    private IReadOnlyList<BizWeldPointRecord> GetTaskStationRecords(int taskId, int stationNo)
    {
        return _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId && record.StationNo == stationNo)
            .ToList()
            .OrderBy(record => record.ProductNo)
            .ThenBy(record => record.SequenceNo)
            .ThenBy(record => record.Id)
            .ToList();
    }

    private static IReadOnlyList<ProductHistoryProduct> BuildProducts(IReadOnlyList<BizWeldPointRecord> records)
    {
        return records
            .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => record.ProductNo, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildProduct(group.ToList()))
            .Where(product => product is not null)
            .Cast<ProductHistoryProduct>()
            .OrderBy(product => product.LastRecordTime ?? DateTime.MinValue)
            .ThenBy(product => product.ProductNo)
            .ToList();
    }

    private static ProductHistoryProduct? BuildProduct(IReadOnlyList<BizWeldPointRecord> records)
    {
        if (records.Count == 0 || !records.Any(record => record.ProductCompleted))
        {
            return null;
        }

        var orderedRecords = records
            .OrderBy(record => record.SequenceNo)
            .ThenBy(record => record.Id)
            .ToList();
        var firstRecord = orderedRecords[0];

        return new ProductHistoryProduct
        {
            TaskId = firstRecord.TaskId,
            StationNo = firstRecord.StationNo,
            ProductNo = firstRecord.ProductNo,
            Result = ResolveProductResult(orderedRecords),
            UploadStatus = ResolveProductUploadStatus(orderedRecords),
            IsTest = orderedRecords.Any(record => record.IsTest),
            TouchCount = orderedRecords.Count,
            LastRecordTime = orderedRecords.Max(record => record.Ts),
            Points = orderedRecords.Select(ToPoint).ToList(),
            CanMarkTest = IsProductMarkable(orderedRecords, out var disabledReason),
            MarkDisabledReason = disabledReason
        };
    }

    private static ProductHistoryPoint ToPoint(BizWeldPointRecord record)
    {
        return new ProductHistoryPoint
        {
            Id = record.Id,
            SequenceNo = record.SequenceNo,
            TouchNo = record.TouchNo,
            Result = record.TestResult,
            UploadStatus = record.UploadStatus,
            IsTest = record.IsTest,
            RecordTime = record.Ts,
            RawDataJson = record.RawDataJson ?? string.Empty
        };
    }

    private static string ResolveProductResult(IReadOnlyList<BizWeldPointRecord> records)
        => TestResultRules.ResolveProductResult(records.Select(record => record.TestResult));

    private static string ResolveProductUploadStatus(IReadOnlyList<BizWeldPointRecord> records)
    {
        var statuses = records.Select(record => record.UploadStatus).ToList();
        if (statuses.All(status => status == ProductionConstants.UploadStatuses.Uploaded))
        {
            return ProductionConstants.UploadStatuses.Uploaded;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Uploading))
        {
            return ProductionConstants.UploadStatuses.Uploading;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Failed))
        {
            return ProductionConstants.UploadStatuses.Failed;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Retrying))
        {
            return ProductionConstants.UploadStatuses.Retrying;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Pending))
        {
            return ProductionConstants.UploadStatuses.Pending;
        }

        return statuses.FirstOrDefault() ?? ProductionConstants.UploadStatuses.Pending;
    }

    private static bool IsProductMarkable(IReadOnlyList<BizWeldPointRecord> records, out string disabledReason)
    {
        if (records.All(record => IsMarkableUploadStatus(record.UploadStatus)))
        {
            disabledReason = string.Empty;
            return true;
        }

        disabledReason = "产品已上传、上传中或已跳过，不能修改试焊件标记。";
        return false;
    }

    private static bool IsMarkableUploadStatus(string status)
    {
        return status is ProductionConstants.UploadStatuses.Pending
            or ProductionConstants.UploadStatuses.Failed
            or ProductionConstants.UploadStatuses.Retrying;
    }
}
