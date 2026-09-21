namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 开工工单数量规则。数量必须为正数，0 不能作为看板工单进度的有效分母。
/// </summary>
public static class StartQuantityRules
{
    public static bool IsPositive(int quantity) => quantity > 0;
}
