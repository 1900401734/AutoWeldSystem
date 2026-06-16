using System.Text.Json.Serialization;

namespace AutoWeldSystem.Core.DTOs.Mes.Response;

/// <summary>
/// 工单信息
/// </summary>
public class WorkOrderRes
{
    /// <summary>
    /// 流转卡号/工单号
    /// </summary>
    public string SN { get; set; } = string.Empty;

    /// <summary>
    /// 产品工号
    /// </summary>
    public string ProdNum { get; set; } = string.Empty;

    /// <summary>
    /// 型号
    /// </summary>
    public string ProdModel { get; set; } = string.Empty;

    /// <summary>
    /// 规格
    /// </summary>
    public string Spec { get; set; } = string.Empty;

    /// <summary>
    /// 批次
    /// </summary>
    public string Batch { get; set; } = string.Empty;

    /// <summary>
    /// 部件名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 零件图号
    /// </summary>
    public string DrawingNo { get; set; } = string.Empty;

    /// <summary>
    /// 项目来源
    /// </summary>
    [JsonPropertyName("ProjectFrom")]
    public string ProjectFrom { get; set; } = string.Empty;

    /// <summary>
    /// 工序列表集合
    /// </summary>
    public List<ExpItemData> ExpItems { get; set; } = [];
}

/// <summary>
/// 工序列表
/// </summary>
public class ExpItemData
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public int ItemID { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    public string? ItemTitle { get; set; }

    /// <summary>
    /// 工艺内容
    /// </summary>
    public string? ItemCont { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// 工序名称
    /// </summary>
    public string ItemName { get; set; } = string.Empty;
    
    /// <summary>
    /// 工序号
    /// </summary>
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// 生产数量
    /// </summary>
    public int StartAmount { get; set; }
}
