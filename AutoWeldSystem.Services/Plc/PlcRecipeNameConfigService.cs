using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Data;
using SqlSugar;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 配方名称地址配置持久化服务。
/// 配置量很小，整体替换可让工位删除和单/双工位切换保持一致。
/// </summary>
public sealed class PlcRecipeNameConfigService(SqlSugarDbContext dbContext) : IPlcRecipeNameConfigService
{
    private readonly object _dbLock = new();

    public IReadOnlyList<BizPlcRecipeNameConfig> GetAll()
    {
        lock (_dbLock)
        {
            dbContext.InitDatabase();
            return dbContext.Db.Queryable<BizPlcRecipeNameConfig>()
                .OrderBy(config => config.StationNo)
                .ToList();
        }
    }

    public BizPlcRecipeNameConfig? GetForStation(int stationNo)
    {
        if (stationNo <= 0)
        {
            return null;
        }

        lock (_dbLock)
        {
            dbContext.InitDatabase();
            return dbContext.Db.Queryable<BizPlcRecipeNameConfig>()
                .Where(config => config.StationNo == stationNo)
                .OrderBy(config => config.UpdatedTime, OrderByType.Desc)
                .OrderBy(config => config.Id, OrderByType.Desc)
                .First();
        }
    }

    public void SaveAll(IEnumerable<BizPlcRecipeNameConfig> configs)
    {
        var normalized = PlcRecipeNameConfigRules.NormalizeAndValidate(configs, DateTime.Now);
        lock (_dbLock)
        {
            dbContext.InitDatabase();
            var transaction = dbContext.Db.Ado.UseTran(() =>
            {
                dbContext.Db.Deleteable<BizPlcRecipeNameConfig>().ExecuteCommand();
                if (normalized.Count > 0)
                {
                    dbContext.Db.Insertable(normalized.ToList()).ExecuteCommand();
                }
            });

            if (!transaction.IsSuccess)
            {
                throw new InvalidOperationException("保存 PLC 配方名称配置失败。", transaction.ErrorException);
            }
        }
    }
}
