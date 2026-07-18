using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 创建产品工艺新增草稿，集中维护可复制的业务字段和不可复制的数据库身份字段。
/// </summary>
public static class ProductProcessDraftRules
{
    /// <summary>
    /// 有源配置时复制其业务内容；无源配置时保持地址维护页原有默认值。
    /// </summary>
    public static BizProductProcessConfig CreateDraft(
        BizProductProcessConfig? source,
        string? defaultProductNum,
        string? defaultSchemeId,
        DateTime draftTime)
    {
        if (source is null)
        {
            return CreateDefaultDraft(defaultProductNum, defaultSchemeId, draftTime);
        }

        return new BizProductProcessConfig
        {
            SchemeId = source.SchemeId,
            ProductNum = source.ProductNum,
            StationNo = source.StationNo,
            TouchCount = source.TouchCount,
            PointName = source.PointName,
            PointNoHeader = source.PointNoHeader,
            PointResultHeader = source.PointResultHeader,
            PointCountHeader = source.PointCountHeader,
            ShowTestFlagInHistory = source.ShowTestFlagInHistory,
            ProductBase = source.ProductBase,
            ProductLen = source.ProductLen,
            ProductNoExpr = source.ProductNoExpr,
            ProductResultExpr = source.ProductResultExpr,
            ActualTouchCountExpr = source.ActualTouchCountExpr,
            PresetTouchCountExpr = source.PresetTouchCountExpr,
            TouchBase = source.TouchBase,
            TouchNoBase = source.TouchNoBase,
            TouchResultBase = source.TouchResultBase,
            TouchHeaderLen = source.TouchHeaderLen,
            TouchNoExpr = source.TouchNoExpr,
            TouchResultExpr = source.TouchResultExpr,
            TestBase = source.TestBase,
            TestAreaLen = source.TestAreaLen,
            Enabled = source.Enabled,
            CreatedTime = draftTime,
            UpdatedTime = draftTime
        };
    }

    /// <summary>
    /// 创建没有复制源时使用的默认产品工艺草稿。
    /// </summary>
    private static BizProductProcessConfig CreateDefaultDraft(
        string? defaultProductNum,
        string? defaultSchemeId,
        DateTime draftTime)
    {
        return new BizProductProcessConfig
        {
            ProductNum = defaultProductNum?.Trim() ?? string.Empty,
            SchemeId = string.IsNullOrWhiteSpace(defaultSchemeId) ? "S01" : defaultSchemeId.Trim(),
            StationNo = ProductionConstants.Stations.SharedStationNo,
            TouchCount = 1,
            PointName = "焊点",
            PointNoHeader = "焊点序号",
            PointResultHeader = "焊点结果",
            PointCountHeader = "焊点数",
            ShowTestFlagInHistory = true,
            ProductBase = "DB8.0",
            ProductLen = 32,
            ProductNoExpr = "0:I-0",
            ProductResultExpr = "4:H-4",
            TouchBase = "DB8.32",
            TouchNoBase = "DB8.32",
            TouchResultBase = "DB8.32",
            TouchHeaderLen = 16,
            TouchNoExpr = "0:I-0",
            TouchResultExpr = "4:H-4",
            TestBase = "DB8.100",
            TestAreaLen = 48,
            Enabled = true,
            CreatedTime = draftTime,
            UpdatedTime = draftTime
        };
    }
}
