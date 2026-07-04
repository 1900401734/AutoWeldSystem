using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
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
    /// 归一化过程参数设备类型，避免历史配置或空值导致上传接口映射落不到默认设备类型。
    /// </summary>
    private static void Normalize(AppSettings settings)
    {
        settings.EnableAutoStart ??= true;
        settings.EnableElevatedAutoStart ??= true;
        settings.ShowTestFlagInHistory ??= true;
        settings.EnablePlcStringNumericFormatting ??= true;
        settings.EnablePlcAlarmReading ??= true;
        settings.EnableDeviceStatusReport ??= true;
        settings.EnableWorkOrderStatusReport ??= true;
        settings.PlcStringNumericFormatMode = PlcStringNumericFormatter.NormalizeMode(settings.PlcStringNumericFormatMode);
        settings.ProgramFileDirectory = string.IsNullOrWhiteSpace(settings.ProgramFileDirectory)
            ? ProgramFileRules.DefaultProgramFileDirectory
            : settings.ProgramFileDirectory.Trim();
        settings.ProcessParameterDeviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        settings.DeviceBaseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(settings.DeviceBaseUrl);
        settings.MesBaseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(settings.MesBaseUrl);
        NormalizeMesEndpointSettings(settings);
        settings.CenterServerBaseUrl = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
        settings.CenterServerSystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType);
        settings.CenterServerHeartbeatIntervalSeconds = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(
            settings.CenterServerHeartbeatIntervalSeconds);
    }

    /// <summary>
    /// 兼容旧数据库：新增路由字段为空时回填原硬编码路径，避免升级后 MES 请求地址变化。
    /// </summary>
    private static void NormalizeMesEndpointSettings(AppSettings settings)
    {
        settings.MesUserRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesUserRoute, MesEndpointRouteRules.UserDefaultRoute);
        settings.MesWorkOrderRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesWorkOrderRoute, MesEndpointRouteRules.WorkOrderDefaultRoute);
        settings.MesServerTimeRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesServerTimeRoute, MesEndpointRouteRules.ServerTimeDefaultRoute);
        settings.MesProgramManageRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesProgramManageRoute, MesEndpointRouteRules.ProgramManageDefaultRoute);
        settings.MesStartWorkRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesStartWorkRoute, MesEndpointRouteRules.StartWorkDefaultRoute);
        settings.MesWorkStatusRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesWorkStatusRoute, MesEndpointRouteRules.WorkStatusDefaultRoute);
        settings.MesEndWorkRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesEndWorkRoute, MesEndpointRouteRules.EndWorkDefaultRoute);
        settings.MesReportFileRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesReportFileRoute, MesEndpointRouteRules.ReportFileDefaultRoute);
        settings.MesPostDataRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesPostDataRoute, MesEndpointRouteRules.PostDataDefaultRoute);
        settings.MesDeviceRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesDeviceRoute, MesEndpointRouteRules.DeviceDefaultRoute);
        settings.MesDeviceStatusRoute = MesEndpointRouteRules.NormalizeRoute(settings.MesDeviceStatusRoute, MesEndpointRouteRules.DeviceStatusDefaultRoute);
        settings.EnablePostDataCustomHeader ??= false;
        settings.PostDataHeaderKey = MesEndpointRouteRules.NormalizeHeaderKey(settings.PostDataHeaderKey);
        settings.PostDataHeaderValue = MesEndpointRouteRules.NormalizeHeaderValue(settings.PostDataHeaderValue);
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

}
