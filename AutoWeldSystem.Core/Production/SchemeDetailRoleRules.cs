using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 方案明细角色规则。
/// 统一维护“实时预览、本地保存、转发看板、写入报表、过程参数”五个通道的关系，
/// 避免规则散落在界面和服务中。五个通道互相独立，实时预览只决定界面显示范围，
/// 不作为其他通道的前置条件。
/// </summary>
public static class SchemeDetailRoleRules
{
    /// <summary>
    /// 固定角色顺序，界面和导出都按此顺序展示。
    /// </summary>
    public static readonly IReadOnlyList<SchemeDetailValueRole> AllRoles =
    [
        SchemeDetailValueRole.Actual,
        SchemeDetailValueRole.Upper,
        SchemeDetailValueRole.Lower,
        SchemeDetailValueRole.Result
    ];

    /// <summary>
    /// 获取测试项中已经配置 PLC 表达式的角色。
    /// </summary>
    public static IEnumerable<SchemeDetailValueRole> GetAvailableRoles(DimTestItem item)
    {
        foreach (var role in AllRoles)
        {
            if (IsRoleAvailable(item, role))
            {
                yield return role;
            }
        }
    }

    /// <summary>
    /// 判断某个角色是否能在方案明细中配置。
    /// </summary>
    public static bool IsRoleAvailable(DimTestItem item, SchemeDetailValueRole role)
        => !string.IsNullOrWhiteSpace(GetExpression(item, role));

    /// <summary>
    /// 获取角色对应的测试项表达式。
    /// </summary>
    public static string? GetExpression(DimTestItem item, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => item.ActualExpression,
            SchemeDetailValueRole.Upper => item.UpperExpression,
            SchemeDetailValueRole.Lower => item.LowerExpression,
            SchemeDetailValueRole.Result => item.ResultExpression,
            _ => null
        };
    }

    /// <summary>
    /// 判断角色是否在实时预览表格中显示。
    /// </summary>
    public static bool IsPreviewEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.EnableActual,
            SchemeDetailValueRole.Upper => detail.EnableUpper,
            SchemeDetailValueRole.Lower => detail.EnableLower,
            SchemeDetailValueRole.Result => detail.EnableResult,
            _ => false
        };
    }

    /// <summary>
    /// 设置角色实时预览开关。
    /// </summary>
    public static void SetPreviewEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.EnableActual = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.EnableUpper = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.EnableLower = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.EnableResult = value;
                break;
        }
    }

    /// <summary>
    /// 判断角色是否保存到本地历史数据。
    /// </summary>
    public static bool IsSaveEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.SaveActual,
            SchemeDetailValueRole.Upper => detail.SaveUpper,
            SchemeDetailValueRole.Lower => detail.SaveLower,
            SchemeDetailValueRole.Result => detail.SaveResult,
            _ => false
        };
    }

    /// <summary>
    /// 设置角色本地保存开关。
    /// </summary>
    public static void SetSaveEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.SaveActual = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.SaveUpper = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.SaveLower = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.SaveResult = value;
                break;
        }
    }

    /// <summary>
    /// 判断角色是否转发到中心服务器看板。
    /// </summary>
    public static bool IsForwardEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.ForwardActual,
            SchemeDetailValueRole.Upper => detail.ForwardUpper,
            SchemeDetailValueRole.Lower => detail.ForwardLower,
            SchemeDetailValueRole.Result => detail.ForwardResult,
            _ => false
        };
    }

    /// <summary>
    /// 设置角色转发看板开关。
    /// </summary>
    public static void SetForwardEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.ForwardActual = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.ForwardUpper = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.ForwardLower = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.ForwardResult = value;
                break;
        }
    }

    /// <summary>
    /// 判断角色是否输出到 Excel 报表。
    /// </summary>
    public static bool IsReportEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.ReportActual,
            SchemeDetailValueRole.Upper => detail.ReportUpper,
            SchemeDetailValueRole.Lower => detail.ReportLower,
            SchemeDetailValueRole.Result => detail.ReportResult,
            _ => false
        };
    }

    /// <summary>
    /// 设置角色报表输出开关。
    /// </summary>
    public static void SetReportEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.ReportActual = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.ReportUpper = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.ReportLower = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.ReportResult = value;
                break;
        }
    }

    /// <summary>
    /// 判断角色是否上传过程参数接口。
    /// </summary>
    public static bool IsMesEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.MesActual,
            SchemeDetailValueRole.Upper => detail.MesUpper,
            SchemeDetailValueRole.Lower => detail.MesLower,
            SchemeDetailValueRole.Result => detail.MesResult,
            _ => false
        };
    }

    /// <summary>
    /// 设置角色过程参数上传开关。
    /// </summary>
    public static void SetMesEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.MesActual = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.MesUpper = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.MesLower = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.MesResult = value;
                break;
        }
    }

    /// <summary>
    /// 判断角色是否需要写入 RawDataJson，供本地保存、转发看板、报表或过程参数使用。
    /// 转发看板必须计入：中心转发从本地记录读值，不落 RawDataJson 就无值可发。
    /// </summary>
    public static bool ShouldPersistRole(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return IsSaveEnabled(detail, role)
            || IsForwardEnabled(detail, role)
            || IsReportEnabled(detail, role)
            || IsMesEnabled(detail, role);
    }

    /// <summary>
    /// 判断角色是否在产品历史和历史数据页显示。
    /// </summary>
    public static bool ShouldShowHistoryRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsSaveEnabled(detail, role);

    /// <summary>
    /// 判断角色是否转发到中心服务器看板。
    /// 中心报表的列定义和值过滤必须共用本规则，否则会出现列与值对不上。
    /// </summary>
    public static bool ShouldForwardCenterRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsForwardEnabled(detail, role);

    /// <summary>
    /// 判断角色是否输出到报表。
    /// </summary>
    public static bool ShouldWriteReportRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsReportEnabled(detail, role);

    /// <summary>
    /// 判断角色是否上传过程参数接口。
    /// </summary>
    public static bool ShouldUploadMesRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsMesEnabled(detail, role);

    /// <summary>
    /// 判断产品完成采集时是否需要读取该角色。
    /// 实时预览开关只控制界面显示，不能作为任何输出通道的数据源前置条件。
    /// </summary>
    public static bool ShouldReadProductRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => ShouldPersistRole(detail, role);

    /// <summary>
    /// 判断角色是否参与整件检测程序判定。
    /// 一个测试项只要在业务上有去向（本地保存、转发看板、报表或过程参数），就参与合格判定；
    /// 只勾实时预览的临时观察项不参与，避免现场为了让预览表格干净而静默改变判定结果。
    /// </summary>
    public static bool ShouldEvaluateProgramRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => ShouldPersistRole(detail, role);

    /// <summary>
    /// 判断方案明细是否至少启用一个实时预览角色。
    /// </summary>
    public static bool HasAnyPreviewEnabled(BizSchemeDetail detail)
        => AllRoles.Any(role => IsPreviewEnabled(detail, role));

    /// <summary>
    /// 判断方案明细是否包含任意通道配置。
    /// 保存时用它保留错误配置，确保能给出明确校验提示。
    /// </summary>
    public static bool HasAnyConfiguredRole(BizSchemeDetail detail)
    {
        return AllRoles.Any(role => IsPreviewEnabled(detail, role)
            || IsSaveEnabled(detail, role)
            || IsForwardEnabled(detail, role)
            || IsReportEnabled(detail, role)
            || IsMesEnabled(detail, role));
    }

    /// <summary>
    /// 获取角色显示表头。
    /// </summary>
    public static string? GetHeader(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.ActualHeader,
            SchemeDetailValueRole.Upper => detail.UpperHeader,
            SchemeDetailValueRole.Lower => detail.LowerHeader,
            SchemeDetailValueRole.Result => detail.ResultHeader,
            _ => null
        };
    }

    /// <summary>
    /// 设置角色显示表头。
    /// </summary>
    public static void SetHeader(BizSchemeDetail detail, SchemeDetailValueRole role, string? value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.ActualHeader = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.UpperHeader = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.LowerHeader = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.ResultHeader = value;
                break;
        }
    }

    /// <summary>
    /// 获取角色 MES 字段名。
    /// </summary>
    public static string? GetMesFieldName(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => detail.ActualMesFieldName,
            SchemeDetailValueRole.Upper => detail.UpperMesFieldName,
            SchemeDetailValueRole.Lower => detail.LowerMesFieldName,
            SchemeDetailValueRole.Result => detail.ResultMesFieldName,
            _ => null
        };
    }

    /// <summary>
    /// 设置角色 MES 字段名。
    /// </summary>
    public static void SetMesFieldName(BizSchemeDetail detail, SchemeDetailValueRole role, string? value)
    {
        switch (role)
        {
            case SchemeDetailValueRole.Actual:
                detail.ActualMesFieldName = value;
                break;
            case SchemeDetailValueRole.Upper:
                detail.UpperMesFieldName = value;
                break;
            case SchemeDetailValueRole.Lower:
                detail.LowerMesFieldName = value;
                break;
            case SchemeDetailValueRole.Result:
                detail.ResultMesFieldName = value;
                break;
        }
    }

    /// <summary>
    /// 清理测试项未配置表达式的角色，避免保存无效配置。
    /// </summary>
    public static void ClearUnavailableRoles(BizSchemeDetail detail, DimTestItem item)
    {
        foreach (var role in AllRoles)
        {
            if (!IsRoleAvailable(item, role))
            {
                ClearRole(detail, role);
            }
        }
    }

    /// <summary>
    /// 清理单个角色的全部通道配置。
    /// </summary>
    public static void ClearRole(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        SetPreviewEnabled(detail, role, false);
        SetSaveEnabled(detail, role, false);
        SetForwardEnabled(detail, role, false);
        SetReportEnabled(detail, role, false);
        SetMesEnabled(detail, role, false);
        SetHeader(detail, role, null);
        SetMesFieldName(detail, role, null);
    }

    /// <summary>
    /// 获取角色中文名称，用于当前 WinForms 配置界面。
    /// </summary>
    public static string GetRoleName(SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => "实际值",
            SchemeDetailValueRole.Upper => "上限",
            SchemeDetailValueRole.Lower => "下限",
            SchemeDetailValueRole.Result => "结果",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 获取角色默认显示表头。
    /// </summary>
    public static string GetDefaultHeader(DimTestItem item, SchemeDetailValueRole role)
        => GetDefaultHeader(item.ItemName, role);

    /// <summary>
    /// 根据测试项名称获取角色默认显示表头，供没有完整测试项实体的展示模型复用。
    /// </summary>
    public static string GetDefaultHeader(string? itemName, SchemeDetailValueRole role)
    {
        var normalizedItemName = itemName?.Trim() ?? string.Empty;
        return role == SchemeDetailValueRole.Actual ? normalizedItemName : $"{normalizedItemName}{GetRoleName(role)}";
    }

    /// <summary>
    /// 获取最终显示表头；优先保留数据库中已有的非空配置，否则使用统一默认值。
    /// </summary>
    public static string ResolveHeader(BizSchemeDetail detail, DimTestItem item, SchemeDetailValueRole role)
        => ResolveHeader(GetHeader(detail, role), item.ItemName, role);

    /// <summary>
    /// 解析已存表头与测试项名称，供 DTO 和界面预览统一使用。
    /// </summary>
    public static string ResolveHeader(string? storedHeader, string? itemName, SchemeDetailValueRole role)
    {
        var normalizedHeader = storedHeader?.Trim();
        return string.IsNullOrWhiteSpace(normalizedHeader) ? GetDefaultHeader(itemName, role) : normalizedHeader;
    }
}
