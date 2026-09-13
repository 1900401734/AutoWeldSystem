using System.Text.Json;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.Center;

/// <summary>中心只传输设备已经判定的有效报警，不用原始 PLC 码重新解释触发模式。</summary>
public static class CenterAlarmRules
{
    // JSON 默认会转义中文；8000 个 UTF-16 字符仍可完整存入 MySQL TEXT。
    public const int MaxMessageLength = 8000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CenterEffectiveAlarmDto FromProductionSnapshot(PlcProductionSnapshot snapshot)
        => Normalize(new CenterEffectiveAlarmDto
        {
            IsActive = snapshot.IsSoftwareAlarmActive,
            IsPendingConfirmation = snapshot.IsAlarmPendingConfirmation,
            IsRawAlarmUnconfirmed = snapshot.IsRawAlarmUnconfirmed,
            Message = snapshot.SoftwareAlarmMessage
        });

    public static CenterEffectiveAlarmDto Normalize(CenterEffectiveAlarmDto alarm)
    {
        var active = alarm.IsActive;
        var pending = !active && alarm.IsPendingConfirmation;
        var rawUnconfirmed = !active && !pending && alarm.IsRawAlarmUnconfirmed;
        var messages = active || pending || rawUnconfirmed
            ? PlcAlarmNotificationRules.SplitMessages(alarm.Message)
                .OrderBy(message => message, StringComparer.OrdinalIgnoreCase)
            : Enumerable.Empty<string>();
        return new CenterEffectiveAlarmDto
        {
            IsActive = active,
            IsPendingConfirmation = pending,
            IsRawAlarmUnconfirmed = rawUnconfirmed,
            Message = string.Join("；", messages)
        };
    }

    public static string? Serialize(CenterEffectiveAlarmDto? alarm)
        => alarm is null ? null : JsonSerializer.Serialize(Normalize(alarm), JsonOptions);

    /// <summary>损坏的存储值报告给调用方记录，返回 null 走旧版兼容，不中断整个看板。</summary>
    public static CenterEffectiveAlarmDto? Deserialize(string? json, Action<Exception>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var alarm = JsonSerializer.Deserialize<CenterEffectiveAlarmDto>(json, JsonOptions);
            if (alarm?.Message?.Length > MaxMessageLength)
                throw new JsonException("有效报警文本超过允许长度。");
            return alarm is null ? null : Normalize(alarm);
        }
        catch (JsonException ex)
        {
            onError?.Invoke(ex);
            return null;
        }
    }
}
