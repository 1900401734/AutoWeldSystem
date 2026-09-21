
namespace AutoWeldSystem.UI.ViewModels;

/// <summary>
/// Identifies the active product used by the real-time preview.
/// </summary>
public sealed record ProductIdentity(
    int StationNo,
    string ProductNum,
    string ProductModel,
    string Source);