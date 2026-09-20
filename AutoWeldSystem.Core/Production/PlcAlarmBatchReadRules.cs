using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 把分散的 PLC Bool 报警地址规划为尽量少的批量读取段。
/// 西门子 DB 位地址（DB{n}.{byte}.{bit}）按 DB 块合并为连续字节段，一段一次往返读取后按位提取；
/// 无法解析为 DB 位的地址（例如 Modbus 线圈）保留逐个读取。
/// </summary>
public static class PlcAlarmBatchReadRules
{
    /// <summary>
    /// 默认单段最大字节数。S7-1200 单个 PDU 为 240 字节，扣除报文头后按 200 字节分段可保证一段只需一次往返。
    /// </summary>
    public const int DefaultMaxSegmentBytes = 200;

    private static readonly Regex SiemensBitAddressRegex = new(
        @"^DB(\d+)\.(\d+)\.([0-7])$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 解析 AlarmAddressImportRules 归一化后的西门子位地址。
    /// </summary>
    public static bool TryParseSiemensBit(string? address, out int dbNumber, out int byteOffset, out int bit)
    {
        dbNumber = 0;
        byteOffset = 0;
        bit = 0;
        var text = address?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var match = SiemensBitAddressRegex.Match(text);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out dbNumber)
            && int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out byteOffset)
            && int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out bit)
            && dbNumber > 0;
    }

    /// <summary>
    /// 生成读取计划：同一 DB 块内允许跨越未配置的字节合并，减少往返次数；单段跨度超过上限时开新段。
    /// </summary>
    public static PlcAlarmReadPlan Plan(IEnumerable<string> addresses, int maxSegmentBytes = DefaultMaxSegmentBytes)
    {
        var segmentLimit = Math.Clamp(maxSegmentBytes, 1, ushort.MaxValue);
        var singleAddresses = new List<string>();
        var bitsByDb = new SortedDictionary<int, List<PlcAlarmSegmentBit>>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawAddress in addresses)
        {
            var address = rawAddress?.Trim() ?? string.Empty;
            if (address.Length == 0 || !seen.Add(address))
            {
                continue;
            }

            if (!TryParseSiemensBit(address, out var dbNumber, out var byteOffset, out var bit))
            {
                singleAddresses.Add(address);
                continue;
            }

            if (!bitsByDb.TryGetValue(dbNumber, out var bits))
            {
                bits = [];
                bitsByDb[dbNumber] = bits;
            }

            bits.Add(new PlcAlarmSegmentBit(address, byteOffset, bit));
        }

        var segments = new List<PlcAlarmReadSegment>();
        foreach (var (dbNumber, bits) in bitsByDb)
        {
            var ordered = bits
                .OrderBy(location => location.ByteOffset)
                .ThenBy(location => location.Bit)
                .ToList();
            var segmentStart = ordered[0].ByteOffset;
            var current = new List<PlcAlarmSegmentBit>();
            foreach (var location in ordered)
            {
                if (current.Count > 0 && location.ByteOffset - segmentStart + 1 > segmentLimit)
                {
                    segments.Add(CreateSegment(dbNumber, segmentStart, current));
                    current = [];
                    segmentStart = location.ByteOffset;
                }

                current.Add(location);
            }

            if (current.Count > 0)
            {
                segments.Add(CreateSegment(dbNumber, segmentStart, current));
            }
        }

        return new PlcAlarmReadPlan(segments, singleAddresses);
    }

    /// <summary>
    /// 从一段原始字节中提取指定位；字节不足时返回 false，交给调用方回退逐地址读取。
    /// </summary>
    public static bool TryExtractBit(byte[]? data, int byteOffset, int bit, out bool value)
    {
        value = false;
        if (data is null || byteOffset < 0 || byteOffset >= data.Length || bit is < 0 or > 7)
        {
            return false;
        }

        // S7 位序与 HslCommunication ReadBool 一致：bit 0 为字节最低位。
        value = (data[byteOffset] & (1 << bit)) != 0;
        return true;
    }

    private static PlcAlarmReadSegment CreateSegment(int dbNumber, int startByte, List<PlcAlarmSegmentBit> bits)
    {
        var length = bits[^1].ByteOffset - startByte + 1;
        return new PlcAlarmReadSegment(
            $"DB{dbNumber}.{startByte}",
            (ushort)length,
            bits.Select(location => location with { ByteOffset = location.ByteOffset - startByte }).ToList());
    }
}

/// <summary>
/// 段内的单个报警位；ByteOffset 为相对段起始字节的偏移。
/// </summary>
public sealed record PlcAlarmSegmentBit(string Address, int ByteOffset, int Bit);

/// <summary>
/// 一次批量读取的连续字节段。
/// </summary>
public sealed record PlcAlarmReadSegment(string StartAddress, ushort Length, IReadOnlyList<PlcAlarmSegmentBit> Bits);

/// <summary>
/// 一轮报警读取计划：可合并的字节段与必须逐个读取的地址。
/// </summary>
public sealed record PlcAlarmReadPlan(IReadOnlyList<PlcAlarmReadSegment> Segments, IReadOnlyList<string> SingleAddresses);
