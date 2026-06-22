using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// PLC 配方号调和规则。
/// 该类只负责业务判断，不直接读写 PLC，便于后台服务、界面和测试复用同一套规则。
/// </summary>
public static class RecipeCodeReconcileRules
{
    /// <summary>
    /// 根据当前任务、PLC 连接状态和 PLC 配方号判断是否需要执行配方调和。
    /// </summary>
    /// <param name="validateEnabled">是否启用开工后配方校验。</param>
    /// <param name="plcConnected">当前工位 PLC 是否已经通过业务验证连接。</param>
    /// <param name="hasRunningTask">当前工位是否存在运行中的未完工任务。</param>
    /// <param name="expectedRecipeCode">软件侧期望保持的任务配方号。</param>
    /// <param name="plcRecipeCode">PLC 侧当前回读到的配方号。</param>
    /// <param name="workOrderStatus">PLC 工单状态；无法读取时传 null。</param>
    /// <returns>配方调和决策。</returns>
    public static RecipeCodeReconcileDecision Decide(
        bool validateEnabled,
        bool plcConnected,
        bool hasRunningTask,
        string? expectedRecipeCode,
        string? plcRecipeCode,
        int? workOrderStatus)
    {
        var expected = NormalizeRecipeCode(expectedRecipeCode);
        var plc = NormalizeRecipeCode(plcRecipeCode);

        if (!validateEnabled || !plcConnected || !hasRunningTask)
        {
            return RecipeCodeReconcileDecision.Skip(expected, plc);
        }

        if (workOrderStatus == ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction)
        {
            return RecipeCodeReconcileDecision.Skip(expected, plc);
        }

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(plc))
        {
            return RecipeCodeReconcileDecision.Skip(expected, plc);
        }

        var shouldReconcile = !string.Equals(expected, plc, StringComparison.OrdinalIgnoreCase);
        return shouldReconcile
            ? RecipeCodeReconcileDecision.Reconcile(expected, plc)
            : RecipeCodeReconcileDecision.Skip(expected, plc);
    }

    /// <summary>
    /// 统一清理 PLC 字符串尾部空字符和空白，避免不同读取类型导致比较不一致。
    /// </summary>
    private static string NormalizeRecipeCode(string? value)
        => (value ?? string.Empty).Trim().Trim('\0');
}

/// <summary>
/// PLC 配方号调和决策结果。
/// </summary>
/// <param name="ShouldReconcile">是否需要把软件侧配方号重新写回 PLC。</param>
/// <param name="ExpectedRecipeCode">软件侧期望配方号。</param>
/// <param name="PlcRecipeCode">PLC 侧当前配方号。</param>
public sealed record RecipeCodeReconcileDecision(
    bool ShouldReconcile,
    string ExpectedRecipeCode,
    string PlcRecipeCode)
{
    /// <summary>
    /// 创建需要调和的决策。
    /// </summary>
    public static RecipeCodeReconcileDecision Reconcile(string expectedRecipeCode, string plcRecipeCode)
        => new(true, expectedRecipeCode, plcRecipeCode);

    /// <summary>
    /// 创建无需调和的决策。
    /// </summary>
    public static RecipeCodeReconcileDecision Skip(string expectedRecipeCode, string plcRecipeCode)
        => new(false, expectedRecipeCode, plcRecipeCode);
}
