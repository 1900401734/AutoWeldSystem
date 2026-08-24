using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 测试方案配置服务实现。
/// 所有保存入口都先做基础清洗和重复校验，减少界面层重复业务规则。
/// </summary>
public sealed class TestSchemeConfigService : ITestSchemeConfigService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public TestSchemeConfigService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<BizTestScheme> GetSchemes()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizTestScheme>()
                .ToList()
                .OrderBy(scheme => scheme.SchemeId)
                .ToList();
        }
    }

    public BizTestScheme SaveScheme(BizTestScheme scheme)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            Normalize(scheme);

            var exists = _dbContext.Db.Queryable<BizTestScheme>()
                .Any(it => it.SchemeId == scheme.SchemeId);
            if (exists)
            {
                _dbContext.Db.Updateable(scheme).ExecuteCommand();
                return _dbContext.Db.Queryable<BizTestScheme>().InSingle(scheme.SchemeId) ?? scheme;
            }

            _dbContext.Db.Insertable(scheme).ExecuteCommand();
            return scheme;
        }
    }

    public void DeleteScheme(string schemeId)
    {
        var normalizedSchemeId = NormalizeRequired(schemeId, "测试方案ID不能为空。");
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            _dbContext.Db.Deleteable<BizSchemeDetail>()
                .Where(it => it.SchemeId == normalizedSchemeId)
                .ExecuteCommand();
            _dbContext.Db.Deleteable<BizTestScheme>()
                .Where(it => it.SchemeId == normalizedSchemeId)
                .ExecuteCommand();
        }
    }

    public IReadOnlyList<BizSchemeDetail> GetDetails(string? schemeId = null)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedSchemeId = NormalizeNullable(schemeId);
            var query = _dbContext.Db.Queryable<BizSchemeDetail>();
            if (!string.IsNullOrWhiteSpace(normalizedSchemeId))
            {
                query = query.Where(it => it.SchemeId == normalizedSchemeId);
            }

            var items = _dbContext.Db.Queryable<DimTestItem>().ToList();
            return query.ToList()
                .Select(detail => NormalizeDetailRoles(detail, items))
                .OrderBy(detail => detail.SchemeId)
                .ThenBy(detail => detail.DetailId)
                .ToList();
        }
    }

    public BizSchemeDetail SaveDetail(BizSchemeDetail detail)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var item = _dbContext.Db.Queryable<DimTestItem>().InSingle(detail.ItemId);
            Normalize(detail, item);
            EnsureSchemeExists(detail.SchemeId);
            EnsureItemExists(detail.ItemId);
            EnsureDetailNotDuplicated(detail);

            if (detail.DetailId <= 0)
            {
                return _dbContext.Db.Insertable(detail).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(detail).ExecuteCommand();
            return _dbContext.Db.Queryable<BizSchemeDetail>().InSingle(detail.DetailId) ?? detail;
        }
    }

    public void DeleteDetail(int detailId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            _dbContext.Db.Deleteable<BizSchemeDetail>()
                .Where(it => it.DetailId == detailId)
                .ExecuteCommand();
        }
    }

    public IReadOnlyList<DimTestItem> GetItems()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<DimTestItem>()
                .ToList()
                .OrderBy(item => item.ItemId)
                .ToList();
        }
    }

    public DimTestItem SaveItem(DimTestItem item)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            Normalize(item);

            if (item.ItemId <= 0)
            {
                // 不依赖 MySQL 的 AUTO_INCREMENT：它在删除记录后不回退计数器，
                // 会把测试项ID变成 21、22、23 这种跟界面序号不一致的跳号。
                // 由应用层在同一把锁内读最大ID并显式写入，保证序号连续。
                item.ItemId = TestItemIdAllocationRules.AllocateNextId(
                    _dbContext.Db.Queryable<DimTestItem>().Select(it => it.ItemId).ToList());
                _dbContext.Db.Insertable(item).OffIdentity().ExecuteCommand();
                return _dbContext.Db.Queryable<DimTestItem>().InSingle(item.ItemId) ?? item;
            }

            _dbContext.Db.Updateable(item).ExecuteCommand();
            return _dbContext.Db.Queryable<DimTestItem>().InSingle(item.ItemId) ?? item;
        }
    }

    public void DeleteItem(int itemId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            _dbContext.Db.Deleteable<BizSchemeDetail>()
                .Where(it => it.ItemId == itemId)
                .ExecuteCommand();
            _dbContext.Db.Deleteable<DimTestItem>()
                .Where(it => it.ItemId == itemId)
                .ExecuteCommand();
        }
    }

    private void EnsureSchemeExists(string schemeId)
    {
        if (!_dbContext.Db.Queryable<BizTestScheme>().Any(it => it.SchemeId == schemeId))
        {
            throw new InvalidOperationException($"测试方案“{schemeId}”不存在。");
        }
    }

    private void EnsureItemExists(int itemId)
    {
        if (!_dbContext.Db.Queryable<DimTestItem>().Any(it => it.ItemId == itemId))
        {
            throw new InvalidOperationException($"测试项ID“{itemId}”不存在。");
        }
    }

    private void EnsureDetailNotDuplicated(BizSchemeDetail detail)
    {
        var exists = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Any(it => it.SchemeId == detail.SchemeId
                && it.ItemId == detail.ItemId
                && it.DetailId != detail.DetailId);
        if (exists)
        {
            throw new InvalidOperationException($"测试方案“{detail.SchemeId}”已包含测试项“{detail.ItemId}”。");
        }
    }

    private static void Normalize(BizTestScheme scheme)
    {
        scheme.SchemeId = NormalizeRequired(scheme.SchemeId, "测试方案ID不能为空。");
        scheme.SchemeName = NormalizeRequired(scheme.SchemeName, "测试方案名称不能为空。");
        scheme.Description = NormalizeNullable(scheme.Description);
    }

    private static void Normalize(BizSchemeDetail detail, DimTestItem? item)
    {
        detail.SchemeId = NormalizeRequired(detail.SchemeId, "测试方案ID不能为空。");
        if (detail.ItemId <= 0)
        {
            throw new InvalidOperationException("测试项ID必须大于0。");
        }

        if (item is not null)
        {
            SchemeDetailRoleRules.ClearUnavailableRoles(detail, item);
        }

        ValidateRoleOutputs(detail);

        if (!HasAnyEnabledRole(detail))
        {
            throw new InvalidOperationException("方案明细至少需要启用实际值、上限、下限或结果中的一项。");
        }

        NormalizeDetailOutput(detail, item);
    }

    /// <summary>
    /// 按测试项字典清理不可用角色，并补齐可用角色的显示表头。
    /// </summary>
    private static BizSchemeDetail NormalizeDetailRoles(BizSchemeDetail detail, IReadOnlyList<DimTestItem> items)
    {
        var item = items.FirstOrDefault(item => item.ItemId == detail.ItemId);
        if (item is not null)
        {
            SchemeDetailRoleRules.ClearUnavailableRoles(detail, item);
        }

        NormalizeDetailOutput(detail, item);
        return detail;
    }

    private static void NormalizeDetailOutput(BizSchemeDetail detail, DimTestItem? item)
    {
        var itemName = NormalizeNullable(item?.ItemName) ?? $"测试项{detail.ItemId}";
        foreach (var role in SchemeDetailRoleRules.AllRoles)
        {
            var header = SchemeDetailRoleRules.ResolveHeader(SchemeDetailRoleRules.GetHeader(detail, role), itemName, role);
            SchemeDetailRoleRules.SetHeader(detail, role, header);
        }
        detail.ActualMesFieldName = NormalizeNullable(detail.ActualMesFieldName);
        detail.UpperMesFieldName = NormalizeNullable(detail.UpperMesFieldName);
        detail.LowerMesFieldName = NormalizeNullable(detail.LowerMesFieldName);
        detail.ResultMesFieldName = NormalizeNullable(detail.ResultMesFieldName);
    }

    /// <summary>
    /// 校验 MES 输出字段；保存和报表可以独立于实时预览采集开关启用。
    /// </summary>
    private static void ValidateRoleOutputs(BizSchemeDetail detail)
    {
        foreach (var role in SchemeDetailRoleRules.AllRoles)
        {
            if (SchemeDetailRoleRules.IsMesEnabled(detail, role)
                && string.IsNullOrWhiteSpace(SchemeDetailRoleRules.GetMesFieldName(detail, role)))
            {
                throw new InvalidOperationException("方案明细中启用 MES 上传的字段，必须填写 MES 字段名。");
            }
        }
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyConfiguredRole(detail);
    }

    private static void Normalize(DimTestItem item)
    {
        item.ItemName = NormalizeRequired(item.ItemName, "测试项名称不能为空。");
        item.ActualExpression = NormalizeRequired(item.ActualExpression, "实际值偏移表达式不能为空。");
        item.UpperExpression = NormalizeNullable(item.UpperExpression);
        item.LowerExpression = NormalizeNullable(item.LowerExpression);
        item.ResultExpression = NormalizeNullable(item.ResultExpression);
        item.Unit = NormalizeNullable(item.Unit);

        ValidateExpression(item.ActualExpression, "实际值偏移表达式");
        ValidateExpression(item.UpperExpression, "上限偏移表达式");
        ValidateExpression(item.LowerExpression, "下限偏移表达式");
        ValidateExpression(item.ResultExpression, "结果偏移表达式");
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
}
