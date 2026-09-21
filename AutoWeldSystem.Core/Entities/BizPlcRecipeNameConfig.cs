using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// PLC 配方名称连续地址配置。
/// 每个工位独立配置数据块，不支持共享工位。
/// </summary>
[SugarTable(tableName: "Biz_PlcRecipeNameConfig", tableDescription: "PLC配方名称地址配置表")]
public class BizPlcRecipeNameConfig
{
    /// <summary>
    /// 主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 工位号，只允许大于 0 的实际工位。
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; }

    /// <summary>
    /// 配方号 1 对应的 PLC 字符串基地址。
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "配方名称基地址")]
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// PLC 中预留的配方槽位总数。
    /// </summary>
    [SugarColumn(ColumnDescription = "配方数量")]
    public int RecipeCount { get; set; }

    /// <summary>
    /// 相邻配方名称地址之间的字节偏移量。
    /// </summary>
    [SugarColumn(ColumnDescription = "地址字节偏移量")]
    public int AddressOffset { get; set; }

    /// <summary>
    /// 每个配方名称的 PLC 字符串读取长度。
    /// </summary>
    [SugarColumn(ColumnDescription = "字符串读取长度")]
    public int StringLength { get; set; }

    /// <summary>
    /// 是否启用当前工位的配方名称读取。
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 创建时间。
    /// </summary>
    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
