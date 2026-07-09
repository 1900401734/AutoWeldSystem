using System.Text.Json;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 构造 MES 程序写入请求。
/// 该规则集中排除 RecipeCode 等本地辅助字段，避免误传给 MES。
/// </summary>
public static class ProgramMesPayloadRules
{
    /// <summary>
    /// 从本地程序实体构造 MES 新增/更新请求。
    /// </summary>
    /// <param name="entity">本地程序实体。</param>
    /// <param name="remark">本次同步要写入 MES 的备注。</param>
    /// <returns>不包含本地配方号的 MES 写入请求。</returns>
    public static ProgramDataWriteReq ToWriteRequest(BizProgram entity, string? remark)
    {
        return new ProgramDataWriteReq
        {
            Id = entity.ProgramId ?? string.Empty,
            ProgramName = entity.ProgramName,
            DeviceId = entity.DeviceId,
            ProgramContent = entity.ProgramContent ?? string.Empty,
            ProgramType = entity.ProgramType,
            ProductNum = entity.ProductNum,
            ProgramFile = entity.ProgramFile ?? string.Empty,
            FileType = ProgramFileRules.ResolveFileType(entity.ProgramFileName),
            Remark = remark ?? string.Empty
        };
    }

    /// <summary>
    /// 从本地程序实体构造 MES 新增请求。
    /// 新增时若用户没有填写任何设定值，MES 要求程序内容和文件字段都留空。
    /// </summary>
    /// <param name="entity">本地程序实体。</param>
    /// <param name="remark">本次同步要写入 MES 的备注。</param>
    /// <returns>用于新增接口的 MES 写入请求。</returns>
    public static ProgramDataWriteReq ToCreateRequest(BizProgram entity, string? remark)
    {
        var request = ToWriteRequest(entity, remark);
        if (!HasConfiguredProgramContent(entity.ProgramContent))
        {
            request.ProgramContent = string.Empty;
            request.ProgramFile = string.Empty;
            request.FileType = string.Empty;
        }

        return request;
    }

    /// <summary>
    /// 判断程序内容中是否真的存在用户填写的设定值。
    /// 空白或空 JSON 对象表示用户未填写任何有效设定值。
    /// </summary>
    private static bool HasConfiguredProgramContent(string? programContent)
    {
        var content = programContent?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return true;
            }

            return document.RootElement.EnumerateObject().Any();
        }
        catch (JsonException)
        {
            // 历史或外部同步内容若不是合法 JSON，不能被误判为“未填写”。
            return true;
        }
    }
}
