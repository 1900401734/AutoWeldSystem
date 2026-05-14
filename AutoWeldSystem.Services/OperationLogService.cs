using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services;

public class OperationLogService : IOperationLogService
{
    private readonly SqlSugarDbContext _dbContext;

    public OperationLogService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Write(string action, string detail, string level = "Info")
    {
        var user = GlobalContext.CurrentUser;

        var log = new SysOperationLog
        {
            UserNumber = user?.UserNumber ?? "system",
            UserName = user?.UserName ?? "system",
            Level = level,
            Action = action,
            Detail = detail,
            CreatedTime = DateTime.Now
        };

        _dbContext.Db.Insertable(log).ExecuteCommand();
    }

    public IReadOnlyList<SysOperationLog> GetRecent(int take = 200)
    {
        return _dbContext.Db.Queryable<SysOperationLog>()
            .OrderBy(it => it.Id, SqlSugar.OrderByType.Desc)
            .Take(take)
            .ToList();
    }
}
