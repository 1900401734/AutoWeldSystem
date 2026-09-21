using SqlSugar;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 加工程序主表。
/// 该表保存当前可用版本；历史版本保存在 BizProgramRevision 中，类似 Git 的提交记录。
/// </summary>
[SugarTable(tableName: "Biz_Program", tableDescription: "加工程序表")]
public class BizProgram
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    #region MES字段

    /// <summary>
    /// 新增试验程序，数据采集平台生成的Id，32位guid
    /// </summary>
    [SugarColumn(Length = 32, ColumnDescription = "云端程序ID", IsNullable = true)]
    public string? ProgramId { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "程序名称")]
    public string ProgramName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "text", ColumnDescription = "JSON字符串", IsNullable = true)]
    public string? ProgramContent { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "程序类型")]
    public string ProgramType { get; set; } = "0";

    [SugarColumn(Length = 50, ColumnDescription = "产品工号")]
    public string ProductNum { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "longtext", ColumnDescription = "执行文件Base64", IsNullable = true)]
    public string? ProgramFile { get; set; }

    [SugarColumn(Length = 500, ColumnDescription = "备注", IsNullable = true)]
    public string? Remark { get; set; }

    #endregion

    #region 本地辅助字段

    [SugarColumn(Length = 50, ColumnDescription = "配方编号", IsNullable = true)]
    public string? RecipeCode { get; set; }

    /// <summary>
    /// 双工位设备的工位 2 配方编号；为空时按旧数据兼容规则回退到 RecipeCode。
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工位2配方编号", IsNullable = true)]
    public string? Station2RecipeCode { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "产品型号", IsNullable = true)]
    public string? ProductModel { get; set; }

    // 界面文案为“部件图号”；字段名、列名和列注释保持原样，避免 CodeFirst 对现场库产生无谓的列变更。
    [SugarColumn(Length = 80, ColumnDescription = "零组件代码(18位字符串)", IsNullable = true)]
    public string? ComponentCode { get; set; }

    [SugarColumn(ColumnDescription = "程序流水号")]
    public int SequenceNumber { get; set; } = 1;

    [SugarColumn(Length = 200, ColumnDescription = "程序文件名", IsNullable = true)]
    public string? ProgramFileName { get; set; }

    [SugarColumn(Length = 500, ColumnDescription = "本地备注", IsNullable = true)]
    public string? Description { get; set; }

    #endregion

    [SugarColumn(ColumnDescription = "本地版本号")]
    public int VersionNumber { get; set; } = 1;

    [SugarColumn(Length = 40, ColumnDescription = "本地提交ID", IsNullable = true)]
    public string? CommitId { get; set; }

    [SugarColumn(Length = 200, ColumnDescription = "提交说明", IsNullable = true)]
    public string? CommitMessage { get; set; }

    [SugarColumn(Length = 30, ColumnDescription = "MES同步状态")]
    public string SyncStatus { get; set; } = AppConstants.ProgramSyncStatus.PendingCreate;

    [SugarColumn(Length = 20, ColumnDescription = "待同步动作", IsNullable = true)]
    public string? SyncAction { get; set; } = AppConstants.ProgramSyncActions.Create;

    [SugarColumn(ColumnDataType = "text", ColumnDescription = "MES同步消息", IsNullable = true)]
    public string? SyncMessage { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "最近同步时间")]
    public DateTime? LastSyncTime { get; set; }

    [SugarColumn(ColumnDescription = "是否本地删除")]
    public bool IsDeleted { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
