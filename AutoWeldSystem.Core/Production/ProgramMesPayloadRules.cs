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
}
