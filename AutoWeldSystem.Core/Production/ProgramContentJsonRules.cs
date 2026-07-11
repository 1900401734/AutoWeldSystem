using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序内容表格与 ProgramContent JSON 的转换规则。
/// </summary>
public static class ProgramContentJsonRules
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>
    /// 判断程序内容是否包含至少一个有效设定项。
    /// 空白和空 JSON 对象表示用户尚未填写设定值；非对象或非法历史内容保守地视为有效。
    /// </summary>
    public static bool HasConfiguredValues(string? programContent)
    {
        var content = programContent?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind != JsonValueKind.Object
                || document.RootElement.EnumerateObject().Any();
        }
        catch (JsonException)
        {
            // 不能因无法解析历史内容而删除已有程序文件。
            return true;
        }
    }
    /// <summary>
    /// 根据测试项字典和已有 JSON 构建程序内容表格行。
    /// </summary>
    public static IReadOnlyList<ProgramContentItemRow> BuildRows(
        IEnumerable<DimTestItem>? dictionaryItems,
        string? existingJson)
    {
        var existingValues = ParseObjectValues(existingJson);
        var rows = new List<ProgramContentItemRow>();
        var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in dictionaryItems ?? Enumerable.Empty<DimTestItem>())
        {
            var itemName = Normalize(item.ItemName);
            if (string.IsNullOrWhiteSpace(itemName) || !knownNames.Add(itemName))
            {
                continue;
            }

            rows.Add(new ProgramContentItemRow
            {
                ItemName = itemName,
                StandardValue = existingValues.TryGetValue(itemName, out var value) ? value : string.Empty,
                IsDictionaryItem = true
            });
        }

        foreach (var pair in existingValues)
        {
            if (knownNames.Contains(pair.Key))
            {
                continue;
            }

            rows.Add(new ProgramContentItemRow
            {
                ItemName = pair.Key,
                StandardValue = pair.Value,
                IsDictionaryItem = false
            });
        }

        // 字典和旧内容都为空时给 UI 一个空行，用户可以直接手动录入。
        if (rows.Count == 0)
        {
            rows.Add(new ProgramContentItemRow());
        }

        return rows;
    }

    /// <summary>
    /// 构建开工预览/微调表格行，将字典项与已下载程序内容映射为带“修改值”列的预览行。
    /// </summary>
    public static IReadOnlyList<ProgramContentReviewRow> BuildReviewRows(
        IEnumerable<DimTestItem>? dictionaryItems,
        string? existingJson)
    {
        var rows = BuildRows(dictionaryItems, existingJson);
        return rows
            .Select(row => new ProgramContentReviewRow
            {
                ItemName = row.ItemName,
                StandardValue = row.StandardValue,
                ModifiedValue = string.Empty,
                IsDictionaryItem = row.IsDictionaryItem
            })
            .ToList();
    }

    /// <summary>
    /// 合并预览/微调行为 MES 需要的 ProgramContent JSON 字符串。
    /// 有效值取 <see cref="ProgramContentReviewRow.ModifiedValue"/>，为空时回退到 <see cref="ProgramContentReviewRow.StandardValue"/>。
    /// </summary>
    public static string MergeReviewRowsToJson(IEnumerable<ProgramContentReviewRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var mergedRows = rows
            .Select(row => new ProgramContentItemRow
            {
                ItemName = row.ItemName,
                StandardValue = string.IsNullOrWhiteSpace(row.ModifiedValue) ? row.StandardValue : row.ModifiedValue,
                IsDictionaryItem = row.IsDictionaryItem
            })
            .ToList();

        return ToJson(mergedRows);
    }

    /// <summary>
    /// 尝试合并预览/微调行为 ProgramContent JSON，失败时返回可展示给用户的错误。
    /// </summary>
    public static bool TryMergeReviewRowsToJson(
        IEnumerable<ProgramContentReviewRow> rows,
        out string json,
        out string errorMessage)
    {
        try
        {
            json = MergeReviewRowsToJson(rows);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            json = "{}";
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 将程序内容表格行转换成 MES 需要的 JSON 字符串。
    /// </summary>
    public static string ToJson(IEnumerable<ProgramContentItemRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var orderedValues = new Dictionary<string, string>();

        foreach (var row in rows)
        {
            var itemName = Normalize(row.ItemName);
            var standardValue = Normalize(row.StandardValue);
            if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(standardValue))
            {
                continue;
            }

            if (values.ContainsKey(itemName))
            {
                throw new InvalidOperationException($"程序内容中存在重复测试项：{itemName}");
            }

            values[itemName] = standardValue;
            orderedValues[itemName] = standardValue;
        }

        return JsonSerializer.Serialize(orderedValues, JsonOptions);
    }

    /// <summary>
    /// 尝试转换程序内容 JSON，失败时返回可展示给用户的错误。
    /// </summary>
    public static bool TryToJson(
        IEnumerable<ProgramContentItemRow> rows,
        out string json,
        out string errorMessage)
    {
        try
        {
            json = ToJson(rows);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            json = "{}";
            errorMessage = ex.Message;
            return false;
        }
    }

    private static Dictionary<string, string> ParseObjectValues(string? json)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return values;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return values;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var key = Normalize(property.Name);
                if (string.IsNullOrWhiteSpace(key) || values.ContainsKey(key))
                {
                    continue;
                }

                values[key] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }
        }
        catch (JsonException)
        {
            // 历史内容若不是合法 JSON，不应阻塞页面打开；保存时会重新按表格生成 JSON。
        }

        return values;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
