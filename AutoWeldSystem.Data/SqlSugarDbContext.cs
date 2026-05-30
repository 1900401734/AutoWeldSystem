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
                    typeof(BizWeldPointRecord),
                    typeof(BizProductInstance),
                    typeof(BizProductProcessConfig),
                    typeof(BizTestScheme),
                    typeof(BizSchemeDetail),
                    typeof(DimTestItem),
                    typeof(BizProductionReportFile),
                    typeof(BizUploadTask),
                    typeof(BizDeviceStatusLog),
                    typeof(BizPlcAddress));

                SeedDefaultTestScheme();

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

    /// <summary>
    /// 写入最小闭环需要的默认测试方案。
    /// 产品工艺只要绑定 S01，就会默认采集峰值电流、峰值电压和有效功率。
    /// </summary>
    private void SeedDefaultTestScheme()
    {
        if (!Db.Queryable<BizTestScheme>().Any(it => it.SchemeId == "S01"))
        {
            Db.Insertable(new BizTestScheme
            {
                SchemeId = "S01",
                SchemeName = "标准3项测试结构",
                Description = "包含峰值电流、峰值电压、有效功率。"
            }).ExecuteCommand();
        }

        var electricId = EnsureTestItem("峰值电流", "KA", "0:F-0", "4:I-2", "8:I-2", "12:H-4");
        var voltageId = EnsureTestItem("峰值电压", "V", "16:F-0", "20:I-2", "24:I-2", "28:H-4");
        var powerId = EnsureTestItem("有效功率", "KW", "32:F-0", "36:I-2", "40:I-2", "44:H-4");

        EnsureSchemeDetail("S01", electricId);
        EnsureSchemeDetail("S01", voltageId);
        EnsureSchemeDetail("S01", powerId);
    }

    private int EnsureTestItem(
        string itemName,
        string unit,
        string actualExpression,
        string upperExpression,
        string lowerExpression,
        string resultExpression)
    {
        var existing = Db.Queryable<DimTestItem>()
            .First(it => it.ItemName == itemName);
        if (existing is not null)
        {
            return existing.ItemId;
        }

        var inserted = Db.Insertable(new DimTestItem
        {
            ItemName = itemName,
            Unit = unit,
            ActualExpression = actualExpression,
            UpperExpression = upperExpression,
            LowerExpression = lowerExpression,
            ResultExpression = resultExpression
        }).ExecuteReturnEntity();

        return inserted.ItemId;
    }

    private void EnsureSchemeDetail(string schemeId, int itemId)
    {
        if (Db.Queryable<BizSchemeDetail>().Any(it => it.SchemeId == schemeId && it.ItemId == itemId))
        {
            return;
        }

        Db.Insertable(new BizSchemeDetail
        {
            SchemeId = schemeId,
            ItemId = itemId
        }).ExecuteCommand();
    }
}
