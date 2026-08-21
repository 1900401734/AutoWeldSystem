namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序内容预览/微调表格行。
/// 在开工前的程序内容弹窗中展示，对 <see cref="StandardValue"/> 的修改只对本次开工生效、不落库。
/// </summary>
public sealed class ProgramContentReviewRow
{
    /// <summary>
    /// 测试项名称，保存为 ProgramContent JSON 的 Key。
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 测试项最大允许值，来自已下载程序的内容，可直接在开工弹窗中修改。
    /// </summary>
    public string StandardValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否来自测试项字典。UI 可用该标记控制字典行的测试项名称不可编辑。
    /// </summary>
    public bool IsDictionaryItem { get; set; }
}
