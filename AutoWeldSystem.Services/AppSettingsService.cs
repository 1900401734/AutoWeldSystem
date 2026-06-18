using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Runtime;
using System.Reflection;

namespace AutoWeldSystem.Services;

/// <summary>
/// 系统基础设置服务。
/// </summary>
public class AppSettingsService(SqlSugarDbContext dbContext) : IAppSettingsService
{
    private static readonly PropertyInfo[] ComparableProperties = typeof(AppSettings)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead)
        .Where(property => property.Name is not nameof(AppSettings.Id) and not nameof(AppSettings.UpdatedTime))
        .ToArray();

    private readonly object _dbLock = new();

    private AppSettings? _cachedSettings;

    public event EventHandler<AppSettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// 获取配置：DCL 极速读取内存克隆快照，彻底告别数据库 IO。
    /// </summary>
    /// <returns>系统设置参数</returns>
    public AppSettings Get()
    {
        if (_cachedSettings is not null)
        {
            return _cachedSettings.Clone();
        }

        lock (_dbLock)
        {
            if (_cachedSettings is not null)
            {
                return _cachedSettings.Clone();
            }

            dbContext.InitDatabase();
            var settings = dbContext.Db.Queryable<AppSettings>().InSingle(1);

            if (settings is null)
            {
                settings = new AppSettings();
                dbContext.Db.Insertable(settings).ExecuteCommand();
            }

            Normalize(settings);
            _cachedSettings = settings.Clone();

            return _cachedSettings.Clone();
        }
    }

    /// <summary>
    /// 保存配置：落库、同步刷新缓存，并广播新旧数据对比。
    /// </summary>
    public AppSettings Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AppSettings previousSnapshot;
        AppSettings currentSnapshot;
        IReadOnlyList<string> changedProperties;

        lock (_dbLock)
        {
            dbContext.InitDatabase();

            var dbPrevious = dbContext.Db.Queryable<AppSettings>().InSingle(1) ?? new AppSettings();
            Normalize(dbPrevious);
            previousSnapshot = dbPrevious.Clone();

            settings.Id = 1;
            settings.UpdatedTime = DateTime.Now;
            Normalize(settings);

            var exists = dbContext.Db.Queryable<AppSettings>().Any(it => it.Id == 1);
            if (exists)
            {
                dbContext.Db.Updateable(settings).ExecuteCommand();
            }
            else
            {
                dbContext.Db.Insertable(settings).ExecuteCommand();
            }

            var dbCurrent = dbContext.Db.Queryable<AppSettings>().InSingle(1) ?? settings;
            Normalize(dbCurrent);
            currentSnapshot = dbCurrent.Clone();
            changedProperties = ResolveChangedProperties(previousSnapshot, currentSnapshot);

            _cachedSettings = currentSnapshot.Clone();
        }

        if (changedProperties.Count > 0)
        {
            SettingsChanged?.Invoke(this, new AppSettingsChangedEventArgs(previousSnapshot, currentSnapshot, changedProperties));
        }

        return currentSnapshot.Clone();
    }

    private static IReadOnlyList<string> ResolveChangedProperties(
        AppSettings previousSettings,
        AppSettings currentSettings)
    {
        return ComparableProperties
            .Where(property => !Equals(
                property.GetValue(previousSettings),
                property.GetValue(currentSettings)))
            .Select(property => property.Name)
            .ToArray();
    }

    /// <summary>
    /// 补齐升级后可能为空的过程参数接口配置，避免上传时拿到空接口名。
    /// </summary>
    private static void Normalize(AppSettings settings)
    {
        settings.ProcessParameterDeviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        if (!Enum.IsDefined(typeof(ApiCode), settings.ProcessParameterApiCode))
        {
            settings.ProcessParameterApiCode = ApiCode.EMWeldDetail_001;
        }

        settings.ProcessParameterApiName = string.IsNullOrWhiteSpace(settings.ProcessParameterApiName)
            ? ResolveDefaultProcessParameterApiName(settings.ProcessParameterApiCode)
            : settings.ProcessParameterApiName.Trim();
    }

    private static string NormalizeProcessParameterDeviceType(string? value)
    {
        return value?.Trim() switch
        {
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck => ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld => ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld,
            _ => ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic
        };
    }

    private static string ResolveDefaultProcessParameterApiName(ApiCode apiCode)
    {
        return apiCode switch
        {
            ApiCode.WholePieceCheckDetail_001 => "WholePieceCheckDetail",
            ApiCode.WholePieceWeldDetail_001 => "WholePieceWeldDetail",
            _ => "EMWeldDetail"
        };
    }
}
