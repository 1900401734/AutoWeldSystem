using System.Text;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 提供 PLC 报警通知所需的消息拆分、去重和稳定签名规则。
/// </summary>
public static class PlcAlarmNotificationRules
{
    /// <summary>
    /// 判断当前快照是否需要展示设备报警通知。
    /// </summary>
    public static bool IsActive(bool softwareAlarmActive, bool alarmPendingConfirmation, bool rawAlarmUnconfirmed)
        => softwareAlarmActive || alarmPendingConfirmation || rawAlarmUnconfirmed;

    /// <summary>
    /// 将多个工位的报警快照聚合为整台设备唯一的报警通知状态。
    /// 报警地址属于整台设备而不属于某个程序工位，双工位会收到内容相同的报警快照，
    /// 因此通知、签名和已读状态都必须按设备聚合，避免同一条报警产生多张卡片、各自独立清除。
    /// </summary>
    public static PlcAlarmNotificationState Aggregate(IEnumerable<PlcAlarmNotificationInput> snapshots)
    {
        var inputs = snapshots.ToList();
        var hasAlarm = inputs.Any(input => IsActive(
            input.IsSoftwareAlarmActive,
            input.IsAlarmPendingConfirmation,
            input.IsRawAlarmUnconfirmed));
        if (!hasAlarm)
        {
            return PlcAlarmNotificationState.Inactive;
        }

        var messages = inputs
            .SelectMany(input => SplitMessages(input.SoftwareAlarmMessage))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (messages.Count == 0)
        {
            messages = [PlcSoftwareAlarmRules.GenericAlarmMessage];
        }

        // 任一工位已确认报警即视为设备报警；全部工位都只有原始状态 4 时才是等待确认。
        return new PlcAlarmNotificationState(
            true,
            !inputs.Any(input => input.IsSoftwareAlarmActive),
            messages);
    }

    /// <summary>
    /// 将 PLC 聚合报警文本拆分为可读的独立报警项，并保持首次出现顺序。
    /// </summary>
    public static IReadOnlyList<string> SplitMessages(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Array.Empty<string>();
        }

        return message
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split(['；', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 创建不受报警地址返回顺序影响的报警签名。
    /// </summary>
    public static string CreateSignature(IEnumerable<string> messages, bool pendingConfirmation)
    {
        var normalizedMessages = messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(message => message, StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder(pendingConfirmation ? "pending|" : "active|");
        foreach (var message in normalizedMessages)
        {
            builder.Append(message).Append('\u001F');
        }

        return builder.ToString();
    }

    /// <summary>
    /// 将报警项格式化为通知正文。
    /// </summary>
    public static string BuildDisplayText(IEnumerable<string> messages)
        => string.Join(Environment.NewLine, messages.Select((message, index) => $"{index + 1}.{message}"));
}

/// <summary>
/// 参与设备级报警聚合的单个工位快照字段。
/// </summary>
public sealed record PlcAlarmNotificationInput(
    bool IsSoftwareAlarmActive,
    bool IsAlarmPendingConfirmation,
    bool IsRawAlarmUnconfirmed,
    string? SoftwareAlarmMessage);

/// <summary>
/// 整台设备当前的报警通知状态。
/// </summary>
public sealed record PlcAlarmNotificationState(
    bool IsActive,
    bool PendingConfirmation,
    IReadOnlyList<string> Messages)
{
    public static PlcAlarmNotificationState Inactive { get; } = new(false, false, []);

    /// <summary>
    /// 当前报警集合的稳定签名；无报警时返回 null。
    /// </summary>
    public string? Signature => IsActive
        ? PlcAlarmNotificationRules.CreateSignature(Messages, PendingConfirmation)
        : null;
}
