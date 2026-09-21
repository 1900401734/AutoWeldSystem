using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Centralizes PLC test-result code conversion so collection, preview, history, report, and MES paths stay consistent.
/// </summary>
public static class TestResultRules
{
    /// <summary>
    /// Converts a PLC raw result code or existing display text to the local canonical result text.
    /// </summary>
    /// <param name="rawResult">PLC raw code or result text.</param>
    /// <returns>OK, NG, pre-weld NG, or Unknown.</returns>
    public static string Normalize(string? rawResult)
    {
        var result = NormalizeText(rawResult);
        if (string.IsNullOrWhiteSpace(result)
            || string.Equals(result, ProductionConstants.TestResults.NoResultRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.NotAvailable, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.Unknown;
        }

        if (string.Equals(result, ProductionConstants.TestResults.NgRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.Ng;
        }

        if (string.Equals(result, ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.Ok;
        }

        if (string.Equals(result, ProductionConstants.TestResults.PreWeldNgRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.PreWeldNg, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.PreWeldNg;
        }

        return ProductionConstants.TestResults.Unknown;
    }

    /// <summary>
    /// Converts a PLC raw result code or existing result text to the text that should be shown on screen.
    /// </summary>
    /// <param name="rawResult">PLC raw code or result text.</param>
    /// <returns>Display text. Raw 0 is displayed as "--" because the device has not produced a result.</returns>
    public static string ToDisplayText(string? rawResult)
    {
        var result = NormalizeText(rawResult);
        if (string.IsNullOrWhiteSpace(result)
            || string.Equals(result, ProductionConstants.TestResults.NoResultRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.NotAvailable, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.NotAvailable;
        }

        var normalized = Normalize(result);
        return string.Equals(normalized, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.NotAvailable
            : normalized;
    }

    /// <summary>
    /// Resolves one product result from multiple point results.
    /// </summary>
    /// <param name="results">Point-level result texts.</param>
    /// <returns>Pre-weld NG, NG, OK, or Unknown.</returns>
    public static string ResolveProductResult(IEnumerable<string?> results)
    {
        var normalizedResults = results.Select(Normalize).ToList();
        if (normalizedResults.Any(IsPreWeldNg))
        {
            return ProductionConstants.TestResults.PreWeldNg;
        }

        if (normalizedResults.Any(IsNg))
        {
            return ProductionConstants.TestResults.Ng;
        }

        return normalizedResults.Count > 0 && normalizedResults.All(IsOk)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Unknown;
    }

    /// <summary>
    /// Determines whether the result means normal NG.
    /// </summary>
    public static bool IsNg(string? result)
        => string.Equals(Normalize(result), ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the result means OK.
    /// </summary>
    public static bool IsOk(string? result)
        => string.Equals(Normalize(result), ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the result means the part failed before welding and has no process values.
    /// </summary>
    public static bool IsPreWeldNg(string? result)
        => string.Equals(Normalize(result), ProductionConstants.TestResults.PreWeldNg, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the result is a failed result that should make the product fail.
    /// </summary>
    public static bool IsFailed(string? result)
    {
        var normalized = Normalize(result);
        return string.Equals(normalized, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, ProductionConstants.TestResults.PreWeldNg, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeText(string? value)
        => value?.Trim().Trim('\0') ?? string.Empty;
}
