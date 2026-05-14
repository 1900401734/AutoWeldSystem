using SqlSugar;

namespace AutoWeldSystem.Core.Models;

[SugarTable("Sys_OperationLog")]
public class SysOperationLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50)]
    public string UserNumber { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string UserName { get; set; } = string.Empty;

    [SugarColumn(Length = 30)]
    public string Level { get; set; } = "Info";

    [SugarColumn(Length = 100)]
    public string Action { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "text")]
    public string Detail { get; set; } = string.Empty;

    [SugarColumn]
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
