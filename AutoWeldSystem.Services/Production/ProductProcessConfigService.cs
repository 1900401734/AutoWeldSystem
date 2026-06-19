using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 产品工艺配置服务。
/// 当前最小闭环只按产品工号和工位匹配，不再使用旧的测试参数绑定方式。
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
                .OrderBy(it => it.ProductNum)
                .ThenBy(it => it.StationNo)
                .ThenBy(it => it.Id)
                .ToList();
        }
    }

    public BizProductProcessConfig? FindActive(
        string productNum,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedProductNum = NormalizeNullable(productNum);
        if (string.IsNullOrWhiteSpace(normalizedProductNum))
        {
            return null;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedStationNo = NormalizeStationNo(stationNo);
            var configs = _dbContext.Db.Queryable<BizProductProcessConfig>()
                .Where(it => it.Enabled && it.ProductNum == normalizedProductNum)
                .ToList();

            return configs
                .Where(it => it.StationNo == normalizedStationNo || it.StationNo == ProductionConstants.Stations.SharedStationNo)
                .OrderByDescending(it => it.StationNo == normalizedStationNo)
                .ThenBy(it => it.Id)
                .FirstOrDefault();
        }
    }

    public BizProductProcessConfig? FindActiveForTask(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var productNum = ResolveProgramBoundProductNum(task);
        return FindActive(productNum, stationNo);
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

    public void Delete(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            _dbContext.Db.Deleteable<BizProductProcessConfig>()
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    private string ResolveProgramBoundProductNum(BizWeldTask task)
    {
        var programId = NormalizeNullable(task.ProgramId);
        if (string.IsNullOrWhiteSpace(programId))
        {
            return NormalizeNullable(task.ProductNum) ?? string.Empty;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var localProgram = _dbContext.Db.Queryable<BizProgram>()
                .Where(program => !program.IsDeleted && program.ProgramId == programId)
                .ToList()
                .OrderByDescending(program => SameText(program.DeviceId, task.DeviceId))
                .ThenByDescending(program => program.UpdatedTime)
                .FirstOrDefault();

            return NormalizeNullable(localProgram?.ProductNum) ?? NormalizeNullable(task.ProductNum) ?? string.Empty;
        }
    }

    private static void Normalize(BizProductProcessConfig config)
    {
        config.SchemeId = string.IsNullOrWhiteSpace(config.SchemeId) ? "S01" : config.SchemeId.Trim();
        config.ProductNum = NormalizeRequired(config.ProductNum, "产品工号不能为空。");
        config.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, config.StationNo);
        config.TouchCount = Math.Max(1, config.TouchCount);
        config.PointName = NormalizeNullable(config.PointName) ?? "焊点";
        config.PointNoHeader = NormalizeNullable(config.PointNoHeader) ?? $"{config.PointName}序号";
        config.PointResultHeader = NormalizeNullable(config.PointResultHeader) ?? $"{config.PointName}结果";
        config.PointCountHeader = NormalizeNullable(config.PointCountHeader) ?? $"{config.PointName}数";
        config.ShowTestFlagInHistory ??= true;
        config.ProductBase = NormalizeRequired(config.ProductBase, "产品头基地址不能为空。");
        config.ProductLen = Math.Max(1, config.ProductLen);
        config.ProductNoExpr = NormalizeRequired(config.ProductNoExpr, "产品编号偏移表达式不能为空。");
        config.ProductResultExpr = NormalizeRequired(config.ProductResultExpr, "产品结果偏移表达式不能为空。");
        config.ActualTouchCountExpr = NormalizeNullable(config.ActualTouchCountExpr);
        config.PresetTouchCountExpr = NormalizeNullable(config.PresetTouchCountExpr);
        config.TouchBase = NormalizeRequired(config.TouchBase, "焊点头基地址不能为空。");
        config.TouchNoBase = NormalizeNullable(config.TouchNoBase) ?? config.TouchBase;
        config.TouchResultBase = NormalizeNullable(config.TouchResultBase) ?? config.TouchBase;
        config.TouchHeaderLen = Math.Max(1, config.TouchHeaderLen);
        config.TouchNoExpr = NormalizeRequired(config.TouchNoExpr, "焊点编号偏移表达式不能为空。");
        config.TouchResultExpr = NormalizeRequired(config.TouchResultExpr, "焊点结果偏移表达式不能为空。");
        config.TestBase = NormalizeRequired(config.TestBase, "测试项基地址不能为空。");
        config.TestAreaLen = Math.Max(1, config.TestAreaLen);
        ValidateExpression(config.ProductNoExpr, "产品编号偏移表达式");
        ValidateExpression(config.ProductResultExpr, "产品结果偏移表达式");
        ValidateExpression(config.ActualTouchCountExpr, "实际采集点数偏移表达式");
        ValidateExpression(config.PresetTouchCountExpr, "预设采集点数偏移表达式");
        ValidateExpression(config.TouchNoExpr, "采集点编号偏移表达式");
        ValidateExpression(config.TouchResultExpr, "采集点结果偏移表达式");
    }

    private static void ValidateExpression(string? expression, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        try
        {
            PlcOffsetExpression.Parse(expression);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{fieldName}无效：{ex.Message} {PlcOffsetExpression.RuleHint}", ex);
        }
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(message);
        }

        return normalized;
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
