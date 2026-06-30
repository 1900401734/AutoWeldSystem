namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序名称解析规则。
/// 统一维护程序名称里的业务片段，避免 MES 下载和本地生成逻辑各写一套。
/// </summary>
public static class ProgramNameRules
{
    private const string ComponentStartMarker = "_CX_";
    private const string ComponentEndMarker = "_DH_";

    /// <summary>
    /// 从程序名称中提取零组件代码。
    /// 当前程序名称格式为：设备编号_CX_零组件代码_DH_流水号_产品工号。
    /// </summary>
    /// <param name="programName">MES 或本地维护的程序名称。</param>
    /// <param name="componentCode">解析成功时返回零组件代码；失败时返回空字符串。</param>
    /// <returns>解析到非空零组件代码时返回 true。</returns>
    public static bool TryExtractComponentCode(string? programName, out string componentCode)
    {
        componentCode = string.Empty;
        if (string.IsNullOrWhiteSpace(programName))
        {
            return false;
        }

        var normalizedName = programName.Trim();
        var startIndex = normalizedName.IndexOf(ComponentStartMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return false;
        }

        var componentStart = startIndex + ComponentStartMarker.Length;
        var endIndex = normalizedName.IndexOf(ComponentEndMarker, componentStart, StringComparison.OrdinalIgnoreCase);
        if (endIndex <= componentStart)
        {
            return false;
        }

        var extracted = normalizedName[componentStart..endIndex].Trim();
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return false;
        }

        componentCode = extracted;
        return true;
    }
}
