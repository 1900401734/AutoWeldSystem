using AutoWeldSystem.Core.Entities;
using SqlSugar;

namespace AutoWeldSystem.Data;

public class SqlSugarDbContext : IDisposable
{
    public SqlSugarScope Db { get; }

    private const string DefaultConnectionString = "server=localhost;port=3306;database=autoweldsystem_db;uid=root;pwd=123456;";
    private readonly object _initLock = new();
    private bool _initialized;

    public SqlSugarDbContext(string? connectionString)
    {
        Db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = connectionString ?? DefaultConnectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    public void InitDatabase()
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                Db.DbMaintenance.CreateDatabase();

                Db.CodeFirst.InitTables(
                    typeof(AppSettings),
                    typeof(SysUser),
                    typeof(SysRole),
                    typeof(SysRolePermission),
                    typeof(SysPermission),
                    typeof(SysOperationLog),
                    typeof(BizProgram),
                    typeof(BizProgramRevision),
                    typeof(BizWeldTask),
                    typeof(BizWeldData),
                    typeof(BizWeldPointRecord),
                    typeof(BizProductProcessConfig),
                    typeof(BizTestScheme),
                    typeof(BizSchemeDetail),
                    typeof(DimTestItem),
                    typeof(BizProductionReportFile),
                    typeof(BizUploadTask),
                    typeof(BizDeviceStatusLog),
                    typeof(BizRuntimeTipState),
                    typeof(BizPlcAddress),
                    typeof(BizPlcAlarmAddress));

                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"MySql initialization or schema migration failed. Check the connection and legacy data. Details: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
