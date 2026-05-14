namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 统一按“从上到下、从左到右”的视觉顺序整理 TabIndex。
/// 这样做虽然不是最复杂的方案，但非常直观，后续维护成本也低。
/// </summary>
public static class TabOrderHelper
{
    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ApplyToContainer(root);
    }

    private static void ApplyToContainer(Control parent)
    {
        var orderedControls = parent.Controls
            .Cast<Control>()
            .OrderBy(control => control.Top)
            .ThenBy(control => control.Left)
            .ToList();

        for (var index = 0; index < orderedControls.Count; index++)
        {
            orderedControls[index].TabIndex = index;
        }

        foreach (var child in orderedControls.Where(control => control.HasChildren))
        {
            ApplyToContainer(child);
        }
    }
}
