using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 程序计数模式下的任务产量统计。
/// 完工上报与监控页指标都从这里取数，保证屏幕显示与 MES 收到的三项数量同源。
/// </summary>
public sealed class ProductionCountService : IProductionCountService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public ProductionCountService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public FinishQuantities GetTaskQuantities(int taskId)
    {
        if (taskId <= 0)
        {
            return FinishQuantities.Empty;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == taskId)
                .ToList();
            return FinishQuantityRules.Calculate(records);
        }
    }
}
