using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using System.Text.RegularExpressions;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Converts PLC business signal logical keys into user-facing names.
/// </summary>
public static class PlcBusinessSignalDisplayHelper
{
    private static readonly IReadOnlyDictionary<string, string> TextKeyByLogicalKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppConstants.PlcLogicalKeys.PcHeartBeat] = TextKeys.Address.NamePcHeartbeat,
            [AppConstants.PlcLogicalKeys.PlcHeartBeat] = TextKeys.Address.NamePlcHeartbeat,
            [AppConstants.PlcLogicalKeys.DeviceStatus] = TextKeys.Address.NameDeviceStatus,
            [AppConstants.PlcLogicalKeys.WorkId] = TextKeys.Address.NameWorkId,
            [AppConstants.PlcLogicalKeys.PcRecipeCode] = TextKeys.Address.NamePcRecipeCode,
            [AppConstants.PlcLogicalKeys.PlcRecipeCode] = TextKeys.Address.NamePlcRecipeCode,
            [AppConstants.PlcLogicalKeys.WorkOrderStatus] = TextKeys.Address.NameWorkOrderStatus,
            [AppConstants.PlcLogicalKeys.DeviceMode] = TextKeys.Address.NameDeviceMode,
            [AppConstants.PlcLogicalKeys.ProductDataReady] = TextKeys.Address.NameProductDataReady,
            [AppConstants.PlcLogicalKeys.ProductCollectionFeedback] = TextKeys.Address.NameProductCollectionFeedback,
            [AppConstants.PlcLogicalKeys.TotalProduction] = TextKeys.Address.NameTotalProduction,
            [AppConstants.PlcLogicalKeys.AcceptedQuantity] = TextKeys.Address.NameAcceptedQuantity,
            [AppConstants.PlcLogicalKeys.RejectedQuantity] = TextKeys.Address.NameRejectedQuantity,
            ["PcHeartBeat"] = TextKeys.Address.NamePcHeartbeat,
            ["PlcHeartBeat"] = TextKeys.Address.NamePlcHeartbeat,
            ["DeviceStatus"] = TextKeys.Address.NameDeviceStatus,
            ["WorkId"] = TextKeys.Address.NameWorkId,
            ["PcRecipeCode"] = TextKeys.Address.NamePcRecipeCode,
            ["PlcRecipeCode"] = TextKeys.Address.NamePlcRecipeCode,
            ["WorkOrderStatus"] = TextKeys.Address.NameWorkOrderStatus,
            ["DeviceMode"] = TextKeys.Address.NameDeviceMode,
            ["ProductDataReady"] = TextKeys.Address.NameProductDataReady,
            ["ProductCollectionFeedback"] = TextKeys.Address.NameProductCollectionFeedback,
            ["TotalProduction"] = TextKeys.Address.NameTotalProduction,
            ["AcceptedQuantity"] = TextKeys.Address.NameAcceptedQuantity,
            ["RejectedQuantity"] = TextKeys.Address.NameRejectedQuantity
        };

    /// <summary>
    /// Converts one PLC logical key to its localized display name.
    /// </summary>
    public static string FormatSignalName(string? logicalKey, ILocalizationService localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        var normalizedKey = logicalKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return "-";
        }

        return TextKeyByLogicalKey.TryGetValue(normalizedKey, out var textKey)
            ? localizer.GetString(textKey)
            : normalizedKey;
    }

    /// <summary>
    /// Replaces PLC logical keys embedded inside a text block with localized display names.
    /// </summary>
    public static string FormatSignalReferences(string? text, ILocalizationService localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var formatted = text.Trim();
        foreach (var pair in TextKeyByLogicalKey)
        {
            // Production-flow summaries can contain raw logical keys from old logs.
            formatted = ReplaceSignalReference(formatted, pair.Key, localizer.GetString(pair.Value));
        }

        return formatted;
    }

    /// <summary>
    /// Replaces a signal only when it appears as a complete PLC business field.
    /// </summary>
    private static string ReplaceSignalReference(string text, string signalName, string displayName)
    {
        var pattern = $@"(?<![A-Za-z0-9_]){Regex.Escape(signalName)}(?![A-Za-z0-9_])";
        return Regex.Replace(text, pattern, displayName, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
