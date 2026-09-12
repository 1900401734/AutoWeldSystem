using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序限值的唯一解析与校验入口。元数据读取不把旧测试项转换为有效限值。
/// </summary>
public static class ProgramContentJsonRules
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public const string RecipeNameStation1Key = "工位1配方名称";
    public const string RecipeNameStation2Key = "工位2配方名称";
    public const string RecipeNameLegacyKey = "配方名称";
    public const string TouchCountKey = "焊点数量";
    public const string UpperLimitSuffix = "上限";
    public const string LowerLimitSuffix = "下限";

    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        RecipeNameStation1Key, RecipeNameStation2Key, RecipeNameLegacyKey, TouchCountKey
    };

    public static bool IsReservedKey(string? key) => ReservedKeys.Contains(Normalize(key));

    /// <summary>
    /// 查看历史记录时只读取计数元数据；生产入口必须另外调用 NormalizeContent 校验全部内容。
    /// </summary>
    public static bool TryGetTouchCount(string? programContent, out int touchCount)
    {
        touchCount = 0;
        try
        {
            var values = ReadObject(programContent);
            if (!values.TryGetValue(TouchCountKey, out var value))
            {
                return false;
            }

            return (value.ValueKind == JsonValueKind.Number
                    ? value.TryGetInt32(out touchCount)
                    : value.ValueKind == JsonValueKind.String && int.TryParse(
                        value.GetString()?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out touchCount))
                && touchCount > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static int GetRequiredTouchCount(string? programContent)
    {
        if (TryGetTouchCount(programContent, out var touchCount))
        {
            return touchCount;
        }

        throw new InvalidOperationException("程序内容缺少有效的焊点数量，请先在程序管理中填写大于 0 的整数。");
    }

    public static string NormalizeTouchCount(string programContent) => NormalizeContent(programContent);

    /// <summary>
    /// 执行边界比编辑保存更严格：整件检测只支持四面，不允许回退 PLC 面结果。
    /// </summary>
    public static string NormalizeForProduction(string? programContent, string? deviceType)
    {
        var content = NormalizeContent(programContent, deviceType);
        if (WholePieceProgramResultRules.IsApplicable(deviceType) && GetRequiredTouchCount(content) != 4)
        {
            throw new InvalidOperationException("整件检测程序的面数量必须为 4，不能开工、恢复生产或输出检测结果。");
        }

        return content;
    }

    /// <summary>
    /// 保存、同步、下载和开工的公共边界；先拒绝重复/旧字段，再规范化，不能用字典覆盖掩盖错误。
    /// </summary>
    public static string NormalizeContent(string? programContent, string? deviceType = null, bool requireTouchCount = true)
    {
        var values = ParseContent(programContent, deviceType);
        if (requireTouchCount)
        {
            values[TouchCountKey] = GetRequiredTouchCount(programContent).ToString(CultureInfo.InvariantCulture);
        }
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    public static IReadOnlyDictionary<string, ProgramLimitRange> ReadLimits(string? programContent, string? deviceType = null)
        => BuildLimits(ParseContent(programContent, deviceType), deviceType);

    public static bool TryReadLimits(
        string? programContent,
        out IReadOnlyDictionary<string, ProgramLimitRange> limits,
        out string errorMessage,
        string? deviceType = null)
    {
        try
        {
            limits = ReadLimits(programContent, deviceType);
            errorMessage = string.Empty;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            limits = new Dictionary<string, ProgramLimitRange>();
            errorMessage = ex.Message;
            return false;
        }
    }

    public static string BuildLimitsSummary(string? programContent)
    {
        var values = ParseContent(programContent, null);
        return string.Join(' ', BuildLimits(values, null).Select(pair =>
        {
            var upper = values.GetValueOrDefault(pair.Key + UpperLimitSuffix);
            var lower = values.GetValueOrDefault(pair.Key + LowerLimitSuffix);
            return upper is null ? $"{lower}≤{pair.Key}"
                : lower is null ? $"{pair.Key}≤{upper}" : $"{lower}≤{pair.Key}≤{upper}";
        }));
    }

    public static bool HasConfiguredValues(string? programContent)
        => !string.IsNullOrWhiteSpace(programContent) && ParseContent(programContent, null).Count > 0;

    public static IReadOnlyList<ProgramContentItemRow> BuildRows(
        IEnumerable<DimTestItem>? dictionaryItems, string? existingJson)
    {
        // null 是 UI 明确新建的空白表格，已有但损坏的 JSON 不得走这个分支。
        var values = existingJson is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : ParseContent(existingJson, null);
        var rows = new List<ProgramContentItemRow>();
        var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in dictionaryItems ?? Enumerable.Empty<DimTestItem>())
        {
            var name = Normalize(item.ItemName);
            if (name.Length == 0 || IsReservedKey(name) || !knownNames.Add(name))
            {
                continue;
            }
            rows.Add(CreateRow(name, values, true));
        }
        foreach (var name in BuildLimits(values, null).Keys)
        {
            if (knownNames.Add(name))
            {
                rows.Add(CreateRow(name, values, false));
            }
        }
        if (rows.Count == 0)
        {
            rows.Add(new ProgramContentItemRow());
        }
        return rows;
    }

    private static ProgramContentItemRow CreateRow(string name, Dictionary<string, string> values, bool dictionaryItem)
        => new()
        {
            ItemName = name,
            UpperLimit = values.GetValueOrDefault(name + UpperLimitSuffix) ?? string.Empty,
            LowerLimit = values.GetValueOrDefault(name + LowerLimitSuffix) ?? string.Empty,
            IsDictionaryItem = dictionaryItem
        };

    public static IReadOnlyList<ProgramContentReviewRow> BuildReviewRows(
        IEnumerable<DimTestItem>? dictionaryItems, string? existingJson)
        => BuildRows(dictionaryItems, existingJson).Select(row => new ProgramContentReviewRow
        {
            ItemName = row.ItemName,
            UpperLimit = row.UpperLimit,
            LowerLimit = row.LowerLimit,
            IsDictionaryItem = row.IsDictionaryItem
        }).ToList();

    public static string MergeReviewRowsToJson(IEnumerable<ProgramContentReviewRow> rows, string? deviceType = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return ToJson(rows.Select(row => new ProgramContentItemRow
        {
            ItemName = row.ItemName,
            UpperLimit = row.UpperLimit,
            LowerLimit = row.LowerLimit,
            IsDictionaryItem = row.IsDictionaryItem
        }), deviceType);
    }

    public static bool TryMergeReviewRowsToJson(
        IEnumerable<ProgramContentReviewRow> rows, out string json, out string errorMessage, string? deviceType = null)
    {
        try
        {
            json = MergeReviewRowsToJson(rows, deviceType);
            errorMessage = string.Empty;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            json = string.Empty;
            errorMessage = ex.Message;
            return false;
        }
    }

    public static string ToJson(IEnumerable<ProgramContentItemRow> rows, string? deviceType = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var name = Normalize(row.ItemName);
            var upper = Normalize(row.UpperLimit);
            var lower = Normalize(row.LowerLimit);
            if (upper.Length == 0 && lower.Length == 0)
            {
                continue;
            }
            if (name.Length == 0)
            {
                throw new InvalidOperationException("已填写限值的测试项名称不能为空。");
            }
            if (IsReservedKey(name) || !names.Add(name))
            {
                throw new InvalidOperationException($"程序内容中存在重复测试项或保留名称：{name}。");
            }
            if (upper.Length > 0) values.Add(name + UpperLimitSuffix, upper);
            if (lower.Length > 0) values.Add(name + LowerLimitSuffix, lower);
        }
        _ = BuildLimits(values, deviceType);
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    public static bool TryToJson(
        IEnumerable<ProgramContentItemRow> rows, out string json, out string errorMessage, string? deviceType = null)
    {
        try
        {
            json = ToJson(rows, deviceType);
            errorMessage = string.Empty;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            json = string.Empty;
            errorMessage = ex.Message;
            return false;
        }
    }

    public static (string? Station1RecipeName, string? Station2RecipeName) ExtractRecipeNames(string? programContent)
    {
        if (string.IsNullOrWhiteSpace(programContent)) return (null, null);
        string? station1 = null;
        string? station2 = null;
        foreach (var (key, value) in ReadObject(programContent))
        {
            if (!IsReservedKey(key) || key.Equals(TouchCountKey, StringComparison.OrdinalIgnoreCase)) continue;
            var name = ReadMetadata(key, value);
            if (name.Length == 0) continue;
            if (key.Equals(RecipeNameStation2Key, StringComparison.OrdinalIgnoreCase)) station2 = name;
            else station1 ??= name;
        }
        return (station1, station2);
    }

    public static string MergeRecipeNamesAndContent(
        string? station1RecipeName, string? station2RecipeName, string testItemContentJson, int? touchCount = null)
    {
        var content = ParseContent(testItemContentJson, null);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var station1 = NormalizeRecipeName(station1RecipeName);
        var station2 = NormalizeRecipeName(station2RecipeName);
        if (station1.Length > 0) merged.Add(RecipeNameStation1Key, station1);
        if (station2.Length > 0) merged.Add(RecipeNameStation2Key, station2);
        if (touchCount.HasValue)
        {
            if (touchCount.Value <= 0) throw new InvalidOperationException("焊点数量必须是大于 0 的整数。");
            merged.Add(TouchCountKey, touchCount.Value.ToString(CultureInfo.InvariantCulture));
        }
        foreach (var pair in content.Where(pair => !IsReservedKey(pair.Key))) merged.Add(pair.Key, pair.Value);
        return JsonSerializer.Serialize(merged, JsonOptions);
    }

    /// <summary>
    /// 本次开工仅替换限值，保留原内容中的元数据键及值，不落程序库。
    /// </summary>
    public static string ReplaceLimits(string originalContent, string limitsJson, string? deviceType = null)
    {
        var original = ParseContent(originalContent, deviceType);
        var limits = ParseContent(limitsJson, deviceType);
        var merged = original.Where(pair => IsReservedKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in limits.Where(pair => !IsReservedKey(pair.Key))) merged.Add(pair.Key, pair.Value);
        return NormalizeContent(JsonSerializer.Serialize(merged, JsonOptions), deviceType);
    }

    /// <summary>
    /// 只供用户明确选择“重新配置”使用：验证纯旧格式后仅取有效元数据，绝不转换旧限值。
    /// 混用、重复、非法结构和非法数字均不能借重配入口绕过校验。
    /// </summary>
    public static bool TryCreateReconfigurationContent(string? legacyJson, out string metadataJson)
    {
        metadataJson = string.Empty;
        try
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasLegacyItem = false;
            foreach (var (key, value) in ReadObject(legacyJson))
            {
                if (IsReservedKey(key))
                {
                    metadata.Add(key, ReadMetadata(key, value));
                    if (key.Equals(TouchCountKey, StringComparison.OrdinalIgnoreCase)) _ = GetRequiredTouchCount(legacyJson);
                    continue;
                }
                if (TrySplitLimitKey(key, out _, out _)) return false;
                _ = ReadNumber(ReadScalar(key, value), key, "旧设定值");
                hasLegacyItem = true;
            }
            if (!hasLegacyItem) return false;
            metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Dictionary<string, JsonElement> ReadObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("程序内容为空，请重新配置上下限或从 MES 下载新格式程序。");
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("程序内容必须是 JSON 对象。");
            var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var key = Normalize(property.Name);
                if (key.Length == 0) throw new InvalidOperationException("程序内容存在空字段名。");
                if (!values.TryAdd(key, property.Value.Clone()))
                    throw new InvalidOperationException($"程序内容存在重复字段“{key}”。");
            }
            return values;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"程序内容不是合法 JSON：{ex.Message}", ex);
        }
    }

    private static Dictionary<string, string> ParseContent(string? json, string? deviceType)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in ReadObject(json))
        {
            if (IsReservedKey(key)) values.Add(key, ReadMetadata(key, value));
            else
            {
                if (!TrySplitLimitKey(key, out _, out _))
                    throw new InvalidOperationException($"程序内容字段“{key}”不是新限值格式，请使用“测试项上限/测试项下限”；旧格式不再支持，请重新配置或从 MES 下载。");
                values.Add(key, ReadScalar(key, value));
            }
        }
        _ = BuildLimits(values, deviceType);
        return values;
    }

    private static Dictionary<string, ProgramLimitRange> BuildLimits(Dictionary<string, string> values, string? deviceType)
    {
        var limits = new Dictionary<string, ProgramLimitRange>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, text) in values)
        {
            if (IsReservedKey(key)) continue;
            if (!TrySplitLimitKey(key, out var name, out var upper))
                throw new InvalidOperationException($"程序内容字段“{key}”不是新限值格式。");
            var number = ReadNumber(text, name, upper ? "设定上限" : "设定下限");
            var range = limits.GetValueOrDefault(name) ?? new ProgramLimitRange(null, null);
            limits[name] = upper ? range with { UpperLimit = number } : range with { LowerLimit = number };
        }
        foreach (var (name, range) in limits)
        {
            if (range.LowerLimit > range.UpperLimit)
                throw new InvalidOperationException($"测试项“{name}”的设定下限不能大于设定上限。");
            if (WholePieceProgramResultRules.IsApplicable(deviceType) && range.UpperLimit is null)
                throw new InvalidOperationException($"整件检测测试项“{name}”只填写了设定下限，请补齐设定上限。");
        }
        return limits;
    }

    private static bool TrySplitLimitKey(string key, out string itemName, out bool upper)
    {
        upper = key.EndsWith(UpperLimitSuffix, StringComparison.Ordinal);
        var isLimit = upper || key.EndsWith(LowerLimitSuffix, StringComparison.Ordinal);
        itemName = isLimit ? key[..^2] : string.Empty;
        if (isLimit && (itemName.Length == 0 || itemName != itemName.Trim() || IsReservedKey(itemName)))
            throw new InvalidOperationException($"限值字段“{key}”的测试项名称无效。");
        return isLimit;
    }

    private static decimal ReadNumber(string text, string itemName, string limitName)
    {
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            throw new InvalidOperationException($"测试项“{itemName}”的{limitName}“{text}”不是合法数字。");
        return number;
    }

    private static string ReadScalar(string key, JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => Normalize(value.GetString()),
            JsonValueKind.Number => value.GetRawText(),
            _ => throw new InvalidOperationException($"程序内容字段“{key}”必须为数字字符串或 JSON 数字。")
        };

    private static string ReadMetadata(string key, JsonElement value)
    {
        if (key.Equals(TouchCountKey, StringComparison.OrdinalIgnoreCase))
        {
            var text = ReadScalar(key, value);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
                throw new InvalidOperationException("焊点数量必须是大于 0 的整数。");
            return count.ToString(CultureInfo.InvariantCulture);
        }
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"配方名称字段“{key}”必须为字符串。");
        return NormalizeRecipeName(value.GetString());
    }

    private static string NormalizeRecipeName(string? value)
    {
        var text = value ?? string.Empty;
        var terminator = text.IndexOf('\0');
        return (terminator >= 0 ? text[..terminator] : text).Trim();
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}

public sealed record ProgramLimitRange(decimal? UpperLimit, decimal? LowerLimit)
{
    public bool Contains(decimal actual)
        => (!UpperLimit.HasValue || actual <= UpperLimit.Value)
            && (!LowerLimit.HasValue || actual >= LowerLimit.Value);
}
