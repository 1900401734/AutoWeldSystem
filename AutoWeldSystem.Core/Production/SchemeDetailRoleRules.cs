using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 方案明细角色规则。
/// 统一维护“采集、保存、报表、MES”之间的关系，避免规则散落在界面和服务中。
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
    /// 判断角色是否已启用采集。
    /// </summary>
    public static bool IsCollectEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
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
    /// 设置角色采集开关。
    /// </summary>
    public static void SetCollectEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
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
    /// 判断角色是否写入历史数据。
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
    /// 设置角色历史保存开关。
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
    /// 判断角色是否上传到 MES。
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
    /// 设置角色 MES 上传开关。
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
    /// 判断角色是否需要写入 RawDataJson，供历史、报表或 MES 使用。
    /// </summary>
    public static bool ShouldPersistRole(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return IsCollectEnabled(detail, role)
            && (IsSaveEnabled(detail, role) || IsReportEnabled(detail, role) || IsMesEnabled(detail, role));
    }

    /// <summary>
    /// 判断角色是否在产品历史和历史数据页显示。
    /// </summary>
    public static bool ShouldShowHistoryRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsCollectEnabled(detail, role) && IsSaveEnabled(detail, role);

    /// <summary>
    /// 判断角色是否输出到报表。
    /// </summary>
    public static bool ShouldWriteReportRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsCollectEnabled(detail, role) && IsReportEnabled(detail, role);

    /// <summary>
    /// 判断角色是否上传到 MES。
    /// </summary>
    public static bool ShouldUploadMesRole(BizSchemeDetail detail, SchemeDetailValueRole role)
        => IsCollectEnabled(detail, role) && IsMesEnabled(detail, role);

    /// <summary>
    /// 判断方案明细是否至少启用一个采集角色。
    /// </summary>
    public static bool HasAnyCollectEnabled(BizSchemeDetail detail)
        => AllRoles.Any(role => IsCollectEnabled(detail, role));

    /// <summary>
    /// 判断方案明细是否包含任意采集或输出配置。
    /// 保存时用它保留错误配置，确保能给出明确校验提示。
    /// </summary>
    public static bool HasAnyConfiguredRole(BizSchemeDetail detail)
    {
        return AllRoles.Any(role => IsCollectEnabled(detail, role)
            || IsSaveEnabled(detail, role)
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
    /// 清理单个角色的全部采集和输出配置。
    /// </summary>
    public static void ClearRole(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        SetCollectEnabled(detail, role, false);
        SetSaveEnabled(detail, role, false);
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
        => $"{item.ItemName}{GetRoleName(role)}";
}
