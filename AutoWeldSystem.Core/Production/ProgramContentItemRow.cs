namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序内容表格行。
/// 一行代表一个测试项及其在该程序中的最大允许值。
/// </summary>
public sealed class ProgramContentItemRow
{
    /// <summary>
    /// 测试项名称，保存为 ProgramContent JSON 的 Key。
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 测试项最大允许值，保存为 ProgramContent JSON 的 Value。
    /// </summary>
    public string StandardValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否来自测试项字典。
    /// UI 可用该标记控制字典行的测试项名称不可编辑。
    /// </summary>
    public bool IsDictionaryItem { get; set; }
}
