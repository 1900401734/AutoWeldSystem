using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// ProductNo 生成服务实现。
/// 使用数据库中的产品实例作为占号记录，避免双工位同时开始时分配重复编号。
/// </summary>
public class ProductNoGeneratorService : IProductNoGeneratorService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _sequenceLock = new();

    public ProductNoGeneratorService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public BizProductInstance GetOrCreateStationProduct(BizWeldTask task, int stationNo, int requiredTouchCount)
    {
        if (task.Id <= 0)
        {
            throw new InvalidOperationException("工单任务尚未保存，无法生成 ProductNo。");
        }

        lock (_sequenceLock)
        {
            _dbContext.InitDatabase();

            var activeProduct = _dbContext.Db.Queryable<BizProductInstance>()
                .First(it => it.TaskId == task.Id
                    && it.StationNo == stationNo
                    && it.ProductStatus == ProductionConstants.ProductInstanceStatuses.Running);
            if (activeProduct is not null)
            {
                return activeProduct;
            }

            var nextNumber = GetNextNumberCore(task.Id);
            var product = new BizProductInstance
            {
                TaskId = task.Id,
                ExpStartId = task.ExpStartId,
                DeviceId = task.DeviceId,
                WorkOrderId = task.WorkOrderId,
                ProcessNo = task.ProcessNo,
                ProductNo = nextNumber.ToString(),
                StationNo = Math.Max(1, stationNo),
                RequiredTouchCount = Math.Max(1, requiredTouchCount),
                ProductStatus = ProductionConstants.ProductInstanceStatuses.Running,
                StartTime = DateTime.Now,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };

            return _dbContext.Db.Insertable(product).ExecuteReturnEntity();
        }
    }

    public BizProductInstance UpdateProgress(int productInstanceId, int collectedTouchCount, string? testResult = null)
    {
        lock (_sequenceLock)
        {
            _dbContext.InitDatabase();
            var product = _dbContext.Db.Queryable<BizProductInstance>().InSingle(productInstanceId)
                ?? throw new InvalidOperationException("产品实例不存在。");

            product.CollectedTouchCount = Math.Max(product.CollectedTouchCount, collectedTouchCount);
            product.TestResult = NormalizeTestResult(testResult, product.TestResult);
            product.UpdatedTime = DateTime.Now;

            if (product.CollectedTouchCount >= product.RequiredTouchCount)
            {
                product.ProductStatus = ProductionConstants.ProductInstanceStatuses.Completed;
                product.CompletedTime ??= DateTime.Now;
            }

            _dbContext.Db.Updateable(product).ExecuteCommand();
            return _dbContext.Db.Queryable<BizProductInstance>().InSingle(product.Id) ?? product;
        }
    }

    public int PeekNextNumber(int taskId)
    {
        lock (_sequenceLock)
        {
            _dbContext.InitDatabase();
            return GetNextNumberCore(taskId);
        }
    }

    private int GetNextNumberCore(int taskId)
    {
        var productNos = _dbContext.Db.Queryable<BizProductInstance>()
            .Where(it => it.TaskId == taskId)
            .Select(it => it.ProductNo)
            .ToList();

        var maxNumber = 0;
        foreach (var productNo in productNos)
        {
            if (int.TryParse(productNo, out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }

        return maxNumber + 1;
    }

    private static string NormalizeTestResult(string? rawResult, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return fallback;
        }

        return string.Equals(rawResult.Trim(), ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(rawResult.Trim(), ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Ng;
    }
}
