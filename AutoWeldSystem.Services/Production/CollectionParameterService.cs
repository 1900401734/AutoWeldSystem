using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 采集参数配置服务实现。
/// 默认种子只提供常见焊接参数，现场地址仍由用户在地址维护界面填写。
/// </summary>
public class CollectionParameterService : ICollectionParameterService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public CollectionParameterService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<BizCollectionParameter> GetAll(bool includeDisabled = false)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var query = _dbContext.Db.Queryable<BizCollectionParameter>();
            if (!includeDisabled)
            {
                query = query.Where(it => it.Enabled);
            }

            return query.ToList()
                .OrderBy(it => it.CollectionGroup)
                .ThenBy(it => it.StationNo)
                .ThenBy(it => it.Sort)
                .ThenBy(it => it.ParameterKey)
                .ToList();
        }
    }

    public IReadOnlyList<BizCollectionParameter> GetEnabledParameters(string collectionGroup, int stationNo)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            var normalizedGroup = NormalizeGroup(collectionGroup);

            return _dbContext.Db.Queryable<BizCollectionParameter>()
                .Where(it => it.Enabled
                    && it.CollectionGroup == normalizedGroup
                    && (it.StationNo == 0 || it.StationNo == stationNo))
                .ToList()
                .OrderBy(it => it.StationNo)
                .ThenBy(it => it.Sort)
                .ThenBy(it => it.ParameterKey)
                .ToList();
        }
    }

    public BizCollectionParameter Save(BizCollectionParameter parameter)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            Normalize(parameter);
            parameter.UpdatedTime = DateTime.Now;

            if (parameter.Id <= 0)
            {
                var existing = FindByLogicalKey(parameter);
                if (existing is not null)
                {
                    parameter.Id = existing.Id;
                    _dbContext.Db.Updateable(parameter).ExecuteCommand();
                    return _dbContext.Db.Queryable<BizCollectionParameter>().InSingle(parameter.Id) ?? parameter;
                }

                return _dbContext.Db.Insertable(parameter).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(parameter).ExecuteCommand();
            return _dbContext.Db.Queryable<BizCollectionParameter>().InSingle(parameter.Id) ?? parameter;
        }
    }

    public IReadOnlyList<BizCollectionParameter> SaveAll(IEnumerable<BizCollectionParameter> parameters)
    {
        var saved = new List<BizCollectionParameter>();
        foreach (var parameter in parameters)
        {
            saved.Add(Save(parameter));
        }

        return saved;
    }

    public void Disable(int id)
    {
        lock (_dbLock)
        {
            EnsureSeedData();
            var parameter = _dbContext.Db.Queryable<BizCollectionParameter>().InSingle(id);
            if (parameter is null)
            {
                return;
            }

            parameter.Enabled = false;
            parameter.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(parameter)
                .UpdateColumns(it => new { it.Enabled, it.UpdatedTime })
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    public void Delete(int id)
    {
        if (id <= 0)
        {
            return;
        }

        lock (_dbLock)
        {
            EnsureSeedData();
            _dbContext.Db.Deleteable<BizCollectionParameter>()
                .Where(it => it.Id == id)
                .ExecuteCommand();
        }
    }

    private void EnsureSeedData()
    {
        _dbContext.InitDatabase();
        if (_dbContext.Db.Queryable<BizCollectionParameter>().Any())
        {
            return;
        }

        foreach (var parameter in BuildDefaultParameters())
        {
            _dbContext.Db.Insertable(parameter).ExecuteCommand();
        }
    }

    private BizCollectionParameter? FindByLogicalKey(BizCollectionParameter parameter)
    {
        return _dbContext.Db.Queryable<BizCollectionParameter>()
            .First(it => it.CollectionGroup == parameter.CollectionGroup
                && it.StationNo == parameter.StationNo
                && it.ParameterKey == parameter.ParameterKey);
    }

    private static IReadOnlyList<BizCollectionParameter> BuildDefaultParameters()
    {
        var parameters = new List<BizCollectionParameter>();
        for (var stationNo = 1; stationNo <= 2; stationNo++)
        {
            parameters.Add(CreateDefault(stationNo, "max_electric", "峰值电流", "MaxElectric", "峰值电流", "KA", 10, true));
            parameters.Add(CreateDefault(stationNo, "max_voltage", "峰值电压", "MaxVoltage", "峰值电压", "V", 20, true));
            parameters.Add(CreateDefault(stationNo, "valid_power", "有效功率", "ValidPower", "有效功率", "KW", 30, true));
            parameters.Add(CreateDefault(stationNo, "displacement", "位移", "Displacement", "位移", null, 40, false));
            parameters.Add(CreateDefault(stationNo, "weld_ts", "焊接时间", "WeldTs", "焊接时间", "ms", 50, false));
        }

        return parameters;
    }

    private static BizCollectionParameter CreateDefault(
        int stationNo,
        string key,
        string name,
        string mesField,
        string reportColumn,
        string? unit,
        int sort,
        bool required)
    {
        return new BizCollectionParameter
        {
            StationNo = stationNo,
            CollectionGroup = "default",
            ParameterKey = key,
            ParameterName = name,
            DataType = AppConstants.PlcDataTypes.Float,
            DataLength = 1,
            Scale = 1m,
            Offset = 0m,
            DecimalPlaces = 2,
            Unit = unit,
            MesFieldName = mesField,
            ReportColumnName = reportColumn,
            Required = required,
            Enabled = true,
            Sort = sort,
            Description = "默认采集参数，PLC 地址需按现场配置填写。",
            UpdatedTime = DateTime.Now
        };
    }

    private static void Normalize(BizCollectionParameter parameter)
    {
        parameter.CollectionGroup = NormalizeGroup(parameter.CollectionGroup);
        parameter.ParameterKey = NormalizeRequired(parameter.ParameterKey, "参数键不能为空。");
        parameter.ParameterName = NormalizeRequired(parameter.ParameterName, "参数名称不能为空。");
        parameter.Address = NormalizeNullable(parameter.Address);
        parameter.DataType = AppConstants.PlcDataTypes.All.Contains(parameter.DataType)
            ? parameter.DataType
            : AppConstants.PlcDataTypes.Float;
        parameter.DataLength = Math.Max(1, parameter.DataLength);
        parameter.Scale = parameter.Scale == 0 ? 1m : parameter.Scale;
        parameter.DecimalPlaces = Math.Clamp(parameter.DecimalPlaces, 0, 6);
        parameter.Unit = NormalizeNullable(parameter.Unit);
        parameter.MesFieldName = NormalizeNullable(parameter.MesFieldName);
        parameter.ReportColumnName = NormalizeNullable(parameter.ReportColumnName);
        parameter.Sort = Math.Max(0, parameter.Sort);
        parameter.Description = NormalizeNullable(parameter.Description);
    }

    private static string NormalizeGroup(string group)
    {
        return string.IsNullOrWhiteSpace(group) ? "default" : group.Trim();
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
