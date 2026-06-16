using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 运行提示持久化服务。
/// 该服务只保存最后一条提示，详细生产链路仍由 ProductionFlowLogService 记录。
/// </summary>
public sealed class RuntimeTipStateService : IRuntimeTipStateService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public RuntimeTipStateService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public BizRuntimeTipState Get(int stationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedStationNo = NormalizeStationNo(stationNo);
            var state = _dbContext.Db.Queryable<BizRuntimeTipState>().InSingle(normalizedStationNo);
            return state ?? new BizRuntimeTipState { StationNo = normalizedStationNo };
        }
    }

    public void Save(BizRuntimeTipState state)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            state.StationNo = NormalizeStationNo(state.StationNo);
            state.UpdatedTime = DateTime.Now;

            var exists = _dbContext.Db.Queryable<BizRuntimeTipState>().InSingle(state.StationNo) is not null;
            if (exists)
            {
                _dbContext.Db.Updateable(state).ExecuteCommand();
                return;
            }

            _dbContext.Db.Insertable(state).ExecuteCommand();
        }
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }
}
