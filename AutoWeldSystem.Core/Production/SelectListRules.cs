namespace AutoWeldSystem.Core.Production;

/// <summary>
/// AntdUI Select 选中项解析规则。
/// AntdUI 在筛选态（用户输入或程序赋值 Text 触发）下弹出的是按匹配权重重排的子列表，
/// SelectedIndexChanged 事件携带的索引指向该子列表而非完整 Items，
/// 因此选中项必须以显示文本回查完整列表，事件索引只用于消歧重名项。
/// </summary>
public static class SelectListRules
{
    /// <summary>
    /// 按选中文本解析下拉在完整列表中的真实索引。
    /// </summary>
    /// <param name="displayTexts">下拉完整选项文本列表。</param>
    /// <param name="selectedText">控件当前选中文本（SelectedValue 优先，Text 兜底）。</param>
    /// <param name="eventIndex">SelectedIndexChanged 事件携带的索引，仅在文本重名时用于消歧。</param>
    /// <returns>完整列表中的索引；无法解析时返回 -1。</returns>
    public static int ResolveSelectedIndex(IReadOnlyList<string?> displayTexts, string? selectedText, int eventIndex)
    {
        var normalized = selectedText?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return -1;
        }

        if (eventIndex >= 0 && eventIndex < displayTexts.Count && Matches(displayTexts[eventIndex], normalized))
        {
            return eventIndex;
        }

        for (var i = 0; i < displayTexts.Count; i++)
        {
            if (Matches(displayTexts[i], normalized))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Matches(string? displayText, string selectedText)
        => string.Equals(displayText?.Trim(), selectedText, StringComparison.Ordinal);
}
