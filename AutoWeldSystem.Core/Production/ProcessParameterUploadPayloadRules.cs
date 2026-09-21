using System.Text.Json;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Reads process-parameter upload-task scope fields from the task payload JSON.
/// </summary>
public static class ProcessParameterUploadPayloadRules
{
    /// <summary>
    /// Reads the scoped station number. A value less than or equal to zero means all stations.
    /// </summary>
    public static int ReadStationNo(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty("StationNo", out var stationElement)
                && stationElement.TryGetInt32(out var stationNo)
                    ? stationNo
                    : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Reads the scoped product numbers from new batch payloads and legacy single-product payloads.
    /// </summary>
    public static IReadOnlyList<string> ReadProductNos(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            var productNos = ReadProductNosArray(root);
            if (productNos.Count > 0)
            {
                return productNos;
            }

            return ProcessParameterBatchUploadRules.NormalizeProductNos(
                [
                    ReadString(root, "ProductNo"),
                    ReadString(root, "ProductNumber")
                ]);
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<string> ReadProductNosArray(JsonElement root)
    {
        if (!root.TryGetProperty("ProductNos", out var productNosElement)
            || productNosElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return ProcessParameterBatchUploadRules.NormalizeProductNos(
            productNosElement
                .EnumerateArray()
                .Select(element => element.ValueKind == JsonValueKind.String
                    ? element.GetString()
                    : element.ToString()));
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element)
            ? element.GetString()
            : null;
    }
}
