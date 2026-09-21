using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 焊点记录的软删过滤单一入口。
/// 所有上传范围、报表、产量统计路径统一经此过滤，避免某条链路漏掉 IsDeleted 导致“删了还在上传”。
/// 产品历史表格是唯一保留已删除行的地方（要显示置灰并支持撤销），不走这里。
/// </summary>
public static class WeldPointRecordScopeRules
{
    public static IEnumerable<BizWeldPointRecord> ExcludeDeleted(IEnumerable<BizWeldPointRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return records.Where(record => !record.IsDeleted);
    }
}
