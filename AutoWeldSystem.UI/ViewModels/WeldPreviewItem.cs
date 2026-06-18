
namespace AutoWeldSystem.UI.ViewModels;

public sealed record WeldPreviewItem(
    int Index,
    string Key,
    string Name,
    int Sort,
    bool EnableActual,
    bool EnableUpper,
    bool EnableLower,
    bool EnableResult,
    string ActualHeader,
    string UpperHeader,
    string LowerHeader,
    string ResultHeader);
