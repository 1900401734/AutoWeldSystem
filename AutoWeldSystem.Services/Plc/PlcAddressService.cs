using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 地址配置服务实现。
/// 默认地址用途由程序维护，用户只在界面填写现场实际 PLC 地址。
/// </summary>
public class PlcAddressService : IPlcAddressService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly object _dbLock = new();

    public PlcAddressService(SqlSugarDbContext dbContext, IAppSettingsService settingsService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
    }

    public IReadOnlyList<BizPlcAddress> GetAll()
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var dualStationEnabled = IsDualStationModeEnabled();
            return _dbContext.Db.Queryable<BizPlcAddress>()
                .OrderBy(it => it.Sort)
                .ToList()
                .Where(address => dualStationEnabled
                    || address.StationNo <= ProductionConstants.Stations.DefaultStationNo)
                .OrderBy(it => it.Sort)
                .ThenBy(it => it.StationNo)
                .ThenBy(it => it.AddressKey)
                .ToList();
        }
    }

    public BizPlcAddress? GetByKey(string addressKey)
        => GetByKey(addressKey, ProductionConstants.Stations.DefaultStationNo);

    public BizPlcAddress? GetByKey(string logicalKey, int stationNo)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            var normalizedLogicalKey = NormalizeLogicalKey(logicalKey);
            var normalizedStationNo = IsDualStationModeEnabled()
                ? NormalizeStationNo(stationNo)
                : ProductionConstants.Stations.DefaultStationNo;
            var addresses = _dbContext.Db.Queryable<BizPlcAddress>()
                .Where(it => it.AddressKey == normalizedLogicalKey || it.LogicalKey == normalizedLogicalKey)
                .ToList()
                .Where(it => it.StationNo == normalizedStationNo
                    || it.StationNo == ProductionConstants.Stations.SharedStationNo)
                .ToList();

            return addresses
                .OrderByDescending(it => it.StationNo == normalizedStationNo)
                .ThenByDescending(it => it.StationNo == ProductionConstants.Stations.SharedStationNo)
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

                var storedAddress = _dbContext.Db.Queryable<BizPlcAddress>()
                    .First(it => it.AddressKey == address.AddressKey);
            if (storedAddress is null)
            {
                if (IsLegacySerialNumberAddress(address))
                {
                    continue;
                }

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
                    .Where(it => it.AddressKey == address.AddressKey)
                    .ExecuteCommand();
            }
        }
    }

    /// <summary>
    /// 保存前统一清理用户输入，避免空格、空长度等小问题直接写入数据库。
    /// </summary>
    private static void NormalizeAddress(BizPlcAddress address)
    {
        address.LogicalKey = NormalizeLogicalKey(string.IsNullOrWhiteSpace(address.LogicalKey)
            ? address.AddressKey
            : address.LogicalKey);
        address.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, address.StationNo);
        address.Address = NormalizeNullableText(address.Address);
        address.DataType = AppConstants.PlcDataTypes.All.Contains(address.DataType)
            ? address.DataType
            : AppConstants.PlcDataTypes.Int16;
        address.DataLength = Math.Max(1, address.DataLength);
        address.Sort = Math.Max(0, address.Sort);
        address.Description = NormalizeNullableText(address.Description);
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

    private static string? NormalizeNullableText(string? text)
    {
        var normalizedText = text?.Trim();
        return string.IsNullOrWhiteSpace(normalizedText)
            ? null
            : normalizedText;
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(
            NormalizeNullableText(left),
            NormalizeNullableText(right),
            StringComparison.Ordinal);
    }

    private static string NormalizeLogicalKey(string? logicalKey)
    {
        var normalizedKey = logicalKey?.Trim();
        return string.IsNullOrWhiteSpace(normalizedKey)
            ? string.Empty
            : normalizedKey;
    }

    private static string BuildStationAddressKey(string logicalKey, int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.DefaultStationNo
            ? logicalKey
            : $"{logicalKey}_s{stationNo}";
    }

    private static bool IsStationSpecificLogicalKey(string? logicalKey)
    {
        return logicalKey is AppConstants.PlcAddressKeys.WeldStart
            or AppConstants.PlcAddressKeys.WeldEnd
            or AppConstants.PlcAddressKeys.WeldCollectionAck
            or AppConstants.PlcAddressKeys.WorkId
            or AppConstants.PlcAddressKeys.ProductNum
            or AppConstants.PlcAddressKeys.ProgramName
            or AppConstants.PlcAddressKeys.ProductModel
            or AppConstants.PlcAddressKeys.RecipeCode
            or AppConstants.PlcAddressKeys.TotalProduction
            or AppConstants.PlcAddressKeys.TargetProduction
            or AppConstants.PlcAddressKeys.AcceptedQuantity
            or AppConstants.PlcAddressKeys.RejectedQuantity;
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private void EnsureSeedData()
    {
        _dbContext.InitDatabase();
        MigrateStationColumns();

        foreach (var address in BuildDefaultAddresses(IsDualStationModeEnabled()))
        {
            var exists = _dbContext.Db.Queryable<BizPlcAddress>()
                .Any(it => it.AddressKey == address.AddressKey);

            if (exists)
            {
                continue;
            }

            _dbContext.Db.Insertable(address).ExecuteCommand();
        }

        MigrateAndRemoveLegacySerialNumberAddress();
    }

    /// <summary>
    /// Existing databases only had AddressKey. Fill the new station fields without changing user-entered PLC addresses.
    /// </summary>
    private void MigrateStationColumns()
    {
        var addresses = _dbContext.Db.Queryable<BizPlcAddress>().ToList();
        foreach (var address in addresses)
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(address.LogicalKey))
            {
                address.LogicalKey = address.AddressKey;
                changed = true;
            }

            if (address.StationNo <= 0 && IsStationSpecificLogicalKey(address.LogicalKey))
            {
                address.StationNo = ProductionConstants.Stations.DefaultStationNo;
                changed = true;
            }

            if (address.StationNo < 0)
            {
                address.StationNo = ProductionConstants.Stations.SharedStationNo;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            _dbContext.Db.Updateable(address)
                .UpdateColumns(it => new { it.LogicalKey, it.StationNo })
                .Where(it => it.AddressKey == address.AddressKey)
                .ExecuteCommand();
        }
    }

    private static IReadOnlyList<BizPlcAddress> BuildDefaultAddresses(bool dualStationEnabled)
    {
        var addresses = new List<BizPlcAddress>
        {
            CreateShared(AppConstants.PlcAddressKeys.PlcHeartBeat, "PLC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 10, "Read this address to confirm PLC communication."),
            CreateShared(AppConstants.PlcAddressKeys.PcHeartBeat, "PC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 20, "Optional address written by PC to notify PLC that software is alive."),
            CreateShared(AppConstants.PlcAddressKeys.DeviceStatus, "Device Status", AppConstants.PlcDataTypes.Int16, 1, 30, "Device running status address.")
        };

        var stationNumbers = dualStationEnabled
            ? new[] { 1, 2 }
            : new[] { ProductionConstants.Stations.DefaultStationNo };

        foreach (var stationNo in stationNumbers)
        {
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.WeldStart, stationNo, "Weld Start", AppConstants.PlcDataTypes.Bool, 1, 40, "Welding start signal."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.WeldEnd, stationNo, "Weld End", AppConstants.PlcDataTypes.Bool, 1, 50, "Welding end signal."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.WeldCollectionAck, stationNo, "Collection Ack", AppConstants.PlcDataTypes.Bool, 1, 55, "PC writes a short pulse after weld point data is saved locally."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.WorkId, stationNo, "Work Order No", AppConstants.PlcDataTypes.String, 32, 60, "Work order number address."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.ProductNum, stationNo, "Product No", AppConstants.PlcDataTypes.String, 32, 70, "Product number address used for offline production."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.ProgramName, stationNo, "Program Name", AppConstants.PlcDataTypes.String, 32, 80, "Program name address."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.ProductModel, stationNo, "Product Model", AppConstants.PlcDataTypes.String, 32, 90, "Product model address."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.RecipeCode, stationNo, "Recipe Code", AppConstants.PlcDataTypes.String, 32, 95, "Recipe code address used to verify PLC and software use the same recipe before start."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.TotalProduction, stationNo, "Total Processed", AppConstants.PlcDataTypes.Int32, 1, 100, "Total processed counter."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.TargetProduction, stationNo, "Target Production", AppConstants.PlcDataTypes.Int32, 1, 110, "Target production counter."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.AcceptedQuantity, stationNo, "Accepted Quantity", AppConstants.PlcDataTypes.Int32, 1, 120, "Accepted quantity counter."));
            addresses.Add(CreateStation(AppConstants.PlcAddressKeys.RejectedQuantity, stationNo, "Rejected Quantity", AppConstants.PlcDataTypes.Int32, 1, 130, "Rejected quantity counter."));
        }

        return addresses;
    }

    private bool IsDualStationModeEnabled()
    {
        return _settingsService.Get().EnableDualStationMode;
    }

    private static BizPlcAddress CreateShared(
        string logicalKey,
        string name,
        string dataType,
        int length,
        int sort,
        string description)
        => Create(logicalKey, logicalKey, ProductionConstants.Stations.SharedStationNo, name, dataType, length, sort, description);

    private static BizPlcAddress CreateStation(
        string logicalKey,
        int stationNo,
        string name,
        string dataType,
        int length,
        int sort,
        string description)
        => Create(BuildStationAddressKey(logicalKey, stationNo), logicalKey, stationNo, name, dataType, length, sort, description);

    private static BizPlcAddress Create(
        string key,
        string logicalKey,
        int stationNo,
        string name,
        string dataType,
        int length,
        int sort,
        string description)
    {
        return new BizPlcAddress
        {
            AddressKey = key,
            LogicalKey = logicalKey,
            StationNo = stationNo,
            AddressName = name,
            DataType = dataType,
            DataLength = length,
            Enabled = true,
            Sort = sort,
            Description = description,
            UpdatedTime = DateTime.Now
        };
    }

    /// <summary>
    /// Moves an old serial_number configuration into work_id once, then removes the duplicate row.
    /// </summary>
    private void MigrateAndRemoveLegacySerialNumberAddress()
    {
        var legacyAddress = _dbContext.Db.Queryable<BizPlcAddress>()
            .First(it => it.AddressKey == AppConstants.PlcAddressKeys.LegacySerialNumber);
        if (legacyAddress is null)
        {
            return;
        }

        var workIdAddress = _dbContext.Db.Queryable<BizPlcAddress>()
            .First(it => it.AddressKey == AppConstants.PlcAddressKeys.WorkId);
        if (workIdAddress is not null && string.IsNullOrWhiteSpace(workIdAddress.Address) && !string.IsNullOrWhiteSpace(legacyAddress.Address))
        {
            workIdAddress.Address = legacyAddress.Address;
            workIdAddress.DataType = legacyAddress.DataType;
            workIdAddress.DataLength = legacyAddress.DataLength;
            workIdAddress.Enabled = legacyAddress.Enabled;
            workIdAddress.Description = legacyAddress.Description;
            workIdAddress.UpdatedTime = DateTime.Now;

            _dbContext.Db.Updateable(workIdAddress)
                .UpdateColumns(it => new
                {
                    it.Address,
                    it.DataType,
                    it.DataLength,
                    it.Enabled,
                    it.Description,
                    it.UpdatedTime
                })
                .Where(it => it.AddressKey == AppConstants.PlcAddressKeys.WorkId)
                .ExecuteCommand();
        }

        _dbContext.Db.Deleteable<BizPlcAddress>()
            .Where(it => it.AddressKey == AppConstants.PlcAddressKeys.LegacySerialNumber)
            .ExecuteCommand();
    }

    private static bool IsLegacySerialNumberAddress(BizPlcAddress address)
    {
        return string.Equals(address.AddressKey, AppConstants.PlcAddressKeys.LegacySerialNumber, StringComparison.OrdinalIgnoreCase);
    }
}
