namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 表格下拉列的候选值规则。
/// AntdUI 的 ColumnSelect 在候选项里找不到当前单元格值时，该单元格既不绘制文本也不参与鼠标命中测试，
/// 于是引用了已改名或已删除主数据的旧配置会显示为空白，并且整行都点不中（点空白单元格不会选中该行）。
/// 因此下拉候选值必须把“已被现有配置引用的值”并进来，让历史值仍可见、可选中、可改可删。
/// </summary>
public static class TableSelectOptionRules
{
    /// <summary>
    /// 下拉展开时一次可见的候选条数。
    /// 现场产品工号常有十几个，AntdUI 默认只显示 4 条，必须滚动才能找到目标工号。
    /// </summary>
    public const int DropDownMaxCount = 16;

    /// <summary>
    /// 合并可选主数据值与配置中在用的值。
    /// 去重按 Ordinal 精确比较：AntdUI 匹配候选项用的是字符串相等，
    /// 大小写不同的写法一旦被折叠掉，对应单元格又会退回空白且点不中。
    /// </summary>
    /// <param name="optionValues">主数据提供的候选值。</param>
    /// <param name="valuesInUse">现有配置已经引用的值。</param>
    /// <returns>去重并排序后的候选值。</returns>
    public static IReadOnlyList<string> MergeValuesInUse(
        IEnumerable<string?> optionValues,
        IEnumerable<string?> valuesInUse)
    {
        ArgumentNullException.ThrowIfNull(optionValues);
        ArgumentNullException.ThrowIfNull(valuesInUse);

        return optionValues
            .Concat(valuesInUse)
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToList();
    }
}
