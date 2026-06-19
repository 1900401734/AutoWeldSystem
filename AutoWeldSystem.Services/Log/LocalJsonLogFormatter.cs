using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// 本地 JSON 日志格式化工具。
/// 新日志按空行分隔记录块，并使用 3 个空格缩进，方便现场直接打开文件查阅。
/// </summary>
internal static class LocalJsonLogFormatter
{
    private const int SystemTextJsonIndentSize = 2;
    private const int LocalLogIndentSize = 3;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 将日志对象序列化为多行 JSON，并把默认 2 空格缩进转换为 3 空格缩进。
    /// </summary>
    public static string Serialize<T>(T entry)
    {
        var json = JsonSerializer.Serialize(entry, WriteOptions);
        return ConvertIndent(json);
    }

    /// <summary>
    /// 反序列化一条日志记录，兼容旧的一行 JSON 和新的多行 JSON。
    /// </summary>
    public static T? Deserialize<T>(string record)
    {
        return JsonSerializer.Deserialize<T>(record, ReadOptions);
    }

    /// <summary>
    /// 读取最近的日志记录块。
    /// 新格式按空行分隔；旧格式如果是一行一条 JSON，也会在识别到新对象时自动切分。
    /// </summary>
    public static IEnumerable<string> ReadLatestRecords(string filePath, int take)
    {
        var records = new Queue<string>(take);
        var builder = new StringBuilder();

        foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                EnqueueRecord(records, builder, take);
                continue;
            }

            // 兼容没有空行分隔的旧 JSONL：当前块已经是完整 JSON 且下一行又是对象开头时，先提交上一条。
            if (builder.Length > 0
                && line.TrimStart().StartsWith('{')
                && IsCompleteJson(builder.ToString()))
            {
                EnqueueRecord(records, builder, take);
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }

        EnqueueRecord(records, builder, take);
        return records;
    }

    private static string ConvertIndent(string json)
    {
        var lines = json.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var leadingSpaces = line.TakeWhile(ch => ch == ' ').Count();
            if (leadingSpaces == 0)
            {
                continue;
            }

            var indentLevel = leadingSpaces / SystemTextJsonIndentSize;
            lines[i] = new string(' ', indentLevel * LocalLogIndentSize) + line[leadingSpaces..];
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void EnqueueRecord(Queue<string> records, StringBuilder builder, int take)
    {
        var record = builder.ToString().Trim();
        builder.Clear();
        if (string.IsNullOrWhiteSpace(record))
        {
            return;
        }

        if (records.Count >= take)
        {
            records.Dequeue();
        }

        records.Enqueue(record);
    }

    private static bool IsCompleteJson(string record)
    {
        try
        {
            using var _ = JsonDocument.Parse(record);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
