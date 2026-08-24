namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 测试项字典ID分配规则。
/// 现场把测试项ID当作可读的排列序号使用，而 MySQL 的 AUTO_INCREMENT 在删除记录后不会回收计数器，
/// 会出现删掉 1、2、20 再重新添加却得到 21、22、23 的跳号。因此新增测试项由应用层显式分配ID，
/// 保证界面新增时显示的序号与最终写入数据库的测试项ID一致。
/// </summary>
public static class TestItemIdAllocationRules
{
    /// <summary>
    /// 测试项ID从 1 开始，0 和负数表示尚未落库的新行。
    /// </summary>
    public const int FirstItemId = 1;

    /// <summary>
    /// 按当前已存在的测试项ID分配下一个ID。
    /// 空表回到 1；否则取最大值加一，不占用已有ID。
    /// </summary>
    /// <param name="existingItemIds">数据库中已存在的测试项ID，可包含未落库的 0。</param>
    /// <returns>可用于新测试项的ID。</returns>
    public static int AllocateNextId(IEnumerable<int> existingItemIds)
    {
        ArgumentNullException.ThrowIfNull(existingItemIds);

        var maxAssignedId = existingItemIds
            .Where(itemId => itemId > 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxAssignedId < FirstItemId ? FirstItemId : maxAssignedId + 1;
    }
}
