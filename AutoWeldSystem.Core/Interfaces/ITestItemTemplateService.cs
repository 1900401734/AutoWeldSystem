using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 测试项目模板服务。
/// 负责维护模板主表和模板明细，采集流程通过模板读取 PLC 地址。
/// </summary>
public interface ITestItemTemplateService
{
    IReadOnlyList<BizTestItemTemplate> GetTemplates(bool includeDisabled = false);

    IReadOnlyList<BizTestItemTemplateItem> GetItems(int templateId, bool includeDisabled = false);

    IReadOnlyList<BizTestItemTemplateItem> GetEnabledItems(int templateId, int stationNo, int touchNo);

    BizTestItemTemplate SaveTemplate(BizTestItemTemplate template);

    BizTestItemTemplateItem SaveItem(BizTestItemTemplateItem item);

    IReadOnlyList<BizTestItemTemplateItem> SaveItems(IEnumerable<BizTestItemTemplateItem> items);

    void DeleteTemplate(int id);

    void DeleteItem(int id);
}
