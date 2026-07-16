using System.Text;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 判断程序保存是否影响 MES 上传字段。
/// 大多数本地辅助字段的变化不会触发 MES 更新；Description 会影响程序名称，因此属于 MES 同步字段。
/// </summary>
public static class ProgramMesSyncRules
{
    /// <summary>
    /// 根据保存前后快照决定本次保存后需要同步到 MES 的动作。
    /// </summary>
    public static string? ResolveSaveAction(BizProgram? original, BizProgram current, bool hadPendingAction)
    {
        var currentSaveAction = ResolveCurrentSaveAction(original, current);
        var hasMesProgramId = HasMesProgramId(current.ProgramId);
        if (!hasMesProgramId)
        {
            return AppConstants.ProgramSyncActions.Create;
        }

        if (hadPendingAction && !string.IsNullOrWhiteSpace(current.SyncAction))
        {
            // 已经拿到 MES ID 的程序不能继续保留 Create，否则后续同步会重复新增远程程序。
            if (Same(current.SyncAction, AppConstants.ProgramSyncActions.Create))
            {
                return hasMesProgramId ? currentSaveAction : AppConstants.ProgramSyncActions.Create;
            }

            return current.SyncAction;
        }

        return currentSaveAction;
    }

    /// <summary>
    /// 只判断“本次保存”是否产生 MES 同步动作，不继承数据库里已有的待同步状态。
    /// </summary>
    public static string? ResolveCurrentSaveAction(BizProgram? original, BizProgram current)
    {
        if (current.Id <= 0)
        {
            return AppConstants.ProgramSyncActions.Create;
        }

        if (!HasMesProgramId(current.ProgramId))
        {
            return AppConstants.ProgramSyncActions.Create;
        }

        return HasMesUploadFieldChanges(original, current)
            ? AppConstants.ProgramSyncActions.Update
            : null;
    }

    /// <summary>
    /// 将待同步动作解析成真正允许执行的 MES 接口动作。
    /// </summary>
    public static string? ResolveExecutableSyncAction(string? syncAction, string? programId)
    {
        var action = syncAction?.Trim();
        if (string.IsNullOrWhiteSpace(action))
        {
            return null;
        }

        var hasMesProgramId = HasMesProgramId(programId);
        if (Same(action, AppConstants.ProgramSyncActions.Create))
        {
            return hasMesProgramId
                ? AppConstants.ProgramSyncActions.Update
                : AppConstants.ProgramSyncActions.Create;
        }

        if (Same(action, AppConstants.ProgramSyncActions.Update))
        {
            return hasMesProgramId ? AppConstants.ProgramSyncActions.Update : null;
        }

        if (Same(action, AppConstants.ProgramSyncActions.Delete))
        {
            return hasMesProgramId ? AppConstants.ProgramSyncActions.Delete : null;
        }

        return null;
    }

    /// <summary>
    /// 判断两个程序快照之间是否存在 MES 上传字段变化。
    /// </summary>
    /// <param name="original">保存前的程序快照。</param>
    /// <param name="current">保存后的程序快照。</param>
    /// <returns>存在 MES 字段变化时返回 true。</returns>
    public static bool HasMesUploadFieldChanges(BizProgram? original, BizProgram current)
    {
        if (original is null)
        {
            return true;
        }

        var sameProgramContent = SameProgramContent(original.ProgramContent, current.ProgramContent);

        return !Same(original.ProgramName, current.ProgramName)
            || !Same(original.DeviceId, current.DeviceId)
            || !sameProgramContent
            || !Same(original.ProgramType, current.ProgramType)
            || !Same(original.ProductNum, current.ProductNum)
            || !Same(original.Description, current.Description)
            || HasProgramFileChanged(original.ProgramFile, current.ProgramFile, current.ProgramContent, sameProgramContent)
            || !Same(original.Remark, current.Remark);
    }

    /// <summary>
    /// 判断当前程序是否已经有等待同步到 MES 的动作。
    /// </summary>
    /// <param name="program">当前程序实体。</param>
    /// <returns>存在待同步动作时返回 true。</returns>
    public static bool HasPendingSyncAction(BizProgram program)
    {
        return !string.IsNullOrWhiteSpace(program.SyncAction);
    }

    private static bool Same(string? left, string? right)
        => string.Equals(left?.Trim() ?? string.Empty, right?.Trim() ?? string.Empty, StringComparison.Ordinal);

    private static bool SameProgramContent(string? left, string? right)
    {
        if (TryNormalizeJson(left, out var leftJson) && TryNormalizeJson(right, out var rightJson))
        {
            return string.Equals(leftJson, rightJson, StringComparison.Ordinal);
        }

        return Same(left, right);
    }

    private static bool SameProgramFile(string? left, string? right)
    {
        if (TryDecodeBase64Text(left, out var leftJson) && TryDecodeBase64Text(right, out var rightJson))
        {
            return SameProgramContent(leftJson, rightJson);
        }

        return Same(left, right);
    }

    private static bool HasProgramFileChanged(
        string? originalProgramFile,
        string? currentProgramFile,
        string? currentProgramContent,
        bool sameProgramContent)
    {
        if (SameProgramFile(originalProgramFile, currentProgramFile))
        {
            return false;
        }

        // ProgramFile 由 ProgramContent 自动生成；内容未变时，重新生成文件不代表 MES 文件真的变了。
        if (sameProgramContent && ProgramFileMatchesContent(currentProgramFile, currentProgramContent))
        {
            return false;
        }

        return true;
    }

    private static bool ProgramFileMatchesContent(string? programFile, string? programContent)
    {
        return TryDecodeBase64Text(programFile, out var fileJson)
            && SameProgramContent(fileJson, programContent);
    }

    private static bool TryDecodeBase64Text(string? base64, out string text)
    {
        text = string.Empty;
        var normalizedBase64 = base64?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBase64))
        {
            return false;
        }

        try
        {
            text = Encoding.UTF8.GetString(Convert.FromBase64String(normalizedBase64));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryNormalizeJson(string? json, out string normalizedJson)
    {
        var normalizedInput = string.IsNullOrWhiteSpace(json) ? "{}" : json.Trim();
        try
        {
            using var document = JsonDocument.Parse(normalizedInput);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteCanonicalJson(writer, document.RootElement);
            }

            normalizedJson = Encoding.UTF8.GetString(stream.ToArray());
            return true;
        }
        catch (JsonException)
        {
            normalizedJson = normalizedInput;
            return false;
        }
    }

    private static void WriteCanonicalJson(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(it => it.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool HasMesProgramId(string? programId)
        => !string.IsNullOrWhiteSpace(programId);
}
