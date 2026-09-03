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
    /// ProgramContent JSON 保留键，不当测试项处理。
    /// </summary>
    public const string RecipeNameStation1Key = "工位1配方名称";
    public const string RecipeNameStation2Key = "工位2配方名称";
    public const string RecipeNameLegacyKey = "配方名称";

    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        RecipeNameStation1Key,
        RecipeNameStation2Key,
        RecipeNameLegacyKey
    };

    /// <summary>
    /// 判断键是否为保留元数据，不应当测试项处理。
    /// </summary>
    public static bool IsReservedKey(string? key)
    {
        return !string.IsNullOrWhiteSpace(key) && ReservedKeys.Contains(key.Trim());
    }

    /// <summary>
    /// 规范化配方名称：截断 PLC 定长字符串的 NUL 填充。
    /// string.Trim() 不去掉 NUL，若不处理会把 \u0000 写进 ProgramContent 并上传 MES，
    /// 也会让另一台设备按名称匹配槽位时对不上。
    /// </summary>
    private static string NormalizeRecipeName(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var terminatorIndex = value.IndexOf('\0');
        var text = terminatorIndex >= 0 ? value[..terminatorIndex] : value;
        return text.Trim();
    }

    /// <summary>
    /// 把程序内容拼成一行「测试项≤最大允许值」摘要，供生产监控页随时查阅设定值。
    /// 判定规则是实际值大于最大允许值才 NG，所以用 ≤ 如实表达合格区间。
    /// 项的顺序沿用 JSON 顺序；没有有效值时返回空字符串。
    /// 该方法在实时预览的每次刷新中调用，任何内容都不能抛异常打断采集显示。
    /// 跳过保留键（配方名称），只展示测试项上限。
    /// </summary>
    public static string BuildLimitsSummary(string? programContent)
    {
        var values = ParseObjectValues(programContent);
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value) && !IsReservedKey(pair.Key))
                .Select(pair => $"{pair.Key}≤{pair.Value.Trim()}"));
    }

    /// <summary>
    /// 判断程序内容是否包含至少一个有效最大允许值或配方名称。
    /// 空白和空 JSON 对象表示用户尚未填写最大允许值；非对象或非法历史内容保守地视为有效。
    /// 只有配方名称、没有测试项上限时，仍视为有内容，以便配方名随 MES 同步上传。
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
    /// 保留键（配方名称）不进表格，由专门的读取/注入 API 处理。
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
            if (string.IsNullOrWhiteSpace(itemName) || !knownNames.Add(itemName) || IsReservedKey(itemName))
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
            if (knownNames.Contains(pair.Key) || IsReservedKey(pair.Key))
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
                IsDictionaryItem = row.IsDictionaryItem
            })
            .ToList();
    }

    /// <summary>
    /// 合并预览/微调行为 MES 需要的 ProgramContent JSON 字符串。
    /// 有效值直接取 <see cref="ProgramContentReviewRow.StandardValue"/>，用户在弹窗中就地修改。
    /// </summary>
    public static string MergeReviewRowsToJson(IEnumerable<ProgramContentReviewRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var mergedRows = rows
            .Select(row => new ProgramContentItemRow
            {
                ItemName = row.ItemName,
                StandardValue = row.StandardValue,
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
    /// 不处理配方名称保留键；保留键应由保存入口单独注入到最终 JSON 前面。
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

            if (IsReservedKey(itemName))
            {
                throw new InvalidOperationException($"测试项名称不能使用保留键：{itemName}");
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

    /// <summary>
    /// 从 ProgramContent JSON 提取工位配方名称。
    /// 工位 1 同时接受「工位1配方名称」和「配方名称」两个键。
    /// </summary>
    public static (string? Station1RecipeName, string? Station2RecipeName) ExtractRecipeNames(string? programContent)
    {
        if (string.IsNullOrWhiteSpace(programContent))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(programContent);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            string? station1 = null;
            string? station2 = null;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var key = property.Name.Trim();
                var value = property.Value.ValueKind == JsonValueKind.String
                    ? NormalizeRecipeName(property.Value.GetString())
                    : null;

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (string.Equals(key, RecipeNameStation1Key, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, RecipeNameLegacyKey, StringComparison.OrdinalIgnoreCase))
                {
                    station1 ??= value;
                }
                else if (string.Equals(key, RecipeNameStation2Key, StringComparison.OrdinalIgnoreCase))
                {
                    station2 = value;
                }
            }

            return (station1, station2);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// 将配方名称和测试项内容合并为最终 ProgramContent JSON，配方名在最前面。
    /// </summary>
    public static string MergeRecipeNamesAndContent(
        string? station1RecipeName,
        string? station2RecipeName,
        string testItemContentJson)
    {
        var merged = new Dictionary<string, string>();

        // 配方名在前；PLC 定长字符串的 NUL 填充必须先截断，否则会写进 MES 程序内容。
        var normalizedStation1 = NormalizeRecipeName(station1RecipeName);
        if (!string.IsNullOrWhiteSpace(normalizedStation1))
        {
            merged[RecipeNameStation1Key] = normalizedStation1;
        }

        var normalizedStation2 = NormalizeRecipeName(station2RecipeName);
        if (!string.IsNullOrWhiteSpace(normalizedStation2))
        {
            merged[RecipeNameStation2Key] = normalizedStation2;
        }

        // 测试项内容在后
        if (!string.IsNullOrWhiteSpace(testItemContentJson))
        {
            try
            {
                using var document = JsonDocument.Parse(testItemContentJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        var key = property.Name.Trim();
                        if (string.IsNullOrWhiteSpace(key) || IsReservedKey(key))
                        {
                            continue;
                        }

                        var value = property.Value.ValueKind == JsonValueKind.String
                            ? property.Value.GetString() ?? string.Empty
                            : property.Value.ToString();

                        merged[key] = value;
                    }
                }
            }
            catch (JsonException)
            {
                // 测试项内容非法时仍保留配方名
            }
        }

        return JsonSerializer.Serialize(merged, JsonOptions);
    }
}
