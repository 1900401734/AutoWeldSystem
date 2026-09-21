namespace AutoWeldSystem.Core.DTOs.Mes.Response;

public class UserInfoRes
{
    /// <summary>
    /// 姓名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 员工号
    /// </summary>
    public string UserNumber { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称
    /// </summary>
    public string DeptName { get; set; } = string.Empty;

    /// <summary>
    /// 班组名称
    /// </summary>
    public string TeamName { get; set; } = string.Empty;
}
