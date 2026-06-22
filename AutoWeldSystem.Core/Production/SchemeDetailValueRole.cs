namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 方案明细中一个测试项可配置的数据角色。
/// </summary>
public enum SchemeDetailValueRole
{
    /// <summary>
    /// 实际采集值。
    /// </summary>
    Actual,

    /// <summary>
    /// 上限值。
    /// </summary>
    Upper,

    /// <summary>
    /// 下限值。
    /// </summary>
    Lower,

    /// <summary>
    /// 结果值。
    /// </summary>
    Result
}
