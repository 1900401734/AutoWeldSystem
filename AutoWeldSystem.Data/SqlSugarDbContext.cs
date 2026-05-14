using AutoWeldSystem.Core.Models;
using SqlSugar;

namespace AutoWeldSystem.Data;

public class SqlSugarDbContext : IDisposable
{
    private const string DefaultConnectionString =
        "Server=127.0.0.1;Port=3306;Database=autoweldsystem_db;Uid=root;Pwd=123456;SslMode=None;AllowPublicKeyRetrieval=True;CharSet=utf8mb4;";

    public SqlSugarScope Db { get; }
    private readonly object _initLock = new();
    private bool _initialized;

    public SqlSugarDbContext(string? connectionString = null)
    {
        var actualConnectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DefaultConnectionString
            : connectionString;

        Db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = actualConnectionString,
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
                    typeof(BizPlcAddress));

                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"MySql initialization failed. Check the connection string. Details: {ex.Message}", ex);
            }
        }
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
