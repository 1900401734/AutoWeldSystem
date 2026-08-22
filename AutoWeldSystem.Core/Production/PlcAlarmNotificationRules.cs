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
