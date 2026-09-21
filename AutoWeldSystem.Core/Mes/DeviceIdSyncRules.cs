namespace AutoWeldSystem.Core.Mes;

/// <summary>
/// 设备编号同步失败后的安全降级规则。
/// 仅在 MES 明确表示旧设备不存在时，允许由用户确认后改为新设备注册。
/// </summary>
public static class DeviceIdSyncRules
{
    private static readonly string[] MissingDeviceMessages =
    [
        "设备不存在",
        "device not found",
        "device does not exist"
    ];

    public static bool ShouldOfferRegisterAsNew(string? oldDeviceId, string? responseMessage)
    {
        if (string.IsNullOrWhiteSpace(oldDeviceId) || string.IsNullOrWhiteSpace(responseMessage))
        {
            return false;
        }

        var normalizedMessage = responseMessage.Trim();
        return MissingDeviceMessages.Any(message =>
            normalizedMessage.Contains(message, StringComparison.OrdinalIgnoreCase));
    }
}
