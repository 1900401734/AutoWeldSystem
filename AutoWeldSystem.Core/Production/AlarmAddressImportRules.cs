using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Parses PLC alarm address rows copied from Excel or engineering documents.
/// </summary>
public static class AlarmAddressImportRules
{
    private static readonly Regex DbnBitAddressRegex = new(
        @"DBnBit-\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SiemensDbxAddressRegex = new(
        @"\bDB\s*(\d+)\s*\.\s*DBX\s*(\d+)\s*\.\s*([0-7])\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SiemensBitAddressRegex = new(
        @"\bDB\s*(\d+)\s*\.\s*(\d+)\s*\.\s*([0-7])\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StationTokenRegex = new(
        @"\bST\s*([1-9]\d*)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses clipboard text into alarm address rows without dropping duplicated source rows.
    /// </summary>
    public static IReadOnlyList<AlarmAddressImportRow> ParseClipboard(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var rows = new List<AlarmAddressImportRow>();
        var header = AlarmAddressImportHeader.Empty;
        foreach (var rawLine in SplitRows(text))
        {
            var cells = ParseCells(rawLine);
            if (cells.Count == 0)
            {
                continue;
            }

            if (TryUpdateHeader(cells, out var newHeader))
            {
                header = newHeader;
                continue;
            }

            if (TryParseRow(cells, header, out var row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    /// <summary>
    /// Normalizes one PLC alarm address. Unknown formats are returned trimmed.
    /// </summary>
    public static string NormalizeAddress(string? address)
    {
        var text = CleanCell(address);
        return TryNormalizeAddress(text, out var normalized)
            ? normalized
            : text;
    }

    private static bool TryParseRow(
        IReadOnlyList<string> cells,
        AlarmAddressImportHeader header,
        out AlarmAddressImportRow row)
    {
        row = default!;
        if (!TryFindAddress(cells, out var addressIndex, out var address))
        {
            return false;
        }

        var content = ResolveContent(cells, addressIndex, header);
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        row = new AlarmAddressImportRow(InferStationNo(address, content), address, content);
        return true;
    }

    private static string ResolveContent(
        IReadOnlyList<string> cells,
        int addressIndex,
        AlarmAddressImportHeader header)
    {
        if (header.ContentColumnIndex >= 0
            && header.ContentColumnIndex < cells.Count
            && header.ContentColumnIndex != addressIndex)
        {
            var headerContent = CleanContent(cells[header.ContentColumnIndex]);
            if (IsContentValue(headerContent))
            {
                return headerContent;
            }
        }

        var afterAddress = cells
            .Skip(addressIndex + 1)
            .Select(CleanContent)
            .Where(IsContentValue)
            .ToList();
        if (afterAddress.Count > 0)
        {
            return string.Join("\uff0c", afterAddress);
        }

        for (var index = addressIndex - 1; index >= 0; index--)
        {
            var candidate = CleanContent(cells[index]);
            if (IsContentValue(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static int InferStationNo(string address, string content)
    {
        var text = content.Trim();
        var stationMatch = StationTokenRegex.Match(text);
        if (stationMatch.Success
            && int.TryParse(stationMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var stationFromText))
        {
            return stationFromText;
        }

        if (StartsWithOrContains(text, "\u5de6", "\u5de6\u5de5\u4f4d"))
        {
            return 1;
        }

        if (StartsWithOrContains(text, "\u53f3", "\u53f3\u5de5\u4f4d"))
        {
            return 2;
        }

        return TryGetDbNumber(address, out var dbNumber) && dbNumber is >= 11 and <= 19
            ? dbNumber - 10
            : 0;
    }

    private static bool StartsWithOrContains(string text, string prefix, string explicitToken)
    {
        return text.StartsWith(prefix, StringComparison.Ordinal)
            || text.Contains(explicitToken, StringComparison.Ordinal);
    }

    private static bool TryUpdateHeader(IReadOnlyList<string> cells, out AlarmAddressImportHeader header)
    {
        header = AlarmAddressImportHeader.Empty;
        var addressColumnIndex = -1;
        var contentColumnIndex = -1;
        for (var index = 0; index < cells.Count; index++)
        {
            var cell = CleanCell(cells[index]);
            if (IsAddressHeader(cell))
            {
                addressColumnIndex = index;
            }
            else if (IsContentHeader(cell))
            {
                contentColumnIndex = index;
            }
        }

        if (addressColumnIndex < 0 || contentColumnIndex < 0)
        {
            return false;
        }

        header = new AlarmAddressImportHeader(addressColumnIndex, contentColumnIndex);
        return true;
    }

    private static bool TryFindAddress(IReadOnlyList<string> cells, out int addressIndex, out string address)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            if (TryNormalizeAddress(cells[index], out address))
            {
                addressIndex = index;
                return true;
            }
        }

        addressIndex = -1;
        address = string.Empty;
        return false;
    }

    private static bool TryNormalizeAddress(string? value, out string normalized)
    {
        var text = CleanCell(value);
        if (TryConvertDbnBitAddress(text, out normalized)
            || TryConvertSiemensDbxAddress(text, out normalized)
            || TryConvertSiemensBitAddress(text, out normalized))
        {
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    private static bool TryConvertDbnBitAddress(string text, out string converted)
    {
        converted = string.Empty;
        var match = DbnBitAddressRegex.Match(text);
        if (!match.Success)
        {
            return false;
        }

        var digits = match.Groups[1].Value;
        if (digits.Length < 6)
        {
            return false;
        }

        var dbText = digits[..^5];
        var byteText = digits.Substring(digits.Length - 5, 4);
        var bitText = digits[^1..];
        if (!int.TryParse(dbText, NumberStyles.None, CultureInfo.InvariantCulture, out var dbNumber)
            || !int.TryParse(byteText, NumberStyles.None, CultureInfo.InvariantCulture, out var byteOffset)
            || !int.TryParse(bitText, NumberStyles.None, CultureInfo.InvariantCulture, out var bitOffset)
            || dbNumber <= 0
            || bitOffset is < 0 or > 7)
        {
            return false;
        }

        converted = $"DB{dbNumber}.{byteOffset}.{bitOffset}";
        return true;
    }

    private static bool TryConvertSiemensDbxAddress(string text, out string converted)
    {
        var match = SiemensDbxAddressRegex.Match(text);
        return TryConvertSiemensMatch(match, out converted);
    }

    private static bool TryConvertSiemensBitAddress(string text, out string converted)
    {
        var match = SiemensBitAddressRegex.Match(text);
        return TryConvertSiemensMatch(match, out converted);
    }

    private static bool TryConvertSiemensMatch(Match match, out string converted)
    {
        converted = string.Empty;
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var dbNumber)
            || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var byteOffset)
            || !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var bitOffset)
            || dbNumber <= 0)
        {
            return false;
        }

        converted = $"DB{dbNumber}.{byteOffset}.{bitOffset}";
        return true;
    }

    private static bool TryGetDbNumber(string address, out int dbNumber)
    {
        dbNumber = 0;
        var match = Regex.Match(address, @"^DB(\d+)\.", RegexOptions.IgnoreCase);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out dbNumber);
    }

    private static bool IsContentValue(string text)
    {
        return !string.IsNullOrWhiteSpace(text)
            && !IsSequenceCell(text)
            && !IsAddressHeader(text)
            && !IsContentHeader(text)
            && !TryNormalizeAddress(text, out _);
    }

    private static bool IsSequenceCell(string text)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsAddressHeader(string text)
    {
        var value = text.Trim().ToLowerInvariant();
        return value.Contains("\u5730\u5740", StringComparison.Ordinal)
            || value is "address" or "plc address"
            || value.Contains("read address", StringComparison.Ordinal);
    }

    private static bool IsContentHeader(string text)
    {
        var value = text.Trim().ToLowerInvariant();
        return value is "\u5185\u5bb9"
            or "\u62a5\u8b66\u5185\u5bb9"
            or "\u6545\u969c\u5185\u5bb9"
            or "\u8bf4\u660e"
            or "\u63cf\u8ff0"
            or "content"
            or "message"
            or "msg";
    }

    private static IReadOnlyList<string> ParseCells(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        if (trimmed.Contains('\t'))
        {
            return trimmed.Split('\t')
                .Select(CleanCell)
                .ToList();
        }

        if (trimmed.Contains(','))
        {
            return ParseCsvLine(trimmed)
                .Select(CleanCell)
                .ToList();
        }

        return Regex.Split(trimmed, @"\s{2,}")
            .Select(CleanCell)
            .Where(cell => cell.Length > 0)
            .ToList();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static IEnumerable<string> SplitRows(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);
    }

    private static string CleanContent(string? value)
    {
        return CleanCell(value).Trim();
    }

    private static string CleanCell(string? value)
    {
        return (value ?? string.Empty).Trim().Trim('"').Trim();
    }

    private readonly record struct AlarmAddressImportHeader(int AddressColumnIndex, int ContentColumnIndex)
    {
        public static AlarmAddressImportHeader Empty { get; } = new(-1, -1);
    }
}

/// <summary>
/// One imported PLC alarm address row.
/// </summary>
public sealed record AlarmAddressImportRow(int StationNo, string Address, string Content);
