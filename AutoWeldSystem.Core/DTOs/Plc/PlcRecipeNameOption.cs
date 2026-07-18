namespace AutoWeldSystem.Core.DTOs.Plc;

/// <summary>
/// 从 PLC 配方名称表读取出的可选项。
/// 界面显示名称，业务保存和下发仍使用 RecipeCode。
/// </summary>
public sealed record PlcRecipeNameOption(
    int StationNo,
    int RecipeCode,
    string Name,
    string Address,
    string DisplayText);

/// <summary>
/// 单个 PLC 配方名称地址的读取失败信息。
/// </summary>
public sealed record PlcRecipeNameReadFailure(
    int StationNo,
    int RecipeCode,
    string Address,
    string Message);

/// <summary>
/// 一个工位的 PLC 配方名称读取结果。
/// 局部地址失败时仍返回其他成功选项，并通过 Failures 供调用方提示或记录日志。
/// </summary>
public sealed record PlcRecipeNameReadResult(
    int StationNo,
    bool IsSuccess,
    string Message,
    IReadOnlyList<PlcRecipeNameOption> Options,
    IReadOnlyList<PlcRecipeNameReadFailure> Failures);
