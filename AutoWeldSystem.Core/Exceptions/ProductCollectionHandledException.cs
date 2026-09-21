namespace AutoWeldSystem.Core.Exceptions;

/// <summary>
/// 产品数据已被上位机接收，但因本地配置/判定规则错误不能落库。
/// PLC 握手仍应反馈“已接收”，避免设备一直等待结果。
/// </summary>
public sealed class ProductCollectionHandledException : Exception
{
    public ProductCollectionHandledException(string source, string message, string? detail = null)
        : base(message)
    {
        SourceName = source;
        Detail = string.IsNullOrWhiteSpace(detail) ? message : detail;
    }

    public string SourceName { get; }

    public string Detail { get; }
}
