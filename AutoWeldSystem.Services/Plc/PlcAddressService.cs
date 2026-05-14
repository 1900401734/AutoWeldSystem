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
    private readonly object _dbLock = new();

    public PlcAddressService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<BizPlcAddress> GetAll()
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            return _dbContext.Db.Queryable<BizPlcAddress>()
                .OrderBy(it => it.Sort)
                .ToList()
                .OrderBy(it => it.Sort)
                .ThenBy(it => it.AddressKey)
                .ToList();
        }
    }

    public BizPlcAddress? GetByKey(string addressKey)
    {
        lock (_dbLock)
        {
            EnsureSeedData();

            return _dbContext.Db.Queryable<BizPlcAddress>()
                .First(it => it.AddressKey == addressKey);
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
        return !SameText(oldAddress.Address, newAddress.Address)
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

    private void EnsureSeedData()
    {
        _dbContext.InitDatabase();

        foreach (var address in BuildDefaultAddresses())
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

    private static IReadOnlyList<BizPlcAddress> BuildDefaultAddresses()
    {
        return new[]
        {
            Create(AppConstants.PlcAddressKeys.PlcHeartBeat, "PLC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 10, "Read this address to confirm PLC communication."),
            Create(AppConstants.PlcAddressKeys.PcHeartBeat, "PC Heartbeat", AppConstants.PlcDataTypes.Int16, 1, 20, "Optional address written by PC to notify PLC that software is alive."),
            Create(AppConstants.PlcAddressKeys.DeviceStatus, "Device Status", AppConstants.PlcDataTypes.Int16, 1, 30, "Device running status address."),
            Create(AppConstants.PlcAddressKeys.WeldStart, "Weld Start", AppConstants.PlcDataTypes.Bool, 1, 40, "Welding start signal."),
            Create(AppConstants.PlcAddressKeys.WeldEnd, "Weld End", AppConstants.PlcDataTypes.Bool, 1, 50, "Welding end signal."),
            Create(AppConstants.PlcAddressKeys.WorkId, "Work Order No", AppConstants.PlcDataTypes.String, 32, 60, "Work order number address."),
            Create(AppConstants.PlcAddressKeys.ProgramName, "Program Name", AppConstants.PlcDataTypes.String, 32, 70, "Program name address."),
            Create(AppConstants.PlcAddressKeys.ProductModel, "Product Model", AppConstants.PlcDataTypes.String, 32, 80, "Product model address."),
            Create(AppConstants.PlcAddressKeys.TotalProduction, "Total Processed", AppConstants.PlcDataTypes.Int32, 1, 90, "Total processed counter."),
            Create(AppConstants.PlcAddressKeys.TargetProduction, "Target Production", AppConstants.PlcDataTypes.Int32, 1, 100, "Target production counter."),
            Create(AppConstants.PlcAddressKeys.AcceptedQuantity, "Accepted Quantity", AppConstants.PlcDataTypes.Int32, 1, 110, "Accepted quantity counter."),
            Create(AppConstants.PlcAddressKeys.RejectedQuantity, "Rejected Quantity", AppConstants.PlcDataTypes.Int32, 1, 120, "Rejected quantity counter.")
        };
    }

    private static BizPlcAddress Create(
        string key,
        string name,
        string dataType,
        int length,
        int sort,
        string description)
    {
        return new BizPlcAddress
        {
            AddressKey = key,
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
