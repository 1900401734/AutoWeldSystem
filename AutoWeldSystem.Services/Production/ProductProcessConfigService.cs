using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 产品工艺配置服务实现。
/// 这层只处理配置持久化，不直接控制 PLC 或 MES。
/// </summary>
public class ProductProcessConfigService : IProductProcessConfigService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public ProductProcessConfigService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<BizProductProcessConfig> GetAll(bool includeDisabled = false)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();

            var query = _dbContext.Db.Queryable<BizProductProcessConfig>();
            if (!includeDisabled)
            {
                query = query.Where(it => it.Enabled);
            }

            return query.ToList()
                .OrderBy(it => it.Sort)
                .ThenBy(it => it.StationNo)
                .ThenBy(it => it.ProductNum)
                .ThenBy(it => it.ProductModel)
                .ToList();
        }
    }

    public BizProductProcessConfig? FindActive(
        string productNum,
        string productModel,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedProductNum = NormalizeNullable(productNum);
            var normalizedModel = NormalizeNullable(productModel);
            var normalizedStationNo = NormalizeStationNo(stationNo);

            var candidates = _dbContext.Db.Queryable<BizProductProcessConfig>()
                .Where(it => it.Enabled
                    && (it.StationNo == normalizedStationNo || it.StationNo == ProductionConstants.Stations.SharedStationNo))
                .ToList()
                .Where(config => IsProductMatch(config, normalizedProductNum, normalizedModel))
                .ToList();

            // 优先使用当前工位专用配置；没有专用配置时回退到 0 号共享配置。
            return candidates
                .OrderByDescending(it => it.StationNo == normalizedStationNo)
                .ThenByDescending(it => IsExactProductModelMatch(it, normalizedModel))
                .ThenByDescending(it => !string.IsNullOrWhiteSpace(it.ProductNum))
                .ThenBy(it => it.Sort)
                .FirstOrDefault();
        }
    }

    public BizProductProcessConfig Save(BizProductProcessConfig config)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            Normalize(config);

            config.UpdatedTime = DateTime.Now;
            if (config.Id <= 0)
            {
                config.CreatedTime = DateTime.Now;
                return _dbContext.Db.Insertable(config).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(config).ExecuteCommand();
            return _dbContext.Db.Queryable<BizProductProcessConfig>().InSingle(config.Id) ?? config;
        }
    }

    public void Disable(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var config = _dbContext.Db.Queryable<BizProductProcessConfig>().InSingle(id);
            if (config is null)
            {
                return;
            }

            config.Enabled = false;
            config.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(config)
                .UpdateColumns(it => new { it.Enabled, it.UpdatedTime })
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    private static void Normalize(BizProductProcessConfig config)
    {
        config.ProductNum = NormalizeNullable(config.ProductNum);
        config.ProductModel = NormalizeNullable(config.ProductModel) ?? string.Empty;
        config.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, config.StationNo);
        // 工序号由 MES 工单提供，不再参与采集参数匹配；保留字段仅兼容已有表结构。
        config.ProcessNo = string.IsNullOrWhiteSpace(config.ProcessNo) ? "*" : config.ProcessNo.Trim();
        config.ProcessName = NormalizeNullable(config.ProcessName);
        config.CollectionGroup = string.IsNullOrWhiteSpace(config.CollectionGroup)
            ? "default"
            : config.CollectionGroup.Trim();
        config.ProgramMatchRule = NormalizeNullable(config.ProgramMatchRule);
        config.ProductNoSource = string.IsNullOrWhiteSpace(config.ProductNoSource)
            ? ProductionConstants.ProductNoSources.AutoIncrement
            : config.ProductNoSource.Trim();
        config.WeldPointCount = Math.Max(1, config.WeldPointCount);
        config.Sort = Math.Max(0, config.Sort);
        config.Description = NormalizeNullable(config.Description);
    }

    private static bool IsProductMatch(BizProductProcessConfig config, string? productNum, string? productModel)
    {
        var configProductNum = NormalizeNullable(config.ProductNum);
        var configProductModel = NormalizeNullable(config.ProductModel);
        var productNumMatches = !string.IsNullOrWhiteSpace(productNum)
            && SameText(configProductNum, productNum);
        var legacyModelMatches = string.IsNullOrWhiteSpace(configProductNum)
            && !string.IsNullOrWhiteSpace(productModel)
            && SameText(configProductModel, productModel);

        if (!productNumMatches && !legacyModelMatches)
        {
            return false;
        }

        // 产品型号是辅助键；为空时表示只按产品工号匹配。
        return string.IsNullOrWhiteSpace(configProductModel)
            || string.IsNullOrWhiteSpace(productModel)
            || SameText(configProductModel, productModel);
    }

    private static bool IsExactProductModelMatch(BizProductProcessConfig config, string? productModel)
    {
        return !string.IsNullOrWhiteSpace(productModel)
            && SameText(config.ProductModel, productModel);
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(
            NormalizeNullable(left),
            NormalizeNullable(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
