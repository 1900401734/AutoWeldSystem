using System.ComponentModel;
using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.DTOs.Mes.Request;

public class PostReq<T>
{
    [DisplayName("接口编码")]
    public ApiCode ApiCode { get; set; }
    
    [DisplayName("接口名称")]
    public string ApiName { get; set; } = string.Empty;

    [DisplayName("实际交互数据对象")]
    public T Data { get; set; } = default!;
}
