namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序内容预览/微调表格行。
/// 在开工前的程序内容弹窗中展示，<see cref="ModifiedValue"/> 只对本次开工生效、不落库。
/// </summary>
public sealed class ProgramContentReviewRow
{
    /// <summary>
    /// 测试项名称，保存为 ProgramContent JSON 的 Key。
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 测试项设定值/标准值，来自已下载程序的内容，只读展示。
    /// </summary>
    public string StandardValue { get; set; } = string.Empty;

    /// <summary>
    /// 本次开工临时修改值；为空则回退到 <see cref="StandardValue"/>。
    /// </summary>
    public string ModifiedValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否来自测试项字典。UI 可用该标记控制字典行的测试项名称不可编辑。
    /// </summary>
    public bool IsDictionaryItem { get; set; }
}
