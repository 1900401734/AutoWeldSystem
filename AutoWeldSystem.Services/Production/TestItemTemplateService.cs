using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 测试项目模板服务实现。
/// 该服务只负责配置持久化，不直接读取 PLC。
/// </summary>
public class TestItemTemplateService : ITestItemTemplateService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public TestItemTemplateService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<BizTestItemTemplate> GetTemplates(bool includeDisabled = false)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var query = _dbContext.Db.Queryable<BizTestItemTemplate>();
            if (!includeDisabled)
            {
                query = query.Where(it => it.Enabled);
            }

            return query.ToList()
                .OrderBy(it => it.Sort)
                .ThenBy(it => it.TemplateCode)
                .ToList();
        }
    }

    public IReadOnlyList<BizTestItemTemplateItem> GetItems(int templateId, bool includeDisabled = false)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var query = _dbContext.Db.Queryable<BizTestItemTemplateItem>()
                .Where(it => it.TemplateId == templateId);
            if (!includeDisabled)
            {
                query = query.Where(it => it.Enabled);
            }

            return query.ToList()
                .OrderBy(it => it.StationNo)
                .ThenBy(it => it.TouchNo)
                .ThenBy(it => it.Sort)
                .ThenBy(it => it.ItemKey)
                .ToList();
        }
    }

    public IReadOnlyList<BizTestItemTemplateItem> GetEnabledItems(int templateId, int stationNo, int touchNo)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            var normalizedStationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, stationNo);
            var normalizedTouchNo = Math.Max(0, touchNo);

            return _dbContext.Db.Queryable<BizTestItemTemplateItem>()
                .Where(it => it.TemplateId == templateId
                    && it.Enabled
                    && (it.StationNo == ProductionConstants.Stations.SharedStationNo || it.StationNo == normalizedStationNo)
                    && (it.TouchNo == 0 || it.TouchNo == normalizedTouchNo))
                .ToList()
                .OrderBy(it => it.StationNo)
                .ThenBy(it => it.TouchNo)
                .ThenBy(it => it.Sort)
                .ThenBy(it => it.ItemKey)
                .ToList();
        }
    }

    public BizTestItemTemplate SaveTemplate(BizTestItemTemplate template)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            Normalize(template);

            template.UpdatedTime = DateTime.Now;
            if (template.Id <= 0)
            {
                template.CreatedTime = DateTime.Now;
                return _dbContext.Db.Insertable(template).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(template).ExecuteCommand();
            return _dbContext.Db.Queryable<BizTestItemTemplate>().InSingle(template.Id) ?? template;
        }
    }

    public BizTestItemTemplateItem SaveItem(BizTestItemTemplateItem item)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            Normalize(item);

            item.UpdatedTime = DateTime.Now;
            if (item.Id <= 0)
            {
                return _dbContext.Db.Insertable(item).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(item).ExecuteCommand();
            return _dbContext.Db.Queryable<BizTestItemTemplateItem>().InSingle(item.Id) ?? item;
        }
    }

    public IReadOnlyList<BizTestItemTemplateItem> SaveItems(IEnumerable<BizTestItemTemplateItem> items)
    {
        var saved = new List<BizTestItemTemplateItem>();
        foreach (var item in items)
        {
            saved.Add(SaveItem(item));
        }

        return saved;
    }

    public void DeleteTemplate(int id)
    {
        if (id <= 0)
        {
            return;
        }

        lock (_dbLock)
        {
            EnsureSeedData();
            _dbContext.Db.Deleteable<BizTestItemTemplateItem>()
                .Where(it => it.TemplateId == id)
                .ExecuteCommand();
            _dbContext.Db.Deleteable<BizTestItemTemplate>()
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    public void DeleteItem(int id)
    {
        if (id <= 0)
        {
            return;
        }

        lock (_dbLock)
        {
            EnsureSeedData();
            _dbContext.Db.Deleteable<BizTestItemTemplateItem>()
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    private void EnsureSeedData()
    {
        _dbContext.InitDatabase();
        if (_dbContext.Db.Queryable<BizTestItemTemplate>().Any())
        {
            return;
        }

        var template = _dbContext.Db.Insertable(new BizTestItemTemplate
        {
            TemplateCode = "default",
            TemplateName = "默认测试项目模板",
            VersionNumber = 1,
            Enabled = true,
            Sort = 10,
            Description = "默认测试项目模板，PLC 地址需按现场配置。",
            CreatedTime = DateTime.Now,
            UpdatedTime = DateTime.Now
        }).ExecuteReturnEntity();

        foreach (var item in BuildDefaultItems(template.Id))
        {
            _dbContext.Db.Insertable(item).ExecuteCommand();
        }
    }

    private static IReadOnlyList<BizTestItemTemplateItem> BuildDefaultItems(int templateId)
    {
        return new[]
        {
            CreateDefaultItem(templateId, "max_electric", "峰值电流", "KA", "MaxElectric", "峰值电流", 10, true),
            CreateDefaultItem(templateId, "max_voltage", "峰值电压", "V", "MaxVoltage", "峰值电压", 20, true),
            CreateDefaultItem(templateId, "valid_power", "有效功率", "KW", "ValidPower", "有效功率", 30, true)
        };
    }

    private static BizTestItemTemplateItem CreateDefaultItem(
        int templateId,
        string itemKey,
        string itemName,
        string unit,
        string mesFieldPrefix,
        string reportColumnName,
        int sort,
        bool required)
    {
        return new BizTestItemTemplateItem
        {
            TemplateId = templateId,
            StationNo = ProductionConstants.Stations.SharedStationNo,
            TouchNo = 0,
            ItemKey = itemKey,
            ItemName = itemName,
            ValueDataType = AppConstants.PlcDataTypes.Float,
            ResultDataType = AppConstants.PlcDataTypes.Int16,
            ValueDataLength = 1,
            ResultDataLength = 1,
            Scale = 1m,
            Offset = 0m,
            DecimalPlaces = 2,
            Unit = unit,
            MesFieldPrefix = mesFieldPrefix,
            ReportColumnName = reportColumnName,
            Required = required,
            Enabled = true,
            Sort = sort,
            Description = "默认测试项目，PLC 地址需按现场配置。",
            UpdatedTime = DateTime.Now
        };
    }

    private static void Normalize(BizTestItemTemplate template)
    {
        template.TemplateCode = NormalizeRequired(template.TemplateCode, "模板编码不能为空。");
        template.TemplateName = NormalizeRequired(template.TemplateName, "模板名称不能为空。");
        template.VersionNumber = Math.Max(1, template.VersionNumber);
        template.Sort = Math.Max(0, template.Sort);
        template.Description = NormalizeNullable(template.Description);
    }

    private static void Normalize(BizTestItemTemplateItem item)
    {
        item.TemplateId = Math.Max(0, item.TemplateId);
        item.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, item.StationNo);
        item.TouchNo = Math.Max(0, item.TouchNo);
        item.ItemKey = NormalizeRequired(item.ItemKey, "测试项目键不能为空。");
        item.ItemName = NormalizeRequired(item.ItemName, "测试项目名称不能为空。");
        item.ActualAddress = NormalizeNullable(item.ActualAddress);
        item.UpperAddress = NormalizeNullable(item.UpperAddress);
        item.LowerAddress = NormalizeNullable(item.LowerAddress);
        item.ResultAddress = NormalizeNullable(item.ResultAddress);
        item.ValueDataType = NormalizeDataType(item.ValueDataType, AppConstants.PlcDataTypes.Float);
        item.ResultDataType = NormalizeDataType(item.ResultDataType, AppConstants.PlcDataTypes.Int16);
        item.ValueDataLength = Math.Max(1, item.ValueDataLength);
        item.ResultDataLength = Math.Max(1, item.ResultDataLength);
        item.Scale = item.Scale == 0 ? 1m : item.Scale;
        item.DecimalPlaces = Math.Clamp(item.DecimalPlaces, 0, 6);
        item.Unit = NormalizeNullable(item.Unit);
        item.MesFieldPrefix = NormalizeNullable(item.MesFieldPrefix);
        item.ReportColumnName = NormalizeNullable(item.ReportColumnName);
        item.Sort = Math.Max(0, item.Sort);
        item.Description = NormalizeNullable(item.Description);
    }

    private static string NormalizeDataType(string value, string fallback)
    {
        return AppConstants.PlcDataTypes.All.Contains(value) ? value : fallback;
    }

    private static string NormalizeRequired(string value, string message)
    {
        var normalized = value.Trim();
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
}
