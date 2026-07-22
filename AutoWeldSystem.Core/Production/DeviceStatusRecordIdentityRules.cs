using System.Text.Json;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 统一解析新 GUID、旧日志整数 Id 和上传任务中的设备状态记录键。
/// </summary>
public static class DeviceStatusRecordIdentityRules
{
    private const string LegacyPrefix = "legacy:";
    private const string BusinessPrefix = "device-status:";

    public static string? GetRecordKey(BizDeviceStatusLog? log)
    {
        if (log is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(log.RecordId))
        {
            return NormalizeRecordKey(log.RecordId);
        }

        return log.Id > 0 ? $"{LegacyPrefix}{log.Id}" : null;
    }

    public static string? NormalizeRecordKey(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(normalized[LegacyPrefix.Length..], out var legacyId) && legacyId > 0
                ? $"{LegacyPrefix}{legacyId}"
                : null;
        }

        return Guid.TryParse(normalized, out var recordId)
            ? recordId.ToString("N")
            : null;
    }

    public static string BuildBusinessId(string recordKey)
    {
        var normalized = NormalizeRecordKey(recordKey)
            ?? throw new ArgumentException("设备状态记录键无效。", nameof(recordKey));
        return $"{BusinessPrefix}{normalized}";
    }

    public static IReadOnlyList<string> GetCompatibleBusinessIds(string recordKey)
    {
        var normalized = NormalizeRecordKey(recordKey)
            ?? throw new ArgumentException("设备状态记录键无效。", nameof(recordKey));
        var values = new List<string> { $"{BusinessPrefix}{normalized}" };
        if (TryGetLegacyId(normalized, out var legacyId))
        {
            values.Add($"{BusinessPrefix}{legacyId}");
        }

        return values;
    }

    public static string? ReadTaskRecordKey(string? businessId, string? payloadJson)
    {
        var payloadKey = ReadPayloadRecordKey(payloadJson);
        if (payloadKey is not null)
        {
            return payloadKey;
        }

        var normalizedBusinessId = businessId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBusinessId)
            || !normalizedBusinessId.StartsWith(BusinessPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = normalizedBusinessId[BusinessPrefix.Length..];
        if (int.TryParse(suffix, out var legacyId) && legacyId > 0)
        {
            return $"{LegacyPrefix}{legacyId}";
        }

        return NormalizeRecordKey(suffix);
    }

    private static string? ReadPayloadRecordKey(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("RecordKey", out var recordKeyElement)
                && recordKeyElement.ValueKind == JsonValueKind.String)
            {
                var recordKey = NormalizeRecordKey(recordKeyElement.GetString());
                if (recordKey is not null)
                {
                    return recordKey;
                }
            }

            return root.TryGetProperty("LogId", out var logIdElement)
                && logIdElement.TryGetInt32(out var logId)
                && logId > 0
                    ? $"{LegacyPrefix}{logId}"
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetLegacyId(string recordKey, out int legacyId)
    {
        legacyId = 0;
        return recordKey.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(recordKey[LegacyPrefix.Length..], out legacyId)
            && legacyId > 0;
    }
}
