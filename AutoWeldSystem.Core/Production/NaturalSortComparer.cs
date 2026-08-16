using System.Text.RegularExpressions;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 自然排序比较器，支持字符串中嵌入的数字按数值排序。
/// 例如："P1" < "P2" < "P10" < "P20"
/// </summary>
public sealed partial class NaturalSortComparer : IComparer<string>
{
    public static NaturalSortComparer Instance { get; } = new();

    private NaturalSortComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var xParts = SplitIntoChunks(x);
        var yParts = SplitIntoChunks(y);
        var minLength = Math.Min(xParts.Count, yParts.Count);

        for (var i = 0; i < minLength; i++)
        {
            var xPart = xParts[i];
            var yPart = yParts[i];

            var xIsNumber = long.TryParse(xPart, out var xNumber);
            var yIsNumber = long.TryParse(yPart, out var yNumber);

            if (xIsNumber && yIsNumber)
            {
                var numberComparison = xNumber.CompareTo(yNumber);
                if (numberComparison != 0)
                {
                    return numberComparison;
                }
            }
            else
            {
                var stringComparison = string.Compare(xPart, yPart, StringComparison.Ordinal);
                if (stringComparison != 0)
                {
                    return stringComparison;
                }
            }
        }

        return xParts.Count.CompareTo(yParts.Count);
    }

    private static List<string> SplitIntoChunks(string value)
    {
        var chunks = new List<string>();
        var match = ChunkRegex().Match(value);

        while (match.Success)
        {
            chunks.Add(match.Value);
            match = match.NextMatch();
        }

        return chunks;
    }

    [GeneratedRegex(@"\d+|\D+")]
    private static partial Regex ChunkRegex();
}

