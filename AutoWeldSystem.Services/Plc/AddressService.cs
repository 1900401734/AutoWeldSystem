using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Plc;

public class AddressService : IPlcAddressService
{
    private readonly object _dbLock = new();

    private AppSettings _appSettings;
    // 补种完成后置 true，避免每次 GetAll/GetAddress/SaveAll 都执行数据库写操作；
    // 双工位从关闭切换为开启时在 OnSettingsChanged 中重置，触发工位2地址的补种。
    private volatile bool _seeded;
    private readonly SqlSugarDbContext _dbContext;

    private readonly IAppSettingsService _settingsService;

    public AddressService(SqlSugarDbContext dbContext, IAppSettingsService settingsService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _appSettings = settingsService.Get();
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        var wasDualStationEnabled = Volatile.Read(ref _appSettings).EnableDualStation;
        Interlocked.Exchange(ref _appSettings, e.CurrentSettings);

        // 双工位从关闭切换为开启时才需要补种工位2地址；其余设置变更（包括过程参数设备类型）
        // 不应触发补种检查，避免每次保存系统设置后打开地址维护页都重新执行一次数据库写操作。
        if (!wasDualStationEnabled && e.CurrentSettings.EnableDualStation)
        {
            _seeded = false;
        }
    }

    public IReadOnlyList<BizPlcAddress> GetAll()
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var dualStationEnabled = Volatile.Read(ref _appSettings).EnableDualStation;

            return _dbContext.Db.Queryable<BizPlcAddress>()
                .OrderBy(it => it.Sort)
                .ToList()
                .Where(address => dualStationEnabled || address.StationNo <= ProductionConstants.Stations.DefaultStationNo)
                .Where(address => !IsLogicalKey(address.LogicalKey) || address.StationNo > ProductionConstants.Stations.SharedStationNo)
                .OrderBy(it => it.Sort)
                .ThenBy(it => it.StationNo)
                .ToList();
        }
    }

    /// <summary>
    /// 根据逻辑地址键和工位号获取PLC地址配置。工位号用于区分同一套业务信号在不同工位的地址差异。
    /// </summary>
    /// <param name="logicalKey">逻辑地址键</param>
    /// <param name="stationNo">工位号</param>
    /// <returns>PLC地址配置</returns>
    public BizPlcAddress? GetAddress(string logicalKey, int stationNo)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var addresses = _dbContext.Db.Queryable<BizPlcAddress>()
                .Where(it => it.LogicalKey == logicalKey)
                .ToList()
                .Where(it => it.StationNo == stationNo)
                .ToList();

            return addresses
                .OrderByDescending(it => it.StationNo == stationNo)
                .ThenBy(it => it.Sort)
                .FirstOrDefault();
        }
    }

    public void SaveAll(IEnumerable<BizPlcAddress> addresses)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            foreach (var address in addresses)
            {
                NormalizeAddress(address);

                var storedAddress = _dbContext.Db.Queryable<BizPlcAddress>().First(it => it.LogicalKey == address.LogicalKey && it.StationNo == address.StationNo);

                if (storedAddress is null)
                {
                    address.UpdatedTime = DateTime.Now;
                    _dbContext.Db.Insertable(address).ExecuteCommand();
                    continue;
                }

                if (!HasAddressChanged(storedAddress, address))
                {
                    // 没有实际改动时保留原更新时间，避免用户误以为所有地址都被修改。
                    address.UpdatedTime = storedAddress.UpdatedTime;
                    continue;
                }

                address.UpdatedTime = DateTime.Now;
                _dbContext.Db.Updateable(address)
                    .UpdateColumns(it => new
                    {
                        it.LogicalKey,
                        it.StationNo,
                        it.Address,
                        it.DataType,
                        it.DataLength,
                        it.Enabled,
                        it.Sort,
                        it.Description,
                        it.UpdatedTime
                    })
                    .Where(it => it.LogicalKey == address.LogicalKey && it.StationNo == address.StationNo)
                    .ExecuteCommand();
            }
        }
    }

    /// <summary>
    /// 保存前统一清理用户输入，避免空格、空长度等小问题直接写入数据库。
    /// </summary>
    private static void NormalizeAddress(BizPlcAddress address)
    {
        address.DataType = AppConstants.PlcDataTypes.All.Contains(address.DataType) ? address.DataType : AppConstants.PlcDataTypes.Int16;
        address.DataLength = Math.Max(1, address.DataLength);
        address.Sort = Math.Max(0, address.Sort);
    }

    /// <summary>
    /// 只比较允许用户维护的字段。只有这些字段变更时才刷新更新时间。
    /// </summary>
    private static bool HasAddressChanged(BizPlcAddress oldAddress, BizPlcAddress newAddress)
    {
        return !SameText(oldAddress.LogicalKey, newAddress.LogicalKey)
            || oldAddress.StationNo != newAddress.StationNo
            || !SameText(oldAddress.Address, newAddress.Address)
            || !SameText(oldAddress.DataType, newAddress.DataType)
            || oldAddress.DataLength != newAddress.DataLength
            || oldAddress.Enabled != newAddress.Enabled
            || oldAddress.Sort != newAddress.Sort
            || !SameText(oldAddress.Description, newAddress.Description);
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    private static bool IsLogicalKey(string? logicalKey)
    {
        return logicalKey is AppConstants.PlcLogicalKeys.WorkId
            or AppConstants.PlcLogicalKeys.DeviceStatus
            or AppConstants.PlcLogicalKeys.PlcHeartBeat
            or AppConstants.PlcLogicalKeys.PcHeartBeat
            or AppConstants.PlcLogicalKeys.PcRecipeCode
            or AppConstants.PlcLogicalKeys.PlcRecipeCode
            or AppConstants.PlcLogicalKeys.WorkOrderStatus
            or AppConstants.PlcLogicalKeys.DeviceMode
            or AppConstants.PlcLogicalKeys.ProductDataReady
            or AppConstants.PlcLogicalKeys.ProductCollectionFeedback
            or AppConstants.PlcLogicalKeys.ProductResultFeedback
            or AppConstants.PlcLogicalKeys.TotalProduction
            or AppConstants.PlcLogicalKeys.AcceptedQuantity
            or AppConstants.PlcLogicalKeys.RejectedQuantity;
    }

    private static IReadOnlyList<int> ResolveSeedStationNumbers(bool dualStationEnabled)
    {
        return dualStationEnabled
            ? new[] { 1, 2 }
            : new[] { ProductionConstants.Stations.DefaultStationNo };
    }

    private void EnsureSeedData()
    {
        if (_seeded)
        {
            return;
        }

        _dbContext.InitDatabase();

        foreach (var address in BuildDefaultAddresses(_appSettings.EnableDualStation))
        {
            var exists = _dbContext.Db.Queryable<BizPlcAddress>()
                .Any(it => it.LogicalKey == address.LogicalKey && it.StationNo == address.StationNo);

            if (exists)
            {
                continue;
            }

            var stationCopy = TryCreateStationCopy(address);
            if (stationCopy is not null)
            {
                _dbContext.Db.Insertable(stationCopy).ExecuteCommand();
                continue;
            }

            _dbContext.Db.Insertable(address).ExecuteCommand();
        }

        _seeded = true;
    }

    /// <summary>
    /// 启用双工位后新增工位2地址时，优先复制工位1已有地址，避免用户重新录入同一套业务信号。
    /// </summary>
    private BizPlcAddress? TryCreateStationCopy(BizPlcAddress targetAddress)
    {
        if (!IsLogicalKey(targetAddress.LogicalKey)
            || targetAddress.StationNo <= ProductionConstants.Stations.DefaultStationNo)
        {
            return null;
        }

        var sourceAddress = _dbContext.Db.Queryable<BizPlcAddress>()
            .Where(it => it.LogicalKey == targetAddress.LogicalKey
                && it.StationNo == ProductionConstants.Stations.DefaultStationNo)
            .First();
        if (sourceAddress is null)
        {
            return null;
        }

        return new BizPlcAddress
        {
            LogicalKey = targetAddress.LogicalKey,
            StationNo = targetAddress.StationNo,
            AddressName = targetAddress.AddressName,
            Address = sourceAddress.Address,
            DataType = sourceAddress.DataType,
            DataLength = sourceAddress.DataLength,
            Enabled = sourceAddress.Enabled,
            Sort = targetAddress.Sort,
            Description = sourceAddress.Description,
            UpdatedTime = DateTime.Now
        };
    }

    private static IReadOnlyList<BizPlcAddress> BuildDefaultAddresses(bool dualStationEnabled)
    {
        var addresses = new List<BizPlcAddress>();
        var stationNumbers = ResolveSeedStationNumbers(dualStationEnabled);

        foreach (var stationNo in stationNumbers)
        {
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.PlcHeartBeat, stationNo, "PLC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 1, "PLC以0.5S为周期进行0/1翻转"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.PcHeartBeat, stationNo, "PC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 2, "PC以1s为周期进行0/1翻转"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.DeviceStatus, stationNo, "Device Status", AppConstants.PlcDataTypes.Int16, 1, 3, "1=运行，2=暂停/空闲，3=停止，4=报警."));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.WorkId, stationNo, "Work Order No", AppConstants.PlcDataTypes.String, 30, 4, "PC每秒读取"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.PcRecipeCode, stationNo, "PC Recipe Code", AppConstants.PlcDataTypes.Int16, 1, 5, "开工成功后写入 "));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.PlcRecipeCode, stationNo, "PLC Recipe Code", AppConstants.PlcDataTypes.Int16, 1, 6, "接收成功后写入"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.WorkOrderStatus, stationNo, "Work Order Status", AppConstants.PlcDataTypes.Int16, 1, 7, "1=开工状态/允许生产，2=完工状态/禁止生产"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.DeviceMode, stationNo, "Device Mode", AppConstants.PlcDataTypes.Int16, 1, 8, "1=单工位/双工位同工单，2=双工位双工单"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.ProductDataReady, stationNo, "Product Data Ready", AppConstants.PlcDataTypes.Int16, 1, 9, "1=数据就绪"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.ProductCollectionFeedback, stationNo, "Product Collection Feedback", AppConstants.PlcDataTypes.Int16, 1, 10, "1=采集成功"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.TotalProduction, stationNo, "Total Processed", AppConstants.PlcDataTypes.Int16, 1, 11, "PC以1s周期轮询"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.AcceptedQuantity, stationNo, "Accepted Quantity", AppConstants.PlcDataTypes.Int16, 1, 12, "PC以1s周期轮询"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.RejectedQuantity, stationNo, "Rejected Quantity", AppConstants.PlcDataTypes.Int16, 1, 13, "PC以1s周期轮询"));
            addresses.Add(CreateStation(AppConstants.PlcLogicalKeys.ProductResultFeedback, stationNo, "Product Result Feedback", AppConstants.PlcDataTypes.Int16, 1, 14, "整件检测产品判定结果：3=OK，2=NG"));
        }

        return addresses;
    }

    private static BizPlcAddress CreateStation(string logicalKey, int stationNo, string name, string dataType, int length, int sort, string description)
        => Create(sort, logicalKey, stationNo, name, dataType, length, description);

    private static BizPlcAddress Create(int sort, string logicalKey, int stationNo, string name, string dataType, int length, string description)
    {
        return new BizPlcAddress
        {
            Sort = sort,
            LogicalKey = logicalKey,
            StationNo = stationNo,
            AddressName = name,
            DataType = dataType,
            DataLength = length,
            Enabled = true,
            Description = description,
            UpdatedTime = DateTime.Now
        };
    }
}
