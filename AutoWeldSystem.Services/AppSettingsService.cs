using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services;

/// <summary>
/// 系统基础设置服务。
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public AppSettingsService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public AppSettings Get()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var settings = _dbContext.Db.Queryable<AppSettings>().InSingle(1);
            if (settings is not null)
            {
                return settings;
            }

            settings = new AppSettings();
            _dbContext.Db.Insertable(settings).ExecuteCommand();
            return settings;
        }
    }

    public AppSettings Save(AppSettings settings)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            settings.Id = 1;
            settings.UpdatedTime = DateTime.Now;
            var exists = _dbContext.Db.Queryable<AppSettings>().Any(it => it.Id == 1);
            if (exists)
            {
                _dbContext.Db.Updateable(settings).ExecuteCommand();
            }
            else
            {
                _dbContext.Db.Insertable(settings).ExecuteCommand();
            }

            return _dbContext.Db.Queryable<AppSettings>().InSingle(1) ?? settings;
        }
    }
}
